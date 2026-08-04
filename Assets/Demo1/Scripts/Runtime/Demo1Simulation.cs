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

    public enum DemoBattleLine
    {
        Vanguard,
        Main,
        Support
    }

    public enum DemoAttackProfile
    {
        Standard,
        ScreenPiercing
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
        Reinforcing,
        Fighting,
        Retreating,
        Protected,
        Destroyed
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

    [Serializable]
    public sealed class Demo1Balance
    {
        public float MinimumDamage = 1f;
        public float CriticalMultiplier = 1.55f;
        public float CoreMultiplier = 2.4f;
        public float ShieldMagicCostPerDamage = 0.55f;
        public float ReinforcementRadius = 13f;
        public float ForcedEngagementRadius = 6f;
        public float ScreenRequiredPerProtectedUnit = 1f;
        public float BattleLineChangeDuration = 2f;
        public float VanguardAttackMultiplier = 0.9f;
        public float VanguardDamageTakenMultiplier = 0.85f;
        public float VanguardScreenMultiplier = 1.25f;
        public float MainAttackMultiplier = 1.15f;
        public float SupportAttackMultiplier = 0.8f;
        public float SupportEffectMultiplier = 1.5f;
        public int ArtillerySalvoEveryAttacks = 3;
        public float ArtillerySalvoDamageMultiplier = 1.45f;
        public float ScoutMarkDuration = 4f;
        public float ScoutMarkDamageMultiplier = 1.2f;
        public float SupportPulseInterval = 4f;
        public float SupportPulseShield = 6f;
        public float SupportPulseMagic = 4f;
        public float GuardInterceptionChance = 0.65f;
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
        public float RetreatBaseDuration = 4.5f;
        public float DisengageProtectionDuration = 5f;
        public float RetreatSafeDistance = 9f;
        public float DestinationRadius = 1.8f;
        public float RemoteStrikeDelay = 3f;
        public float RemoteStrikeRadius = 5f;
        public float RemoteStrikeDamageMultiplier = 1.8f;
        public float RemoteStrikeCooldown = 12f;
        public float RemoteStrikeRange = 42f;
        public float MapHalfWidth = 45f;
        public float MapHalfHeight = 30f;
        public int RandomSeed = 1944;
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
        public float GlobalShieldBonus = 0f;
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
        public bool CanRemoteStrike;
        public DemoBattleLine PreferredBattleLine = DemoBattleLine.Vanguard;
        public DemoAttackProfile AttackProfile = DemoAttackProfile.Standard;
        public float ScreenPower = 1f;
        public float ScreenPenetration;
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
        public int CombatId { get; }

        public DemoBattleEvent(float time, string message, Vector3 position, bool important, int combatId = -1)
        {
            Time = time;
            Message = message;
            Position = position;
            Important = important;
            CombatId = combatId;
        }
    }

    public sealed class DemoCombatParticipantState
    {
        public int UnitId { get; }
        public DemoBattleLine Line;
        public DemoBattleLine TargetLine;
        public float RepositionRemaining;
        public int LastTargetId = -1;
        public int AttacksPerformed;
        public float RoleAbilityRemaining;
        public float MarkedUntil;
        public bool FortressBarrageAnnounced;

        public bool IsRepositioning => RepositionRemaining > 0f;

        public DemoCombatParticipantState(int unitId, DemoBattleLine line)
        {
            UnitId = unitId;
            Line = line;
            TargetLine = line;
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
        public int CombatId = -1;
        public int PendingReinforcementBattleId = -1;
        public DemoUnitActivity Activity = DemoUnitActivity.Idle;
        public float Health;
        public float Magic;
        public float Shield;
        public float AttackCooldown;
        public float RemoteStrikeCooldown;
        public float RetreatRemaining;
        public float RetreatDuration;
        public float ProtectedUntil;
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
        public float HealthRatio => Stats.MaxHealth <= 0f ? 0f : Health / Stats.MaxHealth;
        public float MagicRatio => Stats.MaxMagic <= 0f ? 0f : Magic / Stats.MaxMagic;
        public float ShieldRatio => Stats.MaxShield <= 0f ? 0f : Shield / Stats.MaxShield;
        public float RetreatProgress => RetreatDuration <= 0f ? 0f : 1f - Mathf.Clamp01(RetreatRemaining / RetreatDuration);
        public bool HasPlayerIntel => Team == DemoTeam.Player || IsRevealedToPlayer || PlayerIntelLevel != DemoIntelLevel.Unknown;
        public bool CanBeDirectlyTargetedByPlayer => Team == DemoTeam.Player || HasPersistentPlayerIntel ||
                                                     (IsCurrentlyObservedByPlayer && PlayerIntelLevel >= DemoIntelLevel.Identified) || CombatId >= 0 ||
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
            LastKnownPosition = position;
            EnemyAiHomePosition = position;
            EnemyAiLastKnownPosition = position;
            Destination = position;
            Health = Stats.MaxHealth;
            Magic = Stats.MaxMagic;
            Shield = Stats.MaxShield;
            IsRevealedToPlayer = team == DemoTeam.Player;
            IsCurrentlyObservedByPlayer = team == DemoTeam.Player;
            PlayerIntelLevel = team == DemoTeam.Player ? DemoIntelLevel.Assessed : DemoIntelLevel.Unknown;
        }

        internal void SetEnemyAiPatrolPoints(IEnumerable<Vector3> patrolPoints)
        {
            _enemyAiPatrolPoints.Clear();
            _enemyAiPatrolPoints.AddRange(patrolPoints);
        }
    }

    public sealed class DemoCombatModel
    {
        public int Id { get; }
        public Vector3 Center { get; }
        public float ReinforcementRadius { get; }
        public float ForcedRadius { get; }
        public HashSet<int> Participants { get; } = new HashSet<int>();
        public Dictionary<int, DemoCombatParticipantState> Assignments { get; } = new Dictionary<int, DemoCombatParticipantState>();
        public bool IsFinished;

        public DemoCombatModel(int id, Vector3 center, float reinforcementRadius, float forcedRadius)
        {
            Id = id;
            Center = center;
            ReinforcementRadius = reinforcementRadius;
            ForcedRadius = forcedRadius;
        }

        public DemoCombatParticipantState GetAssignment(int unitId)
        {
            DemoCombatParticipantState state;
            return Assignments.TryGetValue(unitId, out state) ? state : null;
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
                target.PendingReinforcementBattleId = -1;
            }

            return new DemoDamageResult(raw, shieldSpent, healthDamage, critical, coreHit, !target.IsAlive);
        }
    }

    public sealed class Demo1Simulation
    {
        private readonly Dictionary<int, DemoUnitModel> _units = new Dictionary<int, DemoUnitModel>();
        private readonly List<DemoCombatModel> _combats = new List<DemoCombatModel>();
        private readonly List<DemoRemoteStrikeModel> _remoteStrikes = new List<DemoRemoteStrikeModel>();
        private readonly System.Random _random;
        private int _nextUnitId = 1;
        private int _nextCombatId = 1;
        private int _nextStrikeId = 1;

        public Demo1Balance Balance { get; }
        public IReadOnlyCollection<DemoUnitModel> Units => _units.Values;
        public IReadOnlyList<DemoCombatModel> Combats => _combats;
        public IReadOnlyList<DemoRemoteStrikeModel> RemoteStrikes => _remoteStrikes;
        public float SimulationTime { get; private set; }
        public DemoOutcome Outcome { get; private set; } = DemoOutcome.Running;
        public event Action<DemoBattleEvent> EventRaised;

        public Demo1Simulation(Demo1Balance balance = null)
        {
            Balance = balance ?? new Demo1Balance();
            _random = new System.Random(Balance.RandomSeed);
        }

        public DemoUnitModel AddUnit(string name, DemoTeam team, DemoUnitRole role, DemoUnitStats stats, Vector3 position)
        {
            DemoUnitModel unit = new DemoUnitModel(_nextUnitId++, name, team, role, stats, ClampToMap(position));
            _units.Add(unit.Id, unit);
            return unit;
        }

        public DemoUnitModel GetUnit(int id)
        {
            DemoUnitModel unit;
            return _units.TryGetValue(id, out unit) ? unit : null;
        }

        public DemoCommandResult ConfigureScoutAi(int unitId, IEnumerable<Vector3> patrolPoints)
        {
            DemoUnitModel unit = GetUnit(unitId);
            if (unit == null || unit.Team != DemoTeam.Enemy || unit.IsFixed)
                return DemoCommandResult.Fail("侦察 AI 只能配置给可移动的敌方单位");

            List<Vector3> points = (patrolPoints ?? Enumerable.Empty<Vector3>())
                .Select(ClampToMap)
                .ToList();
            if (points.Count == 0)
                points.Add(unit.Position);

            unit.EnemyAiProfile = DemoEnemyAiProfile.Scout;
            unit.EnemyAiState = DemoEnemyAiState.Patrol;
            unit.EnemyAiHomePosition = unit.Position;
            unit.EnemyAiTargetId = -1;
            unit.EnemyAiHasLastKnownPosition = false;
            unit.EnemyAiDecisionRemaining = 0f;
            unit.EnemyAiPatrolIndex = 0;
            unit.SetEnemyAiPatrolPoints(points);
            StopEnemyAiMovement(unit);
            return DemoCommandResult.Ok($"{unit.DisplayName} 已启用独立侦察 AI");
        }

        public DemoCommandResult ConfigureCombatAi(int unitId, Vector3 homePosition)
        {
            DemoUnitModel unit = GetUnit(unitId);
            if (unit == null || unit.Team != DemoTeam.Enemy || unit.IsFixed)
                return DemoCommandResult.Fail("战斗 AI 只能配置给可移动的敌方单位");

            unit.EnemyAiProfile = DemoEnemyAiProfile.Combat;
            unit.EnemyAiState = DemoEnemyAiState.Guard;
            unit.EnemyAiHomePosition = ClampToMap(homePosition);
            unit.EnemyAiTargetId = -1;
            unit.EnemyAiHasLastKnownPosition = false;
            unit.EnemyAiDecisionRemaining = 0f;
            unit.SetEnemyAiPatrolPoints(Enumerable.Empty<Vector3>());
            StopEnemyAiMovement(unit);
            return DemoCommandResult.Ok($"{unit.DisplayName} 已启用独立战斗 AI");
        }

        public DemoCombatModel GetCombat(int id)
        {
            return _combats.FirstOrDefault(combat => combat.Id == id);
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

        public float GetScreeningEfficiency(int combatId, DemoTeam team)
        {
            DemoCombatModel combat = GetCombat(combatId);
            if (combat == null || combat.IsFinished)
                return 0f;

            List<DemoUnitModel> protectedUnits = combat.Participants
                .Select(GetUnit)
                .Where(unit => IsParticipantOnLine(combat, unit, team, DemoBattleLine.Main) ||
                               IsParticipantOnLine(combat, unit, team, DemoBattleLine.Support))
                .ToList();
            if (protectedUnits.Count == 0)
                return 1f;

            float screen = combat.Participants
                .Select(GetUnit)
                .Where(unit => IsActiveParticipant(combat, unit, team) &&
                               combat.GetAssignment(unit.Id).Line == DemoBattleLine.Vanguard)
                .Sum(unit => Mathf.Max(0f, unit.Stats.ScreenPower) * Balance.VanguardScreenMultiplier);
            float required = protectedUnits.Count * Mathf.Max(0.01f, Balance.ScreenRequiredPerProtectedUnit);
            return Mathf.Clamp01(screen / required);
        }

        public float GetBattleLineAttackMultiplier(DemoBattleLine line)
        {
            switch (line)
            {
                case DemoBattleLine.Vanguard: return Balance.VanguardAttackMultiplier;
                case DemoBattleLine.Main: return Balance.MainAttackMultiplier;
                case DemoBattleLine.Support: return Balance.SupportAttackMultiplier;
                default: return 1f;
            }
        }

        public float GetBattleLineDamageTakenMultiplier(DemoBattleLine line)
        {
            return line == DemoBattleLine.Vanguard ? Balance.VanguardDamageTakenMultiplier : 1f;
        }

        public float GetMarkRemaining(int combatId, int unitId)
        {
            DemoCombatParticipantState state = GetCombat(combatId)?.GetAssignment(unitId);
            return state == null ? 0f : Mathf.Max(0f, state.MarkedUntil - SimulationTime);
        }

        public float GetEffectiveCoreDiscovery(int unitId)
        {
            DemoUnitModel unit = GetUnit(unitId);
            if (unit == null)
                return 0f;
            DemoCombatModel combat = unit.CombatId >= 0 ? GetCombat(unit.CombatId) : null;
            return unit.Stats.CoreDiscovery * GetCoreDiscoveryMultiplier(combat, unit);
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

        public float GetEffectiveShieldBonus(int combatId, int targetUnitId)
        {
            DemoCombatModel combat = GetCombat(combatId);
            DemoUnitModel target = GetUnit(targetUnitId);
            if (combat == null || combat.IsFinished || target == null || target.CombatId != combat.Id)
                return 0f;

            float supportBonus = combat.Participants
                .Select(GetUnit)
                .Where(unit => ProvidesShieldSupport(combat, unit, target.Team))
                .Sum(unit => unit.Stats.GlobalShieldBonus * Balance.SupportEffectMultiplier);
            float traitBonus = combat.Participants
                .Select(GetUnit)
                .Where(unit => unit != null && unit.Stats.HasTrait(DemoUnitTrait.MiyafujiShieldAura) &&
                               IsActiveParticipant(combat, unit, target.Team))
                .Sum(unit => Balance.MiyafujiShieldEfficiencyBonus);
            return Mathf.Max(0f, supportBonus + traitBonus);
        }

        public float GetCombatStrength(int combatId, DemoTeam team)
        {
            DemoCombatModel combat = GetCombat(combatId);
            if (combat == null)
                return 0f;
            return combat.Participants.Select(GetUnit)
                .Where(unit => unit != null && unit.IsAlive && unit.Team == team)
                .Sum(unit => unit.Health + unit.Shield + unit.Magic * 0.25f);
        }

        public DemoCommandResult RequestBattleLineChange(int unitId, DemoBattleLine targetLine)
        {
            DemoUnitModel unit = GetUnit(unitId);
            if (unit == null || !unit.IsAlive || unit.Team != DemoTeam.Player)
                return DemoCommandResult.Fail("只能调整己方存活单位的阵位");
            if (unit.IsFixed)
                return DemoCommandResult.Fail("固定目标不能调整阵位");
            if (unit.CombatId < 0)
                return DemoCommandResult.Fail("单位尚未加入战斗");
            if (unit.Activity == DemoUnitActivity.Retreating)
                return DemoCommandResult.Fail("撤退中的单位不能调整阵位");
            if (targetLine != DemoBattleLine.Vanguard && targetLine != DemoBattleLine.Main && targetLine != DemoBattleLine.Support)
                return DemoCommandResult.Fail("目标阵线无效");
            DemoCombatModel combat = GetCombat(unit.CombatId);
            DemoCombatParticipantState state = combat?.GetAssignment(unitId);
            if (combat == null || combat.IsFinished || state == null)
                return DemoCommandResult.Fail("目标战斗已经结束");
            if (state.Line == targetLine && !state.IsRepositioning)
                return DemoCommandResult.Fail("单位已经位于该阵线");
            state.Line = targetLine;
            state.TargetLine = targetLine;
            state.RepositionRemaining = Balance.BattleLineChangeDuration;
            Raise($"{unit.DisplayName} 正在转移至{BattleLineName(targetLine)}", combat.Center, true, combat.Id);
            return DemoCommandResult.Ok($"换位命令已下达，{state.RepositionRemaining:0.0}s 后完成");
        }

        public DemoCommandResult IssueMove(IEnumerable<int> unitIds, Vector3 destination)
        {
            List<DemoUnitModel> candidates = unitIds.Select(GetUnit).Where(CanMove).ToList();
            if (candidates.Count == 0)
                return DemoCommandResult.Fail("没有可执行移动命令的单位（参战单位必须先撤退）");

            float spacing = Mathf.Min(1.2f, Balance.DestinationRadius * 0.65f);
            for (int i = 0; i < candidates.Count; i++)
            {
                float angle = candidates.Count == 1 ? 0f : i * Mathf.PI * 2f / candidates.Count;
                Vector3 offset = new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle)) * spacing;
                DemoUnitModel unit = candidates[i];
                unit.Destination = ClampToMap(destination + offset);
                Vector3 facing = unit.Destination - unit.Position;
                facing.y = 0f;
                if (facing.sqrMagnitude > 0.001f)
                    unit.Facing = facing.normalized;
                unit.HasDestination = true;
                unit.PendingReinforcementBattleId = -1;
                unit.Activity = DemoUnitActivity.Moving;
            }

            Raise($"已向 {candidates.Count} 名魔女下达独立移动命令", destination, false);
            return DemoCommandResult.Ok("移动命令已下达");
        }

        public DemoCommandResult StartCombat(int attackerId, int targetId)
        {
            DemoUnitModel attacker = GetUnit(attackerId);
            DemoUnitModel target = GetUnit(targetId);
            if (attacker == null || target == null || !attacker.IsAlive || !target.IsAlive)
                return DemoCommandResult.Fail("交战目标无效");
            if (attacker.Team == target.Team)
                return DemoCommandResult.Fail("不能攻击友军");
            if (attacker.IsFixed)
                return DemoCommandResult.Fail("固定目标不能主动移动接战");
            if (attacker.CombatId >= 0)
                return DemoCommandResult.Fail("单位已在战斗中");
            if (SimulationTime < attacker.ProtectedUntil || SimulationTime < target.ProtectedUntil)
                return DemoCommandResult.Fail("目标或发起者仍处于脱战保护");
            if (attacker.Team == DemoTeam.Player && !target.CanBeDirectlyTargetedByPlayer)
                return DemoCommandResult.Fail(target.HasPlayerIntel ? "当前只有目标的最后已知位置" : "尚未获得目标情报");

            if (target.CombatId >= 0)
                return RequestReinforcement(new[] { attackerId }, target.CombatId);

            float distance = HorizontalDistance(attacker.Position, target.Position);
            if (distance > attacker.Stats.EngagementRadius)
                return DemoCommandResult.Fail($"目标超出交战半径（{distance:0.0}/{attacker.Stats.EngagementRadius:0.0}）");

            Vector3 center = target.IsFixed ? target.Position : (attacker.Position + target.Position) * 0.5f;
            Vector3 attackerFacing = target.Position - attacker.Position;
            attackerFacing.y = 0f;
            if (attackerFacing.sqrMagnitude > 0.001f)
                attacker.Facing = attackerFacing.normalized;
            Vector3 targetFacing = attacker.Position - target.Position;
            targetFacing.y = 0f;
            if (targetFacing.sqrMagnitude > 0.001f)
                target.Facing = targetFacing.normalized;
            DemoCombatModel combat = new DemoCombatModel(_nextCombatId++, center, Balance.ReinforcementRadius, Balance.ForcedEngagementRadius);
            _combats.Add(combat);
            AddParticipant(combat, attacker);
            AddParticipant(combat, target);
            Raise($"战斗 #{combat.Id} 爆发：{attacker.DisplayName} 对 {target.DisplayName}", center, true, combat.Id);
            return DemoCommandResult.Ok($"战斗 #{combat.Id} 已创建");
        }

        public DemoCommandResult RequestReinforcement(IEnumerable<int> unitIds, int combatId)
        {
            DemoCombatModel combat = GetCombat(combatId);
            if (combat == null || combat.IsFinished)
                return DemoCommandResult.Fail("目标战斗已经结束");

            int accepted = 0;
            foreach (int unitId in unitIds)
            {
                DemoUnitModel unit = GetUnit(unitId);
                if (!CanMove(unit) || unit.CombatId >= 0)
                    continue;

                unit.PendingReinforcementBattleId = combat.Id;
                if (HorizontalDistance(unit.Position, combat.Center) <= combat.ReinforcementRadius)
                {
                    AddParticipant(combat, unit);
                    Raise($"{unit.DisplayName} 加入战斗 #{combat.Id}", combat.Center, false, combat.Id);
                }
                else
                {
                    unit.Destination = combat.Center;
                    unit.HasDestination = true;
                    unit.Activity = DemoUnitActivity.Reinforcing;
                }
                accepted++;
            }

            return accepted > 0
                ? DemoCommandResult.Ok($"{accepted} 个单位正在增援战斗 #{combat.Id}")
                : DemoCommandResult.Fail("选中单位当前无法增援");
        }

        public DemoCommandResult RequestRetreat(IEnumerable<int> unitIds)
        {
            int accepted = 0;
            foreach (int id in unitIds)
            {
                DemoUnitModel unit = GetUnit(id);
                if (unit == null || !unit.IsAlive || unit.CombatId < 0 || unit.IsFixed || unit.Activity == DemoUnitActivity.Retreating)
                    continue;

                unit.Activity = DemoUnitActivity.Retreating;
                unit.RetreatDuration = Mathf.Clamp(Balance.RetreatBaseDuration / Mathf.Max(0.25f, unit.Stats.Mobility), 1.2f, 8f);
                unit.RetreatRemaining = unit.RetreatDuration;
                accepted++;
                Raise($"{unit.DisplayName} 开始撤退", unit.Position, true, unit.CombatId);
            }

            return accepted > 0
                ? DemoCommandResult.Ok($"{accepted} 个单位开始撤退")
                : DemoCommandResult.Fail("没有可撤退的参战单位");
        }

        public DemoCommandResult ScheduleRemoteStrike(int attackerId, Vector3 target)
        {
            DemoUnitModel attacker = GetUnit(attackerId);
            if (attacker == null || !attacker.IsAlive || !attacker.Stats.CanRemoteStrike)
                return DemoCommandResult.Fail("所选单位不具备远程打击能力");
            if (attacker.RemoteStrikeCooldown > 0f)
                return DemoCommandResult.Fail($"远程打击冷却中（{attacker.RemoteStrikeCooldown:0.0}s）");
            if (HorizontalDistance(attacker.Position, target) > Balance.RemoteStrikeRange)
                return DemoCommandResult.Fail("目标区域超出远程打击距离");

            DemoRemoteStrikeModel strike = new DemoRemoteStrikeModel(
                _nextStrikeId++, attacker.Id, ClampToMap(target), Balance.RemoteStrikeRadius, Balance.RemoteStrikeDelay);
            _remoteStrikes.Add(strike);
            attacker.RemoteStrikeCooldown = Balance.RemoteStrikeCooldown;
            Raise($"{attacker.DisplayName} 发起远程打击，{strike.Remaining:0.0}s 后命中", strike.Target, true);
            return DemoCommandResult.Ok("远程打击已进入倒计时");
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
                TickUnits(dt);
                TickVisibility(dt);
                TickEnemyAi(dt);
                TickCombats(dt);
                TickRemoteStrikes(dt);
                EvaluateOutcome();
                remaining -= dt;
            }
        }

        private void TickUnits(float dt)
        {
            foreach (DemoUnitModel unit in _units.Values)
            {
                if (!unit.IsAlive)
                    continue;

                unit.AttackCooldown = Mathf.Max(0f, unit.AttackCooldown - dt);
                unit.RemoteStrikeCooldown = Mathf.Max(0f, unit.RemoteStrikeCooldown - dt);

                if (unit.CombatId < 0)
                {
                    unit.Magic = Mathf.Min(unit.Stats.MaxMagic, unit.Magic + unit.Stats.MagicRecovery * dt);
                    if (unit.Magic > unit.Stats.MaxMagic * 0.25f)
                        unit.Shield = Mathf.Min(unit.Stats.MaxShield, unit.Shield + unit.Stats.MagicRecovery * 0.45f * dt);
                }

                if (unit.Activity == DemoUnitActivity.Protected && SimulationTime >= unit.ProtectedUntil)
                    unit.Activity = DemoUnitActivity.Idle;

                if (!unit.HasDestination || (unit.Activity != DemoUnitActivity.Moving && unit.Activity != DemoUnitActivity.Reinforcing))
                    continue;

                Vector3 movement = unit.Destination - unit.Position;
                movement.y = 0f;
                if (movement.sqrMagnitude > 0.001f)
                    unit.Facing = movement.normalized;
                unit.Position = Vector3.MoveTowards(unit.Position, unit.Destination, unit.Stats.MoveSpeed * dt);
                unit.Position = ClampToMap(unit.Position);

                if (unit.Activity == DemoUnitActivity.Reinforcing)
                {
                    DemoCombatModel combat = GetCombat(unit.PendingReinforcementBattleId);
                    if (combat == null || combat.IsFinished)
                    {
                        unit.PendingReinforcementBattleId = -1;
                        unit.HasDestination = false;
                        unit.Activity = DemoUnitActivity.Idle;
                    }
                    else if (HorizontalDistance(unit.Position, combat.Center) <= combat.ReinforcementRadius)
                    {
                        AddParticipant(combat, unit);
                        Raise($"{unit.DisplayName} 抵达并加入战斗 #{combat.Id}", combat.Center, false, combat.Id);
                    }
                    continue;
                }

                if (HorizontalDistance(unit.Position, unit.Destination) <= Balance.DestinationRadius * 0.2f)
                {
                    unit.Position = unit.Destination;
                    unit.HasDestination = false;
                    unit.Activity = SimulationTime < unit.ProtectedUntil ? DemoUnitActivity.Protected : DemoUnitActivity.Idle;
                }
            }
        }

        private void TickVisibility(float dt)
        {
            List<DemoUnitModel> observers = _units.Values
                .Where(unit => unit.IsAlive && unit.Team == DemoTeam.Player &&
                               unit.Stats.WitchVisionType != DemoWitchVisionType.None)
                .ToList();
            foreach (DemoUnitModel enemy in _units.Values.Where(unit => unit.IsAlive && unit.Team == DemoTeam.Enemy))
            {
                enemy.IsCurrentlyObservedByPlayer = false;
                bool observedInCombat = enemy.CombatId >= 0 && GetCombat(enemy.CombatId)?.Participants
                    .Select(GetUnit).Any(unit => unit != null && unit.IsAlive && unit.Team == DemoTeam.Player) == true;
                DemoUnitModel observer = observedInCombat ? observers.FirstOrDefault() :
                    observers.FirstOrDefault(unit => IsInsideWitchVision(unit, enemy.Position));
                if (observedInCombat || observer != null)
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
                Raise("发现不明接触", enemy.Position, true);
            if (previous < DemoIntelLevel.Identified && enemy.PlayerIntelLevel >= DemoIntelLevel.Identified)
                Raise($"确认敌情：{enemy.DisplayName}", enemy.Position, true);
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
            if (float.IsNegativeInfinity(enemy.LastObservedAt))
            {
                enemy.PlayerIntelLevel = DemoIntelLevel.Unknown;
                enemy.IsRevealedToPlayer = false;
                return;
            }

            DemoIntelLevel previous = enemy.PlayerIntelLevel;
            float age = SimulationTime - enemy.LastObservedAt;
            if (age > Balance.ContactIntelMemoryDuration)
            {
                enemy.PlayerIntelLevel = DemoIntelLevel.Unknown;
                enemy.IsRevealedToPlayer = false;
                enemy.IdentificationProgress = 0f;
                enemy.AssessmentProgress = 0f;
            }
            else if (age > Balance.IdentifiedIntelMemoryDuration)
                enemy.PlayerIntelLevel = DemoIntelLevel.Contact;
            else if (age > Balance.AssessedIntelMemoryDuration)
                enemy.PlayerIntelLevel = DemoIntelLevel.Identified;
            enemy.AssessmentProgress = Mathf.Max(0f, enemy.AssessmentProgress - dt * 0.25f);

            if (previous >= DemoIntelLevel.Identified && enemy.PlayerIntelLevel == DemoIntelLevel.Contact)
                Raise($"失去确认：{enemy.DisplayName}", enemy.LastKnownPosition, false);
            if (previous != DemoIntelLevel.Unknown && enemy.PlayerIntelLevel == DemoIntelLevel.Unknown)
                Raise($"目标失联：{enemy.DisplayName}", enemy.LastKnownPosition, false);
        }

        private void TickEnemyAi(float dt)
        {
            foreach (DemoUnitModel enemy in _units.Values
                         .Where(unit => unit.IsAlive && unit.Team == DemoTeam.Enemy && unit.EnemyAiProfile != DemoEnemyAiProfile.None)
                         .ToList())
            {
                enemy.EnemyAiDecisionRemaining -= dt;

                if (enemy.CombatId >= 0)
                {
                    if (enemy.EnemyAiProfile == DemoEnemyAiProfile.Scout &&
                        enemy.HealthRatio <= Balance.EnemyAiScoutRetreatHealthRatio &&
                        enemy.Activity != DemoUnitActivity.Retreating)
                    {
                        RequestRetreat(new[] { enemy.Id });
                    }
                    enemy.EnemyAiState = enemy.Activity == DemoUnitActivity.Retreating
                        ? DemoEnemyAiState.Retreating
                        : DemoEnemyAiState.Fighting;
                    continue;
                }

                if (enemy.EnemyAiDecisionRemaining > 0f)
                    continue;
                enemy.EnemyAiDecisionRemaining = Mathf.Max(0.05f, Balance.EnemyAiDecisionInterval);

                if (enemy.EnemyAiProfile == DemoEnemyAiProfile.Scout)
                    TickScoutAiDecision(enemy);
                else if (enemy.EnemyAiProfile == DemoEnemyAiProfile.Combat)
                    TickCombatAiDecision(enemy);
            }
        }

        private void TickScoutAiDecision(DemoUnitModel scout)
        {
            if (scout.HealthRatio <= Balance.EnemyAiScoutRetreatHealthRatio)
            {
                scout.EnemyAiTargetId = -1;
                scout.EnemyAiHasLastKnownPosition = false;
                TickScoutPatrol(scout);
                return;
            }

            DemoUnitModel target = FindNearestVisiblePlayer(scout, false);
            if (target != null)
            {
                scout.EnemyAiTargetId = target.Id;
                scout.EnemyAiLastKnownPosition = target.Position;
                scout.EnemyAiHasLastKnownPosition = true;
                if (TryEnemyAiStartCombat(scout, target))
                    return;

                scout.EnemyAiState = DemoEnemyAiState.Pursue;
                SetEnemyAiDestination(scout, target.Position);
                return;
            }

            scout.EnemyAiTargetId = -1;
            if (scout.EnemyAiHasLastKnownPosition)
            {
                if (HorizontalDistance(scout.Position, scout.EnemyAiLastKnownPosition) > Balance.EnemyAiArrivalRadius)
                {
                    scout.EnemyAiState = DemoEnemyAiState.Investigate;
                    SetEnemyAiDestination(scout, scout.EnemyAiLastKnownPosition);
                    return;
                }
                scout.EnemyAiHasLastKnownPosition = false;
            }

            TickScoutPatrol(scout);
        }

        private void TickScoutPatrol(DemoUnitModel scout)
        {
            scout.EnemyAiState = DemoEnemyAiState.Patrol;
            if (scout.EnemyAiPatrolPoints.Count == 0)
            {
                StopEnemyAiMovement(scout);
                return;
            }

            int index = Mathf.Clamp(scout.EnemyAiPatrolIndex, 0, scout.EnemyAiPatrolPoints.Count - 1);
            Vector3 patrolPoint = scout.EnemyAiPatrolPoints[index];
            if (HorizontalDistance(scout.Position, patrolPoint) <= Balance.EnemyAiArrivalRadius)
            {
                index = (index + 1) % scout.EnemyAiPatrolPoints.Count;
                scout.EnemyAiPatrolIndex = index;
                patrolPoint = scout.EnemyAiPatrolPoints[index];
            }
            SetEnemyAiDestination(scout, patrolPoint);
        }

        private void TickCombatAiDecision(DemoUnitModel enemy)
        {
            DemoUnitModel target = FindNearestVisiblePlayer(enemy, true);
            if (target != null)
            {
                enemy.EnemyAiTargetId = target.Id;
                if (TryEnemyAiStartCombat(enemy, target))
                    return;

                enemy.EnemyAiState = DemoEnemyAiState.Pursue;
                SetEnemyAiDestination(enemy, target.Position);
                return;
            }

            enemy.EnemyAiTargetId = -1;
            if (HorizontalDistance(enemy.Position, enemy.EnemyAiHomePosition) > Balance.EnemyAiArrivalRadius)
            {
                enemy.EnemyAiState = DemoEnemyAiState.ReturnHome;
                SetEnemyAiDestination(enemy, enemy.EnemyAiHomePosition);
            }
            else
            {
                enemy.EnemyAiState = DemoEnemyAiState.Guard;
                StopEnemyAiMovement(enemy);
            }
        }

        private DemoUnitModel FindNearestVisiblePlayer(DemoUnitModel observer, bool enforceHomeLeash)
        {
            float visionRadius = Mathf.Max(0f, observer.Stats.VisionRadius);
            return _units.Values
                .Where(unit => unit.IsAlive && unit.Team == DemoTeam.Player && unit.CombatId < 0 &&
                               SimulationTime >= unit.ProtectedUntil &&
                               HorizontalDistance(observer.Position, unit.Position) <= visionRadius &&
                               (!enforceHomeLeash || HorizontalDistance(observer.EnemyAiHomePosition, unit.Position) <= Balance.EnemyAiGuardLeashRadius))
                .OrderBy(unit => HorizontalDistance(observer.Position, unit.Position))
                .ThenBy(unit => unit.Id)
                .FirstOrDefault();
        }

        private bool TryEnemyAiStartCombat(DemoUnitModel enemy, DemoUnitModel target)
        {
            if (target.CombatId >= 0 || HorizontalDistance(enemy.Position, target.Position) > enemy.Stats.EngagementRadius)
                return false;
            DemoCommandResult result = StartCombat(enemy.Id, target.Id);
            if (!result.Success)
                return false;

            enemy.EnemyAiState = DemoEnemyAiState.Fighting;
            return true;
        }

        private void SetEnemyAiDestination(DemoUnitModel unit, Vector3 destination)
        {
            if (!CanMove(unit))
                return;
            unit.Destination = ClampToMap(destination);
            Vector3 facing = unit.Destination - unit.Position;
            facing.y = 0f;
            if (facing.sqrMagnitude > 0.001f)
                unit.Facing = facing.normalized;
            unit.HasDestination = true;
            unit.PendingReinforcementBattleId = -1;
            unit.Activity = DemoUnitActivity.Moving;
        }

        private void StopEnemyAiMovement(DemoUnitModel unit)
        {
            if (unit.CombatId >= 0 || unit.Activity == DemoUnitActivity.Retreating)
                return;
            unit.Destination = unit.Position;
            unit.HasDestination = false;
            unit.PendingReinforcementBattleId = -1;
            unit.Activity = SimulationTime < unit.ProtectedUntil ? DemoUnitActivity.Protected : DemoUnitActivity.Idle;
        }

        private void TickCombats(float dt)
        {
            foreach (DemoCombatModel combat in _combats.Where(item => !item.IsFinished).ToList())
            {
                ForceNearbyUnitsIntoCombat(combat);
                TickRetreats(combat, dt);
                TickBattleLines(combat, dt);
                TickRoleMechanics(combat, dt);

                List<DemoUnitModel> attackers = combat.Participants
                    .Select(GetUnit)
                    .Where(unit => CanAttackFromBattleLine(combat, unit))
                    .ToList();

                foreach (DemoUnitModel attacker in attackers)
                {
                    if (attacker.AttackCooldown > 0f)
                        continue;
                    DemoUnitModel target = SelectBattleTarget(combat, attacker);
                    if (target == null)
                        continue;
                    Vector3 facing = target.Position - attacker.Position;
                    facing.y = 0f;
                    if (facing.sqrMagnitude > 0.001f)
                        attacker.Facing = facing.normalized;

                    float shieldBonus = GetEffectiveShieldBonus(combat.Id, target.Id);
                    DemoCombatParticipantState attackerState = combat.GetAssignment(attacker.Id);
                    DemoCombatParticipantState targetState = combat.GetAssignment(target.Id);
                    float attackMultiplier = GetBattleLineAttackMultiplier(attackerState.Line);
                    if (targetState != null && targetState.MarkedUntil > SimulationTime)
                        attackMultiplier *= Balance.ScoutMarkDamageMultiplier;
                    int nextAttack = attackerState.AttacksPerformed + 1;
                    bool artillerySalvo = attacker.Role == DemoUnitRole.Artillery &&
                                           !attacker.Stats.HasTrait(DemoUnitTrait.LynetteSharpshooter) &&
                                           nextAttack % Mathf.Max(1, Balance.ArtillerySalvoEveryAttacks) == 0;
                    if (artillerySalvo)
                        attackMultiplier *= Balance.ArtillerySalvoDamageMultiplier;
                    bool fortressBarrage = attacker.Role == DemoUnitRole.Fortress &&
                                           attacker.HealthRatio <= Balance.FortressBarrageHealthThreshold;
                    if (fortressBarrage)
                        attackMultiplier *= Balance.FortressBarrageDamageMultiplier;

                    float damageTakenMultiplier = GetBattleLineDamageTakenMultiplier(targetState.Line);
                    float coreDiscoveryMultiplier = GetCoreDiscoveryMultiplier(combat, attacker);
                    float criticalChanceBonus = attacker.Stats.HasTrait(DemoUnitTrait.LynetteSharpshooter)
                        ? Balance.LynetteCriticalChanceBonus
                        : 0f;
                    DemoDamageResult result = DemoDamageResolver.Resolve(
                        attacker, target, Balance, _random, shieldBonus, attackMultiplier, damageTakenMultiplier,
                        coreDiscoveryMultiplier, criticalChanceBonus);
                    attackerState.LastTargetId = target.Id;
                    attackerState.AttacksPerformed = nextAttack;
                    if (attacker.Role == DemoUnitRole.Scout && targetState != null && target.IsAlive)
                        targetState.MarkedUntil = Mathf.Max(targetState.MarkedUntil, SimulationTime + Balance.ScoutMarkDuration);
                    float intervalMultiplier = fortressBarrage ? Balance.FortressBarrageIntervalMultiplier : 1f;
                    attacker.AttackCooldown = Mathf.Max(0.2f, GetEffectiveAttackInterval(attacker.Id) * intervalMultiplier);
                    ReportDamage(attacker, target, result, combat.Center, false);
                    if (artillerySalvo)
                        Raise($"{attacker.DisplayName} 完成校射齐射", combat.Center, false, combat.Id);
                }

                foreach (int id in combat.Participants.ToList())
                {
                    DemoUnitModel unit = GetUnit(id);
                    if (unit == null || unit.IsAlive)
                        continue;
                    combat.Participants.Remove(id);
                    unit.CombatId = -1;
                }

                bool hasPlayer = combat.Participants.Select(GetUnit).Any(unit => unit != null && unit.IsAlive && unit.Team == DemoTeam.Player);
                bool hasEnemy = combat.Participants.Select(GetUnit).Any(unit => unit != null && unit.IsAlive && unit.Team == DemoTeam.Enemy);
                if (!hasPlayer || !hasEnemy)
                    EndCombat(combat);
            }
        }

        private void ForceNearbyUnitsIntoCombat(DemoCombatModel combat)
        {
            foreach (DemoUnitModel unit in _units.Values.Where(unit => unit.IsAlive && unit.CombatId < 0 && !unit.IsFixed).ToList())
            {
                if (SimulationTime < unit.ProtectedUntil || HorizontalDistance(unit.Position, combat.Center) > combat.ForcedRadius)
                    continue;
                AddParticipant(combat, unit);
                Raise($"{unit.DisplayName} 进入强制交战区", combat.Center, true, combat.Id);
            }
        }

        private void TickBattleLines(DemoCombatModel combat, float dt)
        {
            foreach (int id in combat.Participants.ToList())
            {
                DemoCombatParticipantState state = combat.GetAssignment(id);
                if (state == null || !state.IsRepositioning)
                    continue;
                state.RepositionRemaining = Mathf.Max(0f, state.RepositionRemaining - dt);
                if (state.RepositionRemaining <= 0f)
                {
                    DemoUnitModel unit = GetUnit(id);
                    if (unit != null && unit.IsAlive)
                        Raise($"{unit.DisplayName} 已进入{BattleLineName(state.Line)}", combat.Center, false, combat.Id);
                }
            }
        }

        private void TickRoleMechanics(DemoCombatModel combat, float dt)
        {
            foreach (int id in combat.Participants.ToList())
            {
                DemoUnitModel unit = GetUnit(id);
                DemoCombatParticipantState state = combat.GetAssignment(id);
                if (unit == null || state == null || !unit.IsAlive)
                    continue;

                if (unit.Role == DemoUnitRole.Fortress &&
                    unit.HealthRatio <= Balance.FortressBarrageHealthThreshold &&
                    !state.FortressBarrageAnnounced)
                {
                    state.FortressBarrageAnnounced = true;
                    Raise($"{unit.DisplayName} 进入应急齐射阶段", combat.Center, true, combat.Id);
                }

                if (unit.Role != DemoUnitRole.Support ||
                    state.Line != DemoBattleLine.Support ||
                    !IsActiveParticipant(combat, unit, unit.Team))
                    continue;

                state.RoleAbilityRemaining -= dt;
                if (state.RoleAbilityRemaining > 0f)
                    continue;
                state.RoleAbilityRemaining = Mathf.Max(0.1f, Balance.SupportPulseInterval);

                bool restoredAny = false;
                float shieldRestore = Balance.SupportPulseShield * Balance.SupportEffectMultiplier;
                float magicRestore = Balance.SupportPulseMagic * Balance.SupportEffectMultiplier;
                foreach (DemoUnitModel ally in combat.Participants.Select(GetUnit)
                             .Where(ally => ally != null && ally.IsAlive && ally.Team == unit.Team &&
                                            ally.Activity == DemoUnitActivity.Fighting))
                {
                    float oldShield = ally.Shield;
                    float oldMagic = ally.Magic;
                    ally.Shield = Mathf.Min(ally.Stats.MaxShield, ally.Shield + shieldRestore);
                    ally.Magic = Mathf.Min(ally.Stats.MaxMagic, ally.Magic + magicRestore);
                    restoredAny |= ally.Shield > oldShield || ally.Magic > oldMagic;
                }
                if (restoredAny)
                    Raise($"{unit.DisplayName} 发动护盾支援脉冲", combat.Center, false, combat.Id);
            }
        }

        private bool CanAttackFromBattleLine(DemoCombatModel combat, DemoUnitModel unit)
        {
            if (unit == null || !unit.IsAlive || unit.Activity != DemoUnitActivity.Fighting)
                return false;
            DemoCombatParticipantState state = combat.GetAssignment(unit.Id);
            return state != null && !state.IsRepositioning;
        }

        private bool IsActiveParticipant(DemoCombatModel combat, DemoUnitModel unit, DemoTeam team)
        {
            if (unit == null || !unit.IsAlive || unit.Team != team || unit.Activity != DemoUnitActivity.Fighting)
                return false;
            DemoCombatParticipantState state = combat.GetAssignment(unit.Id);
            return state != null && !state.IsRepositioning;
        }

        private bool ProvidesShieldSupport(DemoCombatModel combat, DemoUnitModel unit, DemoTeam team)
        {
            if (!IsActiveParticipant(combat, unit, team) || unit.Role != DemoUnitRole.Support)
                return false;
            return combat.GetAssignment(unit.Id).Line == DemoBattleLine.Support;
        }

        private float GetCoreDiscoveryMultiplier(DemoCombatModel combat, DemoUnitModel attacker)
        {
            if (combat == null || combat.IsFinished || attacker == null || attacker.CombatId != combat.Id)
                return 1f;
            float bonus = combat.Participants
                .Select(GetUnit)
                .Where(unit => unit != null && unit.Stats.HasTrait(DemoUnitTrait.SakamotoCoreInsight) &&
                               IsActiveParticipant(combat, unit, attacker.Team))
                .Sum(unit => Balance.SakamotoCoreDiscoveryBonus);
            return Mathf.Max(0f, 1f + bonus);
        }

        private DemoUnitModel SelectBattleTarget(DemoCombatModel combat, DemoUnitModel attacker)
        {
            List<DemoUnitModel> candidates = combat.Participants
                .Select(GetUnit)
                .Where(unit => unit != null && unit.IsAlive && unit.Team != attacker.Team)
                .ToList();
            if (candidates.Count == 0)
                return null;

            if (attacker.Stats.AttackProfile == DemoAttackProfile.ScreenPiercing)
            {
                List<DemoUnitModel> rear = candidates.Where(unit =>
                {
                    DemoBattleLine line = combat.GetAssignment(unit.Id).Line;
                    return line == DemoBattleLine.Main || line == DemoBattleLine.Support;
                }).ToList();
                float chance = Mathf.Clamp01(attacker.Stats.ScreenPenetration) *
                               (1f - GetScreeningEfficiency(combat.Id, candidates[0].Team));
                if (rear.Count > 0 && _random.NextDouble() < chance)
                {
                    DemoUnitModel rearTarget = SelectPreferredTarget(attacker, rear);
                    DemoUnitModel guard = TrySelectGuardInterceptor(combat, candidates[0].Team);
                    if (guard != null)
                    {
                        Raise($"{guard.DisplayName} 拦截了对后排的穿线攻击", combat.Center, false, combat.Id);
                        return guard;
                    }
                    return rearTarget;
                }
            }

            foreach (DemoBattleLine line in new[] { DemoBattleLine.Vanguard, DemoBattleLine.Main, DemoBattleLine.Support })
            {
                DemoUnitModel target = SelectPreferredTarget(attacker,
                    candidates.Where(unit => combat.GetAssignment(unit.Id).Line == line));
                if (target != null)
                    return target;
            }
            return null;
        }

        private DemoUnitModel SelectPreferredTarget(DemoUnitModel attacker, IEnumerable<DemoUnitModel> candidates)
        {
            IOrderedEnumerable<DemoUnitModel> ordered = attacker.Role == DemoUnitRole.Witch
                ? candidates.OrderBy(unit => unit.Stats.AttackProfile == DemoAttackProfile.ScreenPiercing ? 0 : 1)
                    .ThenBy(unit => unit.HealthRatio)
                    .ThenBy(unit => unit.Id)
                : candidates.OrderBy(unit => unit.HealthRatio).ThenBy(unit => unit.Id);
            return ordered.FirstOrDefault();
        }

        private DemoUnitModel TrySelectGuardInterceptor(DemoCombatModel combat, DemoTeam defendingTeam)
        {
            List<DemoUnitModel> guards = combat.Participants.Select(GetUnit)
                .Where(unit => IsActiveParticipant(combat, unit, defendingTeam) &&
                               unit.Role == DemoUnitRole.Guard &&
                               combat.GetAssignment(unit.Id).Line == DemoBattleLine.Vanguard)
                .OrderBy(unit => unit.HealthRatio)
                .ThenBy(unit => unit.Id)
                .ToList();
            if (guards.Count == 0 || _random.NextDouble() >= Mathf.Clamp01(Balance.GuardInterceptionChance))
                return null;
            return guards[0];
        }

        private void TickRetreats(DemoCombatModel combat, float dt)
        {
            foreach (DemoUnitModel unit in combat.Participants.Select(GetUnit).Where(unit => unit != null && unit.Activity == DemoUnitActivity.Retreating).ToList())
            {
                unit.RetreatRemaining -= dt;
                if (unit.RetreatRemaining > 0f)
                    continue;

                Vector3 away = unit.Position - combat.Center;
                if (away.sqrMagnitude < 0.01f)
                    away = unit.Team == DemoTeam.Player ? Vector3.left : Vector3.right;
                unit.Position = ClampToMap(combat.Center + away.normalized * (combat.ReinforcementRadius + Balance.RetreatSafeDistance));
                unit.CombatId = -1;
                unit.PendingReinforcementBattleId = -1;
                unit.HasDestination = false;
                unit.ProtectedUntil = SimulationTime + Balance.DisengageProtectionDuration;
                unit.Activity = DemoUnitActivity.Protected;
                combat.Participants.Remove(unit.Id);
                Raise($"{unit.DisplayName} 完成撤退并获得脱战保护", unit.Position, true, combat.Id);
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
                if (attacker == null)
                    continue;
                List<DemoUnitModel> targets = _units.Values
                    .Where(unit => unit.IsAlive && unit.Team != attacker.Team && HorizontalDistance(unit.Position, strike.Target) <= strike.Radius)
                    .ToList();
                if (targets.Count == 0)
                {
                    Raise("远程打击落空：目标已离开区域", strike.Target, true);
                    continue;
                }

                foreach (DemoUnitModel target in targets)
                {
                    DemoDamageResult result = DemoDamageResolver.Resolve(
                        attacker, target, Balance, _random, 0f, Balance.RemoteStrikeDamageMultiplier);
                    ReportDamage(attacker, target, result, strike.Target, true);
                }
                Raise($"远程打击命中 {targets.Count} 个目标（未创建新战斗）", strike.Target, true);
            }
        }

        private void AddParticipant(DemoCombatModel combat, DemoUnitModel unit)
        {
            if (!unit.IsAlive || combat.IsFinished)
                return;
            if (!combat.Participants.Add(unit.Id))
                return;
            DemoBattleLine line = unit.Stats.PreferredBattleLine;
            combat.Assignments[unit.Id] = new DemoCombatParticipantState(unit.Id, line);
            if (unit.Role == DemoUnitRole.Support)
                combat.Assignments[unit.Id].RoleAbilityRemaining = Mathf.Max(0.1f, Balance.SupportPulseInterval);
            unit.CombatId = combat.Id;
            unit.PendingReinforcementBattleId = -1;
            unit.HasDestination = false;
            unit.Activity = DemoUnitActivity.Fighting;
            Raise($"{unit.DisplayName} 进入{BattleLineName(line)}", combat.Center, false, combat.Id);
        }

        private bool IsParticipantOnLine(DemoCombatModel combat, DemoUnitModel unit, DemoTeam team, DemoBattleLine line)
        {
            return unit != null && unit.IsAlive && unit.Team == team && combat.Participants.Contains(unit.Id) &&
                   combat.GetAssignment(unit.Id)?.Line == line;
        }

        public static string BattleLineName(DemoBattleLine line)
        {
            switch (line)
            {
                case DemoBattleLine.Vanguard: return "前卫线";
                case DemoBattleLine.Main: return "主战线";
                case DemoBattleLine.Support: return "支援线";
                default: return line.ToString();
            }
        }

        private void EndCombat(DemoCombatModel combat)
        {
            combat.IsFinished = true;
            foreach (DemoUnitModel unit in combat.Participants.Select(GetUnit).Where(unit => unit != null && unit.IsAlive))
            {
                unit.CombatId = -1;
                unit.PendingReinforcementBattleId = -1;
                unit.Activity = SimulationTime < unit.ProtectedUntil ? DemoUnitActivity.Protected : DemoUnitActivity.Idle;
            }
            combat.Participants.Clear();
            Raise($"战斗 #{combat.Id} 结束", combat.Center, true, combat.Id);
        }

        private void ReportDamage(DemoUnitModel attacker, DemoUnitModel target, DemoDamageResult result, Vector3 position, bool remote)
        {
            string modifier = result.CoreHit ? "核心命中" : result.Critical ? "暴击" : "命中";
            int combatId = target.CombatId >= 0 ? target.CombatId : attacker.CombatId;
            if (result.Destroyed)
                Raise($"{target.DisplayName} 被{(remote ? "远程打击" : attacker.DisplayName)}击毁", position, true, combatId);
            else if (result.CoreHit || target.HealthRatio <= 0.3f)
                Raise($"{attacker.DisplayName} {modifier} {target.DisplayName}，目标生命 {target.Health:0}", position, true, combatId);
        }

        private void EvaluateOutcome()
        {
            DemoUnitModel fortress = _units.Values.FirstOrDefault(unit => unit.Team == DemoTeam.Enemy && unit.Role == DemoUnitRole.Fortress);
            if (fortress != null && !fortress.IsAlive)
            {
                Outcome = DemoOutcome.Victory;
                Raise("任务完成：敌方固定目标已摧毁", fortress.Position, true);
                return;
            }

            if (!_units.Values.Any(unit => unit.Team == DemoTeam.Player && unit.IsAlive))
            {
                Outcome = DemoOutcome.Defeat;
                Raise("任务失败：我方已无可作战单位", Vector3.zero, true);
            }
        }

        private bool CanMove(DemoUnitModel unit)
        {
            return unit != null && unit.IsAlive && !unit.IsFixed && unit.CombatId < 0;
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

        private void Raise(string message, Vector3 position, bool important, int combatId = -1)
        {
            EventRaised?.Invoke(new DemoBattleEvent(SimulationTime, message, position, important, combatId));
        }
    }
}
