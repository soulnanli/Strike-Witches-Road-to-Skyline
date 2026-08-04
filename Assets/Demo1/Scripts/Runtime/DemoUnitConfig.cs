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
        public DemoUnitStats Stats = new DemoUnitStats();
        public bool GrantPersistentPlayerIntel;
        public DemoEnemyAiProfile EnemyAiProfile;
        public bool UseStartingPositionAsAiHome = true;
        public Vector3 EnemyAiHomePosition;
        public Vector3[] ScoutPatrolPoints = new Vector3[0];

        public DemoUnitStats CreateRuntimeStats()
        {
            return Stats?.Clone() ?? new DemoUnitStats();
        }

        public Vector3 GetEnemyAiHomePosition()
        {
            return UseStartingPositionAsAiHome ? StartingPosition : EnemyAiHomePosition;
        }
    }
}
