# 방 템플릿 제작 규격

## 기본 구성

- 방 크기는 20×20 Unity 단위, Grid 셀은 1×1이다. 루트의 기준점은 왼쪽 아래이며 회전 0, 스케일 1이다.
- 루트에는 `Grid`와 `RoomTemplate`을 둔다. `FixedGeometry`는 루트 바로 아래에 위치 0·회전 0·스케일 1의 Tilemap으로 두고 지형 Collider를 연결한다.
- `RoomTemplate`은 ID, 방 유형, 열린 출입구, 진입→진출 후보, 변형·보호 영역, 콘텐츠 슬롯과 Player 시작·Goal 마커를 기록한다.
- 방 유형은 출입구의 역할을 나타낸다. `Side`는 출입구가 없고, `Horizontal`은 동·서 중 하나 이상, `Down`은 남쪽, `Arrival`은 북쪽이 열려 있어야 한다. `Arrival`에는 남쪽 출입구를 지정하지 않는다.

## 출입구와 이동 기준

좌표는 방 내부의 타일 셀이다. 출입구의 폭은 네 셀이다.

| 방향 | 경계 셀 | 월드 경계의 중심 |
| --- | --- | --- |
| 서 | x=0, y=4~7 | (0, 6) |
| 동 | x=19, y=4~7 | (20, 6) |
| 남 | x=13~16, y=0 | (15, 0) |
| 북 | x=13~16, y=19 | (15, 20) |

`RoomTemplate.OpeningCells()`가 경계 셀의 기준이다. 열린 출입구는 고정 지형을 비우고, 나머지 경계는 고정 지형으로 닫는다. 맞닿는 방은 같은 위치·폭을 사용한다. 실제 이동을 위해 문 안쪽의 몸통 공간과 착지면도 확보한다.

현재 `ProjectRE_Player Variant`의 CapsuleCollider2D는 1×1.9, 무게 1, 중력 배율 1이다. PlayerMotorData는 지상 이동 5, 공중 이동 4, 점프 힘 8, 수평 대쉬 거리 5, 공중 대쉬 1회를 사용한다. 지면 감지는 `Ground` 레이어를 사용한다. 물리 계산상 수치와 실제 조작 결과는 차이가 있을 수 있으므로 점프 거리·착지·벽 충돌은 실제 Player로 확인한다.

`Passages`는 제작자가 지정한 통과 방향 후보이며 방향별로 별도 기록한다. `Passages Verified`는 **기록한 모든 방향을 실제 Player로 확인한 뒤에만** 켠다. 정적 검사 통과가 이동 성공을 보증하지는 않는다.

`Verified Start Exits`는 시작 마커에서 도달할 수 있는 출입구, `Verified Goal Entries`는 Goal 마커에 도달할 수 있는 진입구를 기록한다. 마커의 존재와 경로 검증은 별도다.

막다른 선택 방에는 `Optional Target`을 지정하고 해당 출입구에서 목표까지 이동한 뒤 같은 출입구로 돌아올 수 있음을 확인한다. 실제 왕복을 확인한 방향만 `Verified Dead End Returns`에 기록한다.

지형·Collider·출입구·통과 방향·마커를 수정하면 검증 결과가 무효다. 관련 `Passages Verified`, 마커 검증 방향과 `Verified Dead End Returns`를 직접 지우고 해당 방향을 다시 확인한다.

## 변형과 콘텐츠 마커

- `Variation Regions`는 내부 패턴과 확률 셀을 적용할 영역이다. 방 경계와 떨어뜨리고 서로 겹치지 않게 둔다. 사용 중인 영역에는 고정 지형이 없어야 한다.
- `Protected Regions`는 필수 통로·착지면 주변에서 변형을 금지할 영역이다. 변형 영역과 겹치지 않아야 한다. 영역 위치가 실제 이동을 보장하는지는 제작자가 확인한다.
- `Terrain Variations`에서 변형 영역의 인덱스, 빈 패턴 허용 여부, 패턴 ID와 프리팹, 확률 셀의 방 내부 좌표·타일·확률(0~100)을 지정한다. 패턴은 영역의 왼쪽 아래를 기준으로 배치되며, 서로 다른 패턴은 같은 영역의 대안이다. 패턴의 타일은 영역 밖이나 확률 셀과 겹칠 수 없다.
- 패턴 프리팹은 셀 크기 1인 Grid 루트와 위치·회전 0, 크기 1인 단일 자식 Tilemap 및 활성 TilemapCollider2D로 구성한다. Tilemap은 고정 지형과 같은 레이어를 사용하며, Collider는 그 Tilemap의 TilemapCollider2D와 CompositeCollider2D만 허용한다. 타일 좌표는 영역의 왼쪽 아래를 `(0,0)`으로 사용한다.
- `Combinations Verified`는 해당 방에서 패턴과 확률 셀의 모든 조합을 실제 Player의 기록된 각 통과 방향과 시작·Goal 연결에 대해 확인한 뒤 켠다. 검증되지 않은 설정은 카탈로그 검사에서 실패한다. 패턴, 확률 셀, 지형, 통로 또는 보호 영역을 수정하면 이 표시를 지우고 다시 확인한다. 표시는 자동으로 해제되지 않는다.
- `Content Slots`에 기준 셀을 넣고 `Content Settings`에 슬롯 인덱스, 충돌 여유 공간(`Clearance`), 설치면, 좌우 방향과 선택지를 지정한다. 선택지는 ID, 프리팹, 양의 가중치, 기준 셀의 `(x+0.5, y)`에서의 위치 오프셋을 가진다. `Empty Weight`는 아무것도 배치하지 않을 가중치다.
- 콘텐츠의 루트에는 활성 BoxCollider2D 또는 CapsuleCollider2D 하나가 필요하다. 지정된 방향의 Collider가 여유 공간 안에 들어가고 설치면에 닿아야 한다. 지지면은 전체 셀이 충돌하는 Grid 타입 타일이어야 하며, 여유 공간·지지면은 변형 영역과 겹치지 않는다. 여유 공간은 고정 지형·다른 슬롯·보호 영역·출입구와 겹치지 않는다.
- 현재 콘텐츠는 왕복이 검증된 선택 방에만 생성한다. 두 막다른 방의 슬롯은 Goblin, 아이템, 빈 슬롯을 같은 가중치로 선택한다. 몬스터의 이동과 추적은 해당 FSM이 담당하므로 생성 이후에는 슬롯 영역 밖으로 이동할 수 있다.
- `Player Start`와 `Goal`은 서로 독립된 위치 마커다. 마커 자체가 Player나 GoalTrigger를 생성하지 않는다.
- 프리팹을 선택하면 Scene 뷰에서 회색 방 경계, 초록색 출입구, 청록색 변형 영역, 빨간색 보호 영역, 노란색 콘텐츠 슬롯을 확인할 수 있다.

현재 `sample.horizontal`은 `(3,10)`의 변형 영역에 `Legacy_EW1_Platform` 또는 빈 패턴을 선택하고 `(3,12)` 셀을 50% 확률로 채운다. 네 조합의 서→동·동→서 통과를 실제 Player로 확인했다. 다른 템플릿에는 아직 지형 변형이 설정되지 않았다.

## 샘플과 검사

`Assets/Prefabs/Map/RoomTemplates/Samples/`에 유형별 기본 샘플과 연속 하강용 `DownThrough` 샘플이 있다. 단색 Tile은 규격을 확인하기 위한 임시 지형 표현이다.

실제 Player와 Input System 가상 입력으로 기록된 17개 통과 방향을 확인했다. `Horizontal` 2개, `Down` 4개, `DownThrough` 7개, `Arrival` 4개가 통과해 네 샘플의 `Passages Verified`를 활성화했다. `Side`는 닫힌 채움 방으로 통과 방향이 없다. `RoomTemplateMovementSample`에서는 연속 하강과 착지도 확인했다.

`Assets/Prefabs/Map/RoomTemplates/Planning/`에는 외곽과 시작·Goal에 맞춰 출입구를 닫은 변형 9개가 있다. 시작 마커→출입구 3개, 출입구→Goal 마커 3개와 나머지 방의 통과 방향 5개를 실제 Player로 확인했다.

`CapWest`, `CapEast`는 출입구에서 내부 목표 지점까지 왕복을 확인했다. 실제 Run에서 Goblin의 Player 인식과 아이템 획득, 선택 방에서 주 경로로 돌아와 하강하는 동작을 확인했다.

프리팹을 선택하고 **Map → Room Templates → Validate Selected**를 실행하면 방 유형에 필요한 출입구, 경계 지형, 통과 방향 데이터, 변형 영역과 슬롯을 검사한다. 실제 점프·충돌·착지는 Play 확인이 필요하다.

수동 확인 씬: `Assets/Scenes/Tests/RoomTemplateMovementSample.unity`

1. 씬을 열고 Play한 뒤 방향키로 이동하고 Space로 점프한다. 대쉬는 왼쪽 Shift다.
2. 시작 방에서 오른쪽 방으로 이동해 경계의 문을 통과하고 바닥에 안정적으로 착지하는지 확인한다.
3. 오른쪽 방의 바닥 구멍을 점프로 건너고, 구멍으로 내려가 연속 하강 방과 아래 방에 도달하는지 확인한다.
4. 각 방의 나머지 기록된 통과 방향도 확인한다. 모든 후보의 검증을 마친 템플릿만 `Passages Verified`를 활성화한다.

## 기존 청크 전환

| 전환 결과 | 원본 | 적용 내용 |
| --- | --- | --- |
| [Legacy_EW2_Horizontal](../../../Assets/Prefabs/Map/RoomTemplates/Converted/Legacy_EW2_Horizontal.prefab) | `E-W 2` | 고정 지형과 기존 RuleTile을 복사하고 출입구·낮은 착지 구간을 조정했다. 서→동·동→서 통과를 확인했다. |
| [Legacy_3Dir1_Arrival](../../../Assets/Prefabs/Map/RoomTemplates/Converted/Legacy_3Dir1_Arrival.prefab) | `3Dir 1` | 고정 지형과 가로등 장식을 복사하고 출입구를 조정했다. 좌우 통과와 북→서·북→동을 확인했다. 북→동은 중앙으로 내려간 뒤 이동한다. |
| [Legacy_EW1_Platform](../../../Assets/Prefabs/Map/RoomTemplates/PatternCandidates/Legacy_EW1_Platform.prefab) | `E-W 1`의 x=4~8, y=14~16 | 발판을 독립 패턴으로 분리해 `sample.horizontal`의 변형 영역에서 사용한다. |

전환 방의 시작·Goal 마커와 콘텐츠 슬롯은 아직 지정하지 않았다. 공용 타일과 장식 프리팹은 새 템플릿 제작에 사용할 수 있다.

## 관련 코드

- [방 템플릿 데이터](../../../Assets/Scripts/Map/RoomTemplates/RoomTemplate.cs)
- [제작 데이터 검사](../../../Assets/Editor/MapGenerator/RoomTemplates/RoomTemplateValidator.cs)
- [맵 재설계 계획](../Spelunky_Chunk_Map_Plan.md)
