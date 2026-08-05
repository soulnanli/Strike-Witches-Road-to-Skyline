using System.Collections;
using System.Linq;
using NUnit.Framework;
using SWRTS.Demo1;
using UnityEngine;
using UnityEngine.TestTools;

namespace SWRTS.Prototype.BaseScene.PlayModeTests
{
    public sealed class BaseCommandSceneSmokeTests
    {
        [Test]
        public void BaseOpensRosterAndDeploysSelectedWitchesAsIndependentMapUnits()
        {
            GameObject root = new GameObject("Base Command Smoke Test");
            BaseCommandSceneController controller = root.AddComponent<BaseCommandSceneController>();
            controller.Initialize();

            Assert.That(controller.AvailableWitchCount, Is.EqualTo(5));
            Assert.That(controller.BaseMapPosition.x, Is.InRange(0.8f, 0.9f));
            Assert.That(controller.BaseMapPosition.y, Is.InRange(0.75f, 0.9f));

            controller.OpenReadinessPanel();
            Assert.That(controller.IsReadinessPanelOpen, Is.True);
            Assert.That(controller.SetWitchSelected("宫藤芳佳", true), Is.True);
            Assert.That(controller.SetWitchSelected("坂本美绪", true), Is.True);
            Assert.That(controller.SelectedWitchCount, Is.EqualTo(2));
            Assert.That(controller.DeploySelected(), Is.True);
            Assert.That(controller.DeployedWitchCount, Is.EqualTo(2));
            Assert.That(controller.SelectedWitchCount, Is.Zero);
            Assert.That(GameObject.Find("Map Unit 宫藤芳佳"), Is.Not.Null);
            Assert.That(GameObject.Find("Map Unit 坂本美绪"), Is.Not.Null);

            Assert.That(controller.SetWitchSelected("宫藤芳佳", true), Is.False,
                "A deployed witch cannot be selected for a duplicate sortie.");
            controller.ClearSelection();
            Assert.That(controller.DeploySelected(), Is.False);
            Assert.That(controller.StatusMessage, Does.Contain("至少选择一名"));

            Object.DestroyImmediate(root);
        }

        [UnityTest]
        public IEnumerator SortieStartsDemoOnRealScaleBaseCommandMap()
        {
            GameObject root = new GameObject("Base Command Operational Test");
            BaseCommandSceneController controller = root.AddComponent<BaseCommandSceneController>();
            DemoUnitConfig[] witches = Resources.LoadAll<DemoUnitConfig>("Configs/Units")
                .Where(config => config.Team == DemoTeam.Player)
                .OrderBy(config => config.SpawnOrder)
                .ToArray();
            Texture2D map = new Texture2D(16, 9);
            controller.ConfigureAssets(map, witches);
            controller.Initialize();
            controller.SetWitchSelected(witches[0].DisplayName, true);

            Assert.That(controller.DeploySelected(), Is.True);
            Assert.That(controller.IsOperationalLevelStarted, Is.True);
            Assert.That(controller.MapSizeKilometers, Is.EqualTo(new Vector2(560f, 315f)));

            yield return null;
            yield return null;

            Demo1GameController operational = Object.FindFirstObjectByType<Demo1GameController>();
            Assert.That(operational, Is.Not.Null);
            Assert.That(operational.Simulation.Units.Count(unit => unit.Team == DemoTeam.Player), Is.EqualTo(1));
            Assert.That(operational.Simulation.Balance.MapHalfWidth, Is.EqualTo(280f));
            Assert.That(operational.Simulation.Balance.MapHalfHeight, Is.EqualTo(157.5f));
            Assert.That(GameObject.Find("Operations Map"), Is.Not.Null);

            Object.Destroy(operational.gameObject);
            Object.Destroy(root);
            Object.Destroy(map);
            yield return null;
        }
    }
}
