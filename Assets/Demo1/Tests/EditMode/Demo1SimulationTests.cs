using System.Linq;
using NUnit.Framework;
using UnityEngine;

namespace SWRTS.Demo1.Tests
{
    public sealed class Demo1SimulationTests
    {
        private static Demo1Balance TestBalance()
        {
            return new Demo1Balance
            {
                RandomSeed = 7,
                MinimumHitChance = 1f,
                MaximumHitChance = 1f,
                MinimumEvasionChance = 0f,
                MaximumEvasionChance = 0f
            };
        }

        private static DemoUnitStats Stats(float attack = 10f, float range = 4f)
        {
            return new DemoUnitStats
            {
                MaxHealth = 500f,
                Attack = attack,
                CriticalChance = 0f,
                Defense = 0f,
                MaxMagic = 100f,
                MaxShield = 0f,
                CoreDiscovery = 0f,
                CoreConcealment = 1f,
                AttackInterval = 0.2f,
                MagicRecovery = 0f,
                Mobility = 1f,
                MoveSpeed = 10f,
                VisionRadius = 30f,
                VisionAngle = 120f,
                WitchVisionType = DemoWitchVisionType.Ordinary,
                EngagementRadius = 12f,
                OptimalAttackRange = range * 0.75f,
                AttackRange = range,
                BaseAccuracy = 1f,
                Penetration = 20f,
                MagazineSize = 8,
                ReserveAmmo = 32,
                AmmoPerAttack = 1,
                ReloadDuration = 0.3f
            };
        }

        private static Demo1Simulation Scenario(out DemoUnitModel player, out DemoUnitModel enemy,
            float distance = 3f, float attack = 10f, float range = 4f)
        {
            Demo1Simulation simulation = new Demo1Simulation(TestBalance());
            simulation.ConfigureMissionObjective(DemoMissionObjective.DestroyAllEnemies);
            player = simulation.AddUnit("player", DemoTeam.Player, DemoUnitRole.Witch, Stats(attack, range), Vector3.zero);
            enemy = simulation.AddUnit("enemy", DemoTeam.Enemy, DemoUnitRole.Guard, Stats(0f, range), new Vector3(distance, 0f, 0f));
            simulation.GrantPersistentPlayerIntel(enemy.Id, DemoIntelLevel.Assessed);
            return simulation;
        }

        [Test]
        public void ManualAttack_PursuesBuildsLockAndFires()
        {
            Demo1Simulation simulation = Scenario(out DemoUnitModel player, out DemoUnitModel enemy, 12f, 25f, 3f);
            Assert.That(simulation.RequestAttack(new[] { player.Id }, enemy.Id).Success, Is.True);

            simulation.Advance(6f);

            Assert.That(player.Position.x, Is.GreaterThan(0f));
            Assert.That(player.LockQuality, Is.GreaterThanOrEqualTo(25f));
            Assert.That(player.AttacksPerformed, Is.GreaterThan(0));
            Assert.That(enemy.Health, Is.LessThan(enemy.Stats.MaxHealth));
        }

        [Test]
        public void MoveThenManualAttack_ClearsRouteAndStartsPursuit()
        {
            Demo1Simulation simulation = Scenario(out DemoUnitModel player, out DemoUnitModel enemy, 6f, 10f, 4f);
            Vector3 destination = new Vector3(0f, 0f, 10f);
            simulation.IssueMove(new[] { player.Id }, destination);
            simulation.RequestAttack(new[] { player.Id }, enemy.Id);

            Assert.That(player.HasManualMoveOrder, Is.False);
            Assert.That(player.HasDestination, Is.False);
            Assert.That(player.Activity, Is.EqualTo(DemoUnitActivity.Pursuing));
            simulation.Advance(0.2f);
            Assert.That(player.Destination.x, Is.EqualTo(enemy.Position.x).Within(0.01f));
        }

        [Test]
        public void AttackThenMove_PreservesTargetAndFiresAlongRoute()
        {
            Demo1Simulation simulation = Scenario(out DemoUnitModel player, out DemoUnitModel enemy, 3f, 10f, 4f);
            simulation.RequestAttack(new[] { player.Id }, enemy.Id);
            simulation.Advance(0.5f);
            float lockBeforeMove = player.LockQuality;
            simulation.IssueMove(new[] { player.Id }, new Vector3(0f, 0f, 10f));

            simulation.Advance(0.8f);

            Assert.That(player.LockedTargetId, Is.EqualTo(enemy.Id));
            Assert.That(player.LockQuality, Is.GreaterThan(lockBeforeMove));
            Assert.That(player.HasManualMoveOrder, Is.True);
            Assert.That(player.AttacksPerformed, Is.GreaterThan(0));
        }

        [Test]
        public void Movement_AcceleratesTurnsAndEntersLoiterAtDestination()
        {
            Demo1Simulation simulation = new Demo1Simulation(TestBalance());
            DemoUnitModel unit = simulation.AddUnit("witch", DemoTeam.Player, DemoUnitRole.Witch, Stats(), Vector3.zero);
            unit.Facing = Vector3.left;
            simulation.IssueMove(new[] { unit.Id }, new Vector3(4f, 0f, 0f));

            simulation.Advance(0.1f);
            Assert.That(unit.CurrentSpeed, Is.GreaterThan(0f).And.LessThan(unit.Stats.MoveSpeed));
            Assert.That(unit.Facing.x, Is.LessThan(0f), "A 180 degree turn must not snap instantly.");

            simulation.Advance(8f);
            Assert.That(unit.HasDestination, Is.False);
            Assert.That(unit.HasLoiter, Is.True);
            Assert.That(unit.FlightMode, Is.EqualTo(DemoFlightMode.Loiter));
            Assert.That(unit.LoiterWaypoints.Count, Is.EqualTo(5));
            Assert.That(Vector3.Distance(unit.LoiterCenter, new Vector3(4f, 0f, 0f)), Is.LessThan(0.2f));
        }

        [Test]
        public void Hover_DeceleratesToStopAndMoveOrderExitsHover()
        {
            Demo1Simulation simulation = new Demo1Simulation(TestBalance());
            DemoUnitModel unit = simulation.AddUnit("witch", DemoTeam.Player, DemoUnitRole.Witch, Stats(), Vector3.zero);
            unit.CurrentSpeed = 4f;
            unit.CurrentVelocity = Vector3.right * 4f;

            Assert.That(simulation.RequestHover(new[] { unit.Id }, true).Success, Is.True);
            Assert.That(unit.FlightMode, Is.EqualTo(DemoFlightMode.EnteringHover));
            simulation.Advance(4f);
            Assert.That(unit.IsHovering, Is.True);
            Assert.That(unit.CurrentSpeed, Is.Zero);

            simulation.IssueMove(new[] { unit.Id }, new Vector3(5f, 0f, 0f));
            Assert.That(unit.FlightMode, Is.EqualTo(DemoFlightMode.Normal));
            Assert.That(unit.HasDestination, Is.True);
        }

        [Test]
        public void ForcedReveal_IdentifiesInsideCircleWithoutAssessing()
        {
            Demo1Simulation simulation = new Demo1Simulation(TestBalance());
            DemoUnitStats witchStats = Stats();
            witchStats.VisionRadius = 4f;
            witchStats.ForcedRevealRadius = 24f;
            DemoUnitModel witch = simulation.AddUnit("witch", DemoTeam.Player, DemoUnitRole.Witch, witchStats, Vector3.zero);
            witch.Facing = Vector3.left;
            DemoUnitModel enemy = simulation.AddUnit("enemy", DemoTeam.Enemy, DemoUnitRole.Guard, Stats(), new Vector3(20f, 0f, 0f));

            simulation.Advance(0.1f);

            Assert.That(enemy.IsCurrentlyObservedByPlayer, Is.True);
            Assert.That(enemy.PlayerIntelLevel, Is.EqualTo(DemoIntelLevel.Identified));
            Assert.That(enemy.AssessmentProgress, Is.Zero);
        }

        [Test]
        public void LockQuality_GrowsFromIntelAndDecaysWhenObservationIsLost()
        {
            Demo1Simulation simulation = new Demo1Simulation(TestBalance());
            DemoUnitModel player = simulation.AddUnit("player", DemoTeam.Player, DemoUnitRole.Witch, Stats(), Vector3.zero);
            DemoUnitModel enemy = simulation.AddUnit("enemy", DemoTeam.Enemy, DemoUnitRole.Guard, Stats(), new Vector3(3f, 0f, 0f));
            player.AutoAttackEnabled = false;
            player.Stats.WitchVisionType = DemoWitchVisionType.Night;
            simulation.Advance(2.3f);
            Assert.That(enemy.PlayerIntelLevel, Is.EqualTo(DemoIntelLevel.Assessed));
            simulation.RequestAttack(new[] { player.Id }, enemy.Id);

            simulation.Advance(0.8f);
            float observedLock = player.LockQuality;
            enemy.Position = new Vector3(50f, 0f, 0f);
            simulation.Advance(0.5f);

            Assert.That(observedLock, Is.GreaterThan(10f));
            Assert.That(player.LockQuality, Is.LessThan(observedLock));
        }

        [Test]
        public void Ammo_ReloadsThenForcesReturnWhenReserveIsExhausted()
        {
            Demo1Simulation simulation = Scenario(out DemoUnitModel player, out DemoUnitModel enemy, 2f, 1f, 4f);
            player.Stats.MagazineSize = 1;
            player.Stats.ReserveAmmo = 1;
            player.MagazineAmmo = 1;
            player.ReserveAmmo = 1;
            simulation.RequestAttack(new[] { player.Id }, enemy.Id);
            player.LockQuality = 100f;

            simulation.Advance(0.1f);
            Assert.That(player.MagazineAmmo, Is.Zero);
            Assert.That(player.IsReloading, Is.True);
            simulation.SetAutoAttack(new[] { player.Id }, false);
            simulation.Advance(0.4f);
            Assert.That(player.MagazineAmmo, Is.EqualTo(1));
            Assert.That(player.ReserveAmmo, Is.Zero);
            simulation.SetAutoAttack(new[] { player.Id }, true);
            simulation.RequestAttack(new[] { player.Id }, enemy.Id);
            player.AttackCooldown = 0f;
            player.LockQuality = 100f;
            simulation.Advance(0.1f);

            Assert.That(player.DeploymentState, Is.EqualTo(DemoUnitDeploymentState.Returning));
            Assert.That(player.LockedTargetId, Is.EqualTo(-1));
        }

        [Test]
        public void BaseService_RestoresHealthMagicShieldAndAmmunition()
        {
            Demo1Balance balance = TestBalance();
            balance.BaseArrivalRadius = 0.2f;
            balance.BaseTurnaroundDuration = 0.5f;
            Demo1Simulation simulation = new Demo1Simulation(balance);
            simulation.ConfigureBase(Vector3.zero);
            DemoUnitStats stats = Stats();
            stats.MaxShield = 50f;
            DemoUnitModel player = simulation.AddUnit("player", DemoTeam.Player, DemoUnitRole.Witch, stats, new Vector3(0.1f, 0f, 0f));
            player.Health = 10f;
            player.Magic = 5f;
            player.Shield = 2f;
            player.MagazineAmmo = 0;
            player.ReserveAmmo = 0;

            simulation.RequestReturnToBase(new[] { player.Id });
            simulation.Advance(0.2f);
            Assert.That(player.DeploymentState, Is.EqualTo(DemoUnitDeploymentState.Servicing));
            simulation.Advance(0.6f);

            Assert.That(player.DeploymentState, Is.EqualTo(DemoUnitDeploymentState.Standby));
            Assert.That(player.Health, Is.EqualTo(stats.MaxHealth));
            Assert.That(player.Magic, Is.EqualTo(stats.MaxMagic));
            Assert.That(player.Shield, Is.EqualTo(stats.MaxShield));
            Assert.That(player.MagazineAmmo, Is.EqualTo(stats.MagazineSize));
            Assert.That(player.ReserveAmmo, Is.EqualTo(stats.ReserveAmmo));
        }

        [Test]
        public void EnemyArmor_UsesThreePenetrationTiers()
        {
            float[] penetrationValues = { 20f, 15f, 14.9f };
            float[] expectedDamageValues = { 100f, 35f, 10f };
            for (int i = 0; i < penetrationValues.Length; i++)
            {
                DemoUnitStats attackerStats = Stats(100f);
                attackerStats.Penetration = penetrationValues[i];
                DemoUnitStats targetStats = Stats();
                targetStats.Armor = 20f;
                DemoUnitModel attacker = new DemoUnitModel(1, "attacker", DemoTeam.Player, DemoUnitRole.Witch, attackerStats, Vector3.zero);
                DemoUnitModel target = new DemoUnitModel(2, "target", DemoTeam.Enemy, DemoUnitRole.Guard, targetStats, Vector3.right);

                DemoDamageResult result = DemoDamageResolver.Resolve(attacker, target, TestBalance(), new System.Random(1),
                    hitChance: 1f, evasionChance: 0f);

                Assert.That(result.HealthDamage, Is.EqualTo(expectedDamageValues[i]).Within(0.01f),
                    $"penetration tier {penetrationValues[i]}");
            }
        }

        [Test]
        public void WitchShield_ConsumesOneCapacityAndPointFiveFiveMagicPerDamage()
        {
            DemoUnitStats attackerStats = Stats(20f);
            DemoUnitStats targetStats = Stats();
            targetStats.MaxShield = 50f;
            targetStats.MaxMagic = 100f;
            DemoUnitModel attacker = new DemoUnitModel(1, "enemy", DemoTeam.Enemy, DemoUnitRole.Guard, attackerStats, Vector3.zero);
            DemoUnitModel target = new DemoUnitModel(2, "witch", DemoTeam.Player, DemoUnitRole.Witch, targetStats, Vector3.right);

            DemoDamageResult result = DemoDamageResolver.Resolve(attacker, target, TestBalance(), new System.Random(1),
                hitChance: 1f, evasionChance: 0f);

            Assert.That(result.ShieldDamage, Is.EqualTo(20f).Within(0.01f));
            Assert.That(target.Magic, Is.EqualTo(89f).Within(0.01f));
            Assert.That(result.HealthDamage, Is.Zero);
        }

        [Test]
        public void CoreHit_HalvesArmorUsesCoreMultiplierAndDoesNotStackCritical()
        {
            DemoUnitStats attackerStats = Stats(100f);
            attackerStats.Penetration = 8f;
            attackerStats.CriticalChance = 1f;
            DemoUnitStats targetStats = Stats();
            targetStats.Armor = 20f;
            DemoUnitModel attacker = new DemoUnitModel(1, "attacker", DemoTeam.Player, DemoUnitRole.Witch, attackerStats, Vector3.zero);
            DemoUnitModel target = new DemoUnitModel(2, "target", DemoTeam.Enemy, DemoUnitRole.Guard, targetStats, Vector3.right);

            DemoDamageResult result = DemoDamageResolver.Resolve(attacker, target, TestBalance(), new System.Random(1),
                hitChance: 1f, evasionChance: 0f, forceCore: true);

            Assert.That(result.CoreHit, Is.True);
            Assert.That(result.Critical, Is.False);
            Assert.That(result.HealthDamage, Is.EqualTo(84f).Within(0.01f));
        }

        [Test]
        public void Suppression_AppliesLinearDebuffsWithoutClearingOrders()
        {
            Demo1Simulation simulation = Scenario(out DemoUnitModel player, out DemoUnitModel enemy, 3f, 10f, 4f);
            enemy.Suppression = 100f;
            enemy.LockedTargetId = player.Id;
            enemy.HasExplicitAttackOrder = true;
            float baseInterval = enemy.Stats.AttackInterval;

            Assert.That(simulation.GetEffectiveAttackInterval(enemy.Id), Is.EqualTo(baseInterval * 1.75f).Within(0.001f));
            Assert.That(simulation.GetEffectiveMoveSpeed(enemy.Id), Is.EqualTo(enemy.Stats.MoveSpeed * 0.7f).Within(0.001f));
            Assert.That(simulation.GetEffectiveVisionRadius(enemy.Id), Is.EqualTo(enemy.Stats.VisionRadius * 0.75f).Within(0.001f));
            Assert.That(enemy.LockedTargetId, Is.EqualTo(player.Id));
            Assert.That(enemy.DeploymentState, Is.EqualTo(DemoUnitDeploymentState.Active));
        }

        [Test]
        public void SakamotoMagicEye_AssessesAndCoreMarksEnemiesInSector()
        {
            Demo1Simulation simulation = new Demo1Simulation(TestBalance());
            DemoUnitStats stats = Stats();
            stats.SpecialAbility = DemoSpecialAbility.MagicEyeSearch;
            stats.AbilityMagicCost = 30f;
            stats.AbilityCooldown = 20f;
            stats.AbilityRange = 36f;
            stats.AbilityArcAngle = 45f;
            stats.AbilityDuration = 6f;
            DemoUnitModel sakamoto = simulation.AddUnit("Sakamoto", DemoTeam.Player, DemoUnitRole.Witch, stats, Vector3.zero);
            DemoUnitModel inside = simulation.AddUnit("inside", DemoTeam.Enemy, DemoUnitRole.Guard, Stats(), new Vector3(20f, 0f, 0f));
            DemoUnitModel outside = simulation.AddUnit("outside", DemoTeam.Enemy, DemoUnitRole.Guard, Stats(), new Vector3(0f, 0f, 20f));

            Assert.That(simulation.RequestSpecialAbility(sakamoto.Id).Success, Is.True);

            Assert.That(inside.PlayerIntelLevel, Is.EqualTo(DemoIntelLevel.Assessed));
            Assert.That(simulation.GetMarkRemaining(inside.Id), Is.EqualTo(6f).Within(0.01f));
            Assert.That(outside.PlayerIntelLevel, Is.Not.EqualTo(DemoIntelLevel.Assessed));
            Assert.That(sakamoto.Magic, Is.EqualTo(stats.MaxMagic - 30f));
        }

        [Test]
        public void MiyafujiHeal_ChannelsHealingAndAttackOrderInterruptsIt()
        {
            Demo1Simulation simulation = new Demo1Simulation(TestBalance());
            DemoUnitStats healerStats = Stats();
            healerStats.SpecialAbility = DemoSpecialAbility.Heal;
            healerStats.AbilityMagicCost = 15f;
            healerStats.AbilityCooldown = 10f;
            healerStats.AbilityRange = 6f;
            healerStats.AbilityDuration = 3f;
            healerStats.AbilityValue = 0.12f;
            DemoUnitModel healer = simulation.AddUnit("Miyafuji", DemoTeam.Player, DemoUnitRole.Support, healerStats, Vector3.zero);
            DemoUnitModel ally = simulation.AddUnit("ally", DemoTeam.Player, DemoUnitRole.Witch, Stats(), new Vector3(2f, 0f, 0f));
            DemoUnitModel enemy = simulation.AddUnit("enemy", DemoTeam.Enemy, DemoUnitRole.Guard, Stats(), new Vector3(3f, 0f, 0f));
            ally.Health = 100f;
            simulation.GrantPersistentPlayerIntel(enemy.Id, DemoIntelLevel.Assessed);

            Assert.That(simulation.RequestSpecialAbility(healer.Id, ally.Id).Success, Is.True);
            simulation.Advance(1f);
            Assert.That(ally.Health, Is.GreaterThan(150f));
            Assert.That(simulation.GetEffectiveMoveSpeed(healer.Id), Is.EqualTo(healer.Stats.MoveSpeed * 0.5f).Within(0.01f));
            simulation.RequestAttack(new[] { healer.Id }, enemy.Id);

            Assert.That(healer.IsChannelingAbility, Is.False);
            Assert.That(healer.AbilityCooldownRemaining, Is.GreaterThan(9f));
        }

        [Test]
        public void LynetteFireControl_PassiveActivatesAfterStableHoverAndFiresAtLongRange()
        {
            Demo1Simulation simulation = new Demo1Simulation(TestBalance());
            DemoUnitStats stats = Stats(20f, 12f);
            stats.SpecialAbility = DemoSpecialAbility.None;
            stats.PassiveAbility = DemoPassiveAbility.FireControlSolution;
            stats.MagazineSize = 5;
            stats.ReserveAmmo = 20;
            stats.PassiveActivationDelay = 3f;
            stats.PassiveAttackRange = 48f;
            stats.PassiveDamageMultiplier = 2f;
            stats.PassivePenetration = 32f;
            stats.PassiveMinimumAccuracy = 0.85f;
            DemoUnitModel lynette = simulation.AddUnit("Lynette", DemoTeam.Player, DemoUnitRole.Artillery, stats, Vector3.zero);
            DemoUnitStats enemyStats = Stats();
            enemyStats.Armor = 30f;
            DemoUnitModel enemy = simulation.AddUnit("enemy", DemoTeam.Enemy, DemoUnitRole.Guard, enemyStats, new Vector3(20f, 0f, 0f));
            simulation.GrantPersistentPlayerIntel(enemy.Id, DemoIntelLevel.Assessed);

            Assert.That(simulation.RequestHover(new[] { lynette.Id }, true).Success, Is.True);
            simulation.Advance(4.2f);

            Assert.That(lynette.IsFireControlReady, Is.True);
            Assert.That(enemy.Health, Is.LessThan(enemy.Stats.MaxHealth));
            Assert.That(lynette.MagazineAmmo, Is.LessThan(5));
        }

        [Test]
        public void SanyaRocket_UsesSingleRoundReloadAndExplosiveSuppression()
        {
            Demo1Simulation simulation = Scenario(out DemoUnitModel sanya, out DemoUnitModel enemy, 4f, 20f, 12f);
            sanya.Stats.MagazineSize = 1;
            sanya.Stats.ReserveAmmo = 8;
            sanya.Stats.ReloadDuration = 5f;
            sanya.Stats.ExplosiveRadius = 2.5f;
            sanya.Stats.ProjectileSpeed = 12f;
            sanya.MagazineAmmo = 1;
            sanya.ReserveAmmo = 8;
            simulation.RequestAttack(new[] { sanya.Id }, enemy.Id);
            sanya.LockQuality = 100f;

            simulation.Advance(0.1f);

            Assert.That(sanya.MagazineAmmo, Is.Zero);
            Assert.That(sanya.ReloadRemaining, Is.GreaterThan(4.8f));
            Assert.That(simulation.Projectiles.Count, Is.EqualTo(1));
            Assert.That(enemy.Suppression, Is.Zero);
            simulation.Advance(0.5f);
            Assert.That(enemy.Suppression, Is.GreaterThan(8f));
        }

        [Test]
        public void PerrineLightning_DamagesNearbyEnemiesAndAddsSuppression()
        {
            Demo1Simulation simulation = new Demo1Simulation(TestBalance());
            DemoUnitStats stats = Stats(20f);
            stats.SpecialAbility = DemoSpecialAbility.LightningStrike;
            stats.AbilityMagicCost = 40f;
            stats.AbilityCooldown = 14f;
            stats.AbilityRadius = 5f;
            stats.AbilityDamageMultiplier = 2f;
            stats.AbilityPenetration = 16f;
            stats.AbilitySuppression = 35f;
            DemoUnitModel perrine = simulation.AddUnit("Perrine", DemoTeam.Player, DemoUnitRole.Witch, stats, Vector3.zero);
            DemoUnitStats enemyStats = Stats();
            enemyStats.Armor = 30f;
            DemoUnitModel enemy = simulation.AddUnit("enemy", DemoTeam.Enemy, DemoUnitRole.Guard, enemyStats, new Vector3(4f, 0f, 0f));

            Assert.That(simulation.RequestSpecialAbility(perrine.Id).Success, Is.True);

            Assert.That(enemy.Health, Is.LessThan(enemy.Stats.MaxHealth));
            Assert.That(enemy.Suppression, Is.GreaterThanOrEqualTo(43f));
            Assert.That(perrine.Magic, Is.EqualTo(stats.MaxMagic - 40f));
        }

        [Test]
        public void ScriptableConfigs_SerializeWeaponsAbilitiesAndNightRadar()
        {
            DemoUnitConfig sakamoto = Resources.Load<DemoUnitConfig>("Configs/Units/Sakamoto");
            DemoUnitConfig miyafuji = Resources.Load<DemoUnitConfig>("Configs/Units/Miyafuji");
            DemoUnitConfig lynette = Resources.Load<DemoUnitConfig>("Configs/Units/Lynette");
            DemoUnitConfig sanya = Resources.Load<DemoUnitConfig>("Configs/Units/Sanya");
            DemoUnitConfig perrine = Resources.Load<DemoUnitConfig>("Configs/Units/Perrine");
            DemoUnitConfig guard = Resources.Load<DemoUnitConfig>("Configs/Units/NeuroiGuardA");
            Demo1BalanceConfig balance = Resources.Load<Demo1BalanceConfig>("Configs/Demo1Balance");

            Assert.That(sakamoto.Stats.SpecialAbility, Is.EqualTo(DemoSpecialAbility.MagicEyeSearch));
            Assert.That(miyafuji.Stats.SpecialAbility, Is.EqualTo(DemoSpecialAbility.Heal));
            Assert.That(lynette.Stats.SpecialAbility, Is.EqualTo(DemoSpecialAbility.None));
            Assert.That(lynette.Stats.PassiveAbility, Is.EqualTo(DemoPassiveAbility.FireControlSolution));
            Assert.That(lynette.Stats.PassiveAttackRange, Is.EqualTo(48f));
            Assert.That(lynette.Stats.AttackInterval, Is.EqualTo(2.2f));
            Assert.That(lynette.Stats.MagazineSize, Is.EqualTo(5));
            Assert.That(sanya.Stats.WitchVisionType, Is.EqualTo(DemoWitchVisionType.Night));
            Assert.That(sanya.Stats.VisionRadius, Is.EqualTo(72f));
            Assert.That(sanya.Stats.AttackRange, Is.EqualTo(72f));
            Assert.That(sanya.Stats.ProjectileTurnRate, Is.EqualTo(120f));
            Assert.That(sanya.Stats.ProjectileLifetime, Is.EqualTo(10f));
            Assert.That(sanya.Stats.ExplosiveRadius, Is.EqualTo(2.5f));
            Assert.That(sakamoto.Stats.ForcedRevealRadius, Is.EqualTo(24f));
            Assert.That(sanya.Stats.ForcedRevealRadius, Is.EqualTo(24f));
            Assert.That(guard.Stats.Mobility, Is.EqualTo(0.65f));
            Assert.That(balance.Values.AccelerationDuration, Is.EqualTo(8f));
            Assert.That(balance.Values.TurnSpeedCapAt180, Is.EqualTo(0.3f));
            Assert.That(balance.Values.LoiterVertexCount, Is.EqualTo(5));
            Assert.That(perrine.Stats.SpecialAbility, Is.EqualTo(DemoSpecialAbility.LightningStrike));
            Assert.That(sakamoto.Stats.Traits, Is.EqualTo(DemoUnitTrait.None));
            Assert.That(miyafuji.Stats.Traits, Is.EqualTo(DemoUnitTrait.None));
            Assert.That(lynette.Stats.Traits, Is.EqualTo(DemoUnitTrait.None));
        }
    }
}
