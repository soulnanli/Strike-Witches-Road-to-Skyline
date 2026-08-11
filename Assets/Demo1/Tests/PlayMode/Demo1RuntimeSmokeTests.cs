using System.Collections;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace SWRTS.Demo1.PlayModeTests
{
    public sealed class Demo1RuntimeSmokeTests
    {
        [UnityTest]
        public IEnumerator ControllerRunsIndividualMapCombatAndPause()
        {
            GameObject root = new GameObject("Demo1 Individual Combat Smoke Test");
            Demo1GameController controller = root.AddComponent<Demo1GameController>();
            yield return null;
            yield return null;

            Assert.That(controller.Simulation, Is.Not.Null);
            Assert.That(Object.FindFirstObjectByType<Demo1LevelSelector>(), Is.Not.Null);
            Assert.That(GameObject.Find("Demo1 Battle UI"), Is.Null);
            Assert.That(Resources.LoadAll<DemoLevelConfig>("Configs/Levels").Length, Is.EqualTo(3));

            DemoUnitModel player = controller.Simulation.Units.First(unit => unit.Team == DemoTeam.Player);
            DemoUnitModel enemy = controller.Simulation.Units.First(unit => unit.Team == DemoTeam.Enemy && !unit.IsFixed);
            player.Position = enemy.Position + Vector3.left * (player.Stats.AttackRange * 0.5f);
            controller.Simulation.GrantPersistentPlayerIntel(enemy.Id);
            controller.SelectUnits(new[] { player.Id });

            float healthBefore = enemy.Health;
            DemoCommandResult attack = controller.CommandEngage(enemy.Id);
            Assert.That(attack.Success, Is.True, attack.Message);
            player.LockQuality = 100f;
            controller.Simulation.Advance(0.2f);
            yield return null;

            Assert.That(player.LockedTargetId, Is.EqualTo(enemy.Id));
            Assert.That(player.Activity, Is.EqualTo(DemoUnitActivity.Attacking));
            Assert.That(enemy.Health, Is.LessThan(healthBefore));
            Assert.That(GameObject.Find("Combat #1"), Is.Null);
            Assert.That(GameObject.Find("Battle Panel"), Is.Null);

            controller.SetPaused(true);
            float pausedAt = controller.Simulation.SimulationTime;
            yield return null;
            yield return null;
            Assert.That(controller.Simulation.SimulationTime, Is.EqualTo(pausedAt).Within(0.0001f));
            controller.SetPaused(false);
            yield return null;
            Assert.That(controller.Simulation.SimulationTime, Is.GreaterThan(pausedAt));

            Object.Destroy(root);
            yield return null;
        }

        [UnityTest]
        public IEnumerator SelectionShowsOnlyAttackRangeAndMergedDetectionLayers()
        {
            GameObject root = new GameObject("Demo1 Target Visualization Test");
            Demo1GameController controller = root.AddComponent<Demo1GameController>();
            yield return null;
            yield return null;

            DemoUnitModel player = controller.Simulation.Units.First(unit => unit.Team == DemoTeam.Player);
            DemoUnitModel enemy = controller.Simulation.Units.First(unit => unit.Team == DemoTeam.Enemy && !unit.IsFixed);
            controller.Simulation.GrantPersistentPlayerIntel(enemy.Id);
            controller.SelectUnits(new[] { player.Id });
            Assert.That(controller.CommandEngage(enemy.Id).Success, Is.True);
            yield return null;

            Demo1UnitView view = Object.FindObjectsByType<Demo1UnitView>(FindObjectsSortMode.None)
                .Single(item => item.UnitId == player.Id);
            Transform attackRange = view.transform.Find("Attack Range");
            Transform optimalRange = view.transform.Find("Optimal Range");
            Transform engagementRange = view.transform.Find("Engagement");
            Transform targetLock = view.transform.Find("Target Lock");
            Transform route = view.transform.Find("Route");
            Assert.That(attackRange, Is.Not.Null);
            Assert.That(optimalRange, Is.Null);
            Assert.That(engagementRange, Is.Null);
            Assert.That(targetLock, Is.Not.Null);
            Assert.That(attackRange.GetComponent<LineRenderer>().enabled, Is.True);
            Assert.That(targetLock.GetComponent<LineRenderer>().enabled, Is.True);
            Assert.That(attackRange.GetComponent<Demo1ScreenSpaceLineWidth>().PixelWidth,
                Is.GreaterThanOrEqualTo(Demo1Drawing.OperationalLinePixelWidth));
            Assert.That(attackRange.GetComponent<LineRenderer>().startColor.a, Is.GreaterThanOrEqualTo(0.85f));
            Assert.That(targetLock.GetComponent<Demo1ScreenSpaceLineWidth>().PixelWidth,
                Is.GreaterThanOrEqualTo(Demo1Drawing.OperationalLinePixelWidth));
            Assert.That(route.GetComponent<Demo1ScreenSpaceLineWidth>().PixelWidth,
                Is.EqualTo(Demo1Drawing.RouteLinePixelWidth));

            GameObject overlay = GameObject.Find("Selected Range Overlay");
            Assert.That(overlay, Is.Not.Null);
            LineRenderer detection = overlay.GetComponentsInChildren<LineRenderer>(true)
                .First(line => line.name.StartsWith("Detection Range"));
            Assert.That(detection.enabled, Is.True);
            Assert.That(overlay.GetComponentsInChildren<LineRenderer>(true)
                .Any(line => line.name.StartsWith("Forced Reveal")), Is.False);
            Assert.That(detection.startColor.r, Is.EqualTo(Demo1RangeOverlay.DetectionColor.r).Within(0.005f));
            Assert.That(detection.startColor.g, Is.EqualTo(Demo1RangeOverlay.DetectionColor.g).Within(0.005f));
            Assert.That(detection.startColor.b, Is.EqualTo(Demo1RangeOverlay.DetectionColor.b).Within(0.005f));
            Assert.That(detection.startColor.a, Is.EqualTo(Demo1RangeOverlay.DetectionColor.a).Within(0.005f));
            Assert.That(detection.startColor.a, Is.GreaterThanOrEqualTo(0.85f));
            Assert.That(detection.GetComponent<Demo1ScreenSpaceLineWidth>().PixelWidth,
                Is.GreaterThanOrEqualTo(Demo1Drawing.OperationalLinePixelWidth));
            Assert.That(detection.numCornerVertices, Is.GreaterThanOrEqualTo(4));

            LineRenderer gridLine = Object.FindObjectsByType<LineRenderer>(FindObjectsSortMode.None)
                .First(line => line.name == "Grid Line");
            Assert.That(gridLine.GetComponent<Demo1ScreenSpaceLineWidth>().PixelWidth,
                Is.EqualTo(Demo1Drawing.BackgroundGridPixelWidth));
            Assert.That(gridLine.startColor.a, Is.EqualTo(0.55f).Within(0.005f));

            Assert.That(controller.CommandSetAutoAttack(false).Success, Is.True);
            yield return null;
            Assert.That(player.LockedTargetId, Is.EqualTo(-1));
            Assert.That(targetLock.GetComponent<LineRenderer>().enabled, Is.False);

            Object.Destroy(root);
            yield return null;
        }

        [UnityTest]
        public IEnumerator OverlappingSelectedVisionUsesSingleContourWithoutForcedRevealLayer()
        {
            GameObject root = new GameObject("Demo1 Range Union Test");
            Demo1GameController controller = root.AddComponent<Demo1GameController>();
            yield return null;
            yield return null;

            DemoUnitModel ordinary = controller.Simulation.Units.First(unit =>
                unit.Team == DemoTeam.Player && unit.Stats.WitchVisionType == DemoWitchVisionType.Ordinary);
            DemoUnitModel night = controller.Simulation.Units.First(unit =>
                unit.Team == DemoTeam.Player && unit.Stats.WitchVisionType == DemoWitchVisionType.Night);
            night.Position = ordinary.Position + Vector3.right;
            controller.SelectUnits(new[] { ordinary.Id, night.Id });
            yield return null;

            GameObject overlay = GameObject.Find("Selected Range Overlay");
            LineRenderer[] lines = overlay.GetComponentsInChildren<LineRenderer>(true);
            Assert.That(lines.Count(line => line.enabled && line.name.StartsWith("Detection Range")), Is.EqualTo(1));
            Assert.That(lines.Any(line => line.name.StartsWith("Forced Reveal")), Is.False);
            Assert.That(ordinary.Stats.WitchVisionType, Is.EqualTo(DemoWitchVisionType.Ordinary));
            Assert.That(night.Stats.WitchVisionType, Is.EqualTo(DemoWitchVisionType.Night));

            Object.Destroy(root);
            yield return null;
        }

        [UnityTest]
        public IEnumerator TacticalSupplyCreatesMapVisualAndAcceptsSelectedWitch()
        {
            GameObject root = new GameObject("Demo1 Tactical Supply Smoke Test");
            Demo1GameController controller = root.AddComponent<Demo1GameController>();
            yield return null;
            yield return null;

            DemoUnitModel witch = controller.Simulation.Units.First(unit =>
                unit.Team == DemoTeam.Player && unit.DeploymentState == DemoUnitDeploymentState.Active);
            controller.SelectUnits(new[] { witch.Id });
            Vector3 dropPosition = witch.Position + Vector3.right * 2f;
            Assert.That(controller.CommandSupplyDrop(dropPosition).Success, Is.True);
            controller.Simulation.Advance(controller.Simulation.Balance.SupplyDeliveryDelay + 0.1f);
            yield return null;

            DemoSupplyDropModel drop = controller.Simulation.SupplyDrops.Single();
            Assert.That(drop.IsActive, Is.True);
            GameObject visual = GameObject.Find($"Supply Drop #{drop.Id}");
            Assert.That(visual, Is.Not.Null);
            Assert.That(visual.transform.Find("Supply Radius"), Is.Not.Null);
            witch.Magic = 0f;
            Assert.That(controller.CommandFieldResupply(new[] { witch.Id }).Success, Is.True);
            controller.Simulation.Advance(0.2f);
            yield return null;
            Assert.That(witch.IsResupplying, Is.True);
            Assert.That(witch.AutoAttackEnabled, Is.False);

            Object.Destroy(root);
            yield return null;
        }
    }
}
