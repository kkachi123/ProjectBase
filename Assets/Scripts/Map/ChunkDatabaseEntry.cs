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
    /// 청크 데이터베이스의 개별 항목. entrances(연결 가능한 전체 지점 목록)는 프리팹의
    /// ChunkData에서 미리 캐싱해두므로, 생성기가 소켓 호환성을 검사할 때 프리팹을 로드하지
    /// 않고도 후보를 필터링할 수 있다.
    /// </summary>
    [System.Serializable]
    public class ChunkDatabaseEntry
    {
        public GameObject prefab;
        public string prefabName;
        public ChunkType type;
        public ChunkDifficulty difficulty;
        public System.Collections.Generic.List<Vector2Int> entrances = new System.Collections.Generic.List<Vector2Int>();
        public EndLineRole endLineRole = EndLineRole.Normal;

        public System.Collections.Generic.List<Vector2Int> GetAllEntrances()
        {
            return entrances != null ? new System.Collections.Generic.List<Vector2Int>(entrances) : new System.Collections.Generic.List<Vector2Int>();
        }
        public int width;   // Combat/Puzzle/Treasure=20, Transition=10
        public int height;  // 10
    }
}
