using UnityEngine;

namespace SWRTS.Demo1
{
    [CreateAssetMenu(fileName = "DemoLevel", menuName = "SWRTS/Demo1/Level Config")]
    public sealed class DemoLevelConfig : ScriptableObject
    {
        public string LevelId = "dover-standard";
        public int SortOrder;
        public bool IsDefault;
        public string DisplayName = "多佛海峡·标准作战";
        [TextArea(2, 4)] public string MissionText = "摧毁东侧异形军巢穴。";
        public Demo1BalanceConfig Balance;
        public DemoUnitConfig[] Units = new DemoUnitConfig[0];
        public Vector3 BasePosition = new Vector3(187.6f, 0f, 100.8f);
        public Vector3 PlayerSpawnOffset;
        public Vector3 EnemySpawnOffset;
        [Min(0.1f)] public float EnemyHealthMultiplier = 1f;
        [Min(0.1f)] public float EnemyAttackMultiplier = 1f;

        public Vector3 GetSpawnPosition(DemoUnitConfig unit)
        {
            if (unit == null)
                return Vector3.zero;
            return unit.StartingPosition + GetTeamOffset(unit.Team);
        }

        public Vector3 GetTeamOffset(DemoTeam team)
        {
            return team == DemoTeam.Player ? PlayerSpawnOffset : EnemySpawnOffset;
        }

        public DemoUnitStats CreateRuntimeStats(DemoUnitConfig unit, Demo1Balance balance = null)
        {
            DemoUnitStats stats = unit != null ? unit.CreateRuntimeStats(balance) : new DemoUnitStats();
            if (unit == null || unit.Team != DemoTeam.Enemy)
                return stats;

            stats.MaxHealth *= Mathf.Max(0.1f, EnemyHealthMultiplier);
            stats.Attack *= Mathf.Max(0.1f, EnemyAttackMultiplier);
            return stats;
        }
    }
}
