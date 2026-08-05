using UnityEngine;

namespace SWRTS.Demo1
{
    [RequireComponent(typeof(Camera))]
    public sealed class Demo1CameraController : MonoBehaviour
    {
        public float PanSpeed = 24f;
        public float EdgeSize = 8f;
        public float MinZoom = 10f;
        public float MaxZoom = 42f;
        public Vector2 MapHalfExtents = new Vector2(45f, 30f);

        private Camera _camera;
        private bool _dragging;
        private bool _fullTheatre;
        private Vector3 _lastGroundPoint;

        private void Awake()
        {
            _camera = GetComponent<Camera>();
        }

        private void Update()
        {
            if (_fullTheatre)
            {
                MaxZoom = GetFullTheatreZoom();
                _camera.orthographicSize = Mathf.Min(_camera.orthographicSize, MaxZoom);
            }
            float dt = Time.unscaledDeltaTime;
            Vector3 movement = new Vector3(Input.GetAxisRaw("Horizontal"), 0f, Input.GetAxisRaw("Vertical"));
            Vector3 mouse = Input.mousePosition;
            if (!_dragging && mouse.x >= 0f && mouse.y >= 0f && mouse.x <= Screen.width && mouse.y <= Screen.height)
            {
                if (mouse.x < EdgeSize) movement.x -= 1f;
                if (mouse.x > Screen.width - EdgeSize) movement.x += 1f;
                if (mouse.y < EdgeSize) movement.z -= 1f;
                if (mouse.y > Screen.height - EdgeSize) movement.z += 1f;
            }

            if (movement.sqrMagnitude > 1f)
                movement.Normalize();
            transform.position += movement * PanSpeed * dt * (_camera.orthographicSize / 22f);

            if (Input.GetMouseButtonDown(2) && TryGroundPoint(Input.mousePosition, out _lastGroundPoint))
                _dragging = true;
            if (Input.GetMouseButtonUp(2))
                _dragging = false;
            if (_dragging && TryGroundPoint(Input.mousePosition, out Vector3 current))
                transform.position += _lastGroundPoint - current;

            float wheel = Input.mouseScrollDelta.y;
            if (Mathf.Abs(wheel) > 0.01f)
                _camera.orthographicSize = Mathf.Clamp(_camera.orthographicSize - wheel * 2.2f, MinZoom, MaxZoom);

            Vector3 position = transform.position;
            float halfVisibleHeight = _camera.orthographicSize;
            float halfVisibleWidth = halfVisibleHeight * Mathf.Max(0.01f, _camera.aspect);
            float horizontalTravel = Mathf.Max(0f, MapHalfExtents.x - halfVisibleWidth);
            float verticalTravel = Mathf.Max(0f, MapHalfExtents.y - halfVisibleHeight);
            position.x = Mathf.Clamp(position.x, -horizontalTravel, horizontalTravel);
            position.z = Mathf.Clamp(position.z, -verticalTravel, verticalTravel);
            transform.position = position;
        }

        public void ConfigureFullTheatre()
        {
            _fullTheatre = true;
            MaxZoom = GetFullTheatreZoom();
            _camera.orthographicSize = MaxZoom;
            Vector3 position = transform.position;
            position.x = 0f;
            position.z = 0f;
            transform.position = position;
        }

        private float GetFullTheatreZoom()
        {
            return Mathf.Max(MapHalfExtents.y, MapHalfExtents.x / Mathf.Max(0.01f, _camera.aspect));
        }

        public void Focus(Vector3 worldPosition)
        {
            Vector3 position = transform.position;
            position.x = worldPosition.x;
            position.z = worldPosition.z;
            transform.position = position;
        }

        private bool TryGroundPoint(Vector3 screenPoint, out Vector3 worldPoint)
        {
            Ray ray = _camera.ScreenPointToRay(screenPoint);
            Plane plane = new Plane(Vector3.up, Vector3.zero);
            if (plane.Raycast(ray, out float distance))
            {
                worldPoint = ray.GetPoint(distance);
                return true;
            }
            worldPoint = Vector3.zero;
            return false;
        }
    }
}
