using System;
using UnityEngine;
using UnityEngine.UI;
using XUGL;

public class LineCtr : MonoBehaviour
{
    [SerializeField] private Transform airplane;
    [SerializeField]
    private int mainTexProperty;
    // 主摄像机
    private Camera mainCam;
    // 目标坐标
    private Vector3 targetPos;
    // 飞机飞行速度
    [SerializeField]
    private float flySpeed = 0.01f;
    // 是否到达目标坐标
    private bool reachTargetPos = false;
    private LineRenderer lineRenderer;
    void Start()
    {
        // 缓存属性id，防止下面设置属性的时候重复计算名字的哈希
        mainTexProperty = Shader.PropertyToID("_MainTex");
        mainCam=Camera.main;
        lineRenderer = GetComponent<LineRenderer>();
        targetPos = airplane.position;
    }
    
    private void Update()
    {
        lineRenderer.SetPosition(0, airplane.position);
        if (Input.GetMouseButtonDown(0))
        {
            
            var screenPos = Input.mousePosition;
            // 屏幕坐标转世界坐标，注意z轴是距离摄像机的距离
            targetPos = mainCam.ScreenToWorldPoint(new Vector3(screenPos.x, screenPos.y, 10));
            // 这里用up是因为飞机的朝向的方向是y轴的方向，如果你的飞机的朝向是z轴的，则用forward
            airplane.up = targetPos - airplane.position;
            lineRenderer.SetPosition(1, targetPos);
            reachTargetPos = false;
            lineRenderer.enabled = true;
        }
        if (!reachTargetPos)
        {
            // 飞机飞向目标的
            airplane.position += airplane.up * flySpeed;
            // 检测是否到达目标坐标
            if (Vector3.Dot(airplane.up, targetPos - airplane.position) < 0)
            {
                airplane.position = targetPos;
                reachTargetPos = true;
            }
        }
    }
}
