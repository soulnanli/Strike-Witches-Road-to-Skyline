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

    public enum DemoSpecialAbility
    {
        None,
        MagicEyeSearch,
        Heal,
        FireControlSolution,
        LightningStrike
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
        Destroyed,
        Resupplying
    }

    public enum DemoFlightMode
    {
        Normal,
        Loiter,
        EnteringHover,
        Hovering
    }

    public enum DemoPassiveAbility
    {
        None,
        FireControlSolution
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
        public int SupplyPackageCount = 3;
        public float SupplyCallRange = 140f;
        public float SupplyCallCooldown = 20f;
        public float SupplyDeliveryDelay = 8f;
        public float SupplyZoneDuration = 35f;
        public float SupplyZoneRadius = 6f;
        public float SupplyPackageCapacity = 180f;
        public float SupplyApproachRadiusRatio = 0.7f;
        public float SupplyHitPauseDuration = 2f;
        public float SupplyAmmoPerSecond = 8f;
        public float SupplyAmmoCost = 1f;
        public float SupplyMagicPerSecond = 12f;
        public float SupplyMagicCost = 0.5f;
        public float SupplyShieldPerSecond = 10f;
        public float SupplyShieldCost = 0.75f;
        public float LoiterRadius = 1.5f;
        public float AccelerationDuration = 8f;
        public float TurnSpeedCapAt180 = 0.3f;
        public float LoiterStraightHalfLength = 2.5f;
        public float LoiterTurnRadius = 2.5f;
        public int LoiterArcSegments = 8;
        public float HoverStopSpeed = 0.02f;
        public float LockFireThreshold = 25f;
        public float LockBaseGrowthPerSecond = 25f;
        public float LockDecayPerSecond = 35f;
        public float MinimumHitChance = 0.05f;
        public float MaximumHitChance = 0.95f;
        public float MinimumEvasionChance = 0.05f;
        public float MaximumEvasionChance = 0.45f;
        public float SuppressionBasePerHit = 8f;
        public float SuppressionDamageScale = 40f;
        public float ExplosiveSuppressionMultiplier = 1.5f;
        public float SuppressionRecoveryDelay = 2f;
        public float SuppressionRecoveryPerSecond = 12f;
        public float FullSuppressionAttackIntervalPenalty = 0.75f;
        public float FullSuppressionSpeedPenalty = 0.3f;
        public float FullSuppressionVisionRangePenalty = 0.25f;
        public float FullSuppressionLockPenalty = 0.5f;
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
        public float AttackRange = 8f;
        public float BaseAccuracy = 0.72f;
        public float Penetration = 16f;
        public float Armor;
        public int MagazineSize = 8;
        public int ReserveAmmo = 32;
        public int AmmoPerAttack = 1;
        public float ReloadDuration = 3f;
        public bool UnlimitedReserveAmmo;
        public float ExplosiveRadius;
        public float ProjectileSpeed = 12f;
        public float ProjectileTurnRate = 120f;
        public float ProjectileLifetime = 10f;
        public float ProjectileContactRadius = 0.5f;
        public float SupportRadius;
        public bool CanRemoteStrike;
        public DemoUnitTrait Traits = DemoUnitTrait.None;
        public DemoSpecialAbility SpecialAbility;
        public DemoPassiveAbility PassiveAbility;
        public float PassiveActivationDelay = 3f;
        public float PassiveAttackRange = 48f;
        public float PassiveDamageMultiplier = 2f;
        public float PassivePenetration = 32f;
        public float PassiveMinimumAccuracy = 0.85f;
        public float AbilityMagicCost;
        public float AbilityCooldown;
        public float AbilityRange;
        public float AbilityRadius;
        public float AbilityDuration;
        public float AbilityValue;
        public float AbilityDamageMultiplier = 1f;
        public float AbilityPenetration;
        public float AbilityMinimumAccuracy;
        public float AbilityArcAngle;
        public float AbilitySuppression;

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
        public readonly bool Hit;
        public readonly bool Evaded;
        public readonly float HitChance;
        public readonly float EvasionChance;

        public DemoDamageResult(float rawDamage, float shieldDamage, float healthDamage, bool critical, bool coreHit,
            bool destroyed, bool hit = true, bool evaded = false, float hitChance = 1f, float evasionChance = 0f)
        {
            RawDamage = rawDamage;
            ShieldDamage = shieldDamage;
            HealthDamage = healthDamage;
            Critical = critical;
            CoreHit = coreHit;
            Destroyed = destroyed;
            Hit = hit;
            Evaded = evaded;
            HitChance = hitChance;
            EvasionChance = evasionChance;
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
        public float CurrentSpeed;
        public Vector3 CurrentVelocity;
        public float TurnRatio;
        public bool HasLoiter;
        public Vector3 LoiterCenter;
        public float LoiterAngle;
        public float StableLoiterTime;
        public DemoFlightMode FlightMode;
        public float HoverStableTime;
        public int LoiterWaypointIndex;
        public DemoUnitActivity Activity = DemoUnitActivity.Idle;
        public DemoUnitDeploymentState DeploymentState;
        public float TurnaroundRemaining;
        public float Health;
        public float Magic;
        public float Shield;
        public float AttackCooldown;
        public int MagazineAmmo;
        public int ReserveAmmo;
        public float ReloadRemaining;
        public int SupplyDropId = -1;
        public float SupplyAmmoProgress;
        public bool AutoAttackBeforeResupply = true;
        public float LockQuality;
        public float Suppression;
        public float LastHitAt = float.NegativeInfinity;
        public float RemoteStrikeCooldown;
        public bool AutoAttackEnabled = true;
        public int LockedTargetId = -1;
        public int OrderedTargetId = -1;
        public bool HasExplicitAttackOrder;
        public Vector3 TargetLastKnownPosition;
        public bool HasTargetLastKnownPosition;
        public int AttacksPerformed;
        public float RoleAbilityRemaining;
        public float AbilityCooldownRemaining;
        public float AbilityChannelRemaining;
        public int AbilityTargetId = -1;
        public Vector3 LastAimPoint;
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
        private readonly List<Vector3> _loiterWaypoints = new List<Vector3>();

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
        public float SuppressionRatio => Mathf.Clamp01(Suppression / 100f);
        public float LockQualityRatio => Mathf.Clamp01(LockQuality / 100f);
        public bool IsReloading => ReloadRemaining > 0f;
        public bool IsResupplying => SupplyDropId >= 0;
        public bool IsChannelingAbility => AbilityChannelRemaining > 0f;
        public bool HasUsableAmmo => Stats.UnlimitedReserveAmmo || MagazineAmmo >= Mathf.Max(1, Stats.AmmoPerAttack) || ReserveAmmo > 0;
        public bool HasPlayerIntel => Team == DemoTeam.Player || IsRevealedToPlayer || PlayerIntelLevel != DemoIntelLevel.Unknown;
        public bool CanBeDirectlyTargetedByPlayer => Team == DemoTeam.Player || HasPersistentPlayerIntel ||
                                                     (IsCurrentlyObservedByPlayer && PlayerIntelLevel >= DemoIntelLevel.Identified) ||
                                                     (IsRevealedToPlayer && PlayerIntelLevel == DemoIntelLevel.Unknown);
        public Vector3 PlayerVisiblePosition => Team == DemoTeam.Player || IsCurrentlyObservedByPlayer || HasPersistentPlayerIntel
            ? Position
            : LastKnownPosition;
        public IReadOnlyList<Vector3> EnemyAiPatrolPoints => _enemyAiPatrolPoints;
        public IReadOnlyList<Vector3> LoiterWaypoints => _loiterWaypoints;
        public bool IsHovering => FlightMode == DemoFlightMode.Hovering;
        public bool IsEnteringHover => FlightMode == DemoFlightMode.EnteringHover;
        public bool IsFireControlReady => Stats.PassiveAbility == DemoPassiveAbility.FireControlSolution &&
                                          IsHovering && HoverStableTime >= Mathf.Max(0f, Stats.PassiveActivationDelay);

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
            MagazineAmmo = Mathf.Max(1, Stats.MagazineSize);
            ReserveAmmo = Mathf.Max(0, Stats.ReserveAmmo);
            LoiterCenter = position;
            LastAimPoint = position;
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

        internal void SetLoiterWaypoints(IEnumerable<Vector3> waypoints)
        {
            _loiterWaypoints.Clear();
            _loiterWaypoints.AddRange(waypoints);
            LoiterWaypointIndex = 0;
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

    public sealed class DemoProjectileModel
    {
        public int Id { get; }
        public int AttackerId { get; }
        public int TargetId { get; }
        public Vector3 Position;
        public Vector3 Facing;
        public float Speed { get; }
        public float TurnRate { get; }
        public float ContactRadius { get; }
        public float ExplosionRadius { get; }
        public float LaunchHitChance { get; }
        public float RemainingLifetime;
        public bool Resolved;

        public DemoProjectileModel(int id, int attackerId, int targetId, Vector3 position, Vector3 facing,
            float speed, float turnRate, float lifetime, float contactRadius, float explosionRadius, float launchHitChance)
        {
            Id = id;
            AttackerId = attackerId;
            TargetId = targetId;
            Position = position;
            Facing = facing.sqrMagnitude > 0.001f ? facing.normalized : Vector3.right;
            Speed = speed;
            TurnRate = turnRate;
            RemainingLifetime = lifetime;
            ContactRadius = contactRadius;
            ExplosionRadius = explosionRadius;
            LaunchHitChance = launchHitChance;
        }
    }

    public sealed class DemoSupplyDropModel
    {
        public int Id { get; }
        public Vector3 Position { get; }
        public float Radius { get; }
        public float Capacity { get; }
        public float RemainingSupply;
        public float InboundRemaining;
        public float ActiveRemaining;
        public bool Finished;

        public bool IsInbound => !Finished && InboundRemaining > 0f;
        public bool IsActive => !Finished && InboundRemaining <= 0f && ActiveRemaining > 0f && RemainingSupply > 0f;
        public float CapacityRatio => Capacity <= 0f ? 0f : Mathf.Clamp01(RemainingSupply / Capacity);

        public DemoSupplyDropModel(int id, Vector3 position, float radius, float capacity,
            float inboundRemaining, float activeRemaining)
        {
            Id = id;
            Position = position;
            Radius = radius;
            Capacity = capacity;
            RemainingSupply = capacity;
            InboundRemaining = inboundRemaining;
            ActiveRemaining = activeRemaining;
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
            float criticalChanceBonus = 0f,
            float hitChance = 1f,
            float evasionChance = 0f,
            float penetrationOverride = -1f,
            bool forceCore = false,
            float targetArmorMultiplier = 1f)
        {
            hitChance = Mathf.Clamp01(hitChance);
            evasionChance = Mathf.Clamp01(evasionChance);
            if (random.NextDouble() >= hitChance)
                return new DemoDamageResult(0f, 0f, 0f, false, false, false, false, false, hitChance, evasionChance);
            if (random.NextDouble() < evasionChance)
                return new DemoDamageResult(0f, 0f, 0f, false, false, false, true, true, hitChance, evasionChance);

            float raw = attacker.Stats.Attack * Mathf.Max(0f, attackMultiplier);
            float coreDiscovery = attacker.Stats.CoreDiscovery * Mathf.Max(0f, coreDiscoveryMultiplier);
            float discoveryChance = coreDiscovery /
                                    Mathf.Max(0.01f, coreDiscovery + target.Stats.CoreConcealment);
            bool coreHit = forceCore || random.NextDouble() < Mathf.Clamp01(discoveryChance);
            float criticalChance = attacker.Stats.CriticalChance + Mathf.Max(0f, criticalChanceBonus);
            bool critical = !coreHit && random.NextDouble() < Mathf.Clamp01(criticalChance);
            if (coreHit)
                raw *= balance.CoreMultiplier;
            else if (critical)
                raw *= balance.CriticalMultiplier;

            float incoming = raw * Mathf.Max(0f, damageTakenMultiplier);
            if (target.Team == DemoTeam.Enemy && target.Stats.Armor > 0f)
            {
                float effectiveArmor = target.Stats.Armor * Mathf.Max(0f, targetArmorMultiplier) * (coreHit ? 0.5f : 1f);
                float penetration = penetrationOverride >= 0f ? penetrationOverride : attacker.Stats.Penetration;
                float armorMultiplier = penetration >= effectiveArmor
                    ? 1f
                    : penetration >= effectiveArmor * 0.75f ? 0.35f : 0.1f;
                incoming *= armorMultiplier;
            }

            float shieldEfficiency = target.Team == DemoTeam.Player ? 1f : 0f;
            float availableShieldAbsorption = target.Shield * shieldEfficiency;
            float availableMagicAbsorption = target.Magic / Mathf.Max(0.01f, balance.ShieldMagicCostPerDamage) * shieldEfficiency;
            float absorbed = Mathf.Min(incoming, Mathf.Min(availableShieldAbsorption, availableMagicAbsorption));
            float shieldSpent = shieldEfficiency > 0f ? absorbed : 0f;
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

            return new DemoDamageResult(raw, shieldSpent, healthDamage, critical, coreHit, !target.IsAlive,
                true, false, hitChance, evasionChance);
        }
    }

    public sealed class Demo1Simulation
    {
        private readonly Dictionary<int, DemoUnitModel> _units = new Dictionary<int, DemoUnitModel>();
        private readonly List<DemoRemoteStrikeModel> _remoteStrikes = new List<DemoRemoteStrikeModel>();
        private readonly List<DemoProjectileModel> _projectiles = new List<DemoProjectileModel>();
        private readonly List<DemoSupplyDropModel> _supplyDrops = new List<DemoSupplyDropModel>();
        private readonly System.Random _random;
        private int _nextUnitId = 1;
        private int _nextStrikeId = 1;
        private int _nextProjectileId = 1;
        private int _nextSupplyDropId = 1;

        public Demo1Balance Balance { get; }
        public IReadOnlyCollection<DemoUnitModel> Units => _units.Values;
        public IReadOnlyList<DemoRemoteStrikeModel> RemoteStrikes => _remoteStrikes;
        public IReadOnlyList<DemoProjectileModel> Projectiles => _projectiles;
        public IReadOnlyList<DemoSupplyDropModel> SupplyDrops => _supplyDrops;
        public int SupplyPackagesRemaining { get; private set; }
        public float SupplyCallCooldownRemaining { get; private set; }
        public float SimulationTime { get; private set; }
        public DemoOutcome Outcome { get; private set; } = DemoOutcome.Running;
        public DemoMissionObjective MissionObjective { get; private set; } = DemoMissionObjective.DestroyFortress;
        public Vector3 BasePosition { get; private set; } = new Vector3(187.6f, 0f, 100.8f);
        public event Action<DemoBattleEvent> EventRaised;

        public Demo1Simulation(Demo1Balance balance = null)
        {
            Balance = balance ?? new Demo1Balance();
            _random = new System.Random(Balance.RandomSeed);
            SupplyPackagesRemaining = Mathf.Max(0, Balance.SupplyPackageCount);
        }

        public DemoUnitModel AddUnit(string name, DemoTeam team, DemoUnitRole role, DemoUnitStats stats, Vector3 position)
        {
            return AddUnit(name, team, role, stats, position, DemoUnitDeploymentState.Active);
        }

        public DemoUnitModel AddUnit(string name, DemoTeam team, DemoUnitRole role, DemoUnitStats stats, Vector3 position,
            DemoUnitDeploymentState deploymentState)
        {
            DemoUnitStats runtimeStats = stats?.Clone() ?? new DemoUnitStats();
            if (runtimeStats.MagazineSize <= 0) runtimeStats.MagazineSize = 8;
            if (runtimeStats.AmmoPerAttack <= 0) runtimeStats.AmmoPerAttack = 1;
            if (runtimeStats.ReloadDuration <= 0f) runtimeStats.ReloadDuration = 3f;
            if (runtimeStats.BaseAccuracy <= 0f) runtimeStats.BaseAccuracy = 0.72f;
            if (runtimeStats.Penetration <= 0f) runtimeStats.Penetration = team == DemoTeam.Enemy ? 18f : 16f;
            if (team == DemoTeam.Enemy)
            {
                runtimeStats.UnlimitedReserveAmmo = true;
                if (runtimeStats.Armor <= 0f)
                    runtimeStats.Armor = Mathf.Max(1f, runtimeStats.Defense * 1.5f);
            }
            DemoUnitModel unit = new DemoUnitModel(_nextUnitId++, name, team, role, runtimeStats, ClampToMap(position));
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
                unit.CurrentSpeed = 0f;
                unit.CurrentVelocity = Vector3.zero;
                unit.HasLoiter = false;
                unit.StableLoiterTime = 0f;
                unit.FlightMode = DemoFlightMode.Normal;
                unit.HoverStableTime = 0f;
                unit.SetLoiterWaypoints(Enumerable.Empty<Vector3>());
                unit.MagazineAmmo = Mathf.Max(1, unit.Stats.MagazineSize);
                unit.ReserveAmmo = Mathf.Max(0, unit.Stats.ReserveAmmo);
                unit.ReloadRemaining = 0f;
                unit.SupplyDropId = -1;
                unit.SupplyAmmoProgress = 0f;
                unit.AutoAttackBeforeResupply = unit.AutoAttackEnabled;
                unit.LockQuality = 0f;
                unit.Suppression = 0f;
                unit.AbilityCooldownRemaining = 0f;
                CancelAbilityChannel(unit, false);
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
                CancelFieldResupply(unit, false);
                CancelAbilityChannel(unit, false);
                ClearTarget(unit);
                unit.DeploymentState = DemoUnitDeploymentState.Returning;
                SetMovement(unit, BasePosition, DemoUnitActivity.Moving, true);
            }
            Raise($"{candidates.Count} witches returning to base.", BasePosition, true);
            return DemoCommandResult.Ok($"{candidates.Count} witches returning to base.");
        }

        public DemoCommandResult RequestSupplyDrop(Vector3 target)
        {
            if (Outcome != DemoOutcome.Running)
                return DemoCommandResult.Fail("Supply cannot be called after the mission has ended.");
            if (SupplyPackagesRemaining <= 0)
                return DemoCommandResult.Fail("No tactical supply packages remain.");
            if (SupplyCallCooldownRemaining > 0f)
                return DemoCommandResult.Fail($"Supply call cooling down ({SupplyCallCooldownRemaining:0.0}s).");

            Vector3 clampedTarget = ClampToMap(target);
            if (HorizontalDistance(BasePosition, clampedTarget) > Mathf.Max(0f, Balance.SupplyCallRange))
                return DemoCommandResult.Fail("Supply target is outside base delivery range.");

            DemoSupplyDropModel drop = new DemoSupplyDropModel(
                _nextSupplyDropId++, clampedTarget, Mathf.Max(0.1f, Balance.SupplyZoneRadius),
                Mathf.Max(0f, Balance.SupplyPackageCapacity), Mathf.Max(0f, Balance.SupplyDeliveryDelay),
                Mathf.Max(0f, Balance.SupplyZoneDuration));
            _supplyDrops.Add(drop);
            SupplyPackagesRemaining--;
            SupplyCallCooldownRemaining = Mathf.Max(0f, Balance.SupplyCallCooldown);
            Raise($"Supply drop #{drop.Id} inbound; arrival in {drop.InboundRemaining:0.0}s.", drop.Position, true);
            return DemoCommandResult.Ok($"Supply drop #{drop.Id} inbound.");
        }

        public DemoCommandResult RequestFieldResupply(IEnumerable<int> unitIds)
        {
            List<DemoSupplyDropModel> activeDrops = _supplyDrops
                .Where(drop => drop.IsActive)
                .OrderBy(drop => drop.Id)
                .ToList();
            if (activeDrops.Count == 0)
                return DemoCommandResult.Fail("No active tactical supply zone is available.");

            List<DemoUnitModel> candidates = (unitIds ?? Enumerable.Empty<int>())
                .Select(GetUnit)
                .Where(unit => unit != null && unit.Team == DemoTeam.Player && unit.IsAlive && !unit.IsFixed &&
                               unit.DeploymentState == DemoUnitDeploymentState.Active)
                .Distinct()
                .ToList();
            if (candidates.Count == 0)
                return DemoCommandResult.Fail("No selected witch can receive tactical supply.");

            foreach (DemoUnitModel unit in candidates)
            {
                DemoSupplyDropModel drop = activeDrops
                    .OrderBy(item => HorizontalDistance(unit.Position, item.Position))
                    .ThenBy(item => item.Id)
                    .First();
                BeginFieldResupply(unit, drop);
            }

            Raise($"{candidates.Count} witches are rendezvousing with tactical supply.",
                GetUnit(candidates[0].Id).Position, true);
            return DemoCommandResult.Ok($"{candidates.Count} witches assigned to tactical supply.");
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
            return unit == null ? 0f : unit.Stats.CoreDiscovery;
        }

        public float GetEffectiveCriticalChance(int unitId)
        {
            DemoUnitModel unit = GetUnit(unitId);
            if (unit == null)
                return 0f;
            return Mathf.Clamp01(unit.Stats.CriticalChance);
        }

        public float GetEffectiveAttackInterval(int unitId)
        {
            DemoUnitModel unit = GetUnit(unitId);
            if (unit == null)
                return 0f;
            float multiplier = 1f + Balance.FullSuppressionAttackIntervalPenalty * unit.SuppressionRatio;
            return Mathf.Max(0.2f, unit.Stats.AttackInterval * multiplier);
        }

        public float GetEffectiveShieldBonus(int targetUnitId)
        {
            return 0f;
        }

        public float GetEffectiveMoveSpeed(int unitId)
        {
            DemoUnitModel unit = GetUnit(unitId);
            if (unit == null)
                return 0f;
            float suppressionMultiplier = 1f - Balance.FullSuppressionSpeedPenalty * unit.SuppressionRatio;
            float abilityMultiplier = unit.Stats.SpecialAbility == DemoSpecialAbility.Heal && unit.IsChannelingAbility ? 0.5f : 1f;
            return Mathf.Max(0f, unit.Stats.MoveSpeed * suppressionMultiplier * abilityMultiplier);
        }

        public float GetEffectiveVisionRadius(int unitId)
        {
            DemoUnitModel unit = GetUnit(unitId);
            return unit == null ? 0f : Mathf.Max(0f,
                unit.Stats.VisionRadius * (1f - Balance.FullSuppressionVisionRangePenalty * unit.SuppressionRatio));
        }

        public float GetEffectiveAttackRange(int unitId)
        {
            DemoUnitModel unit = GetUnit(unitId);
            if (unit == null)
                return 0f;
            float baseRange = unit.IsFireControlReady
                ? Mathf.Max(unit.Stats.AttackRange, unit.Stats.PassiveAttackRange)
                : unit.Stats.AttackRange;
            return Mathf.Max(0.1f, baseRange *
                (1f - Balance.FullSuppressionVisionRangePenalty * unit.SuppressionRatio));
        }

        public DemoCommandResult RequestHover(IEnumerable<int> unitIds, bool enabled)
        {
            List<DemoUnitModel> candidates = (unitIds ?? Enumerable.Empty<int>())
                .Select(GetUnit)
                .Where(unit => unit != null && unit.Team == DemoTeam.Player && CanMove(unit))
                .Distinct()
                .ToList();
            if (candidates.Count == 0)
                return DemoCommandResult.Fail("No selected witch can change hover state.");

            foreach (DemoUnitModel unit in candidates)
            {
                if (!enabled && unit.IsResupplying)
                    CancelFieldResupply(unit, false);
                if (enabled)
                {
                    unit.HasDestination = false;
                    unit.HasManualMoveOrder = false;
                    unit.HasLoiter = false;
                    unit.SetLoiterWaypoints(Enumerable.Empty<Vector3>());
                    unit.FlightMode = unit.CurrentSpeed <= Balance.HoverStopSpeed
                        ? DemoFlightMode.Hovering
                        : DemoFlightMode.EnteringHover;
                    unit.HoverStableTime = 0f;
                    if (unit.Activity == DemoUnitActivity.Moving)
                        unit.Activity = DemoUnitActivity.Idle;
                }
                else if (unit.IsEnteringHover || unit.IsHovering)
                    BeginLoiter(unit, unit.Position);
            }
            return DemoCommandResult.Ok(enabled
                ? $"{candidates.Count} witches are decelerating to hover."
                : $"{candidates.Count} witches resumed loiter flight.");
        }

        public float GetMarkRemaining(int unitId)
        {
            DemoUnitModel unit = GetUnit(unitId);
            return unit == null ? 0f : Mathf.Max(0f, unit.MarkedUntil - SimulationTime);
        }

        public DemoCommandResult RequestSpecialAbility(int unitId, int targetId = -1)
        {
            DemoUnitModel caster = GetUnit(unitId);
            if (caster == null || caster.Team != DemoTeam.Player || !caster.IsAlive ||
                caster.DeploymentState != DemoUnitDeploymentState.Active)
                return DemoCommandResult.Fail("Selected witch cannot use an ability.");
            if (caster.Stats.SpecialAbility == DemoSpecialAbility.None)
                return DemoCommandResult.Fail("Selected witch has no active ability.");
            if (caster.IsResupplying)
                return DemoCommandResult.Fail("Tactical resupply must be cancelled before using an ability.");
            if (caster.AbilityCooldownRemaining > 0f)
                return DemoCommandResult.Fail($"Ability cooling down ({caster.AbilityCooldownRemaining:0.0}s).");
            if (caster.IsChannelingAbility)
                return DemoCommandResult.Fail("Ability is already being prepared.");

            switch (caster.Stats.SpecialAbility)
            {
                case DemoSpecialAbility.MagicEyeSearch:
                    return ActivateMagicEye(caster);
                case DemoSpecialAbility.Heal:
                    return BeginHealing(caster, targetId);
                case DemoSpecialAbility.FireControlSolution:
                    return BeginFireControlSolution(caster, targetId);
                case DemoSpecialAbility.LightningStrike:
                    return ActivateLightning(caster);
                default:
                    return DemoCommandResult.Fail("Ability is not implemented.");
            }
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
                if (unit.IsResupplying)
                {
                    unit.AutoAttackBeforeResupply = enabled;
                    unit.AutoAttackEnabled = false;
                    continue;
                }
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
                CancelFieldResupply(unit, false);
                if (unit.Stats.SpecialAbility == DemoSpecialAbility.Heal && unit.IsChannelingAbility)
                    CancelAbilityChannel(unit, true);
                bool changedTarget = unit.LockedTargetId != target.Id;
                unit.LockedTargetId = target.Id;
                unit.OrderedTargetId = target.Id;
                unit.HasExplicitAttackOrder = true;
                unit.TargetLastKnownPosition = target.Team == DemoTeam.Enemy ? target.PlayerVisiblePosition : target.Position;
                unit.HasTargetLastKnownPosition = true;
                if (changedTarget)
                    unit.LockQuality = 0f;
                unit.HasDestination = false;
                unit.HasManualMoveOrder = false;
                unit.HasLoiter = false;
                unit.FlightMode = DemoFlightMode.Normal;
                unit.HoverStableTime = 0f;
                unit.SetLoiterWaypoints(Enumerable.Empty<Vector3>());
                unit.Activity = DemoUnitActivity.Pursuing;
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
                Vector3 offset = candidates.Count == 1
                    ? Vector3.zero
                    : new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle)) * spacing;
                DemoUnitModel unit = candidates[i];
                CancelFieldResupply(unit, false);
                if (unit.Stats.SpecialAbility == DemoSpecialAbility.FireControlSolution && unit.IsChannelingAbility)
                    CancelAbilityChannel(unit, false);
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
                TickFieldSupply(dt);
                TickVisibility(dt);
                TickLockQuality(dt);
                TickAbilities(dt);
                TickEnemyAi(dt);
                TickAutomaticTargeting();
                TickTargetNavigation();
                TickMovement(dt);
                TickIndividualCombat(dt);
                TickProjectiles(dt);
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
                        unit.MagazineAmmo = Mathf.Max(1, unit.Stats.MagazineSize);
                        unit.ReserveAmmo = Mathf.Max(0, unit.Stats.ReserveAmmo);
                        unit.ReloadRemaining = 0f;
                        unit.LockQuality = 0f;
                        unit.Suppression = 0f;
                        unit.AbilityCooldownRemaining = 0f;
                        CancelAbilityChannel(unit, false);
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
                unit.AbilityCooldownRemaining = Mathf.Max(0f, unit.AbilityCooldownRemaining - dt);
                if (SimulationTime - unit.LastHitAt >= Balance.SuppressionRecoveryDelay)
                    unit.Suppression = Mathf.Max(0f, unit.Suppression - Balance.SuppressionRecoveryPerSecond * dt);

                if (unit.ReloadRemaining > 0f)
                {
                    unit.ReloadRemaining = Mathf.Max(0f, unit.ReloadRemaining - dt);
                    if (unit.ReloadRemaining <= 0f)
                    {
                        int needed = Mathf.Max(0, unit.Stats.MagazineSize - unit.MagazineAmmo);
                        int loaded = unit.Stats.UnlimitedReserveAmmo ? needed : Mathf.Min(needed, unit.ReserveAmmo);
                        unit.MagazineAmmo += loaded;
                        if (!unit.Stats.UnlimitedReserveAmmo)
                            unit.ReserveAmmo -= loaded;
                        Raise($"{unit.DisplayName} completed reload.", unit.Position, false);
                    }
                }
                if (unit.LockedTargetId < 0 && !unit.IsResupplying)
                {
                    unit.Magic = Mathf.Min(unit.Stats.MaxMagic, unit.Magic + unit.Stats.MagicRecovery * dt);
                    if (unit.Magic > unit.Stats.MaxMagic * 0.25f)
                        unit.Shield = Mathf.Min(unit.Stats.MaxShield, unit.Shield + unit.Stats.MagicRecovery * 0.45f * dt);
                }
            }
        }

        private void TickFieldSupply(float dt)
        {
            SupplyCallCooldownRemaining = Mathf.Max(0f, SupplyCallCooldownRemaining - dt);
            foreach (DemoSupplyDropModel drop in _supplyDrops.Where(item => !item.Finished).OrderBy(item => item.Id).ToList())
            {
                if (drop.InboundRemaining > 0f)
                {
                    drop.InboundRemaining = Mathf.Max(0f, drop.InboundRemaining - dt);
                    if (drop.InboundRemaining <= 0f)
                        Raise($"Supply drop #{drop.Id} is active.", drop.Position, true);
                    continue;
                }

                drop.ActiveRemaining = Mathf.Max(0f, drop.ActiveRemaining - dt);
                if (drop.ActiveRemaining <= 0f || drop.RemainingSupply <= 0.0001f)
                {
                    drop.Finished = true;
                    string reason = drop.RemainingSupply <= 0.0001f ? "depleted" : "expired";
                    Raise($"Supply drop #{drop.Id} {reason}.", drop.Position, true);
                }
            }

            foreach (DemoUnitModel unit in _units.Values
                         .Where(item => item.IsAlive && item.IsResupplying)
                         .OrderBy(item => item.Id)
                         .ToList())
            {
                DemoSupplyDropModel drop = GetSupplyDrop(unit.SupplyDropId);
                if (unit.Team != DemoTeam.Player || unit.DeploymentState != DemoUnitDeploymentState.Active ||
                    drop == null || !drop.IsActive)
                {
                    CancelFieldResupply(unit, true);
                    continue;
                }

                float approachRadius = drop.Radius * Mathf.Clamp(Balance.SupplyApproachRadiusRatio, 0.1f, 1f);
                if (HorizontalDistance(unit.Position, drop.Position) > approachRadius)
                {
                    if (!unit.HasDestination || HorizontalDistance(unit.Destination, drop.Position) > 0.05f)
                        SetMovement(unit, drop.Position, DemoUnitActivity.Resupplying, true);
                    unit.Activity = DemoUnitActivity.Resupplying;
                    continue;
                }

                if (!unit.IsHovering)
                {
                    unit.HasDestination = false;
                    unit.HasManualMoveOrder = false;
                    unit.HasLoiter = false;
                    unit.SetLoiterWaypoints(Enumerable.Empty<Vector3>());
                    unit.FlightMode = unit.CurrentSpeed <= Balance.HoverStopSpeed
                        ? DemoFlightMode.Hovering
                        : DemoFlightMode.EnteringHover;
                    unit.HoverStableTime = 0f;
                    unit.Activity = DemoUnitActivity.Resupplying;
                    continue;
                }

                unit.Activity = DemoUnitActivity.Resupplying;
                if (SimulationTime - unit.LastHitAt < Mathf.Max(0f, Balance.SupplyHitPauseDuration))
                    continue;

                bool complete = ReplenishFromSupply(unit, drop, dt);
                if (complete)
                {
                    Raise($"{unit.DisplayName} completed tactical resupply.", unit.Position, true);
                    CancelFieldResupply(unit, true);
                }
            }
        }

        private DemoSupplyDropModel GetSupplyDrop(int id)
        {
            return _supplyDrops.FirstOrDefault(drop => drop.Id == id);
        }

        private void BeginFieldResupply(DemoUnitModel unit, DemoSupplyDropModel drop)
        {
            if (unit == null || drop == null)
                return;
            if (!unit.IsResupplying)
                unit.AutoAttackBeforeResupply = unit.AutoAttackEnabled;
            CancelAbilityChannel(unit, false);
            ClearTarget(unit);
            unit.AutoAttackEnabled = false;
            unit.SupplyDropId = drop.Id;
            unit.SupplyAmmoProgress = 0f;
            SetMovement(unit, drop.Position, DemoUnitActivity.Resupplying, true);
            unit.Activity = DemoUnitActivity.Resupplying;
        }

        private void CancelFieldResupply(DemoUnitModel unit, bool resumeLoiter)
        {
            if (unit == null || !unit.IsResupplying)
                return;
            unit.SupplyDropId = -1;
            unit.SupplyAmmoProgress = 0f;
            unit.AutoAttackEnabled = unit.AutoAttackBeforeResupply;
            if (resumeLoiter && unit.IsAlive && unit.DeploymentState == DemoUnitDeploymentState.Active)
                BeginLoiter(unit, unit.Position);
        }

        private bool ReplenishFromSupply(DemoUnitModel unit, DemoSupplyDropModel drop, float dt)
        {
            int maximumTotalAmmo = Mathf.Max(1, unit.Stats.MagazineSize) + Mathf.Max(0, unit.Stats.ReserveAmmo);
            int missingAmmo = Mathf.Max(0, maximumTotalAmmo - unit.MagazineAmmo - unit.ReserveAmmo);
            if (missingAmmo > 0 && drop.RemainingSupply > 0f)
            {
                unit.SupplyAmmoProgress += Mathf.Max(0f, Balance.SupplyAmmoPerSecond) * dt;
                int availableRounds = Mathf.FloorToInt(unit.SupplyAmmoProgress + 0.0001f);
                int capacityRounds = Balance.SupplyAmmoCost <= 0f
                    ? availableRounds
                    : Mathf.FloorToInt(drop.RemainingSupply / Balance.SupplyAmmoCost + 0.0001f);
                int transferred = Mathf.Min(missingAmmo, availableRounds, capacityRounds);
                if (transferred > 0)
                {
                    unit.ReserveAmmo += transferred;
                    unit.SupplyAmmoProgress -= transferred;
                    drop.RemainingSupply = Mathf.Max(0f,
                        drop.RemainingSupply - transferred * Mathf.Max(0f, Balance.SupplyAmmoCost));
                    if (unit.MagazineAmmo < Mathf.Max(1, unit.Stats.AmmoPerAttack) && unit.ReloadRemaining <= 0f)
                        StartReload(unit);
                }
                return false;
            }

            unit.SupplyAmmoProgress = 0f;
            if (unit.Magic < unit.Stats.MaxMagic - 0.0001f && drop.RemainingSupply > 0f)
            {
                float desired = Mathf.Min(unit.Stats.MaxMagic - unit.Magic,
                    Mathf.Max(0f, Balance.SupplyMagicPerSecond) * dt);
                float cost = Mathf.Max(0f, Balance.SupplyMagicCost);
                float transferred = cost <= 0f ? desired : Mathf.Min(desired, drop.RemainingSupply / cost);
                unit.Magic += transferred;
                drop.RemainingSupply = Mathf.Max(0f, drop.RemainingSupply - transferred * cost);
                return false;
            }

            if (unit.Shield < unit.Stats.MaxShield - 0.0001f && drop.RemainingSupply > 0f)
            {
                float desired = Mathf.Min(unit.Stats.MaxShield - unit.Shield,
                    Mathf.Max(0f, Balance.SupplyShieldPerSecond) * dt);
                float cost = Mathf.Max(0f, Balance.SupplyShieldCost);
                float transferred = cost <= 0f ? desired : Mathf.Min(desired, drop.RemainingSupply / cost);
                unit.Shield += transferred;
                drop.RemainingSupply = Mathf.Max(0f, drop.RemainingSupply - transferred * cost);
                return false;
            }

            return missingAmmo <= 0 && unit.Magic >= unit.Stats.MaxMagic - 0.0001f &&
                   unit.Shield >= unit.Stats.MaxShield - 0.0001f;
        }

        private void TickMovement(float dt)
        {
            foreach (DemoUnitModel unit in _units.Values.Where(unit => unit.IsAlive && unit.IsOperational && !unit.IsFixed))
            {
                float acceleration = Mathf.Max(0.01f,
                    unit.Stats.MoveSpeed / Mathf.Max(0.1f, Balance.AccelerationDuration));
                if (unit.FlightMode == DemoFlightMode.EnteringHover)
                {
                    unit.TurnRatio = 0f;
                    unit.CurrentSpeed = Mathf.MoveTowards(unit.CurrentSpeed, 0f, acceleration * dt);
                    unit.CurrentVelocity = unit.Facing.normalized * unit.CurrentSpeed;
                    unit.Position = ClampToMap(unit.Position + unit.CurrentVelocity * dt);
                    unit.HoverStableTime = 0f;
                    if (unit.CurrentSpeed <= Balance.HoverStopSpeed)
                    {
                        unit.CurrentSpeed = 0f;
                        unit.CurrentVelocity = Vector3.zero;
                        unit.FlightMode = DemoFlightMode.Hovering;
                    }
                    continue;
                }
                if (unit.FlightMode == DemoFlightMode.Hovering)
                {
                    unit.CurrentSpeed = 0f;
                    unit.CurrentVelocity = Vector3.zero;
                    unit.TurnRatio = 0f;
                    unit.HoverStableTime += dt;
                    continue;
                }

                Vector3 desiredDirection = Vector3.zero;
                float distanceToDestination = float.PositiveInfinity;
                if (unit.HasDestination)
                {
                    Vector3 toDestination = unit.Destination - unit.Position;
                    toDestination.y = 0f;
                    distanceToDestination = toDestination.magnitude;
                    if (distanceToDestination > 0.001f)
                        desiredDirection = toDestination / distanceToDestination;
                    unit.StableLoiterTime = 0f;
                }
                else if (unit.FlightMode == DemoFlightMode.Loiter && unit.LoiterWaypoints.Count > 0)
                {
                    int index = Mathf.Clamp(unit.LoiterWaypointIndex, 0, unit.LoiterWaypoints.Count - 1);
                    Vector3 toWaypoint = unit.LoiterWaypoints[index] - unit.Position;
                    toWaypoint.y = 0f;
                    distanceToDestination = toWaypoint.magnitude;
                    if (distanceToDestination <= GetDestinationArrivalRadius(unit))
                    {
                        unit.LoiterWaypointIndex = (index + 1) % unit.LoiterWaypoints.Count;
                        toWaypoint = unit.LoiterWaypoints[unit.LoiterWaypointIndex] - unit.Position;
                        toWaypoint.y = 0f;
                        distanceToDestination = toWaypoint.magnitude;
                    }
                    if (distanceToDestination > 0.001f)
                        desiredDirection = toWaypoint / distanceToDestination;
                    unit.StableLoiterTime += dt;
                }

                float effectiveMaximumSpeed = GetEffectiveMoveSpeed(unit.Id);
                if (desiredDirection.sqrMagnitude > 0.001f)
                {
                    Vector3 facing = unit.Facing.sqrMagnitude > 0.001f ? unit.Facing.normalized : desiredDirection;
                    float headingDelta = Vector3.Angle(facing, desiredDirection);
                    unit.TurnRatio = Mathf.Clamp01(headingDelta / 180f);
                    float turnRate = 60f * Mathf.Max(0f, unit.Stats.Mobility);
                    unit.Facing = Vector3.RotateTowards(facing, desiredDirection, turnRate * Mathf.Deg2Rad * dt, 0f).normalized;
                    effectiveMaximumSpeed *= Mathf.Lerp(1f, Balance.TurnSpeedCapAt180, unit.TurnRatio);
                }
                else
                    unit.TurnRatio = 0f;

                float targetSpeed = desiredDirection.sqrMagnitude > 0.001f ? effectiveMaximumSpeed : 0f;
                if (unit.HasDestination && unit.FlightMode != DemoFlightMode.Loiter && !float.IsInfinity(distanceToDestination))
                    targetSpeed = Mathf.Min(targetSpeed, Mathf.Max(0.5f, distanceToDestination * 0.75f));
                unit.CurrentSpeed = Mathf.MoveTowards(unit.CurrentSpeed, targetSpeed, acceleration * dt);
                unit.CurrentVelocity = unit.Facing * unit.CurrentSpeed;
                Vector3 movementEnd = ClampToMap(unit.Position + unit.CurrentVelocity * dt);

                float arrivalRadius = unit.DeploymentState == DemoUnitDeploymentState.Returning
                    ? Mathf.Max(0.05f, Balance.BaseArrivalRadius)
                    : GetDestinationArrivalRadius(unit);
                if (unit.HasDestination && ReachesPoint(unit.Position, movementEnd, unit.Destination, arrivalRadius))
                {
                    Vector3 reachedDestination = unit.Destination;
                    if (HorizontalDistance(unit.Position, reachedDestination) > arrivalRadius)
                        unit.Position = movementEnd;
                    unit.HasDestination = false;
                    unit.HasManualMoveOrder = false;
                    if (unit.DeploymentState == DemoUnitDeploymentState.Returning)
                    {
                        unit.Position = BasePosition;
                        unit.HasLoiter = false;
                        unit.FlightMode = DemoFlightMode.Normal;
                        unit.CurrentSpeed = 0f;
                        unit.CurrentVelocity = Vector3.zero;
                        unit.Activity = DemoUnitActivity.Idle;
                        unit.DeploymentState = DemoUnitDeploymentState.Servicing;
                        unit.TurnaroundRemaining = Mathf.Max(0f, Balance.BaseTurnaroundDuration);
                        Raise($"{unit.DisplayName} landed and entered service.", BasePosition, true);
                        continue;
                    }
                    BeginLoiter(unit, reachedDestination);
                    if (unit.Activity == DemoUnitActivity.Moving)
                        unit.Activity = DemoUnitActivity.Idle;
                }
                else if (desiredDirection.sqrMagnitude > 0.001f)
                {
                    Vector3 movementStart = unit.Position;
                    unit.Position = movementEnd;
                    if (!unit.HasDestination && unit.FlightMode == DemoFlightMode.Loiter &&
                        unit.LoiterWaypoints.Count > 0)
                    {
                        int index = Mathf.Clamp(unit.LoiterWaypointIndex, 0, unit.LoiterWaypoints.Count - 1);
                        if (ReachesPoint(movementStart, movementEnd, unit.LoiterWaypoints[index],
                                GetDestinationArrivalRadius(unit)))
                            unit.LoiterWaypointIndex = (index + 1) % unit.LoiterWaypoints.Count;
                    }
                }

                if (unit.DeploymentState == DemoUnitDeploymentState.Returning &&
                    HorizontalDistance(unit.Position, BasePosition) <= Balance.BaseArrivalRadius)
                {
                    unit.Position = BasePosition;
                    unit.Destination = BasePosition;
                    unit.HasDestination = false;
                    unit.HasManualMoveOrder = false;
                    unit.HasLoiter = false;
                    unit.FlightMode = DemoFlightMode.Normal;
                    unit.CurrentSpeed = 0f;
                    unit.CurrentVelocity = Vector3.zero;
                    unit.Activity = DemoUnitActivity.Idle;
                    unit.DeploymentState = DemoUnitDeploymentState.Servicing;
                    unit.TurnaroundRemaining = Mathf.Max(0f, Balance.BaseTurnaroundDuration);
                    Raise($"{unit.DisplayName} landed and entered service.", BasePosition, true);
                    continue;
                }

            }
        }

        private float GetDestinationArrivalRadius(DemoUnitModel unit)
        {
            float radius = unit != null && unit.Team == DemoTeam.Enemy
                ? Balance.EnemyAiArrivalRadius
                : Balance.DestinationRadius;
            return Mathf.Max(0.05f, radius);
        }

        private static bool ReachesPoint(Vector3 movementStart, Vector3 movementEnd, Vector3 target, float radius)
        {
            movementStart.y = 0f;
            movementEnd.y = 0f;
            target.y = 0f;
            float radiusSquared = Mathf.Max(0.05f, radius);
            radiusSquared *= radiusSquared;
            if ((movementStart - target).sqrMagnitude <= radiusSquared ||
                (movementEnd - target).sqrMagnitude <= radiusSquared)
                return true;

            Vector3 movement = movementEnd - movementStart;
            float movementSquared = movement.sqrMagnitude;
            if (movementSquared <= 0.000001f)
                return false;
            float progress = Mathf.Clamp01(Vector3.Dot(target - movementStart, movement) / movementSquared);
            Vector3 closestPoint = movementStart + movement * progress;
            return (closestPoint - target).sqrMagnitude <= radiusSquared;
        }

        private void TickVisibility(float dt)
        {
            List<DemoUnitModel> activeWitches = _units.Values
                .Where(unit => unit.IsAlive && unit.IsOperational && unit.Team == DemoTeam.Player &&
                               unit.Stats.WitchVisionType != DemoWitchVisionType.None)
                .ToList();
            foreach (DemoUnitModel enemy in _units.Values.Where(unit => unit.IsAlive && unit.Team == DemoTeam.Enemy))
            {
                enemy.IsCurrentlyObservedByPlayer = false;
                DemoUnitModel observer = activeWitches.FirstOrDefault(unit => IsInsideWitchVision(unit, enemy.Position));
                bool insideAttackRange = activeWitches.Any(unit =>
                    HorizontalDistance(unit.Position, enemy.Position) <= GetEffectiveAttackRange(unit.Id));
                if (insideAttackRange)
                    ForceIdentifyEnemy(enemy);
                if (observer != null)
                    ObserveEnemy(enemy, dt);
                else if (!insideAttackRange)
                    DecayEnemyIntel(enemy, dt);
            }
        }

        private void ForceIdentifyEnemy(DemoUnitModel enemy)
        {
            DemoIntelLevel previous = enemy.PlayerIntelLevel;
            enemy.IsCurrentlyObservedByPlayer = true;
            enemy.IsRevealedToPlayer = true;
            enemy.LastKnownPosition = enemy.Position;
            enemy.LastObservedAt = SimulationTime;
            if (enemy.PlayerIntelLevel < DemoIntelLevel.Identified)
                enemy.PlayerIntelLevel = DemoIntelLevel.Identified;
            enemy.IdentificationProgress = 1f;
            if (previous < DemoIntelLevel.Identified)
                Raise($"Enemy force-identified: {enemy.DisplayName}.", enemy.Position, true);
        }

        private bool IsInsideWitchVision(DemoUnitModel observer, Vector3 targetPosition)
        {
            Vector3 toTarget = targetPosition - observer.Position;
            toTarget.y = 0f;
            float visionRadius = GetEffectiveVisionRadius(observer.Id);
            if (toTarget.sqrMagnitude > visionRadius * visionRadius)
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

        private void TickLockQuality(float dt)
        {
            foreach (DemoUnitModel attacker in _units.Values.Where(unit => unit.IsAlive && unit.IsOperational && unit.LockedTargetId >= 0).ToList())
            {
                DemoUnitModel target = GetUnit(attacker.LockedTargetId);
                if (target == null || !target.IsAlive || !target.IsOperational || target.Team == attacker.Team)
                {
                    ClearTarget(attacker);
                    continue;
                }

                float maximumRange = GetEffectiveAttackRange(attacker, target);
                if (!IsTargetVisibleToAttacker(attacker, target) ||
                    HorizontalDistance(attacker.Position, target.Position) > maximumRange)
                {
                    attacker.LockQuality = Mathf.Max(0f, attacker.LockQuality - Balance.LockDecayPerSecond * dt);
                    continue;
                }

                float intelFactor = attacker.Team == DemoTeam.Player && target.PlayerIntelLevel >= DemoIntelLevel.Assessed ? 1.25f : 1f;
                float turnFactor = 1f - 0.5f * attacker.TurnRatio;
                float suppressionFactor = 1f - Balance.FullSuppressionLockPenalty * attacker.SuppressionRatio;
                float growth = Balance.LockBaseGrowthPerSecond * intelFactor * turnFactor * suppressionFactor;
                attacker.LockQuality = Mathf.Min(100f, attacker.LockQuality + growth * dt);
                if (attacker.LockQuality >= Balance.LockFireThreshold - 0.001f)
                    attacker.LockQuality = Mathf.Max(Balance.LockFireThreshold, attacker.LockQuality);
            }
        }

        private void TickAbilities(float dt)
        {
            foreach (DemoUnitModel caster in _units.Values.Where(unit => unit.IsAlive && unit.IsOperational && unit.IsChannelingAbility).ToList())
            {
                switch (caster.Stats.SpecialAbility)
                {
                    case DemoSpecialAbility.Heal:
                    {
                        DemoUnitModel target = GetUnit(caster.AbilityTargetId);
                        float magicCost = Mathf.Max(0f, caster.Stats.AbilityMagicCost) * dt;
                        if (target == null || !target.IsAlive || target.Team != caster.Team || target.Id == caster.Id ||
                            HorizontalDistance(caster.Position, target.Position) > caster.Stats.AbilityRange || caster.Magic + 0.0001f < magicCost)
                        {
                            CancelAbilityChannel(caster, true);
                            Raise($"{caster.DisplayName}'s healing was interrupted.", caster.Position, false);
                            break;
                        }
                        caster.Magic = Mathf.Max(0f, caster.Magic - magicCost);
                        target.Health = Mathf.Min(target.Stats.MaxHealth,
                            target.Health + target.Stats.MaxHealth * Mathf.Max(0f, caster.Stats.AbilityValue) * dt);
                        caster.AbilityChannelRemaining = Mathf.Max(0f, caster.AbilityChannelRemaining - dt);
                        if (caster.AbilityChannelRemaining <= 0f)
                        {
                            CancelAbilityChannel(caster, true);
                            Raise($"{caster.DisplayName} completed healing {target.DisplayName}.", target.Position, true);
                        }
                        break;
                    }
                    case DemoSpecialAbility.FireControlSolution:
                    {
                        if (caster.HasDestination || !caster.HasLoiter)
                        {
                            CancelAbilityChannel(caster, false);
                            Raise($"{caster.DisplayName}'s fire-control solution was cancelled by movement.", caster.Position, false);
                            break;
                        }
                        caster.AbilityChannelRemaining = Mathf.Max(0f, caster.AbilityChannelRemaining - dt);
                        if (caster.AbilityChannelRemaining <= 0f)
                            ExecuteFireControlShot(caster);
                        break;
                    }
                }
            }
        }

        private DemoCommandResult ActivateMagicEye(DemoUnitModel caster)
        {
            if (caster.Magic < caster.Stats.AbilityMagicCost)
                return DemoCommandResult.Fail("Not enough magic for Magic Eye Search.");
            caster.Magic -= caster.Stats.AbilityMagicCost;
            caster.AbilityCooldownRemaining = caster.Stats.AbilityCooldown;
            float range = Mathf.Max(0f, caster.Stats.AbilityRange);
            float arc = Mathf.Clamp(caster.Stats.AbilityArcAngle, 1f, 360f);
            int count = 0;
            foreach (DemoUnitModel enemy in _units.Values.Where(unit => unit.IsAlive && unit.Team != caster.Team))
            {
                Vector3 toEnemy = enemy.Position - caster.Position;
                toEnemy.y = 0f;
                if (toEnemy.magnitude > range || (toEnemy.sqrMagnitude > 0.001f && Vector3.Angle(caster.Facing, toEnemy) > arc * 0.5f))
                    continue;
                enemy.IsRevealedToPlayer = true;
                enemy.IsCurrentlyObservedByPlayer = true;
                enemy.PlayerIntelLevel = DemoIntelLevel.Assessed;
                enemy.IdentificationProgress = 1f;
                enemy.AssessmentProgress = 1f;
                enemy.LastKnownPosition = enemy.Position;
                enemy.LastObservedAt = SimulationTime;
                enemy.MarkedUntil = Mathf.Max(enemy.MarkedUntil, SimulationTime + caster.Stats.AbilityDuration);
                count++;
            }
            Raise($"{caster.DisplayName} assessed {count} enemies with Magic Eye Search.", caster.Position, true);
            return DemoCommandResult.Ok($"Magic Eye Search assessed {count} enemies.");
        }

        private DemoCommandResult BeginHealing(DemoUnitModel caster, int targetId)
        {
            DemoUnitModel target = GetUnit(targetId);
            if (target == null || !target.IsAlive || !target.IsOperational || target.Team != caster.Team || target.Id == caster.Id)
                return DemoCommandResult.Fail("Healing requires another active friendly witch.");
            if (HorizontalDistance(caster.Position, target.Position) > caster.Stats.AbilityRange)
                return DemoCommandResult.Fail("Healing target is out of range.");
            if (caster.Magic <= 0f)
                return DemoCommandResult.Fail("Not enough magic to begin healing.");
            caster.AbilityTargetId = target.Id;
            caster.AbilityChannelRemaining = Mathf.Max(0.1f, caster.Stats.AbilityDuration);
            Raise($"{caster.DisplayName} began healing {target.DisplayName}.", caster.Position, false);
            return DemoCommandResult.Ok("Healing channel started.");
        }

        private DemoCommandResult BeginFireControlSolution(DemoUnitModel caster, int targetId)
        {
            DemoUnitModel target = GetUnit(targetId);
            if (target == null || !target.IsAlive || !target.IsOperational || target.Team == caster.Team)
                return DemoCommandResult.Fail("Fire-control solution requires an enemy target.");
            if (target.PlayerIntelLevel < DemoIntelLevel.Assessed && GetMarkRemaining(target.Id) <= 0f)
                return DemoCommandResult.Fail("Target must be assessed or core-marked.");
            if (HorizontalDistance(caster.Position, target.Position) > caster.Stats.AbilityRange)
                return DemoCommandResult.Fail("Target is outside fire-control range.");
            if (!CanFireWeapon(caster))
                return DemoCommandResult.Fail("Weapon is reloading or out of ammunition.");
            if (caster.HasDestination)
                return DemoCommandResult.Fail("Stable loiter is required before binding fire-control data.");
            if (!caster.HasLoiter)
                BeginLoiter(caster, caster.Position);
            caster.AbilityTargetId = target.Id;
            caster.AbilityChannelRemaining = Mathf.Max(0.1f, caster.Stats.AbilityDuration);
            caster.StableLoiterTime = 0f;
            Raise($"{caster.DisplayName} began binding fire-control data on {target.DisplayName}.", caster.Position, false);
            return DemoCommandResult.Ok("Fire-control solution started; maintain loiter.");
        }

        private DemoCommandResult ActivateLightning(DemoUnitModel caster)
        {
            if (caster.Magic < caster.Stats.AbilityMagicCost)
                return DemoCommandResult.Fail("Not enough magic for Lightning Strike.");
            caster.Magic -= caster.Stats.AbilityMagicCost;
            caster.AbilityCooldownRemaining = caster.Stats.AbilityCooldown;
            List<DemoUnitModel> targets = _units.Values.Where(unit => unit.IsAlive && unit.IsOperational && unit.Team != caster.Team &&
                HorizontalDistance(unit.Position, caster.Position) <= caster.Stats.AbilityRadius).ToList();
            foreach (DemoUnitModel target in targets)
            {
                DemoDamageResult result = DemoDamageResolver.Resolve(caster, target, Balance, _random, 0f,
                    caster.Stats.AbilityDamageMultiplier, 1f, 1f, 0f, 1f, 0f,
                    caster.Stats.AbilityPenetration > 0f ? caster.Stats.AbilityPenetration : caster.Stats.Penetration,
                    target.MarkedUntil > SimulationTime, 0.5f);
                ApplySuppression(target, result, false, caster.Stats.AbilitySuppression);
                ReportDamage(caster, target, result, target.Position, false);
                if (result.Destroyed)
                    ClearDestroyedTarget(target.Id);
            }
            Raise($"{caster.DisplayName} struck {targets.Count} enemies with lightning.", caster.Position, true);
            return DemoCommandResult.Ok($"Lightning struck {targets.Count} enemies.");
        }

        private void ExecuteFireControlShot(DemoUnitModel caster)
        {
            DemoUnitModel target = GetUnit(caster.AbilityTargetId);
            if (target == null || !target.IsAlive || !target.IsOperational || target.Team == caster.Team ||
                HorizontalDistance(caster.Position, target.Position) > caster.Stats.AbilityRange || !CanFireWeapon(caster))
            {
                CancelAbilityChannel(caster, true);
                Raise($"{caster.DisplayName}'s fire-control shot was aborted.", caster.Position, false);
                return;
            }
            float hitChance = Mathf.Max(caster.Stats.AbilityMinimumAccuracy, CalculateHitChance(caster, target));
            float evasion = CalculateEvasionChance(target);
            DemoDamageResult result = DemoDamageResolver.Resolve(caster, target, Balance, _random, 0f,
                caster.Stats.AbilityDamageMultiplier, 1f, 1f, 0f, hitChance, evasion,
                caster.Stats.AbilityPenetration, target.MarkedUntil > SimulationTime);
            ConsumeAmmo(caster);
            caster.AttacksPerformed++;
            caster.AttackCooldown = GetEffectiveAttackInterval(caster.Id);
            ApplySuppression(target, result, false);
            ReportDamage(caster, target, result, target.Position, false);
            CancelAbilityChannel(caster, true);
            Raise($"{caster.DisplayName} fired a bound anti-armour shot.", caster.Position, true);
            if (result.Destroyed)
                ClearDestroyedTarget(target.Id);
        }

        private void CancelAbilityChannel(DemoUnitModel unit, bool startCooldown)
        {
            if (unit == null)
                return;
            unit.AbilityChannelRemaining = 0f;
            unit.AbilityTargetId = -1;
            if (startCooldown)
                unit.AbilityCooldownRemaining = Mathf.Max(unit.AbilityCooldownRemaining, unit.Stats.AbilityCooldown);
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
            float visionRadius = GetEffectiveVisionRadius(observer.Id);
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
                    if (unit.IsResupplying || !unit.AutoAttackEnabled || unit.HasExplicitAttackOrder || unit.LockedTargetId >= 0 ||
                        unit.DeploymentState != DemoUnitDeploymentState.Active)
                        continue;
                    DemoUnitModel target = _units.Values
                        .Where(candidate => candidate.IsAlive && candidate.IsOperational && candidate.Team == DemoTeam.Enemy &&
                                            candidate.IsCurrentlyObservedByPlayer && candidate.PlayerIntelLevel >= DemoIntelLevel.Identified &&
                                            HorizontalDistance(unit.Position, candidate.Position) <= GetEffectiveAttackRange(unit, candidate))
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
                                            HorizontalDistance(unit.Position, candidate.Position) <= GetEffectiveAttackRange(unit, candidate))
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
                    unit.Activity = DemoUnitActivity.Moving;
                    continue;
                }
                if ((unit.IsHovering || unit.IsEnteringHover) && !unit.HasExplicitAttackOrder)
                {
                    if (targetVisible)
                    {
                        unit.TargetLastKnownPosition = target.Position;
                        unit.HasTargetLastKnownPosition = true;
                        unit.Activity = HorizontalDistance(unit.Position, target.Position) <= GetEffectiveAttackRange(unit, target)
                            ? DemoUnitActivity.Attacking
                            : DemoUnitActivity.Idle;
                    }
                    continue;
                }
                if (unit.Team == DemoTeam.Player && !unit.HasExplicitAttackOrder &&
                    ((!targetVisible && unit.LockQuality <= 0f) ||
                     HorizontalDistance(unit.Position, target.Position) > GetEffectiveAttackRange(unit, target)))
                {
                    ClearTarget(unit);
                    continue;
                }

                if (targetVisible)
                {
                    unit.TargetLastKnownPosition = target.Position;
                    unit.HasTargetLastKnownPosition = true;
                    float distance = HorizontalDistance(unit.Position, target.Position);
                    float attackRange = GetEffectiveAttackRange(unit, target);
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
                    HorizontalDistance(attacker.Position, target.Position) > GetEffectiveAttackRange(attacker, target) ||
                    attacker.LockQuality < Balance.LockFireThreshold)
                    continue;
                if (attacker.AttackCooldown > 0f || attacker.IsChannelingAbility || !CanFireWeapon(attacker))
                    continue;

                int nextAttack = attacker.AttacksPerformed + 1;
                bool fortressBarrage = attacker.Role == DemoUnitRole.Fortress &&
                                       attacker.HealthRatio <= Balance.FortressBarrageHealthThreshold;
                float attackMultiplier = 1f;
                if (fortressBarrage)
                {
                    attackMultiplier *= Balance.FortressBarrageDamageMultiplier;
                    if (!attacker.FortressBarrageAnnounced)
                    {
                        attacker.FortressBarrageAnnounced = true;
                        Raise($"{attacker.DisplayName} entered emergency barrage mode.", attacker.Position, true);
                    }
                }

                if (attacker.Stats.ExplosiveRadius > 0f)
                {
                    LaunchProjectile(attacker, target);
                }
                else
                {
                    bool fireControl = CanUseFireControl(attacker, target);
                    float hitChance = CalculateHitChance(attacker, target);
                    if (fireControl)
                        hitChance = Mathf.Max(attacker.Stats.PassiveMinimumAccuracy, hitChance);
                    float evasionChance = CalculateEvasionChance(target);
                    DemoDamageResult result = DemoDamageResolver.Resolve(attacker, target, Balance, _random, 0f,
                        attackMultiplier * (fireControl ? attacker.Stats.PassiveDamageMultiplier : 1f),
                        1f, 1f, 0f, hitChance, evasionChance,
                        fireControl ? attacker.Stats.PassivePenetration : -1f,
                        target.MarkedUntil > SimulationTime);
                    ApplySuppression(target, result, false);
                    ReportDamage(attacker, target, result, target.Position, false);
                    if (result.Destroyed)
                        ClearDestroyedTarget(target.Id);
                }
                ConsumeAmmo(attacker);
                attacker.AttacksPerformed = nextAttack;
                float intervalMultiplier = fortressBarrage ? Balance.FortressBarrageIntervalMultiplier : 1f;
                attacker.AttackCooldown = Mathf.Max(0.2f, GetEffectiveAttackInterval(attacker.Id) * intervalMultiplier);
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

        private void LaunchProjectile(DemoUnitModel attacker, DemoUnitModel target)
        {
            Vector3 direction = target.Position - attacker.Position;
            direction.y = 0f;
            if (direction.sqrMagnitude < 0.001f)
                direction = attacker.Facing;
            DemoProjectileModel projectile = new DemoProjectileModel(
                _nextProjectileId++, attacker.Id, target.Id, attacker.Position, direction,
                Mathf.Max(0.1f, attacker.Stats.ProjectileSpeed),
                Mathf.Max(0f, attacker.Stats.ProjectileTurnRate),
                Mathf.Max(0.1f, attacker.Stats.ProjectileLifetime),
                Mathf.Max(0.05f, attacker.Stats.ProjectileContactRadius),
                Mathf.Max(0f, attacker.Stats.ExplosiveRadius),
                CalculateHitChance(attacker, target));
            _projectiles.Add(projectile);
            attacker.LastAimPoint = target.Position;
            Raise($"{attacker.DisplayName} launched a tracking rocket at {target.DisplayName}.", attacker.Position, false);
        }

        private void TickProjectiles(float dt)
        {
            foreach (DemoProjectileModel projectile in _projectiles.Where(item => !item.Resolved).ToList())
            {
                projectile.RemainingLifetime -= dt;
                DemoUnitModel target = GetUnit(projectile.TargetId);
                if (projectile.RemainingLifetime <= 0f || target == null || !target.IsAlive || !target.IsOperational)
                {
                    projectile.Resolved = true;
                    continue;
                }

                Vector3 desired = target.Position - projectile.Position;
                desired.y = 0f;
                if (desired.sqrMagnitude > 0.001f)
                    projectile.Facing = Vector3.RotateTowards(projectile.Facing, desired.normalized,
                        projectile.TurnRate * Mathf.Deg2Rad * dt, 0f).normalized;
                Vector3 previous = projectile.Position;
                projectile.Position = ClampToMap(projectile.Position + projectile.Facing * projectile.Speed * dt);
                if (DistanceToSegment(target.Position, previous, projectile.Position) > projectile.ContactRadius)
                    continue;

                projectile.Resolved = true;
                ResolveProjectileImpact(projectile);
            }
        }

        private void ResolveProjectileImpact(DemoProjectileModel projectile)
        {
            DemoUnitModel attacker = GetUnit(projectile.AttackerId);
            if (attacker == null)
                return;
            List<DemoUnitModel> impacted = _units.Values
                .Where(unit => unit.IsAlive && unit.IsOperational && unit.Team != attacker.Team &&
                               HorizontalDistance(unit.Position, projectile.Position) <= projectile.ExplosionRadius)
                .ToList();
            foreach (DemoUnitModel unit in impacted)
            {
                DemoDamageResult result = DemoDamageResolver.Resolve(attacker, unit, Balance, _random, 0f,
                    1f, 1f, 1f, 0f, projectile.LaunchHitChance, CalculateEvasionChance(unit), -1f,
                    unit.MarkedUntil > SimulationTime);
                ApplySuppression(unit, result, true);
                ReportDamage(attacker, unit, result, projectile.Position, false);
                if (result.Destroyed)
                    ClearDestroyedTarget(unit.Id);
            }
            Raise($"Tracking rocket impacted near {targetName(projectile.TargetId)}; {impacted.Count} targets in blast.",
                projectile.Position, impacted.Count > 0);
        }

        private string targetName(int targetId)
        {
            DemoUnitModel target = GetUnit(targetId);
            return target == null ? "lost target" : target.DisplayName;
        }

        private float GetCoreDiscoveryMultiplier(DemoUnitModel attacker)
        {
            return 1f;
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
            return HorizontalDistance(attacker.Position, target.Position) <= GetEffectiveVisionRadius(attacker.Id);
        }

        private void AssignAutomaticTarget(DemoUnitModel unit, DemoUnitModel target)
        {
            bool changedTarget = unit.LockedTargetId != target.Id;
            unit.LockedTargetId = target.Id;
            unit.OrderedTargetId = -1;
            unit.HasExplicitAttackOrder = false;
            unit.TargetLastKnownPosition = target.Position;
            unit.HasTargetLastKnownPosition = true;
            if (changedTarget)
                unit.LockQuality = 0f;
            if (!unit.HasManualMoveOrder)
                unit.Activity = DemoUnitActivity.Pursuing;
        }

        private void AssignEnemyTarget(DemoUnitModel enemy, DemoUnitModel target)
        {
            bool changedTarget = enemy.LockedTargetId != target.Id;
            enemy.LockedTargetId = target.Id;
            enemy.OrderedTargetId = -1;
            enemy.HasExplicitAttackOrder = false;
            enemy.TargetLastKnownPosition = target.Position;
            enemy.HasTargetLastKnownPosition = true;
            enemy.EnemyAiTargetId = target.Id;
            enemy.EnemyAiLastKnownPosition = target.Position;
            enemy.EnemyAiHasLastKnownPosition = true;
            if (changedTarget)
                enemy.LockQuality = 0f;
        }

        private void ClearTarget(DemoUnitModel unit)
        {
            if (unit == null)
                return;
            unit.LockedTargetId = -1;
            unit.LockQuality = 0f;
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
            unit.HasLoiter = false;
            unit.FlightMode = DemoFlightMode.Normal;
            unit.HoverStableTime = 0f;
            unit.SetLoiterWaypoints(Enumerable.Empty<Vector3>());
            unit.StableLoiterTime = 0f;
            unit.Activity = activity;
        }

        private void StopMovement(DemoUnitModel unit)
        {
            if (unit == null)
                return;
            unit.Destination = unit.Position;
            unit.HasDestination = false;
            unit.HasManualMoveOrder = false;
            if (!unit.HasLoiter && !unit.IsHovering && !unit.IsEnteringHover)
                BeginLoiter(unit, unit.Position);
            if (unit.Activity == DemoUnitActivity.Moving || unit.Activity == DemoUnitActivity.Pursuing)
                unit.Activity = DemoUnitActivity.Idle;
        }

        private void BeginLoiter(DemoUnitModel unit, Vector3 center)
        {
            if (unit == null || unit.IsFixed || !unit.IsAlive)
                return;
            unit.LoiterCenter = ClampToMap(center);
            unit.HasLoiter = true;
            unit.HasDestination = false;
            unit.HasManualMoveOrder = false;
            unit.FlightMode = DemoFlightMode.Loiter;
            unit.HoverStableTime = 0f;
            Vector3 longAxis = unit.Facing.sqrMagnitude > 0.001f ? unit.Facing.normalized : Vector3.right;
            Vector3 shortAxis = new Vector3(-longAxis.z, 0f, longAxis.x);
            float straightHalfLength = Mathf.Max(0.1f, Balance.LoiterStraightHalfLength);
            float turnRadius = Mathf.Max(0.1f, Balance.LoiterTurnRadius);
            int arcSegments = Mathf.Max(2, Balance.LoiterArcSegments);
            List<Vector3> waypoints = new List<Vector3>(arcSegments * 2 + 2);
            waypoints.Add(ClampToMap(unit.LoiterCenter - longAxis * straightHalfLength + shortAxis * turnRadius));
            waypoints.Add(ClampToMap(unit.LoiterCenter + longAxis * straightHalfLength + shortAxis * turnRadius));
            for (int i = 1; i <= arcSegments; i++)
            {
                float angle = Mathf.Lerp(90f, -90f, (float)i / arcSegments) * Mathf.Deg2Rad;
                waypoints.Add(ClampToMap(unit.LoiterCenter + longAxis * straightHalfLength +
                    longAxis * (Mathf.Cos(angle) * turnRadius) + shortAxis * (Mathf.Sin(angle) * turnRadius)));
            }
            waypoints.Add(ClampToMap(unit.LoiterCenter - longAxis * straightHalfLength - shortAxis * turnRadius));
            for (int i = 1; i < arcSegments; i++)
            {
                float angle = Mathf.Lerp(-90f, -270f, (float)i / arcSegments) * Mathf.Deg2Rad;
                waypoints.Add(ClampToMap(unit.LoiterCenter - longAxis * straightHalfLength +
                    longAxis * (Mathf.Cos(angle) * turnRadius) + shortAxis * (Mathf.Sin(angle) * turnRadius)));
            }
            unit.SetLoiterWaypoints(waypoints);
            unit.StableLoiterTime = 0f;
        }

        private bool CanFireWeapon(DemoUnitModel unit)
        {
            if (unit == null || unit.IsResupplying || unit.ReloadRemaining > 0f)
                return false;
            int cost = Mathf.Max(1, unit.Stats.AmmoPerAttack);
            if (unit.MagazineAmmo >= cost)
                return true;
            StartReload(unit);
            return false;
        }

        private void StartReload(DemoUnitModel unit)
        {
            if (unit == null || unit.ReloadRemaining > 0f || unit.MagazineAmmo >= unit.Stats.MagazineSize)
                return;
            if (!unit.Stats.UnlimitedReserveAmmo && unit.ReserveAmmo <= 0)
            {
                if (unit.Team == DemoTeam.Player && unit.DeploymentState == DemoUnitDeploymentState.Active)
                {
                    ClearTarget(unit);
                    CancelAbilityChannel(unit, false);
                    unit.DeploymentState = DemoUnitDeploymentState.Returning;
                    SetMovement(unit, BasePosition, DemoUnitActivity.Moving, true);
                    Raise($"{unit.DisplayName} exhausted ammunition and is returning to base.", unit.Position, true);
                }
                return;
            }
            unit.ReloadRemaining = Mathf.Max(0.01f, unit.Stats.ReloadDuration);
            Raise($"{unit.DisplayName} began reloading.", unit.Position, false);
        }

        private void ConsumeAmmo(DemoUnitModel unit)
        {
            int cost = Mathf.Max(1, unit.Stats.AmmoPerAttack);
            unit.MagazineAmmo = Mathf.Max(0, unit.MagazineAmmo - cost);
            if (unit.MagazineAmmo < cost)
                StartReload(unit);
        }

        private float CalculateHitChance(DemoUnitModel attacker, DemoUnitModel target)
        {
            float chance = attacker.Stats.BaseAccuracy *
                           (0.5f + 0.5f * attacker.LockQualityRatio) *
                           (1f - 0.5f * attacker.SuppressionRatio);
            return Mathf.Clamp(chance, Balance.MinimumHitChance, Balance.MaximumHitChance);
        }

        private float CalculateEvasionChance(DemoUnitModel target)
        {
            float speedRatio = target.Stats.MoveSpeed <= 0f ? 0f : Mathf.Clamp01(target.CurrentSpeed / target.Stats.MoveSpeed);
            float chance = 0.05f + 0.08f * (target.Stats.Mobility - 1f) + 0.12f * speedRatio +
                           0.15f * target.TurnRatio - 0.2f * target.SuppressionRatio;
            return Mathf.Clamp(chance, Balance.MinimumEvasionChance, Balance.MaximumEvasionChance);
        }

        private Vector3 CalculateLeadPoint(DemoUnitModel attacker, DemoUnitModel target)
        {
            float projectileSpeed = Mathf.Max(0.1f, attacker.Stats.ProjectileSpeed);
            float travelTime = HorizontalDistance(attacker.Position, target.Position) / projectileSpeed;
            return ClampToMap(target.Position + target.CurrentVelocity * travelTime);
        }

        private bool CanUseFireControl(DemoUnitModel attacker, DemoUnitModel target)
        {
            return attacker != null && target != null && attacker.IsFireControlReady &&
                   (target.PlayerIntelLevel >= DemoIntelLevel.Assessed || target.MarkedUntil > SimulationTime);
        }

        private float GetEffectiveAttackRange(DemoUnitModel attacker, DemoUnitModel target)
        {
            if (attacker == null)
                return 0f;
            float baseRange = CanUseFireControl(attacker, target)
                ? Mathf.Max(attacker.Stats.AttackRange, attacker.Stats.PassiveAttackRange)
                : attacker.Stats.AttackRange;
            return Mathf.Max(0.1f, baseRange *
                (1f - Balance.FullSuppressionVisionRangePenalty * attacker.SuppressionRatio));
        }

        private static float DistanceToSegment(Vector3 point, Vector3 start, Vector3 end)
        {
            Vector3 segment = end - start;
            segment.y = 0f;
            Vector3 offset = point - start;
            offset.y = 0f;
            float lengthSquared = segment.sqrMagnitude;
            if (lengthSquared < 0.0001f)
                return offset.magnitude;
            float t = Mathf.Clamp01(Vector3.Dot(offset, segment) / lengthSquared);
            return (offset - segment * t).magnitude;
        }

        private void ApplySuppression(DemoUnitModel target, DemoDamageResult result, bool explosive, float additional = 0f)
        {
            if (target == null || !result.Hit || result.Evaded)
                return;
            float dealt = result.ShieldDamage + result.HealthDamage;
            float gain = Balance.SuppressionBasePerHit + Balance.SuppressionDamageScale * dealt /
                         Mathf.Max(1f, target.Stats.MaxHealth);
            if (explosive)
                gain *= Balance.ExplosiveSuppressionMultiplier;
            target.Suppression = Mathf.Clamp(target.Suppression + gain + Mathf.Max(0f, additional), 0f, 100f);
            target.LastHitAt = SimulationTime;
        }

        private void ReportDamage(DemoUnitModel attacker, DemoUnitModel target, DemoDamageResult result, Vector3 position, bool remote)
        {
            if (!result.Hit || result.Evaded)
                return;
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
