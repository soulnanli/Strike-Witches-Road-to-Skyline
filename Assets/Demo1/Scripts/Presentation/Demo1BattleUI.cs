using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace SWRTS.Demo1
{
    internal sealed class Demo1BattlePointerGuard : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, ICancelHandler
    {
        private Demo1BattleUI _owner;
        private int _pointerId;
        private bool _isTracking;

        public void Initialize(Demo1BattleUI owner)
        {
            _owner = owner;
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            if (eventData.button != PointerEventData.InputButton.Left)
                return;

            _pointerId = eventData.pointerId;
            _isTracking = true;
            _owner?.BeginPanelPointer(_pointerId);
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            EndTracking();
        }

        public void OnCancel(BaseEventData eventData)
        {
            EndTracking();
        }

        private void OnDisable()
        {
            EndTracking();
        }

        private void EndTracking()
        {
            if (!_isTracking)
                return;

            _isTracking = false;
            _owner?.EndPanelPointer(_pointerId);
        }
    }

    public sealed class Demo1BattleUI : MonoBehaviour
    {
        private sealed class BubbleView
        {
            public RectTransform Rect;
            public Image Image;
            public Button Button;
            public Text Text;
        }

        private readonly Dictionary<int, BubbleView> _bubbles = new Dictionary<int, BubbleView>();
        private readonly Dictionary<string, float> _lineScrollPositions = new Dictionary<string, float>();
        private readonly HashSet<int> _activePanelPointers = new HashSet<int>();
        private Demo1GameController _controller;
        private Canvas _canvas;
        private RectTransform _canvasRect;
        private RectTransform _bubbleLayer;
        private GameObject _panel;
        private RectTransform _panelContent;
        private Font _font;
        private int _openCombatId = -1;
        private int _selectedUnitId = -1;
        private float _nextPanelRefresh;
        private bool _panelDirty;
        private bool _commandFeedbackSuccess = true;
        private string _commandFeedback = string.Empty;
        private static readonly Color PanelBackground = new Color(0.018f, 0.035f, 0.05f, 1f);
        private static readonly Color PanelRaised = new Color(0.04f, 0.075f, 0.095f, 0.98f);
        private static readonly Color PlayerColor = new Color(0.16f, 0.72f, 0.94f, 1f);
        private static readonly Color EnemyColor = new Color(0.94f, 0.27f, 0.24f, 1f);
        private static readonly Color SuccessColor = new Color(0.28f, 0.9f, 0.62f, 1f);
        private static readonly Color WarningColor = new Color(1f, 0.68f, 0.2f, 1f);
        private static readonly Color MutedText = new Color(0.62f, 0.72f, 0.76f, 1f);
        private const float StrategicHudWidth = 340f;

        public bool IsPanelOpen => _panel != null && _panel.activeSelf;
        public int OpenCombatId => _openCombatId;
        public int SelectedUnitId => _selectedUnitId;

        public void Initialize(Demo1GameController controller)
        {
            _controller = controller;
            _font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            EnsureEventSystem();
            BuildCanvas();
            BuildPanelRoot();
        }

        public void Sync()
        {
            if (_controller == null || _controller.Simulation == null || _canvasRect == null)
                return;

            UpdatePanelLayout();
            SyncBubbles();
            if (!IsPanelOpen)
                return;

            DemoCombatModel combat = _controller.Simulation.GetCombat(_openCombatId);
            if (combat == null || combat.IsFinished)
            {
                ClosePanel();
                return;
            }

            if ((_panelDirty || Time.unscaledTime >= _nextPanelRefresh) && !IsPanelPointerBusy())
            {
                _nextPanelRefresh = Time.unscaledTime + 0.1f;
                _panelDirty = false;
                RebuildPanel(combat);
            }
        }

        public void OpenPanel(int combatId)
        {
            DemoCombatModel combat = _controller?.Simulation?.GetCombat(combatId);
            if (combat == null || combat.IsFinished)
                return;
            _openCombatId = combatId;
            _selectedUnitId = -1;
            _lineScrollPositions.Clear();
            _activePanelPointers.Clear();
            _commandFeedback = "请先选择一名己方参战单位";
            _commandFeedbackSuccess = true;
            _panelDirty = false;
            _nextPanelRefresh = Time.unscaledTime + 0.1f;
            _panel.SetActive(true);
            RebuildPanel(combat);
        }

        public void ClosePanel()
        {
            _openCombatId = -1;
            _selectedUnitId = -1;
            _activePanelPointers.Clear();
            _panelDirty = false;
            if (_panel != null)
                _panel.SetActive(false);
        }

        internal void BeginPanelPointer(int pointerId)
        {
            _activePanelPointers.Add(pointerId);
        }

        internal void EndPanelPointer(int pointerId)
        {
            _activePanelPointers.Remove(pointerId);
            _panelDirty = true;
        }

        private bool IsPanelPointerBusy()
        {
            return _activePanelPointers.Count > 0 || Input.GetMouseButton(0) || Input.GetMouseButtonUp(0);
        }

        private void EnsureEventSystem()
        {
            if (EventSystem.current != null)
                return;
            GameObject eventObject = new GameObject("Demo1 Event System", typeof(EventSystem), typeof(StandaloneInputModule));
            eventObject.transform.SetParent(transform, false);
        }

        private void BuildCanvas()
        {
            GameObject canvasObject = new GameObject("Battle Canvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            canvasObject.transform.SetParent(transform, false);
            _canvas = canvasObject.GetComponent<Canvas>();
            _canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            _canvas.sortingOrder = 1000;
            CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;
            _canvasRect = canvasObject.GetComponent<RectTransform>();

            _bubbleLayer = CreateRect("Battle Bubbles", _canvasRect, Vector2.zero, Vector2.zero, Vector2.zero);
            _bubbleLayer.anchorMin = Vector2.zero;
            _bubbleLayer.anchorMax = Vector2.one;
            _bubbleLayer.offsetMin = Vector2.zero;
            _bubbleLayer.offsetMax = Vector2.zero;
        }

        private void BuildPanelRoot()
        {
            _panel = new GameObject("Battle Panel", typeof(RectTransform), typeof(Image), typeof(Outline));
            _panel.transform.SetParent(_canvasRect, false);
            RectTransform rect = _panel.GetComponent<RectTransform>();
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = new Vector2(1180f, 800f);
            rect.anchoredPosition = Vector2.zero;
            Image background = _panel.GetComponent<Image>();
            background.color = PanelBackground;
            background.raycastTarget = true;

            Outline outline = _panel.GetComponent<Outline>();
            outline.effectColor = new Color(0.14f, 0.5f, 0.62f, 0.95f);
            outline.effectDistance = new Vector2(3f, -3f);

            _panelContent = CreateRect("Content", rect, Vector2.zero, Vector2.zero, Vector2.zero);
            _panelContent.anchorMin = Vector2.zero;
            _panelContent.anchorMax = Vector2.one;
            _panelContent.offsetMin = Vector2.zero;
            _panelContent.offsetMax = Vector2.zero;
            _panel.SetActive(false);
            UpdatePanelLayout();
        }

        private void UpdatePanelLayout()
        {
            if (_panel == null || _canvasRect == null || _canvas == null)
                return;

            RectTransform panelRect = _panel.GetComponent<RectTransform>();
            float canvasScale = Mathf.Max(0.1f, _canvas.scaleFactor);
            float desiredLeft = StrategicHudWidth / canvasScale + 12f;
            float x = desiredLeft + panelRect.sizeDelta.x * 0.5f - _canvasRect.rect.width * 0.5f;
            panelRect.anchoredPosition = new Vector2(x, 0f);
        }

        private void SyncBubbles()
        {
            HashSet<int> activeIds = new HashSet<int>();
            foreach (DemoCombatModel combat in _controller.Simulation.Combats.Where(item => !item.IsFinished))
            {
                activeIds.Add(combat.Id);
                BubbleView bubble;
                if (!_bubbles.TryGetValue(combat.Id, out bubble))
                {
                    bubble = CreateBubble(combat.Id);
                    _bubbles.Add(combat.Id, bubble);
                }
                UpdateBubble(combat, bubble);
            }

            foreach (int id in _bubbles.Keys.Where(id => !activeIds.Contains(id)).ToList())
            {
                Destroy(_bubbles[id].Rect.gameObject);
                _bubbles.Remove(id);
            }
        }

        private BubbleView CreateBubble(int combatId)
        {
            GameObject buttonObject = new GameObject($"Battle Bubble {combatId}", typeof(RectTransform), typeof(Image), typeof(Button), typeof(Outline));
            buttonObject.transform.SetParent(_bubbleLayer, false);
            RectTransform rect = buttonObject.GetComponent<RectTransform>();
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = new Vector2(178f, 70f);
            Image image = buttonObject.GetComponent<Image>();
            image.color = new Color(0.045f, 0.14f, 0.18f, 0.98f);
            Outline outline = buttonObject.GetComponent<Outline>();
            outline.effectColor = PlayerColor;
            outline.effectDistance = new Vector2(3f, -3f);
            Button button = buttonObject.GetComponent<Button>();
            button.targetGraphic = image;
            button.colors = CreateButtonColors(image.color);
            button.onClick.AddListener(() => OpenPanel(combatId));
            Text text = AddText(buttonObject.transform, "Label", string.Empty, Vector2.zero, new Vector2(168f, 64f), 14, TextAnchor.MiddleCenter, Color.white);
            text.fontStyle = FontStyle.Bold;
            return new BubbleView { Rect = rect, Image = image, Button = button, Text = text };
        }

        private void UpdateBubble(DemoCombatModel combat, BubbleView bubble)
        {
            Vector3 screen = _controller.BattleCamera.WorldToScreenPoint(combat.Center + Vector3.up * 1.5f);
            bool visible = screen.z > 0f;
            bubble.Rect.gameObject.SetActive(visible && !IsPanelOpen);
            if (!visible)
                return;

            Vector2 local;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(_canvasRect, screen, null, out local);
            float halfWidth = _canvasRect.rect.width * 0.5f - 104f;
            float halfHeight = _canvasRect.rect.height * 0.5f - 52f;
            bubble.Rect.anchoredPosition = new Vector2(Mathf.Clamp(local.x, -halfWidth, halfWidth), Mathf.Clamp(local.y + 55f, -halfHeight, halfHeight));

            int playerCount = CountTeam(combat, DemoTeam.Player);
            int enemyCount = CountTeam(combat, DemoTeam.Enemy);
            float screenEfficiency = _controller.Simulation.GetScreeningEfficiency(combat.Id, DemoTeam.Player);
            float playerStrength = _controller.Simulation.GetCombatStrength(combat.Id, DemoTeam.Player);
            float enemyStrength = _controller.Simulation.GetCombatStrength(combat.Id, DemoTeam.Enemy);
            bool advantaged = playerStrength >= enemyStrength;
            string trend = advantaged ? "优势" : "承压";
            string screening = screenEfficiency < 0.5f ? $"屏卫告警 {screenEfficiency:P0}" : $"屏卫稳定 {screenEfficiency:P0}";
            bubble.Text.text = $"战斗 #{combat.Id}  ·  {trend}\n我方 {playerCount}   /   敌方 {enemyCount}\n{screening}";
            bubble.Image.color = advantaged ? new Color(0.035f, 0.24f, 0.31f, 0.98f) : new Color(0.32f, 0.065f, 0.075f, 0.98f);
            bubble.Button.colors = CreateButtonColors(bubble.Image.color);
            bubble.Text.color = screenEfficiency < 0.5f ? new Color(1f, 0.82f, 0.48f) : Color.white;
        }

        private void RebuildPanel(DemoCombatModel combat)
        {
            for (int i = _panelContent.childCount - 1; i >= 0; i--)
                Destroy(_panelContent.GetChild(i).gameObject);

            AddImage(_panelContent, "Header", new Vector2(0f, 360f), new Vector2(1144f, 66f), new Color(0.035f, 0.075f, 0.095f, 1f));
            AddImage(_panelContent, "Header Accent", new Vector2(0f, 326f), new Vector2(1144f, 2f), new Color(0.14f, 0.52f, 0.64f, 0.95f));
            Text title = AddText(_panelContent, "Title", $"多佛海峡战斗  #{combat.Id}", new Vector2(0f, 362f), new Vector2(650f, 42f), 27, TextAnchor.MiddleCenter, Color.white);
            title.fontStyle = FontStyle.Bold;
            AddText(_panelContent, "Live", _controller.IsPaused ? "■  模拟已暂停" : "●  实时交战", new Vector2(-482f, 362f), new Vector2(190f, 34f), 15, TextAnchor.MiddleLeft,
                _controller.IsPaused ? WarningColor : SuccessColor);
            AddText(_panelContent, "Pause Hint", "SPACE 暂停 / ESC 关闭", new Vector2(405f, 362f), new Vector2(210f, 28f), 12, TextAnchor.MiddleRight, MutedText);
            AddButton(_panelContent, "Close", "×", new Vector2(548f, 362f), new Vector2(42f, 42f), ClosePanel, new Color(0.36f, 0.08f, 0.09f, 1f));

            float playerStrength = _controller.Simulation.GetCombatStrength(combat.Id, DemoTeam.Player);
            float enemyStrength = _controller.Simulation.GetCombatStrength(combat.Id, DemoTeam.Enemy);
            float total = Mathf.Max(1f, playerStrength + enemyStrength);
            float playerRatio = playerStrength / total;
            AddText(_panelContent, "Player Team", "我方作战群", new Vector2(-468f, 310f), new Vector2(220f, 28f), 16, TextAnchor.MiddleLeft, PlayerColor);
            AddText(_panelContent, "Enemy Team", "敌方作战群", new Vector2(468f, 310f), new Vector2(220f, 28f), 16, TextAnchor.MiddleRight, EnemyColor);
            AddBalanceBar(_panelContent, playerRatio, new Vector2(0f, 308f), new Vector2(700f, 16f));
            AddText(_panelContent, "Balance Label", $"战况  {playerRatio:P0}  /  {1f - playerRatio:P0}", new Vector2(0f, 285f), new Vector2(280f, 22f), 12, TextAnchor.MiddleCenter, MutedText);
            AddText(_panelContent, "PlayerScreen", $"屏卫效率  {_controller.Simulation.GetScreeningEfficiency(combat.Id, DemoTeam.Player):P0}", new Vector2(-430f, 274f), new Vector2(260f, 30f), 17, TextAnchor.MiddleLeft, PlayerColor);
            AddText(_panelContent, "EnemyScreen", $"屏卫效率  {_controller.Simulation.GetScreeningEfficiency(combat.Id, DemoTeam.Enemy):P0}", new Vector2(430f, 274f), new Vector2(260f, 30f), 17, TextAnchor.MiddleRight, EnemyColor);

            DrawBattleLine(combat, DemoBattleLine.Vanguard, 196f);
            DrawBattleLine(combat, DemoBattleLine.Main, 78f);
            DrawBattleLine(combat, DemoBattleLine.Support, -40f);
            DrawEvents(combat, -169f);
            DrawCommands(combat, -294f);
        }

        private void DrawBattleLine(DemoCombatModel combat, DemoBattleLine line, float y)
        {
            Image strip = AddImage(_panelContent, $"{line} Strip", new Vector2(0f, y), new Vector2(1100f, 108f), PanelRaised);
            strip.raycastTarget = false;
            AddImage(_panelContent, $"{line} Player Tint", new Vector2(-275f, y), new Vector2(538f, 100f), new Color(0.03f, 0.16f, 0.21f, 0.72f));
            AddImage(_panelContent, $"{line} Enemy Tint", new Vector2(275f, y), new Vector2(538f, 100f), new Color(0.2f, 0.045f, 0.055f, 0.72f));
            AddImage(_panelContent, $"{line} Divider", new Vector2(0f, y), new Vector2(2f, 100f), new Color(0.28f, 0.42f, 0.46f, 0.7f));
            AddImage(_panelContent, $"{line} Badge", new Vector2(0f, y + 15f), new Vector2(112f, 38f), new Color(0.07f, 0.13f, 0.16f, 1f));
            AddText(_panelContent, $"{line} Name", Demo1Simulation.BattleLineName(line), new Vector2(0f, y + 15f), new Vector2(108f, 34f), 17, TextAnchor.MiddleCenter, new Color(0.86f, 0.94f, 0.96f));

            List<DemoUnitModel> players = UnitsOnLine(combat, DemoTeam.Player, line);
            List<DemoUnitModel> enemies = UnitsOnLine(combat, DemoTeam.Enemy, line);
            AddText(_panelContent, $"{line} Counts", $"{players.Count}  :  {enemies.Count}\n{LineTrait(line)}", new Vector2(0f, y - 25f), new Vector2(112f, 38f), 10, TextAnchor.MiddleCenter, MutedText);
            DrawLineSide(combat, players, DemoTeam.Player, line, new Vector2(-340f, y));
            DrawLineSide(combat, enemies, DemoTeam.Enemy, line, new Vector2(340f, y));
        }

        private void DrawLineSide(DemoCombatModel combat, List<DemoUnitModel> units, DemoTeam team, DemoBattleLine line, Vector2 position)
        {
            GameObject viewportObject = new GameObject($"{team} {line} Viewport", typeof(RectTransform), typeof(Image), typeof(RectMask2D), typeof(ScrollRect));
            viewportObject.transform.SetParent(_panelContent, false);
            RectTransform viewport = viewportObject.GetComponent<RectTransform>();
            viewport.anchorMin = viewport.anchorMax = new Vector2(0.5f, 0.5f);
            viewport.sizeDelta = new Vector2(430f, 96f);
            viewport.anchoredPosition = position;
            Image viewportImage = viewportObject.GetComponent<Image>();
            viewportImage.color = new Color(0f, 0f, 0f, 0.001f);

            GameObject contentObject = new GameObject("Scrollable Units", typeof(RectTransform));
            contentObject.transform.SetParent(viewport, false);
            RectTransform content = contentObject.GetComponent<RectTransform>();
            content.anchorMin = content.anchorMax = new Vector2(0.5f, 0.5f);
            content.pivot = new Vector2(0.5f, 0.5f);
            float contentWidth = Mathf.Max(430f, units.Count * 212f - 8f);
            content.sizeDelta = new Vector2(contentWidth, 92f);
            content.anchoredPosition = Vector2.zero;

            ScrollRect scroll = viewportObject.GetComponent<ScrollRect>();
            scroll.viewport = viewport;
            scroll.content = content;
            scroll.horizontal = true;
            scroll.vertical = false;
            scroll.movementType = ScrollRect.MovementType.Clamped;
            scroll.inertia = false;
            string scrollKey = $"{team}:{line}";
            float scrollPosition;
            scroll.horizontalNormalizedPosition = _lineScrollPositions.TryGetValue(scrollKey, out scrollPosition) ? scrollPosition : 0f;
            scroll.onValueChanged.AddListener(value => _lineScrollPositions[scrollKey] = value.x);

            if (units.Count == 0)
            {
                DrawEmptySlot(content, Vector2.zero, team);
                return;
            }

            float firstCardX = -contentWidth * 0.5f + 102f;
            for (int i = 0; i < units.Count; i++)
                DrawUnitCard(content, combat, units[i], new Vector2(firstCardX + i * 212f, 0f));
        }

        private void DrawUnitCard(Transform parent, DemoCombatModel combat, DemoUnitModel unit, Vector2 position)
        {
            DemoCombatParticipantState state = combat.GetAssignment(unit.Id);
            bool selected = unit.Id == _selectedUnitId;
            Color color = unit.Team == DemoTeam.Player
                ? selected ? new Color(0.045f, 0.38f, 0.5f, 1f) : new Color(0.035f, 0.18f, 0.24f, 1f)
                : new Color(0.27f, 0.045f, 0.055f, 1f);
            Button button = AddButton(parent, $"Unit {unit.Id}", string.Empty, position, new Vector2(204f, 88f),
                unit.Team == DemoTeam.Player ? (UnityEngine.Events.UnityAction)(() => SelectUnit(unit.Id)) : null, color);
            Transform root = button.transform;
            Outline outline = button.gameObject.AddComponent<Outline>();
            outline.effectColor = unit.Team == DemoTeam.Player
                ? selected ? new Color(0.35f, 0.94f, 1f, 1f) : new Color(0.1f, 0.42f, 0.54f, 0.75f)
                : new Color(0.62f, 0.13f, 0.13f, 0.8f);
            outline.effectDistance = selected ? new Vector2(3f, -3f) : new Vector2(1f, -1f);
            AddImage(root, "Team Rail", new Vector2(-99f, 0f), new Vector2(4f, 82f), unit.Team == DemoTeam.Player ? PlayerColor : EnemyColor);
            string visionLabel = unit.Team == DemoTeam.Player && unit.Stats.WitchVisionType != DemoWitchVisionType.None
                ? $"·{VisionTypeName(unit.Stats.WitchVisionType)}" : string.Empty;
            Text name = AddText(root, "Name", $"{unit.DisplayName}  ·  {RoleName(unit.Role)}{visionLabel}", new Vector2(3f, 28f), new Vector2(184f, 22f), 14, TextAnchor.MiddleLeft, Color.white);
            name.fontStyle = FontStyle.Bold;
            string stateText = state.IsRepositioning ? $"换位 {state.RepositionRemaining:0.0}s" : unit.Activity == DemoUnitActivity.Retreating ? $"撤退 {unit.RetreatProgress:P0}" : "交战中";
            string roleStatus = RoleStatus(combat, unit, state);
            if (!string.IsNullOrEmpty(roleStatus))
                stateText += $" · {roleStatus}";
            if (state.LastTargetId >= 0)
            {
                DemoUnitModel target = _controller.Simulation.GetUnit(state.LastTargetId);
                if (target != null)
                    stateText += $" → {target.DisplayName}";
            }
            AddText(root, "State", stateText, new Vector2(3f, 8f), new Vector2(184f, 18f), 11, TextAnchor.MiddleLeft, new Color(0.74f, 0.82f, 0.84f));
            AddBar(root, "HP", unit.HealthRatio, new Vector2(0f, -12f), new Color(0.9f, 0.22f, 0.2f));
            AddBar(root, "MP", unit.MagicRatio, new Vector2(0f, -25f), new Color(0.25f, 0.52f, 1f));
            AddBar(root, "盾", unit.ShieldRatio, new Vector2(0f, -38f), new Color(0.12f, 0.76f, 0.88f));
        }

        private void DrawEmptySlot(Transform parent, Vector2 position, DemoTeam team)
        {
            Color color = team == DemoTeam.Player ? new Color(0.025f, 0.11f, 0.14f, 0.72f) : new Color(0.13f, 0.025f, 0.035f, 0.72f);
            AddImage(parent, "Empty Slot", position, new Vector2(204f, 88f), color);
            AddText(parent, "Empty", "本阵线暂无单位", position, new Vector2(190f, 30f), 12, TextAnchor.MiddleCenter, new Color(0.42f, 0.54f, 0.58f));
        }

        private void DrawEvents(DemoCombatModel combat, float y)
        {
            IEnumerable<DemoBattleEvent> events = _controller.BattleEvents.Where(item => item.CombatId == combat.Id).Take(4);
            string eventText = string.Join("\n", events.Select(item => $"{item.Time:000.0}  {item.Message}"));
            AddImage(_panelContent, "Events", new Vector2(0f, y), new Vector2(1100f, 96f), new Color(0.025f, 0.05f, 0.065f, 0.98f));
            AddImage(_panelContent, "Events Accent", new Vector2(-546f, y), new Vector2(4f, 96f), WarningColor);
            AddText(_panelContent, "Events Header", "最近战斗事件", new Vector2(-458f, y + 31f), new Vector2(160f, 20f), 12, TextAnchor.MiddleLeft, WarningColor);
            AddText(_panelContent, "Event Text", string.IsNullOrEmpty(eventText) ? "等待战斗事件……" : eventText,
                new Vector2(80f, y - 3f), new Vector2(880f, 80f), 13, TextAnchor.UpperLeft, new Color(0.78f, 0.84f, 0.86f));
        }

        private void DrawCommands(DemoCombatModel combat, float y)
        {
            bool hasSelectedUnit = IsSelectedUnitValid(combat);
            AddImage(_panelContent, "Command Divider", new Vector2(0f, y + 62f), new Vector2(1100f, 1f), new Color(0.18f, 0.38f, 0.45f, 0.7f));
            Button vanguard = AddButton(_panelContent, "Vanguard", "转移至前卫", new Vector2(-455f, y), new Vector2(145f, 42f), () => ChangeLine(DemoBattleLine.Vanguard), new Color(0.07f, 0.28f, 0.36f, 1f));
            Button main = AddButton(_panelContent, "Main", "转移至主战", new Vector2(-300f, y), new Vector2(145f, 42f), () => ChangeLine(DemoBattleLine.Main), new Color(0.07f, 0.28f, 0.36f, 1f));
            Button support = AddButton(_panelContent, "Support", "转移至支援", new Vector2(-145f, y), new Vector2(145f, 42f), () => ChangeLine(DemoBattleLine.Support), new Color(0.07f, 0.28f, 0.36f, 1f));
            AddButton(_panelContent, "Reinforce", "增援地图所选", new Vector2(75f, y), new Vector2(170f, 42f), () => ExecuteAndRefresh(_controller.CommandReinforceCombat(combat.Id)), new Color(0.045f, 0.34f, 0.24f, 1f));
            Button retreat = AddButton(_panelContent, "Retreat", "撤退选中单位", new Vector2(260f, y), new Vector2(170f, 42f), RetreatSelected, new Color(0.46f, 0.15f, 0.055f, 1f));
            AddButton(_panelContent, "Focus", "聚焦战斗", new Vector2(445f, y), new Vector2(150f, 42f), () => _controller.FocusCombat(combat.Id), new Color(0.11f, 0.18f, 0.22f, 1f));

            vanguard.interactable = hasSelectedUnit;
            main.interactable = hasSelectedUnit;
            support.interactable = hasSelectedUnit;
            retreat.interactable = hasSelectedUnit;
            AddText(_panelContent, "Command Feedback", _commandFeedback, new Vector2(0f, y + 40f), new Vector2(980f, 24f), 13,
                TextAnchor.MiddleCenter, _commandFeedbackSuccess ? SuccessColor : new Color(1f, 0.46f, 0.34f));
        }

        private void SelectUnit(int unitId)
        {
            DemoCombatModel combat = _controller.Simulation.GetCombat(_openCombatId);
            DemoUnitModel unit = _controller.Simulation.GetUnit(unitId);
            if (combat == null || unit == null || !unit.IsAlive || unit.Team != DemoTeam.Player || !combat.Participants.Contains(unitId))
                return;

            _selectedUnitId = unitId;
            _controller.SelectUnits(new[] { unitId });
            _commandFeedback = $"已选中 {unit.DisplayName}";
            _commandFeedbackSuccess = true;
            _panelDirty = true;
        }

        private void ChangeLine(DemoBattleLine line)
        {
            if (_selectedUnitId < 0)
                return;
            ExecuteAndRefresh(_controller.CommandBattleLineChange(_selectedUnitId, line));
        }

        private void RetreatSelected()
        {
            if (_selectedUnitId < 0)
                return;
            ExecuteAndRefresh(_controller.CommandRetreatUnit(_selectedUnitId));
        }

        private void ExecuteAndRefresh(DemoCommandResult result)
        {
            _commandFeedback = result.Message;
            _commandFeedbackSuccess = result.Success;
            _panelDirty = true;
        }

        private bool IsSelectedUnitValid(DemoCombatModel combat)
        {
            if (_selectedUnitId < 0 || combat == null || !combat.Participants.Contains(_selectedUnitId))
                return false;

            DemoUnitModel unit = _controller.Simulation.GetUnit(_selectedUnitId);
            return unit != null && unit.IsAlive && unit.Team == DemoTeam.Player;
        }

        private List<DemoUnitModel> UnitsOnLine(DemoCombatModel combat, DemoTeam team, DemoBattleLine line)
        {
            return combat.Participants.Select(_controller.Simulation.GetUnit)
                .Where(unit => unit != null && unit.IsAlive && unit.Team == team && combat.GetAssignment(unit.Id)?.Line == line)
                .OrderBy(unit => unit.Id).ToList();
        }

        private int CountTeam(DemoCombatModel combat, DemoTeam team)
        {
            return combat.Participants.Select(_controller.Simulation.GetUnit).Count(unit => unit != null && unit.IsAlive && unit.Team == team);
        }

        private static string RoleName(DemoUnitRole role)
        {
            switch (role)
            {
                case DemoUnitRole.Witch: return "魔女";
                case DemoUnitRole.Support: return "支援";
                case DemoUnitRole.Artillery: return "炮击";
                case DemoUnitRole.Scout: return "侦察";
                case DemoUnitRole.Guard: return "护卫";
                case DemoUnitRole.Fortress: return "固定目标";
                default: return role.ToString();
            }
        }

        private static string VisionTypeName(DemoWitchVisionType type)
        {
            return type == DemoWitchVisionType.Night ? "夜战" : "普通";
        }

        private string RoleStatus(DemoCombatModel combat, DemoUnitModel unit, DemoCombatParticipantState state)
        {
            float markRemaining = _controller.Simulation.GetMarkRemaining(combat.Id, unit.Id);
            if (markRemaining > 0f)
                return $"被标记 {markRemaining:0.0}s";
            switch (unit.Role)
            {
                case DemoUnitRole.Witch:
                    return "优先猎杀穿线单位";
                case DemoUnitRole.Artillery:
                    return $"校射 {state.AttacksPerformed % Mathf.Max(1, _controller.Simulation.Balance.ArtillerySalvoEveryAttacks)}/{Mathf.Max(1, _controller.Simulation.Balance.ArtillerySalvoEveryAttacks)}";
                case DemoUnitRole.Scout:
                    return "攻击施加侦察标记";
                case DemoUnitRole.Support:
                    return state.Line == DemoBattleLine.Support && !state.IsRepositioning
                        ? $"护盾脉冲 {Mathf.Max(0f, state.RoleAbilityRemaining):0.0}s"
                        : "支援能力未生效";
                case DemoUnitRole.Guard:
                    return state.Line == DemoBattleLine.Vanguard && !state.IsRepositioning ? "穿线拦截警戒" : "拦截能力未生效";
                case DemoUnitRole.Fortress:
                    return unit.HealthRatio <= _controller.Simulation.Balance.FortressBarrageHealthThreshold ? "应急齐射" : "半血后进入应急齐射";
                default:
                    return string.Empty;
            }
        }

        private string LineTrait(DemoBattleLine line)
        {
            Demo1Balance balance = _controller.Simulation.Balance;
            switch (line)
            {
                case DemoBattleLine.Vanguard:
                    return $"屏卫×{balance.VanguardScreenMultiplier:0.##} 防护{1f - balance.VanguardDamageTakenMultiplier:P0}";
                case DemoBattleLine.Main:
                    return $"火力+{balance.MainAttackMultiplier - 1f:P0}";
                case DemoBattleLine.Support:
                    return $"支援×{balance.SupportEffectMultiplier:0.##} 火力{balance.SupportAttackMultiplier - 1f:P0}";
                default:
                    return string.Empty;
            }
        }

        private void AddBalanceBar(Transform parent, float playerRatio, Vector2 position, Vector2 size)
        {
            AddImage(parent, "Balance Background", position, size, new Color(0.52f, 0.08f, 0.085f, 1f));
            Image player = AddImage(parent, "Player Balance", position, size, PlayerColor);
            RectTransform rect = player.rectTransform;
            rect.pivot = new Vector2(0f, 0.5f);
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = new Vector2(size.x * Mathf.Clamp01(playerRatio), size.y);
            rect.anchoredPosition = position - new Vector2(size.x * 0.5f, 0f);
            AddImage(parent, "Balance Center", position, new Vector2(2f, size.y + 8f), new Color(0.92f, 0.96f, 0.98f, 0.9f));
        }

        private void AddBar(Transform parent, string label, float ratio, Vector2 position, Color color)
        {
            AddText(parent, $"{label} Label", $"{label} {ratio:P0}", new Vector2(-68f, position.y), new Vector2(50f, 13f), 9, TextAnchor.MiddleLeft, new Color(0.88f, 0.93f, 0.95f));
            AddImage(parent, $"{label} Background", new Vector2(27f, position.y), new Vector2(136f, 8f), new Color(0f, 0f, 0f, 0.72f));
            Image fill = AddImage(parent, $"{label} Fill", new Vector2(27f, position.y), new Vector2(136f, 8f), color);
            fill.rectTransform.pivot = new Vector2(0f, 0.5f);
            fill.rectTransform.sizeDelta = new Vector2(136f * Mathf.Clamp01(ratio), 8f);
            fill.rectTransform.anchoredPosition = new Vector2(-41f, position.y);
        }

        private Button AddButton(Transform parent, string name, string label, Vector2 position, Vector2 size, UnityEngine.Events.UnityAction action, Color color)
        {
            GameObject obj = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
            obj.transform.SetParent(parent, false);
            RectTransform rect = obj.GetComponent<RectTransform>();
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = size;
            rect.anchoredPosition = position;
            Image image = obj.GetComponent<Image>();
            image.color = color;
            Button button = obj.GetComponent<Button>();
            button.targetGraphic = image;
            button.colors = CreateButtonColors(color);
            obj.AddComponent<Demo1BattlePointerGuard>().Initialize(this);
            if (action != null)
                button.onClick.AddListener(action);
            else
                button.interactable = false;
            if (!string.IsNullOrEmpty(label))
            {
                Text buttonLabel = AddText(obj.transform, "Label", label, Vector2.zero, size - new Vector2(8f, 4f), 14, TextAnchor.MiddleCenter, Color.white);
                buttonLabel.fontStyle = FontStyle.Bold;
            }
            return button;
        }

        private static ColorBlock CreateButtonColors(Color baseColor)
        {
            ColorBlock colors = ColorBlock.defaultColorBlock;
            colors.normalColor = baseColor;
            colors.highlightedColor = Color.Lerp(baseColor, Color.white, 0.16f);
            colors.pressedColor = Color.Lerp(baseColor, Color.black, 0.18f);
            colors.selectedColor = Color.Lerp(baseColor, Color.white, 0.1f);
            colors.disabledColor = new Color(baseColor.r * 0.45f, baseColor.g * 0.45f, baseColor.b * 0.45f, 0.5f);
            colors.fadeDuration = 0.08f;
            return colors;
        }

        private Text AddText(Transform parent, string name, string value, Vector2 position, Vector2 size, int fontSize, TextAnchor anchor, Color color)
        {
            GameObject obj = new GameObject(name, typeof(RectTransform), typeof(Text));
            obj.transform.SetParent(parent, false);
            RectTransform rect = obj.GetComponent<RectTransform>();
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = size;
            rect.anchoredPosition = position;
            Text text = obj.GetComponent<Text>();
            text.font = _font;
            text.fontSize = fontSize;
            text.alignment = anchor;
            text.color = color;
            text.text = value;
            text.raycastTarget = false;
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            text.verticalOverflow = VerticalWrapMode.Truncate;
            text.lineSpacing = 1.05f;
            return text;
        }

        private Image AddImage(Transform parent, string name, Vector2 position, Vector2 size, Color color)
        {
            GameObject obj = new GameObject(name, typeof(RectTransform), typeof(Image));
            obj.transform.SetParent(parent, false);
            RectTransform rect = obj.GetComponent<RectTransform>();
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = size;
            rect.anchoredPosition = position;
            Image image = obj.GetComponent<Image>();
            image.color = color;
            image.raycastTarget = false;
            return image;
        }

        private static RectTransform CreateRect(string name, Transform parent, Vector2 position, Vector2 size, Vector2 anchor)
        {
            GameObject obj = new GameObject(name, typeof(RectTransform));
            obj.transform.SetParent(parent, false);
            RectTransform rect = obj.GetComponent<RectTransform>();
            rect.anchorMin = rect.anchorMax = anchor;
            rect.sizeDelta = size;
            rect.anchoredPosition = position;
            return rect;
        }
    }
}
