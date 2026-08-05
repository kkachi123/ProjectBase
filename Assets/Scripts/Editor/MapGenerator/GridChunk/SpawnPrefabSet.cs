using UnityEngine;
using GridMapSystem;

namespace GridMapSystem.Editor
{
    // 생성 시점에 SpawnKind별로 어떤 프리팹을 심을지 매핑한다. 아직 실제 콘텐츠 프리팹이
    // 없는 종류는 필드를 비워두면 그냥 스킵된다(에러 없음) — 나중에 프리팹이 생기면
    // 인스펙터에서 채워 넣기만 하면 됨.
    [System.Serializable]
    public class SpawnPrefabSet
    {
        public GameObject coinPrefab;
        public GameObject monsterPrefab;
        public GameObject itemPrefab;
        public GameObject arrowTrapPrefab;
        public GameObject spikeTrapPrefab;

        public GameObject Get(SpawnKind kind)
        {
            switch (kind)
            {
                case SpawnKind.Coin: return coinPrefab;
                case SpawnKind.Monster: return monsterPrefab;
                case SpawnKind.Item: return itemPrefab;
                case SpawnKind.ArrowTrap: return arrowTrapPrefab;
                case SpawnKind.SpikeTrap: return spikeTrapPrefab;
                default: return null;
            }
        }
    }
}
