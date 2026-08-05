using System.Linq;
using SWRTS.Demo1;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace SWRTS.Prototype.BaseScene.Editor
{
    public static class BaseCommandSceneBuilder
    {
        public const string ScenePath = "Assets/Prototype/BaseScene/Scenes/BaseCommand.unity";
        public const string MapPath = "Assets/Prototype/Maps/EnglishChannel-1944-OfficialStyle-v1.png";

        [MenuItem("Strike Witches/Base Prototype/Rebuild Base Command Scene")]
        public static void BuildScene()
        {
            Texture2D map = AssetDatabase.LoadAssetAtPath<Texture2D>(MapPath);
            if (map == null)
                throw new System.InvalidOperationException($"English Channel map is missing or not imported: {MapPath}");

            DemoUnitConfig[] witches = AssetDatabase.FindAssets("t:DemoUnitConfig", new[] { "Assets/Demo1/Resources/Configs/Units" })
                .Select(AssetDatabase.GUIDToAssetPath)
                .Select(AssetDatabase.LoadAssetAtPath<DemoUnitConfig>)
                .Where(config => config != null && config.Team == DemoTeam.Player)
                .OrderBy(config => config.SpawnOrder)
                .ToArray();
            if (witches.Length == 0)
                throw new System.InvalidOperationException("No player witch configs were found for the base roster.");

            System.IO.Directory.CreateDirectory(System.IO.Path.GetDirectoryName(ScenePath));
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            GameObject root = new GameObject("Base Command Runtime");
            BaseCommandSceneController controller = root.AddComponent<BaseCommandSceneController>();
            controller.ConfigureAssets(map, witches);
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene, ScenePath);
            AssetDatabase.SaveAssets();
            Debug.Log($"Base command scene rebuilt: {ScenePath} ({witches.Length} witches)");
        }
    }
}
