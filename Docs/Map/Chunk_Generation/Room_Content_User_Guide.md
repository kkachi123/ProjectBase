# 방 내부 생성 설정과 확인 방법

방 프리팹의 지형·몬스터·아이템·함정 설정을 확인하고 수정하는 방법이다. 더 자세한 제작 조건은 [방 템플릿 제작 규격](Work/Room_Template_Authoring_Guide.md), 진행 상황은 [Chunk 내부 생성 진행 현황](Chunk_Generation_Progress.md)을 참고한다.

## 게임 시작 시 적용 방식

Title에서 게임을 시작하면 설정된 카탈로그에서 방을 고르고, 각 방에 미리 설정된 패턴·확률 셀·콘텐츠·함정의 선택 결과를 자동으로 적용한다. 위치, 후보 프리팹, 타일과 확률은 방을 만들 때 직접 설정한다. 등록되지 않은 몬스터나 함정이 자동으로 추가되지는 않는다.

방별 목표·출구 잠금은 별도 시제품으로 검증 중이며 현재 게임 시작 시 자동 적용되지 않는다. 진행 상태는 [진행 현황](Chunk_Generation_Progress.md)을 참고한다.

현재 Title 씬의 `MapManager`는 [MapPlanningSettings](../../../Assets/Prefabs/@Data/Map/MapPlanningSettings.asset)와 연결돼 있고, 이 설정의 [RoomTemplateCatalog](../../../Assets/Prefabs/@Data/Map/RoomTemplateCatalog.asset)에 실제 사용할 방 Variant가 등록돼 있다. 기본 맵 크기는 4×4다. `MapManager`의 **Use Random Seed**가 켜져 있어 새 Run의 seed를 자동으로 정한다. 끄면 **Seed**에 지정한 값을 사용한다. 같은 설정·카탈로그·코드와 seed의 결과는 재현되지만, 입력 설정이나 템플릿 ID가 바뀌면 달라질 수 있다.

## 방 설정 열기

1. Project 창에서 [Base 방 프리팹 폴더](../../../Assets/Prefabs/Map/RoomTemplates/Base/)의 방을 열고 루트 오브젝트를 선택한다.
2. Inspector의 **Room Template** 컴포넌트에서 `Variation Regions`, `Terrain Variations`, `Content Slots`, `Content Settings`, `Hazards`를 확인한다.
3. Scene 뷰에서 **Gizmos**를 켜면 회색은 방 경계, 초록색은 출입구, 청록색은 변형 영역, 빨간색은 보호 영역, 노란색은 콘텐츠 슬롯 기준점이다. 확률 셀과 함정 위험 공간은 별도로 그려지지 않으므로 Inspector 값을 확인한다.

방은 20×20 셀이며 방의 왼쪽 아래가 `(0, 0)`이다. 한 셀은 1×1 Unity 단위다. Base의 설정은 이를 상속한 Variant에 적용되지만 Variant에서 해당 항목을 Override했다면 그 값이 우선한다. 게임에서 사용하는 Variant가 카탈로그에 등록되어 있는지도 함께 확인한다.

## 랜덤 지형 설정

예시: [Horizontal Base](../../../Assets/Prefabs/Map/RoomTemplates/Base/Horizontal.prefab)의 `Room Template`.

1. **Variation Regions**에 변형할 사각형의 `X`, `Y`, `Width`, `Height`를 지정한다. 첫 번째 영역의 번호는 `0`이다. 영역은 방 경계·보호 영역·다른 변형 영역과 겹치지 않아야 하고, 사용하는 영역에는 고정 타일이 없어야 한다.
2. **Terrain Variations**에 항목을 추가하고 `Region Index`로 위 영역 번호를 연결한다. `Allow Empty Pattern`을 켜면 아무 패턴도 놓지 않는 선택지를 포함한다.
3. **Patterns**에 고유 `Id`와 패턴 `Prefab`을 지정한다. 각 패턴은 같은 영역에서 서로 대안이며 빈 패턴을 포함해 동일한 비중으로 선택한다. 패턴은 정해진 Grid·Tilemap·Collider 구성을 갖춘 프리팹이어야 한다.
4. **Chance Cells**에 방 내부 `Cell` 좌표, 배치할 `Tile`, `Chance` 0~100을 지정한다. 셀은 영역 안에 있어야 하며 패턴의 타일과 겹치지 않아야 한다. 확률 셀은 패턴 선택과 함께 적용될 수 있다.

Horizontal의 현재 설정은 영역 `(3, 10)` 크기 `4×3`, 발판 패턴 또는 빈 패턴, 확률 셀 `(3, 12)`의 타일 배치 확률 50%다. Horizontal의 콘텐츠 기준점 `(7, 5)`은 있지만 콘텐츠 후보와 함정은 비어 있다.

## 몬스터·아이템 설정

예시: [WestSouth Base](../../../Assets/Prefabs/Map/RoomTemplates/Base/WestSouth.prefab).

1. **Content Slots**에 배치 기준 셀을 추가한다. 좌표만 추가해서는 프리팹이 생성되지 않는다.
2. **Content Settings**에 설정을 추가하고 `Slot Index`로 기준점 번호를 연결한다. `Clearance`에는 콘텐츠를 놓을 빈 공간의 좌표와 크기, `Surface`에는 지지면, `Facing`에는 방향을 지정한다.
3. **Options**에 후보의 고유 `Id`, `Prefab`, `Kind`, `Weight`, `Offset`을 지정한다. `Empty Weight`는 그 슬롯을 비울 가중치다. `Offset`은 기준 셀의 `(x+0.5, y)`를 기준으로 한다.
4. 한 슬롯에서는 허용된 후보 중 하나 또는 빈 상태가 선택된다. 일반 경로 방은 일반 몬스터·아이템, 선택 방은 엘리트 몬스터·아이템을 허용한다. 시작·Goal·닫힌 방은 콘텐츠 선택 대상이 아니다.

WestSouth에는 고블린·아이템·빈 상태의 가중치가 각각 1이다. 이 일반 방 슬롯에서는 각각 1/3이며 한 슬롯에서 고블린과 아이템이 동시에 나오지는 않는다. 선택 방은 일반 몬스터 후보를 제외한다. 엘리트 몬스터는 아직 연결되지 않았다.

콘텐츠 공간은 고정 지형·보호 영역·출입구·다른 슬롯과 겹치지 않아야 한다. 공간과 지지면은 변형 영역과 겹칠 수 없고, 지지면은 전체 셀이 충돌하는 Grid 타입 고정 타일이어야 한다. 후보는 루트에 활성 BoxCollider2D 또는 CapsuleCollider2D 하나가 있어야 하며 지정한 공간과 지지면에 맞아야 한다.

## 함정 설정

WestSouth의 **Hazards**에 `floor.fire`와 `overhead.spikehead`가 설정돼 있다.

1. Base 방 안에 함정 프리팹을 자식으로 배치하고 후보 오브젝트 자체를 비활성화한다.
2. **Hazards**에 항목을 추가해 고유 `Id`와 방 안의 `Candidate`를 연결한다.
3. `Clearance`에는 위험 공간, `Support`에는 고정 지형 지지면을 지정한다. 위험 공간에 후보 기준점이 포함되어야 한다. 위험 공간과 지지면은 서로 겹치지 않고, 변형·보호 영역과 콘텐츠 공간을 피해야 한다. 지지면에는 Grid 타입 충돌 타일이 필요하다.
4. `Spawn Weight`와 `Empty Weight`로 생성·빈 상태의 가중치를 정한다. 둘 다 1이면 해당 후보가 선택될 확률은 1/2이다. 서로 다른 함정 후보는 각각 생성 여부를 고르므로 한 방에서 함께 나올 수 있다.
5. 함정이 없을 때와 있을 때 모든 기록된 통과 방향, 함께 나올 수 있는 콘텐츠와 함정의 대표 조합을 실제 Player로 확인한 뒤 `Passage Verified`를 켠다.

정적 검사는 함정의 실제 이동 경로·투사체 범위나 화면 가시성을 보장하지 않는다. 이동 범위, 지형 또는 발사 조건을 바꾸면 검증 표시를 직접 해제하고 다시 확인한다.

## 검사와 결과 확인

1. 프리팹을 선택하고 **Map → Room Templates → Validate Selected**를 실행해 Console의 결과를 확인한다. 설정 오류나 필요한 검증 표시가 없으면 실패 사유가 표시된다.
2. 지형·통로·함정 조건을 바꾼 뒤에는 해당 `Passages Verified`, `Combinations Verified`, `Passage Verified`와 시작·Goal·왕복 검증 표시를 직접 해제한다. 자동으로 해제되지 않는다. 별도 시험 환경에서 실제 Player의 이동과 조합을 확인한 다음 해당 표시를 다시 기록한다. 검증되지 않은 조합은 카탈로그 검사에 실패하므로 검증 전까지 실제 카탈로그에 반영하지 않는다.
3. **Map → Room Map Generator**에서 `Planning Settings`, `Goal Prefab`, `Seed`를 확인하고 **Generate Preview**를 누르면 현재 활성 씬에 결과가 생성된다. 생성된 오브젝트와 방을 확인한 뒤 **Clear This Preview**로 해당 창의 미리보기를 지울 수 있다. 미리보기는 씬을 변경하므로 작업 중인 씬을 보존하려면 별도 씬에서 확인한다.
4. 마지막으로 Title에서 게임을 시작해 실제 화면의 통과·회피와 콘텐츠 동작을 확인한다. 미리보기와 정적 검사만으로 플레이 통과가 보장되지는 않는다.
