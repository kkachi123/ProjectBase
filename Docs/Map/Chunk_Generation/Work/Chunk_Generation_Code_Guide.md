# Chunk 내부 조합 코드 구조

이 문서는 선택된 Chunk의 고정 지형, 변형 패턴·확률 셀과 콘텐츠·함정을 계획하고 적용하는 코드를 설명한다. 실제 출입구·영역·슬롯 작성 방법은 [방 템플릿 제작 규격](Room_Template_Authoring_Guide.md)을, Chunk 간 경로와 연결은 [맵 조합 코드 구조](../../Map_Assembly/Map_Assembly_Code_Guide.md)를 참고한다.

## 현재 구현 범위

`RoomTemplate`은 미리 제작한 기본 방을 보관한다. 생성 시 등록된 변형 영역마다 패턴 하나 또는 빈 패턴을 고르고, 지정된 확률 셀의 타일 채움 여부를 결정한다. 임의의 모든 셀을 자동으로 채우지는 않는다. 콘텐츠는 주 경로의 중간 방과 선택 방에서 최종 방 역할에 맞게 결정한다.

내부 지형 변형은 `sample.horizontal`의 고정 발판 패턴과 확률 셀, `NorthWest_Tiered`의 낙하 발판 패턴에 설정돼 있다. 실제 카탈로그의 Variant는 Base 설정을 상속한다. 검증 범위는 [진행 현황](../Chunk_Generation_Progress.md)을 따른다.

방 목표는 `WestSouth`와 `Down` Base 및 상속 Variant에 설정돼 있다. `RoomMapFactory`가 실제 생성 결과를 `RoomObjectiveSetup`에 전달하면 적용 가능한 규칙 중 하나를 seed로 선택한다.

## 클래스 관계

화살표는 호출하거나 사용하는 관계이고 `*--`는 데이터 포함 관계다. 맵 계획과 씬 생성은 내부 선택의 호출·적용 지점으로 표시한다.

```mermaid
classDiagram
    RoomTemplate ..> RoomTerrainSettings : 지형 설정
    RoomTemplate ..> RoomContentSettings : 콘텐츠 설정
    RoomTemplate ..> RoomHazardSettings : 함정 설정
    RoomTerrainSettings *-- RoomTerrainVariation : 변형 설정
    RoomTerrainVariation *-- RoomTerrainPattern : 패턴 후보
    RoomTerrainVariation *-- RoomChanceCell : 확률 셀
    RoomContentSettings *-- RoomContentSlot : 콘텐츠 슬롯
    RoomHazardSettings *-- RoomHazardSlot : 함정 후보
    RoomContentSlot *-- RoomContentOption : 콘텐츠 후보
    RoomTemplateCatalog ..> RoomTemplateChecks : 원본 검사
    RoomTemplateCatalog ..> RoomTemplateSnapshot : 계산 데이터 생성
    RoomTemplateSnapshot *-- RoomTerrainSnapshot : 지형 선택지
    RoomTemplateSnapshot *-- ContentSlotSnapshot : 슬롯 선택지
    RoomTemplateSnapshot *-- RoomHazardSnapshot : 함정 가중치
    ContentSlotSnapshot *-- ContentOptionSnapshot : 후보 ID와 가중치
    MapPlanner ..> RoomTerrainPlanner : 지형 선택
    MapPlanner ..> RoomContentPlanner : 방 역할별 콘텐츠
    MapPlanner ..> RoomHazardPlanner : 함정 또는 빈 상태
    RoomTerrainPlanner ..> RoomTerrainSelection : 결과
    RoomContentPlanner ..> ContentSelection : 결과
    RoomHazardPlanner ..> RoomHazardSelection : 결과
    MapRoomPlan *-- RoomTerrainSelection : 방별 지형 선택
    MapRoomPlan *-- ContentSelection : 방별 콘텐츠 선택
    MapRoomPlan *-- RoomHazardSelection : 방별 함정 선택
    MapBuilder ..> MapRoomPlan : 선택 적용
    MapBuilder ..> RoomTemplate : 원본 프리팹과 설정 조회
    MapBuilder ..> RoomTerrainBuilder : 지형 적용
    MapBuilder ..> RoomContentBuilder : 콘텐츠 생성
    MapBuilder ..> RoomHazardBuilder : 함정 활성화
    MapBuilder ..> BuiltRoom : 생성된 방과 콘텐츠
    RoomMapFactory ..> RoomObjectiveSetup : 실제 목표 연결
    RoomObjectiveSetup ..> RoomObjectiveRule : 적용 가능 규칙 선택
    RoomObjectiveRule ..> RoomObjectiveCandidates : 실제 후보로 목표 생성
    RoomObjectiveSetup ..> RoomObjectiveController : 목표와 출구 구성
    RoomObjectiveSetup ..> RoomExitGate : 주 출구에 생성
    RoomObjectiveController ..> RoomExitGate : 목표 완료 시 개방
    RoomTemplateValidator ..> RoomTemplateChecks : 제작 데이터 검사
```

## 선택과 적용 순서

1. 제작자는 `RoomTemplate`에 고정 Tilemap·출입구·마커와 검증 표시를 지정한다. 변형·보호 영역, 콘텐츠 슬롯과 함정 후보는 각각 `RoomTerrainSettings`, `RoomContentSettings`, `RoomHazardSettings`에 지정한다.
2. `RoomTemplateCatalog.TryCreateSnapshot()`이 `RoomTemplateChecks`로 원본을 검사하고, 계산에 필요한 ID·확률·가중치를 `RoomTemplateSnapshot`과 내부 스냅샷으로 복사한다. 스냅샷에는 생성할 프리팹 참조를 담지 않는다.
3. `MapPlanner`가 해당 칸의 방을 고르면 `RoomTerrainPlanner.Select()`가 영역별 패턴 ID와 채울 셀을 선택한다. 결과는 `MapRoomPlan.Terrain`에 저장된다. 지형 선택에는 seed, 방 좌표, 템플릿 ID와 영역 인덱스를 사용한다.
4. 경로와 선택 방이 확정되면 `RoomContentPlanner.Select()`가 중간 경로 방과 선택 방의 슬롯별 콘텐츠 ID 또는 빈 선택을 정한다. 일반 방에는 아이템·일반 몬스터, 선택 방에는 아이템·엘리트 몬스터가 허용된다. 시작·Goal과 닫힌 방에는 콘텐츠를 선택하지 않는다. 콘텐츠 난수에는 seed, 방 좌표, 템플릿 ID와 슬롯 인덱스를 사용한다.
5. `RoomHazardPlanner.Select()`가 중간 경로 방과 선택 방의 함정 후보별 생성 또는 빈 상태를 독립 난수로 정한다. seed·방 좌표·템플릿 ID·후보 ID를 사용하며 결과는 `MapRoomPlan.Hazards`에 저장된다.
6. `MapPlanValidator`가 기록된 지형·콘텐츠·함정 선택을 재계산해 계획과 일치하는지 확인한다. `MapBuilder`는 각 Builder에 지형·콘텐츠·함정 적용을 맡긴다.
7. 그래픽 타일이 설정된 경우 `MapVisualBuilder`가 방과 Tilemap 패턴의 충돌 지형을 수집해 맵 전체의 그래픽 Tilemap에 표시한다. 플랫폼 패턴은 별도 오브젝트로 생성되어 자체 Sprite와 동작을 사용한다. RuleTile은 이웃 셀에 따라 그래픽을 고르며 지형의 점유 여부를 결정하지 않는다.
8. `RoomMapFactory`는 비활성 맵의 `BuiltRoom`과 공통 문 프리팹을 `RoomObjectiveSetup`에 전달한다. 실제 몬스터와 확률로 선별된 트리거를 모아 적용 가능한 규칙 하나를 선택한다. 목표가 있으면 `MapRoomPlan.Exit`과 `RoomTemplate.OpeningCells()`를 기준으로 문 하나를 생성한다. `RoomObjectiveController`가 목표 완료를 집계하고 `RoomExitGate`에 개방을 요청한다.

지형·콘텐츠·함정 선택 난수는 주 경로·방 선택과 분리된다. 단, 카탈로그나 템플릿 ID 등의 입력을 변경하면 기존 seed의 결과가 달라질 수 있다.

## 클래스와 소스

| 클래스 | 소스 | 역할 |
| --- | --- | --- |
| `RoomTemplate` | [RoomTemplate.cs](../../../../Assets/Scripts/Map/RoomTemplates/RoomTemplate.cs) | 방 ID·유형·출입구·통과 방향, 고정 Tilemap과 마커를 보관한다. `OpeningCells()`가 출입구 셀의 기준이다. |
| `RoomTerrainSettings`, `RoomContentSettings`, `RoomHazardSettings` | [RoomTerrainSettings.cs](../../../../Assets/Scripts/Map/RoomTemplates/RoomTerrainSettings.cs), [RoomContentSettings.cs](../../../../Assets/Scripts/Map/RoomTemplates/RoomContentSettings.cs), [RoomHazardSettings.cs](../../../../Assets/Scripts/Map/RoomTemplates/RoomHazardSettings.cs) | 지형·콘텐츠·함정 제작 설정을 각각 보관한다. |
| `RoomTerrainBuilder`, `RoomContentBuilder`, `RoomHazardBuilder` | [RoomTerrainBuilder.cs](../../../../Assets/Scripts/Map/RoomTemplates/RoomTerrainBuilder.cs), [RoomContentBuilder.cs](../../../../Assets/Scripts/Map/RoomTemplates/RoomContentBuilder.cs), [RoomHazardBuilder.cs](../../../../Assets/Scripts/Map/RoomTemplates/RoomHazardBuilder.cs) | 선택된 지형·콘텐츠·함정을 각각 적용한다. |
| `RoomType`, `RoomOpening`, `RoomPassage` | [RoomTemplate.cs](../../../../Assets/Scripts/Map/RoomTemplates/RoomTemplate.cs) | 방 유형·출입구 방향·진입→진출 후보를 표현한다. |
| `RoomTerrainVariation`, `RoomTerrainPattern`, `RoomChanceCell` | [RoomTerrainSettings.cs](../../../../Assets/Scripts/Map/RoomTemplates/RoomTerrainSettings.cs) | 변형 영역의 패턴 후보와 확률 셀·검증 표시를 보관한다. |
| `RoomContentSlot`, `RoomContentOption`, `ContentKind`, `ContentRoomRole` | [RoomContent.cs](../../../../Assets/Scripts/Map/RoomTemplates/RoomContent.cs) | 슬롯의 공간·설치면·방향, 후보 종류·가중치와 방 역할을 정의한다. |
| `RoomTemplateChecks` | [RoomTemplateChecks.cs](../../../../Assets/Scripts/Map/RoomTemplates/RoomTemplateChecks.cs) | 원본의 지형·경계·마커·변형 및 슬롯 규격을 검사한다. |
| `RoomTemplateValidator` | [RoomTemplateValidator.cs](../../../../Assets/Editor/MapGenerator/RoomTemplates/RoomTemplateValidator.cs) | 에디터 메뉴에서 선택한 방의 제작 데이터를 검사한다. |
| `RoomTemplateCatalog`, `RoomTemplateSnapshot` | [RoomTemplateCatalog.cs](../../../../Assets/Scripts/Map/RoomTemplates/RoomTemplateCatalog.cs), [RoomTemplateSnapshot.cs](../../../../Assets/Scripts/Map/RoomTemplates/RoomTemplateSnapshot.cs) | 프리팹 목록을 검사하고 계산용 방 데이터를 만든다. |
| `RoomTerrainSnapshot`, `RoomTerrainPlanner`, `RoomTerrainSelection` | [RoomTerrainPlan.cs](../../../../Assets/Scripts/Map/RoomTemplates/RoomTerrainPlan.cs) | 지형 선택지의 복사본, 선택 계산과 확정된 패턴·셀을 담당한다. |
| `ContentOptionSnapshot`, `ContentSlotSnapshot`, `RoomContentPlanner`, `ContentSelection` | [RoomContent.cs](../../../../Assets/Scripts/Map/RoomTemplates/RoomContent.cs) | 콘텐츠 선택지의 복사본, 가중치 계산과 확정된 슬롯 결과를 담당한다. |
| `RoomHazardSlot`, `RoomHazardSnapshot`, `RoomHazardPlanner`, `RoomHazardSelection` | [RoomHazard.cs](../../../../Assets/Scripts/Map/RoomTemplates/RoomHazard.cs) | 함정 후보와 공간, 계산용 가중치, 생성 여부 선택과 결과를 담당한다. |
| `TrapContactDamage` | [TrapContactDamage.cs](../../../../Assets/Scripts/Trap/TrapContactDamage.cs) | 대상 레이어의 접촉 진입마다 피해를 주고 한 대상의 여러 Collider를 중복 집계하지 않는다. Spikes·Fire·Saw·SpikeHead에 연결돼 있다. |
| `FireTrap` | [FireTrap.cs](../../../../Assets/Scripts/Trap/FireTrap.cs) | 점화·소화 시간을 관리하고 불꽃 Sprite·Animator·접촉 피해와 Collider를 전환한다. |
| `SawPatrol` | [SawPatrol.cs](../../../../Assets/Scripts/Trap/SawPatrol.cs) | 물리 갱신에서 톱니를 시작점과 지정된 거리 사이로 왕복시킨다. 재활성화하면 처음 위치로 돌아간다. |
| `SpikeHeadTrap` | [SpikeHeadTrap.cs](../../../../Assets/Scripts/Trap/SpikeHeadTrap.cs) | 아래 Player 감지, 낙하 예고·지형 또는 거리 제한에 따른 정지·복귀·재작동 대기를 처리한다. 접촉 피해는 `TrapContactDamage`에 맡긴다. |
| `RoomObjective`, `RoomTriggerObjective`, `RoomMonsterObjective` | [RoomObjectives](../../../../Assets/Scripts/Map/MapRuntime/RoomObjectives/) | 목표의 완료 이벤트, 지점·스위치 조건 및 지정 몬스터의 `Health.IsDead` 구독을 담당한다. |
| `IRoomObjectiveRule`, `RoomObjectiveRule`, `RoomObjectiveCandidates` | [RoomObjectiveRule.cs](../../../../Assets/Scripts/Map/MapRuntime/RoomObjectives/RoomObjectiveRule.cs) | 규칙의 적용 가능 여부와 목표 생성 계약 및 실제 후보를 제공한다. 네 규칙 에셋은 상태를 보관하지 않는다. |
| `RoomObjectiveSetup`, `BuiltRoom` | [RoomObjectiveSetup.cs](../../../../Assets/Scripts/Map/MapRuntime/RoomObjectives/RoomObjectiveSetup.cs), [MapBuilder.cs](../../../../Assets/Scripts/Map/RoomTemplates/MapBuilder.cs) | 실제 생성된 콘텐츠와 목표 후보를 연결하고 진행 출구를 선택한다. |
| `RoomObjectiveController`, `RoomPresenceSensor`, `RoomObjectivePrototypeDisplay` | [RoomObjectives](../../../../Assets/Scripts/Map/MapRuntime/RoomObjectives/) | 목표를 집계해 문을 제어하고 Player 체류와 임시 목표 표시를 연결한다. 정식 UI는 별도 작업이다. |
| `RoomExitGate` | [RoomExitGate.cs](../../../../Assets/Scripts/Map/MapRuntime/RoomObjectives/RoomExitGate.cs) | 주 출구에 배치되고 완료 시 Collider를 해제한 뒤 DOTween으로 페이드한다. 비활성화 시 진행 중인 Tween을 정리한다. |
| `MapPlanner`, `MapRoomPlan`, `MapPlanValidator` | [MapPlanner.cs](../../../../Assets/Scripts/Map/RoomTemplates/MapPlanner.cs), [MapPlan.cs](../../../../Assets/Scripts/Map/RoomTemplates/MapPlan.cs), [MapPlanValidator.cs](../../../../Assets/Scripts/Map/RoomTemplates/MapPlanValidator.cs) | 내부 선택을 요청·기록하고 완성 계획을 검증한다. |
| `MapBuilder`, `MapVisualBuilder` | [MapBuilder.cs](../../../../Assets/Scripts/Map/RoomTemplates/MapBuilder.cs), [MapVisualBuilder.cs](../../../../Assets/Scripts/Map/RoomTemplates/MapVisualBuilder.cs) | 확정된 내부 선택을 생성하고 선택적 공통 지형 그래픽을 만든다. |

## 검증과 코드 읽는 순서

1. `RoomTemplate`의 고정 지형과 기능별 설정 컴포넌트, `RoomTemplateChecks.Validate()`를 읽는다.
2. `RoomTemplateCatalog.TryCreateSnapshot()`에서 원본의 어떤 값을 복사하는지 확인한다.
3. `MapPlanner.TrySelect()`와 `AddOptionalRoom()`에서 지형과 콘텐츠 선택을 요청하는 지점을 찾는다. 각 선택의 난수 계산과 결과는 `RoomTerrainPlanner`와 `RoomContentPlanner`에서 확인한다.
4. `MapBuilder.Build()`에서 지형·콘텐츠·함정 Builder의 호출을 찾는다. 그래픽과 Collider의 분리는 `MapVisualBuilder`에서 확인한다.
5. [RoomTerrainTests.cs](../../../../Assets/Tests/EditMode/MapGeneration/RoomTerrainTests.cs), [OptionalRoomTests.cs](../../../../Assets/Tests/EditMode/MapGeneration/OptionalRoomTests.cs), [MapVisualBuilderTests.cs](../../../../Assets/Tests/EditMode/MapGeneration/MapVisualBuilderTests.cs)를 참고한다.

`Passages Verified`, `Combinations Verified`, 함정의 `passageVerified`와 시작·Goal·왕복 방향은 실제 Player로 확인한 뒤 제작자가 기록한다. 지형이나 함정 발사 조건을 변경해도 표시가 자동 해제되지 않으며, 정적 검사만으로 투사체 범위나 Player 통과가 보장되지는 않는다. 함정 선택·안전 검사와 빌드 예시는 [RoomHazardTests.cs](../../../../Assets/Tests/EditMode/MapGeneration/RoomHazardTests.cs)를 참고한다.
