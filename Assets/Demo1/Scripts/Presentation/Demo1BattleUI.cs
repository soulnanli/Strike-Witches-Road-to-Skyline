using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace SWRTS.Demo1
{
    public sealed class Demo1BattleUI : MonoBehaviour
    {
        private sealed class BubbleView
        {
            public RectTransform Rect;
            public Image Image;
            public Text Text;
        }

        private readonly Dictionary<int, BubbleView> _bubbles = new Dictionary<int, BubbleView>();
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

        public bool IsPanelOpen => _panel != null && _panel.activeSelf;
        public int OpenCombatId => _openCombatId;

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

            SyncBubbles();
            if (!IsPanelOpen)
                return;

            DemoCombatModel combat = _controller.Simulation.GetCombat(_openCombatId);
            if (combat == null || combat.IsFinished)
            {
                ClosePanel();
                return;
            }

            if (Time.unscaledTime >= _nextPanelRefresh)
            {
                _nextPanelRefresh = Time.unscaledTime + 0.1f;
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
            _panel.SetActive(true);
            RebuildPanel(combat);
        }

        public void ClosePanel()
        {
            _openCombatId = -1;
            _selectedUnitId = -1;
            if (_panel != null)
                _panel.SetActive(false);
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
            _canvas.sortingOrder = 50;
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
            rect.sizeDelta = new Vector2(1160f, 760f);
            rect.anchoredPosition = new Vector2(100f, 0f);
            Image background = _panel.GetComponent<Image>();
            background.color = new Color(0.035f, 0.055f, 0.075f, 0.97f);
            background.raycastTarget = true;

            Outline outline = _panel.GetComponent<Outline>();
            outline.effectColor = new Color(0.25f, 0.48f, 0.58f, 0.9f);
            outline.effectDistance = new Vector2(2f, -2f);

            _panelContent = CreateRect("Content", rect, Vector2.zero, Vector2.zero, Vector2.zero);
            _panelContent.anchorMin = Vector2.zero;
            _panelContent.anchorMax = Vector2.one;
            _panelContent.offsetMin = Vector2.zero;
            _panelContent.offsetMax = Vector2.zero;
            _panel.SetActive(false);
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
            rect.sizeDelta = new Vector2(150f, 58f);
            Image image = buttonObject.GetComponent<Image>();
            image.color = new Color(0.1f, 0.18f, 0.23f, 0.96f);
            Outline outline = buttonObject.GetComponent<Outline>();
            outline.effectColor = new Color(0.25f, 0.9f, 1f, 0.9f);
            outline.effectDistance = new Vector2(2f, -2f);
            Button button = buttonObject.GetComponent<Button>();
            button.targetGraphic = image;
            button.onClick.AddListener(() => OpenPanel(combatId));
            Text text = AddText(buttonObject.transform, "Label", string.Empty, Vector2.zero, new Vector2(144f, 54f), 18, TextAnchor.MiddleCenter, Color.white);
            return new BubbleView { Rect = rect, Image = image, Text = text };
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
            float halfWidth = _canvasRect.rect.width * 0.5f - 90f;
            float halfHeight = _canvasRect.rect.height * 0.5f - 45f;
            bubble.Rect.anchoredPosition = new Vector2(Mathf.Clamp(local.x, -halfWidth, halfWidth), Mathf.Clamp(local.y + 55f, -halfHeight, halfHeight));

            int playerCount = CountTeam(combat, DemoTeam.Player);
            int enemyCount = CountTeam(combat, DemoTeam.Enemy);
            float screenEfficiency = _controller.Simulation.GetScreeningEfficiency(combat.Id, DemoTeam.Player);
            bubble.Text.text = $"战斗 #{combat.Id}  {playerCount} : {enemyCount}\n屏卫 {screenEfficiency:P0}";
            float playerStrength = _controller.Simulation.GetCombatStrength(combat.Id, DemoTeam.Player);
            float enemyStrength = _controller.Simulation.GetCombatStrength(combat.Id, DemoTeam.Enemy);
            bubble.Image.color = playerStrength >= enemyStrength
                ? new Color(0.08f, 0.28f, 0.38f, 0.97f)
                : new Color(0.38f, 0.11f, 0.12f, 0.97f);
        }

        private void RebuildPanel(DemoCombatModel combat)
        {
            for (int i = _panelContent.childCount - 1; i >= 0; i--)
                Destroy(_panelContent.GetChild(i).gameObject);

            AddText(_panelContent, "Title", $"多佛海峡战斗 #{combat.Id}", new Vector2(0f, 344f), new Vector2(700f, 42f), 27, TextAnchor.MiddleCenter, Color.white);
            AddText(_panelContent, "Live", _controller.IsPaused ? "已暂停" : "● 实时进行", new Vector2(-490f, 344f), new Vector2(150f, 34f), 16, TextAnchor.MiddleLeft,
                _controller.IsPaused ? new Color(1f, 0.8f, 0.25f) : new Color(0.3f, 1f, 0.62f));
            AddButton(_panelContent, "Close", "×", new Vector2(545f, 345f), new Vector2(48f, 40f), ClosePanel, new Color(0.32f, 0.1f, 0.12f, 0.95f));

            float playerStrength = _controller.Simulation.GetCombatStrength(combat.Id, DemoTeam.Player);
            float enemyStrength = _controller.Simulation.GetCombatStrength(combat.Id, DemoTeam.Enemy);
            float total = Mathf.Max(1f, playerStrength + enemyStrength);
            AddBalanceBar(_panelContent, playerStrength / total, new Vector2(0f, 303f), new Vector2(760f, 22f));
            AddText(_panelContent, "PlayerScreen", $"我方屏卫 {_controller.Simulation.GetScreeningEfficiency(combat.Id, DemoTeam.Player):P0}", new Vector2(-390f, 270f), new Vector2(250f, 30f), 18, TextAnchor.MiddleLeft, new Color(0.35f, 0.82f, 1f));
            AddText(_panelContent, "EnemyScreen", $"敌方屏卫 {_controller.Simulation.GetScreeningEfficiency(combat.Id, DemoTeam.Enemy):P0}", new Vector2(390f, 270f), new Vector2(250f, 30f), 18, TextAnchor.MiddleRight, new Color(1f, 0.42f, 0.38f));

            DrawBattleLine(combat, DemoBattleLine.Vanguard, 190f);
            DrawBattleLine(combat, DemoBattleLine.Main, 72f);
            DrawBattleLine(combat, DemoBattleLine.Support, -46f);
            DrawReserve(combat, -147f);
            DrawEvents(combat, -245f);
            DrawCommands(combat, -342f);
        }

        private void DrawBattleLine(DemoCombatModel combat, DemoBattleLine line, float y)
        {
            Image strip = AddImage(_panelContent, $"{line} Strip", new Vector2(0f, y), new Vector2(1080f, 104f), new Color(0.08f, 0.12f, 0.15f, 0.92f));
            strip.raycastTarget = false;
            AddText(_panelContent, $"{line} Name", Demo1Simulation.BattleLineName(line), new Vector2(0f, y), new Vector2(120f, 38f), 19, TextAnchor.MiddleCenter, new Color(0.78f, 0.85f, 0.87f));

            List<DemoUnitModel> players = UnitsOnLine(combat, DemoTeam.Player, line);
            List<DemoUnitModel> enemies = UnitsOnLine(combat, DemoTeam.Enemy, line);
            for (int i = 0; i < 2; i++)
            {
                float leftX = -450f + i * 220f;
                float rightX = 230f + i * 220f;
                if (i < players.Count)
                    DrawUnitCard(combat, players[i], new Vector2(leftX, y));
                else
                    DrawEmptySlot(new Vector2(leftX, y), DemoTeam.Player);
                if (i < enemies.Count)
                    DrawUnitCard(combat, enemies[i], new Vector2(rightX, y));
                else
                    DrawEmptySlot(new Vector2(rightX, y), DemoTeam.Enemy);
            }
        }

        private void DrawUnitCard(DemoCombatModel combat, DemoUnitModel unit, Vector2 position)
        {
            DemoCombatParticipantState state = combat.GetAssignment(unit.Id);
            bool selected = unit.Id == _selectedUnitId;
            Color color = unit.Team == DemoTeam.Player
                ? selected ? new Color(0.08f, 0.46f, 0.58f, 1f) : new Color(0.07f, 0.24f, 0.32f, 1f)
                : new Color(0.34f, 0.09f, 0.11f, 1f);
            Button button = AddButton(_panelContent, $"Unit {unit.Id}", string.Empty, position, new Vector2(204f, 88f),
                unit.Team == DemoTeam.Player ? (UnityEngine.Events.UnityAction)(() => SelectUnit(unit.Id)) : null, color);
            Transform root = button.transform;
            AddText(root, "Name", $"{unit.DisplayName}  ·  {RoleName(unit.Role)}", new Vector2(0f, 28f), new Vector2(190f, 22f), 14, TextAnchor.MiddleLeft, Color.white);
            string stateText = state.IsRepositioning ? $"换位 {state.RepositionRemaining:0.0}s" : unit.Activity == DemoUnitActivity.Retreating ? $"撤退 {unit.RetreatProgress:P0}" : "交战中";
            if (state.LastTargetId >= 0)
            {
                DemoUnitModel target = _controller.Simulation.GetUnit(state.LastTargetId);
                if (target != null)
                    stateText += $" → {target.DisplayName}";
            }
            AddText(root, "State", stateText, new Vector2(0f, 6f), new Vector2(190f, 20f), 12, TextAnchor.MiddleLeft, new Color(0.78f, 0.84f, 0.86f));
            AddBar(root, "HP", unit.HealthRatio, new Vector2(0f, -17f), new Color(0.9f, 0.2f, 0.18f));
            AddBar(root, "MP", unit.MagicRatio, new Vector2(0f, -32f), new Color(0.25f, 0.52f, 1f));
            AddBar(root, "盾", unit.ShieldRatio, new Vector2(0f, -47f), new Color(0.18f, 0.9f, 0.95f));
        }

        private void DrawEmptySlot(Vector2 position, DemoTeam team)
        {
            Color color = team == DemoTeam.Player ? new Color(0.05f, 0.14f, 0.18f, 0.55f) : new Color(0.18f, 0.06f, 0.07f, 0.55f);
            AddImage(_panelContent, "Empty Slot", position, new Vector2(204f, 88f), color);
            AddText(_panelContent, "Empty", "— 空位 —", position, new Vector2(190f, 30f), 13, TextAnchor.MiddleCenter, new Color(0.45f, 0.52f, 0.55f));
        }

        private void DrawReserve(DemoCombatModel combat, float y)
        {
            AddImage(_panelContent, "Reserve Strip", new Vector2(0f, y), new Vector2(1080f, 66f), new Color(0.06f, 0.085f, 0.105f, 0.92f));
            AddText(_panelContent, "Reserve Name", "预备队", new Vector2(0f, y), new Vector2(100f, 30f), 17, TextAnchor.MiddleCenter, new Color(0.72f, 0.78f, 0.8f));
            AddText(_panelContent, "Player Reserve", ReserveNames(combat, DemoTeam.Player), new Vector2(-330f, y), new Vector2(390f, 45f), 14, TextAnchor.MiddleLeft, new Color(0.35f, 0.78f, 1f));
            AddText(_panelContent, "Enemy Reserve", ReserveNames(combat, DemoTeam.Enemy), new Vector2(330f, y), new Vector2(390f, 45f), 14, TextAnchor.MiddleRight, new Color(1f, 0.45f, 0.42f));
        }

        private void DrawEvents(DemoCombatModel combat, float y)
        {
            IEnumerable<DemoBattleEvent> events = _controller.BattleEvents.Where(item => item.CombatId == combat.Id).Take(4);
            string eventText = string.Join("\n", events.Select(item => $"{item.Time:000.0}  {item.Message}"));
            AddImage(_panelContent, "Events", new Vector2(0f, y), new Vector2(1080f, 104f), new Color(0.025f, 0.04f, 0.055f, 0.9f));
            AddText(_panelContent, "Event Text", string.IsNullOrEmpty(eventText) ? "等待战斗事件……" : eventText,
                new Vector2(0f, y), new Vector2(1040f, 92f), 14, TextAnchor.UpperLeft, new Color(0.8f, 0.84f, 0.86f));
        }

        private void DrawCommands(DemoCombatModel combat, float y)
        {
            AddButton(_panelContent, "Vanguard", "转移至前卫", new Vector2(-455f, y), new Vector2(145f, 42f), () => ChangeLine(DemoBattleLine.Vanguard), new Color(0.12f, 0.32f, 0.38f, 1f));
            AddButton(_panelContent, "Main", "转移至主战", new Vector2(-300f, y), new Vector2(145f, 42f), () => ChangeLine(DemoBattleLine.Main), new Color(0.12f, 0.32f, 0.38f, 1f));
            AddButton(_panelContent, "Support", "转移至支援", new Vector2(-145f, y), new Vector2(145f, 42f), () => ChangeLine(DemoBattleLine.Support), new Color(0.12f, 0.32f, 0.38f, 1f));
            AddButton(_panelContent, "Reinforce", "增援地图所选", new Vector2(75f, y), new Vector2(170f, 42f), () => ExecuteAndRefresh(_controller.CommandReinforceCombat(combat.Id)), new Color(0.08f, 0.38f, 0.28f, 1f));
            AddButton(_panelContent, "Retreat", "撤退选中单位", new Vector2(260f, y), new Vector2(170f, 42f), RetreatSelected, new Color(0.5f, 0.22f, 0.08f, 1f));
            AddButton(_panelContent, "Focus", "聚焦战斗", new Vector2(445f, y), new Vector2(150f, 42f), () => _controller.FocusCombat(combat.Id), new Color(0.18f, 0.25f, 0.3f, 1f));
        }

        private void SelectUnit(int unitId)
        {
            _selectedUnitId = unitId;
            DemoCombatModel combat = _controller.Simulation.GetCombat(_openCombatId);
            if (combat != null)
                RebuildPanel(combat);
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
            DemoCombatModel combat = _controller.Simulation.GetCombat(_openCombatId);
            if (combat != null && !combat.IsFinished)
                RebuildPanel(combat);
        }

        private List<DemoUnitModel> UnitsOnLine(DemoCombatModel combat, DemoTeam team, DemoBattleLine line)
        {
            return combat.Participants.Select(_controller.Simulation.GetUnit)
                .Where(unit => unit != null && unit.IsAlive && unit.Team == team && combat.GetAssignment(unit.Id)?.Line == line)
                .OrderBy(unit => unit.Id).ToList();
        }

        private string ReserveNames(DemoCombatModel combat, DemoTeam team)
        {
            List<DemoUnitModel> units = UnitsOnLine(combat, team, DemoBattleLine.Reserve);
            return units.Count == 0 ? "无" : string.Join("、", units.Select(unit => unit.DisplayName));
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

        private void AddBalanceBar(Transform parent, float playerRatio, Vector2 position, Vector2 size)
        {
            AddImage(parent, "Balance Background", position, size, new Color(0.2f, 0.06f, 0.07f, 1f));
            Image player = AddImage(parent, "Player Balance", position, size, new Color(0.08f, 0.55f, 0.8f, 1f));
            RectTransform rect = player.rectTransform;
            rect.pivot = new Vector2(0f, 0.5f);
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = new Vector2(size.x * Mathf.Clamp01(playerRatio), size.y);
            rect.anchoredPosition = position - new Vector2(size.x * 0.5f, 0f);
        }

        private void AddBar(Transform parent, string label, float ratio, Vector2 position, Color color)
        {
            AddText(parent, $"{label} Label", label, new Vector2(-79f, position.y), new Vector2(26f, 13f), 10, TextAnchor.MiddleLeft, Color.white);
            AddImage(parent, $"{label} Background", new Vector2(16f, position.y), new Vector2(158f, 9f), new Color(0f, 0f, 0f, 0.65f));
            Image fill = AddImage(parent, $"{label} Fill", new Vector2(16f, position.y), new Vector2(158f, 9f), color);
            fill.rectTransform.pivot = new Vector2(0f, 0.5f);
            fill.rectTransform.sizeDelta = new Vector2(158f * Mathf.Clamp01(ratio), 9f);
            fill.rectTransform.anchoredPosition = new Vector2(-63f, position.y);
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
            if (action != null)
                button.onClick.AddListener(action);
            else
                button.interactable = false;
            if (!string.IsNullOrEmpty(label))
                AddText(obj.transform, "Label", label, Vector2.zero, size - new Vector2(8f, 4f), 15, TextAnchor.MiddleCenter, Color.white);
            return button;
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
