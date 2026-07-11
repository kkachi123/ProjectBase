using UnityEngine;

namespace MapSystem
{
    public enum ChunkDifficulty
    {
        None,   // Transition 등 난이도 구분이 없는 청크
        Easy,
        Medium,
        Hard,
    }

    /// <summary>
    /// 청크 데이터베이스의 개별 항목. entrance1/entrance2 높이는 프리팹의 ChunkData에서
    /// 미리 캐싱해두므로, 생성기가 소켓 호환성을 검사할 때 프리팹을 로드하지 않고도
    /// 후보를 필터링할 수 있다.
    /// </summary>
    [System.Serializable]
    public class ChunkDatabaseEntry
    {
        public GameObject prefab;
        public string prefabName;
        public ChunkType type;
        public ChunkDifficulty difficulty;
        public Vector2Int entrance1;
        public Vector2Int entrance2;
        public int width;   // Combat/Puzzle/Treasure=20, Transition=10
        public int height;  // 10
    }
}
