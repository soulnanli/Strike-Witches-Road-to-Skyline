using System;
using UnityEngine;

public class BattleMapManager : MonoBehaviour
{
    public MapElementLayer mapElementLayer;
    public Camera c;
    private void Start()
    {
        c = Camera.main;
    }

    private void Update()
    {
        foreach (var e in mapElementLayer.elements)
        {
            if (!e.isConstScale)
            {
                // 动态计算物体世界高度
                float orthoSize = c.orthographicSize;
                float screenHeight = Screen.height;

                // 计算新的世界高度
                float worldHeight = (e.targetPixelHeight * orthoSize * 2) / screenHeight;

                // 应用缩放（保持X/Z轴不变）
                e.transform.localScale = new Vector3(
                    worldHeight * e._originalHeight,
                    worldHeight * e._originalHeight,
                    e.transform.localScale.z
                );
            }
        }
    }
}
