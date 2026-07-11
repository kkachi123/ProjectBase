using System.Collections.Generic;
using UnityEngine;

namespace MapSystem
{
    [CreateAssetMenu(fileName = "ChunkDatabase", menuName = "Map/Chunk Database")]
    public class ChunkDatabase : ScriptableObject
    {
        public List<ChunkDatabaseEntry> entries = new List<ChunkDatabaseEntry>();

        public IEnumerable<ChunkDatabaseEntry> GetByType(ChunkType type)
        {
            foreach (var e in entries)
                if (e.type == type) yield return e;
        }

        public IEnumerable<ChunkDatabaseEntry> GetByTypeAndDifficulty(ChunkType type, ChunkDifficulty difficulty)
        {
            foreach (var e in entries)
                if (e.type == type && e.difficulty == difficulty) yield return e;
        }
    }
}
