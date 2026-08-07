using UnityEngine;

namespace SWRTS.Demo1
{
    public sealed class Demo1UnitView : MonoBehaviour
    {
        private Renderer _renderer;
        private LineRenderer _selectionCircle;
        private LineRenderer _attackCircle;
        private LineRenderer _abilityCircle;
        private LineRenderer _abilitySector;
        private LineRenderer _routeLine;
        private LineRenderer _targetLine;
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
            _attackCircle = Demo1Drawing.CreateCircle(transform, "Attack Range", new Color(1f, 0.22f, 0.16f, 0.78f), 2.5f, 64);
            _abilityCircle = Demo1Drawing.CreateCircle(transform, "Ability Range", new Color(0.72f, 0.35f, 1f, 0.82f), 2.5f, 72);
            _abilitySector = Demo1Drawing.CreateSector(transform, "Ability Sector", new Color(0.72f, 0.35f, 1f, 0.82f), 2.5f, 48);
            _routeLine = Demo1Drawing.CreateLine(transform, "Route", new Color(0.2f, 1f, 0.9f, 0.75f), 3f);
            _targetLine = Demo1Drawing.CreateLine(transform, "Target Lock", new Color(1f, 0.24f, 0.16f, 0.9f), 3.5f);
            _routeLine.enabled = false;
            _targetLine.enabled = false;
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

            Color activityColor = model.Activity == DemoUnitActivity.Attacking
                ? new Color(1f, 0.65f, 0.1f)
                : model.Activity == DemoUnitActivity.Pursuing ? new Color(1f, 0.42f, 0.16f) : _baseColor;
            if (model.Team == DemoTeam.Enemy && !model.IsCurrentlyObservedByPlayer && !model.HasPersistentPlayerIntel)
                activityColor = model.PlayerIntelLevel == DemoIntelLevel.Contact
                    ? new Color(0.58f, 0.62f, 0.66f)
                    : Color.Lerp(_baseColor, new Color(0.42f, 0.48f, 0.52f), 0.6f);
            if (_material != null)
                _material.color = Color.Lerp(Color.black, activityColor, 0.55f + model.HealthRatio * 0.45f);
            SetSelected(selected, model);

            _routeLine.enabled = selected && (model.HasDestination ||
                                               model.FlightMode == DemoFlightMode.Loiter && model.LoiterWaypoints.Count > 0);
            if (_routeLine.enabled)
            {
                if (model.FlightMode == DemoFlightMode.Loiter && model.LoiterWaypoints.Count > 0)
                {
                    _routeLine.positionCount = model.LoiterWaypoints.Count + 2;
                    _routeLine.SetPosition(0, model.Position + Vector3.up * 0.12f);
                    for (int i = 0; i <= model.LoiterWaypoints.Count; i++)
                    {
                        int index = (model.LoiterWaypointIndex + i) % model.LoiterWaypoints.Count;
                        _routeLine.SetPosition(i + 1, model.LoiterWaypoints[index] + Vector3.up * 0.12f);
                    }
                }
                else
                {
                    _routeLine.positionCount = 2;
                    _routeLine.SetPosition(0, model.Position + Vector3.up * 0.12f);
                    _routeLine.SetPosition(1, model.Destination + Vector3.up * 0.12f);
                }
            }
            _targetLine.enabled = selected && model.LockedTargetId >= 0 && model.HasTargetLastKnownPosition;
            if (_targetLine.enabled)
            {
                Color lockColor = Color.Lerp(new Color(1f, 0.7f, 0.16f, 0.75f), new Color(1f, 0.16f, 0.12f, 1f), model.LockQualityRatio);
                _targetLine.startColor = lockColor;
                _targetLine.endColor = lockColor;
                _targetLine.SetPosition(0, model.Position + Vector3.up * 0.18f);
                _targetLine.SetPosition(1, model.TargetLastKnownPosition + Vector3.up * 0.18f);
            }
        }

        private void SetSelected(bool selected, DemoUnitModel model)
        {
            _selectionCircle.enabled = selected;
            _attackCircle.enabled = selected;
            _abilityCircle.enabled = selected && model.Team == DemoTeam.Player &&
                                     model.Stats.SpecialAbility != DemoSpecialAbility.None &&
                                     model.Stats.SpecialAbility != DemoSpecialAbility.MagicEyeSearch;
            _abilitySector.enabled = selected && model.Team == DemoTeam.Player &&
                                     model.Stats.SpecialAbility == DemoSpecialAbility.MagicEyeSearch;
            if (!selected)
                return;

            float bodyRadius = model.IsFixed ? 2.2f : 1.15f;
            Demo1Drawing.SetCircle(_selectionCircle, model.Position, bodyRadius, 0.09f);
            float rangePenalty = 1f - 0.25f * model.SuppressionRatio;
            float attackRange = model.IsFireControlReady
                ? Mathf.Max(model.Stats.AttackRange, model.Stats.PassiveAttackRange)
                : model.Stats.AttackRange;
            Demo1Drawing.SetCircle(_attackCircle, model.Position, attackRange * rangePenalty, 0.075f);
            if (_abilityCircle.enabled)
            {
                float radius = model.Stats.SpecialAbility == DemoSpecialAbility.LightningStrike
                    ? model.Stats.AbilityRadius
                    : model.Stats.AbilityRange;
                Demo1Drawing.SetCircle(_abilityCircle, model.Position, radius, 0.08f);
            }
            if (_abilitySector.enabled)
                Demo1Drawing.SetSector(_abilitySector, model.Position, model.Facing, model.Stats.AbilityRange,
                    model.Stats.AbilityArcAngle, 0.08f);
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
