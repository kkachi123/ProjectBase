using UnityEngine;

namespace GridMapSystem
{
    public enum GridChunkDifficulty
    {
        None,   // Transition 등 난이도 구분이 없는 청크
        Easy,
        Medium,
        Hard,
    }

    /// <summary>
    /// 그리드 청크 데이터베이스의 개별 항목. entrances(연결 가능한 전체 지점 목록)와 footprint
    /// (칸수)는 프리팹의 GridChunkData에서 미리 캐싱해두므로, 생성기가 소켓 호환성을 검사할 때
    /// 프리팹을 로드하지 않고도 후보를 필터링할 수 있다.
    /// </summary>
    [System.Serializable]
    public class GridChunkDatabaseEntry
    {
        public GameObject prefab;
        public string prefabName;
        public GridChunkType type;
        public GridChunkDifficulty difficulty;
        public System.Collections.Generic.List<Vector2Int> entrances = new System.Collections.Generic.List<Vector2Int>();
        public GridEndLineRole endLineRole = GridEndLineRole.Normal;
        public int width;    // Content=20, Transition/EndLine=10
        public int height;   // Content=20, Transition/EndLine=10
        public Vector2Int footprint; // width/height를 셀 크기(10)로 나눈 칸수

        public System.Collections.Generic.List<Vector2Int> GetAllEntrances()
        {
            return entrances != null ? new System.Collections.Generic.List<Vector2Int>(entrances) : new System.Collections.Generic.List<Vector2Int>();
        }
    }
}
