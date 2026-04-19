using System.Collections.Generic;
using UnityEngine;

namespace SystemBreachOverdrive
{
    [CreateAssetMenu(fileName = "LevelDatabase", menuName = "System Breach/Level Database", order = 1)]
    public class LevelDatabaseSO : ScriptableObject
    {
        public List<LevelDefinitionSO> Levels = new List<LevelDefinitionSO>();

        public List<LevelConfig> ToConfigs()
        {
            var result = new List<LevelConfig>();
            for (var i = 0; i < Levels.Count; i++)
            {
                if (Levels[i] != null)
                {
                    result.Add(Levels[i].ToConfig());
                }
            }

            return result;
        }
    }
}
