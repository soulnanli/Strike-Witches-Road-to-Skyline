using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace SWRTS.Demo1
{
    public enum DemoRangeShapeKind
    {
        Circle,
        Sector
    }

    public readonly struct DemoRangeShape
    {
        public readonly DemoRangeShapeKind Kind;
        public readonly Vector2 Center;
        public readonly float Radius;
        public readonly Vector2 Facing;
        public readonly float AngleDegrees;

        private DemoRangeShape(DemoRangeShapeKind kind, Vector2 center, float radius, Vector2 facing, float angleDegrees)
        {
            Kind = kind;
            Center = center;
            Radius = Mathf.Max(0f, radius);
            Facing = facing.sqrMagnitude > 0.0001f ? facing.normalized : Vector2.right;
            AngleDegrees = angleDegrees;
        }

        public static DemoRangeShape Circle(Vector3 center, float radius)
        {
            return new DemoRangeShape(DemoRangeShapeKind.Circle, new Vector2(center.x, center.z), radius,
                Vector2.right, 360f);
        }

        public static DemoRangeShape Sector(Vector3 center, Vector3 facing, float radius, float angleDegrees)
        {
            return new DemoRangeShape(DemoRangeShapeKind.Sector, new Vector2(center.x, center.z), radius,
                new Vector2(facing.x, facing.z), Mathf.Clamp(angleDegrees, 1f, 360f));
        }

        public bool Contains(Vector2 point)
        {
            return SignedDistance(point) >= 0f;
        }

        public float SignedDistance(Vector2 point)
        {
            Vector2 offset = point - Center;
            float distance = offset.magnitude;
            float radialMargin = Radius - distance;
            if (Kind == DemoRangeShapeKind.Circle || AngleDegrees >= 359.9f)
                return radialMargin;
            if (distance < 0.0001f)
                return 0f;

            float facingAngle = Mathf.Atan2(Facing.y, Facing.x) * Mathf.Rad2Deg;
            float pointAngle = Mathf.Atan2(offset.y, offset.x) * Mathf.Rad2Deg;
            float angularDelta = Mathf.Abs(Mathf.DeltaAngle(facingAngle, pointAngle)) * Mathf.Deg2Rad;
            float angularMargin = (AngleDegrees * 0.5f * Mathf.Deg2Rad - angularDelta) * distance;
            return Mathf.Min(radialMargin, angularMargin);
        }
    }

    public static class Demo1RangeContourBuilder
    {
        private readonly struct GridPoint : IEquatable<GridPoint>
        {
            public readonly int X;
            public readonly int Z;

            public GridPoint(int x, int z)
            {
                X = x;
                Z = z;
            }

            public bool Equals(GridPoint other)
            {
                return X == other.X && Z == other.Z;
            }

            public override bool Equals(object obj)
            {
                return obj is GridPoint other && Equals(other);
            }

            public override int GetHashCode()
            {
                unchecked
                {
                    return X * 397 ^ Z;
                }
            }
        }

        private readonly struct GridEdge : IEquatable<GridEdge>
        {
            public readonly GridPoint A;
            public readonly GridPoint B;

            public GridEdge(GridPoint first, GridPoint second)
            {
                if (first.X < second.X || first.X == second.X && first.Z <= second.Z)
                {
                    A = first;
                    B = second;
                }
                else
                {
                    A = second;
                    B = first;
                }
            }

            public bool Equals(GridEdge other)
            {
                return A.Equals(other.A) && B.Equals(other.B);
            }

            public override bool Equals(object obj)
            {
                return obj is GridEdge other && Equals(other);
            }

            public override int GetHashCode()
            {
                unchecked
                {
                    return A.GetHashCode() * 397 ^ B.GetHashCode();
                }
            }
        }

        public static List<List<Vector2>> BuildUnion(IReadOnlyList<DemoRangeShape> sourceShapes,
            float preferredCellSize = 0.5f, int maxCellsPerAxis = 256)
        {
            List<DemoRangeShape> shapes = sourceShapes == null
                ? new List<DemoRangeShape>()
                : sourceShapes.Where(shape => shape.Radius > 0.001f).ToList();
            if (shapes.Count == 0)
                return new List<List<Vector2>>();

            float minX = shapes.Min(shape => shape.Center.x - shape.Radius);
            float maxX = shapes.Max(shape => shape.Center.x + shape.Radius);
            float minZ = shapes.Min(shape => shape.Center.y - shape.Radius);
            float maxZ = shapes.Max(shape => shape.Center.y + shape.Radius);
            int gridLimit = Mathf.Max(16, maxCellsPerAxis);
            int innerLimit = Mathf.Max(8, gridLimit - 4);
            float cellSize = Mathf.Max(0.1f, preferredCellSize,
                (maxX - minX) / innerLimit, (maxZ - minZ) / innerLimit);
            minX -= cellSize * 2f;
            minZ -= cellSize * 2f;
            int cellsX = Mathf.Max(1, Mathf.CeilToInt((maxX - minX + cellSize * 2f) / cellSize));
            int cellsZ = Mathf.Max(1, Mathf.CeilToInt((maxZ - minZ + cellSize * 2f) / cellSize));

            float[,] samples = new float[cellsX + 1, cellsZ + 1];
            for (int z = 0; z <= cellsZ; z++)
            {
                for (int x = 0; x <= cellsX; x++)
                    samples[x, z] = SignedDistanceToUnion(shapes,
                        new Vector2(minX + x * cellSize, minZ + z * cellSize));
            }

            Dictionary<GridPoint, List<GridPoint>> adjacency = new Dictionary<GridPoint, List<GridPoint>>();
            for (int z = 0; z < cellsZ; z++)
            {
                for (int x = 0; x < cellsX; x++)
                {
                    int mask = (samples[x, z] >= 0f ? 1 : 0) |
                               (samples[x + 1, z] >= 0f ? 2 : 0) |
                               (samples[x + 1, z + 1] >= 0f ? 4 : 0) |
                               (samples[x, z + 1] >= 0f ? 8 : 0);
                    if (mask == 0 || mask == 15)
                        continue;
                    bool centerInside = SignedDistanceToUnion(shapes,
                        new Vector2(minX + (x + 0.5f) * cellSize, minZ + (z + 0.5f) * cellSize)) >= 0f;
                    AddMarchingSquare(adjacency, x, z, mask, centerInside);
                }
            }

            return StitchContours(adjacency, samples, minX, minZ, cellSize);
        }

        private static float SignedDistanceToUnion(IReadOnlyList<DemoRangeShape> shapes, Vector2 point)
        {
            float distance = float.NegativeInfinity;
            for (int i = 0; i < shapes.Count; i++)
                distance = Mathf.Max(distance, shapes[i].SignedDistance(point));
            return distance;
        }

        private static void AddMarchingSquare(Dictionary<GridPoint, List<GridPoint>> adjacency,
            int x, int z, int mask, bool centerInside)
        {
            GridPoint bottom = new GridPoint(x * 2 + 1, z * 2);
            GridPoint right = new GridPoint(x * 2 + 2, z * 2 + 1);
            GridPoint top = new GridPoint(x * 2 + 1, z * 2 + 2);
            GridPoint left = new GridPoint(x * 2, z * 2 + 1);
            switch (mask)
            {
                case 1: AddSegment(adjacency, left, bottom); break;
                case 2: AddSegment(adjacency, bottom, right); break;
                case 3: AddSegment(adjacency, left, right); break;
                case 4: AddSegment(adjacency, right, top); break;
                case 5:
                    if (centerInside)
                    {
                        AddSegment(adjacency, left, top);
                        AddSegment(adjacency, bottom, right);
                    }
                    else
                    {
                        AddSegment(adjacency, left, bottom);
                        AddSegment(adjacency, right, top);
                    }
                    break;
                case 6: AddSegment(adjacency, bottom, top); break;
                case 7: AddSegment(adjacency, left, top); break;
                case 8: AddSegment(adjacency, top, left); break;
                case 9: AddSegment(adjacency, top, bottom); break;
                case 10:
                    if (centerInside)
                    {
                        AddSegment(adjacency, left, bottom);
                        AddSegment(adjacency, right, top);
                    }
                    else
                    {
                        AddSegment(adjacency, left, top);
                        AddSegment(adjacency, bottom, right);
                    }
                    break;
                case 11: AddSegment(adjacency, right, top); break;
                case 12: AddSegment(adjacency, left, right); break;
                case 13: AddSegment(adjacency, bottom, right); break;
                case 14: AddSegment(adjacency, left, bottom); break;
            }
        }

        private static void AddSegment(Dictionary<GridPoint, List<GridPoint>> adjacency,
            GridPoint first, GridPoint second)
        {
            AddNeighbor(adjacency, first, second);
            AddNeighbor(adjacency, second, first);
        }

        private static void AddNeighbor(Dictionary<GridPoint, List<GridPoint>> adjacency,
            GridPoint point, GridPoint neighbor)
        {
            if (!adjacency.TryGetValue(point, out List<GridPoint> neighbors))
            {
                neighbors = new List<GridPoint>(2);
                adjacency.Add(point, neighbors);
            }
            if (!neighbors.Contains(neighbor))
                neighbors.Add(neighbor);
        }

        private static List<List<Vector2>> StitchContours(Dictionary<GridPoint, List<GridPoint>> adjacency,
            float[,] samples, float minX, float minZ, float cellSize)
        {
            List<List<Vector2>> contours = new List<List<Vector2>>();
            HashSet<GridEdge> visited = new HashSet<GridEdge>();
            foreach (KeyValuePair<GridPoint, List<GridPoint>> pair in adjacency)
            {
                for (int neighborIndex = 0; neighborIndex < pair.Value.Count; neighborIndex++)
                {
                    GridPoint start = pair.Key;
                    GridPoint next = pair.Value[neighborIndex];
                    GridEdge firstEdge = new GridEdge(start, next);
                    if (visited.Contains(firstEdge))
                        continue;

                    List<GridPoint> gridContour = new List<GridPoint> { start };
                    GridPoint current = next;
                    visited.Add(firstEdge);
                    int guard = adjacency.Count * 2 + 4;
                    while (!current.Equals(start) && guard-- > 0)
                    {
                        gridContour.Add(current);
                        if (!adjacency.TryGetValue(current, out List<GridPoint> neighbors))
                            break;
                        bool advanced = false;
                        for (int i = 0; i < neighbors.Count; i++)
                        {
                            GridPoint candidate = neighbors[i];
                            GridEdge edge = new GridEdge(current, candidate);
                            if (visited.Contains(edge))
                                continue;
                            current = candidate;
                            visited.Add(edge);
                            advanced = true;
                            break;
                        }
                        if (!advanced)
                            break;
                    }

                    if (!current.Equals(start) || gridContour.Count < 3)
                        continue;
                    List<Vector2> contour = new List<Vector2>(gridContour.Count);
                    for (int i = 0; i < gridContour.Count; i++)
                        contour.Add(InterpolateGridEdge(gridContour[i], samples, minX, minZ, cellSize));
                    contours.Add(SmoothClosedContour(contour, 2));
                }
            }
            return contours;
        }

        private static List<Vector2> SmoothClosedContour(IReadOnlyList<Vector2> source, int iterations)
        {
            List<Vector2> current = source == null ? new List<Vector2>() : new List<Vector2>(source);
            for (int iteration = 0; iteration < iterations && current.Count >= 3; iteration++)
            {
                List<Vector2> smoothed = new List<Vector2>(current.Count * 2);
                for (int i = 0; i < current.Count; i++)
                {
                    Vector2 first = current[i];
                    Vector2 second = current[(i + 1) % current.Count];
                    smoothed.Add(Vector2.Lerp(first, second, 0.25f));
                    smoothed.Add(Vector2.Lerp(first, second, 0.75f));
                }
                current = smoothed;
            }
            return current;
        }

        private static Vector2 InterpolateGridEdge(GridPoint point, float[,] samples,
            float minX, float minZ, float cellSize)
        {
            if ((point.X & 1) == 1)
            {
                int x = (point.X - 1) / 2;
                int z = point.Z / 2;
                float t = ZeroCrossing(samples[x, z], samples[x + 1, z]);
                return new Vector2(minX + (x + t) * cellSize, minZ + z * cellSize);
            }

            int verticalX = point.X / 2;
            int verticalZ = (point.Z - 1) / 2;
            float verticalT = ZeroCrossing(samples[verticalX, verticalZ], samples[verticalX, verticalZ + 1]);
            return new Vector2(minX + verticalX * cellSize, minZ + (verticalZ + verticalT) * cellSize);
        }

        private static float ZeroCrossing(float first, float second)
        {
            float denominator = first - second;
            return Mathf.Abs(denominator) < 0.000001f ? 0.5f : Mathf.Clamp01(first / denominator);
        }
    }

    public sealed class Demo1RangeOverlay : MonoBehaviour
    {
        public static readonly Color DetectionColor = new Color(0.25f, 0.78f, 1f, 0.88f);

        private readonly List<LineRenderer> _detectionLines = new List<LineRenderer>();
        private int _lastSignature = int.MinValue;

        public void Sync(Demo1Simulation simulation, IEnumerable<int> selectedUnitIds)
        {
            List<DemoRangeShape> detectionShapes = new List<DemoRangeShape>();
            List<DemoUnitModel> selected = simulation == null || selectedUnitIds == null
                ? new List<DemoUnitModel>()
                : selectedUnitIds.Select(simulation.GetUnit)
                    .Where(unit => unit != null && unit.IsAlive && unit.Team == DemoTeam.Player &&
                                   unit.DeploymentState == DemoUnitDeploymentState.Active)
                    .OrderBy(unit => unit.Id)
                    .ToList();
            for (int i = 0; i < selected.Count; i++)
            {
                DemoUnitModel unit = selected[i];
                float visionRadius = simulation.GetEffectiveVisionRadius(unit.Id);
                if (unit.Stats.WitchVisionType == DemoWitchVisionType.Night)
                    detectionShapes.Add(DemoRangeShape.Circle(unit.Position, visionRadius));
                else if (unit.Stats.WitchVisionType == DemoWitchVisionType.Ordinary)
                    detectionShapes.Add(DemoRangeShape.Sector(unit.Position, unit.Facing, visionRadius, unit.Stats.VisionAngle));
            }

            int signature = BuildSignature(detectionShapes);
            if (signature == _lastSignature)
                return;
            _lastSignature = signature;
            RebuildLayer(_detectionLines, Demo1RangeContourBuilder.BuildUnion(detectionShapes),
                "Detection Range", DetectionColor, Demo1Drawing.OperationalLinePixelWidth, 0.055f);
        }

        private static int BuildSignature(IReadOnlyList<DemoRangeShape> detection)
        {
            unchecked
            {
                int hash = 17;
                return AppendShapes(hash, detection);
            }
        }

        private static int AppendShapes(int hash, IReadOnlyList<DemoRangeShape> shapes)
        {
            unchecked
            {
                hash = hash * 31 + shapes.Count;
                for (int i = 0; i < shapes.Count; i++)
                {
                    DemoRangeShape shape = shapes[i];
                    hash = hash * 31 + (int)shape.Kind;
                    hash = hash * 31 + Mathf.RoundToInt(shape.Center.x * 4f);
                    hash = hash * 31 + Mathf.RoundToInt(shape.Center.y * 4f);
                    hash = hash * 31 + Mathf.RoundToInt(shape.Radius * 10f);
                    hash = hash * 31 + Mathf.RoundToInt(Mathf.Atan2(shape.Facing.y, shape.Facing.x) * Mathf.Rad2Deg * 0.5f);
                    hash = hash * 31 + Mathf.RoundToInt(shape.AngleDegrees);
                }
                return hash;
            }
        }

        private void RebuildLayer(List<LineRenderer> pool, IReadOnlyList<List<Vector2>> contours,
            string namePrefix, Color color, float pixelWidth, float height)
        {
            while (pool.Count < contours.Count)
            {
                LineRenderer line = Demo1Drawing.CreateLine(transform, $"{namePrefix} {pool.Count + 1}", color, pixelWidth);
                line.loop = true;
                line.numCornerVertices = 4;
                line.enabled = false;
                pool.Add(line);
            }

            for (int i = 0; i < pool.Count; i++)
            {
                LineRenderer line = pool[i];
                bool visible = i < contours.Count && contours[i].Count >= 3;
                line.enabled = visible;
                if (!visible)
                    continue;
                List<Vector2> contour = contours[i];
                line.positionCount = contour.Count;
                for (int pointIndex = 0; pointIndex < contour.Count; pointIndex++)
                {
                    Vector2 point = contour[pointIndex];
                    line.SetPosition(pointIndex, new Vector3(point.x, height, point.y));
                }
            }
        }
    }
}
