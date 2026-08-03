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
        public IEnumerator FullPlayerLoopMovesFightsStrikesAndPausesWithoutRuntimeErrors()
        {
            GameObject root = new GameObject("Demo1 Smoke Test");
            Demo1GameController controller = root.AddComponent<Demo1GameController>();

            yield return null;
            yield return null;

            Assert.That(controller.Simulation, Is.Not.Null);
            Assert.That(controller.Simulation.Units.Count, Is.EqualTo(8));
            Assert.That(controller.Simulation.Units.Count(unit => unit.Team == DemoTeam.Player), Is.EqualTo(4));
            Assert.That(controller.Simulation.Units.Any(unit => unit.Role == DemoUnitRole.Fortress), Is.True);
            Assert.That(Camera.main, Is.Not.Null);
            Assert.That(controller.Simulation.Outcome, Is.EqualTo(DemoOutcome.Running));
            Assert.That(controller.SelectedUnitIds.Count, Is.EqualTo(4), "The squad should be ready to command on entry.");

            DemoUnitModel mover = controller.Simulation.Units.First(unit => unit.Team == DemoTeam.Player);
            controller.SelectUnits(new[] { mover.Id });
            Vector3 moveStart = mover.Position;
            DemoCommandResult move = controller.CommandMove(moveStart + Vector3.right * 8f);
            Assert.That(move.Success, Is.True, move.Message);
            controller.Simulation.Advance(0.5f);
            yield return null;
            Assert.That(mover.Position.x, Is.GreaterThan(moveStart.x + 0.1f), "A real controller move order should update the unit model.");

            DemoUnitModel target = controller.Simulation.Units.First(unit => unit.Team == DemoTeam.Enemy && !unit.IsFixed);
            mover.Position = target.Position + Vector3.left * (mover.Stats.EngagementRadius * 0.5f);
            target.IsRevealedToPlayer = true;
            controller.SelectUnits(new[] { mover.Id });
            DemoCommandResult engage = controller.CommandEngage(target.Id);
            Assert.That(engage.Success, Is.True, engage.Message);
            Assert.That(controller.Simulation.Combats.Any(combat => !combat.IsFinished), Is.True,
                "Engaging through the controller should create a live battle.");
            DemoCombatModel activeCombat = controller.Simulation.Combats.First(combat => !combat.IsFinished);
            yield return null;
            Demo1BattleUI battleUi = Object.FindFirstObjectByType<Demo1BattleUI>();
            Assert.That(battleUi, Is.Not.Null);
            Assert.That(GameObject.Find($"Battle Bubble {activeCombat.Id}"), Is.Not.Null, "An active battle should create a clickable map bubble.");
            battleUi.OpenPanel(activeCombat.Id);
            Assert.That(battleUi.IsPanelOpen, Is.True);
            DemoCommandResult lineChange = controller.CommandBattleLineChange(mover.Id, DemoBattleLine.Main);
            Assert.That(lineChange.Success, Is.True, lineChange.Message);
            Assert.That(activeCombat.GetAssignment(mover.Id).IsRepositioning, Is.True);

            DemoUnitModel artillery = controller.Simulation.Units.First(unit => unit.Team == DemoTeam.Player && unit.Stats.CanRemoteStrike);
            DemoUnitModel strikeTarget = controller.Simulation.Units.First(unit => unit.Team == DemoTeam.Enemy && unit.IsAlive && unit.CombatId < 0);
            strikeTarget.Position = artillery.Position + Vector3.right * 10f;
            float healthBeforeStrike = strikeTarget.Health;
            float shieldBeforeStrike = strikeTarget.Shield;
            controller.SelectUnits(new[] { artillery.Id });
            DemoCommandResult strike = controller.CommandRemoteStrike(strikeTarget.Position);
            Assert.That(strike.Success, Is.True, strike.Message);
            Assert.That(controller.Simulation.RemoteStrikes.Any(item => !item.Resolved), Is.True);
            controller.Simulation.Advance(3.2f);
            yield return null;
            Assert.That(controller.Simulation.RemoteStrikes.All(item => item.Resolved), Is.True);
            Assert.That(strikeTarget.Health < healthBeforeStrike || strikeTarget.Shield < shieldBeforeStrike, Is.True,
                "The scheduled strike should damage a target in its radius.");

            controller.SetPaused(true);
            float pausedAt = controller.Simulation.SimulationTime;
            yield return null;
            yield return null;
            Assert.That(controller.IsPaused, Is.True);
            Assert.That(controller.Simulation.SimulationTime, Is.EqualTo(pausedAt).Within(0.0001f));
            controller.SetPaused(false);
            yield return null;
            Assert.That(controller.IsPaused, Is.False);
            Assert.That(controller.Simulation.SimulationTime, Is.GreaterThan(pausedAt));

            target.Health = 0f;
            target.Activity = DemoUnitActivity.Destroyed;
            controller.Simulation.Advance(0.2f);
            yield return null;
            Assert.That(activeCombat.IsFinished, Is.True);
            Assert.That(battleUi.IsPanelOpen, Is.False, "The panel should close when its battle ends.");

            Object.Destroy(root);
            yield return null;
        }
    }
}
