using UnityEngine;

namespace SWRTS.Demo1
{
    public sealed class Demo1UnitView : MonoBehaviour
    {
        private Renderer _renderer;
        private LineRenderer _selectionCircle;
        private LineRenderer _visionCircle;
        private LineRenderer _visionSector;
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
                : EnemyRoleColor(model.Role);
            _material = Demo1Drawing.CreateMaterial(_baseColor);
            if (_material != null)
                _renderer.sharedMaterial = _material;

            _selectionCircle = Demo1Drawing.CreateCircle(transform, "Selection", new Color(0.2f, 1f, 0.95f, 0.95f), 3f, 48);
            _visionCircle = Demo1Drawing.CreateCircle(transform, "Night Vision", new Color(0.42f, 0.55f, 1f, 0.48f), 2f, 72);
            _visionSector = Demo1Drawing.CreateSector(transform, "Ordinary Vision", new Color(0.25f, 0.78f, 1f, 0.5f), 2f, 40);
            _engagementCircle = Demo1Drawing.CreateCircle(transform, "Engagement", new Color(1f, 0.72f, 0.18f, 0.55f), 2f, 64);
            _routeLine = Demo1Drawing.CreateLine(transform, "Route", new Color(0.2f, 1f, 0.9f, 0.75f), 3f);
            SetSelected(false, model);
        }

        public void Sync(DemoUnitModel model, bool selected, bool visible)
        {
            Vector3 displayPosition = model.Team == DemoTeam.Enemy ? model.PlayerVisiblePosition : model.Position;
            transform.position = new Vector3(displayPosition.x, model.IsFixed ? 1.1f : 0.75f, displayPosition.z);
            _renderer.enabled = visible && model.IsAlive;
            foreach (Transform child in transform)
                child.gameObject.SetActive(visible && model.IsAlive);

            if (!visible || !model.IsAlive)
                return;

            Color activityColor = model.Activity == DemoUnitActivity.Retreating
                ? new Color(1f, 0.65f, 0.1f)
                : model.Activity == DemoUnitActivity.Protected ? new Color(0.35f, 0.9f, 1f) : _baseColor;
            if (model.Team == DemoTeam.Enemy && !model.IsCurrentlyObservedByPlayer && !model.HasPersistentPlayerIntel)
                activityColor = model.PlayerIntelLevel == DemoIntelLevel.Contact
                    ? new Color(0.58f, 0.62f, 0.66f)
                    : Color.Lerp(_baseColor, new Color(0.42f, 0.48f, 0.52f), 0.6f);
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
            _visionCircle.enabled = selected && model.Team == DemoTeam.Player &&
                                    model.Stats.WitchVisionType == DemoWitchVisionType.Night;
            _visionSector.enabled = selected && model.Team == DemoTeam.Player &&
                                    model.Stats.WitchVisionType == DemoWitchVisionType.Ordinary;
            _engagementCircle.enabled = selected;
            if (!selected)
                return;

            float bodyRadius = model.IsFixed ? 2.2f : 1.15f;
            Demo1Drawing.SetCircle(_selectionCircle, model.Position, bodyRadius, 0.09f);
            if (_visionCircle.enabled)
                Demo1Drawing.SetCircle(_visionCircle, model.Position, model.Stats.VisionRadius, 0.055f);
            if (_visionSector.enabled)
                Demo1Drawing.SetSector(_visionSector, model.Position, model.Facing, model.Stats.VisionRadius, model.Stats.VisionAngle, 0.055f);
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

        private static Color EnemyRoleColor(DemoUnitRole role)
        {
            switch (role)
            {
                case DemoUnitRole.Scout:
                    return new Color(1f, 0.5f, 0.12f);
                case DemoUnitRole.Guard:
                    return new Color(0.95f, 0.16f, 0.2f);
                case DemoUnitRole.Fortress:
                    return new Color(0.7f, 0.08f, 0.12f);
                default:
                    return new Color(0.92f, 0.2f, 0.16f);
            }
        }

        private void OnDestroy()
        {
            if (_material != null)
                Destroy(_material);
        }
    }
}
