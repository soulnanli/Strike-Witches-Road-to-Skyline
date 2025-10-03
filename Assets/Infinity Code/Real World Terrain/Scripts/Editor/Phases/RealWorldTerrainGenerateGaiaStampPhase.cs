/*         INFINITY CODE         */
/*   https://infinity-code.com   */

#if GAIA_PRESENT || GAIA_PRO_PRESENT || GAIA_2_PRESENT || GAIA_2023

#if !GAIA_PRO_PRESENT && !GAIA_2_PRESENT && !GAIA_2023
#define OLD_GAIA
#endif

using System;
using System.IO;
using System.Linq;
using System.Reflection;
using Gaia;
using InfinityCode.RealWorldTerrain.Generators;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace InfinityCode.RealWorldTerrain.Phases
{
    public class RealWorldTerrainGenerateGaiaStampPhase : RealWorldTerrainPhase
    {
        private Scanner scanner;
        private Stamper stamper;

        public override string title
        {
            get { return "Generate Gaia Stamp..."; }
        }

        public override void Enter()
        {
            try
            {
                RealWorldTerrainGaiaStampGenerator.Generate();
            }
            catch
            {
                phaseComplete = true;
            }
            if (!phaseComplete) return;

#if OLD_GAIA
            string basesDir = "Assets/Gaia/Stamps/Bases/";

            if (!Directory.Exists(basesDir)) Directory.CreateDirectory(basesDir);

            string basesDataDir = basesDir + "Data/";
            if (!Directory.Exists(basesDataDir)) Directory.CreateDirectory(basesDataDir);
#endif
            
            GaiaConstants.RawBitDepth bd = GaiaConstants.RawBitDepth.Sixteen;
            int resolution = 0;
            scanner.LoadRawFile(RealWorldTerrainGaiaStampGenerator.fullFilename, GaiaConstants.RawByteOrder.IBM, ref bd, ref resolution);

#if OLD_GAIA
            scanner.m_featureType = GaiaConstants.FeatureType.Bases;
#else
            scanner.m_exportFolder = GaiaDirectories.GetScannerExportDirectory();
            scanner.m_exportFileName = RealWorldTerrainGaiaStampGenerator.shortFilename;
            scanner.m_baseLevel = 0;
#endif
            scanner.SaveScan();

            AssetDatabase.Refresh();

            Selection.activeGameObject = stamper.gameObject;
#if OLD_GAIA
            stamper.LoadStamp(basesDir + RealWorldTerrainGaiaStampGenerator.shortFilename + ".jpg");
#else
            string maskFilename = GaiaDirectories.GetScannerExportDirectory() + "/" + RealWorldTerrainGaiaStampGenerator.shortFilename + ".exr";
            Texture2D maskTexture = AssetDatabase.LoadAssetAtPath<Texture2D>(maskFilename);
            stamper.m_settings.m_imageMasks = new[]
            {
                new ImageMask
                {
                    ImageMaskTexture = maskTexture
                }
            };
            stamper.m_seaLevel = 0;

            float maxElevation = RealWorldTerrainGeo.MAX_ELEVATION;
            float minElevation = -RealWorldTerrainGeo.MAX_ELEVATION;

            if (prefs.elevationRange == RealWorldTerrainElevationRange.autoDetect)
            {
                double maxEl, minEl;
                RealWorldTerrainElevationGenerator.GetElevationRange(out minEl, out maxEl);
                maxElevation = (float)maxEl + prefs.autoDetectElevationOffset.y;
                minElevation = (float)minEl - prefs.autoDetectElevationOffset.x;
            }
            else if (prefs.elevationRange == RealWorldTerrainElevationRange.fixedValue)
            {
                maxElevation = prefs.fixedMaxElevation;
                minElevation = prefs.fixedMinElevation;
            }

            float sY = (maxElevation - minElevation) / 1000;

            stamper.transform.localScale = new Vector3(100, sY, 100);
#endif

            Complete();
        }

        public override void Finish()
        {
            if (scanner != null && scanner.gameObject != null) Object.DestroyImmediate(scanner.gameObject);

            scanner = null;
            stamper = null;
        }

        private static string GetAssetPath(string name)
        {
            string[] assets = AssetDatabase.FindAssets(name, null);
            if (assets.Length > 0)
            {
                return AssetDatabase.GUIDToAssetPath(assets[0]);
            }
            return null;
        }

        private static GaiaSettings GetSettingsAsset()
        {
            Assembly[] assemblies = System.AppDomain.CurrentDomain.GetAssemblies();
            Assembly gaiaEditorAssembly = assemblies.FirstOrDefault(a => a.GetName().Name == "GaiaEditor");
            if (gaiaEditorAssembly == null) return null;
            
            Type gaiaManagerEditorType = gaiaEditorAssembly.GetType("Gaia.GaiaManagerEditor");
            if (gaiaManagerEditorType == null) return null;
            
            MethodInfo createSettingsAssetMethod = gaiaManagerEditorType.GetMethod("CreateSettingsAsset", BindingFlags.Static | BindingFlags.Public);
            if (createSettingsAssetMethod == null) return null;
            
            return createSettingsAssetMethod.Invoke(null, null) as GaiaSettings;
        }

        [MenuItem("test/test" )]
        private static void Test()
        {
            GetSettingsAsset();
        }

        public override void Start()
        {
            try
            {
                GaiaSessionManager.GetSessionManager();
#if OLD_GAIA
                GaiaSettings m_settings = (GaiaSettings)Gaia.Utils.GetAssetScriptableObject("GaiaSettings");
#else
                GaiaSettings m_settings = (GaiaSettings)GaiaUtils.GetAssetScriptableObject("GaiaSettings");
#endif

                if (m_settings == null) m_settings = GetSettingsAsset();

                GaiaDefaults m_defaults = m_settings.m_currentDefaults;

                if (TerrainHelper.GetActiveTerrainCount() == 0)
                {
#if OLD_GAIA
                    GaiaResource m_resources = m_settings.m_currentResources;
                    m_defaults.CreateTerrain(m_resources);
#else
                    WorldCreationSettings worldCreationSettings = ScriptableObject.CreateInstance<WorldCreationSettings>();
                    worldCreationSettings.m_xTiles = m_settings.m_tilesX;
                    worldCreationSettings.m_zTiles = m_settings.m_tilesZ;
                    worldCreationSettings.m_tileSize = m_settings.m_currentDefaults.m_terrainSize;
                    worldCreationSettings.m_tileHeight = m_settings.m_tilesX * worldCreationSettings.m_tileSize;
                    worldCreationSettings.m_createInScene = m_settings.m_createTerrainScenes;
                    worldCreationSettings.m_autoUnloadScenes = m_settings.m_unloadTerrainScenes;
                    worldCreationSettings.m_applyFloatingPointFix = m_settings.m_floatingPointFix;

                    Type gsm = typeof(GaiaSessionManager);

                    MethodInfo createWorldMethod = gsm.GetMethod("CreateWorld", BindingFlags.Static | BindingFlags.Public, null, new[]{typeof(WorldCreationSettings), typeof(bool) }, null);
                    if (createWorldMethod != null)
                    {
                        createWorldMethod.Invoke(null, new object[] { worldCreationSettings, true });
                    }
                    else
                    {
                        createWorldMethod = gsm.GetMethod("CreateOrUpdateWorld", BindingFlags.Static | BindingFlags.Public, null, new[] { typeof(WorldCreationSettings), typeof(bool), typeof(bool) }, null);
                        if (createWorldMethod != null)
                        {
                            createWorldMethod.Invoke(null, new object[] { worldCreationSettings, true, false });
                        }
                        else
                        {
                            phaseComplete = true;
                            NextPhase();
                            return;
                        }
                    }

#if GAIA_PRO_PRESENT
                    WorldOriginEditor.m_sessionManagerExits = true;
#endif
#endif
                }

                GameObject gaiaObj = GameObject.Find("Gaia");
                if (gaiaObj == null) gaiaObj = GameObject.Find("Gaia Tools");
                if (gaiaObj == null) gaiaObj = new GameObject("Gaia");

                GameObject stamperObj = GameObject.Find("Stamper");
                if (stamperObj == null)
                {
                    stamperObj = new GameObject("Stamper");
                    stamperObj.transform.parent = gaiaObj.transform;
                    stamper = stamperObj.AddComponent<Stamper>();
                    stamper.FitToTerrain();
#if OLD_GAIA
                    stamper.m_resources = m_resources;
                    stamperObj.transform.position = new Vector3(stamper.m_x, stamper.m_y, stamper.m_z);
#endif
                }
                else stamper = stamperObj.GetComponent<Stamper>();

                GameObject scannerObj = GameObject.Find("Scanner");
                if (scannerObj == null)
                {
                    scannerObj = new GameObject("Scanner");
                    scannerObj.transform.parent = gaiaObj.transform;
                    scannerObj.transform.position = TerrainHelper.GetActiveTerrainCenter(false);
                    scanner = scannerObj.AddComponent<Scanner>();

                    string matPath = GetAssetPath("GaiaScannerMaterial");
                    if (!string.IsNullOrEmpty(matPath)) scanner.m_previewMaterial = AssetDatabase.LoadAssetAtPath<Material>(matPath);
                }
                else scanner = scannerObj.GetComponent<Scanner>();
            }
            catch (Exception e)
            {
                Debug.LogError(e.Message + "\n" + e.StackTrace);
                phaseComplete = true;
                NextPhase();
            }
        }
    }
}

#endif