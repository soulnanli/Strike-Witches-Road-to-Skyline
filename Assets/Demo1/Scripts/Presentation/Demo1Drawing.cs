using UnityEngine;

namespace SWRTS.Demo1
{
    public static class Demo1Drawing
    {
        private static Shader _flatColorShader;
        private static Shader _lineColorShader;

        public static LineRenderer CreateCircle(Transform parent, string name, Color color, float width, int segments = 64)
        {
            GameObject circle = new GameObject(name);
            circle.transform.SetParent(parent, false);
            LineRenderer line = circle.AddComponent<LineRenderer>();
            line.useWorldSpace = true;
            line.loop = true;
            line.positionCount = segments;
            line.startWidth = width;
            line.endWidth = width;
            line.numCornerVertices = 2;
            Material material = CreateLineMaterial(color);
            if (material != null)
                line.material = material;
            line.startColor = color;
            line.endColor = color;
            return line;
        }

        public static LineRenderer CreateLine(Transform parent, string name, Color color, float width)
        {
            GameObject lineObject = new GameObject(name);
            lineObject.transform.SetParent(parent, false);
            LineRenderer line = lineObject.AddComponent<LineRenderer>();
            line.useWorldSpace = true;
            line.positionCount = 2;
            line.startWidth = width;
            line.endWidth = width;
            Material material = CreateLineMaterial(color);
            if (material != null)
                line.material = material;
            line.startColor = color;
            line.endColor = color;
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

        public static Material CreateMaterial(Color color)
        {
            Shader shader = LoadShader(ref _flatColorShader, "Demo1FlatColor", "SWRTS/Demo1/FlatColor");
            if (shader == null)
                return null;
            Material material = new Material(shader);
            material.SetColor("_BaseColor", color);
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
