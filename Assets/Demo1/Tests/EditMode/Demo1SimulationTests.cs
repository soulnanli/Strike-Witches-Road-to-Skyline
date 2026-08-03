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
        public void BattleLines_AssignPreferredSlotsAndOverflowToReserve()
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

            Assert.That(combat.Participants.Select(simulation.GetUnit).Count(unit => unit.Team == DemoTeam.Player && combat.GetAssignment(unit.Id).Line == DemoBattleLine.Vanguard), Is.EqualTo(2));
            Assert.That(combat.Participants.Select(simulation.GetUnit).Count(unit => unit.Team == DemoTeam.Player && combat.GetAssignment(unit.Id).Line == DemoBattleLine.Main), Is.EqualTo(2));
            Assert.That(combat.Participants.Select(simulation.GetUnit).Count(unit => unit.Team == DemoTeam.Player && combat.GetAssignment(unit.Id).Line == DemoBattleLine.Support), Is.EqualTo(2));
            Assert.That(combat.Participants.Select(simulation.GetUnit).Count(unit => unit.Team == DemoTeam.Player && combat.GetAssignment(unit.Id).Line == DemoBattleLine.Reserve), Is.EqualTo(1));
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
    }
}
