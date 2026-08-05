using System;
using System.Collections.Generic;
using System.Linq;
using SWRTS.Demo1;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace SWRTS.Prototype.BaseScene
{
    [DefaultExecutionOrder(-100)]
    public sealed class BaseCommandSceneController : MonoBehaviour
    {
        private sealed class WitchRow
        {
            public DemoUnitConfig Config;
            public Button Button;
            public Image SelectionMark;
            public Text StateText;
        }

        private static readonly Color DeepNavy = new Color(0.025f, 0.055f, 0.075f, 0.98f);
        private static readonly Color PanelNavy = new Color(0.04f, 0.09f, 0.115f, 0.97f);
        private static readonly Color CardNavy = new Color(0.065f, 0.135f, 0.16f, 0.98f);
        private static readonly Color Cyan = new Color(0.25f, 0.9f, 0.92f, 1f);
        private static readonly Color PaleCyan = new Color(0.78f, 0.97f, 0.96f, 1f);
        private static readonly Color Amber = new Color(1f, 0.69f, 0.25f, 1f);
        private static readonly Color Muted = new Color(0.52f, 0.68f, 0.69f, 1f);
        private static readonly Vector2 FolkestoneNormalizedPosition = new Vector2(0.835f, 0.82f);
        private static readonly Vector2 EnglishChannelMapSizeKilometers = new Vector2(560f, 315f);

        [SerializeField] private Texture2D _englishChannelMap;
        [SerializeField] private DemoUnitConfig[] _availableWitches;

        private readonly HashSet<DemoUnitConfig> _selected = new HashSet<DemoUnitConfig>();
        private readonly HashSet<DemoUnitConfig> _deployed = new HashSet<DemoUnitConfig>();
        private readonly Dictionary<DemoUnitConfig, WitchRow> _witchRows = new Dictionary<DemoUnitConfig, WitchRow>();
        private readonly List<GameObject> _unitTokens = new List<GameObject>();

        private RectTransform _mapLayer;
        private RectTransform _baseAnchor;
        private RectTransform _canvasRect;
        private GameObject _readinessPanel;
        private Text _statusText;
        private Text _selectionSummary;
        private Text _deployButtonLabel;
        private Button _deployButton;
        private Font _font;
        private bool _initialized;
        private bool _operationalLevelStarted;
        private Demo1GameController _operationalController;

        public int AvailableWitchCount => _availableWitches?.Count(config => config != null) ?? 0;
        public int SelectedWitchCount => _selected.Count;
        public int DeployedWitchCount => _deployed.Count;
        public bool IsReadinessPanelOpen => _readinessPanel != null && _readinessPanel.activeSelf;
        public string StatusMessage { get; private set; } = "点击 501 基地进行出动整备。";
        public Vector2 BaseMapPosition => FolkestoneNormalizedPosition;
        public Vector2 MapSizeKilometers => EnglishChannelMapSizeKilometers;
        public bool IsOperationalLevelStarted => _operationalLevelStarted;
        public IReadOnlyCollection<string> DeployedWitchNames => _deployed.Select(config => config.DisplayName).ToArray();

        private void Start()
        {
            Initialize();
        }

        private void LateUpdate()
        {
            if (!_initialized)
                return;

            RefreshReadinessPanel();
            if (_baseAnchor == null || _canvasRect == null || _operationalController == null ||
                !_operationalController.IsInitialized || _operationalController.BattleCamera == null)
                return;

            Vector3 screen = _operationalController.BattleCamera.WorldToScreenPoint(_operationalController.BasePosition);
            bool visible = screen.z > 0f && screen.x >= 0f && screen.x <= Screen.width && screen.y >= 0f && screen.y <= Screen.height;
            _baseAnchor.gameObject.SetActive(visible);
            if (visible && RectTransformUtility.ScreenPointToLocalPointInRectangle(_canvasRect, screen, null, out Vector2 localPoint))
                _baseAnchor.anchoredPosition = localPoint;
        }

        public void ConfigureAssets(Texture2D mapTexture, DemoUnitConfig[] witchConfigs)
        {
            _englishChannelMap = mapTexture;
            _availableWitches = witchConfigs;
        }

        public void Initialize()
        {
            if (_initialized)
                return;

            _initialized = true;
            LoadWitchConfigs();
            StartOperationalLevel(Array.Empty<DemoUnitConfig>());
            BuildInterface();
            SetStatus(StatusMessage);
            RefreshReadinessPanel();
        }

        public void OpenReadinessPanel()
        {
            Initialize();
            _readinessPanel.SetActive(true);
            SetStatus("501 统合战斗航空团：请选择本次出动的魔女。", false);
            RefreshReadinessPanel();
        }

        public void CloseReadinessPanel()
        {
            if (_readinessPanel != null)
                _readinessPanel.SetActive(false);
            SetStatus("整备面板已关闭。点击基地可重新打开。", false);
        }

        public bool SetWitchSelected(string displayName, bool selected)
        {
            Initialize();
            DemoUnitConfig config = _availableWitches.FirstOrDefault(item =>
                item != null && string.Equals(item.DisplayName, displayName, StringComparison.Ordinal));
            if (config == null || !IsStandby(config))
                return false;

            if (selected)
                _selected.Add(config);
            else
                _selected.Remove(config);
            RefreshReadinessPanel();
            return true;
        }

        public void SelectAllAvailable()
        {
            Initialize();
            foreach (DemoUnitConfig config in _availableWitches.Where(config => config != null && IsStandby(config)))
                _selected.Add(config);
            RefreshReadinessPanel();
        }

        public void ClearSelection()
        {
            _selected.Clear();
            RefreshReadinessPanel();
        }

        public bool DeploySelected()
        {
            Initialize();
            if (_selected.Count == 0)
            {
                SetStatus("无法出动：请至少选择一名待命魔女。", true);
                return false;
            }

            if (_operationalController == null || !_operationalController.IsInitialized)
            {
                SetStatus("战区地图仍在初始化，请稍候。", true);
                return false;
            }

            DemoUnitConfig[] sortie = _selected.Where(IsStandby).OrderBy(config => config.SpawnOrder).ToArray();
            DemoCommandResult result = _operationalController.RequestSortie(sortie);
            if (!result.Success)
            {
                SetStatus(result.Message, true);
                return false;
            }
            _selected.Clear();
            SetStatus($"出动命令已下达：{string.Join("、", sortie.Select(config => config.DisplayName))}。", false);
            RefreshReadinessPanel();
            return true;
        }

        private void StartOperationalLevel(DemoUnitConfig[] sortie)
        {
            if (_operationalLevelStarted)
                return;

            _operationalLevelStarted = true;
            GameObject runtime = new GameObject("Demo1 Operational Runtime");
            runtime.transform.SetParent(transform, false);
            _operationalController = runtime.AddComponent<Demo1GameController>();
            _operationalController.ConfigureOperationalLevel(
                _englishChannelMap,
                sortie,
                EnglishChannelMapSizeKilometers,
                FolkestoneNormalizedPosition);
        }

        private void LoadWitchConfigs()
        {
            if (_availableWitches == null || _availableWitches.Length == 0)
            {
                _availableWitches = Resources.LoadAll<DemoUnitConfig>("Configs/Units")
                    .Where(config => config != null && config.Team == DemoTeam.Player)
                    .OrderBy(config => config.SpawnOrder)
                    .ToArray();
            }
            else
            {
                _availableWitches = _availableWitches
                    .Where(config => config != null && config.Team == DemoTeam.Player)
                    .OrderBy(config => config.SpawnOrder)
                    .ToArray();
            }
        }

        private void BuildInterface()
        {
            _font = CreateInterfaceFont();
            EnsureEventSystem();
            EnsureCamera();

            GameObject canvasObject = new GameObject("Base Command Canvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            canvasObject.transform.SetParent(transform, false);
            Canvas canvas = canvasObject.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 50;
            CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;

            _canvasRect = canvasObject.GetComponent<RectTransform>();
            CreateBaseMarker(canvasObject.transform);
            CreateReadinessPanel(canvasObject.transform);
            _readinessPanel.SetActive(false);
        }

        private void CreateMapGrid(RectTransform parent)
        {
            GameObject tint = CreateImage("Map Tactical Tint", parent, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero,
                new Color(0.02f, 0.12f, 0.14f, 0.18f)).gameObject;
            tint.GetComponent<Image>().raycastTarget = false;

            for (int index = 1; index < 8; index++)
            {
                float x = index / 8f;
                Image vertical = CreateImage($"Longitude {index}", parent, new Vector2(x, 0f), new Vector2(x, 1f),
                    new Vector2(-0.5f, 0f), new Vector2(0.5f, 0f), new Color(0.72f, 0.94f, 0.9f, 0.13f));
                vertical.raycastTarget = false;
            }
            for (int index = 1; index < 5; index++)
            {
                float y = index / 5f;
                Image horizontal = CreateImage($"Latitude {index}", parent, new Vector2(0f, y), new Vector2(1f, y),
                    new Vector2(0f, -0.5f), new Vector2(0f, 0.5f), new Color(0.72f, 0.94f, 0.9f, 0.13f));
                horizontal.raycastTarget = false;
            }
        }

        private void CreateMapLabels(RectTransform parent)
        {
            CreateText("Map Title", parent, "BRITANNIA · GALLIA / 1944", 18, TextAnchor.MiddleLeft, PaleCyan,
                new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(195f, -88f), new Vector2(330f, 34f), FontStyle.Bold);
            CreateText("Scale", parent, "战区参考范围 约 64 × 36 km  ·  原型比例", 15, TextAnchor.MiddleRight, PaleCyan,
                new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(-244f, 56f), new Vector2(420f, 30f));
        }

        private void SetRealScaleLabel()
        {
            Transform scale = _mapLayer != null ? _mapLayer.Find("Scale") : null;
            Text label = scale != null ? scale.GetComponent<Text>() : null;
            if (label != null)
                label.text = "REAL-SCALE THEATRE  ·  560 × 315 km  ·  1 UNIT = 1 km";
        }

        private void CreateBaseMarker(Transform parent)
        {
            RectTransform anchor = CreateRect("501 Base Anchor", parent, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                Vector2.zero, new Vector2(110f, 110f));
            _baseAnchor = anchor;

            Image alertArea = anchor.gameObject.AddComponent<Image>();
            alertArea.color = new Color(0.25f, 0.92f, 0.9f, 0.12f);
            alertArea.raycastTarget = false;
            Outline ring = anchor.gameObject.AddComponent<Outline>();
            ring.effectColor = new Color(0.3f, 1f, 0.95f, 0.55f);
            ring.effectDistance = new Vector2(2f, -2f);

            RectTransform buttonRect = CreateRect("501 Base", anchor, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                Vector2.zero, new Vector2(74f, 74f));
            Image buttonImage = buttonRect.gameObject.AddComponent<Image>();
            buttonImage.color = DeepNavy;
            Outline buttonOutline = buttonRect.gameObject.AddComponent<Outline>();
            buttonOutline.effectColor = Cyan;
            buttonOutline.effectDistance = new Vector2(3f, -3f);
            Button button = buttonRect.gameObject.AddComponent<Button>();
            button.targetGraphic = buttonImage;
            button.onClick.AddListener(OpenReadinessPanel);

            CreateText("Base Emblem", buttonRect, "501", 24, TextAnchor.MiddleCenter, Color.white,
                Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero, FontStyle.Bold);
            CreateText("Base Label", anchor, "501 基地  ·  福克斯通\n整备 / 出动", 17, TextAnchor.UpperCenter, PaleCyan,
                new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, -31f), new Vector2(210f, 52f), FontStyle.Bold);
        }

        private void CreateTopBar(Transform parent)
        {
            Image bar = CreateImage("Top Command Bar", parent, new Vector2(0f, 1f), Vector2.one,
                new Vector2(0f, -36f), new Vector2(0f, 72f), DeepNavy);
            CreateText("Title", bar.transform, "联合战区司令部  /  基地整备", 28, TextAnchor.MiddleLeft, Color.white,
                new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(338f, 0f), new Vector2(620f, 52f), FontStyle.Bold);
            CreateText("Operation", bar.transform, "OPERATION CHANNEL WATCH  ·  05:40", 16, TextAnchor.MiddleRight, Muted,
                new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), new Vector2(-338f, 0f), new Vector2(620f, 52f));
        }

        private void CreateStatusBar(Transform parent)
        {
            Image bar = CreateImage("Command Status", parent, Vector2.zero, Vector2.right,
                new Vector2(0f, 26f), new Vector2(0f, 52f), DeepNavy);
            _statusText = CreateText("Status Text", bar.transform, StatusMessage, 17, TextAnchor.MiddleLeft, PaleCyan,
                Vector2.zero, Vector2.one, Vector2.zero, new Vector2(-56f, 0f));
        }

        private void CreateReadinessPanel(Transform parent)
        {
            RectTransform panel = CreateRect("Sortie Readiness Panel", parent, Vector2.zero, Vector2.up,
                new Vector2(236f, -10f), new Vector2(472f, -124f));
            Image panelImage = panel.gameObject.AddComponent<Image>();
            panelImage.color = PanelNavy;
            Outline outline = panel.gameObject.AddComponent<Outline>();
            outline.effectColor = new Color(Cyan.r, Cyan.g, Cyan.b, 0.48f);
            outline.effectDistance = new Vector2(2f, -2f);
            _readinessPanel = panel.gameObject;

            CreateText("Panel Header", panel, "501 JFW  ·  出动整备", 24, TextAnchor.MiddleLeft, Color.white,
                new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(195f, -58f), new Vector2(342f, 46f), FontStyle.Bold);
            Button close = CreateButton("Close", panel, "×", 25, new Vector2(1f, 1f), new Vector2(1f, 1f),
                new Vector2(-25f, -58f), new Vector2(42f, 42f), CardNavy, Color.white, CloseReadinessPanel);
            close.GetComponent<Outline>().effectColor = Muted;

            _selectionSummary = CreateText("Selection Summary", panel, "", 15, TextAnchor.MiddleLeft, Muted,
                new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(224f, -98f), new Vector2(400f, 28f));

            float rowTop = -154f;
            foreach (DemoUnitConfig config in _availableWitches)
            {
                DemoUnitConfig captured = config;
                RectTransform row = CreateRect($"Witch {config.DisplayName}", panel, new Vector2(0f, 1f), Vector2.one,
                    new Vector2(0f, rowTop - 39f), new Vector2(-44f, 78f));
                Image background = row.gameObject.AddComponent<Image>();
                background.color = CardNavy;
                Button rowButton = row.gameObject.AddComponent<Button>();
                rowButton.targetGraphic = background;
                rowButton.onClick.AddListener(() => ToggleWitch(captured));

                Image selectionMark = CreateImage("Selection Mark", row, new Vector2(0f, 0f), new Vector2(0f, 1f),
                    new Vector2(3f, 0f), new Vector2(6f, 0f), Muted);
                CreateText("Name", row, config.DisplayName, 19, TextAnchor.MiddleLeft, Color.white,
                    new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(170f, 17f), new Vector2(290f, 28f), FontStyle.Bold);
                DemoUnitStats stats = config.Stats ?? new DemoUnitStats();
                CreateText("Stats", row, $"HP {stats.MaxHealth:0}   MP {stats.MaxMagic:0}   SH {stats.MaxShield:0}", 13,
                    TextAnchor.MiddleLeft, Muted, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(190f, -21f), new Vector2(330f, 24f));
                Text state = CreateText("State", row, "待命", 15, TextAnchor.MiddleRight, Cyan,
                    new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), new Vector2(-72f, 0f), new Vector2(120f, 36f), FontStyle.Bold);
                _witchRows[config] = new WitchRow { Config = config, Button = rowButton, SelectionMark = selectionMark, StateText = state };
                rowTop -= 88f;
            }

            Button selectAll = CreateButton("Select All", panel, "全选", 16, new Vector2(0f, 0f), new Vector2(0f, 0f),
                new Vector2(74f, 104f), new Vector2(104f, 42f), CardNavy, PaleCyan, SelectAllAvailable);
            selectAll.GetComponent<Outline>().effectColor = Muted;
            Button clear = CreateButton("Clear", panel, "清空", 16, new Vector2(0f, 0f), new Vector2(0f, 0f),
                new Vector2(190f, 104f), new Vector2(104f, 42f), CardNavy, PaleCyan, ClearSelection);
            clear.GetComponent<Outline>().effectColor = Muted;
            _deployButton = CreateButton("Deploy", panel, "", 18, new Vector2(1f, 0f), new Vector2(1f, 0f),
                new Vector2(-116f, 104f), new Vector2(190f, 48f), Cyan, DeepNavy, () => DeploySelected());
            _deployButtonLabel = _deployButton.GetComponentInChildren<Text>();
        }

        private void ToggleWitch(DemoUnitConfig config)
        {
            if (!IsStandby(config))
            {
                SetStatus($"{config.DisplayName} 当前不在待命状态。", true);
                return;
            }
            if (!_selected.Add(config))
                _selected.Remove(config);
            RefreshReadinessPanel();
        }

        private void RefreshReadinessPanel()
        {
            if (!_initialized || _selectionSummary == null)
                return;

            _deployed.Clear();
            foreach (DemoUnitConfig config in _availableWitches.Where(config => config != null))
            {
                DemoUnitModel unit = GetUnit(config);
                if (unit != null && unit.IsOperational)
                    _deployed.Add(config);
            }
            _selected.RemoveWhere(config => !IsStandby(config));
            int standbyCount = _availableWitches.Count(config => config != null && IsStandby(config));
            _selectionSummary.text = $"待命 {standbyCount}  ·  已选择 {_selected.Count}  ·  已出动 {_deployed.Count}";
            foreach (WitchRow row in _witchRows.Values)
            {
                DemoUnitModel unit = GetUnit(row.Config);
                bool deployed = unit != null && unit.IsOperational;
                bool selected = _selected.Contains(row.Config);
                row.SelectionMark.color = deployed ? Amber : selected ? Cyan : Muted;
                row.StateText.text = DeploymentStateLabel(unit, selected);
                row.StateText.color = deployed ? Amber : selected ? Cyan : Muted;
                row.Button.interactable = IsStandby(row.Config);
            }
            _deployButton.interactable = _selected.Count > 0;
            _deployButtonLabel.text = _selected.Count > 0 ? $"出动  {_selected.Count}" : "选择魔女";
        }

        private DemoUnitModel GetUnit(DemoUnitConfig config)
        {
            return _operationalController != null ? _operationalController.GetConfiguredUnit(config) : null;
        }

        private bool IsStandby(DemoUnitConfig config)
        {
            DemoUnitModel unit = GetUnit(config);
            return unit != null && unit.DeploymentState == DemoUnitDeploymentState.Standby;
        }

        private static string DeploymentStateLabel(DemoUnitModel unit, bool selected)
        {
            if (unit == null)
                return "初始化";
            switch (unit.DeploymentState)
            {
                case DemoUnitDeploymentState.Active: return "已出动";
                case DemoUnitDeploymentState.Returning: return "返航中";
                case DemoUnitDeploymentState.Servicing: return $"整备 {unit.TurnaroundRemaining:0}s";
                case DemoUnitDeploymentState.Lost: return "损失";
                default: return selected ? "已编入" : "待命";
            }
        }

        private void RebuildDeployedUnitTokens()
        {
            foreach (GameObject token in _unitTokens)
                Destroy(token);
            _unitTokens.Clear();

            DemoUnitConfig[] deployed = _deployed.OrderBy(config => config.SpawnOrder).ToArray();
            for (int index = 0; index < deployed.Length; index++)
            {
                float angle = Mathf.Lerp(205f, 335f, deployed.Length == 1 ? 0.5f : index / (float)(deployed.Length - 1));
                Vector2 offset = new Vector2(Mathf.Cos(angle * Mathf.Deg2Rad), Mathf.Sin(angle * Mathf.Deg2Rad)) * 128f;
                RectTransform token = CreateRect($"Map Unit {deployed[index].DisplayName}", _mapLayer,
                    FolkestoneNormalizedPosition, FolkestoneNormalizedPosition, offset, new Vector2(156f, 42f));
                Image image = token.gameObject.AddComponent<Image>();
                image.color = DeepNavy;
                Outline outline = token.gameObject.AddComponent<Outline>();
                outline.effectColor = Cyan;
                outline.effectDistance = new Vector2(2f, -2f);
                CreateText("Unit Label", token, $"◆  {deployed[index].DisplayName}", 14, TextAnchor.MiddleCenter, PaleCyan,
                    Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero, FontStyle.Bold);
                _unitTokens.Add(token.gameObject);
            }
        }

        private void SetStatus(string message, bool warning = false)
        {
            StatusMessage = message;
            if (_statusText != null)
            {
                _statusText.text = message;
                _statusText.color = warning ? Amber : PaleCyan;
            }
        }

        private void EnsureEventSystem()
        {
            if (EventSystem.current != null)
                return;
            GameObject eventSystem = new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));
            if (Application.isPlaying)
                DontDestroyOnLoad(eventSystem);
            else
                eventSystem.transform.SetParent(transform, false);
        }

        private void EnsureCamera()
        {
            if (Camera.main != null)
                return;
            GameObject cameraObject = new GameObject("Base Command Camera", typeof(Camera), typeof(AudioListener));
            cameraObject.tag = "MainCamera";
            cameraObject.transform.SetParent(transform, false);
            cameraObject.transform.position = new Vector3(0f, 0f, -10f);
            Camera camera = cameraObject.GetComponent<Camera>();
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = DeepNavy;
            camera.orthographic = true;
        }

        private static Font CreateInterfaceFont()
        {
            try
            {
                Font font = Font.CreateDynamicFontFromOSFont(
                    new[] { "Microsoft YaHei UI", "Microsoft YaHei", "PingFang SC", "Noto Sans CJK SC", "Arial" }, 18);
                if (font != null)
                    return font;
            }
            catch (Exception)
            {
                // Fall through to Unity's built-in font on platforms without the requested fonts.
            }
            return Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        }

        private RectTransform CreateRect(string name, Transform parent, Vector2 anchorMin, Vector2 anchorMax,
            Vector2 anchoredPosition, Vector2 sizeDelta)
        {
            GameObject gameObject = new GameObject(name, typeof(RectTransform));
            RectTransform rect = gameObject.GetComponent<RectTransform>();
            rect.SetParent(parent, false);
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = anchoredPosition;
            rect.sizeDelta = sizeDelta;
            return rect;
        }

        private Image CreateImage(string name, Transform parent, Vector2 anchorMin, Vector2 anchorMax,
            Vector2 anchoredPosition, Vector2 sizeDelta, Color color)
        {
            RectTransform rect = CreateRect(name, parent, anchorMin, anchorMax, anchoredPosition, sizeDelta);
            Image image = rect.gameObject.AddComponent<Image>();
            image.color = color;
            return image;
        }

        private Text CreateText(string name, Transform parent, string content, int fontSize, TextAnchor alignment, Color color,
            Vector2 anchorMin, Vector2 anchorMax, Vector2 anchoredPosition, Vector2 sizeDelta, FontStyle style = FontStyle.Normal)
        {
            RectTransform rect = CreateRect(name, parent, anchorMin, anchorMax, anchoredPosition, sizeDelta);
            Text text = rect.gameObject.AddComponent<Text>();
            text.font = _font;
            text.text = content;
            text.fontSize = fontSize;
            text.alignment = alignment;
            text.color = color;
            text.fontStyle = style;
            text.raycastTarget = false;
            text.horizontalOverflow = HorizontalWrapMode.Overflow;
            text.verticalOverflow = VerticalWrapMode.Truncate;
            return text;
        }

        private Button CreateButton(string name, Transform parent, string label, int fontSize,
            Vector2 anchorMin, Vector2 anchorMax, Vector2 anchoredPosition, Vector2 sizeDelta,
            Color background, Color foreground, UnityEngine.Events.UnityAction action)
        {
            RectTransform rect = CreateRect(name, parent, anchorMin, anchorMax, anchoredPosition, sizeDelta);
            Image image = rect.gameObject.AddComponent<Image>();
            image.color = background;
            Outline outline = rect.gameObject.AddComponent<Outline>();
            outline.effectColor = Cyan;
            outline.effectDistance = new Vector2(2f, -2f);
            Button button = rect.gameObject.AddComponent<Button>();
            button.targetGraphic = image;
            button.onClick.AddListener(action);
            CreateText("Label", rect, label, fontSize, TextAnchor.MiddleCenter, foreground,
                Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero, FontStyle.Bold);
            return button;
        }
    }
}
