using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEditor.Build.Reporting;

namespace SWRTS.Demo1.Editor
{
    public static class Demo1SceneBuilder
    {
        public const string ScenePath = "Assets/Demo1/Scenes/Demo1.unity";

        [MenuItem("Strike Witches/Demo 1.0/Rebuild Scene")]
        public static void BuildScene()
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            GameObject root = new GameObject("Demo1 Runtime");
            root.AddComponent<Demo1GameController>();
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene, ScenePath);

            EditorBuildSettingsScene[] existing = EditorBuildSettings.scenes;
            if (existing.All(item => item.path != ScenePath))
            {
                EditorBuildSettings.scenes = existing
                    .Concat(new[] { new EditorBuildSettingsScene(ScenePath, true) })
                    .ToArray();
            }
            else
            {
                EditorBuildSettings.scenes = existing
                    .Select(item => item.path == ScenePath ? new EditorBuildSettingsScene(ScenePath, true) : item)
                    .ToArray();
            }

            AssetDatabase.SaveAssets();
            Debug.Log($"Demo 1.0 scene rebuilt: {ScenePath}");
        }

        public static void BuildWindowsPlayer()
        {
            BuildScene();
            BuildPlayerOptions options = new BuildPlayerOptions
            {
                scenes = new[] { ScenePath },
                locationPathName = "Builds/Demo1/StrikeWitches-Demo1.exe",
                target = BuildTarget.StandaloneWindows64,
                options = BuildOptions.Development
            };
            BuildReport report = BuildPipeline.BuildPlayer(options);
            if (report.summary.result != BuildResult.Succeeded)
                throw new System.Exception($"Demo 1.0 player build failed: {report.summary.result}");
            Debug.Log($"Demo 1.0 player built: {report.summary.outputPath} ({report.summary.totalSize} bytes)");
        }
    }
}
