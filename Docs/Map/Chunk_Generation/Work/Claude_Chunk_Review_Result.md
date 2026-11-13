# Chunk 콘텐츠 구조 검토 결과

[검토 요청](Claude_Chunk_Review_Prompt.md)에 따라 `c88bf270..dbad9927`의 Chunk 콘텐츠 설정 분리·함정 설정 정리·Base·Variant 이전과 현재 소스(`cfd87050`, 2026-11-13)를 검토한 결과다. 코드와 에셋은 수정하지 않았다.

## 결론

- 확정된 높은 심각도 버그는 없다. Base·Variant 분리, 식별자 저장, 동일 설정에서의 seed 재현성은 현재 에셋에서 의도대로 유지된다.
- 문제는 **늦게 걸리는 검증**(1번), **함정 검사 누락**(2번), **Inspector·오류 메시지 사용성**(3번)에 모여 있다.
- 코드 구조는 큰 골격이 책임 분리 원칙에 맞다. 어긋나는 곳은 콘텐츠 종류 추가와 검사 규칙 추가 두 방향이다. 상세는 [코드 구조 평가](#코드-구조-평가)에 있다.
- EditMode 테스트와 Play 확인은 실행하지 못했다. 아래의 "확인된 사실"은 소스와 에셋 YAML을 정적으로 대조한 결과다.

각 항목의 **구분**은 다음 뜻이다.

| 구분 | 의미 |
| --- | --- |
| 확인된 사실 | 소스 또는 에셋에서 직접 확인했다. |
| 추론한 위험 | 코드상 가능하지만 현재 에셋·흐름에서는 발생하지 않거나 재현하지 않았다. |
| 미확인 | 실행이 필요해 판단하지 못했다. |

## 발견 사항

심각도가 높은 순서다. 1~8번은 모두 커밋된 변경과 현재 소스에 대한 것이다. 미커밋 변경은 [범위 밖 관찰](#범위-밖-관찰)에서 다룬다.

### 1. 목표 설정 오류가 특정 seed에서만 맵 생성 실패로 드러남 — 중간

- **구분:** 확인된 사실(코드 동작). 현재 에셋에서는 발생하지 않는다.
- **발생 조건:** 아래 중 하나가 있고, 해당 방이 뽑히거나 목표가 선택되는 seed일 때다.
  - 트리거 후보의 ID·참조·Collider 설정 오류 또는 `Controller` 누락
  - 몬스터 후보 프리팹 루트에 활성 `Health`가 없음
  - `Exit Gate Prefab` 누락 또는 문·Collider·Sprite 비활성
- **실제 영향:** `Configure`가 예외를 던지면 `RoomMapFactory`가 맵 전체를 파기한다. `MapManager`는 오류만 기록하고 재시도하지 않으므로 그 Run에는 맵이 없다. 카탈로그 검사와 **Validate Selected**는 목표 설정을 검사하지 않아 통과로 표시된다.
- **근거:**
  - 검사와 예외: [RoomObjectiveSetup.cs:25](../../../../Assets/Scripts/Map/MapRuntime/RoomObjectives/RoomObjectiveSetup.cs#L25), [:36](../../../../Assets/Scripts/Map/MapRuntime/RoomObjectives/RoomObjectiveSetup.cs#L36), [:53](../../../../Assets/Scripts/Map/MapRuntime/RoomObjectives/RoomObjectiveSetup.cs#L53), [:72](../../../../Assets/Scripts/Map/MapRuntime/RoomObjectives/RoomObjectiveSetup.cs#L72)
  - 맵 파기: [RoomMapFactory.cs:46](../../../../Assets/Scripts/Map/MapRuntime/RoomMapFactory.cs#L46)
  - 무재시도: [MapManager.cs:26](../../../../Assets/Scripts/Managers/GameManager/MapManger/MapManager.cs#L26)
  - 검사 메뉴 범위: [RoomTemplateValidator.cs:28](../../../../Assets/Editor/MapGenerator/RoomTemplates/RoomTemplateValidator.cs#L28)
  - `Health` 검사는 목표를 쓰지 않는 선택 방에서도 실행된다([RoomObjectiveSetup.cs:49](../../../../Assets/Scripts/Map/MapRuntime/RoomObjectives/RoomObjectiveSetup.cs#L49)).
- **권장 수정 방향:**
  - `RoomObjectiveSetup`에서 예외 없이 오류 목록을 돌려주는 검사를 분리하고, 검사 메뉴와 카탈로그 전체를 도는 테스트에서 호출한다.
  - 출구 문 프리팹은 `RoomMapFactory.TryCreate` 시작 시 Goal 프리팹과 같은 방식으로 한 번만 검사한다.
  - 몬스터 후보의 루트 `Health`는 카탈로그 전체 테스트에서 확인한다.

### 2. 함정 지지면·공간 검사가 실제 설치 관계를 보장하지 않음 — 중간

- **구분:** 확인된 사실.
- **발생 조건:** 지지면을 위험 공간과 떨어진 곳에 지정하거나, 후보의 Collider가 위험 공간 밖으로 나가는 경우다.
- **실제 영향:** 지지면은 "위험 공간과 겹치지 않고 Grid 충돌 타일이 있다"만, 후보는 기준점이 속한 셀만 검사한다. 공중에 뜬 함정이나 콘텐츠 공간을 침범하는 Collider가 검사를 통과한다.
- **근거:**
  - 공간·지지면 검사: [RoomTemplateChecks.cs:236](../../../../Assets/Scripts/Map/RoomTemplates/RoomTemplateChecks.cs#L236), [:255](../../../../Assets/Scripts/Map/RoomTemplates/RoomTemplateChecks.cs#L255)
  - 후보 위치 검사: [RoomTemplateChecks.cs:238](../../../../Assets/Scripts/Map/RoomTemplates/RoomTemplateChecks.cs#L238)
  - 에셋 예: `Optional_RuinedCourt`의 `optional.saw`는 위험 공간 `(10, 4)` 6×3에 지지면이 `(10, 3)` 1칸이다.
  - 대조: 콘텐츠 슬롯은 설치면에서 지지면을 유도하고 Collider 맞춤까지 검사한다([:288](../../../../Assets/Scripts/Map/RoomTemplates/RoomTemplateChecks.cs#L288), [:316](../../../../Assets/Scripts/Map/RoomTemplates/RoomTemplateChecks.cs#L316)).
- **권장 수정 방향:** 지지면이 위험 공간의 한 변과 맞닿는지 검사한다. 움직이지 않는 함정은 Collider 범위가 위험 공간 안에 있는지 확인한다.
- **현재 에셋:** 함정 후보 7개는 모두 기본 비활성이고, 기준점이 위험 공간 안에 있으며, 지지면이 위험 공간과 인접한다.

### 3. 슬롯 Inspector와 오류 메시지로 문제 위치를 알기 어려움 — 중간(제작 효율)

- **구분:** 확인된 사실.
- **발생 조건:** 슬롯을 추가·수정하거나 검사 오류를 해석할 때다.
- **실제 영향:**
  - 슬롯 필드에 Tooltip이 없다. `clearance`는 `position`을 포함하는 절대 좌표 사각형을 따로 입력해야 한다. 현재 슬롯 25개는 모두 `clearance`의 시작점이 `position`과 같다.
  - Gizmo는 기준점만 그린다. 공간과 지지면은 보이지 않는다. 함정 Gizmo는 둘 다 그린다.
  - 오류 메시지에 슬롯 종류와 번호가 없다(예: "콘텐츠 슬롯의 충돌 공간이 겹칩니다."). 슬롯 ID는 숨겨진 GUID라 제작자가 대상을 찾을 수 없다.
  - 몬스터 `Kind`는 컴포넌트 단위다. `Normal` 방이 선택 방으로 쓰이면 경고 없이 몬스터가 나오지 않는다.
- **근거:**
  - 슬롯 필드와 Gizmo: [RoomSpawnSettings.cs:28](../../../../Assets/Scripts/Map/RoomTemplates/RoomSpawnSettings.cs#L28), [:59](../../../../Assets/Scripts/Map/RoomTemplates/RoomSpawnSettings.cs#L59)
  - 함정 Gizmo: [RoomHazardSettings.cs:15](../../../../Assets/Scripts/Map/RoomTemplates/RoomHazardSettings.cs#L15)
  - 오류 메시지: [RoomTemplateChecks.cs:272](../../../../Assets/Scripts/Map/RoomTemplates/RoomTemplateChecks.cs#L272)
  - 종류·번호를 잃는 변환: [RoomContentLayout.cs:26](../../../../Assets/Scripts/Map/RoomTemplates/RoomContentLayout.cs#L26)
  - 역할 필터: [RoomMonsterSettings.cs:10](../../../../Assets/Scripts/Map/RoomTemplates/RoomMonsterSettings.cs#L10), [RoomContent.cs:109](../../../../Assets/Scripts/Map/RoomTemplates/RoomContent.cs#L109)
- **권장 수정 방향:** 필드에 Tooltip을 추가한다. Gizmo에 공간 사각형과 지지면을 그린다. 오류에 "몬스터 슬롯 #n (x, y)"를 넣는다.

### 4. 식별자를 `OnValidate`에서만 조용히 채움 — 낮음

- **구분:** 추론한 위험.
- **발생 조건:** 스크립트·YAML 편집·병합으로 ID가 비었거나 중복된 슬롯·함정이 저장된 경우다.
- **실제 영향:** 에디터가 로드 시 메모리에서만 새 GUID를 채운다. 검사는 통과하지만 저장하기 전까지 세션마다 ID가 달라져 같은 seed의 결과가 바뀐다. 빌드에서의 동작은 미확인이다.
- **근거:** [RoomSpawnSettings.cs:17](../../../../Assets/Scripts/Map/RoomTemplates/RoomSpawnSettings.cs#L17), [:69](../../../../Assets/Scripts/Map/RoomTemplates/RoomSpawnSettings.cs#L69), [RoomHazardSettings.cs:31](../../../../Assets/Scripts/Map/RoomTemplates/RoomHazardSettings.cs#L31)
- **권장 수정 방향:** 프리팹 에셋에서 ID를 새로 부여할 때 경고를 남기고 `EditorUtility.SetDirty`를 호출한다.
- **현재 에셋:** Variant의 슬롯 25개, 후보 25개, 함정 7개는 모두 ID가 저장돼 있고 중복이 없다.

### 5. 몬스터가 사망 없이 제거되면 문이 열리지 않음 — 낮음

- **구분:** 추론한 위험. 현재는 도달 경로가 없다.
- **발생 조건:** 목표 몬스터가 `IsDead`가 되지 않은 채 `Destroy`되는 경로가 생길 때다.
- **실제 영향:** 목표가 완료되지 않아 주 출구가 계속 잠긴다.
- **근거:**
  - 사망 구독만 사용: [RoomMonsterObjective.cs:53](../../../../Assets/Scripts/Map/MapRuntime/RoomObjectives/RoomMonsterObjective.cs#L53)
  - 완료 집계: [RoomObjectiveController.cs:58](../../../../Assets/Scripts/Map/MapRuntime/RoomObjectives/RoomObjectiveController.cs#L58)
  - 현재 제거는 사망 후에만 일어난다: [MonsterController.cs:160](../../../../Assets/Scripts/FSM/NPC/AIMonstor/@Hub/MonsterController.cs#L160)
- **권장 수정 방향:** 대상이 파괴되면 완료로 처리한다.

### 6. 슬롯 변환 계층과 해시·검사 중복 — 낮음

- **구분:** 확인된 사실. 동작 오류는 아니다.
- **실제 영향:**
  - `RoomSpawnSlot`을 예전 인덱스 방식의 `RoomContentSlot`으로 바꾸고 GUID를 31비트 정수로 해시한다. 해시가 충돌하면 원인을 알 수 없는 "슬롯 중복" 오류가 난다(확률은 극히 낮다).
  - `RoomContentLayout.Read`가 함정마다 다시 호출된다.
  - 맵 생성 한 번에 전체 템플릿 검사가 두 번 실행된다.
  - FNV 해시가 서로 다른 마무리 연산으로 5곳에, ID 부여 코드가 3곳에 있다.
  - 가중치·ID 규칙을 `RoomTemplateChecks`와 `MapPlanValidator.ValidateInput`이 각각 검사한다.
- **근거:**
  - 변환과 정수 해시: [RoomContentLayout.cs:45](../../../../Assets/Scripts/Map/RoomTemplates/RoomContentLayout.cs#L45)
  - 함정마다 재호출: [RoomTemplateChecks.cs:247](../../../../Assets/Scripts/Map/RoomTemplates/RoomTemplateChecks.cs#L247)
  - 검사 2회: [MapGenerator.cs:13](../../../../Assets/Scripts/Map/RoomTemplates/MapGenerator.cs#L13), [MapBuilder.cs:51](../../../../Assets/Scripts/Map/RoomTemplates/MapBuilder.cs#L51)
  - 해시 5곳: [RoomContent.cs:73](../../../../Assets/Scripts/Map/RoomTemplates/RoomContent.cs#L73), [RoomHazard.cs:44](../../../../Assets/Scripts/Map/RoomTemplates/RoomHazard.cs#L44), [RoomTerrainPlan.cs:65](../../../../Assets/Scripts/Map/RoomTemplates/RoomTerrainPlan.cs#L65), [RoomContentLayout.cs:47](../../../../Assets/Scripts/Map/RoomTemplates/RoomContentLayout.cs#L47), [RoomObjectiveSetup.cs:91](../../../../Assets/Scripts/Map/MapRuntime/RoomObjectives/RoomObjectiveSetup.cs#L91)
  - 중복 검사: [MapPlanValidator.cs:79](../../../../Assets/Scripts/Map/RoomTemplates/MapPlanValidator.cs#L79)
- **권장 수정 방향:** `ContentSelection`이 문자열 슬롯 ID를 직접 쓰면 `RoomContentSlot`, `RoomContentOption`, `StableIndex`를 없앨 수 있다. 해시는 공용 함수 하나로 모으고, `MapBuilder`는 이미 만든 스냅샷을 받는다.
- **분포 확인:** 마무리 연산이 다르지만 함정·목표 선택의 분포는 정상이다. seed 2만 개 시뮬레이션에서 각 선택이 약 50%, 두 함정 동시 선택이 약 25%였고 연속 seed 간 상관은 없었다.

### 7. 테스트 누락과 취약점 — 낮음

- **구분:** 확인된 사실.
- **누락:**
  - 목표 설정 실패 경로(1번)와 실패 시 맵 정리
  - 목표 관리자의 비활성화→재활성화 재구독
  - 함정의 콘텐츠·보호 영역·출입구 겹침과 후보 위치 오류
  - 실제 에셋의 Elite 경로(현재 Elite를 쓰는 Variant가 없다)
  - 잠금→처치→개방의 Play 흐름
- **취약:**
  - `seen.Count == 11`과 슬롯 수 1 단정은 Variant에 슬롯을 추가하면 깨진다([RoomSpawnSettingsTests.cs:26](../../../../Assets/Tests/EditMode/MapGeneration/RoomSpawnSettingsTests.cs#L26), [:30](../../../../Assets/Tests/EditMode/MapGeneration/RoomSpawnSettingsTests.cs#L30)).
  - 목표 테스트는 문자열 리플렉션으로 `Assembly-CSharp` 타입에 접근한다. 이름을 바꾸면 컴파일이 아니라 실행 시점에 실패한다([RoomMapPreviewTests.cs:51](../../../../Assets/Tests/EditMode/MapGeneration/RoomMapPreviewTests.cs#L51)).
- **권장 수정 방향:** 누락 항목 중 1번과 2번의 수정에 맞춰 실패 경로와 겹침 검사부터 추가한다. 단정의 고정 숫자는 "주 경로 Variant마다 슬롯이 1개 이상"처럼 제작 변경에 견디는 조건으로 바꾼다.

### 8. 문서 불일치 — 낮음

- **구분:** 확인된 사실.
- [코드 구조 문서](Chunk_Generation_Code_Guide.md)의 `AddOptionalRoom()`은 실제로 `AddOptionalRooms`다([MapPlanner.cs:110](../../../../Assets/Scripts/Map/RoomTemplates/MapPlanner.cs#L110)).
- 같은 문서의 "슬롯 인덱스"는 실제로 자동 ID의 해시이며 목록 순서와 무관하다.
- [맵 생성 코드 안내](../../Map_Code_Architecture_Guide.md)의 표에 `RoomSpawnSlot`, `RoomSpawnOption`, `RoomContentLayout`이 없고, `RoomContentSlot`이 제작 정의로 적혀 있다.
- [사용자 안내서](../Room_Content_User_Guide.md)의 설정 설명은 소스와 맞는다.

## 범위 밖 관찰

### Base의 합성 충돌 캐시가 타일과 다름 — 미확인

Base의 `CompositeCollider2D`에 직렬화된 경로가 고정 타일과 다르다. `WestSouth`는 동쪽 벽 `x=19`가 전부 타일인데 캐시에는 `y` 4~8 구간이 비어 있다. 경로 수를 단순 비교하면 Base 20개 중 10개가 타일 윤곽과 맞지 않는다. 타일과 직접 대조한 것은 `WestSouth`뿐이다.

- **커밋된 변경:** 기준 범위에서 Base의 이 값은 바뀌지 않았으므로 기존 상태다. Variant 4개(`Arrival_TieredGallery`, `Junction_FourWay_Ruins`, `Optional_CapEast_Ruins`, `Optional_CapWest_Ruins`)에는 이 캐시의 Override가 커밋돼 있다.
- **미커밋 변경:** `Down_ForestStep.prefab`의 변경(+164줄)은 이 캐시의 Override 41건이 전부다. 에디터가 재생성한 값으로 보이며 슬롯·함정·목표 값은 바뀌지 않았다.
- **미확인:** Unity가 생성 시 캐시를 다시 계산하는지 확인하지 못했다. 다시 계산하지 않으면 벽에 구멍이 생기고, 다시 계산하면 영향이 없다.
- **권장 방향:** Play에서 해당 벽의 충돌을 확인한다. Base에서 캐시를 재생성해 저장하고 Variant의 Override는 제거한다. Variant별 Override는 Base 지형이 바뀌면 낡은 값으로 남는다.

## 코드 구조 평가

큰 골격은 SOLID·DRY에 맞게 잡혀 있다. 어긋나는 곳은 "콘텐츠 종류 추가"와 "검사 규칙 추가" 두 방향에 몰려 있다. 현재 규모(맵 스크립트 약 2,700줄)에서는 유지보수에 지장을 줄 정도가 아니다.

### 원칙에 맞는 부분

- **단계별 책임 분리:** 제작 설정(컴포넌트) → 스냅샷(순수 데이터) → 선택(Planner) → 계획 → 검증 → 생성(Builder) → 런타임 연결로 나뉜다. Planner는 씬 오브젝트를 참조하지 않아 seed 계산만 따로 테스트할 수 있다.
- **기능별 분리:** 지형·몬스터·아이템·함정 설정이 각각의 컴포넌트이고 Builder도 기능별로 나뉜다.
- **확장에 열린 지점:** 목표 규칙은 ScriptableObject라서 새 규칙을 추가할 때 `RoomObjectiveSetup`을 고치지 않는다. `MapBuildContext`로 생성·파괴 방식을 주입해 런타임과 에디터 미리보기가 같은 코드를 쓴다.
- **의존 방향:** 맵 생성 어셈블리는 게임 코드(`Health`, `PlayerController`)를 참조하지 않는다. 목표 연결만 `MapRuntime`에서 담당한다.

### 어긋나는 부분

| 문제 | 구체적 내용 | 근거 |
| --- | --- | --- |
| 콘텐츠 종류 추가가 닫혀 있지 않음(OCP) | 상자·NPC 같은 종류를 추가하려면 `ContentKind`, `RoomContentLayout.Read`의 고정된 두 `GetComponent`, 필수 컴포넌트 검사, 역할 필터, 몬스터 판별의 다섯 곳을 함께 고쳐야 한다. | [RoomContent.cs:9](../../../../Assets/Scripts/Map/RoomTemplates/RoomContent.cs#L9), [RoomContentLayout.cs:21](../../../../Assets/Scripts/Map/RoomTemplates/RoomContentLayout.cs#L21), [RoomTemplateChecks.cs:24](../../../../Assets/Scripts/Map/RoomTemplates/RoomTemplateChecks.cs#L24), [RoomContent.cs:109](../../../../Assets/Scripts/Map/RoomTemplates/RoomContent.cs#L109), [RoomContentBuilder.cs:36](../../../../Assets/Scripts/Map/RoomTemplates/RoomContentBuilder.cs#L36) |
| 함정이 콘텐츠와 평행한 별도 파이프라인(DRY) | 슬롯·스냅샷·선택·Planner·Builder가 콘텐츠와 같은 모양으로 한 벌 더 있다. 생성과 활성화의 차이가 있어 지금은 타당하지만, 세 번째 종류가 생기면 같은 구조가 또 복제된다. | [RoomHazard.cs](../../../../Assets/Scripts/Map/RoomTemplates/RoomHazard.cs), [RoomContent.cs](../../../../Assets/Scripts/Map/RoomTemplates/RoomContent.cs) |
| 검사가 한 메서드에 몰려 있음(SRP) | `RoomTemplateChecks.Validate`가 약 180줄짜리 한 메서드다. 목표 설정 검사는 반대로 `Configure` 안에 후보 선택·문 생성과 섞여 있어 사전 검사로 꺼내 쓸 수 없다(1번의 원인). | [RoomTemplateChecks.cs:11](../../../../Assets/Scripts/Map/RoomTemplates/RoomTemplateChecks.cs#L11), [RoomObjectiveSetup.cs:23](../../../../Assets/Scripts/Map/MapRuntime/RoomObjectives/RoomObjectiveSetup.cs#L23) |
| 중복 코드(DRY) | 해시 5곳, ID 부여 3곳, 가중치·ID 검사 2곳, 예전 인덱스 방식의 변환 계층(6번). | 6번 근거 |
| 긴 생성자 | `RoomTemplateSnapshot`은 인자 16개, `MapRoomPlan`은 11개다. `MapPlanner`가 방 계획을 갱신할 때마다 전체 인자를 다시 나열하므로 필드를 추가하면 여러 호출부를 고쳐야 한다. | [RoomTemplateSnapshot.cs:30](../../../../Assets/Scripts/Map/RoomTemplates/RoomTemplateSnapshot.cs#L30), [MapPlan.cs:21](../../../../Assets/Scripts/Map/RoomTemplates/MapPlan.cs#L21), [MapPlanner.cs:100](../../../../Assets/Scripts/Map/RoomTemplates/MapPlanner.cs#L100), [:149](../../../../Assets/Scripts/Map/RoomTemplates/MapPlanner.cs#L149) |
| 콘텐츠 후보의 Collider 제약 | 후보는 계층 전체에 Collider가 루트 1개만 있어야 한다. 자식에 감지·공격 Collider를 둔 몬스터는 등록할 수 없다. | [RoomTemplateChecks.cs:317](../../../../Assets/Scripts/Map/RoomTemplates/RoomTemplateChecks.cs#L317) |

### 권장 정리 순서

1. **지금 정리:** 해시 공용 함수, ID 부여 공용화, `RoomContentSlot` 변환 계층 제거, 목표 설정 검사 분리. 코드가 줄어드는 방향이고 seed 결과를 바꾸지 않게 할 수 있다. 변환 계층을 제거할 때는 슬롯 해시 입력이 바뀌지 않도록 기존 `StableIndex` 값을 유지하거나, 과거 seed 결과가 달라지는 것을 받아들일지 먼저 정한다.
2. **필요해질 때 정리:** 콘텐츠 종류의 일반화, 검사 규칙 분할, 생성자 정리. 세 번째 콘텐츠 종류를 실제로 추가하는 시점에 한다. 몬스터·아이템 두 종류뿐인 지금 미리 추상화하면 구조가 과해진다.

## 문제 없음으로 확인한 항목

- **Base·Variant:** Base 20개는 몬스터·아이템 슬롯, 함정, 목표가 비어 있다. 카탈로그 21개는 모두 Variant이고 각각 Base의 프리팹 Variant다. 제거된 컴포넌트나 끊긴 Override 대상이 없다. Variant의 Override는 템플릿 ID·이름·슬롯·함정·배경이며, 예외는 가로 Variant의 보호 영역(문서화됨)과 선택·분기 Variant의 시작·Goal 마커 해제다.
- **독립 선택과 역할:** 슬롯마다 seed·방 좌표·템플릿 ID·슬롯 ID로 따로 해시한다. 일반 방은 아이템과 일반 몬스터만, 선택 방은 아이템과 엘리트만 허용한다. 시작·Goal·닫힌 방은 선택 대상이 아니다.
- **재현성:** 슬롯·후보·함정이 ID 순으로 정렬되므로 목록 재정렬은 결과를 바꾸지 않는다. 가중치 변경, 후보 추가·삭제, 슬롯 재생성, 템플릿 ID 변경은 과거 seed의 결과를 바꾼다(의도된 동작).
- **함정 참조:** 이전된 함정 후보 7개는 모두 Variant 안의 중첩 프리팹이고 `m_IsActive = 0`이 Override돼 있다. 끊긴 참조가 없다.
- **목표 흐름:** 실제 생성된 몬스터만 목표 후보로 전달된다. 적용할 규칙이 없으면 문을 만들지 않는다. 문은 주 출구에만 놓이고, 이벤트 구독은 비활성화 시 해제된다.

## 검토 범위와 검사

- **실행한 검사:**
  - `git status --short`와 기준 범위 diff 확인
  - `Assets/Scripts/Map/RoomTemplates/`, `MapRuntime/` 소스 전체와 관련 테스트 읽기
  - Base 20개, Variant 21개, 카탈로그, 맵 설정의 YAML 대조(스크립트로 Override·참조·ID 집계)
  - 참고 문서 3종과 소스 대조
  - 함정·목표 선택 해시의 분포 시뮬레이션
- **실행하지 않은 검사:**
  - EditMode 테스트(Unity 에디터 미연결)와 Play 확인
  - 빌드에서의 ID 동작
  - Goblin 프리팹의 Collider·`Health` 구성 직접 확인
  - 미추적 씬 `MapChunkEditor.unity`의 내용
  - 미리보기 맵을 씬에 저장했다가 다시 열었을 때의 목표 표식 상태
