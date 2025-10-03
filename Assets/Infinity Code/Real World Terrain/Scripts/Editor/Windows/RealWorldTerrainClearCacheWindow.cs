/*         INFINITY CODE         */
/*   https://infinity-code.com   */

using System.IO;
using UnityEditor;
using UnityEngine;

namespace InfinityCode.RealWorldTerrain.Windows
{
    public class RealWorldTerrainClearCacheWindow : EditorWindow
    {
        private long elevationSize;
        private long osmSize;
        private long textureSize;

        private void ClearElevationCache()
        {
            RealWorldTerrainFileSystem.SafeDeleteDirectory(RealWorldTerrainEditorUtils.heightmapCacheFolder);
            elevationSize = 0;
        }

        private void ClearHistory()
        {
            RealWorldTerrainFileSystem.SafeDeleteDirectory(RealWorldTerrainEditorUtils.historyCacheFolder);
            RealWorldTerrainHistoryWindow.Load();
        }

        private void ClearOSMCache()
        {
            RealWorldTerrainFileSystem.SafeDeleteDirectory(RealWorldTerrainEditorUtils.osmCacheFolder);
            osmSize = 0;
        }

        private static void ClearSettings()
        {
            if (File.Exists(RealWorldTerrainPrefs.prefsFilename)) File.Delete(RealWorldTerrainPrefs.prefsFilename);
            RealWorldTerrainSettingsWindow.ClearSettings();
            RealWorldTerrainEditorUtils.ClearFoldersCache();
        }

        private void ClearTextureCache(bool errorOnly = false)
        {
            if (!errorOnly)
            {
                RealWorldTerrainFileSystem.SafeDeleteDirectory(RealWorldTerrainEditorUtils.textureCacheFolder);
                textureSize = 0;
            }
            else
            {
                string[] files = Directory.GetFiles(RealWorldTerrainEditorUtils.textureCacheFolder, "*.err", SearchOption.AllDirectories);
                foreach (string file in files) RealWorldTerrainFileSystem.SafeDeleteFile(file);
                textureSize = RealWorldTerrainFileSystem.GetDirectorySize(RealWorldTerrainEditorUtils.textureCacheFolder);
            }
        }

        private void OnEnable()
        {
            elevationSize = RealWorldTerrainFileSystem.GetDirectorySize(RealWorldTerrainEditorUtils.heightmapCacheFolder);
            osmSize = RealWorldTerrainFileSystem.GetDirectorySize(RealWorldTerrainEditorUtils.osmCacheFolder);
            textureSize = RealWorldTerrainFileSystem.GetDirectorySize(RealWorldTerrainEditorUtils.textureCacheFolder);
        }

        public static string FormatSize(long size)
        {
            if (size > 10485760) return size / 1048576 + " MB";
            if (size > 1024) return (size / 1048576f).ToString("0.000") + " MB";
            return size + " B";
        }

        private void OnGUI()
        {
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Elevations", FormatSize(elevationSize), EditorStyles.textField);
            if (GUILayout.Button("Open", GUILayout.ExpandWidth(false))) EditorUtility.RevealInFinder(RealWorldTerrainEditorUtils.heightmapCacheFolder);
            if (GUILayout.Button("Clear", GUILayout.ExpandWidth(false)))
            {
                if (EditorUtility.DisplayDialog("Clear cache", "Are you sure you want to clear the elevation cache?", "Yes", "No"))
                {
                    ClearElevationCache();
                    EditorUtility.DisplayDialog("Complete", "Clear cache complete.", "OK");
                }
            }
            EditorGUILayout.EndHorizontal();
            
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Textures", FormatSize(textureSize), EditorStyles.textField);
            if (GUILayout.Button("Open", GUILayout.ExpandWidth(false))) EditorUtility.RevealInFinder(RealWorldTerrainEditorUtils.textureCacheFolder);
            if (GUILayout.Button("Clear", GUILayout.ExpandWidth(false)))
            {
                int result = EditorUtility.DisplayDialogComplex("Clear cache", "Are you sure you want to clear the texture cache?", "Full", "No", "Errors only");
                if (result == 0)
                {
                    ClearTextureCache();
                    EditorUtility.DisplayDialog("Complete", "Clear cache complete.", "OK");
                }
                else if (result == 2)
                {
                    ClearTextureCache(true);
                    EditorUtility.DisplayDialog("Complete", "Clear cache complete.", "OK");
                }
            }
            EditorGUILayout.EndHorizontal();
            
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("OSM", FormatSize(osmSize), EditorStyles.textField);
            if (GUILayout.Button("Open", GUILayout.ExpandWidth(false))) EditorUtility.RevealInFinder(RealWorldTerrainEditorUtils.osmCacheFolder);
            if (GUILayout.Button("Clear", GUILayout.ExpandWidth(false)))
            {
                if (EditorUtility.DisplayDialog("Clear cache", "Are you sure you want to clear the OSM cache?", "Yes", "No"))
                {
                    ClearOSMCache();
                    EditorUtility.DisplayDialog("Complete", "Clear cache complete.", "OK");
                }
            }
            EditorGUILayout.EndHorizontal();
            
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("History");
            if (GUILayout.Button("Open", GUILayout.ExpandWidth(false))) EditorUtility.RevealInFinder(RealWorldTerrainEditorUtils.historyCacheFolder);
            if (GUILayout.Button("Clear", GUILayout.ExpandWidth(false)))
            {
                if (EditorUtility.DisplayDialog("Clear cache", "Are you sure you want to clear the history?", "Yes", "No"))
                {
                    ClearHistory();
                    EditorUtility.DisplayDialog("Complete", "Clear cache complete.", "OK");
                }
            }
            EditorGUILayout.EndHorizontal();
            
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Settings");
            if (GUILayout.Button("Clear", GUILayout.ExpandWidth(false)))
            {
                if (EditorUtility.DisplayDialog("Clear cache", "Are you sure you want to clear the settings?", "Yes", "No"))
                {
                    ClearSettings();
                    EditorUtility.DisplayDialog("Complete", "Clear cache complete.", "OK");
                }
            }
            EditorGUILayout.EndHorizontal();
            
            if (GUILayout.Button("Clear All"))
            {
                if (EditorUtility.DisplayDialog("Clear cache", "Are you sure you want to clear the cache?", "Yes", "No"))
                {
                    ClearElevationCache();
                    ClearTextureCache();
                    ClearOSMCache();
                    ClearHistory();
                    ClearSettings();
                    EditorUtility.DisplayDialog("Complete", "Clear cache complete.", "OK");
                    Close();
                }
            }
        }

        public static void OpenWindow()
        {
            RealWorldTerrainClearCacheWindow wnd = GetWindow<RealWorldTerrainClearCacheWindow>(true, "Clear cache");
            DontDestroyOnLoad(wnd);
        }
    }
}