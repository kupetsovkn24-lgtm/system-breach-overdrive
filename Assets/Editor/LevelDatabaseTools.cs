#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace SystemBreachOverdrive.Editor
{
    public static class LevelDatabaseTools
    {
        private const string ResourcesDir = "Assets/Resources";
        private const string DatabasePath = "Assets/Resources/LevelDatabase.asset";
        private const int TotalLevels = 20;

        [MenuItem("System Breach/Levels/Create Default Level Database")]
        public static void CreateDefaultDatabase()
        {
            if (!Directory.Exists(ResourcesDir))
            {
                Directory.CreateDirectory(ResourcesDir);
            }

            var database = AssetDatabase.LoadAssetAtPath<LevelDatabaseSO>(DatabasePath);
            if (database == null)
            {
                database = ScriptableObject.CreateInstance<LevelDatabaseSO>();
                AssetDatabase.CreateAsset(database, DatabasePath);
            }

            database.Levels = new List<LevelDefinitionSO>();
            var defaults = BuildDefaultConfigs();

            for (var i = 0; i < defaults.Count; i++)
            {
                var levelAsset = ScriptableObject.CreateInstance<LevelDefinitionSO>();
                var config = defaults[i];
                levelAsset.GridSize = config.GridSize;
                levelAsset.TimerSeconds = config.TimerSeconds;
                levelAsset.AllowTNodes = config.AllowTNodes;
                levelAsset.LockedNodes = config.LockedNodes;
                levelAsset.Seed = config.Seed;
                levelAsset.ExitCount = config.ExitCount;
                levelAsset.RequiredNodes = config.RequiredNodes;
                levelAsset.OverloadedNodes = config.OverloadedNodes;
                levelAsset.OverloadPenaltySeconds = config.OverloadPenaltySeconds;
                levelAsset.QuickFinishThreshold = config.QuickFinishThreshold;

                var levelPath = $"Assets/Resources/Level_{i + 1:00}.asset";
                if (File.Exists(levelPath))
                {
                    AssetDatabase.DeleteAsset(levelPath);
                }

                AssetDatabase.CreateAsset(levelAsset, levelPath);
                database.Levels.Add(levelAsset);
            }

            for (var i = TotalLevels + 1; i <= 99; i++)
            {
                var extraLevelPath = $"Assets/Resources/Level_{i:00}.asset";
                if (AssetDatabase.LoadAssetAtPath<LevelDefinitionSO>(extraLevelPath) != null)
                {
                    AssetDatabase.DeleteAsset(extraLevelPath);
                }
            }

            EditorUtility.SetDirty(database);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            EditorUtility.DisplayDialog("Level Database", "LevelDatabase created with 20 levels in Assets/Resources.", "OK");
        }

        private static List<LevelConfig> BuildDefaultConfigs()
        {
            var levels = new List<LevelConfig>(TotalLevels);

            for (var i = 0; i < TotalLevels; i++)
            {
                var progression = i;
                var gridSize = progression < 5 ? 3 : progression < 10 ? 4 : 5;
                var timer = Mathf.Max(30f, 62f - progression * 1.35f);
                var allowTNodes = progression >= 3;
                var lockedNodes = Mathf.Min(gridSize * gridSize - 3, progression / 2);
                var exitCount = Mathf.Clamp(1 + progression / 6, 1, 3);
                var requiredNodes = Mathf.Clamp(progression / 4, 0, gridSize + 1);
                var overloadedNodes = Mathf.Clamp((progression - 5) / 4, 0, gridSize);
                var penalty = Mathf.Min(4.5f, 2.4f + progression * 0.09f);
                var quickThreshold = Mathf.Clamp(0.72f - progression * 0.012f, 0.45f, 0.72f);
                var seed = 1201 + progression * 173;

                levels.Add(new LevelConfig(
                    gridSize,
                    timer,
                    allowTNodes,
                    lockedNodes,
                    seed,
                    exitCount,
                    requiredNodes,
                    overloadedNodes,
                    penalty,
                    quickThreshold));
            }

            return levels;
        }
    }
}
#endif
