using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace SWRTS.Demo1
{
    public enum DemoTeam
    {
        Player,
        Enemy
    }

    public enum DemoUnitRole
    {
        Witch,
        Support,
        Artillery,
        Scout,
        Guard,
        Fortress
    }

    public enum DemoWitchVisionType
    {
        None,
        Ordinary,
        Night
    }

    [Flags]
    public enum DemoUnitTrait
    {
        None = 0,
        SakamotoCoreInsight = 1 << 0,
        MiyafujiShieldAura = 1 << 1,
        LynetteSharpshooter = 1 << 2
    }

    public enum DemoIntelLevel
    {
        Unknown,
        Contact,
        Identified,
        Assessed
    }

    public enum DemoUnitActivity
    {
        Idle,
        Moving,
        Pursuing,
        Attacking,
        Destroyed
    }

    public enum DemoUnitDeploymentState
    {
        Standby,
        Active,
        Returning,
        Servicing,
        Lost
    }

    public enum DemoEnemyAiProfile
    {
        None,
        Scout,
        Combat
    }

    public enum DemoEnemyAiState
    {
        None,
        Patrol,
        Pursue,
        Investigate,
        Guard,
        ReturnHome,
        Fighting,
        Retreating
    }

    public enum DemoOutcome
    {
        Running,
        Victory,
        Defeat
    }

    public enum DemoMissionObjective
    {
        DestroyFortress,
        DestroyAllEnemies
    }

    [Serializable]
    public sealed class Demo1Balance
    {
        public float MinimumDamage = 1f;
        public float CriticalMultiplier = 1.55f;
        public float CoreMultiplier = 2.4f;
        public float ShieldMagicCostPerDamage = 0.55f;
        public float SupportEffectMultiplier = 1.5f;
        public int ArtillerySalvoEveryAttacks = 3;
        public float ArtillerySalvoDamageMultiplier = 1.45f;
        public float ScoutMarkDuration = 4f;
        public float ScoutMarkDamageMultiplier = 1.2f;
        public float SupportPulseInterval = 4f;
        public float SupportPulseShield = 6f;
        public float SupportPulseMagic = 4f;
        public float FortressBarrageHealthThreshold = 0.5f;
        public float FortressBarrageDamageMultiplier = 1.15f;
        public float FortressBarrageIntervalMultiplier = 0.65f;
        public float SakamotoCoreDiscoveryBonus = 1f;
        public float MiyafujiShieldEfficiencyBonus = 0.15f;
        public float LynetteCriticalChanceBonus = 0.18f;
        public float LynetteAttackIntervalMultiplier = 1.375f;
        public float VisionIdentificationDuration = 0.5f;
        public float VisionAssessmentDuration = 1.5f;
        public float AssessedIntelMemoryDuration = 3f;
        public float IdentifiedIntelMemoryDuration = 7f;
        public float ContactIntelMemoryDuration = 15f;
        public float EnemyAiDecisionInterval = 0.5f;
        public float EnemyAiScoutRetreatHealthRatio = 0.3f;
        public float EnemyAiGuardLeashRadius = 18f;
        public float EnemyAiArrivalRadius = 1.25f;
        public float DestinationRadius = 1.8f;
        public float RemoteStrikeDelay = 3f;
        public float RemoteStrikeRadius = 5f;
        public float RemoteStrikeDamageMultiplier = 1.8f;
        public float RemoteStrikeCooldown = 12f;
        public float RemoteStrikeRange = 42f;
        public float MapHalfWidth = 280f;
        public float MapHalfHeight = 157.5f;
        public float MapKilometersPerUnit = 1f;
        public float StrategicMovementTimeCompression = 12f;
        public float BaseArrivalRadius = 2f;
        public float BaseLaunchSpread = 1.5f;
        public float BaseTurnaroundDuration = 20f;
        public int RandomSeed = 1944;

        public float HistoricalSpeedToMapUnitsPerSecond(float kilometersPerHour)
        {
            float kilometersPerUnit = Mathf.Max(0.001f, MapKilometersPerUnit);
            float timeCompression = Mathf.Max(0.001f, StrategicMovementTimeCompression);
            return Mathf.Max(0f, kilometersPerHour) / 3600f * timeCompression / kilometersPerUnit;
        }

        public Demo1Balance Clone()
        {
            return (Demo1Balance)MemberwiseClone();
        }
    }

    [Serializable]
    public sealed class DemoUnitStats
    {
        public float MaxHealth = 120f;
        public float Attack = 24f;
        public float CriticalChance = 0.12f;
        public float Defense = 7f;
        public float MaxMagic = 80f;
        public float MaxShield = 45f;
        public float GlobalShieldBonus;
        public float CoreDiscovery = 0.18f;
        public float CoreConcealment = 0.5f;
        public float AttackInterval = 1.6f;
        public float MagicRecovery = 4f;
        public float Mobility = 1f;
        public float MoveSpeed = 6f;
        public float VisionRadius = 22f;
        public float VisionAngle = 100f;
        public DemoWitchVisionType WitchVisionType = DemoWitchVisionType.None;
        public float EngagementRadius = 8f;
        public float AttackRange = 8f;
        public float SupportRadius;
        public bool CanRemoteStrike;
        public DemoUnitTrait Traits = DemoUnitTrait.None;

        public bool HasTrait(DemoUnitTrait trait)
        {
            return (Traits & trait) != 0;
        }

        public DemoUnitStats Clone()
        {
            return (DemoUnitStats)MemberwiseClone();
        }
    }

    public readonly struct DemoCommandResult
    {
        public readonly bool Success;
        public readonly string Message;

        public DemoCommandResult(bool success, string message)
        {
            Success = success;
            Message = message;
        }

        public static DemoCommandResult Ok(string message)
        {
            return new DemoCommandResult(true, message);
        }

        public static DemoCommandResult Fail(string message)
        {
            return new DemoCommandResult(false, message);
        }
    }

    public readonly struct DemoDamageResult
    {
        public readonly float RawDamage;
        public readonly float ShieldDamage;
        public readonly float HealthDamage;
        public readonly bool Critical;
        public readonly bool CoreHit;
        public readonly bool Destroyed;

        public DemoDamageResult(float rawDamage, float shieldDamage, float healthDamage, bool critical, bool coreHit, bool destroyed)
        {
            RawDamage = rawDamage;
            ShieldDamage = shieldDamage;
            HealthDamage = healthDamage;
            Critical = critical;
            CoreHit = coreHit;
            Destroyed = destroyed;
        }
    }

    public sealed class DemoBattleEvent
    {
        public float Time { get; }
        public string Message { get; }
        public Vector3 Position { get; }
        public bool Important { get; }

        public DemoBattleEvent(float time, string message, Vector3 position, bool important)
        {
            Time = time;
            Message = message;
            Position = position;
            Important = important;
        }
    }

    public sealed class DemoUnitModel
    {
        public int Id { get; }
        public string DisplayName { get; }
        public DemoTeam Team { get; }
        public DemoUnitRole Role { get; }
        public DemoUnitStats Stats { get; }
        public Vector3 Position;
        public Vector3 Facing = Vector3.right;
        public Vector3 Destination;
        public bool HasDestination;
        public bool HasManualMoveOrder;
        public DemoUnitActivity Activity = DemoUnitActivity.Idle;
        public DemoUnitDeploymentState DeploymentState;
        public float TurnaroundRemaining;
        public float Health;
        public float Magic;
        public float Shield;
        public float AttackCooldown;
        public float RemoteStrikeCooldown;
        public bool AutoAttackEnabled = true;
        public int LockedTargetId = -1;
        public int OrderedTargetId = -1;
        public bool HasExplicitAttackOrder;
        public Vector3 TargetLastKnownPosition;
        public bool HasTargetLastKnownPosition;
        public int AttacksPerformed;
        public float RoleAbilityRemaining;
        public float MarkedUntil;
        public bool FortressBarrageAnnounced;
        public bool IsRevealedToPlayer;
        public bool IsCurrentlyObservedByPlayer;
        public bool HasPersistentPlayerIntel;
        public DemoIntelLevel PlayerIntelLevel;
        public Vector3 LastKnownPosition;
        public float LastObservedAt = float.NegativeInfinity;
        public float IdentificationProgress;
        public float AssessmentProgress;
        public DemoEnemyAiProfile EnemyAiProfile;
        public DemoEnemyAiState EnemyAiState;
        public Vector3 EnemyAiHomePosition;
        public int EnemyAiTargetId = -1;
        public Vector3 EnemyAiLastKnownPosition;
        public bool EnemyAiHasLastKnownPosition;
        public float EnemyAiDecisionRemaining;
        public int EnemyAiPatrolIndex;
        private readonly List<Vector3> _enemyAiPatrolPoints = new List<Vector3>();

        public bool IsFixed => Role == DemoUnitRole.Fortress;
        public bool IsAlive => Activity != DemoUnitActivity.Destroyed && Health > 0f;
        public bool IsOperational => Team == DemoTeam.Enemy ||
                                     DeploymentState == DemoUnitDeploymentState.Active ||
                                     DeploymentState == DemoUnitDeploymentState.Returning;
        public bool IsAtBase => Team == DemoTeam.Player &&
                                (DeploymentState == DemoUnitDeploymentState.Standby ||
                                 DeploymentState == DemoUnitDeploymentState.Servicing);
        public float HealthRatio => Stats.MaxHealth <= 0f ? 0f : Health / Stats.MaxHealth;
        public float MagicRatio => Stats.MaxMagic <= 0f ? 0f : Magic / Stats.MaxMagic;
        public float ShieldRatio => Stats.MaxShield <= 0f ? 0f : Shield / Stats.MaxShield;
        public bool HasPlayerIntel => Team == DemoTeam.Player || IsRevealedToPlayer || PlayerIntelLevel != DemoIntelLevel.Unknown;
        public bool CanBeDirectlyTargetedByPlayer => Team == DemoTeam.Player || HasPersistentPlayerIntel ||
                                                     (IsCurrentlyObservedByPlayer && PlayerIntelLevel >= DemoIntelLevel.Identified) ||
                                                     (IsRevealedToPlayer && PlayerIntelLevel == DemoIntelLevel.Unknown);
        public Vector3 PlayerVisiblePosition => Team == DemoTeam.Player || IsCurrentlyObservedByPlayer || HasPersistentPlayerIntel
            ? Position
            : LastKnownPosition;
        public IReadOnlyList<Vector3> EnemyAiPatrolPoints => _enemyAiPatrolPoints;

        public DemoUnitModel(int id, string displayName, DemoTeam team, DemoUnitRole role, DemoUnitStats stats, Vector3 position)
        {
            Id = id;
            DisplayName = displayName;
            Team = team;
            Role = role;
            Stats = stats.Clone();
            Position = position;
            Destination = position;
            LastKnownPosition = position;
            TargetLastKnownPosition = position;
            EnemyAiHomePosition = position;
            EnemyAiLastKnownPosition = position;
            Health = Stats.MaxHealth;
            Magic = Stats.MaxMagic;
            Shield = Stats.MaxShield;
            RoleAbilityRemaining = Mathf.Max(0.1f, 4f);
            IsRevealedToPlayer = team == DemoTeam.Player;
            IsCurrentlyObservedByPlayer = team == DemoTeam.Player;
            PlayerIntelLevel = team == DemoTeam.Player ? DemoIntelLevel.Assessed : DemoIntelLevel.Unknown;
            DeploymentState = DemoUnitDeploymentState.Active;
        }

        internal void SetEnemyAiPatrolPoints(IEnumerable<Vector3> patrolPoints)
        {
            _enemyAiPatrolPoints.Clear();
            _enemyAiPatrolPoints.AddRange(patrolPoints);
        }
    }

    public sealed class DemoRemoteStrikeModel
    {
        public int Id { get; }
        public int AttackerId { get; }
        public Vector3 Target { get; }
        public float Radius { get; }
        public float Remaining;
        public bool Resolved;

        public DemoRemoteStrikeModel(int id, int attackerId, Vector3 target, float radius, float remaining)
        {
            Id = id;
            AttackerId = attackerId;
            Target = target;
            Radius = radius;
            Remaining = remaining;
        }
    }

    public static class DemoDamageResolver
    {
        public static DemoDamageResult Resolve(
            DemoUnitModel attacker,
            DemoUnitModel target,
            Demo1Balance balance,
            System.Random random,
            float globalShieldBonus = 0f,
            float attackMultiplier = 1f,
            float damageTakenMultiplier = 1f,
            float coreDiscoveryMultiplier = 1f,
            float criticalChanceBonus = 0f)
        {
            float raw = attacker.Stats.Attack * Mathf.Max(0f, attackMultiplier);
            float coreDiscovery = attacker.Stats.CoreDiscovery * Mathf.Max(0f, coreDiscoveryMultiplier);
            float discoveryChance = coreDiscovery /
                                    Mathf.Max(0.01f, coreDiscovery + target.Stats.CoreConcealment);
            bool coreHit = random.NextDouble() < Mathf.Clamp01(discoveryChance);
            float criticalChance = attacker.Stats.CriticalChance + Mathf.Max(0f, criticalChanceBonus);
            bool critical = !coreHit && random.NextDouble() < Mathf.Clamp01(criticalChance);
            if (coreHit)
                raw *= balance.CoreMultiplier;
            else if (critical)
                raw *= balance.CriticalMultiplier;

            float incoming = Mathf.Max(balance.MinimumDamage, raw - Mathf.Max(0f, target.Stats.Defense));
            incoming = Mathf.Max(balance.MinimumDamage, incoming * Mathf.Max(0f, damageTakenMultiplier));
            float shieldEfficiency = Mathf.Max(0.1f, 1f + globalShieldBonus);
            float availableShieldAbsorption = target.Shield * shieldEfficiency;
            float availableMagicAbsorption = target.Magic / Mathf.Max(0.01f, balance.ShieldMagicCostPerDamage) * shieldEfficiency;
            float absorbed = Mathf.Min(incoming, Mathf.Min(availableShieldAbsorption, availableMagicAbsorption));
            float shieldSpent = absorbed / shieldEfficiency;
            target.Shield = Mathf.Max(0f, target.Shield - shieldSpent);
            target.Magic = Mathf.Max(0f, target.Magic - shieldSpent * balance.ShieldMagicCostPerDamage);

            float healthDamage = Mathf.Max(0f, incoming - absorbed);
            target.Health = Mathf.Max(0f, target.Health - healthDamage);
            if (target.Health <= 0f)
            {
                target.Activity = DemoUnitActivity.Destroyed;
                target.HasDestination = false;
                target.HasManualMoveOrder = false;
                target.LockedTargetId = -1;
                target.OrderedTargetId = -1;
                target.HasExplicitAttackOrder = false;
            }

            return new DemoDamageResult(raw, shieldSpent, healthDamage, critical, coreHit, !target.IsAlive);
        }
    }

    public sealed class Demo1Simulation
    {
        private readonly Dictionary<int, DemoUnitModel> _units = new Dictionary<int, DemoUnitModel>();
        private readonly List<DemoRemoteStrikeModel> _remoteStrikes = new List<DemoRemoteStrikeModel>();
        private readonly System.Random _random;
        private int _nextUnitId = 1;
        private int _nextStrikeId = 1;

        public Demo1Balance Balance { get; }
        public IReadOnlyCollection<DemoUnitModel> Units => _units.Values;
        public IReadOnlyList<DemoRemoteStrikeModel> RemoteStrikes => _remoteStrikes;
        public float SimulationTime { get; private set; }
        public DemoOutcome Outcome { get; private set; } = DemoOutcome.Running;
        public DemoMissionObjective MissionObjective { get; private set; } = DemoMissionObjective.DestroyFortress;
        public Vector3 BasePosition { get; private set; } = new Vector3(187.6f, 0f, 100.8f);
        public event Action<DemoBattleEvent> EventRaised;

        public Demo1Simulation(Demo1Balance balance = null)
        {
            Balance = balance ?? new Demo1Balance();
            _random = new System.Random(Balance.RandomSeed);
        }

        public DemoUnitModel AddUnit(string name, DemoTeam team, DemoUnitRole role, DemoUnitStats stats, Vector3 position)
        {
            return AddUnit(name, team, role, stats, position, DemoUnitDeploymentState.Active);
        }

        public DemoUnitModel AddUnit(string name, DemoTeam team, DemoUnitRole role, DemoUnitStats stats, Vector3 position,
            DemoUnitDeploymentState deploymentState)
        {
            DemoUnitModel unit = new DemoUnitModel(_nextUnitId++, name, team, role, stats, ClampToMap(position));
            unit.DeploymentState = team == DemoTeam.Player ? deploymentState : DemoUnitDeploymentState.Active;
            unit.RoleAbilityRemaining = Mathf.Max(0.1f, Balance.SupportPulseInterval);
            _units.Add(unit.Id, unit);
            return unit;
        }

        public DemoUnitModel GetUnit(int id)
        {
            DemoUnitModel unit;
            return _units.TryGetValue(id, out unit) ? unit : null;
        }

        public void ConfigureBase(Vector3 basePosition)
        {
            BasePosition = ClampToMap(basePosition);
        }

        public void ConfigureMissionObjective(DemoMissionObjective objective)
        {
            MissionObjective = objective;
        }

        public DemoCommandResult RequestSortie(IEnumerable<int> unitIds)
        {
            List<DemoUnitModel> candidates = (unitIds ?? Enumerable.Empty<int>())
                .Select(GetUnit)
                .Where(unit => unit != null && unit.Team == DemoTeam.Player && unit.IsAlive &&
                               unit.DeploymentState == DemoUnitDeploymentState.Standby)
                .Distinct()
                .ToList();
            if (candidates.Count == 0)
                return DemoCommandResult.Fail("No standby witches are available to sortie.");

            for (int i = 0; i < candidates.Count; i++)
            {
                DemoUnitModel unit = candidates[i];
                float angle = candidates.Count == 1 ? 0f : i * Mathf.PI * 2f / candidates.Count;
                Vector3 offset = new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle)) * Balance.BaseLaunchSpread;
                unit.Position = ClampToMap(BasePosition + offset);
                unit.Destination = unit.Position;
                unit.HasDestination = false;
                unit.HasManualMoveOrder = false;
                ClearTarget(unit);
                unit.TurnaroundRemaining = 0f;
                unit.DeploymentState = DemoUnitDeploymentState.Active;
                unit.Activity = DemoUnitActivity.Idle;
            }

            Raise($"{candidates.Count} witches launched from base.", BasePosition, true);
            return DemoCommandResult.Ok($"{candidates.Count} witches launched.");
        }

        public DemoCommandResult RequestReturnToBase(IEnumerable<int> unitIds)
        {
            List<DemoUnitModel> candidates = (unitIds ?? Enumerable.Empty<int>())
                .Select(GetUnit)
                .Where(unit => unit != null && unit.Team == DemoTeam.Player && unit.IsAlive && !unit.IsFixed &&
                               unit.DeploymentState == DemoUnitDeploymentState.Active)
                .Distinct()
                .ToList();
            if (candidates.Count == 0)
                return DemoCommandResult.Fail("No selected witches can return to base.");

            foreach (DemoUnitModel unit in candidates)
            {
                ClearTarget(unit);
                unit.DeploymentState = DemoUnitDeploymentState.Returning;
                SetMovement(unit, BasePosition, DemoUnitActivity.Moving, true);
            }
            Raise($"{candidates.Count} witches returning to base.", BasePosition, true);
            return DemoCommandResult.Ok($"{candidates.Count} witches returning to base.");
        }

        public DemoCommandResult ConfigureScoutAi(int unitId, IEnumerable<Vector3> patrolPoints)
        {
            DemoUnitModel unit = GetUnit(unitId);
            if (unit == null || unit.Team != DemoTeam.Enemy || unit.IsFixed)
                return DemoCommandResult.Fail("Scout AI can only be assigned to mobile enemies.");

            List<Vector3> points = (patrolPoints ?? Enumerable.Empty<Vector3>()).Select(ClampToMap).ToList();
            if (points.Count == 0)
                points.Add(unit.Position);
            unit.EnemyAiProfile = DemoEnemyAiProfile.Scout;
            unit.EnemyAiState = DemoEnemyAiState.Patrol;
            unit.EnemyAiHomePosition = unit.Position;
            unit.EnemyAiDecisionRemaining = 0f;
            unit.EnemyAiPatrolIndex = 0;
            unit.SetEnemyAiPatrolPoints(points);
            ClearTarget(unit);
            StopMovement(unit);
            return DemoCommandResult.Ok($"{unit.DisplayName} uses independent scout AI.");
        }

        public DemoCommandResult ConfigureCombatAi(int unitId, Vector3 homePosition)
        {
            DemoUnitModel unit = GetUnit(unitId);
            if (unit == null || unit.Team != DemoTeam.Enemy || unit.IsFixed)
                return DemoCommandResult.Fail("Combat AI can only be assigned to mobile enemies.");
            unit.EnemyAiProfile = DemoEnemyAiProfile.Combat;
            unit.EnemyAiState = DemoEnemyAiState.Guard;
            unit.EnemyAiHomePosition = ClampToMap(homePosition);
            unit.EnemyAiDecisionRemaining = 0f;
            unit.SetEnemyAiPatrolPoints(Enumerable.Empty<Vector3>());
            ClearTarget(unit);
            StopMovement(unit);
            return DemoCommandResult.Ok($"{unit.DisplayName} uses independent combat AI.");
        }

        public void GrantPersistentPlayerIntel(int unitId, DemoIntelLevel level = DemoIntelLevel.Identified)
        {
            DemoUnitModel unit = GetUnit(unitId);
            if (unit == null || unit.Team != DemoTeam.Enemy)
                return;
            unit.HasPersistentPlayerIntel = true;
            unit.PlayerIntelLevel = (DemoIntelLevel)Mathf.Max((int)DemoIntelLevel.Identified, (int)level);
            unit.LastKnownPosition = unit.Position;
            unit.LastObservedAt = SimulationTime;
            unit.IsRevealedToPlayer = true;
        }

        public bool IsUnitVisibleOnStrategicMap(int unitId)
        {
            DemoUnitModel unit = GetUnit(unitId);
            if (unit == null || !unit.IsAlive || !unit.IsOperational)
                return false;
            bool hasDeployedWitch = _units.Values.Any(candidate =>
                candidate.Team == DemoTeam.Player && candidate.IsAlive && candidate.IsOperational);
            if (!hasDeployedWitch)
                return false;
            return unit.Team == DemoTeam.Player || unit.HasPlayerIntel;
        }

        public float GetEffectiveCoreDiscovery(int unitId)
        {
            DemoUnitModel unit = GetUnit(unitId);
            return unit == null ? 0f : unit.Stats.CoreDiscovery * GetCoreDiscoveryMultiplier(unit);
        }

        public float GetEffectiveCriticalChance(int unitId)
        {
            DemoUnitModel unit = GetUnit(unitId);
            if (unit == null)
                return 0f;
            float bonus = unit.Stats.HasTrait(DemoUnitTrait.LynetteSharpshooter)
                ? Balance.LynetteCriticalChanceBonus
                : 0f;
            return Mathf.Clamp01(unit.Stats.CriticalChance + bonus);
        }

        public float GetEffectiveAttackInterval(int unitId)
        {
            DemoUnitModel unit = GetUnit(unitId);
            if (unit == null)
                return 0f;
            float multiplier = unit.Stats.HasTrait(DemoUnitTrait.LynetteSharpshooter)
                ? Balance.LynetteAttackIntervalMultiplier
                : 1f;
            return Mathf.Max(0.2f, unit.Stats.AttackInterval * multiplier);
        }

        public float GetEffectiveShieldBonus(int targetUnitId)
        {
            DemoUnitModel target = GetUnit(targetUnitId);
            if (!CanReceiveAura(target))
                return 0f;
            float bonus = 0f;
            foreach (DemoUnitModel provider in _units.Values.Where(unit => CanProvideAura(unit, target)))
            {
                if (provider.Role == DemoUnitRole.Support)
                    bonus += provider.Stats.GlobalShieldBonus * Balance.SupportEffectMultiplier;
                if (provider.Stats.HasTrait(DemoUnitTrait.MiyafujiShieldAura))
                    bonus += Balance.MiyafujiShieldEfficiencyBonus;
            }
            return Mathf.Max(0f, bonus);
        }

        public float GetMarkRemaining(int unitId)
        {
            DemoUnitModel unit = GetUnit(unitId);
            return unit == null ? 0f : Mathf.Max(0f, unit.MarkedUntil - SimulationTime);
        }

        public DemoCommandResult SetAutoAttack(IEnumerable<int> unitIds, bool enabled)
        {
            List<DemoUnitModel> candidates = (unitIds ?? Enumerable.Empty<int>())
                .Select(GetUnit)
                .Where(unit => unit != null && unit.Team == DemoTeam.Player && unit.IsAlive && !unit.IsFixed)
                .Distinct()
                .ToList();
            if (candidates.Count == 0)
                return DemoCommandResult.Fail("No selected witch can change auto-attack stance.");
            foreach (DemoUnitModel unit in candidates)
            {
                unit.AutoAttackEnabled = enabled;
                if (!enabled)
                {
                    unit.HasExplicitAttackOrder = false;
                    ClearTarget(unit);
                }
            }
            return DemoCommandResult.Ok($"Auto attack {(enabled ? "enabled" : "disabled")} for {candidates.Count} witches.");
        }

        public DemoCommandResult RequestAttack(IEnumerable<int> unitIds, int targetId)
        {
            DemoUnitModel target = GetUnit(targetId);
            if (target == null || !target.IsAlive || !target.IsOperational)
                return DemoCommandResult.Fail("Attack target is invalid.");

            List<DemoUnitModel> candidates = (unitIds ?? Enumerable.Empty<int>())
                .Select(GetUnit)
                .Where(unit => unit != null && unit.IsAlive && unit.IsOperational && !unit.IsFixed && unit.Team != target.Team)
                .Distinct()
                .ToList();
            if (candidates.Count == 0)
                return DemoCommandResult.Fail("No selected unit can attack this target.");
            if (candidates.Any(unit => unit.Team == DemoTeam.Player) && !target.CanBeDirectlyTargetedByPlayer)
                return DemoCommandResult.Fail(target.HasPlayerIntel ? "Only the target's last known position is available." : "Target intelligence is unavailable.");

            foreach (DemoUnitModel unit in candidates)
            {
                unit.LockedTargetId = target.Id;
                unit.OrderedTargetId = target.Id;
                unit.HasExplicitAttackOrder = true;
                unit.TargetLastKnownPosition = target.Team == DemoTeam.Enemy ? target.PlayerVisiblePosition : target.Position;
                unit.HasTargetLastKnownPosition = true;
                if (unit.HasManualMoveOrder && unit.HasDestination)
                    unit.Activity = DemoUnitActivity.Moving;
                else
                {
                    unit.HasDestination = false;
                    unit.HasManualMoveOrder = false;
                    unit.Activity = DemoUnitActivity.Pursuing;
                }
            }
            Raise($"{candidates.Count} units locked {target.DisplayName}.", target.PlayerVisiblePosition, true);
            return DemoCommandResult.Ok($"Attack order assigned to {candidates.Count} units.");
        }

        public DemoCommandResult IssueMove(IEnumerable<int> unitIds, Vector3 destination)
        {
            List<DemoUnitModel> candidates = (unitIds ?? Enumerable.Empty<int>())
                .Select(GetUnit)
                .Where(CanMove)
                .Distinct()
                .ToList();
            if (candidates.Count == 0)
                return DemoCommandResult.Fail("No selected unit can move.");

            float spacing = Mathf.Min(1.2f, Balance.DestinationRadius * 0.65f);
            for (int i = 0; i < candidates.Count; i++)
            {
                float angle = candidates.Count == 1 ? 0f : i * Mathf.PI * 2f / candidates.Count;
                Vector3 offset = new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle)) * spacing;
                DemoUnitModel unit = candidates[i];
                unit.OrderedTargetId = -1;
                unit.HasExplicitAttackOrder = false;
                SetMovement(unit, destination + offset, DemoUnitActivity.Moving, true);
            }
            Raise($"Independent move order assigned to {candidates.Count} units.", destination, false);
            return DemoCommandResult.Ok("Move order assigned.");
        }

        public DemoCommandResult ScheduleRemoteStrike(int attackerId, Vector3 target)
        {
            DemoUnitModel attacker = GetUnit(attackerId);
            if (attacker == null || !attacker.IsAlive || !attacker.IsOperational || !attacker.Stats.CanRemoteStrike)
                return DemoCommandResult.Fail("Selected unit cannot perform a remote strike.");
            if (attacker.RemoteStrikeCooldown > 0f)
                return DemoCommandResult.Fail($"Remote strike cooling down ({attacker.RemoteStrikeCooldown:0.0}s).");
            if (HorizontalDistance(attacker.Position, target) > Balance.RemoteStrikeRange)
                return DemoCommandResult.Fail("Target area is outside remote strike range.");

            DemoRemoteStrikeModel strike = new DemoRemoteStrikeModel(
                _nextStrikeId++, attacker.Id, ClampToMap(target), Balance.RemoteStrikeRadius, Balance.RemoteStrikeDelay);
            _remoteStrikes.Add(strike);
            attacker.RemoteStrikeCooldown = Balance.RemoteStrikeCooldown;
            Raise($"{attacker.DisplayName} launched a remote strike; impact in {strike.Remaining:0.0}s.", strike.Target, true);
            return DemoCommandResult.Ok("Remote strike scheduled.");
        }

        public void Advance(float deltaTime)
        {
            if (deltaTime <= 0f || Outcome != DemoOutcome.Running)
                return;
            float remaining = deltaTime;
            while (remaining > 0f && Outcome == DemoOutcome.Running)
            {
                float dt = Mathf.Min(remaining, 0.1f);
                SimulationTime += dt;
                TickUnitResources(dt);
                TickVisibility(dt);
                TickEnemyAi(dt);
                TickAutomaticTargeting();
                TickTargetNavigation();
                TickMovement(dt);
                TickIndividualCombat(dt);
                TickSupportPulses(dt);
                TickRemoteStrikes(dt);
                EvaluateOutcome();
                remaining -= dt;
            }
        }

        private void TickUnitResources(float dt)
        {
            foreach (DemoUnitModel unit in _units.Values)
            {
                if (!unit.IsAlive)
                {
                    if (unit.Team == DemoTeam.Player)
                        unit.DeploymentState = DemoUnitDeploymentState.Lost;
                    continue;
                }
                if (unit.Team == DemoTeam.Player && unit.DeploymentState == DemoUnitDeploymentState.Servicing)
                {
                    unit.TurnaroundRemaining = Mathf.Max(0f, unit.TurnaroundRemaining - dt);
                    if (unit.TurnaroundRemaining <= 0f)
                    {
                        unit.Health = unit.Stats.MaxHealth;
                        unit.Magic = unit.Stats.MaxMagic;
                        unit.Shield = unit.Stats.MaxShield;
                        unit.AttackCooldown = 0f;
                        unit.RemoteStrikeCooldown = 0f;
                        unit.DeploymentState = DemoUnitDeploymentState.Standby;
                        unit.Activity = DemoUnitActivity.Idle;
                        Raise($"{unit.DisplayName} is ready for another sortie.", BasePosition, true);
                    }
                    continue;
                }
                if (unit.Team == DemoTeam.Player && unit.DeploymentState == DemoUnitDeploymentState.Standby)
                    continue;

                unit.AttackCooldown = Mathf.Max(0f, unit.AttackCooldown - dt);
                unit.RemoteStrikeCooldown = Mathf.Max(0f, unit.RemoteStrikeCooldown - dt);
                if (unit.LockedTargetId < 0)
                {
                    unit.Magic = Mathf.Min(unit.Stats.MaxMagic, unit.Magic + unit.Stats.MagicRecovery * dt);
                    if (unit.Magic > unit.Stats.MaxMagic * 0.25f)
                        unit.Shield = Mathf.Min(unit.Stats.MaxShield, unit.Shield + unit.Stats.MagicRecovery * 0.45f * dt);
                }
            }
        }

        private void TickMovement(float dt)
        {
            foreach (DemoUnitModel unit in _units.Values.Where(unit => unit.IsAlive && unit.IsOperational && unit.HasDestination && !unit.IsFixed))
            {
                Vector3 movement = unit.Destination - unit.Position;
                movement.y = 0f;
                if (movement.sqrMagnitude > 0.001f)
                    unit.Facing = movement.normalized;
                unit.Position = ClampToMap(Vector3.MoveTowards(unit.Position, unit.Destination, unit.Stats.MoveSpeed * dt));

                if (unit.DeploymentState == DemoUnitDeploymentState.Returning &&
                    HorizontalDistance(unit.Position, BasePosition) <= Balance.BaseArrivalRadius)
                {
                    unit.Position = BasePosition;
                    unit.Destination = BasePosition;
                    unit.HasDestination = false;
                    unit.HasManualMoveOrder = false;
                    unit.Activity = DemoUnitActivity.Idle;
                    unit.DeploymentState = DemoUnitDeploymentState.Servicing;
                    unit.TurnaroundRemaining = Mathf.Max(0f, Balance.BaseTurnaroundDuration);
                    Raise($"{unit.DisplayName} landed and entered service.", BasePosition, true);
                    continue;
                }

                if (HorizontalDistance(unit.Position, unit.Destination) <= Balance.DestinationRadius * 0.2f)
                {
                    unit.Position = unit.Destination;
                    unit.HasDestination = false;
                    unit.HasManualMoveOrder = false;
                    if (unit.Activity == DemoUnitActivity.Moving)
                        unit.Activity = DemoUnitActivity.Idle;
                }
            }
        }

        private void TickVisibility(float dt)
        {
            List<DemoUnitModel> observers = _units.Values
                .Where(unit => unit.IsAlive && unit.IsOperational && unit.Team == DemoTeam.Player &&
                               unit.Stats.WitchVisionType != DemoWitchVisionType.None)
                .ToList();
            foreach (DemoUnitModel enemy in _units.Values.Where(unit => unit.IsAlive && unit.Team == DemoTeam.Enemy))
            {
                enemy.IsCurrentlyObservedByPlayer = false;
                DemoUnitModel observer = observers.FirstOrDefault(unit => IsInsideWitchVision(unit, enemy.Position));
                if (observer != null)
                    ObserveEnemy(enemy, dt);
                else
                    DecayEnemyIntel(enemy, dt);
            }
        }

        private bool IsInsideWitchVision(DemoUnitModel observer, Vector3 targetPosition)
        {
            Vector3 toTarget = targetPosition - observer.Position;
            toTarget.y = 0f;
            if (toTarget.sqrMagnitude > observer.Stats.VisionRadius * observer.Stats.VisionRadius)
                return false;
            if (observer.Stats.WitchVisionType == DemoWitchVisionType.Night || toTarget.sqrMagnitude < 0.001f)
                return true;
            if (observer.Stats.WitchVisionType != DemoWitchVisionType.Ordinary)
                return false;
            Vector3 facing = observer.Facing.sqrMagnitude > 0.001f ? observer.Facing.normalized : Vector3.right;
            float minimumDot = Mathf.Cos(Mathf.Clamp(observer.Stats.VisionAngle, 1f, 359f) * 0.5f * Mathf.Deg2Rad);
            return Vector3.Dot(facing, toTarget.normalized) >= minimumDot;
        }

        private void ObserveEnemy(DemoUnitModel enemy, float dt)
        {
            DemoIntelLevel previous = enemy.PlayerIntelLevel;
            enemy.IsCurrentlyObservedByPlayer = true;
            enemy.IsRevealedToPlayer = true;
            enemy.LastKnownPosition = enemy.Position;
            enemy.LastObservedAt = SimulationTime;
            if (enemy.PlayerIntelLevel < DemoIntelLevel.Identified)
            {
                enemy.PlayerIntelLevel = DemoIntelLevel.Contact;
                enemy.IdentificationProgress += dt / Mathf.Max(0.01f, Balance.VisionIdentificationDuration);
                if (enemy.IdentificationProgress >= 1f)
                    enemy.PlayerIntelLevel = DemoIntelLevel.Identified;
            }
            if (enemy.PlayerIntelLevel >= DemoIntelLevel.Identified)
            {
                enemy.AssessmentProgress += dt / Mathf.Max(0.01f, Balance.VisionAssessmentDuration);
                if (enemy.AssessmentProgress >= 1f)
                    enemy.PlayerIntelLevel = DemoIntelLevel.Assessed;
            }
            if (previous == DemoIntelLevel.Unknown && enemy.PlayerIntelLevel >= DemoIntelLevel.Contact)
                Raise("Unidentified contact detected.", enemy.Position, true);
            if (previous < DemoIntelLevel.Identified && enemy.PlayerIntelLevel >= DemoIntelLevel.Identified)
                Raise($"Enemy identified: {enemy.DisplayName}.", enemy.Position, true);
        }

        private void DecayEnemyIntel(DemoUnitModel enemy, float dt)
        {
            if (enemy.HasPersistentPlayerIntel)
            {
                if (enemy.IsFixed)
                    enemy.LastKnownPosition = enemy.Position;
                enemy.PlayerIntelLevel = (DemoIntelLevel)Mathf.Max((int)DemoIntelLevel.Identified, (int)enemy.PlayerIntelLevel);
                enemy.IsRevealedToPlayer = true;
                return;
            }
            DemoIntelLevel previous = enemy.PlayerIntelLevel;
            float elapsed = SimulationTime - enemy.LastObservedAt;
            if (elapsed >= Balance.ContactIntelMemoryDuration)
            {
                enemy.PlayerIntelLevel = DemoIntelLevel.Unknown;
                enemy.IsRevealedToPlayer = false;
            }
            else if (elapsed >= Balance.IdentifiedIntelMemoryDuration)
                enemy.PlayerIntelLevel = DemoIntelLevel.Contact;
            else if (elapsed >= Balance.AssessedIntelMemoryDuration && enemy.PlayerIntelLevel == DemoIntelLevel.Assessed)
                enemy.PlayerIntelLevel = DemoIntelLevel.Identified;
            enemy.IdentificationProgress = Mathf.Max(0f, enemy.IdentificationProgress - dt * 0.15f);
            enemy.AssessmentProgress = Mathf.Max(0f, enemy.AssessmentProgress - dt * 0.25f);
            if (previous >= DemoIntelLevel.Identified && enemy.PlayerIntelLevel == DemoIntelLevel.Contact)
                Raise($"Lost identification: {enemy.DisplayName}.", enemy.LastKnownPosition, false);
            if (previous != DemoIntelLevel.Unknown && enemy.PlayerIntelLevel == DemoIntelLevel.Unknown)
                Raise($"Contact lost: {enemy.DisplayName}.", enemy.LastKnownPosition, false);
        }

        private void TickEnemyAi(float dt)
        {
            foreach (DemoUnitModel enemy in _units.Values
                         .Where(unit => unit.IsAlive && unit.Team == DemoTeam.Enemy && unit.EnemyAiProfile != DemoEnemyAiProfile.None)
                         .ToList())
            {
                enemy.EnemyAiDecisionRemaining -= dt;
                if (enemy.EnemyAiDecisionRemaining > 0f)
                    continue;
                enemy.EnemyAiDecisionRemaining = Mathf.Max(0.05f, Balance.EnemyAiDecisionInterval);
                if (enemy.EnemyAiProfile == DemoEnemyAiProfile.Scout)
                    TickScoutAiDecision(enemy);
                else
                    TickCombatAiDecision(enemy);
            }
        }

        private void TickScoutAiDecision(DemoUnitModel scout)
        {
            if (scout.HealthRatio <= Balance.EnemyAiScoutRetreatHealthRatio)
            {
                ClearTarget(scout);
                scout.EnemyAiState = DemoEnemyAiState.Retreating;
                TickScoutPatrol(scout);
                return;
            }
            DemoUnitModel target = FindNearestVisiblePlayer(scout, false);
            if (target != null)
            {
                AssignEnemyTarget(scout, target);
                scout.EnemyAiState = HorizontalDistance(scout.Position, target.Position) <= scout.Stats.AttackRange
                    ? DemoEnemyAiState.Fighting
                    : DemoEnemyAiState.Pursue;
                return;
            }
            ClearTarget(scout);
            if (scout.EnemyAiHasLastKnownPosition &&
                HorizontalDistance(scout.Position, scout.EnemyAiLastKnownPosition) > Balance.EnemyAiArrivalRadius)
            {
                scout.EnemyAiState = DemoEnemyAiState.Investigate;
                SetMovement(scout, scout.EnemyAiLastKnownPosition, DemoUnitActivity.Moving);
                return;
            }
            scout.EnemyAiHasLastKnownPosition = false;
            TickScoutPatrol(scout);
        }

        private void TickScoutPatrol(DemoUnitModel scout)
        {
            scout.EnemyAiState = scout.HealthRatio <= Balance.EnemyAiScoutRetreatHealthRatio
                ? DemoEnemyAiState.Retreating
                : DemoEnemyAiState.Patrol;
            if (scout.EnemyAiPatrolPoints.Count == 0)
            {
                StopMovement(scout);
                return;
            }
            int index = Mathf.Clamp(scout.EnemyAiPatrolIndex, 0, scout.EnemyAiPatrolPoints.Count - 1);
            Vector3 point = scout.EnemyAiPatrolPoints[index];
            if (HorizontalDistance(scout.Position, point) <= Balance.EnemyAiArrivalRadius)
            {
                index = (index + 1) % scout.EnemyAiPatrolPoints.Count;
                scout.EnemyAiPatrolIndex = index;
                point = scout.EnemyAiPatrolPoints[index];
            }
            SetMovement(scout, point, DemoUnitActivity.Moving);
        }

        private void TickCombatAiDecision(DemoUnitModel enemy)
        {
            DemoUnitModel target = FindNearestVisiblePlayer(enemy, true);
            if (target != null)
            {
                AssignEnemyTarget(enemy, target);
                enemy.EnemyAiState = HorizontalDistance(enemy.Position, target.Position) <= enemy.Stats.AttackRange
                    ? DemoEnemyAiState.Fighting
                    : DemoEnemyAiState.Pursue;
                return;
            }
            ClearTarget(enemy);
            if (HorizontalDistance(enemy.Position, enemy.EnemyAiHomePosition) > Balance.EnemyAiArrivalRadius)
            {
                enemy.EnemyAiState = DemoEnemyAiState.ReturnHome;
                SetMovement(enemy, enemy.EnemyAiHomePosition, DemoUnitActivity.Moving);
            }
            else
            {
                enemy.EnemyAiState = DemoEnemyAiState.Guard;
                StopMovement(enemy);
            }
        }

        private DemoUnitModel FindNearestVisiblePlayer(DemoUnitModel observer, bool enforceHomeLeash)
        {
            float visionRadius = Mathf.Max(0f, observer.Stats.VisionRadius);
            return _units.Values
                .Where(unit => unit.IsAlive && unit.IsOperational && unit.Team == DemoTeam.Player &&
                               HorizontalDistance(observer.Position, unit.Position) <= visionRadius &&
                               (!enforceHomeLeash || HorizontalDistance(observer.EnemyAiHomePosition, unit.Position) <= Balance.EnemyAiGuardLeashRadius))
                .OrderBy(unit => HorizontalDistance(observer.Position, unit.Position))
                .ThenBy(unit => unit.Id)
                .FirstOrDefault();
        }

        private void TickAutomaticTargeting()
        {
            foreach (DemoUnitModel unit in _units.Values.Where(unit => unit.IsAlive && unit.IsOperational))
            {
                if (unit.Team == DemoTeam.Player)
                {
                    if (!unit.AutoAttackEnabled || unit.HasExplicitAttackOrder || unit.LockedTargetId >= 0 ||
                        unit.DeploymentState != DemoUnitDeploymentState.Active)
                        continue;
                    DemoUnitModel target = _units.Values
                        .Where(candidate => candidate.IsAlive && candidate.IsOperational && candidate.Team == DemoTeam.Enemy &&
                                            candidate.IsCurrentlyObservedByPlayer && candidate.PlayerIntelLevel >= DemoIntelLevel.Identified &&
                                            HorizontalDistance(unit.Position, candidate.Position) <= unit.Stats.EngagementRadius)
                        .OrderBy(candidate => HorizontalDistance(unit.Position, candidate.Position))
                        .ThenBy(candidate => candidate.Id)
                        .FirstOrDefault();
                    if (target != null)
                        AssignAutomaticTarget(unit, target);
                }
                else if (unit.IsFixed && unit.LockedTargetId < 0)
                {
                    DemoUnitModel target = _units.Values
                        .Where(candidate => candidate.IsAlive && candidate.IsOperational && candidate.Team == DemoTeam.Player &&
                                            HorizontalDistance(unit.Position, candidate.Position) <= unit.Stats.EngagementRadius)
                        .OrderBy(candidate => HorizontalDistance(unit.Position, candidate.Position))
                        .ThenBy(candidate => candidate.Id)
                        .FirstOrDefault();
                    if (target != null)
                        AssignEnemyTarget(unit, target);
                }
            }
        }

        private void TickTargetNavigation()
        {
            foreach (DemoUnitModel unit in _units.Values.Where(unit => unit.IsAlive && unit.IsOperational && unit.LockedTargetId >= 0).ToList())
            {
                DemoUnitModel target = GetUnit(unit.LockedTargetId);
                if (target == null || !target.IsAlive || !target.IsOperational || target.Team == unit.Team)
                {
                    ClearTarget(unit);
                    continue;
                }

                bool targetVisible = IsTargetVisibleToAttacker(unit, target);
                if (unit.HasManualMoveOrder)
                {
                    if (targetVisible)
                    {
                        unit.TargetLastKnownPosition = target.Position;
                        unit.HasTargetLastKnownPosition = true;
                    }
                    else
                        ClearTarget(unit);
                    unit.Activity = DemoUnitActivity.Moving;
                    continue;
                }
                if (unit.Team == DemoTeam.Player && !unit.HasExplicitAttackOrder &&
                    (!targetVisible || HorizontalDistance(unit.Position, target.Position) > unit.Stats.EngagementRadius))
                {
                    ClearTarget(unit);
                    continue;
                }

                if (targetVisible)
                {
                    unit.TargetLastKnownPosition = target.Position;
                    unit.HasTargetLastKnownPosition = true;
                    float distance = HorizontalDistance(unit.Position, target.Position);
                    float attackRange = Mathf.Max(0.1f, unit.Stats.AttackRange);
                    float pursuitStopRange = attackRange * 0.75f;
                    if (unit.IsFixed)
                        unit.Activity = distance <= attackRange ? DemoUnitActivity.Attacking : DemoUnitActivity.Idle;
                    else if (distance <= pursuitStopRange)
                    {
                        StopMovement(unit);
                        unit.Activity = DemoUnitActivity.Attacking;
                    }
                    else
                        SetMovement(unit, target.Position, DemoUnitActivity.Pursuing);
                    continue;
                }

                if (unit.HasExplicitAttackOrder && unit.HasTargetLastKnownPosition && !unit.IsFixed)
                {
                    if (HorizontalDistance(unit.Position, unit.TargetLastKnownPosition) <= Balance.EnemyAiArrivalRadius)
                        ClearTarget(unit);
                    else
                        SetMovement(unit, unit.TargetLastKnownPosition, DemoUnitActivity.Pursuing);
                }
                else
                    ClearTarget(unit);
            }
        }

        private void TickIndividualCombat(float dt)
        {
            foreach (DemoUnitModel attacker in _units.Values
                         .Where(unit => unit.IsAlive && unit.IsOperational && unit.LockedTargetId >= 0)
                         .OrderBy(unit => unit.Id)
                         .ToList())
            {
                DemoUnitModel target = GetUnit(attacker.LockedTargetId);
                if (target == null || !target.IsAlive || !IsTargetVisibleToAttacker(attacker, target) ||
                    HorizontalDistance(attacker.Position, target.Position) > Mathf.Max(0.1f, attacker.Stats.AttackRange))
                    continue;
                if (attacker.AttackCooldown > 0f)
                    continue;

                Vector3 facing = target.Position - attacker.Position;
                facing.y = 0f;
                if (facing.sqrMagnitude > 0.001f)
                    attacker.Facing = facing.normalized;
                float attackMultiplier = target.MarkedUntil > SimulationTime ? Balance.ScoutMarkDamageMultiplier : 1f;
                int nextAttack = attacker.AttacksPerformed + 1;
                bool artillerySalvo = attacker.Role == DemoUnitRole.Artillery &&
                                       !attacker.Stats.HasTrait(DemoUnitTrait.LynetteSharpshooter) &&
                                       nextAttack % Mathf.Max(1, Balance.ArtillerySalvoEveryAttacks) == 0;
                if (artillerySalvo)
                    attackMultiplier *= Balance.ArtillerySalvoDamageMultiplier;
                bool fortressBarrage = attacker.Role == DemoUnitRole.Fortress &&
                                       attacker.HealthRatio <= Balance.FortressBarrageHealthThreshold;
                if (fortressBarrage)
                {
                    attackMultiplier *= Balance.FortressBarrageDamageMultiplier;
                    if (!attacker.FortressBarrageAnnounced)
                    {
                        attacker.FortressBarrageAnnounced = true;
                        Raise($"{attacker.DisplayName} entered emergency barrage mode.", attacker.Position, true);
                    }
                }

                float criticalBonus = attacker.Stats.HasTrait(DemoUnitTrait.LynetteSharpshooter)
                    ? Balance.LynetteCriticalChanceBonus
                    : 0f;
                DemoDamageResult result = DemoDamageResolver.Resolve(
                    attacker, target, Balance, _random, GetEffectiveShieldBonus(target.Id), attackMultiplier, 1f,
                    GetCoreDiscoveryMultiplier(attacker), criticalBonus);
                attacker.AttacksPerformed = nextAttack;
                if (attacker.Role == DemoUnitRole.Scout && target.IsAlive)
                    target.MarkedUntil = Mathf.Max(target.MarkedUntil, SimulationTime + Balance.ScoutMarkDuration);
                float intervalMultiplier = fortressBarrage ? Balance.FortressBarrageIntervalMultiplier : 1f;
                attacker.AttackCooldown = Mathf.Max(0.2f, GetEffectiveAttackInterval(attacker.Id) * intervalMultiplier);
                ReportDamage(attacker, target, result, target.Position, false);
                if (artillerySalvo)
                    Raise($"{attacker.DisplayName} completed a calibrated salvo.", attacker.Position, false);
                if (result.Destroyed)
                    ClearDestroyedTarget(target.Id);
            }
        }

        private void TickSupportPulses(float dt)
        {
            foreach (DemoUnitModel support in _units.Values.Where(unit => CanProvideAura(unit, unit) && unit.Role == DemoUnitRole.Support))
            {
                support.RoleAbilityRemaining -= dt;
                if (support.RoleAbilityRemaining > 0f)
                    continue;
                support.RoleAbilityRemaining = Mathf.Max(0.1f, Balance.SupportPulseInterval);
                float radius = Mathf.Max(0f, support.Stats.SupportRadius);
                bool restored = false;
                foreach (DemoUnitModel ally in _units.Values.Where(unit => CanReceiveAura(unit) && unit.Team == support.Team &&
                                                                           HorizontalDistance(unit.Position, support.Position) <= radius))
                {
                    float oldShield = ally.Shield;
                    float oldMagic = ally.Magic;
                    ally.Shield = Mathf.Min(ally.Stats.MaxShield, ally.Shield + Balance.SupportPulseShield * Balance.SupportEffectMultiplier);
                    ally.Magic = Mathf.Min(ally.Stats.MaxMagic, ally.Magic + Balance.SupportPulseMagic * Balance.SupportEffectMultiplier);
                    restored |= ally.Shield > oldShield || ally.Magic > oldMagic;
                }
                if (restored)
                    Raise($"{support.DisplayName} emitted a support pulse.", support.Position, false);
            }
        }

        private void TickRemoteStrikes(float dt)
        {
            foreach (DemoRemoteStrikeModel strike in _remoteStrikes.Where(item => !item.Resolved).ToList())
            {
                strike.Remaining -= dt;
                if (strike.Remaining > 0f)
                    continue;
                strike.Resolved = true;
                DemoUnitModel attacker = GetUnit(strike.AttackerId);
                if (attacker == null || !attacker.IsAlive)
                    continue;
                List<DemoUnitModel> targets = _units.Values
                    .Where(unit => unit.IsAlive && unit.IsOperational && unit.Team != attacker.Team &&
                                   HorizontalDistance(unit.Position, strike.Target) <= strike.Radius)
                    .ToList();
                foreach (DemoUnitModel target in targets)
                {
                    DemoDamageResult result = DemoDamageResolver.Resolve(
                        attacker, target, Balance, _random, GetEffectiveShieldBonus(target.Id), Balance.RemoteStrikeDamageMultiplier);
                    ReportDamage(attacker, target, result, strike.Target, true);
                    if (result.Destroyed)
                        ClearDestroyedTarget(target.Id);
                }
                Raise($"Remote strike hit {targets.Count} targets.", strike.Target, true);
            }
        }

        private float GetCoreDiscoveryMultiplier(DemoUnitModel attacker)
        {
            if (attacker == null || !attacker.IsAlive)
                return 1f;
            float bonus = _units.Values
                .Where(provider => CanProvideAura(provider, attacker) && provider.Stats.HasTrait(DemoUnitTrait.SakamotoCoreInsight))
                .Sum(provider => Balance.SakamotoCoreDiscoveryBonus);
            return Mathf.Max(0f, 1f + bonus);
        }

        private bool CanProvideAura(DemoUnitModel provider, DemoUnitModel target)
        {
            return provider != null && target != null && provider.IsAlive && target.IsAlive &&
                   provider.IsOperational && target.IsOperational && provider.Team == target.Team &&
                   provider.Stats.SupportRadius > 0f &&
                   HorizontalDistance(provider.Position, target.Position) <= provider.Stats.SupportRadius;
        }

        private static bool CanReceiveAura(DemoUnitModel unit)
        {
            return unit != null && unit.IsAlive && unit.IsOperational;
        }

        private bool IsTargetVisibleToAttacker(DemoUnitModel attacker, DemoUnitModel target)
        {
            if (attacker.Team == DemoTeam.Player)
                return target.HasPersistentPlayerIntel ||
                       (target.IsCurrentlyObservedByPlayer && target.PlayerIntelLevel >= DemoIntelLevel.Identified);
            return HorizontalDistance(attacker.Position, target.Position) <= Mathf.Max(0f, attacker.Stats.VisionRadius);
        }

        private void AssignAutomaticTarget(DemoUnitModel unit, DemoUnitModel target)
        {
            unit.LockedTargetId = target.Id;
            unit.OrderedTargetId = -1;
            unit.HasExplicitAttackOrder = false;
            unit.TargetLastKnownPosition = target.Position;
            unit.HasTargetLastKnownPosition = true;
            if (!unit.HasManualMoveOrder)
                unit.Activity = DemoUnitActivity.Pursuing;
        }

        private void AssignEnemyTarget(DemoUnitModel enemy, DemoUnitModel target)
        {
            enemy.LockedTargetId = target.Id;
            enemy.OrderedTargetId = -1;
            enemy.HasExplicitAttackOrder = false;
            enemy.TargetLastKnownPosition = target.Position;
            enemy.HasTargetLastKnownPosition = true;
            enemy.EnemyAiTargetId = target.Id;
            enemy.EnemyAiLastKnownPosition = target.Position;
            enemy.EnemyAiHasLastKnownPosition = true;
        }

        private void ClearTarget(DemoUnitModel unit)
        {
            if (unit == null)
                return;
            unit.LockedTargetId = -1;
            unit.OrderedTargetId = -1;
            unit.HasExplicitAttackOrder = false;
            unit.HasTargetLastKnownPosition = false;
            unit.EnemyAiTargetId = -1;
            if (unit.HasManualMoveOrder && unit.HasDestination)
            {
                unit.Activity = DemoUnitActivity.Moving;
                return;
            }
            if (unit.Activity == DemoUnitActivity.Attacking || unit.Activity == DemoUnitActivity.Pursuing)
            {
                unit.HasDestination = false;
                unit.Activity = DemoUnitActivity.Idle;
            }
        }

        private void ClearDestroyedTarget(int targetId)
        {
            foreach (DemoUnitModel unit in _units.Values.Where(unit => unit.LockedTargetId == targetId).ToList())
                ClearTarget(unit);
        }

        private void SetMovement(DemoUnitModel unit, Vector3 destination, DemoUnitActivity activity, bool manualMoveOrder = false)
        {
            if (unit == null || unit.IsFixed || !unit.IsAlive || !unit.IsOperational)
                return;
            unit.Destination = ClampToMap(destination);
            unit.HasDestination = true;
            unit.HasManualMoveOrder = manualMoveOrder;
            unit.Activity = activity;
            Vector3 facing = unit.Destination - unit.Position;
            facing.y = 0f;
            if (facing.sqrMagnitude > 0.001f)
                unit.Facing = facing.normalized;
        }

        private void StopMovement(DemoUnitModel unit)
        {
            if (unit == null)
                return;
            unit.Destination = unit.Position;
            unit.HasDestination = false;
            unit.HasManualMoveOrder = false;
            if (unit.Activity == DemoUnitActivity.Moving || unit.Activity == DemoUnitActivity.Pursuing)
                unit.Activity = DemoUnitActivity.Idle;
        }

        private void ReportDamage(DemoUnitModel attacker, DemoUnitModel target, DemoDamageResult result, Vector3 position, bool remote)
        {
            string modifier = result.CoreHit ? "core hit" : result.Critical ? "critical hit" : "hit";
            if (result.Destroyed)
            {
                if (target.Team == DemoTeam.Player)
                    target.DeploymentState = DemoUnitDeploymentState.Lost;
                Raise($"{target.DisplayName} was destroyed by {(remote ? "a remote strike" : attacker.DisplayName)}.", position, true);
            }
            else if (result.CoreHit || target.HealthRatio <= 0.3f)
                Raise($"{attacker.DisplayName} scored a {modifier} on {target.DisplayName}; HP {target.Health:0}.", position, true);
        }

        private void EvaluateOutcome()
        {
            List<DemoUnitModel> enemies = _units.Values.Where(unit => unit.Team == DemoTeam.Enemy).ToList();
            if (MissionObjective == DemoMissionObjective.DestroyAllEnemies && enemies.Count > 0 && enemies.All(unit => !unit.IsAlive))
            {
                Outcome = DemoOutcome.Victory;
                Raise("Mission complete: all attacking enemies destroyed.", enemies[0].Position, true);
                return;
            }
            if (MissionObjective == DemoMissionObjective.DestroyFortress)
            {
                List<DemoUnitModel> fortresses = enemies.Where(unit => unit.Role == DemoUnitRole.Fortress).ToList();
                if (fortresses.Count > 0 && fortresses.All(unit => !unit.IsAlive))
                {
                    Outcome = DemoOutcome.Victory;
                    Raise("Mission complete: fixed enemy objective destroyed.", fortresses[0].Position, true);
                    return;
                }
            }
            List<DemoUnitModel> playerRoster = _units.Values.Where(unit => unit.Team == DemoTeam.Player).ToList();
            if (playerRoster.Count > 0 && playerRoster.All(unit => unit.DeploymentState == DemoUnitDeploymentState.Lost))
            {
                Outcome = DemoOutcome.Defeat;
                Raise("Mission failed: no player units remain operational.", Vector3.zero, true);
            }
        }

        private bool CanMove(DemoUnitModel unit)
        {
            return unit != null && unit.IsAlive && unit.IsOperational &&
                   (unit.Team == DemoTeam.Enemy || unit.DeploymentState == DemoUnitDeploymentState.Active) && !unit.IsFixed;
        }

        private Vector3 ClampToMap(Vector3 position)
        {
            return new Vector3(
                Mathf.Clamp(position.x, -Balance.MapHalfWidth, Balance.MapHalfWidth),
                0f,
                Mathf.Clamp(position.z, -Balance.MapHalfHeight, Balance.MapHalfHeight));
        }

        private static float HorizontalDistance(Vector3 a, Vector3 b)
        {
            a.y = 0f;
            b.y = 0f;
            return Vector3.Distance(a, b);
        }

        private void Raise(string message, Vector3 position, bool important)
        {
            EventRaised?.Invoke(new DemoBattleEvent(SimulationTime, message, position, important));
        }
    }
}
