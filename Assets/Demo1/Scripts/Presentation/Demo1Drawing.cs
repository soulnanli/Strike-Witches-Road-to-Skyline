using UnityEngine;

namespace SWRTS.Demo1
{
    public static class Demo1Drawing
    {
        private static Shader _flatColorShader;
        private static Shader _lineColorShader;

        public static LineRenderer CreateCircle(Transform parent, string name, Color color, float pixelWidth, int segments = 64)
        {
            GameObject circle = new GameObject(name);
            circle.transform.SetParent(parent, false);
            LineRenderer line = circle.AddComponent<LineRenderer>();
            line.useWorldSpace = true;
            line.loop = true;
            line.positionCount = segments;
            ConfigureScreenSpaceWidth(line, pixelWidth);
            line.numCornerVertices = 2;
            Material material = CreateLineMaterial(color);
            if (material != null)
                line.material = material;
            line.startColor = color;
            line.endColor = color;
            return line;
        }

        public static LineRenderer CreateLine(Transform parent, string name, Color color, float pixelWidth)
        {
            GameObject lineObject = new GameObject(name);
            lineObject.transform.SetParent(parent, false);
            LineRenderer line = lineObject.AddComponent<LineRenderer>();
            line.useWorldSpace = true;
            line.positionCount = 2;
            ConfigureScreenSpaceWidth(line, pixelWidth);
            Material material = CreateLineMaterial(color);
            if (material != null)
                line.material = material;
            line.startColor = color;
            line.endColor = color;
            return line;
        }

        public static LineRenderer CreateSector(Transform parent, string name, Color color, float pixelWidth, int segments = 40)
        {
            LineRenderer line = CreateLine(parent, name, color, pixelWidth);
            line.loop = false;
            line.positionCount = Mathf.Max(4, segments + 3);
            return line;
        }

        public static void SetCircle(LineRenderer line, Vector3 center, float radius, float y = 0.08f)
        {
            if (line == null)
                return;
            int count = line.positionCount;
            for (int i = 0; i < count; i++)
            {
                float angle = i * Mathf.PI * 2f / count;
                line.SetPosition(i, new Vector3(center.x + Mathf.Cos(angle) * radius, y, center.z + Mathf.Sin(angle) * radius));
            }
        }

        public static void SetSector(LineRenderer line, Vector3 center, Vector3 facing, float radius, float angleDegrees, float y = 0.08f)
        {
            if (line == null)
                return;
            facing.y = 0f;
            if (facing.sqrMagnitude < 0.001f)
                facing = Vector3.right;
            float centerAngle = Mathf.Atan2(facing.z, facing.x);
            float halfAngle = Mathf.Clamp(angleDegrees, 1f, 359f) * 0.5f * Mathf.Deg2Rad;
            int arcPoints = line.positionCount - 2;
            line.SetPosition(0, new Vector3(center.x, y, center.z));
            for (int i = 0; i < arcPoints; i++)
            {
                float t = arcPoints <= 1 ? 0.5f : (float)i / (arcPoints - 1);
                float angle = Mathf.Lerp(centerAngle - halfAngle, centerAngle + halfAngle, t);
                line.SetPosition(i + 1, new Vector3(center.x + Mathf.Cos(angle) * radius, y, center.z + Mathf.Sin(angle) * radius));
            }
            line.SetPosition(line.positionCount - 1, new Vector3(center.x, y, center.z));
        }

        public static Material CreateMaterial(Color color)
        {
            Shader shader = LoadShader(ref _flatColorShader, "Demo1FlatColor", "SWRTS/Demo1/FlatColor");
            if (shader == null)
                return null;
            Material material = new Material(shader);
            material.SetColor("_BaseColor", color);
            return material;
        }

        public static Material CreateMapMaterial(Texture2D texture, Color tint)
        {
            Material material = CreateMaterial(tint);
            if (material != null && texture != null)
            {
                material.SetTexture("_BaseMap", texture);
                // Unity's world Plane UVs run opposite to the previous RawImage map on screen.
                // Flip only the texture V axis so north remains screen-up without changing world coordinates.
                material.SetTextureScale("_BaseMap", new Vector2(1f, -1f));
                material.SetTextureOffset("_BaseMap", new Vector2(0f, 1f));
            }
            return material;
        }

        private static Material CreateLineMaterial(Color color)
        {
            Shader shader = LoadShader(ref _lineColorShader, "Demo1LineColor", "SWRTS/Demo1/LineColor");
            if (shader == null)
                return null;
            Material material = new Material(shader);
            material.SetColor("_BaseColor", Color.white);
            return material;
        }

        private static void ConfigureScreenSpaceWidth(LineRenderer line, float pixelWidth)
        {
            line.startWidth = 0f;
            line.endWidth = 0f;
            line.gameObject.AddComponent<Demo1ScreenSpaceLineWidth>().Initialize(pixelWidth);
        }

        private static Shader LoadShader(ref Shader cache, string resourceName, string shaderName)
        {
            if (cache != null)
                return cache;
            cache = Resources.Load<Shader>(resourceName);
            if (cache == null)
                cache = Shader.Find(shaderName);
            if (cache == null)
                Debug.LogError($"Demo1 shader is missing from the player build: {shaderName}");
            else if (!cache.isSupported)
                Debug.LogError($"Demo1 shader is unsupported on this graphics device: {shaderName}");
            return cache;
        }
    }
}
