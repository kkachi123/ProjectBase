# Room Template 사용 관계 조사

기준 위치는 [RoomTemplates](../../../../Assets/Prefabs/Map/RoomTemplates/)다. 카탈로그의 직렬화된 GUID, 프리팹의 출입구·검증 표시, 씬 참조와 코드의 에셋 경로를 대조했다. 카탈로그 등록은 **생성 후보**라는 뜻이며 모든 seed에서 해당 방이 선택된다는 뜻은 아니다.

## 설정과 카탈로그

| 설정 | 연결과 사용 | 카탈로그 구성 |
| --- | --- | --- |
| [기본 설정](../../../../Assets/Prefabs/@Data/Map/MapPlanningSettings.asset) | [Title 씬](../../../../Assets/Scenes/Build/Title.unity)의 `MapManager`, 에디터 미리보기 기본값, 맵 테스트 | [기본 카탈로그](../../../../Assets/Prefabs/@Data/Map/RoomTemplateCatalog.asset): Nova Variant 17개 |

기본 설정은 `Nova_Ground_Prototype` 그래픽 타일과 지형 머티리얼을 지정한다. 설정은 Inspector나 에디터에서 바꿀 수 있으므로 파일 참조 조사만으로 모든 실행 시 사용을 단정하지 않는다.

## 방별 역할과 출입구

표의 N·E·S·W는 각각 북·동·남·서다. 화살표는 프리팹에 기록된 진입→진출 방향이며, 시작·Goal·왕복은 해당 마커와 연결된 검증 방향이다. 방향별 실제 검증 과정과 제한은 [템플릿 제작 규격](Room_Template_Authoring_Guide.md)의 방별 기록을 따른다.

### Base — 기본 방 5개

| 프리팹 | 열린 면 | 역할과 기록된 방향 |
| --- | --- | --- |
| `Side` | 없음 | 경로 밖의 닫힌 방 |
| `Horizontal` | E, W | W→E, E→W; 내부 패턴과 확률 셀 사용 |
| `Down` | E, S, W | W→E, E→W, W→S, E→S |
| `DownThrough` | N, E, S, W | W→E, E→W, W→S, E→S, N→W, N→E, N→S |
| `Arrival` | N, E, W | W→E, E→W, N→W, N→E |

### Base — 출입구 변형 9개와 역할 구분 5개

| 프리팹 | 열린 면 | 역할과 기록된 방향 |
| --- | --- | --- |
| `CapEast` | E | 시작 E, Goal E, 선택 방 왕복 E |
| `CapWest` | W | 시작 W, Goal W, 선택 방 왕복 W |
| `CapNorth` | N | Goal N |
| `CapSouth` | S | 시작 S |
| `EastSouth` | E, S | E→S |
| `WestSouth` | S, W | W→S |
| `NorthEast` | N, E | N→E |
| `NorthWest` | N, W | N→W |
| `NorthSouth` | N, S | N→S |
| `StartEast`, `StartWest` | E 또는 W | 해당 방향 시작 전용 |
| `GoalEast`, `GoalWest` | E 또는 W | 해당 방향 Goal 전용 |
| `OptionalWest` | W | 선택 방 왕복 W; 콘텐츠 슬롯 사용 |

### NovaPrototype — Nova 시제품 14개

| 프리팹 | 열린 면 | 역할과 기록된 방향 |
| --- | --- | --- |
| `Start_ForestGateway` | E | 시작 E |
| `Start_ShrineWest` | W | 시작 W |
| `Start_ForestDrop` | S | 시작 S |
| `Goal_RuinedSanctuary` | W | Goal W |
| `Goal_OvergrownCourt` | E | Goal E |
| `Goal_ForestDescent` | N | Goal N |
| `Horizontal_Step` | E, W | W→E, E→W |
| `Horizontal_Shrine` | E, W | W→E, E→W |
| `Down_ForestStep` | S, W | W→S |
| `Down_RuinedOverlook` | E, S | E→S |
| `Arrival_RuinedGallery` | N, W | N→W |
| `Arrival_ShrineAtrium` | N, E | N→E |
| `DownThrough_RuinedShaft` | N, S | N→S |
| `Optional_RuinedCourt` | W | 선택 방 왕복 W; 콘텐츠 슬롯 사용 |

이 14개 방의 내부 지형 변형은 비어 있다.

### NovaVariants — Base 상속 방 17개

기존 Nova 시제품 방 14개에 대응하는 Variant와 `ClosedFill`, `Junction_Arrival`, `Junction_Down`이 기본 카탈로그에 등록돼 있다. 사당·숲 가로 Variant는 `Base/Horizontal`을 공유한다. 분기 방과 일부 인접 방의 배경 이음새·장식 배치를 수정했으며, 확인한 그래픽 및 Player 검증 범위와 남은 한계는 [진행 현황](../Chunk_Generation_Progress.md)을 참고한다.

| Variant | 부모 Base |
| --- | --- |
| `Start_ForestGateway`, `Start_ShrineWest`, `Start_ForestDrop` | `StartEast`, `StartWest`, `CapSouth` |
| `Goal_OvergrownCourt`, `Goal_RuinedSanctuary`, `Goal_ForestDescent` | `GoalEast`, `GoalWest`, `CapNorth` |
| `Optional_RuinedCourt` | `OptionalWest` |
| `Horizontal_Forest`, `Horizontal_Shrine` | `Horizontal` |
| `Down_ForestStep`, `Down_RuinedOverlook` | `WestSouth`, `EastSouth` |
| `Arrival_RuinedGallery`, `Arrival_ShrineAtrium` | `NorthWest`, `NorthEast` |
| `DownThrough_RuinedShaft` | `NorthSouth` |
| `ClosedFill`, `Junction_Arrival`, `Junction_Down` | `Side`, `Arrival`, `Down` |

## 공용 에셋과 직접 참조

- [SampleSolid.asset](../../../../Assets/Prefabs/Map/RoomTemplates/Shared/SampleSolid.asset)과 [SampleSolid.png](../../../../Assets/Prefabs/Map/RoomTemplates/Shared/SampleSolid.png)는 Base와 NovaPrototype의 충돌 지형에 사용되며 Nova Variant도 부모 지형을 상속한다. `OptionalRoomTests`도 타일을 경로로 직접 읽는다.
- [Legacy_EW1_Platform](../../../../Assets/Prefabs/Map/RoomTemplates/Patterns/Legacy_EW1_Platform.prefab)은 `Base/Horizontal`이 지형 패턴으로 참조한다. 카탈로그에 직접 등록된 방은 아니지만 내부 조합에 사용된다.
- [RoomTemplateMovementSample](../../../../Assets/Scenes/Tests/RoomTemplateMovementSample.unity)은 `Horizontal`, `Down`, `DownThrough`, `Arrival` Base 프리팹을 직접 참조한다.
- [RoomTerrainTests](../../../../Assets/Tests/EditMode/MapGeneration/RoomTerrainTests.cs)는 `Base/DownThrough`, `Base/Horizontal`을, [OptionalRoomTests](../../../../Assets/Tests/EditMode/MapGeneration/OptionalRoomTests.cs)는 `Base/CapWest`와 `Shared/SampleSolid.asset`을 경로로 직접 읽는다.
- [MapVisualIntegrationTests](../../../../Assets/Tests/EditMode/MapGeneration/MapVisualIntegrationTests.cs)는 기본 설정과 `NovaVariants` 프리팹 및 seed별 결과를 사용한다. [RoomMapPreviewTests](../../../../Assets/Tests/EditMode/MapGeneration/RoomMapPreviewTests.cs)도 기본 설정과 특정 방 ID를 사용한다. 경로·ID 또는 카탈로그 구성이 바뀌면 테스트의 기대 결과를 살펴야 한다.
- 기본 설정·카탈로그 경로는 `MapBuilderTests`, `MapPlannerTests`, `OptionalRoomTests`, `RoomTerrainTests`와 에디터 창 기본 설정에서 사용한다. 각 테스트와 에디터 코드는 [MapGeneration 테스트](../../../../Assets/Tests/EditMode/MapGeneration/)와 [에디터 도구](../../../../Assets/Editor/MapGenerator/RoomTemplates/)에서 확인한다.

## 폴더 구성

폴더는 출처보다 현재 역할에 맞춰 구분한다.

```text
RoomTemplates/
  Base/            기본 방·출입구 변형·역할 구분 19개
  NovaPrototype/   Nova 시제품 14개 방
  NovaVariants/    Base를 상속하는 Nova Variant
  Patterns/        내부 변형 패턴
  Shared/          공용 지형 타일과 Sprite
```

`Base`의 방은 Variant의 부모이며 일부는 이동 검증에도 쓰인다. 별도로 보관한 `NovaPrototype` 시제품은 기본 카탈로그에 등록돼 있지 않다. 방의 기능은 현재 프리팹 이름과 데이터로 구별할 수 있으므로 방마다 별도 폴더를 만들지는 않는다.

폴더 정리 시 프리팹·타일·Sprite의 GUID, 템플릿 ID와 카탈로그 구성을 유지했다. 테스트의 하드코딩 경로와 문서 링크도 함께 갱신했다. 방 지형이나 Collider를 바꾸는 작업은 별도의 이동 검증이 필요하다.
