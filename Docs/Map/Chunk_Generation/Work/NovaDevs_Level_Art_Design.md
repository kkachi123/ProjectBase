# NovaDevs 맵 그래픽 배치 설계안

## 목표와 범위

[NovaDevs 에셋](../../../../Assets/@tempAssets/NovaDevs/)을 사용해 현재 방 템플릿과 랜덤 지형에 맞는 그래픽을 구성한다. 맵에 사용할 Sprite와 장식은 이 에셋을 원본으로 하며, 필요한 Tile·RuleTile과 프리팹 설정은 프로젝트에서 제작한다.

## 에셋별 사용 방식

| 에셋 | 권장 배치 | 용도와 조건 |
| --- | --- | --- |
| [Platform 1 Sprite Sheet](../../../../Assets/@tempAssets/NovaDevs/2D%20Platformer%20-%20Skull%20Garden%20%20Tilesets%20Asset%20Pack/Platform%20Tiles/Skull%20Garden%20Tiles_96x96%20Sprite%20Sheet.png) | 기본 지형 RuleTile 후보 하나 | 돌 바닥·벽·천장과 모서리를 지형 이웃 조건으로 선택한다. 내부 채움과 고립 발판까지 자연스럽게 표현되는지 확인하고, 부족한 조합은 원본 Sprite의 재분할 또는 수동 보완을 검토한다. |
| [Platform 2 Sprite Sheet](../../../../Assets/@tempAssets/NovaDevs/2D%20Platformer%20-%20Skull%20Garden%20%20Tilesets%20Asset%20Pack/Platform%20Tiles/Skull%20Garden%20Tiles_960x960%20Sprite%20Sheet.png) | 큰 Sprite의 수동 배치 후보 | 큰 지형 형태와 강조 구간에 활용할 수 있는지 검토한다. 현재 조각 크기가 1×1 셀에 맞지 않아 일반 지형 RuleTile에 바로 넣지 않는다. |
| [Bushes](../../../../Assets/@tempAssets/NovaDevs/2D%20Platformer%20-%20Skull%20Garden%20%20Tilesets%20Asset%20Pack/Bushes/)·[Rocks](../../../../Assets/@tempAssets/NovaDevs/2D%20Platformer%20-%20Skull%20Garden%20%20Tilesets%20Asset%20Pack/Rocks/) | 장식 Tile 또는 SpriteRenderer 수동 배치 | 지형 위의 풀·수풀과 돌 장식. 통로와 캐릭터를 가리지 않게 배치하며, 우선 시각 요소로 사용한다. |
| [Trees](../../../../Assets/@tempAssets/NovaDevs/2D%20Platformer%20-%20Skull%20Garden%20%20Tilesets%20Asset%20Pack/Trees/) | 배경 SpriteRenderer 수동 배치 | 먼 나무와 수풀을 지형 뒤에 배치해 깊이감을 만든다. 방 경계에서 그림이 잘리지 않는지 확인한다. |
| [Architecture](../../../../Assets/@tempAssets/NovaDevs/2D%20Platformer%20-%20Skull%20Garden%20%20Tilesets%20Asset%20Pack/Architecture/)·[Prefabs](../../../../Assets/@tempAssets/NovaDevs/2D%20Platformer%20-%20Skull%20Garden%20%20Tilesets%20Asset%20Pack/Prefabs/) | 프리팹 또는 Sprite 수동 배치 | `LampPost`, `Orion_Shrine_0`, `CastIronArch_0`, `SideWall_0`, `Stone Podium_0` 등으로 장소의 특징을 만든다. 가로등·사당 등에는 조명이나 파티클이 포함되어 있어 수와 표시 순서를 확인한다. |

장식 배치의 랜덤화는 대표 방의 수동 배치 결과와 필요한 설치 조건을 확인한 뒤 설계한다. 같은 규칙으로 연결할 지형부터 RuleTile 하나로 시도하고, 별도의 충돌이나 배치 의미가 필요한 경우에만 추가 타일을 구분한다.

## 데모 씬 참고

- [Demo 1](../../../../Assets/@tempAssets/NovaDevs/2D%20Platformer%20-%20Skull%20Garden%20%20Tilesets%20Asset%20Pack/Scenes/Demo%201.unity)과 [Demo 2](../../../../Assets/@tempAssets/NovaDevs/2D%20Platformer%20-%20Skull%20Garden%20%20Tilesets%20Asset%20Pack/Scenes/Demo%202.unity)는 `Platform 1`, `Platform 2`, `Trees`, `Bushes`, `Architecture` 등 여러 Tilemap과 사당·나무 Sprite를 함께 사용한다. 서로 다른 지형 이미지와 장식을 겹치는 구성의 참고 사례다.
- [Demo 3](../../../../Assets/@tempAssets/NovaDevs/2D%20Platformer%20-%20Skull%20Garden%20%20Tilesets%20Asset%20Pack/Scenes/Demo%203.unity)는 `Platform`, `Bushes`, `Tree`, `Rails` Tilemap과 배경 나무 Sprite를 사용한다. 플랫폼에는 Platform 1 Palette의 개별 Tile을 여러 종류 배치하고, 씬 Grid의 셀 크기는 1×1이다. 지형·식생·구조물의 역할 분리와 개별 타일 조합을 참고한다.
- 실제 렌더링에서 Demo 3은 넓은 보라색 수관, 여러 종류의 줄기, 바닥 수풀과 난간을 겹쳐 숲의 밀도를 만든다. Demo 2의 사당 구간은 큰 아치 안에 조형물과 받침을 모아 시선을 집중한다. 방의 배경·장식과 상호작용 요소를 배치하기 전에 해당 에셋의 데모 화면과 구성 요소를 확인한다.
- 데모는 방 경계와 랜덤 패턴 연결의 검증 자료가 아니므로, 이음새와 충돌은 현재 맵의 생성 결과에서 따로 확인한다. 데모의 1×1 Grid와 Palette의 약 1.176 셀 크기 차이가 표시 결과에 미치는 영향도 확인한다.

## 지형과 그래픽의 책임

**선택한 설계:** 방마다 충돌 Tilemap을 유지하고, 최종 지형을 맵 전체의 그래픽 Tilemap 하나에 표시한다. 이 방식은 기존 콘텐츠 지지면 검사와 방별 Collider를 유지하면서 RuleTile이 방·패턴 경계의 이웃을 같은 Tilemap에서 판정하게 한다. 그래픽 전용 Tilemap과 점유 셀 수집이 추가되는 비용이 있다.

방별 Tilemap에 RuleTile을 직접 적용하는 방식은 구성이 단순하지만 다른 방·패턴 Tilemap의 이웃을 기본 판정으로 읽지 못한다. 반대로 충돌까지 맵 전체에 합치면 현재 방 프리팹·콘텐츠 설치면 검사와 Collider 관리 방식을 변경해야 한다.

현재 Builder는 각 방과 패턴을 별도 Tilemap으로 생성하고 확률 셀을 고정 지형에 적용한다. `MapPlanningSettings`에 그래픽 타일을 지정하면 확정 지형을 공통 그래픽 Tilemap으로 표시한다. 지정하지 않으면 방별 지형을 그대로 표시한다.

1. **지형 원본:** `FixedGeometry`, 선택된 패턴, 확률 셀은 지금처럼 방별 충돌 지형을 결정한다. 각 TilemapCollider2D와 콘텐츠 지지면 검사를 유지한다.
2. **확정 지형 수집:** 모든 방·패턴·확률 셀 적용 후, 생성된 충돌 Tilemap의 셀을 맵 기준 정수 좌표로 변환한다. 방 `(x,y)` 원점은 `(20x,-20y)`이고 고정 지형은 여기에 방 내부 셀을 더한다. 패턴은 여기에 변형 영역의 왼쪽 아래와 패턴 로컬 셀을 더한다. 음수 y와 네 방향 경계를 같은 기준으로 처리한다.
3. **시각화:** 맵 루트 아래 위치·회전 0, 스케일 1의 Grid와 1×1 그래픽 Tilemap을 만들고, 수집된 각 지형 셀에 같은 지형 RuleTile을 배치한다. 모든 셀 적용 후 갱신해 맵 전체의 이웃을 판정한다. 선택적 지형 머티리얼을 지정하면 이 Tilemap의 Renderer에 적용한다. 이 Tilemap에는 Collider를 붙이지 않는다.
4. **표시와 실패 처리:** 그래픽 생성에 성공한 뒤 해당 충돌 TilemapRenderer만 숨긴다. 방의 배경·장식 Renderer는 유지한다. 셀이 겹치거나 지원되지 않는 충돌 타입이면 이유를 반환하고 비활성 임시 맵을 정리한다. 생성 실패 시 기존 맵과 미리보기는 보존한다.
5. **호출 경계:** `MapBuilder`가 방과 지형을 만든 후 전용 그래픽 생성 책임을 호출한다. `MapPlanner`와 `MapPlan`의 경로·난수·콘텐츠 선택은 변경하지 않는다. 런타임과 에디터는 공통 흐름을 사용하고, 생성·Renderer 변경은 `MapBuildContext`와 Undo에 맞게 처리한다.

그래픽 통합은 **전체 셀이 충돌하는 Grid 지형**부터 지원한다. 타일 존재만으로 실제 충돌이 전체 셀이라고 가정하지 않는다. `Sprite` 또는 `None` 충돌과 부분 형상은 자동으로 전체 셀 그래픽으로 변환하지 않으며, 적용 대상 템플릿을 검증해 미지원 사유를 표시한다. 기존 전환 템플릿의 실제 Collider 타입은 적용 전 확인하고, 지원 범위를 확장하거나 템플릿을 별도로 전환하기 전까지 기존 동작을 유지한다. 그래픽 적용 여부와 설정은 명시적으로 선택할 수 있어야 한다.

## 기본 지형 RuleTile 규칙

- [시제품 RuleTile](../../../../Assets/Art/Map/Terrain/Nova_Ground_Prototype.asset)은 같은 지형의 상하좌우와 관련 대각선을 판정한다. 16가지 기본 형상과 31가지 복합 안쪽 모서리 조건을 사용하며, 구체적인 모서리 조건을 기본 형상보다 먼저 검사한다.
- `Nova_Platform1_1Unit.png`는 원본의 분할을 유지한 300 PPU 복사본이다. `Nova_Interior.png`는 내부 Sprite의 중심 무늬를 반복 가능하게 만든 채움이고, `Nova_Vertical_*`와 `Nova_Isolated.png`는 원본 조각을 조합한 한 셀 지형이다.
- `Nova_SingleCorners.png`는 원본 안쪽 모서리와 인접한 바닥·벽·내부 그림을 섞어 만든 네 방향 모서리다. `Nova_InnerCorners.png`는 여러 경계가 함께 있는 31가지 조합을 담는다. `Nova_WallSeams.png`는 원본 좌우 벽과 한 셀 세로 벽의 위아래 경계를 완화한 Sprite를 담는다. 각 그림은 원본 NovaDevs Sprite를 바탕으로 만들었다.
- 기본 Sprite는 내부 채움이다. 출력은 그래픽 전용이며 충돌 판정에는 사용하지 않는다. 무작위 Sprite나 회전·반전은 적용하지 않는다.
- [지형 머티리얼](../../../../Assets/Art/Map/Terrain/NovaTerrainLit.mat)은 Sprite의 어두운 색을 2D 조명 계산 전에 보정해 검은 내부를 빈 공간과 구분한다. 알파와 충돌은 바꾸지 않는다. [셰이더](../../../../Assets/Art/Map/Terrain/NovaTerrainLit.shader)는 현재 URP `Sprite-Lit-Default` 구현을 바탕으로 하므로 URP 버전 변경 시 원본의 렌더링·조명 처리와 비교해야 한다. 현재 확인은 전역 조명과 대표 방에 한정된다.

## 배경과 장식 배치

배경 나무·수풀과 가로등·사당·아치는 방별 Sprite 또는 프리팹으로 수동 배치한다. 현재 프로젝트의 정렬값, 기준점, 출입구와 방 경계의 조건은 [방 지형·배경·장식 제작 규칙](Room_Art_Authoring_Guide.md)을 따른다.

## 구현 검증

- 같은 seed의 방 배치·패턴·확률 셀·콘텐츠 결과가 그래픽 적용 전후에 같고, 방별 Collider와 지지면 검사가 유지된다.
- 고정 지형·패턴·확률 셀의 좌표와 그래픽 셀이 일치하며 좌우·상하 방 경계, 음수 좌표, 출입구, 빈 패턴과 단일 셀을 확인한다. 중복 또는 미지원 타일이 있으면 명확히 실패한다.
- 렌더링 결과에서 안쪽·바깥쪽 모서리와 발판 끝의 Sprite, 1×1 셀 정렬과 시각·충돌 경계를 확인한다. 데모 씬의 실제 화면 및 원본 Sprite의 조합 가능성도 확인한다.
- 생성 실패 후 기존 맵·미리보기가 유지되고, 에디터 Undo·Redo 및 런타임 생성과 정리가 일관되게 동작한다.

## 제작 시 확인할 사항

- Platform 1 원본은 34개 Sprite로 분할되어 있으며 각 분할 영역은 300×300픽셀, import 설정은 255 PPU다. 복사본은 300 PPU로 가져와 1×1 셀에 맞췄고, 원본 참조는 유지한다. 원본 import 설정을 변경하면 이를 참조하는 다른 에셋에도 영향을 줄 수 있다.
- Platform 2의 현재 분할 영역은 960×718픽셀이고 import 설정은 640 PPU다. 1×1 셀의 자동 타일에 사용하려면 별도의 크기와 분할 검토가 필요하다.
- 시제품 RuleTile은 47가지 이웃 조합에 Sprite를 지정한다. 대표 경계에서 명암과 반복 이음새를 줄였으며, 모든 조합의 미관을 확인한 것은 아니다.
- 인접 방의 좌우·상하 경계, 패턴의 유무와 확률 셀, 출입구와 착지면에서 그래픽 이음새와 실제 충돌을 확인한다.
- Room Template 제작에는 단색 지형으로 통로를 먼저 확인한 대표 방 1~2개를 사용한다. 그래픽과 충돌의 적용 결과에 따라 관련 Player 통과 검증을 다시 수행한다.

상세 출입구와 지형 조건은 [방 템플릿 제작 규격](Room_Template_Authoring_Guide.md), 남은 작업 단계는 [Chunk 내부 생성 진행 현황](../Chunk_Generation_Progress.md)을 참고한다.
