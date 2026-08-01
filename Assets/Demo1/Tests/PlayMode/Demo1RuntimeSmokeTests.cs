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
        public IEnumerator BootstrapBuildsPlayableScenarioWithoutRuntimeErrors()
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

            Object.Destroy(root);
            yield return null;
        }
    }
}
