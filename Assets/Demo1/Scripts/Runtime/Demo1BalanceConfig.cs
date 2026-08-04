using UnityEngine;

namespace SWRTS.Demo1
{
    [CreateAssetMenu(fileName = "Demo1Balance", menuName = "SWRTS/Demo1/Balance Config")]
    public sealed class Demo1BalanceConfig : ScriptableObject
    {
        public Demo1Balance Values = new Demo1Balance();

        public Demo1Balance CreateRuntimeValue()
        {
            return Values?.Clone() ?? new Demo1Balance();
        }
    }
}
