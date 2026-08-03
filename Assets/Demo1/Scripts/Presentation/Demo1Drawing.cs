using UnityEngine;

namespace SWRTS.Demo1
{
    public static class Demo1Drawing
    {
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
            Shader shader = Shader.Find("Universal Render Pipeline/Lit");
            if (shader == null)
                shader = Shader.Find("Universal Render Pipeline/Simple Lit");
            if (shader == null)
                shader = Shader.Find("Universal Render Pipeline/Unlit");
            if (shader == null)
                shader = Shader.Find("Standard");
            if (shader == null)
                return null;
            Material material = new Material(shader);
            material.color = color;
            return material;
        }

        private static Material CreateLineMaterial(Color color)
        {
            Shader shader = Shader.Find("Sprites/Default");
            if (shader == null)
                shader = Shader.Find("Universal Render Pipeline/Unlit");
            if (shader == null)
                shader = Shader.Find("UI/Default");
            if (shader == null)
                return null;
            Material material = new Material(shader);
            material.color = color;
            return material;
        }
    }
}
