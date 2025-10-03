using System;
using System.Collections.Generic;
using UnityEngine;
using Color = UnityEngine.Color;

namespace SW.Core
{
    public class CameraProjection : MonoBehaviour
    {
        LineRenderer lineRenderer;
        // 默认透视相机
        public Vector2 aspectRatio = new Vector2(16, 9);
        public float nearLimitedMag = 0.3f;
        public float farLimitedMag = 150f;
        private new Camera camera;
        private Camera.FieldOfViewAxis fovAxis = Camera.FieldOfViewAxis.Vertical;
        // 相机向前推n后的最高视线: 长度:n/cos(FOV/2), 然后绕x轴旋转FOV/2度
        private Vector3 viewUpVector;
        private Vector3 viewDownVector;
        // 从相机Forward到相机空间最高视线视点的距离:长边 * cos(90-FOV/2)
        private float viewHeight;
        // 视线半宽:相机右方向 * (最高点 / aspectRatio.height * aspectRatio.width)
        private Vector3 viewM2R;
        private Vector3 lu, ru, ld, rd;
        // 向后延长
        private Vector3 lb, rb;
        public Vector3 LeftForward => lu;
        public Vector3 RightForward => ru;
        public Vector3 LeftBack => ld;
        public Vector3 RightBack => rd;
        [Obsolete("对应的Forward不正确,如果需要使用,将给lb赋值的语句改为给ld赋值")]
        public Vector3 LeftBackExpand => lb;
        [Obsolete("对应的Forward不正确,如果需要使用,将给rb赋值的语句改为给rd赋值")]
        public Vector3 RightBackExpand => rb;

        private Vector3 CameraRotation => camera.transform.rotation.eulerAngles;
        private float ViewAngle => camera.fieldOfView;
        private float HalfViewAngle => ViewAngle / 2;
        private float UpToVerAngle => 90 - CameraRotation.x + HalfViewAngle;    // 下(CameraRotation.x)再上抬(HalfViewAngle)
        private float DownToVerAngle => 90 - CameraRotation.x - HalfViewAngle;  // 下(CameraRotation.x)再下抬(HalfViewAngle)

        private void Awake()
        {
            lineRenderer = gameObject.AddComponent<LineRenderer>();
            lineRenderer.positionCount = 4;
            lineRenderer.startWidth = lineRenderer.endWidth = 1f;
            lineRenderer.loop = true;
            //slineRenderer.SetColors(Color.blue,  Color.blue);
            camera = GetComponent<Camera>();
        }
        public void DrowArea() => DrowArea(CameraRotation, UpToVerAngle, DownToVerAngle, camera.transform.right);
        public Vector3[] ProjectSegmentation(float span)
        {
            Queue<Vector3> points = new Queue<Vector3>();
            int veritySeg = Mathf.CeilToInt((LeftForward - LeftBack).magnitude / span) + 1;
            Vector3 nowLeft;
            Vector3 nowRight;
            Vector3 targetPos;
            int horizontalSeg;
            Debug.DrawRay(LeftBack, (LeftForward - LeftBack).magnitude * (LeftForward - LeftBack).normalized);
            Debug.DrawRay(RightBack, (LeftForward - LeftBack).magnitude * (RightForward - RightBack).normalized);
            for (int back = 0; back < veritySeg; back++)
            {
                nowLeft = LeftBack + back * span * (LeftForward - LeftBack).normalized;
                nowRight = RightBack + back * span * (RightForward - RightBack).normalized;
                horizontalSeg = Mathf.CeilToInt((nowLeft - nowRight).magnitude / span) + 3; // +3:补偿边界 左1右2
                for (int right = 0; right < horizontalSeg; right++)
                {
                    targetPos = nowLeft +
                        (Quaternion.Euler(0, Camera.main.transform.rotation.eulerAngles.y, 0) *
                        Vector3.right).normalized *
                        span *
                        (right - 1); // +1: 将补偿边界给予左边界
                    points.Enqueue(targetPos);
                    Debug.DrawRay(targetPos, Vector3.up * 100, UnityEngine.Color.blue);
                }
            }
            return points.ToArray();
        }
        private void DrowArea(Vector3 cameraRotation, float upToVerAngle, float downToVerAngle, Vector3 cameraRight)
        {
            cameraRotation.x = cameraRotation.x > 180 ? cameraRotation.x - 360 : cameraRotation.x;
            switch (fovAxis)
            {
                default:
                case Camera.FieldOfViewAxis.Vertical:
                    // 这个区间基本写好了,唯一的问题的最远距离是手动设置的
                    if (cameraRotation.x > HalfViewAngle)   // 上视角与地面有交点
                    {
                        viewUpVector = Vector3.forward * transform.position.y / Mathf.Cos(upToVerAngle * Mathf.Deg2Rad);
                        viewUpVector = Quaternion.Euler(-HalfViewAngle + cameraRotation.x, cameraRotation.y, 0) * viewUpVector;
                        viewHeight = viewUpVector.magnitude * Mathf.Cos((90 - HalfViewAngle) * Mathf.Deg2Rad);
                        viewM2R = cameraRight * (viewHeight / aspectRatio.y * aspectRatio.x);
                        lu = viewUpVector - viewM2R + transform.position;
                        ru = viewUpVector + viewM2R + transform.position;
                        // 不能再使用viewUpVector在x轴旋转求down,期望的旋转并不发生在世界坐标系x轴
                        viewDownVector = Vector3.forward * transform.position.y / Mathf.Cos(downToVerAngle * Mathf.Deg2Rad);
                        viewDownVector = Quaternion.Euler(HalfViewAngle + cameraRotation.x, cameraRotation.y, 0) * viewDownVector;
                        // 因为前推的距离不同 高和到中心到水平边界需要重算
                        viewHeight = viewDownVector.magnitude * Mathf.Cos((90 - HalfViewAngle) * Mathf.Deg2Rad);
                        viewM2R = cameraRight * (viewHeight / aspectRatio.y * aspectRatio.x);
                        ld = viewDownVector - viewM2R + transform.position;
                        rd = viewDownVector + viewM2R + transform.position;
                        // 下视线向后延长
                        lb = ld + (ld - lu).normalized * (ld - transform.position).magnitude;
                        rb = rd + (rd - ru).normalized * (rd - transform.position).magnitude;
                        if ((lu - ld).magnitude > farLimitedMag)
                            lu = ld + (lu - ld).normalized * farLimitedMag;
                        if ((ru - rd).magnitude > farLimitedMag)
                            ru = rd + (ru - rd).normalized * farLimitedMag;
                    }
                    else if (cameraRotation.x > -HalfViewAngle && cameraRotation.x <= HalfViewAngle)
                    { // 上视角与地面无交点 下视角与地面有交点
                        viewUpVector = Vector3.forward * camera.farClipPlane / Mathf.Cos(HalfViewAngle * Mathf.Deg2Rad);
                        viewUpVector = Quaternion.Euler(-HalfViewAngle + cameraRotation.x, cameraRotation.y, 0) * viewUpVector;
                        viewHeight = viewUpVector.magnitude * Mathf.Cos((90 - HalfViewAngle) * Mathf.Deg2Rad);
                        viewM2R = cameraRight * (viewHeight / aspectRatio.y * aspectRatio.x);
                        float downDirLength = ((viewUpVector - new Vector3(viewUpVector.x, 0, viewUpVector.z)).magnitude + transform.position.y) / Mathf.Cos((180 - camera.fieldOfView - upToVerAngle) * Mathf.Deg2Rad);
                        lu = viewUpVector - viewM2R + transform.position + -camera.transform.up * downDirLength;
                        ru = viewUpVector + viewM2R + transform.position + -camera.transform.up * downDirLength;
                        viewDownVector = Vector3.forward * transform.position.y / Mathf.Cos(downToVerAngle * Mathf.Deg2Rad);
                        viewDownVector = Quaternion.Euler(HalfViewAngle + cameraRotation.x, cameraRotation.y, 0) * viewDownVector;
                        // 因为前推的距离不同 高和到中心到水平边界需要重算
                        viewHeight = viewDownVector.magnitude * Mathf.Cos((90 - HalfViewAngle) * Mathf.Deg2Rad);
                        viewM2R = cameraRight * (viewHeight / aspectRatio.y * aspectRatio.x);
                        ld = viewDownVector - viewM2R + transform.position;
                        rd = viewDownVector + viewM2R + transform.position;
                        // 下视线向后延长
                        lb = ld + (ld - lu).normalized * (ld - transform.position).magnitude;
                        rb = rd + (rd - ru).normalized * (rd - transform.position).magnitude;
                        if ((camera.transform.position - lu).magnitude > (camera.transform.position - ld).magnitude)
                        {
                            lu = ld + (lu - ld).normalized * farLimitedMag;
                            ru = rd + (ru - rd).normalized * farLimitedMag;
                        }
                        else // x旋转小于0°时,可能在某个角度达成:触地下视线会在上视线之前
                        {
                            lu = ld + (ld - lu).normalized * farLimitedMag;
                            ru = rd + (rd - ru).normalized * farLimitedMag;
                        }
                    }
                    lineRenderer.SetPosition(0, lu);
                    lineRenderer.SetPosition(1, ru);
                    lineRenderer.SetPosition(2, rd);
                    lineRenderer.SetPosition(3, ld);
                    break;
                case Camera.FieldOfViewAxis.Horizontal:
                    break;
            }
        }
    }
}