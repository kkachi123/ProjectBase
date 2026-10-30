# 맵 생성 코드 안내

맵은 Chunk를 선택·연결하고, 선택된 Chunk의 내부 지형과 콘텐츠를 정한 뒤 씬에 생성한다. 전체 흐름과 주요 클래스는 이 문서에서 확인하고, 상세 관계와 소스 읽는 순서는 각 영역의 코드 구조 문서를 참고한다.

```text
Chunk 원본·카탈로그 → 맵 경로와 Chunk 선택 → Chunk 내부 선택 → 씬 생성 → Run 연결
```

`RoomTemplate`이 Chunk 제작 원본이며, `RoomTemplateSnapshot`은 계획 계산용 복사본이다. `MapPlan`은 선택 결과를 기록하고 `MapBuilder`가 이를 씬에 적용한다. 맵 계획 중 내부 선택을 요청하므로 두 영역은 같은 클래스 일부를 사용한다.

## 문서 읽는 순서

1. **전체 구조:** 이 문서에서 맵 조합과 Chunk 내부 조합의 역할을 구분한다.
2. **맵 조합:** [맵 조합 코드 구조](Map_Assembly/Map_Assembly_Code_Guide.md)에서 경로 생성·연결 검증·씬 생성·Run 연결의 클래스와 호출 순서를 살펴본다. 사용 방법은 [맵 연결 계획](Map_Assembly/Map_Planning_Guide.md)을 참고한다.
3. **Chunk 내부:** [Chunk 내부 조합 코드 구조](Chunk_Generation/Work/Chunk_Generation_Code_Guide.md)에서 템플릿 데이터·패턴과 확률 셀·콘텐츠 선택을 살펴본다. 프리팹 작성과 이동 검증은 [방 템플릿 제작 규격](Chunk_Generation/Work/Room_Template_Authoring_Guide.md)을 참고한다. 설명서와 작업 계획의 구분은 [Chunk 내부 생성 문서 안내](Chunk_Generation/README.md)에 있다.
4. **그래픽과 진행 현황:** [방 지형·배경·장식 제작 규칙](Chunk_Generation/Work/Room_Art_Authoring_Guide.md)과 [맵 작업 진행 현황](Map_Progress.md)을 확인한다. 전체 목표는 [맵 재설계 계획](Spelunky_Chunk_Map_Plan.md), 작업 이력은 [맵 제작·리팩토링 로그](Map_Refactoring_Log.md)에 있다.

## 맵 조합에 사용하는 주요 클래스

| 클래스 | 역할 |
| --- | --- |
| `MapPlanningSettings`, `RoomTemplateCatalog` | 격자·생성 설정과 사용할 Chunk 원본 목록을 제공한다. |
| `RoomTemplateSnapshot` | 경로와 Chunk 선택에 필요한 읽기 전용 정보를 제공한다. |
| `MapPlanner`, `RoomDirection` | 주 경로·방향·조건에 맞는 Chunk와 선택 방을 정한다. |
| `MapPlan`, `MapRoomPlan`, `MapPlanValidator` | 맵과 방별 선택 결과를 저장하고 연결 조건을 검사한다. |
| `MapGenerator`, `MapBuilder`, `MapBuildContext` | 계획과 씬 생성을 조율하고 선택된 방을 배치한다. |
| `MapVisualBuilder` | 확정된 지형을 공통 그래픽 Tilemap에 표시한다. |
| `RoomMapFactory`, `GeneratedMapInfo`, `MapManager` | 생성 결과를 Run에서 사용할 맵 정보로 연결하고 현재 맵을 관리한다. |
| `RoomMapGeneratorWindow` | 같은 생성 흐름으로 에디터 미리보기를 만든다. |
| `AdventureRunManager`, `PlayerSpawnManager`, `GoalTrigger` | 생성된 맵의 Player 시작과 Run 종료를 연결한다. |

## Chunk 내부 조합에 사용하는 주요 클래스

| 클래스 | 역할 |
| --- | --- |
| `RoomTemplate`, `RoomTemplateChecks`, `RoomTemplateValidator` | Chunk 원본과 제작 데이터 검사를 담당한다. |
| `RoomTerrainVariation`, `RoomTerrainPattern`, `RoomChanceCell` | 내부 변형 영역의 패턴과 확률 셀을 정의한다. |
| `RoomTerrainSnapshot`, `RoomTerrainPlanner`, `RoomTerrainSelection` | 지형 선택지를 계산용 데이터로 변환하고 선택 결과를 기록한다. |
| `RoomContentSlot`, `RoomContentOption`, `RoomContentPlanner`, `ContentSelection` | 콘텐츠 슬롯과 후보를 정의하고 선택 결과를 기록한다. |
| `MapPlanner`, `MapBuilder` | 각각 내부 선택을 요청하고 확정된 선택을 실제 Chunk에 적용한다. |

현재 내부 지형 변형은 샘플 가로방 한 곳에서 설정·검증됐으며, Nova 시제품 방의 내부 지형은 고정돼 있다. 콘텐츠 선택은 주 경로의 중간 방과 선택 방에 적용되며, 실제 배치는 슬롯이 설정된 방에서만 일어난다.

문서의 구분은 책임을 설명하기 위한 것이다. 실제 생성 계산 코드는 [RoomTemplates](../../Assets/Scripts/Map/RoomTemplates/)에 함께 있으며, 런타임 연결 코드는 [MapRuntime](../../Assets/Scripts/Map/MapRuntime/) 등에 있다.
