using UnityEngine;

namespace SystemBreachOverdrive
{
    [CreateAssetMenu(fileName = "LevelDefinition", menuName = "System Breach/Level Definition", order = 0)]
    public class LevelDefinitionSO : ScriptableObject
    {
        [Min(2)] public int GridSize = 3;
        [Min(5f)] public float TimerSeconds = 60f;
        public bool AllowTNodes = false;
        [Min(0)] public int LockedNodes = 0;
        public int Seed = 1001;

        [Min(1)] public int ExitCount = 1;
        [Min(0)] public int RequiredNodes = 0;
        [Min(0)] public int OverloadedNodes = 0;

        [Min(0f)] public float OverloadPenaltySeconds = 2.5f;
        [Range(0.1f, 1f)] public float QuickFinishThreshold = 0.65f;

        public LevelConfig ToConfig()
        {
            return new LevelConfig(
                GridSize,
                TimerSeconds,
                AllowTNodes,
                LockedNodes,
                Seed,
                ExitCount,
                RequiredNodes,
                OverloadedNodes,
                OverloadPenaltySeconds,
                QuickFinishThreshold);
        }
    }
}
