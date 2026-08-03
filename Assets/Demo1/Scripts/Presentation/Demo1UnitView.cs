using UnityEngine;

namespace SWRTS.Demo1
{
    public sealed class Demo1UnitView : MonoBehaviour
    {
        private Renderer _renderer;
        private LineRenderer _selectionCircle;
        private LineRenderer _visionCircle;
        private LineRenderer _engagementCircle;
        private LineRenderer _routeLine;
        private Material _material;
        private Color _baseColor;

        public int UnitId { get; private set; }

        public void Initialize(DemoUnitModel model)
        {
            UnitId = model.Id;
            _renderer = GetComponent<Renderer>();
            _baseColor = model.Team == DemoTeam.Player
                ? RoleColor(model.Role)
                : model.Role == DemoUnitRole.Fortress ? new Color(0.7f, 0.08f, 0.12f) : new Color(0.92f, 0.2f, 0.16f);
            _material = Demo1Drawing.CreateMaterial(_baseColor);
            if (_material != null)
                _renderer.sharedMaterial = _material;

            _selectionCircle = Demo1Drawing.CreateCircle(transform, "Selection", new Color(0.2f, 1f, 0.95f, 0.95f), 0.13f, 48);
            _visionCircle = Demo1Drawing.CreateCircle(transform, "Vision", new Color(0.25f, 0.75f, 1f, 0.28f), 0.06f, 72);
            _engagementCircle = Demo1Drawing.CreateCircle(transform, "Engagement", new Color(1f, 0.72f, 0.18f, 0.55f), 0.08f, 64);
            _routeLine = Demo1Drawing.CreateLine(transform, "Route", new Color(0.2f, 1f, 0.9f, 0.75f), 0.1f);
            SetSelected(false, model);
        }

        public void Sync(DemoUnitModel model, bool selected)
        {
            transform.position = new Vector3(model.Position.x, model.IsFixed ? 1.1f : 0.75f, model.Position.z);
            bool visible = model.Team == DemoTeam.Player || model.IsRevealedToPlayer;
            _renderer.enabled = visible && model.IsAlive;
            foreach (Transform child in transform)
                child.gameObject.SetActive(visible && model.IsAlive);

            if (!visible || !model.IsAlive)
                return;

            Color activityColor = model.Activity == DemoUnitActivity.Retreating
                ? new Color(1f, 0.65f, 0.1f)
                : model.Activity == DemoUnitActivity.Protected ? new Color(0.35f, 0.9f, 1f) : _baseColor;
            if (_material != null)
                _material.color = Color.Lerp(Color.black, activityColor, 0.55f + model.HealthRatio * 0.45f);
            SetSelected(selected, model);

            _routeLine.enabled = selected && model.HasDestination;
            if (_routeLine.enabled)
            {
                _routeLine.SetPosition(0, model.Position + Vector3.up * 0.12f);
                _routeLine.SetPosition(1, model.Destination + Vector3.up * 0.12f);
            }
        }

        private void SetSelected(bool selected, DemoUnitModel model)
        {
            _selectionCircle.enabled = selected;
            _visionCircle.enabled = selected && model.Team == DemoTeam.Player;
            _engagementCircle.enabled = selected;
            if (!selected)
                return;

            float bodyRadius = model.IsFixed ? 2.2f : 1.15f;
            Demo1Drawing.SetCircle(_selectionCircle, model.Position, bodyRadius, 0.09f);
            Demo1Drawing.SetCircle(_visionCircle, model.Position, model.Stats.VisionRadius, 0.055f);
            Demo1Drawing.SetCircle(_engagementCircle, model.Position, model.Stats.EngagementRadius, 0.065f);
        }

        private static Color RoleColor(DemoUnitRole role)
        {
            switch (role)
            {
                case DemoUnitRole.Support:
                    return new Color(0.2f, 0.9f, 0.55f);
                case DemoUnitRole.Artillery:
                    return new Color(0.75f, 0.42f, 1f);
                default:
                    return new Color(0.15f, 0.58f, 1f);
            }
        }

        private void OnDestroy()
        {
            if (_material != null)
                Destroy(_material);
        }
    }
}
