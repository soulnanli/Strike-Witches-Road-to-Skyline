using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace SWRTS.Demo1
{
    [DefaultExecutionOrder(-100)]
    public sealed class Demo1GameController : MonoBehaviour
    {
        private enum CommandMode
        {
            Select,
            Engage,
            RemoteStrike
        }

        private sealed class CombatVisual
        {
            public GameObject Root;
            public LineRenderer Reinforcement;
            public LineRenderer Forced;
        }

        private sealed class StrikeVisual
        {
            public GameObject Root;
            public LineRenderer Radius;
        }

        private readonly Dictionary<int, Demo1UnitView> _unitViews = new Dictionary<int, Demo1UnitView>();
        private readonly Dictionary<int, CombatVisual> _combatViews = new Dictionary<int, CombatVisual>();
        private readonly Dictionary<int, StrikeVisual> _strikeViews = new Dictionary<int, StrikeVisual>();
        private readonly HashSet<int> _selection = new HashSet<int>();
        private readonly Dictionary<int, List<int>> _controlGroups = new Dictionary<int, List<int>>();
        private readonly List<DemoBattleEvent> _events = new List<DemoBattleEvent>();

        private Demo1Simulation _simulation;
        private Demo1Balance _balance;
        private Camera _camera;
        private Demo1CameraController _cameraController;
        private CommandMode _commandMode;
        private Vector2 _dragStart;
        private bool _dragSelecting;
        private bool _paused;
        private string _statusMessage = "左键选择，右键移动；先靠近已发现目标再交战。";
        private GUIStyle _titleStyle;
        private GUIStyle _smallStyle;
        private GUIStyle _centerStyle;
        private GUIStyle _worldLabelStyle;
        private Texture2D _selectionTexture;
        private const float PanelWidth = 340f;

        public Demo1Simulation Simulation => _simulation;

        private void Start()
        {
            _balance = new Demo1Balance();
            _simulation = new Demo1Simulation(_balance);
            _simulation.EventRaised += OnBattleEvent;
            BuildEnvironment();
            BuildCamera();
            CreateScenario();
            SyncViews();
        }

        private void Update()
        {
            if (_simulation == null)
                return;

            HandleGlobalInput();
            if (!_paused)
                _simulation.Advance(Time.deltaTime);
            HandlePointerInput();
            SyncViews();
        }

        private void OnDisable()
        {
            Time.timeScale = 1f;
        }

        private void BuildEnvironment()
        {
            RenderSettings.ambientLight = new Color(0.48f, 0.52f, 0.56f);

            GameObject ground = GameObject.CreatePrimitive(PrimitiveType.Plane);
            ground.name = "Operations Map";
            ground.transform.SetParent(transform);
            ground.transform.position = Vector3.zero;
            ground.transform.localScale = new Vector3(_balance.MapHalfWidth / 5f, 1f, _balance.MapHalfHeight / 5f);
            ground.GetComponent<Renderer>().sharedMaterial = Demo1Drawing.CreateMaterial(new Color(0.075f, 0.12f, 0.14f));

            GameObject gridRoot = new GameObject("Map Grid");
            gridRoot.transform.SetParent(transform);
            for (int x = -(int)_balance.MapHalfWidth; x <= _balance.MapHalfWidth; x += 10)
                CreateGridLine(gridRoot.transform, new Vector3(x, 0.025f, -_balance.MapHalfHeight), new Vector3(x, 0.025f, _balance.MapHalfHeight));
            for (int z = -(int)_balance.MapHalfHeight; z <= _balance.MapHalfHeight; z += 10)
                CreateGridLine(gridRoot.transform, new Vector3(-_balance.MapHalfWidth, 0.025f, z), new Vector3(_balance.MapHalfWidth, 0.025f, z));

            CreateLandmark("Dover", new Vector3(-33f, 0.3f, -20f), new Vector3(6f, 0.6f, 6f), new Color(0.18f, 0.26f, 0.28f));
            CreateLandmark("Calais", new Vector3(31f, 0.3f, 20f), new Vector3(7f, 0.6f, 5f), new Color(0.2f, 0.22f, 0.25f));

            GameObject lightObject = new GameObject("Directional Light");
            lightObject.transform.SetParent(transform);
            Light light = lightObject.AddComponent<Light>();
            light.type = LightType.Directional;
            light.intensity = 1.25f;
            lightObject.transform.rotation = Quaternion.Euler(50f, -35f, 0f);
        }

        private void BuildCamera()
        {
            GameObject cameraObject = new GameObject("Demo1 Camera");
            cameraObject.tag = "MainCamera";
            cameraObject.transform.SetParent(transform);
            cameraObject.transform.position = new Vector3(-5f, 50f, 0f);
            cameraObject.transform.rotation = Quaternion.Euler(90f, 0f, 0f);
            _camera = cameraObject.AddComponent<Camera>();
            _camera.orthographic = true;
            _camera.orthographicSize = 24f;
            _camera.nearClipPlane = 0.1f;
            _camera.farClipPlane = 100f;
            _camera.backgroundColor = new Color(0.025f, 0.04f, 0.055f);
            _camera.clearFlags = CameraClearFlags.SolidColor;
            cameraObject.AddComponent<AudioListener>();
            _cameraController = cameraObject.AddComponent<Demo1CameraController>();
            _cameraController.MapHalfExtents = new Vector2(_balance.MapHalfWidth, _balance.MapHalfHeight);
        }

        private void CreateScenario()
        {
            DemoUnitStats witch = new DemoUnitStats();
            DemoUnitStats ace = witch.Clone();
            ace.MaxHealth = 145f;
            ace.Attack = 29f;
            ace.CoreDiscovery = 0.28f;
            ace.Mobility = 1.25f;

            DemoUnitStats support = witch.Clone();
            support.Attack = 18f;
            support.MaxMagic = 120f;
            support.MaxShield = 60f;
            support.GlobalShieldBonus = 0.28f;
            support.MagicRecovery = 6f;

            DemoUnitStats artillery = witch.Clone();
            artillery.Attack = 22f;
            artillery.CanRemoteStrike = true;
            artillery.CoreDiscovery = 0.22f;

            DemoUnitStats neuroi = witch.Clone();
            neuroi.MaxHealth = 105f;
            neuroi.Attack = 20f;
            neuroi.MaxMagic = 55f;
            neuroi.MaxShield = 34f;
            neuroi.MoveSpeed = 4.8f;
            neuroi.EngagementRadius = 7.5f;

            DemoUnitStats fortress = neuroi.Clone();
            fortress.MaxHealth = 360f;
            fortress.Attack = 31f;
            fortress.Defense = 11f;
            fortress.MaxMagic = 180f;
            fortress.MaxShield = 120f;
            fortress.CoreConcealment = 0.9f;
            fortress.AttackInterval = 2.2f;
            fortress.EngagementRadius = 10f;
            fortress.VisionRadius = 26f;
            fortress.MoveSpeed = 0f;

            AddUnit("宫藤芳佳", DemoTeam.Player, DemoUnitRole.Support, support, new Vector3(-27f, 0f, -10f));
            AddUnit("坂本美绪", DemoTeam.Player, DemoUnitRole.Witch, ace, new Vector3(-25f, 0f, -3f));
            AddUnit("莉涅特", DemoTeam.Player, DemoUnitRole.Artillery, artillery, new Vector3(-27f, 0f, 5f));
            AddUnit("佩琳", DemoTeam.Player, DemoUnitRole.Witch, witch, new Vector3(-23f, 0f, 12f));

            DemoUnitModel scout = AddUnit("异形军侦察体", DemoTeam.Enemy, DemoUnitRole.Witch, neuroi, new Vector3(2f, 0f, -9f));
            AddUnit("异形军护卫 A", DemoTeam.Enemy, DemoUnitRole.Witch, neuroi, new Vector3(17f, 0f, -3f));
            AddUnit("异形军护卫 B", DemoTeam.Enemy, DemoUnitRole.Witch, neuroi, new Vector3(18f, 0f, 8f));
            DemoUnitModel objective = AddUnit("异形军巢穴", DemoTeam.Enemy, DemoUnitRole.Fortress, fortress, new Vector3(34f, 0f, 3f));
            objective.IsRevealedToPlayer = true;

            _simulation.IssueMove(new[] { scout.Id }, new Vector3(-10f, 0f, -5f));
            _events.Clear();
            _statusMessage = "任务：摧毁地图东侧的异形军巢穴。紫色单位可执行远程打击。";
        }

        private DemoUnitModel AddUnit(string name, DemoTeam team, DemoUnitRole role, DemoUnitStats stats, Vector3 position)
        {
            DemoUnitModel model = _simulation.AddUnit(name, team, role, stats, position);
            PrimitiveType primitive = role == DemoUnitRole.Fortress ? PrimitiveType.Cube : PrimitiveType.Capsule;
            GameObject viewObject = GameObject.CreatePrimitive(primitive);
            viewObject.name = $"Unit {model.Id} - {name}";
            viewObject.transform.SetParent(transform);
            viewObject.transform.localScale = role == DemoUnitRole.Fortress
                ? new Vector3(4.2f, 2.2f, 4.2f)
                : new Vector3(1.3f, 1.1f, 1.3f);
            Demo1UnitView view = viewObject.AddComponent<Demo1UnitView>();
            view.Initialize(model);
            _unitViews.Add(model.Id, view);
            return model;
        }

        private void HandleGlobalInput()
        {
            if (Input.GetKeyDown(KeyCode.Space))
                TogglePause();
            if (Input.GetKeyDown(KeyCode.Escape))
            {
                _commandMode = CommandMode.Select;
                _statusMessage = "命令已取消";
            }
            if (Input.GetKeyDown(KeyCode.A))
            {
                _commandMode = CommandMode.Engage;
                _statusMessage = "交战模式：左键点击已发现的敌方目标";
            }
            if (Input.GetKeyDown(KeyCode.B))
            {
                _commandMode = CommandMode.RemoteStrike;
                _statusMessage = "远程打击模式：左键指定目标区域";
            }
            if (Input.GetKeyDown(KeyCode.R))
                ApplyResult(_simulation.RequestRetreat(_selection));
            if (Input.GetKeyDown(KeyCode.G))
                ReinforceNearestBattle();
            if (Input.GetKeyDown(KeyCode.F))
                FocusSelection();
            if (Input.GetKeyDown(KeyCode.Return) && _simulation.Outcome != DemoOutcome.Running)
                RestartScene();

            for (int i = 1; i <= 9; i++)
            {
                KeyCode key = (KeyCode)((int)KeyCode.Alpha0 + i);
                if (!Input.GetKeyDown(key))
                    continue;
                if (Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl))
                {
                    _controlGroups[i] = _selection.ToList();
                    _statusMessage = $"控制组 {i} 已保存（{_selection.Count} 个单位）";
                }
                else if (_controlGroups.TryGetValue(i, out List<int> group))
                {
                    _selection.Clear();
                    foreach (int id in group.Where(id => _simulation.GetUnit(id)?.IsAlive == true))
                        _selection.Add(id);
                    FocusSelection();
                }
            }
        }

        private void HandlePointerInput()
        {
            if (IsPointerOverHud())
                return;

            if (Input.GetMouseButtonDown(0))
            {
                _dragStart = Input.mousePosition;
                _dragSelecting = true;
            }

            if (Input.GetMouseButtonUp(0) && _dragSelecting)
            {
                Vector2 end = Input.mousePosition;
                _dragSelecting = false;
                if ((end - _dragStart).sqrMagnitude > 64f && _commandMode == CommandMode.Select)
                    SelectInRectangle(_dragStart, end, Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift));
                else
                    HandlePrimaryClick();
            }

            if (Input.GetMouseButtonDown(1))
            {
                _commandMode = CommandMode.Select;
                Demo1UnitView targetView = RaycastUnit(Input.mousePosition);
                if (targetView != null && _simulation.GetUnit(targetView.UnitId)?.Team == DemoTeam.Enemy)
                    EngageTarget(targetView.UnitId);
                else if (TryGroundPoint(Input.mousePosition, out Vector3 point))
                    ApplyResult(_simulation.IssueMove(_selection, point));
            }
        }

        private void HandlePrimaryClick()
        {
            if (_commandMode == CommandMode.RemoteStrike)
            {
                if (TryGroundPoint(Input.mousePosition, out Vector3 target))
                    RemoteStrike(target);
                return;
            }

            Demo1UnitView view = RaycastUnit(Input.mousePosition);
            if (_commandMode == CommandMode.Engage)
            {
                if (view == null || _simulation.GetUnit(view.UnitId)?.Team != DemoTeam.Enemy)
                    _statusMessage = "交战命令需要一个已发现的敌方目标";
                else
                    EngageTarget(view.UnitId);
                return;
            }

            bool additive = Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift);
            if (!additive)
                _selection.Clear();
            if (view == null)
                return;
            DemoUnitModel unit = _simulation.GetUnit(view.UnitId);
            if (unit == null || unit.Team != DemoTeam.Player || !unit.IsAlive)
                return;
            if (additive && _selection.Contains(unit.Id))
                _selection.Remove(unit.Id);
            else
                _selection.Add(unit.Id);
        }

        private void SelectInRectangle(Vector2 start, Vector2 end, bool additive)
        {
            if (!additive)
                _selection.Clear();
            Rect rect = ScreenRect(start, end);
            foreach (DemoUnitModel unit in _simulation.Units.Where(unit => unit.Team == DemoTeam.Player && unit.IsAlive))
            {
                Vector3 screen = _camera.WorldToScreenPoint(unit.Position);
                if (screen.z > 0f && rect.Contains(new Vector2(screen.x, screen.y)))
                    _selection.Add(unit.Id);
            }
        }

        private void EngageTarget(int targetId)
        {
            DemoUnitModel target = _simulation.GetUnit(targetId);
            DemoUnitModel attacker = _selection
                .Select(_simulation.GetUnit)
                .Where(unit => unit != null && unit.IsAlive && unit.Team == DemoTeam.Player && !unit.IsFixed)
                .OrderBy(unit => Vector3.Distance(unit.Position, target.Position))
                .FirstOrDefault();
            if (attacker == null)
            {
                _statusMessage = "请先选择一个可作战单位";
                return;
            }

            DemoCommandResult result = _simulation.StartCombat(attacker.Id, targetId);
            ApplyResult(result);
            if (!result.Success)
                return;

            int combatId = target.CombatId >= 0 ? target.CombatId : attacker.CombatId;
            List<int> remaining = _selection.Where(id => id != attacker.Id).ToList();
            if (remaining.Count > 0 && combatId >= 0)
                _simulation.RequestReinforcement(remaining, combatId);
            _commandMode = CommandMode.Select;
        }

        private void RemoteStrike(Vector3 target)
        {
            DemoUnitModel artillery = _selection.Select(_simulation.GetUnit)
                .FirstOrDefault(unit => unit != null && unit.IsAlive && unit.Stats.CanRemoteStrike);
            ApplyResult(artillery == null
                ? DemoCommandResult.Fail("选中单位中没有远程打击单位（紫色单位）")
                : _simulation.ScheduleRemoteStrike(artillery.Id, target));
            _commandMode = CommandMode.Select;
        }

        private void ReinforceNearestBattle()
        {
            List<DemoCombatModel> battles = _simulation.Combats.Where(combat => !combat.IsFinished).ToList();
            if (battles.Count == 0)
            {
                _statusMessage = "当前没有可增援的战斗";
                return;
            }
            DemoUnitModel anchor = _selection.Select(_simulation.GetUnit).FirstOrDefault(unit => unit != null);
            if (anchor == null)
            {
                _statusMessage = "请先选择增援单位";
                return;
            }
            DemoCombatModel closest = battles.OrderBy(combat => Vector3.Distance(anchor.Position, combat.Center)).First();
            ApplyResult(_simulation.RequestReinforcement(_selection, closest.Id));
        }

        private void FocusSelection()
        {
            List<DemoUnitModel> units = _selection.Select(_simulation.GetUnit).Where(unit => unit != null && unit.IsAlive).ToList();
            if (units.Count == 0)
                return;
            Vector3 center = units.Aggregate(Vector3.zero, (sum, unit) => sum + unit.Position) / units.Count;
            _cameraController.Focus(center);
        }

        private void TogglePause()
        {
            _paused = !_paused;
            Time.timeScale = _paused ? 0f : 1f;
            _statusMessage = _paused ? "模拟已暂停：仍可浏览、选择和下达命令" : "模拟继续";
        }

        private void ApplyResult(DemoCommandResult result)
        {
            _statusMessage = result.Message;
        }

        private void OnBattleEvent(DemoBattleEvent battleEvent)
        {
            _events.Insert(0, battleEvent);
            if (_events.Count > 8)
                _events.RemoveAt(_events.Count - 1);
            if (battleEvent.Important)
                _statusMessage = battleEvent.Message;
        }

        private void SyncViews()
        {
            foreach (KeyValuePair<int, Demo1UnitView> pair in _unitViews)
            {
                DemoUnitModel model = _simulation.GetUnit(pair.Key);
                pair.Value.Sync(model, _selection.Contains(pair.Key));
            }

            foreach (DemoCombatModel combat in _simulation.Combats)
            {
                if (!_combatViews.TryGetValue(combat.Id, out CombatVisual visual))
                {
                    GameObject root = new GameObject($"Combat #{combat.Id}");
                    root.transform.SetParent(transform);
                    visual = new CombatVisual
                    {
                        Root = root,
                        Reinforcement = Demo1Drawing.CreateCircle(root.transform, "Reinforcement Zone", new Color(0.2f, 0.75f, 1f, 0.55f), 0.12f, 96),
                        Forced = Demo1Drawing.CreateCircle(root.transform, "Forced Engagement Zone", new Color(1f, 0.28f, 0.12f, 0.85f), 0.16f, 72)
                    };
                    _combatViews.Add(combat.Id, visual);
                }
                visual.Root.SetActive(!combat.IsFinished);
                Demo1Drawing.SetCircle(visual.Reinforcement, combat.Center, combat.ReinforcementRadius, 0.07f);
                Demo1Drawing.SetCircle(visual.Forced, combat.Center, combat.ForcedRadius, 0.09f);
            }

            foreach (DemoRemoteStrikeModel strike in _simulation.RemoteStrikes)
            {
                if (!_strikeViews.TryGetValue(strike.Id, out StrikeVisual visual))
                {
                    GameObject root = new GameObject($"Remote Strike #{strike.Id}");
                    root.transform.SetParent(transform);
                    visual = new StrikeVisual
                    {
                        Root = root,
                        Radius = Demo1Drawing.CreateCircle(root.transform, "Target Area", new Color(1f, 0.78f, 0.1f, 0.95f), 0.18f, 64)
                    };
                    _strikeViews.Add(strike.Id, visual);
                }
                visual.Root.SetActive(!strike.Resolved);
                Demo1Drawing.SetCircle(visual.Radius, strike.Target, strike.Radius, 0.11f);
            }

            _selection.RemoveWhere(id => _simulation.GetUnit(id)?.IsAlive != true);
        }

        private void OnGUI()
        {
            if (_simulation == null || _camera == null)
                return;
            EnsureGuiStyles();
            DrawWorldLabels();
            DrawTopBar();
            DrawSidePanel();
            DrawSelectionBox();
            if (_simulation.Outcome != DemoOutcome.Running)
                DrawOutcome();
        }

        private void DrawTopBar()
        {
            GUI.Box(new Rect(0f, 0f, Screen.width, 44f), string.Empty);
            GUI.Label(new Rect(16f, 8f, 430f, 30f), "DEMO 1.0  ·  多佛海峡防卫战", _titleStyle);
            string state = _paused ? "已暂停" : "进行中";
            GUI.Label(new Rect(Screen.width - 320f, 10f, 300f, 25f), $"{state}  |  T+ {_simulation.SimulationTime:0.0}s", _centerStyle);
        }

        private void DrawSidePanel()
        {
            GUI.Box(new Rect(0f, 44f, PanelWidth, Screen.height - 44f), string.Empty);
            float y = 58f;
            GUI.Label(new Rect(14f, y, PanelWidth - 28f, 46f), "目标：摧毁东侧异形军巢穴\n战斗自动结算；每名魔女独立移动。", _smallStyle);
            y += 52f;

            if (GUI.Button(new Rect(14f, y, 98f, 32f), _paused ? "继续 [Space]" : "暂停 [Space]")) TogglePause();
            if (GUI.Button(new Rect(120f, y, 98f, 32f), "聚焦 [F]")) FocusSelection();
            if (GUI.Button(new Rect(226f, y, 98f, 32f), "撤退 [R]")) ApplyResult(_simulation.RequestRetreat(_selection));
            y += 40f;
            if (GUI.Button(new Rect(14f, y, 98f, 32f), "交战 [A]"))
            {
                _commandMode = CommandMode.Engage;
                _statusMessage = "交战模式：点击敌方目标";
            }
            if (GUI.Button(new Rect(120f, y, 98f, 32f), "增援 [G]")) ReinforceNearestBattle();
            if (GUI.Button(new Rect(226f, y, 98f, 32f), "打击 [B]"))
            {
                _commandMode = CommandMode.RemoteStrike;
                _statusMessage = "远程打击模式：点击目标区域";
            }
            y += 43f;

            GUI.Label(new Rect(14f, y, PanelWidth - 28f, 42f), _statusMessage, _smallStyle);
            y += 48f;
            GUI.Label(new Rect(14f, y, 150f, 24f), $"已选择 {_selection.Count} 个单位", _titleStyle);
            y += 27f;

            List<DemoUnitModel> selected = _selection.Select(_simulation.GetUnit).Where(unit => unit != null).Take(4).ToList();
            foreach (DemoUnitModel unit in selected)
            {
                DrawUnitCard(unit, y);
                y += 70f;
            }

            y = Mathf.Max(y + 4f, Screen.height - 218f);
            GUI.Label(new Rect(14f, y, 200f, 24f), "事件（点击可定位）", _titleStyle);
            y += 27f;
            foreach (DemoBattleEvent battleEvent in _events.Take(5))
            {
                if (GUI.Button(new Rect(14f, y, PanelWidth - 28f, 27f), $"{battleEvent.Time:000.0}  {battleEvent.Message}"))
                    _cameraController.Focus(battleEvent.Position);
                y += 30f;
            }
        }

        private void DrawUnitCard(DemoUnitModel unit, float y)
        {
            GUI.Box(new Rect(14f, y, PanelWidth - 28f, 64f), string.Empty);
            GUI.Label(new Rect(22f, y + 4f, 190f, 22f), $"{unit.DisplayName}  ·  {ActivityName(unit.Activity)}", _smallStyle);
            GUI.Label(new Rect(218f, y + 4f, 96f, 22f), unit.Role.ToString(), _smallStyle);
            DrawBar(new Rect(22f, y + 29f, 88f, 11f), unit.HealthRatio, new Color(0.9f, 0.22f, 0.2f), $"HP {unit.Health:0}");
            DrawBar(new Rect(118f, y + 29f, 88f, 11f), unit.MagicRatio, new Color(0.35f, 0.55f, 1f), $"MP {unit.Magic:0}");
            DrawBar(new Rect(214f, y + 29f, 88f, 11f), unit.ShieldRatio, new Color(0.2f, 0.9f, 0.95f), $"盾 {unit.Shield:0}");
            if (unit.Activity == DemoUnitActivity.Retreating)
                GUI.Label(new Rect(22f, y + 44f, 280f, 18f), $"撤退进度 {unit.RetreatProgress:P0}", _smallStyle);
            else if (unit.Stats.CanRemoteStrike)
                GUI.Label(new Rect(22f, y + 44f, 280f, 18f), $"远程打击冷却 {unit.RemoteStrikeCooldown:0.0}s", _smallStyle);
        }

        private void DrawWorldLabels()
        {
            foreach (DemoUnitModel unit in _simulation.Units)
            {
                if (!unit.IsAlive || (unit.Team == DemoTeam.Enemy && !unit.IsRevealedToPlayer))
                    continue;
                Vector3 screen = _camera.WorldToScreenPoint(unit.Position + Vector3.up * (unit.IsFixed ? 2.8f : 2f));
                if (screen.z <= 0f)
                    continue;
                float y = Screen.height - screen.y;
                GUI.Label(new Rect(screen.x - 80f, y - 14f, 160f, 20f), unit.DisplayName, _worldLabelStyle);
                DrawBar(new Rect(screen.x - 34f, y + 7f, 68f, 5f), unit.HealthRatio,
                    unit.Team == DemoTeam.Player ? new Color(0.2f, 0.75f, 1f) : new Color(1f, 0.2f, 0.18f), string.Empty);
            }

            foreach (DemoCombatModel combat in _simulation.Combats.Where(combat => !combat.IsFinished))
            {
                Vector3 screen = _camera.WorldToScreenPoint(combat.Center);
                GUI.Label(new Rect(screen.x - 60f, Screen.height - screen.y - 15f, 120f, 24f), $"战斗 #{combat.Id}", _worldLabelStyle);
            }

            foreach (DemoRemoteStrikeModel strike in _simulation.RemoteStrikes.Where(strike => !strike.Resolved))
            {
                Vector3 screen = _camera.WorldToScreenPoint(strike.Target);
                GUI.Label(new Rect(screen.x - 70f, Screen.height - screen.y - 15f, 140f, 24f), $"打击 {Mathf.Max(0f, strike.Remaining):0.0}s", _worldLabelStyle);
            }
        }

        private void DrawSelectionBox()
        {
            if (!_dragSelecting || _commandMode != CommandMode.Select)
                return;
            Vector2 current = Input.mousePosition;
            Rect screenRect = ScreenRect(_dragStart, current);
            Rect guiRect = new Rect(screenRect.x, Screen.height - screenRect.yMax, screenRect.width, screenRect.height);
            GUI.DrawTexture(guiRect, _selectionTexture);
            GUI.Box(guiRect, string.Empty);
        }

        private void DrawOutcome()
        {
            Rect box = new Rect(Screen.width * 0.5f - 220f, Screen.height * 0.5f - 100f, 440f, 200f);
            GUI.Box(box, string.Empty);
            string title = _simulation.Outcome == DemoOutcome.Victory ? "任务完成" : "任务失败";
            GUI.Label(new Rect(box.x + 20f, box.y + 28f, box.width - 40f, 40f), title, _titleStyle);
            GUI.Label(new Rect(box.x + 20f, box.y + 75f, box.width - 40f, 36f),
                _simulation.Outcome == DemoOutcome.Victory ? "敌方固定目标已被摧毁。" : "我方已无可继续作战的单位。", _centerStyle);
            if (GUI.Button(new Rect(box.x + 120f, box.y + 132f, 200f, 38f), "重新开始 [Enter]"))
                RestartScene();
        }

        private void DrawBar(Rect rect, float ratio, Color color, string text)
        {
            Color old = GUI.color;
            GUI.color = new Color(0f, 0f, 0f, 0.7f);
            GUI.DrawTexture(rect, Texture2D.whiteTexture);
            GUI.color = color;
            GUI.DrawTexture(new Rect(rect.x, rect.y, rect.width * Mathf.Clamp01(ratio), rect.height), Texture2D.whiteTexture);
            GUI.color = old;
            if (!string.IsNullOrEmpty(text))
                GUI.Label(new Rect(rect.x, rect.y - 2f, rect.width, 18f), text, _smallStyle);
        }

        private void EnsureGuiStyles()
        {
            if (_titleStyle != null)
                return;
            _titleStyle = new GUIStyle(GUI.skin.label) { fontSize = 17, fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleLeft };
            _smallStyle = new GUIStyle(GUI.skin.label) { fontSize = 12, wordWrap = true, alignment = TextAnchor.UpperLeft };
            _centerStyle = new GUIStyle(GUI.skin.label) { fontSize = 14, alignment = TextAnchor.MiddleCenter };
            _worldLabelStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 12,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter,
                normal = { textColor = Color.white }
            };
            _selectionTexture = new Texture2D(1, 1);
            _selectionTexture.SetPixel(0, 0, new Color(0.12f, 0.8f, 1f, 0.18f));
            _selectionTexture.Apply();
        }

        private Demo1UnitView RaycastUnit(Vector3 screenPoint)
        {
            Ray ray = _camera.ScreenPointToRay(screenPoint);
            RaycastHit[] hits = Physics.RaycastAll(ray, 1000f);
            return hits.OrderBy(hit => hit.distance)
                .Select(hit => hit.collider.GetComponent<Demo1UnitView>())
                .FirstOrDefault(view => view != null && _simulation.GetUnit(view.UnitId)?.IsAlive == true);
        }

        private bool TryGroundPoint(Vector3 screenPoint, out Vector3 worldPoint)
        {
            Ray ray = _camera.ScreenPointToRay(screenPoint);
            Plane plane = new Plane(Vector3.up, Vector3.zero);
            if (plane.Raycast(ray, out float distance))
            {
                worldPoint = ray.GetPoint(distance);
                return true;
            }
            worldPoint = Vector3.zero;
            return false;
        }

        private bool IsPointerOverHud()
        {
            Vector3 mouse = Input.mousePosition;
            return mouse.x <= PanelWidth || mouse.y >= Screen.height - 44f;
        }

        private void CreateGridLine(Transform parent, Vector3 a, Vector3 b)
        {
            LineRenderer line = Demo1Drawing.CreateLine(parent, "Grid Line", new Color(0.22f, 0.38f, 0.42f, 0.35f), 0.025f);
            line.SetPosition(0, a);
            line.SetPosition(1, b);
        }

        private void CreateLandmark(string name, Vector3 position, Vector3 scale, Color color)
        {
            GameObject landmark = GameObject.CreatePrimitive(PrimitiveType.Cube);
            landmark.name = name;
            landmark.transform.SetParent(transform);
            landmark.transform.position = position;
            landmark.transform.localScale = scale;
            landmark.GetComponent<Renderer>().sharedMaterial = Demo1Drawing.CreateMaterial(color);
        }

        private void RestartScene()
        {
            Time.timeScale = 1f;
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
        }

        private static Rect ScreenRect(Vector2 a, Vector2 b)
        {
            return Rect.MinMaxRect(Mathf.Min(a.x, b.x), Mathf.Min(a.y, b.y), Mathf.Max(a.x, b.x), Mathf.Max(a.y, b.y));
        }

        private static string ActivityName(DemoUnitActivity activity)
        {
            switch (activity)
            {
                case DemoUnitActivity.Idle: return "待命";
                case DemoUnitActivity.Moving: return "移动";
                case DemoUnitActivity.Reinforcing: return "增援中";
                case DemoUnitActivity.Fighting: return "战斗中";
                case DemoUnitActivity.Retreating: return "撤退中";
                case DemoUnitActivity.Protected: return "脱战保护";
                case DemoUnitActivity.Destroyed: return "失去战斗力";
                default: return activity.ToString();
            }
        }
    }
}
