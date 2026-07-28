using System.Collections.Generic;
using UnityEngine;

namespace GridMapSystem
{
    [CreateAssetMenu(fileName = "GridChunkDatabase", menuName = "Map/Grid Chunk Database")]
    public class GridChunkDatabase : ScriptableObject
    {
        public List<GridChunkDatabaseEntry> entries = new List<GridChunkDatabaseEntry>();

        public IEnumerable<GridChunkDatabaseEntry> GetByType(GridChunkType type)
        {
            foreach (var e in entries)
                if (e.type == type) yield return e;
        }

        public IEnumerable<GridChunkDatabaseEntry> GetByTypeAndDifficulty(GridChunkType type, GridChunkDifficulty difficulty)
        {
            foreach (var e in entries)
                if (e.type == type && e.difficulty == difficulty) yield return e;
        }
    }
}
