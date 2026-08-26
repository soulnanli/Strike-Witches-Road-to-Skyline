using UnityEngine;

public class SimpleCameraController : MonoBehaviour
{
    [Header("移动参数")]
    public float moveSpeed = 5f;    // 移动速度
    public float rotateSpeed = 2f;  // 旋转速度

    [Header("旋转限制")]
    public float minYAngle = -80f;  // 最小俯角
    public float maxYAngle = 80f;   // 最大仰角

    private float mouseX, mouseY;

    void Update()
    {
        // 获取鼠标输入
        mouseX += Input.GetAxis("Mouse X") * rotateSpeed;
        mouseY -= Input.GetAxis("Mouse Y") * rotateSpeed;
        mouseY = Mathf.Clamp(mouseY, minYAngle, maxYAngle);  // 限制垂直角度

        // 应用旋转
        transform.rotation = Quaternion.Euler(mouseY, mouseX, 0);
    }

    void LateUpdate()
    {
        // 获取键盘输入
        Vector3 moveDir = new Vector3(
            Input.GetAxis("Horizontal"),
            0,
            Input.GetAxis("Vertical")
        ).normalized;

        // 移动摄像机（脱离玩家坐标系）
        transform.Translate(moveDir * moveSpeed * Time.deltaTime, Space.Self);
    }
}
