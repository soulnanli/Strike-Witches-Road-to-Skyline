using UnityEngine;

namespace SWRTS.Demo1
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(LineRenderer))]
    public sealed class Demo1ScreenSpaceLineWidth : MonoBehaviour
    {
        [SerializeField, Min(0.5f)] private float _pixelWidth = 2f;

        private LineRenderer _line;
        private Camera _camera;

        public float PixelWidth => _pixelWidth;

        public void Initialize(float pixelWidth)
        {
            _pixelWidth = Mathf.Max(0.5f, pixelWidth);
            _line = GetComponent<LineRenderer>();
            Refresh();
        }

        public void Refresh(Camera camera = null)
        {
            if (camera != null)
                _camera = camera;
            if (_line == null)
                _line = GetComponent<LineRenderer>();

            Camera activeCamera = _camera != null && _camera.isActiveAndEnabled ? _camera : Camera.main;
            if (_line == null || activeCamera == null || activeCamera.pixelHeight <= 0)
                return;

            _camera = activeCamera;
            float worldWidth = CalculateWorldWidth(activeCamera, _pixelWidth, transform.position);
            _line.startWidth = worldWidth;
            _line.endWidth = worldWidth;
        }

        public static float CalculateWorldWidth(Camera camera, float pixelWidth, Vector3 worldPosition)
        {
            if (camera == null || camera.pixelHeight <= 0)
                return 0f;

            float visibleWorldHeight;
            if (camera.orthographic)
            {
                visibleWorldHeight = camera.orthographicSize * 2f;
            }
            else
            {
                float depth = Mathf.Max(0.01f,
                    Mathf.Abs(Vector3.Dot(worldPosition - camera.transform.position, camera.transform.forward)));
                visibleWorldHeight = 2f * depth * Mathf.Tan(camera.fieldOfView * 0.5f * Mathf.Deg2Rad);
            }

            return Mathf.Max(0.5f, pixelWidth) * visibleWorldHeight / camera.pixelHeight;
        }

        private void LateUpdate()
        {
            Refresh();
        }
    }
}
