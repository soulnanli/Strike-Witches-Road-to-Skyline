using UnityEngine;

namespace SWRTS.Demo1
{
    [CreateAssetMenu(fileName = "DemoUnit", menuName = "SWRTS/Demo1/Unit Config")]
    public sealed class DemoUnitConfig : ScriptableObject
    {
        public int SpawnOrder;
        public string DisplayName = "新单位";
        public DemoTeam Team;
        public DemoUnitRole Role;
        public Vector3 StartingPosition;
        [Header("Historical movement reference")]
        public string StrikerUnitModel;
        [Min(0f)] public float HistoricalMaxSpeedKph;
        public string HistoricalSpeedBasis;
        public bool UseHistoricalMovementSpeed;
        public DemoUnitStats Stats = new DemoUnitStats();
        public bool GrantPersistentPlayerIntel;
        public DemoEnemyAiProfile EnemyAiProfile;
        public bool UseStartingPositionAsAiHome = true;
        public Vector3 EnemyAiHomePosition;
        public Vector3[] ScoutPatrolPoints = new Vector3[0];

        public DemoUnitStats CreateRuntimeStats(Demo1Balance balance = null)
        {
            DemoUnitStats stats = Stats?.Clone() ?? new DemoUnitStats();
            if (UseHistoricalMovementSpeed && HistoricalMaxSpeedKph > 0f && balance != null)
                stats.MoveSpeed = balance.HistoricalSpeedToMapUnitsPerSecond(HistoricalMaxSpeedKph);
            return stats;
        }

        public Vector3 GetEnemyAiHomePosition()
        {
            return UseStartingPositionAsAiHome ? StartingPosition : EnemyAiHomePosition;
        }
    }
}
