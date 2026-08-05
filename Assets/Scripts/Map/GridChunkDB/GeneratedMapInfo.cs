using UnityEngine;

namespace GridMapSystem
{
    // 생성된 맵의 플레이어 시작 위치 / 종료 위치만 담아두는 컴포넌트. 실제 Player 생성이나
    // 종료(클리어) 처리는 여기서 하지 않는다 — 런타임의 GameManager 등이 이 컴포넌트를 찾아
    // 위치 정보만 읽어서 알아서 처리한다.
    public class GeneratedMapInfo : MonoBehaviour
    {
        public Vector3 playerSpawnPosition;
        public Vector3 endPosition;
    }
}
