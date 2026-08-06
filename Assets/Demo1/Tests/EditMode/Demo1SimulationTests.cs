using System.Linq;
using NUnit.Framework;
using UnityEngine;

namespace SWRTS.Demo1.Tests
{
    public sealed class Demo1SimulationTests
    {
        private static DemoUnitStats Stats(float attack = 10f, float range = 4f)
        {
            return new DemoUnitStats
            {
                MaxHealth = 500f,
                Attack = attack,
                CriticalChance = 0f,
                Defense = 0f,
                MaxMagic = 0f,
                MaxShield = 0f,
                CoreDiscovery = 0f,
                CoreConcealment = 1f,
                AttackInterval = 0.2f,
                MoveSpeed = 10f,
                VisionRadius = 30f,
                VisionAngle = 120f,
                WitchVisionType = DemoWitchVisionType.Ordinary,
                EngagementRadius = 12f,
                AttackRange = range
            };
        }

        private static Demo1Simulation Scenario(out DemoUnitModel player, out DemoUnitModel enemy,
            float distance = 3f, float attack = 10f, float range = 4f)
        {
            Demo1Simulation simulation = new Demo1Simulation(new Demo1Balance { RandomSeed = 7 });
            simulation.ConfigureMissionObjective(DemoMissionObjective.DestroyAllEnemies);
            player = simulation.AddUnit("player", DemoTeam.Player, DemoUnitRole.Witch, Stats(attack, range), Vector3.zero);
            enemy = simulation.AddUnit("enemy", DemoTeam.Enemy, DemoUnitRole.Guard, Stats(0f, range), new Vector3(distance, 0f, 0f));
            simulation.GrantPersistentPlayerIntel(enemy.Id);
            return simulation;
        }

        [Test]
        public void ManualAttack_PursuesAndFiresWithoutCreatingBattleState()
        {
            Demo1Simulation simulation = Scenario(out DemoUnitModel player, out DemoUnitModel enemy, 12f, 25f, 3f);

            Assert.That(simulation.RequestAttack(new[] { player.Id }, enemy.Id).Success, Is.True);
            simulation.Advance(1.5f);

            Assert.That(player.LockedTargetId, Is.EqualTo(enemy.Id));
            Assert.That(player.Position.x, Is.GreaterThan(0f));
            Assert.That(enemy.Health, Is.LessThan(enemy.Stats.MaxHealth));
            Assert.That(player.AttacksPerformed, Is.GreaterThan(0));
        }

        [Test]
        public void AttackRange_IsIndependentFromWarningRadius()
        {
            Demo1Simulation simulation = Scenario(out DemoUnitModel player, out DemoUnitModel enemy, 9f, 0f, 2f);
            player.Stats.EngagementRadius = 12f;
            simulation.Advance(0.6f);

            Assert.That(player.LockedTargetId, Is.EqualTo(enemy.Id));
            Assert.That(player.Activity, Is.EqualTo(DemoUnitActivity.Pursuing));
            Assert.That(Vector3.Distance(player.Position, enemy.Position), Is.GreaterThan(player.Stats.AttackRange));
        }

        [Test]
        public void AutoAttack_AcquiresNearestIdentifiedEnemy()
        {
            Demo1Simulation simulation = Scenario(out DemoUnitModel player, out DemoUnitModel farther, 8f, 0f, 2f);
            DemoUnitModel nearer = simulation.AddUnit("nearer", DemoTeam.Enemy, DemoUnitRole.Scout, Stats(0f), new Vector3(4f, 0f, 0f));
            simulation.GrantPersistentPlayerIntel(nearer.Id);
            simulation.Advance(0.1f);

            Assert.That(player.LockedTargetId, Is.EqualTo(nearer.Id));
            Assert.That(player.HasExplicitAttackOrder, Is.False);
            Assert.That(farther.IsAlive, Is.True);
        }

        [Test]
        public void DisablingAutoAttack_ClearsManualLockAndHoldsFire()
        {
            Demo1Simulation simulation = Scenario(out DemoUnitModel player, out DemoUnitModel enemy);
            simulation.RequestAttack(new[] { player.Id }, enemy.Id);

            Assert.That(simulation.SetAutoAttack(new[] { player.Id }, false).Success, Is.True);
            simulation.Advance(1f);

            Assert.That(player.AutoAttackEnabled, Is.False);
            Assert.That(player.LockedTargetId, Is.EqualTo(-1));
            Assert.That(enemy.Health, Is.EqualTo(enemy.Stats.MaxHealth));
        }

        [Test]
        public void MoveOrder_KeepsTargetLockAndFiresWhileMoving()
        {
            Demo1Simulation simulation = Scenario(out DemoUnitModel player, out DemoUnitModel enemy);
            simulation.RequestAttack(new[] { player.Id }, enemy.Id);
            float healthBefore = enemy.Health;

            simulation.IssueMove(new[] { player.Id }, new Vector3(0f, 0f, 10f));
            simulation.Advance(0.1f);

            Assert.That(player.LockedTargetId, Is.EqualTo(enemy.Id));
            Assert.That(player.Activity, Is.EqualTo(DemoUnitActivity.Moving));
            Assert.That(player.HasDestination, Is.True);
            Assert.That(player.Position, Is.Not.EqualTo(Vector3.zero));
            Assert.That(enemy.Health, Is.LessThan(healthBefore));
        }

        [Test]
        public void Pursuit_StartsFiringBeforeMovementStops()
        {
            Demo1Simulation simulation = Scenario(out DemoUnitModel player, out DemoUnitModel enemy, 5f, 20f, 4f);
            simulation.RequestAttack(new[] { player.Id }, enemy.Id);

            simulation.Advance(0.1f);

            Assert.That(player.Activity, Is.EqualTo(DemoUnitActivity.Pursuing));
            Assert.That(player.HasDestination, Is.True);
            Assert.That(player.Position.x, Is.GreaterThan(0f));
            Assert.That(enemy.Health, Is.LessThan(enemy.Stats.MaxHealth));
        }

        [Test]
        public void AutoAttack_CanAcquireAndFireDuringMoveOrder()
        {
            Demo1Simulation simulation = Scenario(out DemoUnitModel player, out DemoUnitModel enemy, 3f, 10f, 4f);
            player.AutoAttackEnabled = false;
            simulation.IssueMove(new[] { player.Id }, new Vector3(10f, 0f, 0f));
            simulation.SetAutoAttack(new[] { player.Id }, true);
            enemy.IsCurrentlyObservedByPlayer = true;
            enemy.PlayerIntelLevel = DemoIntelLevel.Identified;
            float healthBefore = enemy.Health;

            simulation.Advance(0.1f);

            Assert.That(player.LockedTargetId, Is.EqualTo(enemy.Id));
            Assert.That(player.Activity, Is.EqualTo(DemoUnitActivity.Moving));
            Assert.That(player.HasManualMoveOrder, Is.True);
            Assert.That(enemy.Health, Is.LessThan(healthBefore));
        }

        [Test]
        public void ManualAttack_DuringMoveOrderPreservesRouteAndFires()
        {
            Demo1Simulation simulation = Scenario(out DemoUnitModel player, out DemoUnitModel enemy, 3f, 10f, 4f);
            Vector3 destination = new Vector3(10f, 0f, 0f);
            simulation.IssueMove(new[] { player.Id }, destination);
            Vector3 assignedDestination = player.Destination;
            simulation.RequestAttack(new[] { player.Id }, enemy.Id);
            float healthBefore = enemy.Health;

            simulation.Advance(0.1f);

            Assert.That(player.LockedTargetId, Is.EqualTo(enemy.Id));
            Assert.That(player.Activity, Is.EqualTo(DemoUnitActivity.Moving));
            Assert.That(player.HasManualMoveOrder, Is.True);
            Assert.That(player.Destination, Is.EqualTo(assignedDestination));
            Assert.That(enemy.Health, Is.LessThan(healthBefore));
        }

        [Test]
        public void LostManualTarget_IsPursuedToLastKnownPositionThenCleared()
        {
            Demo1Simulation simulation = new Demo1Simulation();
            simulation.ConfigureMissionObjective(DemoMissionObjective.DestroyAllEnemies);
            DemoUnitModel player = simulation.AddUnit("player", DemoTeam.Player, DemoUnitRole.Witch, Stats(0f, 2f), Vector3.zero);
            DemoUnitModel enemy = simulation.AddUnit("enemy", DemoTeam.Enemy, DemoUnitRole.Guard, Stats(0f), new Vector3(5f, 0f, 0f));
            simulation.Advance(0.6f);
            Assert.That(enemy.CanBeDirectlyTargetedByPlayer, Is.True);
            simulation.RequestAttack(new[] { player.Id }, enemy.Id);
            enemy.Position = new Vector3(-25f, 0f, 0f);

            simulation.Advance(1.5f);

            Assert.That(player.LockedTargetId, Is.EqualTo(-1));
            Assert.That(player.Position.x, Is.EqualTo(5f).Within(1.3f));
        }

        [Test]
        public void DestroyedTarget_IsClearedAndAutoAttackCanAcquireAnother()
        {
            Demo1Simulation simulation = Scenario(out DemoUnitModel player, out DemoUnitModel first, 2f, 1000f, 5f);
            DemoUnitModel second = simulation.AddUnit("second", DemoTeam.Enemy, DemoUnitRole.Guard, Stats(0f), new Vector3(4f, 0f, 0f));
            second.Stats.MaxHealth = 5000f;
            second.Health = 5000f;
            simulation.GrantPersistentPlayerIntel(second.Id);
            first.IsCurrentlyObservedByPlayer = true;
            second.IsCurrentlyObservedByPlayer = true;
            simulation.RequestAttack(new[] { player.Id }, first.Id);
            simulation.Advance(0.2f);
            Assert.That(first.IsAlive, Is.False);

            simulation.Advance(0.2f);

            Assert.That(player.LockedTargetId, Is.EqualTo(second.Id));
        }

        [Test]
        public void TraitAuras_AffectSelfAndNearbyAlliesOnly()
        {
            Demo1Simulation simulation = new Demo1Simulation();
            DemoUnitStats sakamotoStats = Stats();
            sakamotoStats.CoreDiscovery = 0.2f;
            sakamotoStats.SupportRadius = 12f;
            sakamotoStats.Traits = DemoUnitTrait.SakamotoCoreInsight;
            DemoUnitModel sakamoto = simulation.AddUnit("Sakamoto", DemoTeam.Player, DemoUnitRole.Witch, sakamotoStats, Vector3.zero);
            DemoUnitModel nearby = simulation.AddUnit("near", DemoTeam.Player, DemoUnitRole.Witch, Stats(), new Vector3(10f, 0f, 0f));
            nearby.Stats.CoreDiscovery = 0.2f;
            DemoUnitModel far = simulation.AddUnit("far", DemoTeam.Player, DemoUnitRole.Witch, Stats(), new Vector3(13f, 0f, 0f));
            far.Stats.CoreDiscovery = 0.2f;

            Assert.That(simulation.GetEffectiveCoreDiscovery(sakamoto.Id), Is.EqualTo(0.4f).Within(0.001f));
            Assert.That(simulation.GetEffectiveCoreDiscovery(nearby.Id), Is.EqualTo(0.4f).Within(0.001f));
            Assert.That(simulation.GetEffectiveCoreDiscovery(far.Id), Is.EqualTo(0.2f).Within(0.001f));
        }

        [Test]
        public void MiyafujiAuraAndSupportPulse_UseSupportRadius()
        {
            Demo1Balance balance = new Demo1Balance { SupportPulseInterval = 0.2f };
            Demo1Simulation simulation = new Demo1Simulation(balance);
            DemoUnitStats supportStats = Stats();
            supportStats.MaxMagic = 100f;
            supportStats.MaxShield = 100f;
            supportStats.SupportRadius = 12f;
            supportStats.Traits = DemoUnitTrait.MiyafujiShieldAura;
            DemoUnitModel miyafuji = simulation.AddUnit("Miyafuji", DemoTeam.Player, DemoUnitRole.Support, supportStats, Vector3.zero);
            DemoUnitStats allyStats = supportStats.Clone();
            allyStats.Traits = DemoUnitTrait.None;
            DemoUnitModel nearby = simulation.AddUnit("near", DemoTeam.Player, DemoUnitRole.Witch, allyStats, new Vector3(10f, 0f, 0f));
            DemoUnitModel far = simulation.AddUnit("far", DemoTeam.Player, DemoUnitRole.Witch, allyStats, new Vector3(13f, 0f, 0f));
            miyafuji.Shield = nearby.Shield = far.Shield = 0f;

            Assert.That(simulation.GetEffectiveShieldBonus(miyafuji.Id), Is.EqualTo(0.15f).Within(0.001f));
            Assert.That(simulation.GetEffectiveShieldBonus(nearby.Id), Is.EqualTo(0.15f).Within(0.001f));
            Assert.That(simulation.GetEffectiveShieldBonus(far.Id), Is.Zero);
            simulation.Advance(0.3f);
            Assert.That(miyafuji.Shield, Is.GreaterThan(0f));
            Assert.That(nearby.Shield, Is.GreaterThan(far.Shield + 5f));
            Assert.That(far.Shield, Is.LessThan(1f), "The distant ally should receive only passive recovery, not the support pulse.");
        }

        [Test]
        public void Artillery_PerformsCalibratedSalvoEveryThirdAttack()
        {
            Demo1Simulation simulation = Scenario(out DemoUnitModel attacker, out DemoUnitModel enemy, 2f, 10f, 5f);
            DemoUnitStats artillery = attacker.Stats;
            simulation.SetAutoAttack(new[] { attacker.Id }, false);
            DemoUnitModel gunner = simulation.AddUnit("gunner", DemoTeam.Player, DemoUnitRole.Artillery, artillery, Vector3.zero);
            simulation.RequestAttack(new[] { gunner.Id }, enemy.Id);
            float start = enemy.Health;
            simulation.Advance(0.1f);
            float first = start - enemy.Health;
            simulation.Advance(0.2f);
            float second = start - first - enemy.Health;
            simulation.Advance(0.2f);
            float third = start - first - second - enemy.Health;

            Assert.That(gunner.AttacksPerformed, Is.EqualTo(3));
            Assert.That(third, Is.GreaterThan(first));
        }

        [Test]
        public void ScoutAttack_MarksTarget()
        {
            Demo1Simulation simulation = Scenario(out DemoUnitModel unused, out DemoUnitModel enemy, 2f, 0f, 5f);
            simulation.SetAutoAttack(new[] { unused.Id }, false);
            DemoUnitModel scout = simulation.AddUnit("scout", DemoTeam.Player, DemoUnitRole.Scout, Stats(5f, 5f), Vector3.zero);
            simulation.RequestAttack(new[] { scout.Id }, enemy.Id);
            simulation.Advance(0.1f);

            Assert.That(simulation.GetMarkRemaining(enemy.Id), Is.GreaterThan(3.8f));
        }

        [Test]
        public void LynetteTrait_AdjustsCriticalChanceAndAttackInterval()
        {
            Demo1Simulation simulation = new Demo1Simulation();
            DemoUnitStats stats = Stats();
            stats.CriticalChance = 0.12f;
            stats.AttackInterval = 1.6f;
            stats.Traits = DemoUnitTrait.LynetteSharpshooter;
            DemoUnitModel lynette = simulation.AddUnit("Lynette", DemoTeam.Player, DemoUnitRole.Artillery, stats, Vector3.zero);

            Assert.That(simulation.GetEffectiveCriticalChance(lynette.Id), Is.EqualTo(0.3f).Within(0.001f));
            Assert.That(simulation.GetEffectiveAttackInterval(lynette.Id), Is.EqualTo(2.2f).Within(0.001f));
        }

        [Test]
        public void RemoteStrike_ResolvesOnMapWithoutLockingUnits()
        {
            Demo1Simulation simulation = Scenario(out DemoUnitModel player, out DemoUnitModel enemy, 10f, 20f, 2f);
            player.Stats.CanRemoteStrike = true;
            player.Stats.Attack = 20f;
            simulation.SetAutoAttack(new[] { player.Id }, false);
            Assert.That(simulation.ScheduleRemoteStrike(player.Id, enemy.Position).Success, Is.True);

            simulation.Advance(simulation.Balance.RemoteStrikeDelay + 0.1f);

            Assert.That(simulation.RemoteStrikes.Single().Resolved, Is.True);
            Assert.That(player.LockedTargetId, Is.EqualTo(-1));
            Assert.That(enemy.Health, Is.LessThan(enemy.Stats.MaxHealth));
        }

        [Test]
        public void ReturnToBase_ClearsTargetAndStartsService()
        {
            Demo1Balance balance = new Demo1Balance { BaseArrivalRadius = 0.2f, BaseTurnaroundDuration = 1f };
            Demo1Simulation simulation = new Demo1Simulation(balance);
            simulation.ConfigureBase(Vector3.zero);
            DemoUnitModel player = simulation.AddUnit("player", DemoTeam.Player, DemoUnitRole.Witch, Stats(), new Vector3(1f, 0f, 0f));
            DemoUnitModel enemy = simulation.AddUnit("enemy", DemoTeam.Enemy, DemoUnitRole.Guard, Stats(), new Vector3(2f, 0f, 0f));
            simulation.GrantPersistentPlayerIntel(enemy.Id);
            simulation.RequestAttack(new[] { player.Id }, enemy.Id);

            Assert.That(simulation.RequestReturnToBase(new[] { player.Id }).Success, Is.True);
            simulation.Advance(0.2f);

            Assert.That(player.LockedTargetId, Is.EqualTo(-1));
            Assert.That(player.DeploymentState, Is.EqualTo(DemoUnitDeploymentState.Servicing));
        }

        [Test]
        public void ScriptableConfigs_SerializeNewRangeFieldsAndNightVision()
        {
            DemoUnitConfig sakamoto = Resources.Load<DemoUnitConfig>("Configs/Units/Sakamoto");
            DemoUnitConfig miyafuji = Resources.Load<DemoUnitConfig>("Configs/Units/Miyafuji");
            DemoUnitConfig sanya = Resources.Load<DemoUnitConfig>("Configs/Units/Sanya");

            Assert.That(sakamoto.Stats.AttackRange, Is.EqualTo(8f));
            Assert.That(sakamoto.Stats.SupportRadius, Is.EqualTo(12f));
            Assert.That(miyafuji.Stats.SupportRadius, Is.EqualTo(12f));
            Assert.That(sanya.Stats.WitchVisionType, Is.EqualTo(DemoWitchVisionType.Night));
            Assert.That(sanya.Stats.VisionRadius, Is.EqualTo(48f));
        }
    }
}
