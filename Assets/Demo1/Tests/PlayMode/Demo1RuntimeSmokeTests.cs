using System.Collections;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace SWRTS.Demo1.PlayModeTests
{
    public sealed class Demo1RuntimeSmokeTests
    {
        [UnityTest]
        public IEnumerator FullPlayerLoopMovesFightsAndPausesWithoutRuntimeErrors()
        {
            GameObject root = new GameObject("Demo1 Smoke Test");
            Demo1GameController controller = root.AddComponent<Demo1GameController>();

            yield return null;
            yield return null;

            Assert.That(controller.Simulation, Is.Not.Null);
            Demo1BalanceConfig balanceConfig = Resources.Load<Demo1BalanceConfig>("Configs/Demo1Balance");
            DemoUnitConfig[] unitConfigs = Resources.LoadAll<DemoUnitConfig>("Configs/Units");
            Assert.That(balanceConfig, Is.Not.Null, "The scenario must use its balance ScriptableObject.");
            Assert.That(unitConfigs.Length, Is.EqualTo(9));
            Assert.That(unitConfigs.Select(config => config.SpawnOrder).Distinct().Count(), Is.EqualTo(9));
            Assert.That(unitConfigs.Select(config => config.DisplayName).Distinct().Count(), Is.EqualTo(9));
            Assert.That(controller.Simulation.Units.Count, Is.EqualTo(9));
            Assert.That(controller.Simulation.Units.Count(unit => unit.Team == DemoTeam.Player), Is.EqualTo(5));
            Assert.That(controller.Simulation.Units.Count(unit => unit.Team == DemoTeam.Player && unit.Stats.WitchVisionType == DemoWitchVisionType.Ordinary), Is.EqualTo(4));
            Assert.That(controller.Simulation.Units.Single(unit => unit.DisplayName.Contains("桑妮亚")).Stats.WitchVisionType, Is.EqualTo(DemoWitchVisionType.Night));
            DemoUnitModel sakamoto = controller.Simulation.Units.Single(unit => unit.DisplayName.Contains("坂本"));
            DemoUnitModel miyafuji = controller.Simulation.Units.Single(unit => unit.DisplayName.Contains("宫藤"));
            DemoUnitModel lynette = controller.Simulation.Units.Single(unit => unit.DisplayName.Contains("莉涅特"));
            DemoUnitModel perrine = controller.Simulation.Units.Single(unit => unit.DisplayName.Contains("佩琳"));
            DemoUnitModel sanya = controller.Simulation.Units.Single(unit => unit.DisplayName.Contains("桑妮亚"));
            Assert.That(new[] { miyafuji.Stats.MaxHealth, miyafuji.Stats.Attack, miyafuji.Stats.Defense, miyafuji.Stats.MaxMagic, miyafuji.Stats.MaxShield, miyafuji.Stats.MagicRecovery },
                Is.EqualTo(new[] { 270f, 40f, 16f, 270f, 144f, 14f }));
            Assert.That(new[] { sakamoto.Stats.MaxHealth, sakamoto.Stats.Attack, sakamoto.Stats.Defense, sakamoto.Stats.MaxMagic, sakamoto.Stats.MaxShield, sakamoto.Stats.MagicRecovery },
                Is.EqualTo(new[] { 330f, 66f, 18f, 180f, 110f, 8f }));
            Assert.That(new[] { lynette.Stats.MaxHealth, lynette.Stats.Attack, lynette.Stats.Defense, lynette.Stats.MaxMagic, lynette.Stats.MaxShield, lynette.Stats.MagicRecovery },
                Is.EqualTo(new[] { 260f, 54f, 16f, 180f, 100f, 8f }));
            Assert.That(new[] { perrine.Stats.MaxHealth, perrine.Stats.Attack, perrine.Stats.Defense, perrine.Stats.MaxMagic, perrine.Stats.MaxShield, perrine.Stats.MagicRecovery },
                Is.EqualTo(new[] { 280f, 56f, 18f, 180f, 110f, 8f }));
            Assert.That(new[] { sanya.Stats.MaxHealth, sanya.Stats.Attack, sanya.Stats.Defense, sanya.Stats.MaxMagic, sanya.Stats.MaxShield, sanya.Stats.MagicRecovery },
                Is.EqualTo(new[] { 270f, 52f, 16f, 250f, 136f, 10f }));
            Assert.That(sakamoto.Stats.HasTrait(DemoUnitTrait.SakamotoCoreInsight), Is.True);
            Assert.That(miyafuji.Stats.HasTrait(DemoUnitTrait.MiyafujiShieldAura), Is.True);
            Assert.That(lynette.Stats.HasTrait(DemoUnitTrait.LynetteSharpshooter), Is.True);
            Assert.That(lynette.Stats.CanRemoteStrike, Is.False);
            Assert.That(lynette.Stats.AttackProfile, Is.EqualTo(DemoAttackProfile.Standard));
            Assert.That(lynette.Stats.ScreenPenetration, Is.EqualTo(0f));
            Assert.That(controller.Simulation.GetEffectiveCriticalChance(lynette.Id), Is.EqualTo(0.3f).Within(0.001f));
            Assert.That(controller.Simulation.GetEffectiveAttackInterval(lynette.Id), Is.EqualTo(2.2f).Within(0.001f));
            DemoUnitConfig lynetteConfig = unitConfigs.Single(config => config.DisplayName.Contains("莉涅特"));
            Assert.That(lynette.Stats, Is.Not.SameAs(lynetteConfig.Stats), "Runtime combat must not mutate the source asset.");
            Assert.That(controller.Simulation.Units.Any(unit => unit.Role == DemoUnitRole.Fortress), Is.True);
            Assert.That(Camera.main, Is.Not.Null);
            Assert.That(controller.Simulation.Outcome, Is.EqualTo(DemoOutcome.Running));
            Assert.That(controller.SelectedUnitIds.Count, Is.EqualTo(5), "The squad should be ready to command on entry.");
            Assert.That(controller.CharacterDetailUnitId, Is.EqualTo(-1), "Multi-selection should hide the character detail panel.");

            DemoUnitModel mover = controller.Simulation.Units.First(unit => unit.Team == DemoTeam.Player);
            controller.SelectUnits(new[] { mover.Id });
            Assert.That(controller.CharacterDetailUnitId, Is.EqualTo(mover.Id), "Single-selection should expose that witch in the detail panel.");
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
            Assert.That(controller.CharacterDetailUnitId, Is.EqualTo(-1), "The battle panel should take priority over the right-side detail panel.");
            DemoCommandResult lineChange = controller.CommandBattleLineChange(mover.Id, DemoBattleLine.Main);
            Assert.That(lineChange.Success, Is.True, lineChange.Message);
            Assert.That(activeCombat.GetAssignment(mover.Id).IsRepositioning, Is.True);

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

        [UnityTest]
        public IEnumerator BattlePanelButtonsSurvivePointerPressAndExecuteSelectionLineChangeAndRetreat()
        {
            GameObject root = new GameObject("Demo1 Battle UI Interaction Test");
            Demo1GameController controller = root.AddComponent<Demo1GameController>();

            yield return null;
            yield return null;

            DemoUnitModel player = controller.Simulation.Units.First(unit => unit.Team == DemoTeam.Player);
            DemoUnitModel enemy = controller.Simulation.Units.First(unit => unit.Team == DemoTeam.Enemy && !unit.IsFixed);
            player.Position = enemy.Position + Vector3.left * (player.Stats.EngagementRadius * 0.5f);
            enemy.IsRevealedToPlayer = true;
            controller.SelectUnits(new[] { player.Id });
            DemoCommandResult engage = controller.CommandEngage(enemy.Id);
            Assert.That(engage.Success, Is.True, engage.Message);

            DemoCombatModel combat = controller.Simulation.Combats.First(item => !item.IsFinished);
            controller.SetPaused(true);
            Demo1BattleUI battleUi = Object.FindFirstObjectByType<Demo1BattleUI>();
            battleUi.OpenPanel(combat.Id);
            yield return null;

            Button unitButton = GameObject.Find($"Unit {player.Id}").GetComponent<Button>();
            int pressedButtonInstanceId = unitButton.gameObject.GetInstanceID();
            PointerEventData pointer = new PointerEventData(EventSystem.current)
            {
                button = PointerEventData.InputButton.Left,
                pointerId = -1
            };

            ExecuteEvents.Execute(unitButton.gameObject, pointer, ExecuteEvents.pointerDownHandler);
            yield return new WaitForSecondsRealtime(0.2f);
            Assert.That(unitButton, Is.Not.Null, "A live panel refresh must not destroy the button while it is pressed.");
            Assert.That(unitButton.gameObject.GetInstanceID(), Is.EqualTo(pressedButtonInstanceId));
            ExecuteEvents.Execute(unitButton.gameObject, pointer, ExecuteEvents.pointerUpHandler);
            ExecuteEvents.Execute(unitButton.gameObject, pointer, ExecuteEvents.pointerClickHandler);
            yield return null;

            Assert.That(battleUi.SelectedUnitId, Is.EqualTo(player.Id));
            Assert.That(controller.SelectedUnitIds, Is.EquivalentTo(new[] { player.Id }));

            Button mainButton = GameObject.Find("Main").GetComponent<Button>();
            ExecuteEvents.Execute(mainButton.gameObject, pointer, ExecuteEvents.pointerClickHandler);
            yield return null;
            DemoCombatParticipantState assignment = combat.GetAssignment(player.Id);
            Assert.That(assignment.Line, Is.EqualTo(DemoBattleLine.Main));
            Assert.That(assignment.IsRepositioning, Is.True);

            Button retreatButton = GameObject.Find("Retreat").GetComponent<Button>();
            ExecuteEvents.Execute(retreatButton.gameObject, pointer, ExecuteEvents.pointerClickHandler);
            yield return null;
            Assert.That(player.Activity, Is.EqualTo(DemoUnitActivity.Retreating));
            Assert.That(player.RetreatRemaining, Is.GreaterThan(0f));

            Object.Destroy(root);
            yield return null;
        }
    }
}
