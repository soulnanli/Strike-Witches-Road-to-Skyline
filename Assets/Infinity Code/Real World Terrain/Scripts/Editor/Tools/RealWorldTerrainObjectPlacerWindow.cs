/*         INFINITY CODE         */
/*   https://infinity-code.com   */

using System;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace InfinityCode.RealWorldTerrain.Tools
{
    public class RealWorldTerrainObjectPlacerWindow : EditorWindow
    {
        private static string[] gridLabels = { "Place New Object", "Update Position" };

        private static RealWorldTerrainObjectPlacerWindow wnd;
        private int isNewGameobject = 0;
        private GameObject obj;
        private double latitude;
        private double longitude;
        private double altitude;
        private RealWorldTerrainContainer container;
        private bool selectGameObject = true;
        private PlaceMode placeMode = PlaceMode.singleLocation;
        private double[] locations;
        private string multipleLocations;
        private string multipleLocationsFile;

        private bool hasCoordinates = false;
        private bool useAltitude = false;
        private double cursorLongitude;
        private double cursorLatitude;
        private double cursorAltitude;
        private Vector2 scrollPosition;

        private void DrawCursorLocation()
        {
            if (!hasCoordinates) return;
            
            EditorGUILayout.LabelField("Cursor Coordinates:");
            EditorGUILayout.LabelField("Latitude: ", cursorLatitude.ToString());
            EditorGUILayout.LabelField("Longitude: ", cursorLongitude.ToString());
            EditorGUILayout.LabelField("Altitude: ", cursorAltitude.ToString("F2") + " meters");
            EditorGUILayout.LabelField("Use CTRL+SHIFT to insert the coordinates.");

            if (Event.current.control && Event.current.shift)
            {
                latitude = cursorLatitude;
                longitude = cursorLongitude;
                altitude = cursorAltitude;
            }
        }

        private void DrawMultipleLocations()
        {
            EditorGUILayout.HelpBox("Use one location in decimal per line in \"latitude;longitude\" format without quotes.\n", MessageType.None);
            scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition, GUILayout.MaxHeight(200));
            multipleLocations = EditorGUILayout.TextArea(multipleLocations, GUILayout.ExpandHeight(true));
            EditorGUILayout.EndScrollView();
        }

        private void DrawMultipleLocationsFromFile()
        {
            EditorGUILayout.HelpBox("Use one location in decimal per line in \"latitude;longitude\" format without quotes.\n", MessageType.None);
            EditorGUILayout.BeginHorizontal();
            multipleLocationsFile = EditorGUILayout.TextField("File: ", multipleLocationsFile);
            if (GUILayout.Button("...", GUILayout.ExpandWidth(false)))
            {
                multipleLocationsFile = EditorUtility.OpenFilePanel("Select Input File", multipleLocationsFile, "txt");
            }
            EditorGUILayout.EndHorizontal();
        }

        private void DrawNewUI()
        {
            obj = EditorGUILayout.ObjectField("Prefab: ", obj, typeof(GameObject), true) as GameObject;

            placeMode = (PlaceMode)EditorGUILayout.EnumPopup("Place Mode", placeMode);
            if (placeMode == PlaceMode.singleLocation) DrawSingleLocation();
            else if (placeMode == PlaceMode.multipleLocations) DrawMultipleLocations();
            else if (placeMode == PlaceMode.multipleLocationsFromFile) DrawMultipleLocationsFromFile();
            
            selectGameObject = EditorGUILayout.Toggle("Select GameObject(s)?", selectGameObject);

            if (GUILayout.Button("Place") && ValidateFields()) PlaceItems();
        }

        private void DrawSingleLocation()
        {
            latitude = EditorGUILayout.DoubleField("Latitude", latitude);
            longitude = EditorGUILayout.DoubleField("Longitude", longitude);
            EditorGUILayout.BeginHorizontal();

            useAltitude = GUILayout.Toggle(useAltitude, GUIContent.none, GUILayout.Width(16));
            EditorGUI.BeginDisabledGroup(!useAltitude);
            altitude = EditorGUILayout.DoubleField("Altitude", altitude);
            EditorGUI.EndDisabledGroup();

            EditorGUILayout.EndHorizontal();
        }

        private void DrawUpdateUI()
        {
            obj = EditorGUILayout.ObjectField("GameObject: ", obj, typeof(GameObject), true) as GameObject;

            DrawSingleLocation();
            selectGameObject = EditorGUILayout.Toggle("Select GameObject?", selectGameObject);

            if (GUILayout.Button("Update") && ValidateFields())
            {
                UpdateGameObjectPosition(obj, longitude, latitude, altitude);
                if (selectGameObject) SelectGameObject(obj);
            }
        }

        private bool LoadMultipleLocations(string content)
        {
            if (string.IsNullOrEmpty(content))
            {
                ShowError("List of locations is empty.");
                return false;
            }
            
            string[] lines = content.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.RemoveEmptyEntries);
            if (lines.Length == 0)
            {
                ShowError("List of locations is empty.");
                return false;
            }
            
            locations = new double[lines.Length * 2];
            for (int i = 0; i < lines.Length; i++)
            {
                string[] coordinates = lines[i].Split(';');
                if (coordinates.Length != 2)
                {
                    ShowError(string.Format("Invalid coordinates in line {0}.", i + 1));
                    return false;
                }

                double lng, lat;
                if (!double.TryParse(coordinates[0].Trim(), out lat) || !double.TryParse(coordinates[1].Trim(), out lng))
                {
                    ShowError(string.Format("Invalid coordinates in line {0}.", i + 1));
                    return false;
                }

                locations[i * 2] = lng;
                locations[i * 2 + 1] = lat;
            }
            
            return true;
        }

        private bool LoadMultipleLocationsFromFile()
        {
            if (!File.Exists(multipleLocationsFile))
            {
                ShowError("Input file does not exist.");
                return false;
            }

            try
            {
                string content = File.ReadAllText(multipleLocationsFile);
                return LoadMultipleLocations(content);
            }
            catch (Exception e)
            {
                ShowError("Exception: " + e.Message);
                return false;
            }
        }

        private void OnDestroy()
        {
            EditorApplication.update -= OnUpdate;
            SceneView.duringSceneGui -= OnSceneGUI;
            wnd = null;
        }

        private void OnEnable()
        {
            OnDestroy();

            wnd = this;
            EditorApplication.update += OnUpdate;
            SceneView.duringSceneGui += OnSceneGUI;
        }

        private void OnGUI()
        {
            EditorGUI.BeginChangeCheck();
            isNewGameobject = GUILayout.SelectionGrid(isNewGameobject, gridLabels, 2);
            if (EditorGUI.EndChangeCheck()) obj = null;

            container = EditorGUILayout.ObjectField("Container", container, typeof(RealWorldTerrainContainer), true) as RealWorldTerrainContainer;

            if (isNewGameobject == 0) DrawNewUI();
            else DrawUpdateUI();

            DrawCursorLocation();
        }

        private void OnSceneGUI(SceneView view)
        {
            if (container == null) return;

            Vector2 mp = Event.current.mousePosition;
            mp.y = view.camera.pixelHeight - mp.y;

            Ray ray = HandleUtility.GUIPointToWorldRay(Event.current.mousePosition);
            RaycastHit hit;
            hasCoordinates = Physics.Raycast(ray, out hit);
            if (hasCoordinates) container.GetCoordinatesByWorldPosition(hit.point, out cursorLongitude, out cursorLatitude, out cursorAltitude);
        }

        private void OnUpdate()
        {
            Repaint();
        }

        [MenuItem("Window/Infinity Code/Real World Terrain/Tools/Object Placer")]
        public static void OpenWindow()
        {
            OpenWindow(null);
        }

        public static void OpenWindow(RealWorldTerrainContainer container)
        {
            if (wnd != null) wnd.Close();

            wnd = GetWindow<RealWorldTerrainObjectPlacerWindow>(false, "Object Placer", true);
            if (container == null)
            {
                wnd.container = RealWorldTerrainUtils.FindObjectOfType<RealWorldTerrainContainer>();
            }
            else wnd.container = container;
        }

        public static void OpenWindow(RealWorldTerrainContainer container, double lng, double lat)
        {
            OpenWindow(container);
            wnd.latitude = lat;
            wnd.longitude = lng;
        }

        private void PlaceItems()
        {
            if (placeMode == PlaceMode.singleLocation)
            {
                GameObject go = Instantiate(obj);
                UpdateGameObjectPosition(go, longitude, latitude, altitude);
                if (selectGameObject) SelectGameObject(go);
            }
            else
            {
                int count = locations.Length / 2;
                GameObject[] gos = new GameObject[count];
                
                for (int i = 0; i < count; i++)
                {
                    GameObject go = Instantiate(obj);
                    double lng = locations[i * 2];
                    double lat = locations[i * 2 + 1];
                    
                    UpdateGameObjectPosition(go, lng, lat);
                    gos[i] = go;
                }

                if (selectGameObject)
                {
                    Selection.objects = gos;
                    EditorGUIUtility.PingObject(gos[0]);
                }
            }
        }

        private void SelectGameObject(GameObject go)
        {
            if (!selectGameObject) return;

            Selection.activeGameObject = go;
            EditorGUIUtility.PingObject(go);
        }

        private static void ShowError(string message)
        {
            EditorUtility.DisplayDialog("Error", message, "OK");
        }

        private void UpdateGameObjectPosition(GameObject go, double lng, double lat, double alt = 0)
        {
            Vector3 worldPosition;
            bool status = false;

            if (useAltitude) status = container.GetWorldPosition(lng, lat, alt, out worldPosition);
            else status = container.GetWorldPosition(lng, lat, out worldPosition);

            if (status) go.transform.position = worldPosition;
        }

        private bool ValidateFields()
        {
            if (container == null)
            {
                ShowError("Please select Real World Terrain Container.");
                return false;
            }

            if (obj == null)
            {
                ShowError(string.Format("Please select {0}.", isNewGameobject == 0 ? "Prefab" : "GameObject"));
                return false;
            }

            if (!container.Contains(longitude, latitude))
            {
                ShowError("These the coordinates outside terrain.");
                return false;
            }

            if (placeMode == PlaceMode.multipleLocations)
            {
                if (!LoadMultipleLocations(multipleLocations)) return false;
            }
            else if (placeMode == PlaceMode.multipleLocationsFromFile)
            {
                if (!LoadMultipleLocationsFromFile()) return false;
            }

            return true;
        }

        private enum PlaceMode
        {
            singleLocation,
            multipleLocations,
            multipleLocationsFromFile
        }
    }
}