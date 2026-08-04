using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.EventSystems;
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

        private sealed class PendingEngagement
        {
            public int TargetId;
            public List<int> UnitIds;
        }

        private Demo1Simulation _simulation;
        private Demo1Balance _balance;
        [SerializeField] private Demo1BalanceConfig _balanceConfig;
        [SerializeField] private DemoUnitConfig[] _unitConfigs;
        private Camera _camera;
        private Demo1CameraController _cameraController;
        private Demo1BattleUI _battleUi;
        private CommandMode _commandMode;
        private Vector2 _dragStart;
        private bool _dragSelecting;
        private bool _paused;
        private PendingEngagement _pendingEngagement;
        private string _statusMessage = "左键选择，右键移动；先靠近已发现目标再交战。";
        private GUIStyle _titleStyle;
        private GUIStyle _smallStyle;
        private GUIStyle _centerStyle;
        private GUIStyle _worldLabelStyle;
        private GUIStyle _detailHeaderStyle;
        private GUIStyle _detailValueStyle;
        private GUIStyle _detailMutedStyle;
        private Vector2 _detailScroll;
        private Texture2D _selectionTexture;
        private const float PanelWidth = 340f;
        private const string BalanceResourcePath = "Configs/Demo1Balance";
        private const string UnitResourcePath = "Configs/Units";

        public Demo1Simulation Simulation => _simulation;
        public IReadOnlyCollection<int> SelectedUnitIds => _selection;
        public bool IsPaused => _paused;
        public string StatusMessage => _statusMessage;
        public IReadOnlyList<DemoBattleEvent> BattleEvents => _events;
        public Camera BattleCamera => _camera;
        public int CharacterDetailUnitId => IsCharacterDetailVisible(out DemoUnitModel unit) ? unit.Id : -1;

        private void Start()
        {
            LoadScenarioConfigs();
            _balance = _balanceConfig != null ? _balanceConfig.CreateRuntimeValue() : new Demo1Balance();
            _simulation = new Demo1Simulation(_balance);
            _simulation.EventRaised += OnBattleEvent;
            BuildEnvironment();
            BuildCamera();
            CreateScenario();
            BuildBattleUi();
            SyncViews();
        }

        private void LoadScenarioConfigs()
        {
            if (_balanceConfig == null)
                _balanceConfig = Resources.Load<Demo1BalanceConfig>(BalanceResourcePath);
            if (_unitConfigs != null)
            {
                _unitConfigs = _unitConfigs
                    .Where(config => config != null)
                    .OrderBy(config => config.SpawnOrder)
                    .ToArray();
            }
            if (_unitConfigs == null || _unitConfigs.Length == 0)
            {
                _unitConfigs = Resources.LoadAll<DemoUnitConfig>(UnitResourcePath)
                    .Where(config => config != null)
                    .OrderBy(config => config.SpawnOrder)
                    .ToArray();
            }
        }

        private void Update()
        {
            if (_simulation == null)
                return;

            HandleGlobalInput();
            if (!_paused)
                _simulation.Advance(Time.deltaTime);
            ResolvePendingEngagement();
            HandlePointerInput();
            SyncViews();
            _battleUi?.Sync();
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
            if (_unitConfigs != null && _unitConfigs.Length > 0)
                CreateConfiguredScenario();
            else
                CreateFallbackScenario();
        }

        private void CreateConfiguredScenario()
        {
            List<KeyValuePair<DemoUnitConfig, DemoUnitModel>> spawned = new List<KeyValuePair<DemoUnitConfig, DemoUnitModel>>();
            foreach (DemoUnitConfig config in _unitConfigs.Where(config => config != null).OrderBy(config => config.SpawnOrder))
            {
                DemoUnitModel unit = AddUnit(config.DisplayName, config.Team, config.Role,
                    config.CreateRuntimeStats(), config.StartingPosition);
                spawned.Add(new KeyValuePair<DemoUnitConfig, DemoUnitModel>(config, unit));
                if (config.GrantPersistentPlayerIntel)
                    _simulation.GrantPersistentPlayerIntel(unit.Id);
            }

            foreach (KeyValuePair<DemoUnitConfig, DemoUnitModel> item in spawned)
            {
                switch (item.Key.EnemyAiProfile)
                {
                    case DemoEnemyAiProfile.Scout:
                        _simulation.ConfigureScoutAi(item.Value.Id, item.Key.ScoutPatrolPoints);
                        break;
                    case DemoEnemyAiProfile.Combat:
                        _simulation.ConfigureCombatAi(item.Value.Id, item.Key.GetEnemyAiHomePosition());
                        break;
                }
            }

            FinishScenarioSetup();
        }

        private void CreateFallbackScenario()
        {
            DemoUnitStats witch = new DemoUnitStats();
            witch.PreferredBattleLine = DemoBattleLine.Vanguard;
            witch.ScreenPower = 1f;
            witch.WitchVisionType = DemoWitchVisionType.Ordinary;
            witch.VisionRadius = 24f;
            witch.VisionAngle = 100f;
            DemoUnitStats ace = witch.Clone();
            ace.MaxHealth = 145f;
            ace.Attack = 29f;
            ace.CoreDiscovery = 0.28f;
            ace.Mobility = 1.25f;
            ace.ScreenPower = 1.2f;
            ace.Traits = DemoUnitTrait.SakamotoCoreInsight;

            DemoUnitStats support = witch.Clone();
            support.Attack = 18f;
            support.MaxMagic = 120f;
            support.MaxShield = 60f;
            support.GlobalShieldBonus = 0f;
            support.MagicRecovery = 6f;
            support.PreferredBattleLine = DemoBattleLine.Support;
            support.ScreenPower = 0.35f;
            support.Traits = DemoUnitTrait.MiyafujiShieldAura;

            DemoUnitStats artillery = witch.Clone();
            artillery.Attack = 22f;
            artillery.CanRemoteStrike = false;
            artillery.CoreDiscovery = 0.22f;
            artillery.PreferredBattleLine = DemoBattleLine.Main;
            artillery.AttackProfile = DemoAttackProfile.Standard;
            artillery.ScreenPenetration = 0f;
            artillery.ScreenPower = 0.25f;
            artillery.Traits = DemoUnitTrait.LynetteSharpshooter;

            DemoUnitStats nightWitch = witch.Clone();
            nightWitch.MaxMagic = 110f;
            nightWitch.MaxShield = 55f;
            nightWitch.Attack = 23f;
            nightWitch.CoreDiscovery = 0.24f;
            nightWitch.PreferredBattleLine = DemoBattleLine.Main;
            nightWitch.WitchVisionType = DemoWitchVisionType.Night;
            nightWitch.VisionRadius = 28f;
            nightWitch.VisionAngle = 360f;

            DemoUnitStats neuroi = witch.Clone();
            neuroi.WitchVisionType = DemoWitchVisionType.None;
            neuroi.MaxHealth = 190f;
            neuroi.Attack = 26f;
            neuroi.Defense = 10f;
            neuroi.MaxMagic = 110f;
            neuroi.MaxShield = 70f;
            neuroi.CoreConcealment = 0.65f;
            neuroi.MoveSpeed = 4.8f;
            neuroi.EngagementRadius = 7.5f;
            neuroi.PreferredBattleLine = DemoBattleLine.Main;
            neuroi.AttackProfile = DemoAttackProfile.ScreenPiercing;
            neuroi.ScreenPenetration = 0.5f;
            neuroi.ScreenPower = 0.45f;

            DemoUnitStats guard = neuroi.Clone();
            guard.MaxHealth = 260f;
            guard.Defense = 13f;
            guard.Attack = 29f;
            guard.MaxMagic = 120f;
            guard.MaxShield = 100f;
            guard.MoveSpeed = 4.2f;
            guard.PreferredBattleLine = DemoBattleLine.Vanguard;
            guard.AttackProfile = DemoAttackProfile.Standard;
            guard.ScreenPenetration = 0f;
            guard.ScreenPower = 1.15f;

            DemoUnitStats fortress = neuroi.Clone();
            fortress.MaxHealth = 720f;
            fortress.Attack = 42f;
            fortress.Defense = 16f;
            fortress.MaxMagic = 300f;
            fortress.MaxShield = 260f;
            fortress.CoreConcealment = 0.9f;
            fortress.AttackInterval = 2.2f;
            fortress.EngagementRadius = 10f;
            fortress.VisionRadius = 26f;
            fortress.MoveSpeed = 0f;
            fortress.PreferredBattleLine = DemoBattleLine.Support;
            fortress.AttackProfile = DemoAttackProfile.Standard;
            fortress.ScreenPenetration = 0f;
            fortress.ScreenPower = 0f;

            AddUnit("宫藤芳佳", DemoTeam.Player, DemoUnitRole.Support, support, new Vector3(-27f, 0f, -10f));
            AddUnit("坂本美绪", DemoTeam.Player, DemoUnitRole.Witch, ace, new Vector3(-25f, 0f, -3f));
            AddUnit("莉涅特", DemoTeam.Player, DemoUnitRole.Artillery, artillery, new Vector3(-27f, 0f, 5f));
            AddUnit("佩琳", DemoTeam.Player, DemoUnitRole.Witch, witch, new Vector3(-23f, 0f, 12f));
            AddUnit("桑妮亚·V·利特维亚克", DemoTeam.Player, DemoUnitRole.Witch, nightWitch, new Vector3(-32f, 0f, 12f));

            DemoUnitModel scout = AddUnit("异形军侦察体", DemoTeam.Enemy, DemoUnitRole.Scout, neuroi, new Vector3(2f, 0f, -9f));
            DemoUnitModel guardA = AddUnit("异形军护卫 A", DemoTeam.Enemy, DemoUnitRole.Guard, guard, new Vector3(17f, 0f, -3f));
            DemoUnitModel guardB = AddUnit("异形军护卫 B", DemoTeam.Enemy, DemoUnitRole.Guard, guard, new Vector3(18f, 0f, 8f));
            DemoUnitModel objective = AddUnit("异形军巢穴", DemoTeam.Enemy, DemoUnitRole.Fortress, fortress, new Vector3(34f, 0f, 3f));
            _simulation.GrantPersistentPlayerIntel(objective.Id);

            _simulation.ConfigureScoutAi(scout.Id, new[]
            {
                scout.Position,
                new Vector3(-10f, 0f, -5f),
                new Vector3(-2f, 0f, 4f),
                new Vector3(8f, 0f, -1f)
            });
            _simulation.ConfigureCombatAi(guardA.Id, guardA.Position);
            _simulation.ConfigureCombatAi(guardB.Id, guardB.Position);
            FinishScenarioSetup();
        }

        private void FinishScenarioSetup()
        {
            _events.Clear();
            SelectAllPlayerUnits();
            _statusMessage = "全队已选中：右键地面移动，右键红色敌人会自动接近并开战。";
        }

        public void SelectAllPlayerUnits()
        {
            if (_simulation == null)
                return;

            _selection.Clear();
            foreach (DemoUnitModel unit in _simulation.Units.Where(unit => unit.Team == DemoTeam.Player && unit.IsAlive))
                _selection.Add(unit.Id);
        }

        public void SelectUnits(IEnumerable<int> unitIds)
        {
            _selection.Clear();
            if (_simulation == null || unitIds == null)
                return;

            foreach (int id in unitIds)
            {
                DemoUnitModel unit = _simulation.GetUnit(id);
                if (unit != null && unit.IsAlive && unit.Team == DemoTeam.Player)
                    _selection.Add(id);
            }
        }

        public DemoCommandResult CommandMove(Vector3 destination)
        {
            _pendingEngagement = null;
            DemoCommandResult result = _simulation == null
                ? DemoCommandResult.Fail("战场尚未初始化")
                : _simulation.IssueMove(_selection, destination);
            ApplyResult(result);
            return result;
        }

        public DemoCommandResult CommandEngage(int targetId)
        {
            if (_simulation == null)
                return ApplyAndReturn(DemoCommandResult.Fail("战场尚未初始化"));

            DemoUnitModel target = _simulation.GetUnit(targetId);
            if (target == null || !target.IsAlive || target.Team != DemoTeam.Enemy)
                return ApplyAndReturn(DemoCommandResult.Fail("交战目标无效"));
            if (!target.CanBeDirectlyTargetedByPlayer)
                return ApplyAndReturn(DemoCommandResult.Fail(target.HasPlayerIntel ? "当前只有目标的最后已知位置" : "尚未获得目标情报"));

            List<DemoUnitModel> attackers = _selection
                .Select(_simulation.GetUnit)
                .Where(unit => unit != null && unit.IsAlive && unit.Team == DemoTeam.Player && !unit.IsFixed && unit.CombatId < 0)
                .OrderBy(unit => Vector3.Distance(unit.Position, target.Position))
                .ToList();
            if (attackers.Count == 0)
                return ApplyAndReturn(DemoCommandResult.Fail("请先选择一个可作战单位"));

            DemoUnitModel attacker = attackers[0];
            if (target.CombatId >= 0)
                return ApplyAndReturn(_simulation.RequestReinforcement(attackers.Select(unit => unit.Id), target.CombatId));

            float distance = Vector3.Distance(attacker.Position, target.Position);
            if (distance <= attacker.Stats.EngagementRadius)
                return StartSelectedCombat(attacker, target, attackers.Select(unit => unit.Id).ToList());

            List<int> unitIds = attackers.Select(unit => unit.Id).ToList();
            DemoCommandResult moveResult = _simulation.IssueMove(unitIds, target.Position);
            if (!moveResult.Success)
                return ApplyAndReturn(moveResult);

            _pendingEngagement = new PendingEngagement { TargetId = targetId, UnitIds = unitIds };
            _commandMode = CommandMode.Select;
            return ApplyAndReturn(DemoCommandResult.Ok($"正在接近 {target.DisplayName}，进入射程后将自动开战"));
        }

        public DemoCommandResult CommandRemoteStrike(Vector3 target)
        {
            if (_simulation == null)
                return ApplyAndReturn(DemoCommandResult.Fail("战场尚未初始化"));

            DemoUnitModel artillery = _selection.Select(_simulation.GetUnit)
                .FirstOrDefault(unit => unit != null && unit.IsAlive && unit.Stats.CanRemoteStrike);
            _commandMode = CommandMode.Select;
            return ApplyAndReturn(artillery == null
                ? DemoCommandResult.Fail("选中单位中没有远程打击单位（紫色单位）")
                : _simulation.ScheduleRemoteStrike(artillery.Id, target));
        }

        private void EnterRemoteStrikeMode()
        {
            bool hasRemoteStriker = _simulation != null && _selection.Select(_simulation.GetUnit)
                .Any(unit => unit != null && unit.IsAlive && unit.Stats.CanRemoteStrike);
            if (!hasRemoteStriker)
            {
                _commandMode = CommandMode.Select;
                _statusMessage = "当前选中单位没有远程打击能力";
                return;
            }
            _commandMode = CommandMode.RemoteStrike;
            _statusMessage = "远程打击模式：左键指定目标区域";
        }

        public void SetPaused(bool paused)
        {
            if (_paused == paused)
                return;
            TogglePause();
        }

        public DemoCommandResult CommandBattleLineChange(int unitId, DemoBattleLine line)
        {
            DemoCommandResult result = _simulation.RequestBattleLineChange(unitId, line);
            ApplyResult(result);
            return result;
        }

        public DemoCommandResult CommandReinforceCombat(int combatId)
        {
            DemoCommandResult result = _simulation.RequestReinforcement(_selection, combatId);
            ApplyResult(result);
            return result;
        }

        public DemoCommandResult CommandRetreatUnit(int unitId)
        {
            DemoCommandResult result = _simulation.RequestRetreat(new[] { unitId });
            ApplyResult(result);
            return result;
        }

        public void FocusCombat(int combatId)
        {
            DemoCombatModel combat = _simulation.GetCombat(combatId);
            if (combat != null)
                _cameraController.Focus(combat.Center);
        }

        private void BuildBattleUi()
        {
            GameObject uiObject = new GameObject("Demo1 Battle UI");
            uiObject.transform.SetParent(transform, false);
            _battleUi = uiObject.AddComponent<Demo1BattleUI>();
            _battleUi.Initialize(this);
        }

        private DemoUnitModel AddUnit(string name, DemoTeam team, DemoUnitRole role, DemoUnitStats stats, Vector3 position)
        {
            DemoUnitModel model = _simulation.AddUnit(name, team, role, stats, position);
            PrimitiveType primitive = role == DemoUnitRole.Fortress
                ? PrimitiveType.Cube
                : role == DemoUnitRole.Scout ? PrimitiveType.Sphere : PrimitiveType.Capsule;
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
                if (_battleUi != null && _battleUi.IsPanelOpen)
                {
                    _battleUi.ClosePanel();
                    return;
                }
                _commandMode = CommandMode.Select;
                _statusMessage = "命令已取消";
            }
            if (Input.GetKeyDown(KeyCode.A))
            {
                _commandMode = CommandMode.Engage;
                _statusMessage = "交战模式：左键点击已发现的敌方目标";
            }
            if (Input.GetKeyDown(KeyCode.B))
                EnterRemoteStrikeMode();
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
            if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
                return;
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
                DemoUnitModel enemyTarget = targetView == null ? null : _simulation.GetUnit(targetView.UnitId);
                if (enemyTarget != null && enemyTarget.Team == DemoTeam.Enemy && enemyTarget.CanBeDirectlyTargetedByPlayer)
                    CommandEngage(enemyTarget.Id);
                else if (enemyTarget != null && enemyTarget.Team == DemoTeam.Enemy && enemyTarget.HasPlayerIntel)
                {
                    CommandMove(enemyTarget.PlayerVisiblePosition);
                    _statusMessage = "正在前往敌方最后已知位置搜索";
                }
                else if (TryGroundPoint(Input.mousePosition, out Vector3 point))
                    CommandMove(point);
            }
        }

        private void HandlePrimaryClick()
        {
            if (_commandMode == CommandMode.RemoteStrike)
            {
                if (TryGroundPoint(Input.mousePosition, out Vector3 target))
                    CommandRemoteStrike(target);
                return;
            }

            Demo1UnitView view = RaycastUnit(Input.mousePosition);
            if (_commandMode == CommandMode.Engage)
            {
                if (view == null || _simulation.GetUnit(view.UnitId)?.Team != DemoTeam.Enemy)
                    _statusMessage = "交战命令需要一个已发现的敌方目标";
                else
                    CommandEngage(view.UnitId);
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

        private DemoCommandResult StartSelectedCombat(DemoUnitModel attacker, DemoUnitModel target, List<int> selectedIds)
        {
            DemoCommandResult result = _simulation.StartCombat(attacker.Id, target.Id);
            ApplyResult(result);
            if (!result.Success)
                return result;

            int combatId = target.CombatId >= 0 ? target.CombatId : attacker.CombatId;
            List<int> remaining = selectedIds.Where(id => id != attacker.Id).ToList();
            if (remaining.Count > 0 && combatId >= 0)
                _simulation.RequestReinforcement(remaining, combatId);
            _pendingEngagement = null;
            _commandMode = CommandMode.Select;
            return result;
        }

        private void ResolvePendingEngagement()
        {
            if (_pendingEngagement == null || _simulation == null || _paused)
                return;

            DemoUnitModel target = _simulation.GetUnit(_pendingEngagement.TargetId);
            if (target == null || !target.IsAlive)
            {
                _pendingEngagement = null;
                _statusMessage = "自动接战已取消：目标已失效";
                return;
            }
            if (!target.CanBeDirectlyTargetedByPlayer)
            {
                _pendingEngagement = null;
                _statusMessage = "自动接战已取消：目标失去确认，单位将前往最后已知位置";
                return;
            }

            List<DemoUnitModel> attackers = _pendingEngagement.UnitIds
                .Select(_simulation.GetUnit)
                .Where(unit => unit != null && unit.IsAlive && unit.Team == DemoTeam.Player && unit.CombatId < 0)
                .OrderBy(unit => Vector3.Distance(unit.Position, target.Position))
                .ToList();
            if (attackers.Count == 0)
            {
                _pendingEngagement = null;
                return;
            }

            if (target.CombatId >= 0)
            {
                ApplyResult(_simulation.RequestReinforcement(attackers.Select(unit => unit.Id), target.CombatId));
                _pendingEngagement = null;
                return;
            }

            DemoUnitModel attacker = attackers[0];
            if (Vector3.Distance(attacker.Position, target.Position) <= attacker.Stats.EngagementRadius)
                StartSelectedCombat(attacker, target, _pendingEngagement.UnitIds);
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

        private DemoCommandResult ApplyAndReturn(DemoCommandResult result)
        {
            ApplyResult(result);
            return result;
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
            if (_battleUi == null || !_battleUi.IsPanelOpen)
            {
                DrawWorldLabels();
                DrawCharacterDetailPanel();
            }
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
                EnterRemoteStrikeMode();
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
            GUI.Label(new Rect(196f, y + 4f, 118f, 22f), $"{unit.Role} / {VisionTypeName(unit.Stats.WitchVisionType)}", _smallStyle);
            DrawBar(new Rect(22f, y + 29f, 88f, 11f), unit.HealthRatio, new Color(0.9f, 0.22f, 0.2f), $"HP {unit.Health:0}");
            DrawBar(new Rect(118f, y + 29f, 88f, 11f), unit.MagicRatio, new Color(0.35f, 0.55f, 1f), $"MP {unit.Magic:0}");
            DrawBar(new Rect(214f, y + 29f, 88f, 11f), unit.ShieldRatio, new Color(0.2f, 0.9f, 0.95f), $"盾 {unit.Shield:0}");
            if (unit.Activity == DemoUnitActivity.Retreating)
                GUI.Label(new Rect(22f, y + 44f, 280f, 18f), $"撤退进度 {unit.RetreatProgress:P0}", _smallStyle);
            else if (unit.Stats.CanRemoteStrike)
                GUI.Label(new Rect(22f, y + 44f, 280f, 18f), $"远程打击冷却 {unit.RemoteStrikeCooldown:0.0}s", _smallStyle);
        }

        private void DrawCharacterDetailPanel()
        {
            if (!IsCharacterDetailVisible(out DemoUnitModel unit))
                return;

            Rect panel = GetCharacterDetailRect();
            GUI.Box(panel, string.Empty);
            GUI.BeginGroup(panel);
            Rect viewport = new Rect(4f, 4f, panel.width - 8f, panel.height - 8f);
            float contentHeight = 660f;
            _detailScroll = GUI.BeginScrollView(viewport, _detailScroll, new Rect(0f, 0f, panel.width - 26f, contentHeight));

            float width = panel.width - 26f;
            float contentWidth = width - 28f;
            float y = 13f;
            GUI.Label(new Rect(14f, y, contentWidth, 27f), "角色详情", _detailHeaderStyle);
            y += 31f;
            GUI.Label(new Rect(14f, y, contentWidth, 25f), unit.DisplayName, _detailValueStyle);
            y += 24f;
            GUI.Label(new Rect(14f, y, contentWidth, 20f),
                $"{RoleName(unit.Role)}  ·  {VisionTypeName(unit.Stats.WitchVisionType)}魔女  ·  ID {unit.Id}", _detailMutedStyle);
            y += 28f;

            DrawDetailBar(new Rect(14f, y, contentWidth, 17f), unit.HealthRatio, new Color(0.9f, 0.22f, 0.2f),
                $"生命  {unit.Health:0}/{unit.Stats.MaxHealth:0}");
            y += 25f;
            DrawDetailBar(new Rect(14f, y, contentWidth, 17f), unit.MagicRatio, new Color(0.32f, 0.52f, 1f),
                $"魔力  {unit.Magic:0}/{unit.Stats.MaxMagic:0}");
            y += 25f;
            DrawDetailBar(new Rect(14f, y, contentWidth, 17f), unit.ShieldRatio, new Color(0.18f, 0.82f, 0.92f),
                $"护盾  {unit.Shield:0}/{unit.Stats.MaxShield:0}");
            y += 34f;

            GUI.Label(new Rect(14f, y, contentWidth, 20f), "当前状态", _detailHeaderStyle);
            y += 22f;
            GUI.Label(new Rect(14f, y, contentWidth, 42f), BuildCurrentActionText(unit), _smallStyle);
            y += 45f;

            GUI.Label(new Rect(14f, y, contentWidth, 20f), "阵位与目标", _detailHeaderStyle);
            y += 22f;
            GUI.Label(new Rect(14f, y, contentWidth, 44f), BuildFormationText(unit), _smallStyle);
            y += 48f;

            GUI.Label(new Rect(14f, y, contentWidth, 20f), "视野", _detailHeaderStyle);
            y += 22f;
            string visionShape = unit.Stats.WitchVisionType == DemoWitchVisionType.Night
                ? $"环形侦测 360°  ·  半径 {unit.Stats.VisionRadius:0.#}"
                : $"扇形目视 {unit.Stats.VisionAngle:0.#}°  ·  半径 {unit.Stats.VisionRadius:0.#}";
            GUI.Label(new Rect(14f, y, contentWidth, 38f), visionShape, _smallStyle);
            y += 42f;

            GUI.Label(new Rect(14f, y, contentWidth, 20f), "作战参数", _detailHeaderStyle);
            y += 22f;
            DrawDetailStatRow(ref y, contentWidth, "攻击", unit.Stats.Attack.ToString("0.#"), "防御", unit.Stats.Defense.ToString("0.#"));
            DrawDetailStatRow(ref y, contentWidth, "机动", unit.Stats.Mobility.ToString("0.#"), "移速", unit.Stats.MoveSpeed.ToString("0.#"));
            DrawDetailStatRow(ref y, contentWidth, "交战距离", unit.Stats.EngagementRadius.ToString("0.#"), "攻击间隔",
                FormatAdjustedSeconds(unit.Stats.AttackInterval, _simulation.GetEffectiveAttackInterval(unit.Id)));
            DrawDetailStatRow(ref y, contentWidth, "暴击",
                FormatAdjustedPercent(unit.Stats.CriticalChance, _simulation.GetEffectiveCriticalChance(unit.Id)), "核心发现",
                FormatAdjustedPercent(unit.Stats.CoreDiscovery, _simulation.GetEffectiveCoreDiscovery(unit.Id)));
            DrawDetailStatRow(ref y, contentWidth, "屏障", unit.Stats.ScreenPower.ToString("0.##"), "穿线", unit.Stats.ScreenPenetration.ToString("P0"));
            y += 4f;
            GUI.Label(new Rect(14f, y, contentWidth, 20f), $"特质 · {TraitName(unit.Stats.Traits)}", _detailHeaderStyle);
            y += 22f;
            GUI.Label(new Rect(14f, y, contentWidth, 48f), TraitDescription(unit.Stats.Traits, _balance), _detailMutedStyle);
            y += 51f;
            GUI.Label(new Rect(14f, y, contentWidth, 42f), RoleDescription(unit), _detailMutedStyle);

            GUI.EndScrollView();
            GUI.EndGroup();
        }

        private void DrawDetailBar(Rect rect, float ratio, Color color, string text)
        {
            Color previous = GUI.color;
            GUI.color = new Color(0.025f, 0.04f, 0.05f, 0.9f);
            GUI.DrawTexture(rect, Texture2D.whiteTexture);
            GUI.color = color;
            GUI.DrawTexture(new Rect(rect.x, rect.y, rect.width * Mathf.Clamp01(ratio), rect.height), Texture2D.whiteTexture);
            GUI.color = previous;
            GUI.Label(rect, text, _centerStyle);
        }

        private void DrawDetailStatRow(ref float y, float contentWidth, string leftName, string leftValue, string rightName, string rightValue)
        {
            float half = contentWidth * 0.5f;
            GUI.Label(new Rect(14f, y, half, 20f), $"{leftName}  {leftValue}", _smallStyle);
            GUI.Label(new Rect(14f + half, y, half, 20f), $"{rightName}  {rightValue}", _smallStyle);
            y += 21f;
        }

        private static string FormatAdjustedPercent(float baseValue, float effectiveValue)
        {
            return Mathf.Approximately(baseValue, effectiveValue)
                ? baseValue.ToString("P0")
                : $"{effectiveValue:P0}（基础 {baseValue:P0}）";
        }

        private static string FormatAdjustedSeconds(float baseValue, float effectiveValue)
        {
            return Mathf.Approximately(baseValue, effectiveValue)
                ? $"{baseValue:0.#}s"
                : $"{effectiveValue:0.#}s（基础 {baseValue:0.#}）";
        }

        private string BuildCurrentActionText(DemoUnitModel unit)
        {
            DemoCombatModel combat = unit.CombatId >= 0 ? _simulation.GetCombat(unit.CombatId) : null;
            DemoCombatParticipantState assignment = combat?.GetAssignment(unit.Id);
            if (assignment?.IsRepositioning == true)
                return $"换位中：前往{Demo1Simulation.BattleLineName(assignment.TargetLine)}，剩余 {assignment.RepositionRemaining:0.0}s";
            if (unit.Activity == DemoUnitActivity.Retreating)
                return $"撤退中：完成度 {unit.RetreatProgress:P0}，剩余 {unit.RetreatRemaining:0.0}s";
            if (unit.Activity == DemoUnitActivity.Reinforcing)
                return $"增援战斗 #{unit.PendingReinforcementBattleId}，目的地 ({unit.Destination.x:0.0}, {unit.Destination.z:0.0})";
            if (unit.HasDestination)
                return $"{ActivityName(unit.Activity)}：前往 ({unit.Destination.x:0.0}, {unit.Destination.z:0.0})";
            if (unit.Activity == DemoUnitActivity.Fighting && combat != null)
                return $"战斗中：战斗 #{combat.Id}";
            return $"{ActivityName(unit.Activity)}  ·  位置 ({unit.Position.x:0.0}, {unit.Position.z:0.0})";
        }

        private string BuildFormationText(DemoUnitModel unit)
        {
            DemoCombatModel combat = unit.CombatId >= 0 ? _simulation.GetCombat(unit.CombatId) : null;
            DemoCombatParticipantState assignment = combat?.GetAssignment(unit.Id);
            if (assignment == null)
                return $"默认阵位：{Demo1Simulation.BattleLineName(unit.Stats.PreferredBattleLine)}\n当前未加入战斗";

            string targetName = "暂无目标";
            DemoUnitModel target = assignment.LastTargetId >= 0 ? _simulation.GetUnit(assignment.LastTargetId) : null;
            if (target != null)
                targetName = $"最近目标：{target.DisplayName}";
            string line = assignment.IsRepositioning
                ? $"{Demo1Simulation.BattleLineName(assignment.Line)} → {Demo1Simulation.BattleLineName(assignment.TargetLine)}"
                : Demo1Simulation.BattleLineName(assignment.Line);
            return $"战斗 #{combat.Id}  ·  {line}\n{targetName}";
        }

        private bool IsCharacterDetailVisible(out DemoUnitModel unit)
        {
            unit = null;
            if (_simulation == null || _selection.Count != 1 || (_battleUi != null && _battleUi.IsPanelOpen))
                return false;
            unit = _simulation.GetUnit(_selection.First());
            return unit != null && unit.IsAlive && unit.Team == DemoTeam.Player;
        }

        private static Rect GetCharacterDetailRect()
        {
            float width = Mathf.Clamp(Screen.width * 0.22f, 280f, 340f);
            float height = Mathf.Clamp(Screen.height - 64f, 360f, 660f);
            return new Rect(Screen.width - width - 12f, 54f, width, height);
        }

        private void DrawWorldLabels()
        {
            foreach (DemoUnitModel unit in _simulation.Units)
            {
                if (!unit.IsAlive || (unit.Team == DemoTeam.Enemy && !unit.HasPlayerIntel))
                    continue;
                Vector3 displayPosition = unit.Team == DemoTeam.Enemy ? unit.PlayerVisiblePosition : unit.Position;
                Vector3 screen = _camera.WorldToScreenPoint(displayPosition + Vector3.up * (unit.IsFixed ? 2.8f : 2f));
                if (screen.z <= 0f)
                    continue;
                float y = Screen.height - screen.y;
                string label = unit.DisplayName;
                if (unit.Team == DemoTeam.Enemy && unit.PlayerIntelLevel == DemoIntelLevel.Contact)
                    label = $"不明接触 · {Mathf.Max(0f, _simulation.SimulationTime - unit.LastObservedAt):0.0}s";
                else if (unit.Team == DemoTeam.Enemy && !unit.IsCurrentlyObservedByPlayer && !unit.HasPersistentPlayerIntel)
                    label += $" · 最后发现 {Mathf.Max(0f, _simulation.SimulationTime - unit.LastObservedAt):0.0}s";
                GUI.Label(new Rect(screen.x - 100f, y - 14f, 200f, 20f), label, _worldLabelStyle);
                if (unit.Team == DemoTeam.Player || unit.PlayerIntelLevel == DemoIntelLevel.Assessed)
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
            _detailHeaderStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 14,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleLeft,
                normal = { textColor = new Color(0.62f, 0.88f, 0.95f) }
            };
            _detailValueStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 18,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleLeft
            };
            _detailMutedStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 12,
                wordWrap = true,
                alignment = TextAnchor.UpperLeft,
                normal = { textColor = new Color(0.67f, 0.74f, 0.76f) }
            };
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
                .FirstOrDefault(view =>
                {
                    DemoUnitModel unit = view == null ? null : _simulation.GetUnit(view.UnitId);
                    return unit != null && unit.IsAlive && (unit.Team == DemoTeam.Player || unit.HasPlayerIntel);
                });
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
            if (mouse.x <= PanelWidth || mouse.y >= Screen.height - 44f)
                return true;
            Vector2 guiMouse = new Vector2(mouse.x, Screen.height - mouse.y);
            return IsCharacterDetailVisible(out _) && GetCharacterDetailRect().Contains(guiMouse);
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
            Scene activeScene = SceneManager.GetActiveScene();
            if (activeScene.buildIndex >= 0)
                SceneManager.LoadScene(activeScene.buildIndex);
            else if (!string.IsNullOrEmpty(activeScene.path))
                SceneManager.LoadScene(activeScene.path);
            else
                _statusMessage = "无法重新载入当前场景，请停止并重新进入播放模式";
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

        private static string VisionTypeName(DemoWitchVisionType type)
        {
            switch (type)
            {
                case DemoWitchVisionType.Ordinary: return "普通";
                case DemoWitchVisionType.Night: return "夜战";
                default: return "无视野";
            }
        }

        private static string RoleName(DemoUnitRole role)
        {
            switch (role)
            {
                case DemoUnitRole.Witch: return "战斗魔女";
                case DemoUnitRole.Support: return "支援魔女";
                case DemoUnitRole.Artillery: return "炮击魔女";
                case DemoUnitRole.Scout: return "侦察体";
                case DemoUnitRole.Guard: return "护卫";
                case DemoUnitRole.Fortress: return "固定目标";
                default: return role.ToString();
            }
        }

        private static string RoleDescription(DemoUnitModel unit)
        {
            if (unit.Stats.HasTrait(DemoUnitTrait.LynetteSharpshooter))
                return "作战方式：使用不能穿线的标准单体攻击，不发动炮击校射齐射。";
            switch (unit.Role)
            {
                case DemoUnitRole.Witch: return "角色特性：优先压制当前暴露阵线中的穿线威胁。";
                case DemoUnitRole.Support: return "角色特性：在支援线周期性恢复友军护盾与魔力。";
                case DemoUnitRole.Artillery: return "角色特性：每三次攻击发动一次校准齐射，并可尝试穿线。";
                case DemoUnitRole.Scout: return "角色特性：命中后标记目标，使其承受更多伤害。";
                case DemoUnitRole.Guard: return "角色特性：在前卫线拦截敌方穿线攻击。";
                case DemoUnitRole.Fortress: return "角色特性：低生命时进入紧急弹幕状态。";
                default: return string.Empty;
            }
        }

        private static string TraitName(DemoUnitTrait traits)
        {
            List<string> names = new List<string>();
            if ((traits & DemoUnitTrait.SakamotoCoreInsight) != 0) names.Add("魔眼指挥");
            if ((traits & DemoUnitTrait.MiyafujiShieldAura) != 0) names.Add("守护之心");
            if ((traits & DemoUnitTrait.LynetteSharpshooter) != 0) names.Add("精密射手");
            return names.Count == 0 ? "无" : string.Join(" / ", names);
        }

        private static string TraitDescription(DemoUnitTrait traits, Demo1Balance balance)
        {
            List<string> descriptions = new List<string>();
            if ((traits & DemoUnitTrait.SakamotoCoreInsight) != 0)
                descriptions.Add($"同场全体友军（含自身）的基础核心发现提高 {balance.SakamotoCoreDiscoveryBonus:P0}。");
            if ((traits & DemoUnitTrait.MiyafujiShieldAura) != 0)
                descriptions.Add($"同场全体友军（含自身）的护盾吸收效率提高 {balance.MiyafujiShieldEfficiencyBonus:P0}。");
            if ((traits & DemoUnitTrait.LynetteSharpshooter) != 0)
                descriptions.Add($"暴击率提高 {balance.LynetteCriticalChanceBonus:P0}，攻击间隔变为 {balance.LynetteAttackIntervalMultiplier:0.###} 倍。");
            return descriptions.Count == 0 ? "该单位目前没有个人特质。" : string.Join("\n", descriptions);
        }
    }
}
