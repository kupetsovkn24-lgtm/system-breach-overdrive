using System;
using UnityEngine;

namespace SystemBreachOverdrive
{
    [Serializable]
    public struct LevelConfig
    {
        public int GridSize;
        public float TimerSeconds;
        public bool AllowTNodes;
        public int LockedNodes;
        public int Seed;
        public int ExitCount;
        public int RequiredNodes;
        public int OverloadedNodes;
        public float OverloadPenaltySeconds;
        public float QuickFinishThreshold;

        public LevelConfig(
            int gridSize,
            float timerSeconds,
            bool allowTNodes,
            int lockedNodes,
            int seed,
            int exitCount,
            int requiredNodes,
            int overloadedNodes,
            float overloadPenaltySeconds,
            float quickFinishThreshold)
        {
            GridSize = gridSize;
            TimerSeconds = timerSeconds;
            AllowTNodes = allowTNodes;
            LockedNodes = lockedNodes;
            Seed = seed;
            ExitCount = exitCount;
            RequiredNodes = requiredNodes;
            OverloadedNodes = overloadedNodes;
            OverloadPenaltySeconds = overloadPenaltySeconds;
            QuickFinishThreshold = quickFinishThreshold;
        }

        public static LevelConfig CreateDefault(
            int gridSize,
            float timerSeconds,
            bool allowTNodes,
            int lockedNodes,
            int seed,
            int exitCount,
            int requiredNodes,
            int overloadedNodes)
        {
            return new LevelConfig(
                gridSize,
                timerSeconds,
                allowTNodes,
                lockedNodes,
                seed,
                Mathf.Max(1, exitCount),
                Mathf.Max(0, requiredNodes),
                Mathf.Max(0, overloadedNodes),
                2.5f,
                0.65f);
        }
    }
}
