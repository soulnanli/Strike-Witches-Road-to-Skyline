using System.Linq;
using NUnit.Framework;
using UnityEngine;

namespace SWRTS.Demo1.Tests
{
    public sealed class Demo1SimulationTests
    {
        private static DemoUnitStats BasicStats(float attack = 20f)
        {
            return new DemoUnitStats
            {
                MaxHealth = 100f,
                Attack = attack,
                CriticalChance = 0f,
                Defense = 0f,
                MaxMagic = 100f,
                MaxShield = 100f,
                CoreDiscovery = 0f,
                CoreConcealment = 1f,
                AttackInterval = 1f,
                MagicRecovery = 0f,
                Mobility = 1f,
                MoveSpeed = 10f,
                VisionRadius = 30f,
                EngagementRadius = 10f
            };
        }

        [Test]
        public void Damage_ConsumesShieldAndMagicBeforeHealth()
        {
            Demo1Balance balance = new Demo1Balance();
            Demo1Simulation simulation = new Demo1Simulation(balance);
            DemoUnitModel attacker = simulation.AddUnit("attacker", DemoTeam.Player, DemoUnitRole.Witch, BasicStats(30f), Vector3.zero);
            DemoUnitModel target = simulation.AddUnit("target", DemoTeam.Enemy, DemoUnitRole.Witch, BasicStats(), Vector3.right);

            DemoDamageResult result = DemoDamageResolver.Resolve(attacker, target, balance, new System.Random(1));

            Assert.That(result.ShieldDamage, Is.GreaterThan(0f));
            Assert.That(result.HealthDamage, Is.EqualTo(0f).Within(0.001f));
            Assert.That(target.Magic, Is.LessThan(target.Stats.MaxMagic));
            Assert.That(target.Health, Is.EqualTo(target.Stats.MaxHealth));
        }

        [Test]
        public void GroupMove_AssignsIndependentDestinationsInsideArrivalArea()
        {
            Demo1Simulation simulation = new Demo1Simulation();
            DemoUnitModel first = simulation.AddUnit("first", DemoTeam.Player, DemoUnitRole.Witch, BasicStats(), new Vector3(-8f, 0f, 0f));
            DemoUnitModel second = simulation.AddUnit("second", DemoTeam.Player, DemoUnitRole.Witch, BasicStats(), new Vector3(-8f, 0f, 2f));

            DemoCommandResult result = simulation.IssueMove(new[] { first.Id, second.Id }, new Vector3(8f, 0f, 0f));
            for (int i = 0; i < 250; i++) simulation.Advance(0.1f);

            Assert.That(result.Success, Is.True);
            Assert.That(first.HasDestination, Is.False);
            Assert.That(second.HasDestination, Is.False);
            Assert.That(first.Position, Is.Not.EqualTo(second.Position));
            Assert.That(Vector3.Distance(first.Position, new Vector3(8f, 0f, 0f)), Is.LessThanOrEqualTo(simulation.Balance.DestinationRadius));
            Assert.That(Vector3.Distance(second.Position, new Vector3(8f, 0f, 0f)), Is.LessThanOrEqualTo(simulation.Balance.DestinationRadius));
        }

        [Test]
        public void Combat_CreatesInstanceAndAutomaticallyDealsDamage()
        {
            Demo1Simulation simulation = new Demo1Simulation();
            DemoUnitModel attacker = simulation.AddUnit("attacker", DemoTeam.Player, DemoUnitRole.Witch, BasicStats(60f), Vector3.zero);
            DemoUnitModel target = simulation.AddUnit("target", DemoTeam.Enemy, DemoUnitRole.Witch, BasicStats(), new Vector3(2f, 0f, 0f));
            target.IsRevealedToPlayer = true;
            target.Shield = 0f;
            target.Magic = 0f;

            DemoCommandResult result = simulation.StartCombat(attacker.Id, target.Id);
            for (int i = 0; i < 15; i++) simulation.Advance(0.1f);

            Assert.That(result.Success, Is.True);
            Assert.That(simulation.Combats.Count, Is.EqualTo(1));
            Assert.That(target.Health, Is.LessThan(target.Stats.MaxHealth));
        }

        [Test]
        public void Reinforcement_MovesIndependentlyThenJoinsExistingBattle()
        {
            Demo1Simulation simulation = new Demo1Simulation();
            DemoUnitStats harmless = BasicStats(0f);
            DemoUnitModel attacker = simulation.AddUnit("attacker", DemoTeam.Player, DemoUnitRole.Witch, harmless, Vector3.zero);
            DemoUnitModel target = simulation.AddUnit("target", DemoTeam.Enemy, DemoUnitRole.Witch, harmless, new Vector3(2f, 0f, 0f));
            DemoUnitModel reinforcement = simulation.AddUnit("reinforcement", DemoTeam.Player, DemoUnitRole.Witch, harmless, new Vector3(-30f, 0f, 0f));
            target.IsRevealedToPlayer = true;
            simulation.StartCombat(attacker.Id, target.Id);
            DemoCombatModel combat = simulation.Combats.Single();

            DemoCommandResult result = simulation.RequestReinforcement(new[] { reinforcement.Id }, combat.Id);
            for (int i = 0; i < 300 && reinforcement.CombatId < 0; i++) simulation.Advance(0.1f);

            Assert.That(result.Success, Is.True);
            Assert.That(reinforcement.CombatId, Is.EqualTo(combat.Id));
            Assert.That(combat.Participants, Does.Contain(reinforcement.Id));
        }

        [Test]
        public void Retreat_IsDelayedAndGrantsDisengageProtection()
        {
            Demo1Simulation simulation = new Demo1Simulation();
            DemoUnitStats harmless = BasicStats(0f);
            DemoUnitModel player = simulation.AddUnit("player", DemoTeam.Player, DemoUnitRole.Witch, harmless, Vector3.zero);
            DemoUnitModel enemy = simulation.AddUnit("enemy", DemoTeam.Enemy, DemoUnitRole.Witch, harmless, Vector3.right * 2f);
            enemy.IsRevealedToPlayer = true;
            simulation.StartCombat(player.Id, enemy.Id);

            DemoCommandResult result = simulation.RequestRetreat(new[] { player.Id });
            Assert.That(player.Activity, Is.EqualTo(DemoUnitActivity.Retreating));
            for (int i = 0; i < 100 && player.CombatId >= 0; i++) simulation.Advance(0.1f);

            Assert.That(result.Success, Is.True);
            Assert.That(player.CombatId, Is.EqualTo(-1));
            Assert.That(player.Activity, Is.EqualTo(DemoUnitActivity.Protected));
            Assert.That(player.ProtectedUntil, Is.GreaterThan(simulation.SimulationTime));
        }

        [Test]
        public void RemoteStrike_IsDelayedAndDoesNotCreateCombat()
        {
            Demo1Balance balance = new Demo1Balance { RemoteStrikeDelay = 0.5f };
            Demo1Simulation simulation = new Demo1Simulation(balance);
            DemoUnitStats artilleryStats = BasicStats(45f);
            artilleryStats.CanRemoteStrike = true;
            DemoUnitModel artillery = simulation.AddUnit("artillery", DemoTeam.Player, DemoUnitRole.Artillery, artilleryStats, Vector3.zero);
            DemoUnitModel target = simulation.AddUnit("target", DemoTeam.Enemy, DemoUnitRole.Witch, BasicStats(), new Vector3(20f, 0f, 0f));
            target.Shield = 0f;
            target.Magic = 0f;

            DemoCommandResult result = simulation.ScheduleRemoteStrike(artillery.Id, target.Position);
            simulation.Advance(0.2f);
            Assert.That(target.Health, Is.EqualTo(target.Stats.MaxHealth));
            for (int i = 0; i < 10; i++) simulation.Advance(0.1f);

            Assert.That(result.Success, Is.True);
            Assert.That(target.Health, Is.LessThan(target.Stats.MaxHealth));
            Assert.That(simulation.Combats, Is.Empty);
            Assert.That(artillery.CombatId, Is.EqualTo(-1));
        }

        [Test]
        public void Fortress_CannotMoveOrRetreat()
        {
            Demo1Simulation simulation = new Demo1Simulation();
            DemoUnitModel fortress = simulation.AddUnit("fortress", DemoTeam.Player, DemoUnitRole.Fortress, BasicStats(), Vector3.zero);

            DemoCommandResult move = simulation.IssueMove(new[] { fortress.Id }, Vector3.right * 10f);
            DemoCommandResult retreat = simulation.RequestRetreat(new[] { fortress.Id });

            Assert.That(move.Success, Is.False);
            Assert.That(retreat.Success, Is.False);
            Assert.That(fortress.Position, Is.EqualTo(Vector3.zero));
        }

        [Test]
        public void BattleLines_HaveUnlimitedCapacityAndKeepPreferredLine()
        {
            Demo1Simulation simulation = new Demo1Simulation();
            DemoUnitStats harmless = BasicStats(0f);
            DemoUnitModel attacker = simulation.AddUnit("attacker", DemoTeam.Player, DemoUnitRole.Witch, harmless, Vector3.zero);
            DemoUnitModel enemy = simulation.AddUnit("enemy", DemoTeam.Enemy, DemoUnitRole.Guard, harmless, Vector3.right * 2f);
            enemy.IsRevealedToPlayer = true;
            simulation.StartCombat(attacker.Id, enemy.Id);
            DemoCombatModel combat = simulation.Combats.Single();

            DemoUnitModel[] reinforcements = Enumerable.Range(0, 6)
                .Select(index => simulation.AddUnit($"reinforcement-{index}", DemoTeam.Player, DemoUnitRole.Witch, harmless, combat.Center))
                .ToArray();
            simulation.RequestReinforcement(reinforcements.Select(unit => unit.Id), combat.Id);
            DemoUnitStats mainStats = harmless.Clone();
            mainStats.PreferredBattleLine = DemoBattleLine.Main;
            DemoUnitModel mainUnit = simulation.AddUnit("main-reinforcement", DemoTeam.Player, DemoUnitRole.Artillery, mainStats, combat.Center);
            simulation.RequestReinforcement(new[] { mainUnit.Id }, combat.Id);

            Assert.That(combat.Participants.Select(simulation.GetUnit).Count(unit => unit.Team == DemoTeam.Player && combat.GetAssignment(unit.Id).Line == DemoBattleLine.Vanguard), Is.EqualTo(7));
            Assert.That(simulation.RequestBattleLineChange(mainUnit.Id, DemoBattleLine.Vanguard).Success, Is.True);
            Assert.That(combat.Participants.Select(simulation.GetUnit).Count(unit => unit.Team == DemoTeam.Player && combat.GetAssignment(unit.Id).Line == DemoBattleLine.Vanguard), Is.EqualTo(8));
            Assert.That(combat.Participants.Select(simulation.GetUnit).Any(unit => unit.Team == DemoTeam.Player && combat.GetAssignment(unit.Id).Line != DemoBattleLine.Vanguard), Is.False);
        }

        [Test]
        public void BattleLineChange_IsDelayedAndRejectsFixedTargets()
        {
            Demo1Simulation simulation = new Demo1Simulation();
            DemoUnitStats harmless = BasicStats(0f);
            DemoUnitModel player = simulation.AddUnit("player", DemoTeam.Player, DemoUnitRole.Witch, harmless, Vector3.zero);
            DemoUnitStats fortressStats = harmless.Clone();
            fortressStats.PreferredBattleLine = DemoBattleLine.Support;
            DemoUnitModel fortress = simulation.AddUnit("fortress", DemoTeam.Enemy, DemoUnitRole.Fortress, fortressStats, Vector3.right * 2f);
            fortress.IsRevealedToPlayer = true;
            simulation.StartCombat(player.Id, fortress.Id);
            DemoCombatModel combat = simulation.Combats.Single();

            DemoCommandResult move = simulation.RequestBattleLineChange(player.Id, DemoBattleLine.Main);
            DemoCommandResult fixedMove = simulation.RequestBattleLineChange(fortress.Id, DemoBattleLine.Main);

            Assert.That(move.Success, Is.True);
            Assert.That(combat.GetAssignment(player.Id).Line, Is.EqualTo(DemoBattleLine.Main));
            Assert.That(combat.GetAssignment(player.Id).IsRepositioning, Is.True);
            simulation.Advance(simulation.Balance.BattleLineChangeDuration + 0.1f);
            Assert.That(combat.GetAssignment(player.Id).IsRepositioning, Is.False);
            Assert.That(fixedMove.Success, Is.False);
        }

        [Test]
        public void Screening_BlocksStandardAndFullyScreenedPiercingAttacks()
        {
            Demo1Simulation simulation = new Demo1Simulation();
            DemoUnitStats piercing = BasicStats(10f);
            piercing.AttackProfile = DemoAttackProfile.ScreenPiercing;
            piercing.ScreenPenetration = 1f;
            DemoUnitModel attacker = simulation.AddUnit("piercer", DemoTeam.Player, DemoUnitRole.Artillery, piercing, Vector3.zero);

            DemoUnitStats screenStats = BasicStats(0f);
            screenStats.PreferredBattleLine = DemoBattleLine.Vanguard;
            screenStats.ScreenPower = 1f;
            DemoUnitModel screen = simulation.AddUnit("screen", DemoTeam.Enemy, DemoUnitRole.Guard, screenStats, Vector3.right * 2f);
            screen.IsRevealedToPlayer = true;
            simulation.StartCombat(attacker.Id, screen.Id);
            DemoCombatModel combat = simulation.Combats.Single();

            DemoUnitStats rearStats = BasicStats(0f);
            rearStats.PreferredBattleLine = DemoBattleLine.Support;
            DemoUnitModel rear = simulation.AddUnit("rear", DemoTeam.Enemy, DemoUnitRole.Support, rearStats, combat.Center);
            rear.Shield = 0f;
            rear.Magic = 0f;
            simulation.RequestReinforcement(new[] { rear.Id }, combat.Id);
            float rearHealth = rear.Health;

            Assert.That(simulation.GetScreeningEfficiency(combat.Id, DemoTeam.Enemy), Is.EqualTo(1f).Within(0.001f));
            simulation.Advance(1.1f);
            Assert.That(rear.Health, Is.EqualTo(rearHealth), "Full screening must block even a 100% base piercing attack.");
        }

        [Test]
        public void ScreeningLoss_ExposesRearLineToPiercingAttack()
        {
            Demo1Simulation simulation = new Demo1Simulation();
            DemoUnitStats piercing = BasicStats(25f);
            piercing.AttackProfile = DemoAttackProfile.ScreenPiercing;
            piercing.ScreenPenetration = 1f;
            DemoUnitModel attacker = simulation.AddUnit("piercer", DemoTeam.Player, DemoUnitRole.Scout, piercing, Vector3.zero);

            DemoUnitStats screenStats = BasicStats(0f);
            screenStats.PreferredBattleLine = DemoBattleLine.Vanguard;
            screenStats.ScreenPower = 1f;
            DemoUnitModel screen = simulation.AddUnit("screen", DemoTeam.Enemy, DemoUnitRole.Guard, screenStats, Vector3.right * 2f);
            screen.IsRevealedToPlayer = true;
            simulation.StartCombat(attacker.Id, screen.Id);
            DemoCombatModel combat = simulation.Combats.Single();

            DemoUnitStats rearStats = BasicStats(0f);
            rearStats.PreferredBattleLine = DemoBattleLine.Support;
            DemoUnitModel rear = simulation.AddUnit("rear", DemoTeam.Enemy, DemoUnitRole.Support, rearStats, combat.Center);
            rear.Shield = 0f;
            rear.Magic = 0f;
            simulation.RequestReinforcement(new[] { rear.Id }, combat.Id);
            screen.Health = 0f;
            screen.Activity = DemoUnitActivity.Destroyed;
            float rearHealth = rear.Health;

            simulation.Advance(1.1f);

            Assert.That(simulation.GetScreeningEfficiency(combat.Id, DemoTeam.Enemy), Is.EqualTo(0f));
            Assert.That(rear.Health, Is.LessThan(rearHealth));
        }

        [Test]
        public void BattleLines_ApplyDistinctAttackDefenseAndSupportTradeoffs()
        {
            Demo1Simulation simulation = new Demo1Simulation();

            Assert.That(simulation.GetBattleLineAttackMultiplier(DemoBattleLine.Vanguard), Is.EqualTo(0.9f).Within(0.001f));
            Assert.That(simulation.GetBattleLineDamageTakenMultiplier(DemoBattleLine.Vanguard), Is.EqualTo(0.85f).Within(0.001f));
            Assert.That(simulation.GetBattleLineAttackMultiplier(DemoBattleLine.Main), Is.EqualTo(1.15f).Within(0.001f));
            Assert.That(simulation.GetBattleLineAttackMultiplier(DemoBattleLine.Support), Is.EqualTo(0.8f).Within(0.001f));
            Assert.That(simulation.Balance.SupportEffectMultiplier, Is.EqualTo(1.5f).Within(0.001f));
        }

        [Test]
        public void Witch_PrioritizesScreenPiercingThreatOnExposedLine()
        {
            Demo1Simulation simulation = new Demo1Simulation();
            DemoUnitStats harmless = BasicStats(0f);
            DemoUnitModel witch = simulation.AddUnit("witch", DemoTeam.Player, DemoUnitRole.Witch, BasicStats(10f), Vector3.zero);
            DemoUnitModel guard = simulation.AddUnit("guard", DemoTeam.Enemy, DemoUnitRole.Guard, harmless, Vector3.right * 2f);
            guard.IsRevealedToPlayer = true;
            simulation.StartCombat(witch.Id, guard.Id);
            DemoCombatModel combat = simulation.Combats.Single();
            DemoUnitStats piercing = harmless.Clone();
            piercing.AttackProfile = DemoAttackProfile.ScreenPiercing;
            DemoUnitModel artillery = simulation.AddUnit("artillery", DemoTeam.Enemy, DemoUnitRole.Artillery, piercing, combat.Center);
            simulation.RequestReinforcement(new[] { artillery.Id }, combat.Id);

            simulation.Advance(0.1f);

            Assert.That(combat.GetAssignment(witch.Id).LastTargetId, Is.EqualTo(artillery.Id));
        }

        [Test]
        public void Artillery_EveryThirdAttackUsesCalibratedSalvo()
        {
            Demo1Simulation simulation = new Demo1Simulation();
            DemoUnitStats artilleryStats = BasicStats(10f);
            artilleryStats.PreferredBattleLine = DemoBattleLine.Main;
            DemoUnitModel artillery = simulation.AddUnit("artillery", DemoTeam.Player, DemoUnitRole.Artillery, artilleryStats, Vector3.zero);
            DemoUnitStats targetStats = BasicStats(0f);
            targetStats.MaxHealth = 1000f;
            DemoUnitModel target = simulation.AddUnit("target", DemoTeam.Enemy, DemoUnitRole.Guard, targetStats, Vector3.right * 2f);
            target.IsRevealedToPlayer = true;
            target.Shield = 0f;
            target.Magic = 0f;
            simulation.StartCombat(artillery.Id, target.Id);

            float before = target.Health;
            simulation.Advance(0.1f);
            float firstDamage = before - target.Health;
            before = target.Health;
            simulation.Advance(1f);
            float secondDamage = before - target.Health;
            before = target.Health;
            simulation.Advance(1f);
            float thirdDamage = before - target.Health;

            Assert.That(secondDamage, Is.EqualTo(firstDamage).Within(0.01f));
            Assert.That(thirdDamage, Is.GreaterThan(firstDamage * 1.4f));
        }

        [Test]
        public void TeamTraits_ApplyToHolderAndAlliesOnlyWhileProviderIsActive()
        {
            Demo1Simulation simulation = new Demo1Simulation();
            DemoUnitStats sakamotoStats = BasicStats(0f);
            sakamotoStats.CoreDiscovery = 0.2f;
            sakamotoStats.Traits = DemoUnitTrait.SakamotoCoreInsight;
            DemoUnitModel sakamoto = simulation.AddUnit("sakamoto", DemoTeam.Player, DemoUnitRole.Witch, sakamotoStats, Vector3.zero);
            DemoUnitModel enemy = simulation.AddUnit("enemy", DemoTeam.Enemy, DemoUnitRole.Guard, BasicStats(0f), Vector3.right * 2f);
            enemy.IsRevealedToPlayer = true;
            simulation.StartCombat(sakamoto.Id, enemy.Id);
            DemoCombatModel combat = simulation.Combats.Single();

            DemoUnitStats allyStats = BasicStats(0f);
            allyStats.CoreDiscovery = 0.1f;
            DemoUnitModel ally = simulation.AddUnit("ally", DemoTeam.Player, DemoUnitRole.Witch, allyStats, combat.Center);
            simulation.RequestReinforcement(new[] { ally.Id }, combat.Id);

            DemoUnitStats miyafujiStats = BasicStats(0f);
            miyafujiStats.PreferredBattleLine = DemoBattleLine.Support;
            miyafujiStats.Traits = DemoUnitTrait.MiyafujiShieldAura;
            DemoUnitModel miyafuji = simulation.AddUnit("miyafuji", DemoTeam.Player, DemoUnitRole.Support, miyafujiStats, combat.Center);
            simulation.RequestReinforcement(new[] { miyafuji.Id }, combat.Id);

            Assert.That(simulation.GetEffectiveCoreDiscovery(sakamoto.Id), Is.EqualTo(0.4f).Within(0.001f));
            Assert.That(simulation.GetEffectiveCoreDiscovery(ally.Id), Is.EqualTo(0.2f).Within(0.001f));
            Assert.That(simulation.GetEffectiveShieldBonus(combat.Id, miyafuji.Id), Is.EqualTo(0.15f).Within(0.001f));
            Assert.That(simulation.GetEffectiveShieldBonus(combat.Id, ally.Id), Is.EqualTo(0.15f).Within(0.001f));

            Assert.That(simulation.RequestBattleLineChange(sakamoto.Id, DemoBattleLine.Main).Success, Is.True);
            Assert.That(simulation.GetEffectiveCoreDiscovery(sakamoto.Id), Is.EqualTo(0.2f).Within(0.001f));
            Assert.That(simulation.GetEffectiveCoreDiscovery(ally.Id), Is.EqualTo(0.1f).Within(0.001f));

            Assert.That(simulation.RequestBattleLineChange(miyafuji.Id, DemoBattleLine.Main).Success, Is.True);
            Assert.That(simulation.GetEffectiveShieldBonus(combat.Id, miyafuji.Id), Is.EqualTo(0f).Within(0.001f));
            Assert.That(simulation.GetEffectiveShieldBonus(combat.Id, ally.Id), Is.EqualTo(0f).Within(0.001f));
        }

        [Test]
        public void LynetteTrait_IncreasesCriticalAndIntervalButUsesOnlyStandardAttacks()
        {
            Demo1Balance balance = new Demo1Balance
            {
                LynetteCriticalChanceBonus = 0f,
                LynetteAttackIntervalMultiplier = 1f
            };
            Demo1Simulation simulation = new Demo1Simulation(balance);
            DemoUnitStats lynetteStats = BasicStats(10f);
            lynetteStats.PreferredBattleLine = DemoBattleLine.Main;
            lynetteStats.Traits = DemoUnitTrait.LynetteSharpshooter;
            lynetteStats.CanRemoteStrike = false;
            lynetteStats.AttackProfile = DemoAttackProfile.Standard;
            lynetteStats.ScreenPenetration = 0f;
            DemoUnitModel lynette = simulation.AddUnit("lynette", DemoTeam.Player, DemoUnitRole.Artillery, lynetteStats, Vector3.zero);
            DemoUnitStats targetStats = BasicStats(0f);
            targetStats.MaxHealth = 1000f;
            DemoUnitModel target = simulation.AddUnit("target", DemoTeam.Enemy, DemoUnitRole.Guard, targetStats, Vector3.right * 2f);
            target.IsRevealedToPlayer = true;
            target.Shield = 0f;
            target.Magic = 0f;
            simulation.StartCombat(lynette.Id, target.Id);

            float before = target.Health;
            simulation.Advance(0.1f);
            float firstDamage = before - target.Health;
            before = target.Health;
            simulation.Advance(1f);
            float secondDamage = before - target.Health;
            before = target.Health;
            simulation.Advance(1f);
            float thirdDamage = before - target.Health;

            Assert.That(secondDamage, Is.EqualTo(firstDamage).Within(0.01f));
            Assert.That(thirdDamage, Is.EqualTo(firstDamage).Within(0.01f), "Lynette's third shot must not use the artillery salvo multiplier.");

            Demo1Simulation effectiveStatsSimulation = new Demo1Simulation();
            DemoUnitStats effectiveStats = BasicStats();
            effectiveStats.CriticalChance = 0.12f;
            effectiveStats.AttackInterval = 1.6f;
            effectiveStats.Traits = DemoUnitTrait.LynetteSharpshooter;
            DemoUnitModel effectiveLynette = effectiveStatsSimulation.AddUnit(
                "effective-lynette", DemoTeam.Player, DemoUnitRole.Artillery, effectiveStats, Vector3.zero);
            Assert.That(effectiveStatsSimulation.GetEffectiveCriticalChance(effectiveLynette.Id), Is.EqualTo(0.3f).Within(0.001f));
            Assert.That(effectiveStatsSimulation.GetEffectiveAttackInterval(effectiveLynette.Id), Is.EqualTo(2.2f).Within(0.001f));
        }

        [Test]
        public void Scout_MarksTargetAndAmplifiesFollowingAlliedAttack()
        {
            Demo1Simulation simulation = new Demo1Simulation();
            DemoUnitStats scoutStats = BasicStats(0f);
            scoutStats.PreferredBattleLine = DemoBattleLine.Main;
            DemoUnitModel scout = simulation.AddUnit("scout", DemoTeam.Player, DemoUnitRole.Scout, scoutStats, Vector3.zero);
            DemoUnitStats targetStats = BasicStats(0f);
            targetStats.MaxHealth = 1000f;
            DemoUnitModel target = simulation.AddUnit("target", DemoTeam.Enemy, DemoUnitRole.Guard, targetStats, Vector3.right * 2f);
            target.IsRevealedToPlayer = true;
            target.Shield = 0f;
            target.Magic = 0f;
            simulation.StartCombat(scout.Id, target.Id);
            DemoCombatModel combat = simulation.Combats.Single();
            DemoUnitStats allyStats = BasicStats(10f);
            allyStats.PreferredBattleLine = DemoBattleLine.Main;
            DemoUnitModel ally = simulation.AddUnit("ally", DemoTeam.Player, DemoUnitRole.Witch, allyStats, combat.Center);
            simulation.RequestReinforcement(new[] { ally.Id }, combat.Id);

            float before = target.Health;
            simulation.Advance(0.1f);
            float totalDamage = before - target.Health;

            Assert.That(simulation.GetMarkRemaining(combat.Id, target.Id), Is.GreaterThan(3.8f));
            Assert.That(totalDamage, Is.GreaterThan(12f), "The allied attack in the same combat tick should consume the scout's mark bonus.");
        }

        [Test]
        public void Support_PulsesOnlyWhileActiveOnSupportLine()
        {
            Demo1Balance balance = new Demo1Balance { SupportPulseInterval = 0.5f };
            Demo1Simulation simulation = new Demo1Simulation(balance);
            DemoUnitStats supportStats = BasicStats(0f);
            supportStats.PreferredBattleLine = DemoBattleLine.Support;
            supportStats.GlobalShieldBonus = 0.25f;
            DemoUnitModel support = simulation.AddUnit("support", DemoTeam.Player, DemoUnitRole.Support, supportStats, Vector3.zero);
            DemoUnitModel enemy = simulation.AddUnit("enemy", DemoTeam.Enemy, DemoUnitRole.Guard, BasicStats(0f), Vector3.right * 2f);
            enemy.IsRevealedToPlayer = true;
            simulation.StartCombat(support.Id, enemy.Id);
            DemoCombatModel combat = simulation.Combats.Single();
            DemoUnitModel ally = simulation.AddUnit("ally", DemoTeam.Player, DemoUnitRole.Witch, BasicStats(0f), combat.Center);
            ally.Shield = 0f;
            ally.Magic = 0f;
            simulation.RequestReinforcement(new[] { ally.Id }, combat.Id);

            simulation.Advance(0.6f);
            Assert.That(ally.Shield, Is.GreaterThan(0f));
            Assert.That(ally.Magic, Is.GreaterThan(0f));

            simulation.RequestBattleLineChange(support.Id, DemoBattleLine.Main);
            simulation.Advance(balance.BattleLineChangeDuration + 0.1f);
            ally.Shield = 0f;
            ally.Magic = 0f;
            simulation.Advance(0.6f);
            Assert.That(ally.Shield, Is.EqualTo(0f));
            Assert.That(ally.Magic, Is.EqualTo(0f));
        }

        [Test]
        public void Guard_InterceptsSuccessfulRearLinePenetration()
        {
            Demo1Balance balance = new Demo1Balance { GuardInterceptionChance = 1f };
            Demo1Simulation simulation = new Demo1Simulation(balance);
            DemoUnitStats piercing = BasicStats(10f);
            piercing.PreferredBattleLine = DemoBattleLine.Main;
            piercing.AttackProfile = DemoAttackProfile.ScreenPiercing;
            piercing.ScreenPenetration = 1f;
            DemoUnitModel attacker = simulation.AddUnit("piercer", DemoTeam.Player, DemoUnitRole.Artillery, piercing, Vector3.zero);
            DemoUnitStats guardStats = BasicStats(0f);
            guardStats.ScreenPower = 0f;
            DemoUnitModel guard = simulation.AddUnit("guard", DemoTeam.Enemy, DemoUnitRole.Guard, guardStats, Vector3.right * 2f);
            guard.IsRevealedToPlayer = true;
            simulation.StartCombat(attacker.Id, guard.Id);
            DemoCombatModel combat = simulation.Combats.Single();
            DemoUnitStats rearStats = BasicStats(0f);
            rearStats.PreferredBattleLine = DemoBattleLine.Support;
            DemoUnitModel rear = simulation.AddUnit("rear", DemoTeam.Enemy, DemoUnitRole.Support, rearStats, combat.Center);
            simulation.RequestReinforcement(new[] { rear.Id }, combat.Id);
            string interceptEvent = null;
            simulation.EventRaised += item =>
            {
                if (item.Message.Contains("拦截")) interceptEvent = item.Message;
            };

            simulation.Advance(0.1f);

            Assert.That(combat.GetAssignment(attacker.Id).LastTargetId, Is.EqualTo(guard.Id));
            Assert.That(interceptEvent, Does.Contain("拦截"));
        }

        [Test]
        public void Fortress_EntersFasterEmergencyBarrageBelowHalfHealth()
        {
            Demo1Simulation simulation = new Demo1Simulation();
            DemoUnitModel attacker = simulation.AddUnit("attacker", DemoTeam.Player, DemoUnitRole.Witch, BasicStats(0f), Vector3.zero);
            DemoUnitStats fortressStats = BasicStats(20f);
            fortressStats.MaxHealth = 500f;
            fortressStats.PreferredBattleLine = DemoBattleLine.Support;
            DemoUnitModel fortress = simulation.AddUnit("fortress", DemoTeam.Enemy, DemoUnitRole.Fortress, fortressStats, Vector3.right * 2f);
            fortress.IsRevealedToPlayer = true;
            fortress.Health = fortress.Stats.MaxHealth * 0.49f;
            simulation.StartCombat(attacker.Id, fortress.Id);

            simulation.Advance(0.1f);

            Assert.That(simulation.Combats.Single().GetAssignment(fortress.Id).FortressBarrageAnnounced, Is.True);
            Assert.That(fortress.AttackCooldown, Is.EqualTo(fortress.Stats.AttackInterval * simulation.Balance.FortressBarrageIntervalMultiplier).Within(0.01f));
        }

        [Test]
        public void OrdinaryWitch_ObservesOnlyInsideForwardVisualSector()
        {
            Demo1Simulation simulation = new Demo1Simulation();
            DemoUnitStats observerStats = BasicStats(0f);
            observerStats.WitchVisionType = DemoWitchVisionType.Ordinary;
            observerStats.VisionRadius = 20f;
            observerStats.VisionAngle = 90f;
            DemoUnitModel observer = simulation.AddUnit("ordinary", DemoTeam.Player, DemoUnitRole.Witch, observerStats, Vector3.zero);
            observer.Facing = Vector3.right;
            DemoUnitStats enemyStats = BasicStats(0f);
            enemyStats.EngagementRadius = 0f;
            DemoUnitModel front = simulation.AddUnit("front", DemoTeam.Enemy, DemoUnitRole.Guard, enemyStats, Vector3.right * 12f);
            DemoUnitModel behind = simulation.AddUnit("behind", DemoTeam.Enemy, DemoUnitRole.Guard, enemyStats, Vector3.left * 12f);

            simulation.Advance(0.6f);

            Assert.That(front.PlayerIntelLevel, Is.GreaterThanOrEqualTo(DemoIntelLevel.Identified));
            Assert.That(front.IsCurrentlyObservedByPlayer, Is.True);
            Assert.That(behind.PlayerIntelLevel, Is.EqualTo(DemoIntelLevel.Unknown));
            observer.Facing = Vector3.left;
            simulation.Advance(0.6f);
            Assert.That(behind.PlayerIntelLevel, Is.GreaterThanOrEqualTo(DemoIntelLevel.Identified));
        }

        [Test]
        public void NightWitch_ObservesEveryDirectionInsideCircularArea()
        {
            Demo1Simulation simulation = new Demo1Simulation();
            DemoUnitStats observerStats = BasicStats(0f);
            observerStats.WitchVisionType = DemoWitchVisionType.Night;
            observerStats.VisionRadius = 15f;
            DemoUnitModel observer = simulation.AddUnit("night", DemoTeam.Player, DemoUnitRole.Witch, observerStats, Vector3.zero);
            observer.Facing = Vector3.right;
            DemoUnitStats enemyStats = BasicStats(0f);
            enemyStats.EngagementRadius = 0f;
            DemoUnitModel front = simulation.AddUnit("front", DemoTeam.Enemy, DemoUnitRole.Guard, enemyStats, Vector3.right * 12f);
            DemoUnitModel behind = simulation.AddUnit("behind", DemoTeam.Enemy, DemoUnitRole.Guard, enemyStats, Vector3.left * 12f);
            DemoUnitModel outside = simulation.AddUnit("outside", DemoTeam.Enemy, DemoUnitRole.Guard, enemyStats, Vector3.forward * 18f);

            simulation.Advance(0.6f);

            Assert.That(front.PlayerIntelLevel, Is.GreaterThanOrEqualTo(DemoIntelLevel.Identified));
            Assert.That(behind.PlayerIntelLevel, Is.GreaterThanOrEqualTo(DemoIntelLevel.Identified));
            Assert.That(outside.PlayerIntelLevel, Is.EqualTo(DemoIntelLevel.Unknown));
        }

        [Test]
        public void LostContact_FreezesLastKnownPositionAndDecaysToUnknown()
        {
            Demo1Balance balance = new Demo1Balance
            {
                VisionIdentificationDuration = 0.1f,
                VisionAssessmentDuration = 0.1f,
                AssessedIntelMemoryDuration = 0.3f,
                IdentifiedIntelMemoryDuration = 0.6f,
                ContactIntelMemoryDuration = 0.9f
            };
            Demo1Simulation simulation = new Demo1Simulation(balance);
            DemoUnitStats observerStats = BasicStats(0f);
            observerStats.WitchVisionType = DemoWitchVisionType.Ordinary;
            observerStats.VisionRadius = 20f;
            observerStats.VisionAngle = 90f;
            DemoUnitModel observer = simulation.AddUnit("ordinary", DemoTeam.Player, DemoUnitRole.Witch, observerStats, Vector3.zero);
            observer.Facing = Vector3.right;
            DemoUnitStats enemyStats = BasicStats(0f);
            enemyStats.EngagementRadius = 0f;
            DemoUnitModel enemy = simulation.AddUnit("enemy", DemoTeam.Enemy, DemoUnitRole.Guard, enemyStats, Vector3.right * 10f);

            simulation.Advance(0.3f);
            Assert.That(enemy.PlayerIntelLevel, Is.EqualTo(DemoIntelLevel.Assessed));
            Vector3 lastKnown = enemy.LastKnownPosition;
            enemy.Position = Vector3.left * 10f;
            simulation.Advance(0.4f);
            Assert.That(enemy.PlayerIntelLevel, Is.EqualTo(DemoIntelLevel.Identified));
            Assert.That(enemy.PlayerVisiblePosition, Is.EqualTo(lastKnown));
            simulation.Advance(0.3f);
            Assert.That(enemy.PlayerIntelLevel, Is.EqualTo(DemoIntelLevel.Contact));
            Assert.That(enemy.CanBeDirectlyTargetedByPlayer, Is.False);
            simulation.Advance(0.3f);
            Assert.That(enemy.PlayerIntelLevel, Is.EqualTo(DemoIntelLevel.Unknown));
            Assert.That(enemy.HasPlayerIntel, Is.False);
        }

        [Test]
        public void PersistentMissionIntel_RemainsIdentifiedOutsideVision()
        {
            Demo1Balance balance = new Demo1Balance { ContactIntelMemoryDuration = 0.2f };
            Demo1Simulation simulation = new Demo1Simulation(balance);
            DemoUnitModel objective = simulation.AddUnit("objective", DemoTeam.Enemy, DemoUnitRole.Fortress, BasicStats(0f), Vector3.right * 30f);

            simulation.GrantPersistentPlayerIntel(objective.Id);
            simulation.Advance(1f);

            Assert.That(objective.PlayerIntelLevel, Is.GreaterThanOrEqualTo(DemoIntelLevel.Identified));
            Assert.That(objective.HasPlayerIntel, Is.True);
            Assert.That(objective.CanBeDirectlyTargetedByPlayer, Is.True);
        }

        [Test]
        public void ScoutAi_PatrolsConfiguredRouteWithoutAVisibleTarget()
        {
            Demo1Simulation simulation = new Demo1Simulation();
            DemoUnitStats scoutStats = BasicStats(0f);
            scoutStats.VisionRadius = 5f;
            DemoUnitModel scout = simulation.AddUnit("scout", DemoTeam.Enemy, DemoUnitRole.Scout, scoutStats, Vector3.zero);
            simulation.AddUnit("distant-player", DemoTeam.Player, DemoUnitRole.Witch, BasicStats(0f), Vector3.right * 30f);

            DemoCommandResult configured = simulation.ConfigureScoutAi(scout.Id, new[]
            {
                Vector3.zero,
                Vector3.right * 10f
            });
            simulation.Advance(0.7f);

            Assert.That(configured.Success, Is.True);
            Assert.That(scout.EnemyAiState, Is.EqualTo(DemoEnemyAiState.Patrol));
            Assert.That(scout.Position.x, Is.GreaterThan(0f));
            Assert.That(scout.Destination, Is.EqualTo(Vector3.right * 10f));
        }

        [Test]
        public void ScoutAi_UsesOwnVisionThenInvestigatesItsLastKnownPosition()
        {
            Demo1Simulation simulation = new Demo1Simulation();
            DemoUnitStats scoutStats = BasicStats(0f);
            scoutStats.VisionRadius = 15f;
            scoutStats.EngagementRadius = 2f;
            DemoUnitModel scout = simulation.AddUnit("scout", DemoTeam.Enemy, DemoUnitRole.Scout, scoutStats, Vector3.zero);
            DemoUnitModel player = simulation.AddUnit("player", DemoTeam.Player, DemoUnitRole.Witch, BasicStats(0f), Vector3.right * 10f);
            simulation.ConfigureScoutAi(scout.Id, new[] { Vector3.zero });

            simulation.Advance(0.1f);
            Assert.That(scout.EnemyAiState, Is.EqualTo(DemoEnemyAiState.Pursue));
            Assert.That(scout.EnemyAiTargetId, Is.EqualTo(player.Id));
            Assert.That(scout.EnemyAiLastKnownPosition, Is.EqualTo(Vector3.right * 10f));

            player.Position = Vector3.right * 30f;
            simulation.Advance(0.6f);

            Assert.That(scout.EnemyAiState, Is.EqualTo(DemoEnemyAiState.Investigate));
            Assert.That(scout.EnemyAiTargetId, Is.EqualTo(-1));
            Assert.That(scout.Destination, Is.EqualTo(Vector3.right * 10f));
        }

        [Test]
        public void EnemyAi_DoesNotShareAnotherUnitsContact()
        {
            Demo1Simulation simulation = new Demo1Simulation();
            DemoUnitStats scoutStats = BasicStats(0f);
            scoutStats.VisionRadius = 20f;
            scoutStats.EngagementRadius = 1f;
            DemoUnitStats guardStats = BasicStats(0f);
            guardStats.VisionRadius = 6f;
            guardStats.EngagementRadius = 1f;
            DemoUnitModel scout = simulation.AddUnit("scout", DemoTeam.Enemy, DemoUnitRole.Scout, scoutStats, Vector3.zero);
            DemoUnitModel guard = simulation.AddUnit("guard", DemoTeam.Enemy, DemoUnitRole.Guard, guardStats, Vector3.left * 15f);
            DemoUnitModel player = simulation.AddUnit("player", DemoTeam.Player, DemoUnitRole.Witch, BasicStats(0f), Vector3.right * 10f);
            simulation.ConfigureScoutAi(scout.Id, new[] { Vector3.zero });
            simulation.ConfigureCombatAi(guard.Id, guard.Position);

            simulation.Advance(0.1f);

            Assert.That(scout.EnemyAiTargetId, Is.EqualTo(player.Id));
            Assert.That(guard.EnemyAiTargetId, Is.EqualTo(-1));
            Assert.That(guard.EnemyAiState, Is.EqualTo(DemoEnemyAiState.Guard));
            Assert.That(guard.HasDestination, Is.False);
        }

        [Test]
        public void ScoutAi_RetreatsFromCombatBelowHealthThreshold()
        {
            Demo1Simulation simulation = new Demo1Simulation();
            DemoUnitStats harmless = BasicStats(0f);
            DemoUnitModel scout = simulation.AddUnit("scout", DemoTeam.Enemy, DemoUnitRole.Scout, harmless, Vector3.zero);
            DemoUnitModel player = simulation.AddUnit("player", DemoTeam.Player, DemoUnitRole.Witch, harmless, Vector3.right * 2f);
            simulation.ConfigureScoutAi(scout.Id, new[] { Vector3.zero });
            simulation.StartCombat(scout.Id, player.Id);
            scout.Health = scout.Stats.MaxHealth * 0.2f;

            simulation.Advance(0.1f);

            Assert.That(scout.EnemyAiState, Is.EqualTo(DemoEnemyAiState.Retreating));
            Assert.That(scout.Activity, Is.EqualTo(DemoUnitActivity.Retreating));
            Assert.That(scout.RetreatRemaining, Is.GreaterThan(0f));
        }

        [Test]
        public void CombatAi_PursuesVisibleTargetThenReturnsToItsOwnPost()
        {
            Demo1Simulation simulation = new Demo1Simulation();
            DemoUnitStats guardStats = BasicStats(0f);
            guardStats.VisionRadius = 15f;
            guardStats.EngagementRadius = 2f;
            DemoUnitModel guard = simulation.AddUnit("guard", DemoTeam.Enemy, DemoUnitRole.Guard, guardStats, Vector3.zero);
            DemoUnitModel player = simulation.AddUnit("player", DemoTeam.Player, DemoUnitRole.Witch, BasicStats(0f), Vector3.right * 10f);
            simulation.ConfigureCombatAi(guard.Id, guard.Position);

            simulation.Advance(0.6f);
            Assert.That(guard.EnemyAiState, Is.EqualTo(DemoEnemyAiState.Pursue));
            Assert.That(guard.Position.x, Is.GreaterThan(0f));

            player.Position = Vector3.right * 30f;
            simulation.Advance(2f);

            Assert.That(guard.EnemyAiTargetId, Is.EqualTo(-1));
            Assert.That(guard.EnemyAiState, Is.EqualTo(DemoEnemyAiState.Guard));
            Assert.That(Vector3.Distance(guard.Position, guard.EnemyAiHomePosition), Is.LessThanOrEqualTo(simulation.Balance.EnemyAiArrivalRadius));
        }
    }
}
