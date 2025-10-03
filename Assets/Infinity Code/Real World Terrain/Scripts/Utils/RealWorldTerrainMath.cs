/*         INFINITY CODE         */
/*   https://infinity-code.com   */

using System;
using System.Collections.Generic;
using UnityEngine;

namespace InfinityCode.RealWorldTerrain
{
    /// <summary>
    /// Provides utility methods for mathematical operations related to terrain generation.
    /// </summary>
    public static class RealWorldTerrainMath
    {
        /// <summary>
        /// Degrees-to-radians conversion constant.
        /// </summary>
        public const double DEG2RAD = Math.PI / 180;
        
        /// <summary>
        /// PI * 4
        /// </summary>
        public const float PI4 = 4 * Mathf.PI;
        
        /// <summary>
        /// The angle between the two points in degree.
        /// </summary>
        /// <param name="point1">Point 1</param>
        /// <param name="point2">Point 2</param>
        /// <returns>Angle in degree</returns>
        public static float Angle2D(Vector2 point1, Vector2 point2)
        {
            return Mathf.Atan2((point2.y - point1.y), (point2.x - point1.x)) * Mathf.Rad2Deg;
        }

        /// <summary>
        /// The angle between the two points in degree.
        /// </summary>
        /// <param name="point1">Point 1</param>
        /// <param name="point2">Point 2</param>
        /// <returns>Angle in degree</returns>
        public static float Angle2D(Vector3 point1, Vector3 point2)
        {
            return Mathf.Atan2((point2.z - point1.z), (point2.x - point1.x)) * Mathf.Rad2Deg;
        }

        /// <summary>
        /// The angle between the three points in degree.
        /// </summary>
        /// <param name="point1">Point 1</param>
        /// <param name="point2">Point 2</param>
        /// <param name="point3">Point 3</param>
        /// <param name="unsigned">Return a positive result.</param>
        /// <returns>Angle in degree</returns>
        public static float Angle2D(Vector3 point1, Vector3 point2, Vector3 point3, bool unsigned = true)
        {
            float angle1 = Angle2D(point1, point2);
            float angle2 = Angle2D(point2, point3);
            float angle = angle1 - angle2;
            if (angle > 180) angle -= 360;
            if (angle < -180) angle += 360;
            if (unsigned) angle = Mathf.Abs(angle);
            return angle;
        }

        /// <summary>
        /// The angle between the two points in radians.
        /// </summary>
        /// <param name="point1">Point 1</param>
        /// <param name="point2">Point 2</param>
        /// <param name="offset">Result offset in degrees.</param>
        /// <returns>Angle in radians</returns>
        public static float Angle2DRad(Vector3 point1, Vector3 point2, float offset)
        {
            return Mathf.Atan2((point2.z - point1.z), (point2.x - point1.x)) + offset * Mathf.Deg2Rad;
        }

        /// <summary>
        /// Clamps value between min and max and returns value.
        /// </summary>
        /// <param name="n">Value</param>
        /// <param name="minValue">Min</param>
        /// <param name="maxValue">Max</param>
        /// <returns>Value in the range between the min and max.</returns>
        public static double Clamp(double n, double minValue, double maxValue)
        {
            if (n < minValue) return minValue;
            if (n > maxValue) return maxValue;
            return n;
        }

        /// <summary>
        /// Clamps a value between a minimum double and maximum double value.
        /// </summary>
        /// <param name="n">Value</param>
        /// <param name="minValue">Minimum</param>
        /// <param name="maxValue">Maximum</param>
        /// <returns>Value between a minimum and maximum.</returns>
        public static double Clip(double n, double minValue, double maxValue)
        {
            if (n < minValue) return minValue;
            if (n > maxValue) return maxValue;
            return n;
        }
        
        /// <summary>
        /// The distance between two geographical coordinates.
        /// </summary>
        /// <param name="point1">Coordinate (X - Lng, Y - Lat)</param>
        /// <param name="point2">Coordinate (X - Lng, Y - Lat)</param>
        /// <returns>Distance (km).</returns>
        public static Vector2 DistanceBetweenPoints(Vector2 point1, Vector2 point2)
        {
            Vector2 range = point1 - point2;

            double scfY = Math.Sin(point1.y * Mathf.Deg2Rad);
            double sctY = Math.Sin(point2.y * Mathf.Deg2Rad);
            double ccfY = Math.Cos(point1.y * Mathf.Deg2Rad);
            double cctY = Math.Cos(point2.y * Mathf.Deg2Rad);
            double cX = Math.Cos(range.x * Mathf.Deg2Rad);
            double sizeX1 = Math.Abs(RealWorldTerrainGeo.EARTH_RADIUS * Math.Acos(scfY * scfY + ccfY * ccfY * cX));
            double sizeX2 = Math.Abs(RealWorldTerrainGeo.EARTH_RADIUS * Math.Acos(sctY * sctY + cctY * cctY * cX));
            float sizeX = (float)((sizeX1 + sizeX2) / 2.0);
            float sizeY = (float)(RealWorldTerrainGeo.EARTH_RADIUS * Math.Acos(scfY * sctY + ccfY * cctY));
            return new Vector2(sizeX, sizeY);
        }

        /// <summary>
        /// Calculates the distance between two geographical points.
        /// </summary>
        /// <param name="x1">Longitude of the first point</param>
        /// <param name="y1">Latitude of the first point</param>
        /// <param name="x2">Longitude of the second point</param>
        /// <param name="y2">Latitude of the second point</param>
        /// <param name="dx">Output distance in the x direction (longitude)</param>
        /// <param name="dy">Output distance in the y direction (latitude)</param>
        public static void DistanceBetweenPoints(double x1, double y1, double x2, double y2, out double dx, out double dy)
        {
            double rx = x1 - x2;
            double scfY = Math.Sin(y1 * Mathf.Deg2Rad);
            double sctY = Math.Sin(y2 * Mathf.Deg2Rad);
            double ccfY = Math.Cos(y1 * Mathf.Deg2Rad);
            double cctY = Math.Cos(y2 * Mathf.Deg2Rad);
            double cX = Math.Cos(rx * Mathf.Deg2Rad);
            double sizeX1 = Math.Abs(RealWorldTerrainGeo.EARTH_RADIUS * Math.Acos(scfY * scfY + ccfY * ccfY * cX));
            double sizeX2 = Math.Abs(RealWorldTerrainGeo.EARTH_RADIUS * Math.Acos(sctY * sctY + cctY * cctY * cX));
            dx = (sizeX1 + sizeX2) / 2.0;
            dy = RealWorldTerrainGeo.EARTH_RADIUS * Math.Acos(scfY * sctY + ccfY * cctY);
        }
        
        /// <summary>
        /// Calculates the center point and zoom level for a given set of geographic coordinates.
        /// </summary>
        /// <param name="positions">Array of geographic coordinates (longitude and latitude).</param>
        /// <param name="center">Output center point of the geographic coordinates.</param>
        /// <param name="zoom">Output zoom level that encompasses all the geographic coordinates.</param>
        public static void GetCenterPointAndZoom(double[] positions, out Vector2 center, out int zoom)
        {
            double minX = Single.MaxValue;
            double minY = Single.MaxValue;
            double maxX = Single.MinValue;
            double maxY = Single.MinValue;

            for (int i = 0; i < positions.Length; i += 2)
            {
                double lng = positions[i];
                double lat = positions[i + 1];
                if (lng < minX) minX = lng;
                if (lat < minY) minY = lat;
                if (lng > maxX) maxX = lng;
                if (lat > maxY) maxY = lat;
            }

            double rx = maxX - minX;
            double ry = maxY - minY;
            double cx = rx / 2 + minX;
            double cy = ry / 2 + minY;

            center = new Vector2((float)cx, (float)cy);

            int width = 1024;
            int height = 1024;

            float countX = width / (float)RealWorldTerrainUtils.TILE_SIZE / 2;
            float countY = height / (float)RealWorldTerrainUtils.TILE_SIZE / 2;

            for (int z = 20; z > 4; z--)
            {
                bool success = true;

                double tcx, tcy;
                RealWorldTerrainGeo.LatLongToTile(cx, cy, z, out tcx, out tcy);

                for (int i = 0; i < positions.Length; i += 2)
                {
                    double lng = positions[i];
                    double lat = positions[i + 1];
                    double px, py;
                    RealWorldTerrainGeo.LatLongToTile(lng, lat, z, out px, out py);
                    px -= tcx - countX;
                    py -= tcy - countY;

                    if (px < 0 || py < 0 || px > width || py > height)
                    {
                        success = false;
                        break;
                    }
                }

                if (success)
                {
                    zoom = z;
                    return;
                }
            }

            zoom = 3;
        }

        /// <summary>
        /// Calculates the center point and zoom level for a given set of geographic coordinates.
        /// </summary>
        /// <param name="positions">Array of geographic coordinates (longitude and latitude).</param>
        /// <param name="center">Output center point of the geographic coordinates.</param>
        /// <param name="zoom">Output zoom level that encompasses all the geographic coordinates.</param>
        public static void GetCenterPointAndZoom(Vector2[] positions, out Vector2 center, out int zoom)
        {
            float minX = Single.MaxValue;
            float minY = Single.MaxValue;
            float maxX = Single.MinValue;
            float maxY = Single.MinValue;

            foreach (Vector2 p in positions)
            {
                if (p.x < minX) minX = p.x;
                if (p.y < minY) minY = p.y;
                if (p.x > maxX) maxX = p.x;
                if (p.y > maxY) maxY = p.y;
            }

            float rx = maxX - minX;
            float ry = maxY - minY;
            double cx = rx / 2 + minX;
            double cy = ry / 2 + minY;

            center = new Vector2((float)cx, (float)cy);

            int width = 1024;
            int height = 1024;

            float countX = width / (float)RealWorldTerrainUtils.TILE_SIZE / 2;
            float countY = height / (float)RealWorldTerrainUtils.TILE_SIZE / 2;

            for (int z = 20; z > 4; z--)
            {
                bool success = true;

                double tcx, tcy;
                RealWorldTerrainGeo.LatLongToTile(cx, cy, z, out tcx, out tcy);

                foreach (Vector2 pos in positions)
                {
                    double px, py;
                    RealWorldTerrainGeo.LatLongToTile(pos.x, pos.y, z, out px, out py);
                    px -= tcx - countX;
                    py -= tcy - countY;

                    if (px < 0 || py < 0 || px > width || py > height)
                    {
                        success = false;
                        break;
                    }
                }
                if (success)
                {
                    zoom = z;
                    return;
                }
            }

            zoom = 3;
        }

        /// <summary>
        /// Calculates the intersection point of two lines in 2D space.
        /// </summary>
        /// <param name="p11">Start point of the first line.</param>
        /// <param name="p12">End point of the first line.</param>
        /// <param name="p21">Start point of the second line.</param>
        /// <param name="p22">End point of the second line.</param>
        /// <param name="state">Output state indicating the result of the calculation. -2: Not calculated yet, -1: Lines are parallel, 0: Lines are coincident, 1: Intersection point found.</param>
        /// <returns>The intersection point if it exists, otherwise a default Vector2.</returns>
        public static Vector2 GetIntersectionPointOfTwoLines(Vector2 p11, Vector2 p12, Vector2 p21, Vector2 p22,
            out int state)
        {
            state = -2;
            Vector2 result = new Vector2();
            float m = (p22.x - p21.x) * (p11.y - p21.y) - (p22.y - p21.y) * (p11.x - p21.x);
            float n = (p22.y - p21.y) * (p12.x - p11.x) - (p22.x - p21.x) * (p12.y - p11.y);

            float Ua = m / n;

            if (n == 0 && m != 0) state = -1;
            else if (m == 0 && n == 0) state = 0;
            else
            {
                result.x = p11.x + Ua * (p12.x - p11.x);
                result.y = p11.y + Ua * (p12.y - p11.y);

                if (((result.x >= p11.x || result.x <= p11.x) && (result.x >= p21.x || result.x <= p21.x))
                    && ((result.y >= p11.y || result.y <= p11.y) && (result.y >= p21.y || result.y <= p21.y)))
                    state = 1;
            }
            return result;
        }

        /// <summary>
        /// Calculates the intersection point of two lines in 3D space, projected onto the XZ plane.
        /// </summary>
        /// <param name="p11">Start point of the first line.</param>
        /// <param name="p12">End point of the first line.</param>
        /// <param name="p21">Start point of the second line.</param>
        /// <param name="p22">End point of the second line.</param>
        /// <param name="state">Output state indicating the result of the calculation. -2: Not calculated yet, -1: Lines are parallel, 0: Lines are coincident, 1: Intersection point found.</param>
        /// <returns>The intersection point if it exists, otherwise a default Vector2.</returns>
        public static Vector2 GetIntersectionPointOfTwoLines(Vector3 p11, Vector3 p12, Vector3 p21, Vector3 p22,
            out int state)
        {
            return GetIntersectionPointOfTwoLines(new Vector2(p11.x, p11.z), new Vector2(p12.x, p12.z),
                new Vector2(p21.x, p21.z), new Vector2(p22.x, p22.z), out state);
        }
        
        /// <summary>
        /// Determines if three points in 3D space form a clockwise rotation when projected onto the XZ plane.
        /// </summary>
        /// <param name="A">First point</param>
        /// <param name="B">Second point</param>
        /// <param name="C">Third point</param>
        /// <returns>True if the points form a clockwise rotation, false otherwise.</returns>
        public static bool IsClockWise(Vector3 A, Vector3 B, Vector3 C)
        {
            return (B.x - A.x) * (C.z - A.z) - (C.x - A.x) * (B.z - A.z) > 0;
        }

        /// <summary>
        /// Determines if a sequence of points in 3D space forms a clockwise rotation when projected onto the XZ plane.
        /// </summary>
        /// <param name="points">Array of points in 3D space.</param>
        /// <param name="count">Number of points to consider from the start of the array.</param>
        /// <returns>True if the points form a clockwise rotation, false otherwise.</returns>
        public static bool IsClockwise(Vector3[] points, int count)
        {
            double sum = 0d;
            for (int i = 0; i < count; i++)
            {
                Vector3 v1 = points[i];
                Vector3 v2 = points[(i + 1) % count];
                sum += (v2.x - v1.x) * (v2.z + v1.z);
            }

            return sum > 0d;
        }

        /// <summary>
        /// Determines if a point is inside a polygon in 3D space, considering only the XZ plane.
        /// </summary>
        /// <param name="poly">Array of points forming the polygon.</param>
        /// <param name="x">X coordinate of the point.</param>
        /// <param name="y">Y coordinate of the point (considered as Z in 3D space).</param>
        /// <returns>True if the point is inside the polygon, false otherwise.</returns>
        public static bool IsPointInPolygon(Vector3[] poly, float x, float y)
        {
            int i, j;
            bool c = false;
            for (i = 0, j = poly.Length - 1; i < poly.Length; j = i++)
            {
                if ((poly[i].z <= y && y < poly[j].z ||
                     poly[j].z <= y && y < poly[i].z) &&
                    x < (poly[j].x - poly[i].x) * (y - poly[i].z) / (poly[j].z - poly[i].z) + poly[i].x)
                {
                    c = !c;
                }
            }
            return c;
        }

        /// <summary>
        /// Determines if a point is inside a polygon in 3D space, considering only the XZ plane.
        /// </summary>
        /// <param name="poly">Array of points forming the polygon.</param>
        /// <param name="length">Number of points to consider from the start of the array.</param>
        /// <param name="x">X coordinate of the point.</param>
        /// <param name="y">Y coordinate of the point (considered as Z in 3D space).</param>
        /// <returns>True if the point is inside the polygon, false otherwise.</returns>
        public static bool IsPointInPolygon(Vector3[] poly, int length, float x, float y)
        {
            int i, j;
            bool c = false;
            for (i = 0, j = length - 1; i < length; j = i++)
            {
                if ((poly[i].z <= y && y < poly[j].z ||
                     poly[j].z <= y && y < poly[i].z) &&
                    x < (poly[j].x - poly[i].x) * (y - poly[i].z) / (poly[j].z - poly[i].z) + poly[i].x)
                {
                    c = !c;
                }
            }
            return c;
        }

        /// <summary>
        /// Determines if a point is inside a polygon in 3D space, considering only the XZ plane.
        /// </summary>
        /// <param name="poly">List of points forming the polygon.</param>
        /// <param name="x">X coordinate of the point.</param>
        /// <param name="y">Y coordinate of the point (considered as Z in 3D space).</param>
        /// <returns>True if the point is inside the polygon, false otherwise.</returns>
        public static bool IsPointInPolygon(List<Vector3> poly, float x, float y)
        {
            int i, j;
            bool c = false;
            for (i = 0, j = poly.Count - 1; i < poly.Count; j = i++)
            {
                if ((poly[i].z <= y && y < poly[j].z ||
                     poly[j].z <= y && y < poly[i].z) &&
                    x < (poly[j].x - poly[i].x) * (y - poly[i].z) / (poly[j].z - poly[i].z) + poly[i].x)
                {
                    c = !c;
                }
            }
            return c;
        }

        /// <summary>
        /// Determines if a point is inside a polygon in 3D space, considering only the XZ plane.
        /// </summary>
        /// <param name="poly">List of points forming the polygon.</param>
        /// <param name="point">Point to check.</param>
        /// <returns>True if the point is inside the polygon, false otherwise.</returns>
        public static bool IsPointInPolygon(List<Vector3> poly, Vector3 point)
        {
            return IsPointInPolygon(poly, point.x, point.z);
        }
        
        /// <summary>
        /// Clamps a value between a minimum and maximum integer value.
        /// </summary>
        /// <param name="val">Value to be clamped.</param>
        /// <param name="min">Minimum value. Default is 32.</param>
        /// <param name="max">Maximum value. Default is 4096.</param>
        /// <returns>Value clamped between the min and max.</returns>
        public static int Limit(int val, int min = 32, int max = 4096)
        {
            return Mathf.Clamp(val, min, max);
        }

        /// <summary>
        /// Clamps a value between a minimum and maximum integer value, ensuring the result is a power of two.
        /// </summary>
        /// <param name="val">Value to be clamped and adjusted to the nearest power of two.</param>
        /// <param name="min">Minimum value. Default is 32.</param>
        /// <param name="max">Maximum value. Default is 4096.</param>
        /// <returns>Value clamped between the min and max, adjusted to the nearest power of two.</returns>
        public static int LimitPowTwo(int val, int min = 32, int max = 4096)
        {
            return Mathf.Clamp(Mathf.ClosestPowerOfTwo(val), min, max);
        }
        
        /// <summary>
        /// Calculates the nearest point on a line segment to a given point in 2D space.
        /// </summary>
        /// <param name="point">The point to find the nearest point on the line segment to.</param>
        /// <param name="lineStart">The start point of the line segment.</param>
        /// <param name="lineEnd">The end point of the line segment.</param>
        /// <returns>The nearest point on the line segment to the given point.</returns>
        public static Vector2 NearestPointStrict(Vector2 point, Vector2 lineStart, Vector2 lineEnd)
        {
            Vector2 fullDirection = lineEnd - lineStart;
            Vector2 lineDirection = fullDirection.normalized;
            float closestPoint = Vector2.Dot(point - lineStart, lineDirection) / Vector2.Dot(lineDirection, lineDirection);
            return lineStart + Mathf.Clamp(closestPoint, 0, fullDirection.magnitude) * lineDirection;
        }

        /// <summary>
        /// Repeats the value in the range from minValue to maxValue.
        /// </summary>
        /// <param name="n">The value to repeat.</param>
        /// <param name="minValue">The minimum value in the range.</param>
        /// <param name="maxValue">The maximum value in the range.</param>
        /// <returns>The repeated value in the range from minValue to maxValue.</returns>
        public static double Repeat(double n, double minValue, double maxValue)
        {
            if (double.IsInfinity(n) || double.IsInfinity(minValue) || double.IsInfinity(maxValue) || double.IsNaN(n) || double.IsNaN(minValue) || double.IsNaN(maxValue)) return n;

            double range = maxValue - minValue;
            while (n < minValue || n > maxValue)
            {
                if (n < minValue) n += range;
                else if (n > maxValue) n -= range;
            }
            return n;
        }
        
        /// <summary>
        /// Triangulates a polygon defined by a list of points in 2D space.
        /// </summary>
        /// <param name="points">List of points forming the polygon.</param>
        /// <returns>An enumerable of indices representing the triangles that make up the polygon.</returns>
        public static IEnumerable<int> Triangulate(List<Vector2> points)
        {
            List<int> indices = new List<int>();

            int n = points.Count;
            if (n < 3) return indices;

            int[] V = new int[n];
            if (TriangulateArea(points) > 0) for (int v = 0; v < n; v++) V[v] = v;
            else for (int v = 0; v < n; v++) V[v] = (n - 1) - v;

            int nv = n;
            int count = 2 * nv;
            for (int v = nv - 1; nv > 2;)
            {
                if ((count--) <= 0) return indices;

                int u = v;
                if (nv <= u) u = 0;
                v = u + 1;
                if (nv <= v) v = 0;
                int w = v + 1;
                if (nv <= w) w = 0;

                if (TriangulateSnip(points, u, v, w, nv, V))
                {
                    int s, t;
                    indices.Add(V[u]);
                    indices.Add(V[v]);
                    indices.Add(V[w]);
                    for (s = v, t = v + 1; t < nv; s++, t++) V[s] = V[t];
                    nv--;
                    count = 2 * nv;
                }
            }

            indices.Reverse();
            return indices;
        }

        private static float TriangulateArea(List<Vector2> points)
        {
            int n = points.Count;
            float A = 0.0f;
            for (int p = n - 1, q = 0; q < n; p = q++)
            {
                Vector2 pval = points[p];
                Vector2 qval = points[q];
                A += pval.x * qval.y - qval.x * pval.y;
            }
            return (A * 0.5f);
        }

        private static bool TriangulateInsideTriangle(Vector2 A, Vector2 B, Vector2 C, Vector2 P)
        {
            float bp = (C.x - B.x) * (P.y - B.y) - (C.y - B.y) * (P.x - B.x);
            float ap = (B.x - A.x) * (P.y - A.y) - (B.y - A.y) * (P.x - A.x);
            float cp = (A.x - C.x) * (P.y - C.y) - (A.y - C.y) * (P.x - C.x);
            return bp >= 0.0f && cp >= 0.0f && ap >= 0.0f;
        }

        private static bool TriangulateSnip(List<Vector2> points, int u, int v, int w, int n, int[] V)
        {
            Vector2 A = points[V[u]];
            Vector2 B = points[V[v]];
            Vector2 C = points[V[w]];
            if (Mathf.Epsilon > (B.x - A.x) * (C.y - A.y) - (B.y - A.y) * (C.x - A.x)) return false;
            for (int p = 0; p < n; p++)
            {
                if (p == u || p == v || p == w) continue;
                if (TriangulateInsideTriangle(A, B, C, points[V[p]])) return false;
            }
            return true;
        }
    }
}