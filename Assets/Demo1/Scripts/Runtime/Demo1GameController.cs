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
            RemoteStrike,
            AbilityTarget,
            SupplyDrop
        }

        private sealed class StrikeVisual
        {
            public GameObject Root;
            public LineRenderer Radius;
        }

        private sealed class ProjectileVisual
        {
            public GameObject Root;
            public LineRenderer Trail;
        }

        private sealed class SupplyVisual
        {
            public GameObject Root;
            public GameObject Marker;
            public LineRenderer Radius;
        }

        private readonly Dictionary<int, Demo1UnitView> _unitViews = new Dictionary<int, Demo1UnitView>();
        private readonly Dictionary<DemoUnitConfig, int> _configuredUnitIds = new Dictionary<DemoUnitConfig, int>();
        private readonly Dictionary<int, StrikeVisual> _strikeViews = new Dictionary<int, StrikeVisual>();
        private readonly Dictionary<int, ProjectileVisual> _projectileViews = new Dictionary<int, ProjectileVisual>();
        private readonly Dictionary<int, SupplyVisual> _supplyViews = new Dictionary<int, SupplyVisual>();
        private readonly HashSet<int> _selection = new HashSet<int>();
        private readonly Dictionary<int, List<int>> _controlGroups = new Dictionary<int, List<int>>();
        private readonly List<DemoBattleEvent> _events = new List<DemoBattleEvent>();

        private Demo1Simulation _simulation;
        private Demo1Balance _balance;
        [SerializeField] private Demo1BalanceConfig _balanceConfig;
        [SerializeField] private DemoUnitConfig[] _unitConfigs;
        [SerializeField] private DemoLevelConfig[] _levelConfigs;
        private Texture2D _operationalMapTexture;
        private DemoUnitConfig[] _sortieWitches;
        private Vector2 _operationalMapSizeKilometers;
        private Vector2 _operationalStartNormalized = new Vector2(0.835f, 0.82f);
        private bool _baseCommandMode;
        private DemoLevelConfig _activeLevel;
        private Camera _camera;
        private Demo1CameraController _cameraController;
        private Demo1LevelSelector _levelSelector;
        private Demo1RangeOverlay _rangeOverlay;
        private CommandMode _commandMode;
        private int _abilitySourceId = -1;
        private Vector2 _dragStart;
        private bool _dragSelecting;
        private bool _paused;
        private string _statusMessage = "左键选择，右键移动；先靠近已发现目标再交战。";
        private GUIStyle _titleStyle;
        private GUIStyle _smallStyle;
        private GUIStyle _centerStyle;
        private GUIStyle _worldLabelStyle;
        private GUIStyle _detailHeaderStyle;
        private GUIStyle _detailValueStyle;
        private GUIStyle _detailMutedStyle;
        private GUIStyle _hudButtonStyle;
        private GUIStyle _hudPrimaryButtonStyle;
        private GUIStyle _hudDangerButtonStyle;
        private GUIStyle _eventButtonStyle;
        private GUIStyle _unitCardStyle;
        private GUIStyle _unitNameStyle;
        private GUIStyle _tagStyle;
        private GUIStyle _barTextStyle;
        private Vector2 _detailScroll;
        private Texture2D _selectionTexture;
        private Texture2D _hudCardTexture;
        private Texture2D _hudSectionTexture;
        private const float PanelWidth = 340f;
        private const float TopBarHeight = 52f;
        private static readonly Color HudPanelColor = new Color(0.025f, 0.045f, 0.062f, 1f);
        private static readonly Color HudCardColor = new Color(0.055f, 0.09f, 0.115f, 0.98f);
        private static readonly Color HudSectionColor = new Color(0.075f, 0.13f, 0.16f, 0.98f);
        private static readonly Color PlayerAccent = new Color(0.18f, 0.78f, 0.96f, 1f);
        private static readonly Color EnemyAccent = new Color(0.95f, 0.28f, 0.25f, 1f);
        private static readonly Color WarningAccent = new Color(1f, 0.66f, 0.2f, 1f);
        private const string BalanceResourcePath = "Configs/Demo1Balance";
        private const string UnitResourcePath = "Configs/Units";
        private const string LevelResourcePath = "Configs/Levels";
        private static string s_requestedLevelId;

        public Demo1Simulation Simulation => _simulation;
        public IReadOnlyCollection<int> SelectedUnitIds => _selection;
        public bool IsPaused => _paused;
        public string StatusMessage => _statusMessage;
        public IReadOnlyList<DemoBattleEvent> BattleEvents => _events;
        public Camera BattleCamera => _camera;
        public int CharacterDetailUnitId => IsCharacterDetailVisible(out DemoUnitModel unit) ? unit.Id : -1;
        public int LevelCount => _levelConfigs?.Length ?? 0;
        public int ActiveLevelIndex => _activeLevel == null || _levelConfigs == null ? -1 : System.Array.IndexOf(_levelConfigs, _activeLevel);
        public string ActiveLevelName => _activeLevel != null ? _activeLevel.DisplayName : "Demo 1.0";
        public string ActiveMissionText => _activeLevel != null ? _activeLevel.MissionText : "摧毁东侧异形军巢穴。";
        public string ActiveVictoryText => _activeLevel != null ? _activeLevel.VictoryText : "敌方固定目标已被摧毁。";
        public string ActiveDefeatText => _activeLevel != null ? _activeLevel.DefeatText : "我方已无可继续作战的单位。";
        public bool IsInitialized => _simulation != null;
        public Vector3 BasePosition => _simulation != null
            ? _simulation.BasePosition
            : (_activeLevel != null ? _activeLevel.BasePosition : new Vector3(187.6f, 0f, 100.8f));

        public void ConfigureOperationalLevel(Texture2D mapTexture, IEnumerable<DemoUnitConfig> sortieWitches,
            Vector2 mapSizeKilometers, Vector2 startNormalized)
        {
            _baseCommandMode = true;
            _operationalMapTexture = mapTexture;
            _sortieWitches = sortieWitches?
                .Where(config => config != null && config.Team == DemoTeam.Player)
                .Distinct()
                .OrderBy(config => config.SpawnOrder)
                .ToArray();
            _operationalMapSizeKilometers = mapSizeKilometers;
            _operationalStartNormalized = startNormalized;
        }

        private void Start()
        {
            LoadScenarioConfigs();
            _balance = _balanceConfig != null ? _balanceConfig.CreateRuntimeValue() : new Demo1Balance();
            if (_operationalMapSizeKilometers.x > 0f && _operationalMapSizeKilometers.y > 0f)
            {
                _balance.MapHalfWidth = _operationalMapSizeKilometers.x * 0.5f;
                _balance.MapHalfHeight = _operationalMapSizeKilometers.y * 0.5f;
                _balance.MapKilometersPerUnit = 1f;
            }
            _simulation = new Demo1Simulation(_balance);
            _simulation.ConfigureBase(_activeLevel != null
                ? _activeLevel.BasePosition
                : NormalizedMapPosition(_operationalStartNormalized));
            _simulation.ConfigureMissionObjective(_activeLevel != null
                ? _activeLevel.MissionObjective
                : DemoMissionObjective.DestroyFortress);
            _simulation.EventRaised += OnBattleEvent;
            BuildEnvironment();
            BuildCamera();
            BuildRangeOverlay();
            CreateScenario();
            if (_sortieWitches != null && _sortieWitches.Length > 0)
                RequestSortie(_sortieWitches);
            BuildLevelSelector();
            SyncViews();
        }

        private void LoadScenarioConfigs()
        {
            if (_levelConfigs == null || _levelConfigs.Length == 0)
            {
                _levelConfigs = Resources.LoadAll<DemoLevelConfig>(LevelResourcePath)
                    .Where(config => config != null)
                    .OrderBy(config => config.SortOrder)
                    .ThenBy(config => config.LevelId)
                    .ToArray();
            }
            else
            {
                _levelConfigs = _levelConfigs
                    .Where(config => config != null)
                    .OrderBy(config => config.SortOrder)
                    .ThenBy(config => config.LevelId)
                    .ToArray();
            }

            if (_levelConfigs.Length > 0)
            {
                _activeLevel = _levelConfigs.FirstOrDefault(config => config.LevelId == s_requestedLevelId)
                    ?? _levelConfigs.FirstOrDefault(config => config.IsDefault)
                    ?? _levelConfigs[0];
                s_requestedLevelId = _activeLevel.LevelId;
                if (_activeLevel.Balance != null)
                    _balanceConfig = _activeLevel.Balance;
                if (_activeLevel.Units != null && _activeLevel.Units.Length > 0)
                    _unitConfigs = _activeLevel.Units;
            }

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
            ground.GetComponent<Renderer>().sharedMaterial = _operationalMapTexture != null
                ? Demo1Drawing.CreateMapMaterial(_operationalMapTexture, Color.white)
                : Demo1Drawing.CreateMaterial(new Color(0.075f, 0.12f, 0.14f));

            GameObject gridRoot = new GameObject("Map Grid");
            gridRoot.transform.SetParent(transform);
            for (int x = -(int)_balance.MapHalfWidth; x <= _balance.MapHalfWidth; x += 10)
                CreateGridLine(gridRoot.transform, new Vector3(x, 0.025f, -_balance.MapHalfHeight), new Vector3(x, 0.025f, _balance.MapHalfHeight));
            for (int z = -(int)_balance.MapHalfHeight; z <= _balance.MapHalfHeight; z += 10)
                CreateGridLine(gridRoot.transform, new Vector3(-_balance.MapHalfWidth, 0.025f, z), new Vector3(_balance.MapHalfWidth, 0.025f, z));

            if (_operationalMapTexture == null)
            {
                CreateLandmark("Dover", new Vector3(-33f, 0.3f, -20f), new Vector3(6f, 0.6f, 6f), new Color(0.18f, 0.26f, 0.28f));
                CreateLandmark("Calais", new Vector3(31f, 0.3f, 20f), new Vector3(7f, 0.6f, 5f), new Color(0.2f, 0.22f, 0.25f));
            }

            GameObject lightObject = new GameObject("Directional Light");
            lightObject.transform.SetParent(transform);
            Light light = lightObject.AddComponent<Light>();
            light.type = LightType.Directional;
            light.intensity = 1.25f;
            lightObject.transform.rotation = Quaternion.Euler(50f, -35f, 0f);

            if (_baseCommandMode)
                CreateLandmark("501 Base - Folkestone", _simulation.BasePosition + new Vector3(0f, 0.3f, 0f),
                    new Vector3(2.4f, 0.8f, 2.4f), new Color(0.12f, 0.68f, 0.72f));
        }

        private void BuildCamera()
        {
            _camera = Camera.main;
            GameObject cameraObject;
            if (_camera == null)
            {
                cameraObject = new GameObject("Demo1 Camera");
                cameraObject.tag = "MainCamera";
                cameraObject.transform.SetParent(transform);
                _camera = cameraObject.AddComponent<Camera>();
            }
            else
            {
                cameraObject = _camera.gameObject;
                cameraObject.name = "Demo1 Camera";
            }
            Vector3 cameraCenter = _baseCommandMode ? Vector3.zero :
                (_operationalMapTexture != null ? NormalizedMapPosition(_operationalStartNormalized) : GetScenarioCameraCenter());
            cameraObject.transform.position = new Vector3(cameraCenter.x, 50f, cameraCenter.z);
            cameraObject.transform.rotation = Quaternion.Euler(90f, 0f, 0f);
            _camera.orthographic = true;
            _camera.orthographicSize = _baseCommandMode ? _balance.MapHalfHeight : 24f;
            _camera.nearClipPlane = 0.1f;
            _camera.farClipPlane = 100f;
            _camera.backgroundColor = new Color(0.025f, 0.04f, 0.055f);
            _camera.clearFlags = CameraClearFlags.SolidColor;
            if (cameraObject.GetComponent<AudioListener>() == null)
                cameraObject.AddComponent<AudioListener>();
            _cameraController = cameraObject.GetComponent<Demo1CameraController>();
            if (_cameraController == null)
                _cameraController = cameraObject.AddComponent<Demo1CameraController>();
            _cameraController.MapHalfExtents = new Vector2(_balance.MapHalfWidth, _balance.MapHalfHeight);
            if (_baseCommandMode)
                _cameraController.ConfigureFullTheatre();
        }

        private void BuildRangeOverlay()
        {
            GameObject overlayObject = new GameObject("Selected Range Overlay");
            overlayObject.transform.SetParent(transform, false);
            _rangeOverlay = overlayObject.AddComponent<Demo1RangeOverlay>();
        }

        private Vector3 NormalizedMapPosition(Vector2 normalized)
        {
            return new Vector3(
                Mathf.Lerp(-_balance.MapHalfWidth, _balance.MapHalfWidth, Mathf.Clamp01(normalized.x)),
                0f,
                Mathf.Lerp(-_balance.MapHalfHeight, _balance.MapHalfHeight, Mathf.Clamp01(normalized.y)));
        }

        private Vector3 GetScenarioCameraCenter()
        {
            DemoUnitConfig[] players = _unitConfigs?
                .Where(config => config != null && config.Team == DemoTeam.Player)
                .ToArray();
            if (players == null || players.Length == 0)
                return Vector3.zero;

            Vector3 center = Vector3.zero;
            foreach (DemoUnitConfig config in players)
                center += _activeLevel != null ? _activeLevel.GetSpawnPosition(config) : config.StartingPosition;
            center /= players.Length;
            center.y = 0f;
            return center;
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
                bool startsAtBase = _baseCommandMode && config.Team == DemoTeam.Player;
                Vector3 startingPosition = startsAtBase
                    ? _simulation.BasePosition
                    : (_activeLevel != null ? _activeLevel.GetSpawnPosition(config) : config.StartingPosition);
                DemoUnitStats runtimeStats = _activeLevel != null
                    ? _activeLevel.CreateRuntimeStats(config, _balance)
                    : config.CreateRuntimeStats(_balance);
                DemoUnitModel unit = AddUnit(config.DisplayName, config.Team, config.Role,
                    runtimeStats, startingPosition, startsAtBase
                        ? DemoUnitDeploymentState.Standby
                        : DemoUnitDeploymentState.Active);
                _configuredUnitIds[config] = unit.Id;
                spawned.Add(new KeyValuePair<DemoUnitConfig, DemoUnitModel>(config, unit));
                if (config.GrantPersistentPlayerIntel)
                    _simulation.GrantPersistentPlayerIntel(unit.Id);
            }

            foreach (KeyValuePair<DemoUnitConfig, DemoUnitModel> item in spawned)
            {
                switch (item.Key.EnemyAiProfile)
                {
                    case DemoEnemyAiProfile.Scout:
                        Vector3 scoutOffset = _activeLevel != null ? _activeLevel.GetTeamOffset(item.Key.Team) : Vector3.zero;
                        _simulation.ConfigureScoutAi(item.Value.Id, item.Key.ScoutPatrolPoints.Select(point => point + scoutOffset));
                        break;
                    case DemoEnemyAiProfile.Combat:
                        Vector3 homePosition = item.Key.UseStartingPositionAsAiHome
                            ? item.Value.Position
                            : item.Key.EnemyAiHomePosition + (_activeLevel != null ? _activeLevel.GetTeamOffset(item.Key.Team) : Vector3.zero);
                        _simulation.ConfigureCombatAi(item.Value.Id, homePosition);
                        break;
                }
            }

            FinishScenarioSetup();
        }

        private void CreateFallbackScenario()
        {
            DemoUnitStats witch = new DemoUnitStats();
            witch.MaxHealth = 188f;
            witch.Attack = 38f;
            witch.Defense = 12f;
            witch.MaxMagic = 121f;
            witch.MaxShield = 74f;
            witch.MagicRecovery = 5f;
            witch.AttackRange = 8f;
            witch.BaseAccuracy = 0.72f;
            witch.Penetration = 16f;
            witch.MagazineSize = 8;
            witch.ReserveAmmo = 32;
            witch.AmmoPerAttack = 1;
            witch.ReloadDuration = 3f;
            witch.WitchVisionType = DemoWitchVisionType.Ordinary;
            witch.VisionRadius = 24f;
            witch.VisionAngle = 100f;
            DemoUnitStats ace = witch.Clone();
            ace.MaxHealth = 221f;
            ace.Attack = 44f;
            ace.CoreDiscovery = 0.28f;
            ace.Mobility = 1.25f;
            ace.SupportRadius = 0f;
            ace.Traits = DemoUnitTrait.None;
            ace.SpecialAbility = DemoSpecialAbility.MagicEyeSearch;
            ace.AbilityMagicCost = 30f;
            ace.AbilityCooldown = 20f;
            ace.AbilityRange = 36f;
            ace.AbilityDuration = 6f;
            ace.AbilityArcAngle = 45f;

            DemoUnitStats support = witch.Clone();
            support.MaxHealth = 181f;
            support.Attack = 27f;
            support.Defense = 11f;
            support.MaxMagic = 181f;
            support.MaxShield = 96f;
            support.GlobalShieldBonus = 0f;
            support.MagicRecovery = 9f;
            support.SupportRadius = 0f;
            support.Traits = DemoUnitTrait.None;
            support.SpecialAbility = DemoSpecialAbility.Heal;
            support.AbilityMagicCost = 15f;
            support.AbilityCooldown = 10f;
            support.AbilityRange = 6f;
            support.AbilityDuration = 3f;
            support.AbilityValue = 0.12f;

            DemoUnitStats artillery = witch.Clone();
            artillery.MaxHealth = 174f;
            artillery.Attack = 36f;
            artillery.Defense = 11f;
            artillery.MaxShield = 67f;
            artillery.CanRemoteStrike = false;
            artillery.CoreDiscovery = 0.22f;
            artillery.Traits = DemoUnitTrait.None;
            artillery.MagazineSize = 5;
            artillery.ReserveAmmo = 20;
            artillery.ReloadDuration = 4f;
            artillery.AttackInterval = 2.2f;
            artillery.AttackRange = 12f;
            artillery.Penetration = 24f;
            artillery.SpecialAbility = DemoSpecialAbility.None;
            artillery.PassiveAbility = DemoPassiveAbility.FireControlSolution;
            artillery.PassiveActivationDelay = 3f;
            artillery.PassiveAttackRange = 48f;
            artillery.PassiveDamageMultiplier = 2f;
            artillery.PassivePenetration = 32f;
            artillery.PassiveMinimumAccuracy = 0.85f;

            DemoUnitStats nightWitch = witch.Clone();
            nightWitch.MaxHealth = 181f;
            nightWitch.MaxMagic = 168f;
            nightWitch.MaxShield = 91f;
            nightWitch.Attack = 35f;
            nightWitch.Defense = 11f;
            nightWitch.MagicRecovery = 7f;
            nightWitch.CoreDiscovery = 0.24f;
            nightWitch.WitchVisionType = DemoWitchVisionType.Night;
            nightWitch.VisionRadius = 72f;
            nightWitch.VisionAngle = 360f;
            nightWitch.MagazineSize = 1;
            nightWitch.ReserveAmmo = 24;
            nightWitch.ReloadDuration = 5f;
            nightWitch.AttackInterval = 5f;
            nightWitch.AttackRange = 72f;
            nightWitch.ExplosiveRadius = 2.5f;
            nightWitch.Penetration = 20f;
            nightWitch.ProjectileSpeed = 12f;
            nightWitch.ProjectileTurnRate = 120f;
            nightWitch.ProjectileLifetime = 10f;
            nightWitch.ProjectileContactRadius = 0.5f;

            DemoUnitStats perrine = witch.Clone();
            perrine.SpecialAbility = DemoSpecialAbility.LightningStrike;
            perrine.AbilityMagicCost = 40f;
            perrine.AbilityCooldown = 14f;
            perrine.AbilityRadius = 5f;
            perrine.AbilityDamageMultiplier = 2f;
            perrine.AbilityPenetration = perrine.Penetration;
            perrine.AbilitySuppression = 35f;

            DemoUnitStats neuroi = witch.Clone();
            neuroi.WitchVisionType = DemoWitchVisionType.None;
            neuroi.Mobility = 0.65f;
            neuroi.MaxHealth = 190f;
            neuroi.Attack = 26f;
            neuroi.Defense = 10f;
            neuroi.MaxMagic = 110f;
            neuroi.MaxShield = 70f;
            neuroi.CoreConcealment = 0.65f;
            neuroi.MoveSpeed = 4.8f;
            neuroi.AttackRange = 7.5f;
            neuroi.BaseAccuracy = 0.68f;
            neuroi.Penetration = 18f;
            neuroi.Armor = 15f;
            neuroi.UnlimitedReserveAmmo = true;

            DemoUnitStats guard = neuroi.Clone();
            guard.MaxHealth = 260f;
            guard.Defense = 13f;
            guard.Attack = 29f;
            guard.MaxMagic = 120f;
            guard.MaxShield = 100f;
            guard.MoveSpeed = 4.2f;

            DemoUnitStats fortress = neuroi.Clone();
            fortress.MaxHealth = 720f;
            fortress.Attack = 42f;
            fortress.Defense = 16f;
            fortress.MaxMagic = 300f;
            fortress.MaxShield = 260f;
            fortress.CoreConcealment = 0.9f;
            fortress.AttackInterval = 2.2f;
            fortress.AttackRange = 10f;
            fortress.VisionRadius = 26f;
            fortress.MoveSpeed = 0f;

            DemoUnitDeploymentState fallbackDeployment = _baseCommandMode
                ? DemoUnitDeploymentState.Standby
                : DemoUnitDeploymentState.Active;
            AddUnit("宫藤芳佳", DemoTeam.Player, DemoUnitRole.Support, support,
                _baseCommandMode ? _simulation.BasePosition : new Vector3(-27f, 0f, -10f), fallbackDeployment);
            AddUnit("坂本美绪", DemoTeam.Player, DemoUnitRole.Witch, ace,
                _baseCommandMode ? _simulation.BasePosition : new Vector3(-25f, 0f, -3f), fallbackDeployment);
            AddUnit("莉涅特", DemoTeam.Player, DemoUnitRole.Artillery, artillery,
                _baseCommandMode ? _simulation.BasePosition : new Vector3(-27f, 0f, 5f), fallbackDeployment);
            AddUnit("佩琳", DemoTeam.Player, DemoUnitRole.Witch, perrine,
                _baseCommandMode ? _simulation.BasePosition : new Vector3(-23f, 0f, 12f), fallbackDeployment);
            AddUnit("桑妮亚·V·利特维亚克", DemoTeam.Player, DemoUnitRole.Witch, nightWitch,
                _baseCommandMode ? _simulation.BasePosition : new Vector3(-32f, 0f, 12f), fallbackDeployment);

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
            _statusMessage = _baseCommandMode
                ? "敌军活动已经开始。点击福克斯通 501 基地编组出动。"
                : "全队已选中：右键地面移动，右键红色敌人会自动接近并开战。";
        }

        public void SelectAllPlayerUnits()
        {
            if (_simulation == null)
                return;

            _selection.Clear();
            foreach (DemoUnitModel unit in _simulation.Units.Where(unit => unit.Team == DemoTeam.Player && unit.IsAlive && unit.IsOperational))
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
                if (unit != null && unit.IsAlive && unit.IsOperational && unit.Team == DemoTeam.Player)
                    _selection.Add(id);
            }
        }

        public DemoUnitModel GetConfiguredUnit(DemoUnitConfig config)
        {
            return config != null && _simulation != null && _configuredUnitIds.TryGetValue(config, out int id)
                ? _simulation.GetUnit(id)
                : null;
        }

        public DemoCommandResult RequestSortie(IEnumerable<DemoUnitConfig> configs)
        {
            if (_simulation == null)
                return DemoCommandResult.Fail("Operational map is still initializing.");
            int[] ids = (configs ?? Enumerable.Empty<DemoUnitConfig>())
                .Select(GetConfiguredUnit)
                .Where(unit => unit != null)
                .Select(unit => unit.Id)
                .ToArray();
            DemoCommandResult result = _simulation.RequestSortie(ids);
            if (result.Success)
                SelectUnits(ids);
            ApplyResult(result);
            return result;
        }

        public DemoCommandResult CommandReturnToBase(IEnumerable<int> unitIds)
        {
            DemoCommandResult result = _simulation == null
                ? DemoCommandResult.Fail("Operational map is still initializing.")
                : _simulation.RequestReturnToBase(unitIds);
            ApplyResult(result);
            return result;
        }

        public DemoCommandResult CommandMove(Vector3 destination)
        {
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
            _commandMode = CommandMode.Select;
            return ApplyAndReturn(_simulation.RequestAttack(_selection, targetId));
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

        public DemoCommandResult CommandSupplyDrop(Vector3 target)
        {
            if (_simulation == null)
                return ApplyAndReturn(DemoCommandResult.Fail("战场尚未初始化"));
            _commandMode = CommandMode.Select;
            return ApplyAndReturn(_simulation.RequestSupplyDrop(target));
        }

        public DemoCommandResult CommandFieldResupply(IEnumerable<int> unitIds)
        {
            DemoCommandResult result = _simulation == null
                ? DemoCommandResult.Fail("战场尚未初始化")
                : _simulation.RequestFieldResupply(unitIds);
            ApplyResult(result);
            return result;
        }

        private void EnterSupplyDropMode()
        {
            if (_simulation == null)
                return;
            if (_simulation.SupplyPackagesRemaining <= 0)
            {
                _commandMode = CommandMode.Select;
                _statusMessage = "战术补给包已耗尽";
                return;
            }
            if (_simulation.SupplyCallCooldownRemaining > 0f)
            {
                _commandMode = CommandMode.Select;
                _statusMessage = $"补给投放冷却中：{_simulation.SupplyCallCooldownRemaining:0.0}s";
                return;
            }
            _commandMode = CommandMode.SupplyDrop;
            _statusMessage = $"补给投放：左键指定基地 {_simulation.Balance.SupplyCallRange:0} 范围内的位置";
        }

        public DemoCommandResult CommandSpecialAbility(int targetId = -1)
        {
            if (_simulation == null)
                return ApplyAndReturn(DemoCommandResult.Fail("战场尚未初始化"));
            DemoUnitModel caster = _selection.Count == 1 ? _simulation.GetUnit(_selection.First()) : null;
            if (caster == null || caster.Team != DemoTeam.Player)
                return ApplyAndReturn(DemoCommandResult.Fail("主动技能需要单选一名魔女"));
            _commandMode = CommandMode.Select;
            _abilitySourceId = -1;
            return ApplyAndReturn(_simulation.RequestSpecialAbility(caster.Id, targetId));
        }

        private void EnterSpecialAbilityMode()
        {
            DemoUnitModel caster = _simulation != null && _selection.Count == 1
                ? _simulation.GetUnit(_selection.First())
                : null;
            if (caster == null || caster.Team != DemoTeam.Player || caster.Stats.SpecialAbility == DemoSpecialAbility.None)
            {
                _statusMessage = "主动技能需要单选具有技能的魔女";
                return;
            }
            if (caster.Stats.SpecialAbility == DemoSpecialAbility.MagicEyeSearch ||
                caster.Stats.SpecialAbility == DemoSpecialAbility.LightningStrike)
            {
                CommandSpecialAbility();
                return;
            }
            _abilitySourceId = caster.Id;
            _commandMode = CommandMode.AbilityTarget;
            _statusMessage = caster.Stats.SpecialAbility == DemoSpecialAbility.Heal
                ? "治疗：点击范围内另一名友军"
                : "射击诸元装订：点击已评估或核心标记的敌人";
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

        public DemoCommandResult CommandSetAutoAttack(bool enabled)
        {
            DemoCommandResult result = _simulation.SetAutoAttack(_selection, enabled);
            ApplyResult(result);
            return result;
        }

        private void BuildLevelSelector()
        {
            if (LevelCount == 0)
                return;
            GameObject selectorObject = new GameObject("Demo1 Level Selector UI");
            selectorObject.transform.SetParent(transform, false);
            _levelSelector = selectorObject.AddComponent<Demo1LevelSelector>();
            _levelSelector.Initialize(this);
        }

        public string GetLevelName(int index)
        {
            return index >= 0 && index < LevelCount && _levelConfigs[index] != null
                ? _levelConfigs[index].DisplayName
                : "未知关卡";
        }

        public void RequestLevelLoad(int index)
        {
            if (index < 0 || index >= LevelCount || _levelConfigs[index] == null)
            {
                _statusMessage = "无法载入关卡：选择无效。";
                return;
            }

            s_requestedLevelId = _levelConfigs[index].LevelId;
            RestartScene();
        }

        private DemoUnitModel AddUnit(string name, DemoTeam team, DemoUnitRole role, DemoUnitStats stats, Vector3 position,
            DemoUnitDeploymentState deploymentState = DemoUnitDeploymentState.Active)
        {
            DemoUnitModel model = _simulation.AddUnit(name, team, role, stats, position, deploymentState);
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
                _commandMode = CommandMode.Select;
                _abilitySourceId = -1;
                _statusMessage = "命令已取消";
            }
            if (Input.GetKeyDown(KeyCode.A))
            {
                _commandMode = CommandMode.Engage;
                _statusMessage = "交战模式：左键点击已发现的敌方目标";
            }
            if (Input.GetKeyDown(KeyCode.B))
                EnterRemoteStrikeMode();
            if (Input.GetKeyDown(KeyCode.S))
                EnterSpecialAbilityMode();
            if (Input.GetKeyDown(KeyCode.G))
                EnterSupplyDropMode();
            if (Input.GetKeyDown(KeyCode.R))
                ApplyResult(_simulation.RequestFieldResupply(_selection));
            if (Input.GetKeyDown(KeyCode.H))
                ApplyResult(_simulation.RequestReturnToBase(_selection));
            if (Input.GetKeyDown(KeyCode.V))
                ToggleHoverSelection();
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
            if (_commandMode == CommandMode.SupplyDrop)
            {
                if (TryGroundPoint(Input.mousePosition, out Vector3 supplyTarget))
                    CommandSupplyDrop(supplyTarget);
                return;
            }

            if (_commandMode == CommandMode.RemoteStrike)
            {
                if (TryGroundPoint(Input.mousePosition, out Vector3 target))
                    CommandRemoteStrike(target);
                return;
            }

            Demo1UnitView view = RaycastUnit(Input.mousePosition);
            if (_commandMode == CommandMode.AbilityTarget)
            {
                DemoUnitModel caster = _simulation.GetUnit(_abilitySourceId);
                DemoUnitModel target = view == null ? null : _simulation.GetUnit(view.UnitId);
                if (caster == null || target == null)
                {
                    _statusMessage = "技能需要选择有效目标";
                    return;
                }
                _selection.Clear();
                _selection.Add(caster.Id);
                CommandSpecialAbility(target.Id);
                return;
            }
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
            foreach (DemoUnitModel unit in _simulation.Units.Where(unit =>
                         unit.Team == DemoTeam.Player && _simulation.IsUnitVisibleOnStrategicMap(unit.Id)))
            {
                Vector3 screen = _camera.WorldToScreenPoint(unit.Position);
                if (screen.z > 0f && rect.Contains(new Vector2(screen.x, screen.y)))
                    _selection.Add(unit.Id);
            }
        }

        private void FocusSelection()
        {
            List<DemoUnitModel> units = _selection.Select(_simulation.GetUnit).Where(unit => unit != null && unit.IsAlive).ToList();
            if (units.Count == 0)
                return;
            Vector3 center = units.Aggregate(Vector3.zero, (sum, unit) => sum + unit.Position) / units.Count;
            _cameraController.Focus(center);
        }

        private bool IsAutoAttackEnabledForSelection()
        {
            List<DemoUnitModel> selected = _selection.Select(_simulation.GetUnit)
                .Where(unit => unit != null && unit.IsAlive && unit.Team == DemoTeam.Player && !unit.IsFixed)
                .ToList();
            return selected.Count > 0 && selected.All(unit => unit.AutoAttackEnabled);
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
            _selection.RemoveWhere(id => _simulation.GetUnit(id)?.IsAlive != true || _simulation.GetUnit(id)?.IsOperational != true);
            foreach (KeyValuePair<int, Demo1UnitView> pair in _unitViews)
            {
                DemoUnitModel model = _simulation.GetUnit(pair.Key);
                bool visible = model != null && _simulation.IsUnitVisibleOnStrategicMap(pair.Key);
                pair.Value.gameObject.SetActive(visible);
                if (model != null)
                    pair.Value.Sync(model, _selection.Contains(pair.Key), visible);
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
                        Radius = Demo1Drawing.CreateCircle(root.transform, "Target Area", new Color(1f, 0.78f, 0.1f, 0.97f),
                            Demo1Drawing.EmphasizedLinePixelWidth, 96)
                    };
                    _strikeViews.Add(strike.Id, visual);
                }
                visual.Root.SetActive(!strike.Resolved);
                Demo1Drawing.SetCircle(visual.Radius, strike.Target, strike.Radius, 0.11f);
            }

            foreach (DemoSupplyDropModel drop in _simulation.SupplyDrops)
            {
                if (!_supplyViews.TryGetValue(drop.Id, out SupplyVisual visual))
                {
                    GameObject root = new GameObject($"Supply Drop #{drop.Id}");
                    root.transform.SetParent(transform);
                    GameObject marker = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                    marker.name = "Supply Beacon";
                    marker.transform.SetParent(root.transform, false);
                    marker.transform.localScale = new Vector3(1.1f, 0.12f, 1.1f);
                    Collider collider = marker.GetComponent<Collider>();
                    if (collider != null)
                        Destroy(collider);
                    marker.GetComponent<Renderer>().sharedMaterial =
                        Demo1Drawing.CreateMaterial(new Color(1f, 0.72f, 0.16f));
                    visual = new SupplyVisual
                    {
                        Root = root,
                        Marker = marker,
                        Radius = Demo1Drawing.CreateCircle(root.transform, "Supply Radius",
                            new Color(1f, 0.72f, 0.16f, 0.96f), Demo1Drawing.EmphasizedLinePixelWidth, 96)
                    };
                    _supplyViews.Add(drop.Id, visual);
                }

                visual.Root.SetActive(!drop.Finished);
                if (drop.Finished)
                    continue;
                visual.Root.transform.position = drop.Position;
                float pulse = drop.IsInbound ? 0.82f + 0.14f * Mathf.Sin(Time.unscaledTime * 6f) : 1f;
                visual.Marker.transform.localScale = new Vector3(1.1f * pulse, 0.12f, 1.1f * pulse);
                visual.Marker.transform.localPosition = Vector3.up * 0.14f;
                visual.Radius.enabled = drop.IsActive;
                if (drop.IsActive)
                    Demo1Drawing.SetCircle(visual.Radius, drop.Position, drop.Radius, 0.115f);
            }

            foreach (DemoProjectileModel projectile in _simulation.Projectiles)
            {
                if (projectile.Resolved)
                {
                    if (_projectileViews.TryGetValue(projectile.Id, out ProjectileVisual resolvedVisual))
                        resolvedVisual.Root.SetActive(false);
                    continue;
                }
                if (!_projectileViews.TryGetValue(projectile.Id, out ProjectileVisual visual))
                {
                    GameObject root = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                    root.name = $"Tracking Rocket #{projectile.Id}";
                    root.transform.SetParent(transform);
                    root.transform.localScale = Vector3.one * 0.38f;
                    Collider collider = root.GetComponent<Collider>();
                    if (collider != null)
                        Destroy(collider);
                    root.GetComponent<Renderer>().sharedMaterial =
                        Demo1Drawing.CreateMaterial(new Color(1f, 0.58f, 0.12f));
                    visual = new ProjectileVisual
                    {
                        Root = root,
                        Trail = Demo1Drawing.CreateLine(root.transform, "Rocket Trail",
                            new Color(1f, 0.38f, 0.08f, 0.95f), Demo1Drawing.OperationalLinePixelWidth)
                    };
                    _projectileViews.Add(projectile.Id, visual);
                }
                visual.Root.SetActive(true);
                visual.Root.transform.position = projectile.Position + Vector3.up * 0.55f;
                visual.Trail.positionCount = 2;
                visual.Trail.SetPosition(0, projectile.Position + Vector3.up * 0.16f);
                visual.Trail.SetPosition(1, projectile.Position - projectile.Facing * 1.8f + Vector3.up * 0.16f);
            }

            if (_rangeOverlay != null)
                _rangeOverlay.Sync(_simulation, _selection);
        }

        private bool IsHoverEnabledForSelection()
        {
            return _selection.Select(_simulation.GetUnit)
                .Any(unit => unit != null && (unit.IsHovering || unit.IsEnteringHover));
        }

        private void ToggleHoverSelection()
        {
            ApplyResult(_simulation.RequestHover(_selection, !IsHoverEnabledForSelection()));
        }

        private void OnGUI()
        {
            if (_simulation == null || _camera == null)
                return;
            EnsureGuiStyles();
            DrawWorldLabels();
            DrawCharacterDetailPanel();
            DrawTopBar();
            DrawSidePanel();
            DrawSelectionBox();
            if (_simulation.Outcome != DemoOutcome.Running)
                DrawOutcome();
        }

        private void DrawTopBar()
        {
            FillRect(new Rect(0f, 0f, Screen.width, TopBarHeight), new Color(0.018f, 0.034f, 0.048f, 0.99f));
            FillRect(new Rect(0f, TopBarHeight - 2f, Screen.width, 2f), new Color(0.12f, 0.42f, 0.52f, 0.9f));
            FillRect(new Rect(16f, 12f, 4f, 28f), PlayerAccent);
            GUI.Label(new Rect(30f, 8f, 560f, 34f), $"DEMO 1.0  ·  {ActiveLevelName}", _titleStyle);
            string state = _paused ? "已暂停" : "进行中";
            Color stateColor = _paused ? WarningAccent : new Color(0.3f, 0.95f, 0.62f, 1f);
            Rect stateRect = new Rect(Screen.width - 254f, 10f, 230f, 32f);
            FillRect(stateRect, _paused ? new Color(0.24f, 0.17f, 0.06f, 0.98f) : new Color(0.04f, 0.2f, 0.15f, 0.98f));
            FillRect(new Rect(stateRect.x, stateRect.y, 4f, stateRect.height), stateColor);
            Color previous = GUI.color;
            GUI.color = stateColor;
            GUI.Label(stateRect, $"{state}    T+ {_simulation.SimulationTime:0.0}s", _centerStyle);
            GUI.color = previous;
        }

        private void DrawSidePanel()
        {
            FillRect(new Rect(0f, TopBarHeight, PanelWidth, Screen.height - TopBarHeight), HudPanelColor);
            FillRect(new Rect(PanelWidth - 1f, TopBarHeight, 1f, Screen.height - TopBarHeight), new Color(0.16f, 0.34f, 0.4f, 0.75f));
            float y = TopBarHeight + 12f;
            Rect missionRect = new Rect(12f, y, PanelWidth - 24f, 84f);
            FillRect(missionRect, HudCardColor);
            FillRect(new Rect(missionRect.x, missionRect.y, 4f, missionRect.height), PlayerAccent);
            GUI.Label(new Rect(26f, y + 7f, 130f, 20f), "当前任务", _detailHeaderStyle);
            GUI.Label(new Rect(26f, y + 28f, PanelWidth - 54f, 30f), ActiveMissionText, _smallStyle);
            string supplyCooldown = _simulation.SupplyCallCooldownRemaining > 0f
                ? $"冷却 {_simulation.SupplyCallCooldownRemaining:0.0}s"
                : "可投放";
            int activeSupply = _simulation.SupplyDrops.Count(drop => drop.IsActive);
            GUI.Label(new Rect(26f, y + 59f, PanelWidth - 54f, 18f),
                $"战术补给 {_simulation.SupplyPackagesRemaining}/{_simulation.Balance.SupplyPackageCount}  ·  {supplyCooldown}  ·  有效区 {activeSupply}",
                _detailMutedStyle);
            y += 96f;

            GUI.Label(new Rect(14f, y, 180f, 22f), "战术命令", _detailHeaderStyle);
            y += 27f;
            if (DrawHudButton(new Rect(14f, y, 98f, 34f), _paused ? "继续  Space" : "暂停  Space", _hudButtonStyle, new Color(0.09f, 0.16f, 0.2f), new Color(0.13f, 0.25f, 0.3f))) TogglePause();
            if (DrawHudButton(new Rect(120f, y, 98f, 34f), "聚焦  F", _hudButtonStyle, new Color(0.09f, 0.16f, 0.2f), new Color(0.13f, 0.25f, 0.3f))) FocusSelection();
            bool autoAttackEnabled = IsAutoAttackEnabledForSelection();
            if (DrawHudButton(new Rect(226f, y, 98f, 34f), autoAttackEnabled ? "自动：开" : "自动：关", _hudPrimaryButtonStyle,
                    new Color(0.07f, 0.31f, 0.4f), new Color(0.08f, 0.46f, 0.58f)))
                CommandSetAutoAttack(!autoAttackEnabled);
            y += 42f;
            if (DrawHudButton(new Rect(14f, y, 98f, 34f), "交战  A", _hudPrimaryButtonStyle, new Color(0.07f, 0.31f, 0.4f), new Color(0.08f, 0.46f, 0.58f)))
            {
                _commandMode = CommandMode.Engage;
                _statusMessage = "交战模式：点击敌方目标";
            }
            if (DrawHudButton(new Rect(120f, y, 98f, 34f), "停止攻击", _hudButtonStyle, new Color(0.09f, 0.16f, 0.2f), new Color(0.13f, 0.25f, 0.3f)))
                CommandSetAutoAttack(false);
            if (DrawHudButton(new Rect(226f, y, 98f, 34f), "技能  S", _hudPrimaryButtonStyle, new Color(0.07f, 0.31f, 0.4f), new Color(0.08f, 0.46f, 0.58f)))
                EnterSpecialAbilityMode();
            y += 42f;
            bool hovering = IsHoverEnabledForSelection();
            if (DrawHudButton(new Rect(14f, y, 151f, 34f), hovering ? "退出悬停  V" : "悬停  V", _hudPrimaryButtonStyle,
                    new Color(0.07f, 0.31f, 0.4f), new Color(0.08f, 0.46f, 0.58f)))
                ToggleHoverSelection();
            if (DrawHudButton(new Rect(173f, y, 151f, 34f), "返航基地  H", _hudButtonStyle,
                    new Color(0.09f, 0.16f, 0.2f), new Color(0.13f, 0.25f, 0.3f)))
                ApplyResult(_simulation.RequestReturnToBase(_selection));
            y += 46f;
            if (DrawHudButton(new Rect(14f, y, 151f, 34f), "投放补给  G", _hudPrimaryButtonStyle,
                    new Color(0.22f, 0.22f, 0.08f), new Color(0.38f, 0.34f, 0.08f)))
                EnterSupplyDropMode();
            if (DrawHudButton(new Rect(173f, y, 151f, 34f), "前往补给  R", _hudButtonStyle,
                    new Color(0.16f, 0.14f, 0.06f), new Color(0.3f, 0.25f, 0.07f)))
                ApplyResult(_simulation.RequestFieldResupply(_selection));
            y += 46f;

            Rect feedbackRect = new Rect(14f, y, PanelWidth - 28f, 42f);
            FillRect(feedbackRect, new Color(0.035f, 0.075f, 0.095f, 0.98f));
            FillRect(new Rect(feedbackRect.x, feedbackRect.y, 3f, feedbackRect.height), WarningAccent);
            GUI.Label(new Rect(feedbackRect.x + 10f, feedbackRect.y + 5f, feedbackRect.width - 18f, feedbackRect.height - 8f), _statusMessage, _smallStyle);
            y += 52f;
            GUI.Label(new Rect(14f, y, 210f, 24f), $"已选择  {_selection.Count}", _detailHeaderStyle);
            GUI.Label(new Rect(218f, y + 1f, 106f, 22f), "独立行动单位", _detailMutedStyle);
            y += 29f;

            float eventTop = Mathf.Max(500f, Screen.height - 214f);
            int visibleCards = Mathf.Clamp(Mathf.FloorToInt((eventTop - y - 8f) / 104f), 0, 3);
            List<DemoUnitModel> selected = _selection.Select(_simulation.GetUnit).Where(unit => unit != null).Take(visibleCards).ToList();
            foreach (DemoUnitModel unit in selected)
            {
                DrawUnitCard(unit, y);
                y += 104f;
            }

            y = Mathf.Max(y + 4f, Screen.height - 214f);
            FillRect(new Rect(12f, y - 7f, PanelWidth - 24f, 1f), new Color(0.18f, 0.36f, 0.42f, 0.7f));
            GUI.Label(new Rect(14f, y, 220f, 24f), "战场事件", _detailHeaderStyle);
            GUI.Label(new Rect(218f, y + 1f, 106f, 22f), "点击快速定位", _detailMutedStyle);
            y += 28f;
            foreach (DemoBattleEvent battleEvent in _events.Take(5))
            {
                if (DrawHudButton(new Rect(14f, y, PanelWidth - 28f, 28f), $"{battleEvent.Time:000.0}   {battleEvent.Message}", _eventButtonStyle, new Color(0.04f, 0.08f, 0.105f), new Color(0.08f, 0.18f, 0.22f)))
                    _cameraController.Focus(battleEvent.Position);
                y += 31f;
            }
        }

        private void DrawUnitCard(DemoUnitModel unit, float y)
        {
            Rect card = new Rect(14f, y, PanelWidth - 28f, 98f);
            GUI.Box(card, string.Empty, _unitCardStyle);
            Color stateAccent = unit.IsResupplying
                ? new Color(1f, 0.72f, 0.16f)
                : unit.Activity == DemoUnitActivity.Attacking ? WarningAccent : PlayerAccent;
            FillRect(new Rect(card.x, card.y, 4f, card.height), stateAccent);
            GUI.Label(new Rect(24f, y + 5f, 190f, 22f), unit.DisplayName, _unitNameStyle);
            GUI.Label(new Rect(212f, y + 5f, 96f, 22f), $"{ActivityName(unit.Activity)}  ·  {VisionTypeName(unit.Stats.WitchVisionType)}", _tagStyle);
            DrawBar(new Rect(24f, y + 32f, 88f, 15f), unit.HealthRatio, new Color(0.88f, 0.24f, 0.22f), $"HP {unit.Health:0}");
            DrawBar(new Rect(120f, y + 32f, 88f, 15f), unit.MagicRatio, new Color(0.3f, 0.52f, 0.96f), $"MP {unit.Magic:0}");
            DrawBar(new Rect(216f, y + 32f, 88f, 15f), unit.ShieldRatio, new Color(0.12f, 0.72f, 0.84f), $"盾 {unit.Shield:0}");
            string reserve = unit.Stats.UnlimitedReserveAmmo ? "∞" : unit.ReserveAmmo.ToString();
            string reload = unit.IsReloading ? $" · 装填 {unit.ReloadRemaining:0.0}s" : string.Empty;
            GUI.Label(new Rect(24f, y + 50f, 280f, 17f),
                $"速度 {unit.CurrentSpeed:0.0}/{_simulation.GetEffectiveMoveSpeed(unit.Id):0.0} · 弹药 {unit.MagazineAmmo}/{reserve}{reload}", _detailMutedStyle);
            GUI.Label(new Rect(24f, y + 66f, 280f, 17f),
                $"锁定 {unit.LockQuality:0}% · 压制 {unit.Suppression:0}% · {AbilityStatus(unit)}", _detailMutedStyle);
            if (unit.IsResupplying)
                GUI.Label(new Rect(24f, y + 82f, 280f, 15f), BuildSupplyStatus(unit), _detailMutedStyle);
            else if (unit.LockedTargetId >= 0)
            {
                DemoUnitModel target = _simulation.GetUnit(unit.LockedTargetId);
                GUI.Label(new Rect(24f, y + 82f, 280f, 15f), $"目标  {(target != null ? target.DisplayName : "最后已知位置")}", _detailMutedStyle);
            }
        }

        private void DrawCharacterDetailPanel()
        {
            if (!IsCharacterDetailVisible(out DemoUnitModel unit))
                return;

            Rect panel = GetCharacterDetailRect();
            FillRect(panel, HudPanelColor);
            FillRect(new Rect(panel.x, panel.y, panel.width, 2f), PlayerAccent);
            FillRect(new Rect(panel.x, panel.y, 1f, panel.height), new Color(0.2f, 0.48f, 0.56f, 0.8f));
            GUI.BeginGroup(panel);
            Rect viewport = new Rect(4f, 4f, panel.width - 8f, panel.height - 8f);
            float contentHeight = 830f;
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
            y += 25f;
            DrawDetailBar(new Rect(14f, y, contentWidth, 17f), unit.Stats.MoveSpeed <= 0f ? 0f : unit.CurrentSpeed / unit.Stats.MoveSpeed,
                new Color(0.22f, 0.82f, 0.56f), $"速度  {unit.CurrentSpeed:0.0}/{_simulation.GetEffectiveMoveSpeed(unit.Id):0.0}");
            y += 25f;
            DrawDetailBar(new Rect(14f, y, contentWidth, 17f), unit.LockQualityRatio,
                new Color(1f, 0.68f, 0.16f), $"锁定质量  {unit.LockQuality:0}%");
            y += 25f;
            DrawDetailBar(new Rect(14f, y, contentWidth, 17f), unit.SuppressionRatio,
                new Color(0.92f, 0.25f, 0.22f), $"压制  {unit.Suppression:0}%");
            y += 34f;

            GUI.Label(new Rect(14f, y, contentWidth, 20f), "当前状态", _detailHeaderStyle);
            y += 22f;
            GUI.Label(new Rect(14f, y, contentWidth, 42f), BuildCurrentActionText(unit), _smallStyle);
            y += 45f;

            GUI.Label(new Rect(14f, y, contentWidth, 20f), "目标与姿态", _detailHeaderStyle);
            y += 22f;
            GUI.Label(new Rect(14f, y, contentWidth, 44f), BuildTargetingText(unit), _smallStyle);
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
            DrawDetailStatRow(ref y, contentWidth, "机动", unit.Stats.Mobility.ToString("0.#"), "最大移速", _simulation.GetEffectiveMoveSpeed(unit.Id).ToString("0.#"));
            DrawDetailStatRow(ref y, contentWidth, "攻击射程", _simulation.GetEffectiveAttackRange(unit.Id).ToString("0.#"), "基础命中", unit.Stats.BaseAccuracy.ToString("P0"));
            DrawDetailStatRow(ref y, contentWidth, "攻击间隔",
                FormatAdjustedSeconds(unit.Stats.AttackInterval, _simulation.GetEffectiveAttackInterval(unit.Id)), "转向率", $"{60f * unit.Stats.Mobility:0.#}°/s");
            DrawDetailStatRow(ref y, contentWidth, "暴击",
                FormatAdjustedPercent(unit.Stats.CriticalChance, _simulation.GetEffectiveCriticalChance(unit.Id)), "核心发现",
                FormatAdjustedPercent(unit.Stats.CoreDiscovery, _simulation.GetEffectiveCoreDiscovery(unit.Id)));
            DrawDetailStatRow(ref y, contentWidth, "穿透", unit.Stats.Penetration.ToString("0.#"), "护甲", unit.Stats.Armor.ToString("0.#"));
            string reserve = unit.Stats.UnlimitedReserveAmmo ? "∞" : unit.ReserveAmmo.ToString();
            DrawDetailStatRow(ref y, contentWidth, "弹匣/备弹", $"{unit.MagazineAmmo}/{reserve}", "装填", unit.IsReloading ? $"{unit.ReloadRemaining:0.0}s" : "就绪");
            y += 4f;
            string mechanismTitle = unit.Stats.PassiveAbility == DemoPassiveAbility.FireControlSolution
                ? "被动机制 · 射击诸元装订"
                : $"主动机制 · {AbilityName(unit.Stats.SpecialAbility)}";
            GUI.Label(new Rect(14f, y, contentWidth, 20f), mechanismTitle, _detailHeaderStyle);
            y += 22f;
            GUI.Label(new Rect(14f, y, contentWidth, 55f), AbilityDescription(unit), _detailMutedStyle);
            y += 58f;
            if (unit.Stats.SpecialAbility != DemoSpecialAbility.None && DrawHudButton(new Rect(14f, y, contentWidth, 31f),
                    $"使用 {AbilityName(unit.Stats.SpecialAbility)}  ·  {AbilityStatus(unit)}", _hudPrimaryButtonStyle,
                    new Color(0.07f, 0.31f, 0.4f), new Color(0.08f, 0.46f, 0.58f)))
                EnterSpecialAbilityMode();
            y += 38f;
            GUI.Label(new Rect(14f, y, contentWidth, 42f), RoleDescription(unit), _detailMutedStyle);

            GUI.EndScrollView();
            GUI.EndGroup();
        }

        private void DrawDetailBar(Rect rect, float ratio, Color color, string text)
        {
            FillRect(rect, new Color(0.012f, 0.025f, 0.034f, 0.95f));
            FillRect(new Rect(rect.x, rect.y, rect.width * Mathf.Clamp01(ratio), rect.height), color);
            FillRect(new Rect(rect.x, rect.yMax - 1f, rect.width, 1f), new Color(1f, 1f, 1f, 0.16f));
            GUI.Label(rect, text, _barTextStyle);
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
            if (unit.IsResupplying)
                return BuildSupplyStatus(unit);
            DemoUnitModel target = unit.LockedTargetId >= 0 ? _simulation.GetUnit(unit.LockedTargetId) : null;
            if (unit.IsEnteringHover)
                return $"减速悬停中  ·  当前速度 {unit.CurrentSpeed:0.00}";
            if (unit.IsHovering)
                return unit.Stats.PassiveAbility != DemoPassiveAbility.FireControlSolution
                    ? "悬停中"
                    : unit.IsFireControlReady
                        ? "悬停稳定  ·  射击诸元已装订"
                        : $"悬停稳定中  ·  {unit.HoverStableTime:0.0}/{unit.Stats.PassiveActivationDelay:0.0}s";
            if (unit.HasDestination && target != null &&
                Vector3.Distance(unit.Position, target.Position) <= unit.Stats.AttackRange)
                return $"移动射击：{target.DisplayName}  ·  前往 ({unit.Destination.x:0.0}, {unit.Destination.z:0.0})";
            if (unit.Activity == DemoUnitActivity.Attacking && target != null)
                return $"攻击中：{target.DisplayName}，距离 {Vector3.Distance(unit.Position, target.Position):0.0}";
            if (unit.Activity == DemoUnitActivity.Pursuing)
                return target != null
                    ? $"追击中：{target.DisplayName}，距离 {Vector3.Distance(unit.Position, target.Position):0.0}"
                    : $"前往目标最后已知位置 ({unit.TargetLastKnownPosition.x:0.0}, {unit.TargetLastKnownPosition.z:0.0})";
            if (unit.HasDestination)
                return $"{ActivityName(unit.Activity)}：前往 ({unit.Destination.x:0.0}, {unit.Destination.z:0.0})";
            return $"{ActivityName(unit.Activity)}  ·  位置 ({unit.Position.x:0.0}, {unit.Position.z:0.0})";
        }

        private string BuildSupplyStatus(DemoUnitModel unit)
        {
            DemoSupplyDropModel drop = _simulation?.SupplyDrops.FirstOrDefault(item => item.Id == unit.SupplyDropId);
            if (drop == null || !drop.IsActive)
                return "补给区失效，正在恢复待机";
            float distance = Vector3.Distance(unit.Position, drop.Position);
            if (distance > drop.Radius * Mathf.Clamp(_simulation.Balance.SupplyApproachRadiusRatio, 0.1f, 1f))
                return $"前往补给 #{drop.Id}  ·  距离 {distance:0.0}";
            if (unit.IsEnteringHover)
                return $"补给 #{drop.Id}  ·  减速悬停 {unit.CurrentSpeed:0.0}";
            float hitPause = Mathf.Max(0f,
                _simulation.Balance.SupplyHitPauseDuration - (_simulation.SimulationTime - unit.LastHitAt));
            if (hitPause > 0f)
                return $"补给 #{drop.Id}  ·  受击暂停 {hitPause:0.0}s";
            return $"接收补给 #{drop.Id}  ·  库存 {drop.RemainingSupply:0}/{drop.Capacity:0}";
        }

        private string BuildTargetingText(DemoUnitModel unit)
        {
            string stance = unit.AutoAttackEnabled ? "自动攻击：开启" : "自动攻击：关闭";
            DemoUnitModel target = unit.LockedTargetId >= 0 ? _simulation.GetUnit(unit.LockedTargetId) : null;
            if (target != null && target.IsAlive)
                return $"{stance}  ·  {(unit.HasExplicitAttackOrder ? "手动锁定" : "自动锁定")}\n目标：{target.DisplayName}";
            return unit.HasTargetLastKnownPosition
                ? $"{stance}\n目标丢失，最后位置 ({unit.TargetLastKnownPosition.x:0.0}, {unit.TargetLastKnownPosition.z:0.0})"
                : $"{stance}\n当前无目标";
        }

        private bool IsCharacterDetailVisible(out DemoUnitModel unit)
        {
            unit = null;
            if (_simulation == null || _selection.Count != 1)
                return false;
            unit = _simulation.GetUnit(_selection.First());
            return unit != null && unit.IsAlive && unit.Team == DemoTeam.Player;
        }

        private static Rect GetCharacterDetailRect()
        {
            float width = Mathf.Clamp(Screen.width * 0.22f, 280f, 340f);
            float height = Mathf.Clamp(Screen.height - 74f, 360f, 660f);
            return new Rect(Screen.width - width - 12f, TopBarHeight + 10f, width, height);
        }

        private void DrawWorldLabels()
        {
            foreach (DemoUnitModel unit in _simulation.Units)
            {
                if (!_simulation.IsUnitVisibleOnStrategicMap(unit.Id))
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

            foreach (DemoRemoteStrikeModel strike in _simulation.RemoteStrikes.Where(strike => !strike.Resolved))
            {
                Vector3 screen = _camera.WorldToScreenPoint(strike.Target);
                GUI.Label(new Rect(screen.x - 70f, Screen.height - screen.y - 15f, 140f, 24f), $"打击 {Mathf.Max(0f, strike.Remaining):0.0}s", _worldLabelStyle);
            }

            foreach (DemoSupplyDropModel drop in _simulation.SupplyDrops.Where(drop => !drop.Finished))
            {
                Vector3 screen = _camera.WorldToScreenPoint(drop.Position + Vector3.up * 1.2f);
                if (screen.z <= 0f)
                    continue;
                string label = drop.IsInbound
                    ? $"补给 #{drop.Id} 投放 {drop.InboundRemaining:0.0}s"
                    : $"补给 #{drop.Id}  {drop.RemainingSupply:0}/{drop.Capacity:0}  ·  {drop.ActiveRemaining:0.0}s";
                GUI.Label(new Rect(screen.x - 115f, Screen.height - screen.y - 16f, 230f, 24f), label, _worldLabelStyle);
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
                _simulation.Outcome == DemoOutcome.Victory ? ActiveVictoryText : ActiveDefeatText, _centerStyle);
            if (GUI.Button(new Rect(box.x + 120f, box.y + 132f, 200f, 38f), "重新开始 [Enter]"))
                RestartScene();
        }

        private void DrawBar(Rect rect, float ratio, Color color, string text)
        {
            FillRect(rect, new Color(0.01f, 0.02f, 0.028f, 0.95f));
            FillRect(new Rect(rect.x, rect.y, rect.width * Mathf.Clamp01(ratio), rect.height), color);
            if (!string.IsNullOrEmpty(text))
                GUI.Label(rect, text, _barTextStyle);
        }

        private void EnsureGuiStyles()
        {
            if (_hudCardTexture == null) _hudCardTexture = CreateGuiTexture(HudCardColor);
            if (_hudSectionTexture == null) _hudSectionTexture = CreateGuiTexture(HudSectionColor);
            if (_titleStyle != null)
            {
                RefreshGuiStylePalette();
                return;
            }
            _titleStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 18,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleLeft,
                normal = { textColor = new Color(0.92f, 0.97f, 1f) }
            };
            _smallStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 12,
                wordWrap = true,
                alignment = TextAnchor.UpperLeft,
                normal = { textColor = new Color(0.82f, 0.88f, 0.9f) }
            };
            _centerStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 14,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter,
                normal = { textColor = Color.white }
            };
            _detailHeaderStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 14,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleLeft,
                padding = new RectOffset(7, 4, 1, 1),
                normal = { background = _hudSectionTexture, textColor = new Color(0.64f, 0.9f, 0.98f) }
            };
            _detailValueStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 20,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleLeft,
                normal = { textColor = Color.white }
            };
            _detailMutedStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 12,
                wordWrap = true,
                alignment = TextAnchor.UpperLeft,
                normal = { textColor = new Color(0.62f, 0.72f, 0.76f) }
            };
            _unitCardStyle = new GUIStyle(GUI.skin.box) { normal = { background = _hudCardTexture } };
            _unitNameStyle = new GUIStyle(_smallStyle)
            {
                fontSize = 14,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleLeft,
                normal = { textColor = Color.white }
            };
            _tagStyle = new GUIStyle(_smallStyle)
            {
                fontSize = 11,
                alignment = TextAnchor.MiddleRight,
                normal = { textColor = new Color(0.58f, 0.8f, 0.86f) }
            };
            _barTextStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 10,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter,
                normal = { textColor = Color.white }
            };
            _hudButtonStyle = CreateHudButtonStyle();
            _hudPrimaryButtonStyle = CreateHudButtonStyle();
            _hudDangerButtonStyle = CreateHudButtonStyle();
            _eventButtonStyle = CreateHudButtonStyle();
            _eventButtonStyle.alignment = TextAnchor.MiddleLeft;
            _eventButtonStyle.fontSize = 11;
            _eventButtonStyle.padding = new RectOffset(9, 6, 2, 2);
            _worldLabelStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 12,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter,
                normal = { textColor = Color.white }
            };
            RefreshGuiStylePalette();
            _selectionTexture = new Texture2D(1, 1);
            _selectionTexture.SetPixel(0, 0, new Color(0.12f, 0.8f, 1f, 0.18f));
            _selectionTexture.Apply();
        }

        private static GUIStyle CreateHudButtonStyle()
        {
            GUIStyle style = new GUIStyle(GUI.skin.label)
            {
                fontSize = 12,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter,
                padding = new RectOffset(4, 4, 3, 3)
            };
            style.normal.textColor = new Color(0.88f, 0.94f, 0.96f);
            style.hover.textColor = Color.white;
            style.active.textColor = Color.white;
            return style;
        }

        private static void SetStyleTextColor(GUIStyle style, Color color)
        {
            style.normal.textColor = color;
            style.hover.textColor = color;
            style.active.textColor = color;
            style.focused.textColor = color;
            style.onNormal.textColor = color;
            style.onHover.textColor = color;
            style.onActive.textColor = color;
            style.onFocused.textColor = color;
        }

        private void RefreshGuiStylePalette()
        {
            SetStyleTextColor(_titleStyle, new Color(0.92f, 0.97f, 1f));
            SetStyleTextColor(_smallStyle, new Color(0.82f, 0.88f, 0.9f));
            SetStyleTextColor(_centerStyle, Color.white);
            SetStyleTextColor(_detailHeaderStyle, new Color(0.64f, 0.9f, 0.98f));
            _detailHeaderStyle.normal.background = _hudSectionTexture;
            SetStyleTextColor(_detailValueStyle, Color.white);
            SetStyleTextColor(_detailMutedStyle, new Color(0.62f, 0.72f, 0.76f));
            _unitCardStyle.normal.background = _hudCardTexture;
            SetStyleTextColor(_unitNameStyle, Color.white);
            SetStyleTextColor(_tagStyle, new Color(0.58f, 0.8f, 0.86f));
            SetStyleTextColor(_barTextStyle, Color.white);
            SetStyleTextColor(_hudButtonStyle, new Color(0.88f, 0.94f, 0.96f));
            SetStyleTextColor(_hudPrimaryButtonStyle, Color.white);
            SetStyleTextColor(_hudDangerButtonStyle, Color.white);
            SetStyleTextColor(_eventButtonStyle, new Color(0.78f, 0.86f, 0.89f));
            SetStyleTextColor(_worldLabelStyle, Color.white);
        }

        private static bool DrawHudButton(Rect rect, string label, GUIStyle style, Color normalColor, Color hoverColor)
        {
            bool hover = rect.Contains(Event.current.mousePosition);
            FillRect(rect, hover ? hoverColor : normalColor);
            FillRect(new Rect(rect.x, rect.yMax - 2f, rect.width, 2f), new Color(0.35f, 0.72f, 0.8f, hover ? 0.9f : 0.38f));
            return GUI.Button(rect, label, style);
        }

        private static Texture2D CreateGuiTexture(Color color)
        {
            Texture2D texture = new Texture2D(1, 1, TextureFormat.RGBA32, false);
            texture.hideFlags = HideFlags.HideAndDontSave;
            texture.SetPixel(0, 0, color);
            texture.Apply();
            return texture;
        }

        private static void FillRect(Rect rect, Color color)
        {
            Color previous = GUI.color;
            GUI.color = color;
            GUI.DrawTexture(rect, Texture2D.whiteTexture);
            GUI.color = previous;
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
                    return unit != null && _simulation.IsUnitVisibleOnStrategicMap(unit.Id);
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
            if (mouse.x <= PanelWidth || mouse.y >= Screen.height - TopBarHeight)
                return true;
            Vector2 guiMouse = new Vector2(mouse.x, Screen.height - mouse.y);
            return IsCharacterDetailVisible(out _) && GetCharacterDetailRect().Contains(guiMouse);
        }

        private void CreateGridLine(Transform parent, Vector3 a, Vector3 b)
        {
            LineRenderer line = Demo1Drawing.CreateLine(parent, "Grid Line", new Color(0.22f, 0.38f, 0.42f, 0.55f),
                Demo1Drawing.BackgroundGridPixelWidth);
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
                case DemoUnitActivity.Pursuing: return "追击中";
                case DemoUnitActivity.Attacking: return "攻击中";
                case DemoUnitActivity.Destroyed: return "失去战斗力";
                case DemoUnitActivity.Resupplying: return "补给中";
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
            if (unit.Stats.ExplosiveRadius > 0f)
                return $"作战方式：发射追踪火箭，飞行中持续转向目标，爆炸半径 {unit.Stats.ExplosiveRadius:0.#}。";
            switch (unit.Role)
            {
                case DemoUnitRole.Witch: return "角色特性：在警戒半径内自动锁定目标，进入射程后直接攻击。";
                case DemoUnitRole.Support: return "角色特性：可在机动中引导治疗，但治疗期间不能射击。";
                case DemoUnitRole.Artillery: return "角色特性：单发反装甲武器，远距离射击依赖锁定质量。";
                case DemoUnitRole.Scout: return "角色特性：独立侦察并利用高机动规避攻击。";
                case DemoUnitRole.Guard: return "角色特性：在警戒半径内主动追击并压制魔女。";
                case DemoUnitRole.Fortress: return "角色特性：低生命时进入紧急弹幕状态。";
                default: return string.Empty;
            }
        }

        private static string AbilityName(DemoSpecialAbility ability)
        {
            switch (ability)
            {
                case DemoSpecialAbility.MagicEyeSearch: return "魔眼搜索";
                case DemoSpecialAbility.Heal: return "治疗";
                case DemoSpecialAbility.FireControlSolution: return "射击诸元装订";
                case DemoSpecialAbility.LightningStrike: return "雷击";
                default: return "无";
            }
        }

        private static string AbilityStatus(DemoUnitModel unit)
        {
            if (unit.IsChannelingAbility)
                return $"准备 {unit.AbilityChannelRemaining:0.0}s";
            if (unit.AbilityCooldownRemaining > 0f)
                return $"冷却 {unit.AbilityCooldownRemaining:0.0}s";
            if (unit.Stats.SpecialAbility != DemoSpecialAbility.None)
                return "就绪";
            return unit.Stats.ExplosiveRadius > 0f ? "火箭齐射" : "常规武器";
        }

        private static string AbilityDescription(DemoUnitModel unit)
        {
            if (unit.Stats.PassiveAbility == DemoPassiveAbility.FireControlSolution)
            {
                string state = unit.IsFireControlReady
                    ? "已生效"
                    : unit.IsHovering
                        ? $"稳定中 {unit.HoverStableTime:0.0}/{unit.Stats.PassiveActivationDelay:0.0}s"
                        : "需要悬停";
                return $"悬停稳定 {unit.Stats.PassiveActivationDelay:0.#}s 后自动生效；对已评估或核心标记目标获得 {unit.Stats.PassiveAttackRange:0.#} 射程、最低 {unit.Stats.PassiveMinimumAccuracy:P0} 命中、{unit.Stats.PassiveDamageMultiplier:0.#} 倍伤害和 {unit.Stats.PassivePenetration:0.#} 穿透。当前：{state}。";
            }
            switch (unit.Stats.SpecialAbility)
            {
                case DemoSpecialAbility.MagicEyeSearch:
                    return $"消耗 {unit.Stats.AbilityMagicCost:0} 魔力，扫描前方 {unit.Stats.AbilityArcAngle:0}° / {unit.Stats.AbilityRange:0}，评估并标记核心 {unit.Stats.AbilityDuration:0}s。";
                case DemoSpecialAbility.Heal:
                    return $"选择 {unit.Stats.AbilityRange:0} 距离内其他友军，引导 {unit.Stats.AbilityDuration:0}s，每秒恢复 {unit.Stats.AbilityValue:P0} 最大生命。";
                case DemoSpecialAbility.FireControlSolution:
                    return $"稳定盘旋 {unit.Stats.AbilityDuration:0}s，对 {unit.Stats.AbilityRange:0} 距离内已评估目标进行高精度反装甲射击。";
                case DemoSpecialAbility.LightningStrike:
                    return $"消耗 {unit.Stats.AbilityMagicCost:0} 魔力，对半径 {unit.Stats.AbilityRadius:0.#} 内敌人造成范围雷击并增加压制。";
                default:
                    return unit.Stats.ExplosiveRadius > 0f
                        ? $"追踪火箭速度 {unit.Stats.ProjectileSpeed:0.#}，转向 {unit.Stats.ProjectileTurnRate:0.#}°/s，爆炸半径 {unit.Stats.ExplosiveRadius:0.#}。"
                        : "该单位没有主动技能。";
            }
        }
    }
}
