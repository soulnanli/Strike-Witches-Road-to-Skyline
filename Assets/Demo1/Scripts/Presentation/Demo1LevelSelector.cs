using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace SWRTS.Demo1
{
    public sealed class Demo1LevelSelector : MonoBehaviour
    {
        private static readonly Color PanelColor = new Color(0.018f, 0.035f, 0.05f, 1f);
        private static readonly Color RaisedColor = new Color(0.045f, 0.09f, 0.115f, 1f);
        private static readonly Color HoverColor = new Color(0.07f, 0.22f, 0.28f, 1f);
        private static readonly Color AccentColor = new Color(0.16f, 0.72f, 0.94f, 1f);
        private static readonly Color ConfirmColor = new Color(0.04f, 0.32f, 0.23f, 1f);

        private readonly List<Button> _optionButtons = new List<Button>();
        private Demo1GameController _controller;
        private Font _font;
        private Canvas _canvas;
        private RectTransform _root;
        private Text _caption;
        private GameObject _optionsPanel;
        private Button _loadButton;
        private int _selectedLevelIndex;

        public bool IsExpanded => _optionsPanel != null && _optionsPanel.activeSelf;
        public int SelectedLevelIndex => _selectedLevelIndex;
        public int OptionCount => _optionButtons.Count;
        public string SelectedLevelName => _controller == null ? string.Empty : _controller.GetLevelName(_selectedLevelIndex);

        public void Initialize(Demo1GameController controller)
        {
            _controller = controller;
            _font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            _selectedLevelIndex = Mathf.Max(0, controller.ActiveLevelIndex);
            EnsureEventSystem();
            Build();
            RefreshCaption();
        }

        public void ToggleDropdown()
        {
            if (_optionsPanel != null)
                _optionsPanel.SetActive(!_optionsPanel.activeSelf);
        }

        public void SelectLevel(int index)
        {
            if (_controller == null || index < 0 || index >= _controller.LevelCount)
                return;

            _selectedLevelIndex = index;
            RefreshCaption();
            if (_optionsPanel != null)
                _optionsPanel.SetActive(false);
        }

        public void LoadSelectedLevel()
        {
            _controller?.RequestLevelLoad(_selectedLevelIndex);
        }

        private void LateUpdate()
        {
            if (_root == null || _canvas == null)
                return;
            float scale = Mathf.Max(0.1f, _canvas.scaleFactor);
            _root.anchoredPosition = new Vector2(0f, -60f / scale);
        }

        private void EnsureEventSystem()
        {
            if (EventSystem.current != null)
                return;
            GameObject eventObject = new GameObject("Demo1 Level Selector Event System", typeof(EventSystem), typeof(StandaloneInputModule));
            eventObject.transform.SetParent(transform, false);
        }

        private void Build()
        {
            GameObject canvasObject = new GameObject("Level Selector Canvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            canvasObject.transform.SetParent(transform, false);
            _canvas = canvasObject.GetComponent<Canvas>();
            _canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            _canvas.sortingOrder = 1200;
            CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;

            _root = CreateRect("Level Selector", canvasObject.transform);
            _root.anchorMin = _root.anchorMax = new Vector2(0.5f, 1f);
            _root.pivot = new Vector2(0.5f, 1f);
            _root.anchoredPosition = new Vector2(0f, -60f);
            _root.sizeDelta = new Vector2(430f, 48f);
            Image background = _root.gameObject.AddComponent<Image>();
            background.color = PanelColor;
            Outline outline = _root.gameObject.AddComponent<Outline>();
            outline.effectColor = new Color(0.12f, 0.45f, 0.56f, 0.95f);
            outline.effectDistance = new Vector2(2f, -2f);

            AddImage(_root, "Accent", new Vector2(-211f, 0f), new Vector2(4f, 42f), AccentColor);
            Text label = AddText(_root, "Level Label", "关卡", new Vector2(-174f, 0f), new Vector2(62f, 42f), 13, TextAnchor.MiddleCenter, AccentColor);
            label.fontStyle = FontStyle.Bold;

            Button dropdown = AddButton(_root, "Level Dropdown", string.Empty, new Vector2(-35f, 0f), new Vector2(220f, 36f), ToggleDropdown, RaisedColor);
            _caption = AddText(dropdown.transform, "Caption", string.Empty, new Vector2(-8f, 0f), new Vector2(178f, 32f), 13, TextAnchor.MiddleLeft, Color.white);
            AddText(dropdown.transform, "Arrow", "▼", new Vector2(91f, 0f), new Vector2(24f, 30f), 12, TextAnchor.MiddleCenter, AccentColor);

            _loadButton = AddButton(_root, "Load Level", "载入", new Vector2(154f, 0f), new Vector2(84f, 36f), LoadSelectedLevel, ConfirmColor);

            _optionsPanel = new GameObject("Level Dropdown Options", typeof(RectTransform), typeof(Image), typeof(Outline));
            _optionsPanel.transform.SetParent(_root, false);
            RectTransform optionsRect = _optionsPanel.GetComponent<RectTransform>();
            optionsRect.anchorMin = optionsRect.anchorMax = new Vector2(0.5f, 0.5f);
            optionsRect.pivot = new Vector2(0.5f, 1f);
            optionsRect.anchoredPosition = new Vector2(-35f, -22f);
            optionsRect.sizeDelta = new Vector2(220f, Mathf.Max(38f, _controller.LevelCount * 38f + 8f));
            _optionsPanel.GetComponent<Image>().color = PanelColor;
            Outline optionsOutline = _optionsPanel.GetComponent<Outline>();
            optionsOutline.effectColor = new Color(0.12f, 0.45f, 0.56f, 0.95f);
            optionsOutline.effectDistance = new Vector2(2f, -2f);

            for (int i = 0; i < _controller.LevelCount; i++)
            {
                int capturedIndex = i;
                Button option = AddButton(optionsRect, $"Level Option {i}", _controller.GetLevelName(i), Vector2.zero, new Vector2(210f, 34f),
                    () => SelectLevel(capturedIndex), i == _controller.ActiveLevelIndex ? new Color(0.055f, 0.25f, 0.31f, 1f) : RaisedColor);
                RectTransform optionRect = option.GetComponent<RectTransform>();
                optionRect.anchorMin = optionRect.anchorMax = new Vector2(0.5f, 1f);
                optionRect.pivot = new Vector2(0.5f, 1f);
                optionRect.anchoredPosition = new Vector2(0f, -4f - i * 38f);
                _optionButtons.Add(option);
            }
            _optionsPanel.SetActive(false);
        }

        private void RefreshCaption()
        {
            if (_caption == null || _controller == null)
                return;
            bool isActive = _selectedLevelIndex == _controller.ActiveLevelIndex;
            _caption.text = _controller.GetLevelName(_selectedLevelIndex) + (isActive ? "  · 当前" : "");
            if (_loadButton != null)
                _loadButton.GetComponentInChildren<Text>().text = isActive ? "重置" : "载入";
        }

        private RectTransform CreateRect(string name, Transform parent)
        {
            GameObject gameObject = new GameObject(name, typeof(RectTransform));
            gameObject.transform.SetParent(parent, false);
            RectTransform rect = gameObject.GetComponent<RectTransform>();
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            return rect;
        }

        private Image AddImage(Transform parent, string name, Vector2 position, Vector2 size, Color color)
        {
            RectTransform rect = CreateRect(name, parent);
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
            Image image = rect.gameObject.AddComponent<Image>();
            image.color = color;
            image.raycastTarget = false;
            return image;
        }

        private Text AddText(Transform parent, string name, string value, Vector2 position, Vector2 size, int fontSize, TextAnchor alignment, Color color)
        {
            RectTransform rect = CreateRect(name, parent);
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
            Text text = rect.gameObject.AddComponent<Text>();
            text.font = _font;
            text.text = value;
            text.fontSize = fontSize;
            text.alignment = alignment;
            text.color = color;
            text.raycastTarget = false;
            return text;
        }

        private Button AddButton(Transform parent, string name, string label, Vector2 position, Vector2 size, UnityEngine.Events.UnityAction action, Color color)
        {
            RectTransform rect = CreateRect(name, parent);
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
            Image image = rect.gameObject.AddComponent<Image>();
            image.color = color;
            Button button = rect.gameObject.AddComponent<Button>();
            button.targetGraphic = image;
            ColorBlock colors = button.colors;
            colors.normalColor = color;
            colors.highlightedColor = HoverColor;
            colors.pressedColor = new Color(color.r * 0.7f, color.g * 0.7f, color.b * 0.7f, 1f);
            colors.selectedColor = colors.highlightedColor;
            colors.fadeDuration = 0.08f;
            button.colors = colors;
            if (action != null)
                button.onClick.AddListener(action);
            if (!string.IsNullOrEmpty(label))
            {
                Text text = AddText(rect, "Text", label, Vector2.zero, size - new Vector2(10f, 4f), 13, TextAnchor.MiddleCenter, Color.white);
                text.fontStyle = FontStyle.Bold;
            }
            return button;
        }
    }
}
