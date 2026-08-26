
using Unity.Transforms;
using Unity.VisualScripting;
using UnityEngine;
using Unity.Profiling;


namespace SW.Core
{
    public static class Utils
    {
        static readonly ProfilerMarker ReadMeshMarker = new ProfilerMarker("heightMap2Mesh.ReadMesh");
        static readonly ProfilerMarker SampleHeightMarker = new ProfilerMarker("heightMap2Mesh.SampleHeight");
        static readonly ProfilerMarker SetVerticesMarker = new ProfilerMarker("heightMap2Mesh.SetVertices");
        static readonly ProfilerMarker RecalculateBoundsMarker = new ProfilerMarker("heightMap2Mesh.RecalculateBounds");
        static readonly ProfilerMarker RecalculateNormalsMarker = new ProfilerMarker("heightMap2Mesh.RecalculateNormals");

        public static Rect GetWorldRect(SpriteRenderer sr)
        {
            var position = sr.bounds.center - sr.bounds.size / 2;
            var size = sr.bounds.size;
            return new Rect(position, size);
        }

        public static Rect GetWorldRect(RectTransform rt)
        {
            float xmin, xmax, ymin, ymax;
            float scaleFactor = rt.GetComponentInParent<Canvas>().scaleFactor;
            xmin = rt.anchorMin.x * Screen.width + rt.offsetMin.x * scaleFactor;
            xmax = rt.anchorMax.x * Screen.width + rt.offsetMax.x * scaleFactor;
            ymin = rt.anchorMin.y * Screen.height + rt.offsetMin.y * scaleFactor;
            ymax = rt.anchorMax.y * Screen.height + rt.offsetMax.y * scaleFactor;

            Vector2 leftBottom = new Vector2(xmin, ymin);
            Vector2 rightTop = new Vector2(xmax, ymax);
            leftBottom = Camera.main.ScreenToWorldPoint(leftBottom);
            rightTop = Camera.main.ScreenToWorldPoint(rightTop);
            return new Rect(leftBottom, rightTop - leftBottom);
        }

        private static int _patchSize = 64;
        private static int _planeSize = 8;
        public static (Mesh,Vector3) heightMap2Mesh(Mesh mesh, Color[] heightMapData, int heightMapWidth, int scale, float size, Vector3 center, float mapSize,float heightScale,int col,int row,Vector3 pos)
        {
            float gray = -1;
            int heightMapHeight = heightMapData.Length / heightMapWidth;
            Vector3[] vertices;
            Bounds bounds;
            using (ReadMeshMarker.Auto())
            {
                vertices = mesh.vertices;
                bounds = mesh.bounds;
            }

            // 找到 mesh 的边界，方便做归一化
            float meshWidth = bounds.size.x;
            float meshHeight = bounds.size.z;

            float leftOffset = center.x + mapSize / 2 - scale * (_patchSize / 2) + row * _planeSize * scale;
            float downOffset = center.z + mapSize / 2 - scale * (_patchSize / 2) + col * _planeSize * scale;

            using (SampleHeightMarker.Auto())
            {
                for (int i = 0; i < vertices.Length; i++)
                {
                    // 把 mesh 顶点坐标映射到 [0,1]
                    float u = (vertices[i].x - bounds.min.x) / meshWidth;
                    float v = (vertices[i].z - bounds.min.z) / meshHeight;

                    // 用 UV 在 heightmap 上采样
                    float x = (u * _planeSize * scale );
                    float y = (v * _planeSize * scale );

                    int mx = Mathf.RoundToInt((x + leftOffset) );
                    int my = Mathf.RoundToInt((y + downOffset) );

                    mx %= heightMapWidth;
                    if (mx < 0) mx += heightMapWidth;
                    my %= heightMapHeight;
                    if (my < 0) my += heightMapHeight;

                    gray = heightMapData[my * heightMapWidth + mx].grayscale;

                    // 修改顶点高度
                    vertices[i].y = gray * heightScale;
                }
            }

            // 更新 mesh
            using (SetVerticesMarker.Auto())
            {
                mesh.vertices = vertices;
            }
            using (RecalculateBoundsMarker.Auto())
            {
                mesh.RecalculateBounds();
            }
            using (RecalculateNormalsMarker.Auto())
            {
                mesh.RecalculateNormals();
            }
            mesh.bounds.Expand(25f);
            //视锥裁减
            return (mesh, new Vector3(leftOffset,downOffset, gray));
        }
    }
    // float2 heightUV = (inVertex.xz + (_WorldSize.xz * 0.5) + 0.5) / (_WorldSize.xz + 1);
    // float height = tex2Dlod(_HeightMap,float4(heightUV,0,0)).r;
    // inVertex.y = height * _WorldSize.y;
}
