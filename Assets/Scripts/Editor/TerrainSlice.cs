using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

public class MapSplitterEditor : EditorWindow
{
    private int rows = 4;       // 行数
    private int columns = 4;    // 列数

    [MenuItem("Tool/Map Splitter")]
    public static void ShowWindow()
    {
        GetWindow<MapSplitterEditor>("Map Splitter");
    }

    private void OnGUI()
    {
        EditorGUILayout.LabelField("行数列数相同，且必须为2的n次幂");
        rows = EditorGUILayout.IntField("行数", rows);
        columns = rows;

        if (GUILayout.Button("切割地图"))
        {
            SplitMaps();
        }
    }

    private void SplitMaps()
    {
        if (Selection.activeGameObject == null)
        {
            Debug.LogError("请选择一个GameObject进行切割。");
            return;
        }

        Terrain selectedTerrain = Selection.activeGameObject.GetComponent<Terrain>();
        Selection.activeGameObject.SetActive(false);
        if (selectedTerrain == null)
        {
            Debug.LogError("选择的GameObject没有Terrain组件。");
            return;
        }

        TerrainData terrainData = selectedTerrain.terrainData;

        // 计算贴图和高度图分辨率
        int adaptedAlphamapResolution = terrainData.baseMapResolution / rows;
        int adaptedBaseMapResolution = terrainData.alphamapResolution / rows;
        int heightmapResolution = (terrainData.heightmapResolution - 1) / rows;
        SplatPrototype[] splatProtos = terrainData.splatPrototypes;

        Vector3 originalSize = terrainData.size;
        float tileWidth = originalSize.x / columns;
        float tileLength = originalSize.z / rows;

        GameObject parent = new GameObject(Selection.activeGameObject.name + "Slice");
        parent.transform.SetParent(Selection.activeGameObject.transform.parent);
        parent.transform.position = Selection.activeGameObject.transform.position;

        // 切割循环
        for (int row = 0; row < rows; row++)
        {
            for (int col = 0; col < columns; col++)
            {
                // 新建子地形对象
                GameObject tileObject = Terrain.CreateTerrainGameObject(null);
                tileObject.name = "Tile_" + row + "_" + col;
                tileObject.transform.SetParent(parent.transform);

                // 高度图切割（注意 row -> y, col -> x）
                int xBase = (terrainData.heightmapResolution - 1) / columns;
                int yBase = (terrainData.heightmapResolution - 1) / rows;
                float[,] height = terrainData.GetHeights(col * xBase, row * yBase, xBase + 1, yBase + 1);

                // 生成 TerrainData
                Terrain tileTerrain = tileObject.GetComponent<Terrain>();
                tileTerrain.terrainData = CreateTerrainData(height, adaptedAlphamapResolution, adaptedBaseMapResolution, heightmapResolution, originalSize, rows, columns);
                tileTerrain.terrainData.name = tileTerrain.name + "_terrainData";

                // 设置纹理原型
                SplatPrototype[] newSplats = new SplatPrototype[splatProtos.Length];
                var newData = tileTerrain.terrainData;
                for (int i = 0; i < splatProtos.Length; ++i)
                {
                    newSplats[i] = new SplatPrototype
                    {
                        texture = splatProtos[i].texture,
                        tileSize = splatProtos[i].tileSize
                    };
                    float offsetX = (newData.size.x * col) % splatProtos[i].tileSize.x + splatProtos[i].tileOffset.x;
                    float offsetY = (newData.size.z * row) % splatProtos[i].tileSize.y + splatProtos[i].tileOffset.y;
                    newSplats[i].tileOffset = new Vector2(offsetX, offsetY);
                }
                newData.splatPrototypes = newSplats;

                // 尺寸与位置
                tileTerrain.terrainData.size = new Vector3(tileWidth, originalSize.y, tileLength);
                tileObject.transform.localPosition = new Vector3(col * tileWidth, 0, row * tileLength);

                // 碰撞体绑定
                TerrainCollider terrainCollider = tileObject.GetComponent<TerrainCollider>();
                if (terrainCollider != null)
                {
                    terrainCollider.terrainData = tileTerrain.terrainData;
                }

                // 树木 & 草地
                CopyVegetationData(terrainData, tileTerrain.terrainData, row, col, rows, columns);
                CopyTerrainTextureData(selectedTerrain, tileTerrain, row, col, rows, columns);
            }
        }

        Debug.Log("地图切割成功。");
    }

    private TerrainData CreateTerrainData(float[,] heightmap, int alphamapResolution, int baseMapResolution, int heightmapResolution, Vector3 originalSize, int rows, int columns)
    {
        TerrainData terrainData = new TerrainData
        {
            heightmapResolution = heightmapResolution,
            alphamapResolution = alphamapResolution,
            baseMapResolution = baseMapResolution,
            size = new Vector3(originalSize.x / columns, originalSize.y, originalSize.z / rows)
        };
        terrainData.SetHeights(0, 0, heightmap);
        return terrainData;
    }

    private void CopyVegetationData(TerrainData sourceTerrainData, TerrainData targetTerrainData, int row, int col, int rows, int columns)
    {
        targetTerrainData.treePrototypes = sourceTerrainData.treePrototypes;
        List<TreeInstance> adjustedTreeInstances = new List<TreeInstance>();

        float tileWidth = sourceTerrainData.size.x / columns;
        float tileLength = sourceTerrainData.size.z / rows;

        float startX = col * tileWidth;
        float endX = (col + 1) * tileWidth;
        float startZ = row * tileLength;
        float endZ = (row + 1) * tileLength;

        foreach (TreeInstance sourceTreeInstance in sourceTerrainData.treeInstances)
        {
            float worldX = sourceTreeInstance.position.x * sourceTerrainData.size.x;
            float worldZ = sourceTreeInstance.position.z * sourceTerrainData.size.z;

            if (worldX >= startX && worldX < endX && worldZ >= startZ && worldZ < endZ)
            {
                TreeInstance adjustedTreeInstance = sourceTreeInstance;
                adjustedTreeInstance.position = new Vector3(
                    (worldX - startX) / targetTerrainData.size.x,
                    sourceTreeInstance.position.y,
                    (worldZ - startZ) / targetTerrainData.size.z
                );
                adjustedTreeInstances.Add(adjustedTreeInstance);
            }
        }

        targetTerrainData.treeInstances = adjustedTreeInstances.ToArray();
    }

    private void CopyTerrainTextureData(Terrain sourceTerrain, Terrain targetTerrain, int row, int col, int rows, int columns)
    {
        TerrainData sourceTerrainData = sourceTerrain.terrainData;
        TerrainData targetTerrainData = targetTerrain.terrainData;

        // 注意这里 row -> Y(第一维), col -> X(第二维)
        int startAlphamapY = Mathf.FloorToInt((float)row / rows * sourceTerrainData.alphamapResolution);
        int endAlphamapY = Mathf.FloorToInt((float)(row + 1) / rows * sourceTerrainData.alphamapResolution);
        int startAlphamapX = Mathf.FloorToInt((float)col / columns * sourceTerrainData.alphamapResolution);
        int endAlphamapX = Mathf.FloorToInt((float)(col + 1) / columns * sourceTerrainData.alphamapResolution);

        float[,,] sourceAlphamaps = sourceTerrainData.GetAlphamaps(0, 0, sourceTerrainData.alphamapResolution, sourceTerrainData.alphamapResolution);
        float[,,] targetAlphamaps = new float[endAlphamapY - startAlphamapY, endAlphamapX - startAlphamapX, sourceTerrainData.alphamapLayers];

        for (int layer = 0; layer < sourceTerrainData.alphamapLayers; layer++)
        {
            for (int y = startAlphamapY, ty = 0; y < endAlphamapY; y++, ty++)
            {
                for (int x = startAlphamapX, tx = 0; x < endAlphamapX; x++, tx++)
                {
                    targetAlphamaps[ty, tx, layer] = sourceAlphamaps[y, x, layer];
                }
            }
        }

        targetTerrainData.SetAlphamaps(0, 0, targetAlphamaps);
        targetTerrain.Flush();
    }
}
