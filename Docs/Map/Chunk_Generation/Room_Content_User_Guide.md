# 방 내부 생성 설정과 확인 방법

방 프리팹의 지형·몬스터·아이템·함정 설정을 확인하고 수정하는 방법이다. 더 자세한 제작 조건은 [방 템플릿 제작 규격](Work/Room_Template_Authoring_Guide.md), 진행 상황은 [Chunk 내부 생성 진행 현황](Chunk_Generation_Progress.md)을 참고한다.

## 게임 시작 시 적용 방식

Title에서 게임을 시작하면 설정된 카탈로그에서 방을 고르고, 각 방에 미리 설정된 패턴·확률 셀·콘텐츠·함정의 선택 결과를 자동으로 적용한다. 위치, 후보 프리팹, 타일과 확률은 방을 만들 때 직접 설정한다. 등록되지 않은 몬스터나 함정이 자동으로 추가되지는 않는다.

방별 목표·출구 잠금은 현재 `WestSouth`와 `Down` Base 및 상속 Variant에 설정돼 있다. 주 경로의 중간 방으로 선택되면 실제 생성된 콘텐츠와 목표 후보를 바탕으로 규칙 하나를 정한다. 검증 상태는 [진행 현황](Chunk_Generation_Progress.md)을 참고한다.

낙하 발판은 [맵 플랫폼](../../../Assets/Prefabs/Map/Platforms/FallingPlatform/)에, 함정은 [Trap](../../../Assets/Prefabs/Trap/)에 둔다. 몬스터 프리팹은 각 종류의 `Monster` 하위 폴더에서 확인한다.

현재 Title 씬의 `MapManager`는 [MapPlanningSettings](../../../Assets/Prefabs/@Data/Map/MapPlanningSettings.asset)와 연결돼 있고, 이 설정의 [RoomTemplateCatalog](../../../Assets/Prefabs/@Data/Map/RoomTemplateCatalog.asset)에 실제 사용할 방 Variant가 등록돼 있다. 기본 맵 크기는 4×4다. `MapManager`의 **Use Random Seed**가 켜져 있어 새 Run의 seed를 자동으로 정한다. 끄면 **Seed**에 지정한 값을 사용한다. 같은 설정·카탈로그·코드와 seed의 결과는 재현되지만, 입력 설정이나 템플릿 ID가 바뀌면 달라질 수 있다.

## 방 설정 열기

1. Project 창에서 [Base 방 프리팹 폴더](../../../Assets/Prefabs/Map/RoomTemplates/Base/)의 방을 열고 루트 오브젝트를 선택한다.
2. Inspector에서 **Room Template**은 방 구조, **Room Terrain Settings**는 지형·플랫폼, **Room Monster Settings**와 **Room Item Settings**는 각각 몬스터·아이템, **Room Hazard Settings**는 함정을 설정한다. 기존 후보는 이전이 끝날 때까지 **Room Content Settings**에 남아 있다.
3. Scene 뷰에서 **Gizmos**를 켜면 회색은 방 경계, 초록색은 출입구, 청록색은 변형 영역, 빨간색은 보호 영역, 자홍색은 새 몬스터 슬롯, 노란색은 아이템과 기존 콘텐츠 슬롯 기준점이다. 확률 셀과 함정 위험 공간은 별도로 그려지지 않으므로 Inspector 값을 확인한다.

방은 20×20 셀이며 방의 왼쪽 아래가 `(0, 0)`이다. 한 셀은 1×1 Unity 단위다. Base의 설정은 이를 상속한 Variant에 적용되지만 Variant에서 해당 항목을 Override했다면 그 값이 우선한다. 게임에서 사용하는 Variant가 카탈로그에 등록되어 있는지도 함께 확인한다.

## Inspector 항목의 역할

아래 항목은 방 루트의 기능별 컴포넌트에 있다. 각 목록은 방을 제작할 때 설정하며, 게임 시작 시 선택된 방의 설정을 바탕으로 배치 결과를 정한다.

| 컴포넌트 | 항목 | 의미 | 예시 |
| --- | --- | --- | --- |
| **Room Terrain Settings** | **Variation Regions** | 랜덤 지형을 적용할 사각형 영역의 위치와 크기다. | `(3, 10)`부터 `4×3` 크기로 지정한다. |
| **Room Terrain Settings** | **Terrain Variations** | `Region Index`로 영역을 연결하고 패턴·확률 셀을 정한다. | 0번 영역에 발판 또는 빈 패턴을 고른다. |
| **Room Terrain Settings** | **Protected Regions** | 변형 영역이나 콘텐츠가 침범하지 못하는 영역이다. | 통과에 필요한 공간을 보호한다. |
| **Room Content Settings** | **Content Slots** | 몬스터·아이템을 배치할 기준 셀이다. | 0번 슬롯을 `(7, 5)`에 지정한다. |
| **Room Content Settings** | **Content Settings** | 슬롯별 공간·지지면·방향·후보와 가중치를 정한다. | 0번 슬롯에 고블린·아이템·빈 상태를 설정한다. |
| **Room Monster Settings** | **Kind, Slots** | 일반·엘리트 몬스터 종류와 각 위치·공간·프리팹 후보를 정한다. | 일반 몬스터 슬롯에 고블린 후보를 등록한다. |
| **Room Item Settings** | **Slots** | 아이템 위치·공간·프리팹 후보를 별도로 정한다. | 몬스터와 다른 위치에 아이템 슬롯을 둔다. |
| **Room Hazard Settings** | **Hazards** | 방 안의 함정 후보, 위험 공간·지지면과 출현 가중치를 정한다. | 화염 함정 후보와 빈 상태를 각각 가중치 1로 둔다. |

목록의 항목 번호는 `0`부터 시작한다. 예를 들어 `Variation Regions`의 첫 영역을 사용하려면 `Terrain Variations`의 `Region Index`를 `0`으로 설정한다. 콘텐츠도 같은 방식으로 기준 셀과 설정을 연결한다. 한 콘텐츠 슬롯에서는 허용된 후보 하나 또는 빈 상태가 선택되며, 서로 다른 함정 후보는 각각 출현 여부를 고른다.

## 랜덤 지형 설정

예시: [Horizontal Base](../../../Assets/Prefabs/Map/RoomTemplates/Base/Horizontal.prefab)의 `Room Terrain Settings`.

1. **Variation Regions**에 변형할 사각형의 `X`, `Y`, `Width`, `Height`를 지정한다. 첫 번째 영역의 번호는 `0`이다. 영역은 방 경계·보호 영역·다른 변형 영역과 겹치지 않아야 하고, 사용하는 영역에는 고정 타일이 없어야 한다.
2. **Terrain Variations**에 항목을 추가하고 `Region Index`로 위 영역 번호를 연결한다. `Allow Empty Pattern`을 켜면 아무 패턴도 놓지 않는 선택지를 포함한다.
3. **Patterns**에 고유 `Id`, `Prefab`과 `Kind`를 지정한다. 고정 타일 지형은 `Tilemap`, 움직이는 발판은 `Platform`을 선택한다. 플랫폼의 `Offset`은 영역의 왼쪽 아래를 기준으로 한 위치다. 같은 영역의 패턴은 빈 패턴을 포함해 동일한 비중으로 선택한다.
4. **Chance Cells**에 방 내부 `Cell` 좌표, 배치할 `Tile`, `Chance` 0~100을 지정한다. 셀은 영역 안에 있어야 하며 패턴의 타일과 겹치지 않아야 한다. 확률 셀은 패턴 선택과 함께 적용될 수 있다.

Horizontal의 현재 설정은 영역 `(3, 10)` 크기 `4×3`, 발판 패턴 또는 빈 패턴, 확률 셀 `(3, 12)`의 타일 배치 확률 50%다. Horizontal의 콘텐츠 기준점 `(7, 5)`은 있지만 콘텐츠 후보와 함정은 비어 있다.

`NorthWest_Tiered`에는 `(15, 4)` 크기 `3×3`의 변형 영역에 낙하 발판 패턴과 빈 패턴이 있다. 발판의 Offset은 `(1.5, 2.2)`이므로 방 내부 위치는 `(16.5, 6.2)`다. 패턴 하나와 빈 패턴을 각각 50%로 선택하며, 같은 방의 가시와 가시 공은 별도로 선택한다.

`Tilemap` 패턴은 Grid·Tilemap·Collider 구성이 필요하다. `Platform` 패턴은 루트에 활성 BoxCollider2D와 Kinematic Rigidbody2D가 필요하며 초기 충돌체는 변형 영역 안에 있어야 한다. 자동 검사는 발판이 움직이는 전체 경로나 다른 함정과의 실제 충돌을 보장하지 않으므로 각 조합의 이동을 직접 확인한다.

## 몬스터·아이템 설정

새 설정은 Variant의 `Room Monster Settings`와 `Room Item Settings`에서 구분한다. 각 `Slots` 항목에 기준 셀 `Position`, 빈 공간 `Clearance`, 지지면 `Surface`, 방향 `Facing`, 빈 상태 가중치 `Empty Weight`와 `Options`의 프리팹·가중치·위치 오프셋을 넣는다. 몬스터의 `Kind`는 일반 경로용 `Normal` 또는 선택 방용 `Elite`다. 슬롯과 후보의 내부 식별자는 자동으로 관리한다.

각 슬롯은 후보 하나 또는 빈 상태를 선택한다. 몬스터와 아이템을 별도 슬롯에 설정하면 함께 나올 수 있으며, 공간·지지면 검사 조건은 아래 기존 설정과 같다. Base의 새 설정은 현재 비어 있고 기존 후보는 아직 이전하지 않았다.

기존 설정 예시: [WestSouth Base](../../../Assets/Prefabs/Map/RoomTemplates/Base/WestSouth.prefab).

방 루트의 `Room Content Settings`에서 설정한다.

1. **Content Slots**에 배치 기준 셀을 추가한다. 좌표만 추가해서는 프리팹이 생성되지 않는다.
2. **Content Settings**에 설정을 추가하고 `Slot Index`로 기준점 번호를 연결한다. `Clearance`에는 콘텐츠를 놓을 빈 공간의 좌표와 크기, `Surface`에는 지지면, `Facing`에는 방향을 지정한다.
3. **Options**에 후보의 고유 `Id`, `Prefab`, `Kind`, `Weight`, `Offset`을 지정한다. `Empty Weight`는 그 슬롯을 비울 가중치다. `Offset`은 기준 셀의 `(x+0.5, y)`를 기준으로 한다.
4. 한 슬롯에서는 허용된 후보 중 하나 또는 빈 상태가 선택된다. 일반 경로 방은 일반 몬스터·아이템, 선택 방은 엘리트 몬스터·아이템을 허용한다. 시작·Goal·닫힌 방은 콘텐츠 선택 대상이 아니다.

WestSouth에는 고블린·아이템·빈 상태의 가중치가 각각 1이다. 이 일반 방 슬롯에서는 각각 1/3이며 한 슬롯에서 고블린과 아이템이 동시에 나오지는 않는다. 선택 방은 일반 몬스터 후보를 제외한다. 엘리트 몬스터는 아직 연결되지 않았다.

`Optional_CapEast_Ruins`와 `Optional_CapWest_Ruins`는 Variant에서 콘텐츠 후보를 Override해 아이템과 빈 상태를 각각 가중치 1로 설정했다. `Junction_FourWay_Ruins`에는 콘텐츠 슬롯 설정이 없다.

콘텐츠 공간은 고정 지형·보호 영역·출입구·다른 슬롯과 겹치지 않아야 한다. 공간과 지지면은 변형 영역과 겹칠 수 없고, 지지면은 전체 셀이 충돌하는 Grid 타입 고정 타일이어야 한다. 후보는 루트에 활성 BoxCollider2D 또는 CapsuleCollider2D 하나가 있어야 하며 지정한 공간과 지지면에 맞아야 한다.

## 방별 진행 목표 설정

예시: [WestSouth Base](../../../Assets/Prefabs/Map/RoomTemplates/Base/WestSouth.prefab)의 자식 **RoomObjectives**에 있는 `Room Objective Setup` 컴포넌트다.

| 항목 | 의미 |
| --- | --- |
| **Controller** | 선택된 목표와 출구를 관리하는 `Room Objective Controller`다. |
| **Condition Chance** | 해당 방을 조건 방으로 만들 확률이다. 현재 대표 방의 기본값은 50%다. |
| **Rules** | 이 방에서 사용할 목표 규칙 ScriptableObject 목록이다. 실제 후보에 적용 가능한 규칙 중 하나를 seed로 선택한다. 전체 몬스터 처치·무작위 몬스터 한 마리 처치·지점 도달·스위치 작동 규칙이 있다. |
| **Triggers** | 방에 미리 둔 지점·스위치 후보 목록이다. 고유 `Id`, 후보 `Objective`와 출현 확률 `Chance`를 지정한다. 확률로 선택된 후보 중 선택한 규칙과 같은 종류만 활성화한다. |

몬스터를 목표로 지정하기 위해 슬롯 번호를 따로 입력하지 않는다. 실제 생성된 몬스터가 있을 때만 몬스터 규칙이 후보가 되며, 아이템과 빈 슬롯은 제외한다. 지점·스위치 규칙은 선택된 같은 종류의 후보를 모두 완료해야 한다. 적용할 규칙이 없거나 조건 방으로 선택되지 않으면 출구는 열린 상태다. 규칙 에셋은 [Objectives](../../../Assets/Prefabs/@Data/Map/Objectives/)에서 확인할 수 있다.

출구 문은 방마다 지정하지 않는다. [MapPlanningSettings](../../../Assets/Prefabs/@Data/Map/MapPlanningSettings.asset)의 **Exit Gate Prefab**에 [공통 문 프리팹](../../../Assets/Prefabs/Map/Progression/RoomExitGate.prefab)을 할당하면 목표가 생성된 방의 주 경로 출구에 자동 배치된다. 목표가 있는 방에서 프리팹이 없거나 문·Collider·Sprite가 비활성 상태면 맵 생성에 실패한다. 입구와 선택 방 분기는 열려 있다. 목표를 완료하면 충돌이 즉시 해제되고 `Room Exit Gate`의 **Fade Duration** 동안 Sprite가 사라진다. 현재 빨간 사각형은 임시 외형이다.

후보 위치·출구를 바꾸거나 방을 추가하면 실제 Player로 접근·완료와 통과를 다시 확인한다. 정식 목표 UI는 별도 UI 작업에서 제작한다.

## 함정 설정

WestSouth의 **Room Hazard Settings → Hazards**에 `floor.fire`와 `overhead.spikehead`가 설정돼 있다.

1. Base 방 안에 함정 프리팹을 자식으로 배치하고 후보 오브젝트 자체를 비활성화한다.
2. **Hazards**에 항목을 추가해 고유 `Id`와 방 안의 `Candidate`를 연결한다.
3. `Clearance`에는 위험 공간, `Support`에는 고정 지형 지지면을 지정한다. 위험 공간에 후보 기준점이 포함되어야 한다. 위험 공간과 지지면은 서로 겹치지 않고, 변형·보호 영역과 콘텐츠 공간을 피해야 한다. 지지면에는 Grid 타입 충돌 타일이 필요하다.
4. `Spawn Weight`와 `Empty Weight`로 생성·빈 상태의 가중치를 정한다. 둘 다 1이면 해당 후보가 선택될 확률은 1/2이다. 서로 다른 함정 후보는 각각 생성 여부를 고르므로 한 방에서 함께 나올 수 있다.
5. 함정이 없을 때와 있을 때 모든 기록된 통과 방향, 함께 나올 수 있는 콘텐츠와 함정의 대표 조합을 실제 Player로 확인한 뒤 `Passage Verified`를 켠다.

정적 검사는 함정의 실제 이동 경로·투사체 범위나 화면 가시성을 보장하지 않는다. 이동 범위, 지형 또는 발사 조건을 바꾸면 검증 표시를 직접 해제하고 다시 확인한다.

## 검사와 결과 확인

1. 프리팹을 선택하고 **Map → Room Templates → Validate Selected**를 실행해 Console의 결과를 확인한다. 설정 오류나 필요한 검증 표시가 없으면 실패 사유가 표시된다.
2. 지형·통로·함정 조건을 바꾼 뒤에는 해당 `Passages Verified`, `Combinations Verified`, `Passage Verified`와 시작·Goal·왕복 검증 표시를 직접 해제한다. 자동으로 해제되지 않는다. 별도 시험 환경에서 실제 Player의 이동과 조합을 확인한 다음 해당 표시를 다시 기록한다. 검증되지 않은 조합은 카탈로그 검사에 실패하므로 검증 전까지 실제 카탈로그에 반영하지 않는다.
3. **Map → Room Map Generator**에서 `Planning Settings`, `Goal Prefab`, `Seed`를 확인하고 **입력한 Seed로 생성**을 누르면 현재 활성 씬에 결과가 생성된다. **랜덤 맵 생성**으로 다른 seed를 고를 수도 있다. 확인한 뒤 **미리보기 삭제**로 해당 창의 미리보기를 지울 수 있다. 미리보기는 씬을 변경하므로 작업 중인 씬을 보존하려면 별도 씬에서 확인한다.
4. 마지막으로 Title에서 게임을 시작해 실제 화면의 통과·회피와 콘텐츠 동작을 확인한다. 미리보기와 정적 검사만으로 플레이 통과가 보장되지는 않는다.
