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
        Fortress
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
        public float EngagementRadius = 8f;
        public bool CanRemoteStrike;

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

        public bool IsFixed => Role == DemoUnitRole.Fortress;
        public bool IsAlive => Activity != DemoUnitActivity.Destroyed && Health > 0f;
        public float HealthRatio => Stats.MaxHealth <= 0f ? 0f : Health / Stats.MaxHealth;
        public float MagicRatio => Stats.MaxMagic <= 0f ? 0f : Magic / Stats.MaxMagic;
        public float ShieldRatio => Stats.MaxShield <= 0f ? 0f : Shield / Stats.MaxShield;
        public float RetreatProgress => RetreatDuration <= 0f ? 0f : 1f - Mathf.Clamp01(RetreatRemaining / RetreatDuration);

        public DemoUnitModel(int id, string displayName, DemoTeam team, DemoUnitRole role, DemoUnitStats stats, Vector3 position)
        {
            Id = id;
            DisplayName = displayName;
            Team = team;
            Role = role;
            Stats = stats.Clone();
            Position = position;
            Destination = position;
            Health = Stats.MaxHealth;
            Magic = Stats.MaxMagic;
            Shield = Stats.MaxShield;
            IsRevealedToPlayer = team == DemoTeam.Player;
        }
    }

    public sealed class DemoCombatModel
    {
        public int Id { get; }
        public Vector3 Center { get; }
        public float ReinforcementRadius { get; }
        public float ForcedRadius { get; }
        public HashSet<int> Participants { get; } = new HashSet<int>();
        public bool IsFinished;

        public DemoCombatModel(int id, Vector3 center, float reinforcementRadius, float forcedRadius)
        {
            Id = id;
            Center = center;
            ReinforcementRadius = reinforcementRadius;
            ForcedRadius = forcedRadius;
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
            float attackMultiplier = 1f)
        {
            float raw = attacker.Stats.Attack * Mathf.Max(0f, attackMultiplier);
            float discoveryChance = attacker.Stats.CoreDiscovery /
                                    Mathf.Max(0.01f, attacker.Stats.CoreDiscovery + target.Stats.CoreConcealment);
            bool coreHit = random.NextDouble() < Mathf.Clamp01(discoveryChance);
            bool critical = !coreHit && random.NextDouble() < Mathf.Clamp01(attacker.Stats.CriticalChance);
            if (coreHit)
                raw *= balance.CoreMultiplier;
            else if (critical)
                raw *= balance.CriticalMultiplier;

            float incoming = Mathf.Max(balance.MinimumDamage, raw - Mathf.Max(0f, target.Stats.Defense));
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

        public DemoCombatModel GetCombat(int id)
        {
            return _combats.FirstOrDefault(combat => combat.Id == id);
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
            if (attacker.Team == DemoTeam.Player && !target.IsRevealedToPlayer)
                return DemoCommandResult.Fail("尚未获得目标情报");

            if (target.CombatId >= 0)
                return RequestReinforcement(new[] { attackerId }, target.CombatId);

            float distance = HorizontalDistance(attacker.Position, target.Position);
            if (distance > attacker.Stats.EngagementRadius)
                return DemoCommandResult.Fail($"目标超出交战半径（{distance:0.0}/{attacker.Stats.EngagementRadius:0.0}）");

            Vector3 center = target.IsFixed ? target.Position : (attacker.Position + target.Position) * 0.5f;
            DemoCombatModel combat = new DemoCombatModel(_nextCombatId++, center, Balance.ReinforcementRadius, Balance.ForcedEngagementRadius);
            _combats.Add(combat);
            AddParticipant(combat, attacker);
            AddParticipant(combat, target);
            Raise($"战斗 #{combat.Id} 爆发：{attacker.DisplayName} 对 {target.DisplayName}", center, true);
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
                    Raise($"{unit.DisplayName} 加入战斗 #{combat.Id}", combat.Center, false);
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
                Raise($"{unit.DisplayName} 开始撤退", unit.Position, true);
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
                TickVisibility();
                TickEnemyEngagement();
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
                        Raise($"{unit.DisplayName} 抵达并加入战斗 #{combat.Id}", combat.Center, false);
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

        private void TickVisibility()
        {
            List<DemoUnitModel> observers = _units.Values.Where(unit => unit.IsAlive && unit.Team == DemoTeam.Player).ToList();
            foreach (DemoUnitModel enemy in _units.Values.Where(unit => unit.IsAlive && unit.Team == DemoTeam.Enemy && !unit.IsRevealedToPlayer))
            {
                DemoUnitModel observer = observers.FirstOrDefault(unit => HorizontalDistance(unit.Position, enemy.Position) <= unit.Stats.VisionRadius);
                if (observer == null)
                    continue;
                enemy.IsRevealedToPlayer = true;
                Raise($"发现敌情：{enemy.DisplayName}", enemy.Position, true);
            }
        }

        private void TickEnemyEngagement()
        {
            foreach (DemoUnitModel enemy in _units.Values.Where(unit => unit.IsAlive && unit.Team == DemoTeam.Enemy && !unit.IsFixed && unit.CombatId < 0).ToList())
            {
                DemoUnitModel target = _units.Values
                    .Where(unit => unit.IsAlive && unit.Team == DemoTeam.Player && unit.CombatId < 0 && SimulationTime >= unit.ProtectedUntil)
                    .OrderBy(unit => HorizontalDistance(enemy.Position, unit.Position))
                    .FirstOrDefault();
                if (target == null)
                    continue;
                float distance = HorizontalDistance(enemy.Position, target.Position);
                if (distance <= enemy.Stats.EngagementRadius && distance <= enemy.Stats.VisionRadius)
                    StartCombat(enemy.Id, target.Id);
            }
        }

        private void TickCombats(float dt)
        {
            foreach (DemoCombatModel combat in _combats.Where(item => !item.IsFinished).ToList())
            {
                ForceNearbyUnitsIntoCombat(combat);
                TickRetreats(combat, dt);

                List<DemoUnitModel> attackers = combat.Participants
                    .Select(GetUnit)
                    .Where(unit => unit != null && unit.IsAlive && unit.Activity == DemoUnitActivity.Fighting)
                    .ToList();

                foreach (DemoUnitModel attacker in attackers)
                {
                    if (attacker.AttackCooldown > 0f)
                        continue;
                    DemoUnitModel target = combat.Participants
                        .Select(GetUnit)
                        .Where(unit => unit != null && unit.IsAlive && unit.Team != attacker.Team)
                        .OrderBy(unit => unit.HealthRatio)
                        .ThenBy(unit => unit.Id)
                        .FirstOrDefault();
                    if (target == null)
                        continue;

                    float shieldBonus = combat.Participants
                        .Select(GetUnit)
                        .Where(unit => unit != null && unit.IsAlive && unit.Team == target.Team)
                        .Sum(unit => unit.Stats.GlobalShieldBonus);
                    DemoDamageResult result = DemoDamageResolver.Resolve(attacker, target, Balance, _random, shieldBonus);
                    attacker.AttackCooldown = Mathf.Max(0.2f, attacker.Stats.AttackInterval);
                    ReportDamage(attacker, target, result, combat.Center, false);
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
                Raise($"{unit.DisplayName} 进入强制交战区", combat.Center, true);
            }
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
                Raise($"{unit.DisplayName} 完成撤退并获得脱战保护", unit.Position, true);
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
            combat.Participants.Add(unit.Id);
            unit.CombatId = combat.Id;
            unit.PendingReinforcementBattleId = -1;
            unit.HasDestination = false;
            unit.Activity = DemoUnitActivity.Fighting;
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
            Raise($"战斗 #{combat.Id} 结束", combat.Center, true);
        }

        private void ReportDamage(DemoUnitModel attacker, DemoUnitModel target, DemoDamageResult result, Vector3 position, bool remote)
        {
            string modifier = result.CoreHit ? "核心命中" : result.Critical ? "暴击" : "命中";
            if (result.Destroyed)
                Raise($"{target.DisplayName} 被{(remote ? "远程打击" : attacker.DisplayName)}击毁", position, true);
            else if (result.CoreHit || target.HealthRatio <= 0.3f)
                Raise($"{attacker.DisplayName} {modifier} {target.DisplayName}，目标生命 {target.Health:0}", position, true);
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

        private void Raise(string message, Vector3 position, bool important)
        {
            EventRaised?.Invoke(new DemoBattleEvent(SimulationTime, message, position, important));
        }
    }
}
