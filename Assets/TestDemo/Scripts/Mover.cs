using UnityEngine;

public class Mover : MonoBehaviour
{
     [Header("Settings")]
        public GameObject uiElement;      // 需要移动的UI元素
        public LineRenderer lineRenderer;   // 用于绘制的LineRenderer
        public float moveSpeed = 5f;       // 移动速度
        public bool showLine = true;       // 是否显示线条
    
        private Vector3 currentPos;       // UI元素当前世界坐标
        private Vector3 targetPos;       // 目标位置
        private bool isMoving = false;     // 移动状态
    
        void Start()
        {
            // 初始化LineRenderer
            lineRenderer.startWidth = 2;
            lineRenderer.endWidth = 2;
            lineRenderer.material = new Material(Shader.Find("Sprites/Default"));
            
            // 初始位置设为UI元素的世界坐标
            currentPos = uiElement.transform.position;
        }
    
        void Update()
        {
            if (Input.GetMouseButtonDown(0)) // 检测鼠标左键点击
            {
                targetPos = Camera.main.ScreenToWorldPoint(Input.mousePosition); 
                
                // 如果点击位置在Game窗口内
                if (targetPos.z > 0) 
                {
                    isMoving = true;
                    
                    // 每次点击时重置LineRenderer的顶点
                    lineRenderer.SetVertexCount(2);
                    lineRenderer.SetPosition(0, currentPos);
                    lineRenderer.SetPosition(1, targetPos);
                }
            }
    
            if (isMoving)
            {
                // 平滑移动UI元素
                currentPos = Vector3.Lerp(currentPos, targetPos, Time.deltaTime * moveSpeed);
                uiElement.transform.position = currentPos;
    
                // 到达目标后停止
                if (Vector3.Distance(currentPos, targetPos) < 0.1f)
                {
                    isMoving = false;
                    
                    // 可选：隐藏线条
                    if (!showLine) 
                    {
                        lineRenderer.enabled = false;
                    }
                }
            }
        }
}
