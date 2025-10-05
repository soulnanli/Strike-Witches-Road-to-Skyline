using UnityEngine;
using UnityEditor;
using System.IO;

public class MinMaxHeightMapGeneratorWindow : EditorWindow
{
    private Texture2D heightMap;
    private string outputFileName = "MinMaxHeightMap.exr";

    [MenuItem("Tools/Terrain/Generate MinMax HeightMap")]
    public static void ShowWindow()
    {
        GetWindow<MinMaxHeightMapGeneratorWindow>("MinMaxHeightMap Generator");
    }

    private void OnGUI()
    {
        GUILayout.Label("MinMax HeightMap Generator", EditorStyles.boldLabel);
        GUILayout.Space(8);

        heightMap = (Texture2D)EditorGUILayout.ObjectField("Height Map (Texture2D)", heightMap, typeof(Texture2D), false);
        outputFileName = EditorGUILayout.TextField("Output File Name", outputFileName);

        GUILayout.Space(12);
        if (GUILayout.Button("Generate MinMaxHeightMap", GUILayout.Height(30)))
        {
            if (heightMap == null)
            {
                EditorUtility.DisplayDialog("Error", "Please assign a HeightMap texture first!", "OK");
                return;
            }

            GenerateAndSave();
        }
    }

    private void GenerateAndSave()
    {
        string path = EditorUtility.SaveFilePanel("Save MinMaxHeightMap", Application.dataPath, outputFileName, "exr");
        if (string.IsNullOrEmpty(path))
            return;

        // 确保源纹理是可读的
        string heightMapPath = AssetDatabase.GetAssetPath(heightMap);
        TextureImporter importer = AssetImporter.GetAtPath(heightMapPath) as TextureImporter;
        bool wasReadable = false;

        if (importer != null)
        {
            wasReadable = importer.isReadable;
            if (!wasReadable)
            {
                importer.isReadable = true;
                importer.SaveAndReimport();
            }
        }

        // 执行生成
        Texture2D minMaxTex = GenerateMinMaxHeightMap(heightMap);

        // 保存为EXR
        byte[] bytes = minMaxTex.EncodeToEXR(Texture2D.EXRFlags.OutputAsFloat);
        File.WriteAllBytes(path, bytes);
        AssetDatabase.Refresh();

        // 还原可读设置
        if (importer != null && !wasReadable)
        {
            importer.isReadable = false;
            importer.SaveAndReimport();
        }

        EditorUtility.DisplayDialog("Success", "MinMaxHeightMap generated successfully!\nSaved to:\n" + path, "OK");
    }

    private Texture2D GenerateMinMaxHeightMap(Texture2D heightMap)
    {
        int srcW = heightMap.width;
        int srcH = heightMap.height;

        // 输出比输入少1像素
        int w = srcW - 1;
        int h = srcH - 1;

        Texture2D minMaxTex = new Texture2D(w, h, TextureFormat.RGFloat, true, true);
        minMaxTex.wrapMode = TextureWrapMode.Clamp;

        // Step 1: 第一层（从HeightMap计算）
        for (int y = 0; y < h; y++)
        {
            for (int x = 0; x < w; x++)
            {
                float h00 = heightMap.GetPixel(x, y).r;
                float h10 = heightMap.GetPixel(x + 1, y).r;
                float h01 = heightMap.GetPixel(x, y + 1).r;
                float h11 = heightMap.GetPixel(x + 1, y + 1).r;

                float minH = Mathf.Min(h00, Mathf.Min(h10, Mathf.Min(h01, h11)));
                float maxH = Mathf.Max(h00, Mathf.Max(h10, Mathf.Max(h01, h11)));

                minMaxTex.SetPixel(x, y, new Color(minH, maxH, 0, 0));
            }
        }

        minMaxTex.Apply(updateMipmaps: false);

        // Step 2: 生成自定义 Mipmaps
        GenerateCustomMips(minMaxTex);

        return minMaxTex;
    }

    private void GenerateCustomMips(Texture2D tex)
    {
        int mipCount = tex.mipmapCount;

        for (int mip = 1; mip < mipCount; mip++)
        {
            int prevW = Mathf.Max(1, tex.width >> (mip - 1));
            int prevH = Mathf.Max(1, tex.height >> (mip - 1));
            int curW = Mathf.Max(1, tex.width >> mip);
            int curH = Mathf.Max(1, tex.height >> mip);

            Color[] prevColors = tex.GetPixels(mip - 1);
            Color[] curColors = new Color[curW * curH];

            for (int y = 0; y < curH; y++)
            {
                for (int x = 0; x < curW; x++)
                {
                    int x0 = x * 2;
                    int y0 = y * 2;

                    int i00 = (y0 * prevW) + x0;
                    int i10 = Mathf.Min(i00 + 1, prevColors.Length - 1);
                    int i01 = Mathf.Min(i00 + prevW, prevColors.Length - 1);
                    int i11 = Mathf.Min(i01 + 1, prevColors.Length - 1);

                    float minR = Mathf.Min(prevColors[i00].r, Mathf.Min(prevColors[i10].r, Mathf.Min(prevColors[i01].r, prevColors[i11].r)));
                    float maxG = Mathf.Max(prevColors[i00].g, Mathf.Max(prevColors[i10].g, Mathf.Max(prevColors[i01].g, prevColors[i11].g)));

                    curColors[y * curW + x] = new Color(minR, maxG, 0, 0);
                }
            }

            tex.SetPixels(curColors, mip);
        }

        tex.Apply(updateMipmaps: false);
    }
}
