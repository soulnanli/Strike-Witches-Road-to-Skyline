using System;
using UnityEngine;
using UnityEngine.UI;


public class WatchLineCtr : MaskableGraphic
{
    [SerializeField] private Transform airplane;
    [SerializeField]
    private LineRenderer lineRenderer;
    private int mainTexProperty;
    // 主摄像机
    private Camera mainCam;

    private Vector3[] vertices;
    [Header("Sector Settings")]
    public float radius = 3f;          // 扇形半径
    public float angle = 30f;     // 起始角度（度数）
    public int vertexCount = 32;       // 顶点数量（越多越平滑）
    public float rotationSpeed = 2f;     // 旋转速度（演示用）

    private void Update()
    {

    }
}
