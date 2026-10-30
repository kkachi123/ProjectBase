# 맵 제작·리팩토링 로그

맵 기능의 변경 전 구조, 변경 이유, 변경 후 책임과 검증 결과를 기록한다. 현재 사용법과 클래스 정보는 [맵 생성 코드 안내](Map_Code_Architecture_Guide.md)를 따른다.

## 기록 방법

- 작업 전에 대상과 기존 동작, 해결할 문제, 영향 범위를 기록한다.
- 작업 후 실제 변경된 구조와 선택한 이유, 확인한 결과와 미검증 사항을 기록한다. 완료하지 않은 작업은 예정 또는 진행 중으로 표시한다.
- 기존 구현의 경로가 삭제되면 기준 커밋을 남긴다. 상세한 변경 내용은 Git과 연결된 가이드를 참고한다.

## 방 템플릿 기반 맵 생성 전환

**상태:** 구현 및 검증 완료. 기준 커밋 `ea1c43a6`의 부모가 변경 전, `ea1c43a6`이 변경 후다.

### 제작 전

- `MapManager`가 `GridMapGenerationSettings`를 받아 `GridChunkRuntimeGenerator`를 호출했다. 생성기는 `GridChunkDatabase`의 출입구와 청크 프리팹을 이용해 지형 배치·생성을 진행했다.
- `GridChunkData`와 DB가 청크 유형·출입구·스폰 정보를 관리했고, 에디터 도구에서 DB를 제작했다. 생성 과정에서 씬 오브젝트를 만들고 기존 생성 루트를 이름으로 찾아 제거했다.
- 맵 경로 결정, 실제 방 생성, 내부 조합의 책임과 방별 실제 통과 조건을 분리해 이해하고 검증할 필요가 있었다.

### 제작 후

| 책임 | 변경 후 구성 |
| --- | --- |
| 방 제작 데이터 | [RoomTemplate](../../Assets/Scripts/Map/RoomTemplates/RoomTemplate.cs)과 [RoomTemplateCatalog](../../Assets/Scripts/Map/RoomTemplates/RoomTemplateCatalog.cs)에 지형·출입구·검증된 통과 방향·마커·변형·콘텐츠를 기록한다. |
| 맵 계획과 검사 | [MapPlanner](../../Assets/Scripts/Map/RoomTemplates/MapPlanner.cs)가 seed로 경로·방·내부 선택을 계획하고 [MapPlanValidator](../../Assets/Scripts/Map/RoomTemplates/MapPlanValidator.cs)가 연결과 기록된 조건을 검사한다. |
| 씬 생성 | [MapGenerator](../../Assets/Scripts/Map/RoomTemplates/MapGenerator.cs)가 계획·생성을 조율하고 [MapBuilder](../../Assets/Scripts/Map/RoomTemplates/MapBuilder.cs)가 확정된 방·패턴·콘텐츠를 생성한다. |
| Run과 편집 | [RoomMapFactory](../../Assets/Scripts/Map/MapRuntime/RoomMapFactory.cs)와 [MapManager](../../Assets/Scripts/Managers/GameManager/MapManger/MapManager.cs)가 Run을 연결한다. [RoomMapGeneratorWindow](../../Assets/Editor/MapGenerator/RoomTemplates/RoomMapGeneratorWindow.cs)는 같은 생성 흐름으로 미리보기를 제공한다. |

- 기본 맵은 4×4 격자에서 좌우·하강 주 경로와 시작·Goal을 계획하고, 조건이 맞으면 막다른 선택 방을 최대 하나 추가한다. 내부 패턴·확률 셀과 선택 방 콘텐츠는 별도 난수 흐름으로 선택한다.
- 새 맵을 비활성 상태로 만든 뒤 성공하면 기존 맵과 교체한다. 기존 생성기·DB와 방향별 청크 프리팹을 정리하고 일부 지형은 새 템플릿과 패턴 후보로 전환했다.
- 제작 규격은 [방 템플릿 제작 규격](Chunk_Generation/Work/Room_Template_Authoring_Guide.md), 클래스와 관계는 [코드 분석 가이드](Map_Code_Architecture_Guide.md)에 정리했다.

### 검증과 남은 사항

- 작업 당시 EditMode 테스트 29개가 통과했다. 샘플의 17개 통과 방향, 대표 지형 조합과 선택 방 왕복을 실제 Player로 확인했다. 대표 seed의 시작부터 Goal과 타이틀 복귀도 확인했다.
- 검증은 당시의 샘플·지형·설정에 대한 결과다. 타일·Collider·통로를 변경하면 해당 방의 검증 표시를 해제하고 관련 방향과 조합을 다시 확인한다.
- 지형 그래픽에는 단색 샘플과 일부 기존 지형이 사용되고 있다. 새 RuleTile과 NovaDevs 그래픽의 실제 적용·경계 검증은 남아 있다.

## NovaDevs 지형 타일과 그래픽

**상태:** 대표 지형 RuleTile 시제품과 맵 그래픽 생성 구현·대표 검증 완료. 최종 배치 규칙과 추가 템플릿 제작은 다음 단계다.

### 제작 전

- 방 고정 지형과 패턴은 별도 Tilemap이며, 확률 셀은 고정 지형에 적용된다. 현재 콘텐츠 지지면 검사는 고정 지형의 Grid Collider를 기준으로 한다.
- 방과 패턴이 맞닿는 경우 지형 그래픽의 이웃 판정과 이음새를 해결해야 한다. NovaDevs Platform 1 Sprite와 현재 1×1 방 셀의 크기 차이도 확인해야 한다.

### 설계 및 확인 대상

- NovaDevs Sprite를 사용한 기본 지형 RuleTile 하나를 우선 제작하고, 배경과 장식은 별도 배치한다. 방별 충돌 지형은 유지하면서 확정된 점유 셀을 공통 그래픽 Tilemap에 표시하도록 설계했다. 초기 대상은 전체 셀이 충돌하는 Grid 지형이며 다른 Collider 형상은 적용 전에 별도 확인한다.
- 대표 방과 인접 방·패턴에서 Sprite 크기, 이음새, Collider와 Player 이동을 검증한다.
- 데모 씬의 Tile 배치를 분석하고 새 규칙은 별도 프리뷰에서 렌더링했다. 데모 씬 자체의 실제 화면은 아직 확인하지 못했다.
- 설계와 작업 기준은 [NovaDevs 배치 설계안](Chunk_Generation/Work/NovaDevs_Level_Art_Design.md)과 [타일·그래픽 작업 지침](Chunk_Generation/Work/Room_Tile_Visual_Plan.md)을 참고한다.

### 구현 진행

- [MapVisualBuilder](../../Assets/Scripts/Map/RoomTemplates/MapVisualBuilder.cs)를 추가하고 `MapBuilder`가 방·패턴의 지형을 수집해 선택된 그래픽 타일을 적용하도록 연결했다. 설정의 그래픽 타일이 비어 있으면 기존 생성 흐름을 사용한다.
- 음수 좌표와 방·패턴 셀 통합, 미지원 Collider, 중복 셀을 확인하는 [EditMode 테스트](../../Assets/Tests/EditMode/MapGeneration/MapVisualBuilderTests.cs)를 작성했다. Unity에서 맵 관련 EditMode 테스트 35개가 통과했다.
- 기존 `DownThrough` 프리팹의 출입구가 Unity Flags Inspector의 `Everything` 값인 `-1`로 저장되어 검사에 실패하던 문제를 발견했다. `RoomTemplate.Openings`에서 이 값을 네 방향으로 해석하고, 다른 잘못된 비트는 계속 거부한다. 두 경우를 테스트로 확인했다.
- Platform 1 Sprite 34개의 3×3 색상 분석에서 대부분이 같은 패턴으로 분류됐다. 자동 분류 결과만으로는 모서리와 지형의 역할을 구분할 수 없으므로 데모 배치와 실제 화면을 확인해 Sprite를 선정해야 한다.
- [Nova 지형 RuleTile 시제품](../../Assets/Art/Map/Terrain/Nova_Ground_Prototype.asset)과 시험용 맵 설정을 만들었다. 원본 시트는 유지하고 1×1 셀에 맞춘 복사본과 원본 조각을 조합한 세로 벽·고립 셀·안쪽 모서리 Sprite를 사용한다. 그래픽 Tilemap에는 Collider를 두지 않는다.
- 4개 seed의 4×4 맵 생성과 지형 셀 수집을 확인했다. 통합 검사와 그래픽 미리보기 Undo·Redo를 포함한 EditMode 테스트 38개가 통과했다. Play 모드에서도 seed 42의 방 16개와 그래픽 셀 1,972개가 생성됐다.
- 가상 입력으로 대표 방의 좌→우·우→좌 경계 통과와 점프 후 착지를 확인했다. 패턴과 확률 셀이 적용된 샘플을 사용했다. 첫 입력은 포커스 설정으로 전달되지 않았고, 수정 후 정상 동작했다. 가상 키보드와 콜백을 제거하고 입력 설정을 복원했다.
- 복합 안쪽 모서리를 포함한 47가지 이웃 조합의 Sprite 선택을 회귀 테스트로 확인했다. 최종 변경 후 맵 EditMode 테스트 39개가 통과했다.
- Player의 오른쪽 대시 통과, 왼쪽 복귀, 점프와 착지를 가상 입력으로 확인했다. 입력 장치·콜백과 임시 설정을 정리했으며 확인 당시 Console의 현재 오류는 0개였다.
- Player 검증 이후 그래픽 Sprite만 변경해 안쪽 모서리의 명암 경계, 내부 채움과 세로 벽의 반복 이음새를 줄였다. 지형과 Collider는 변경하지 않았으며 Player 조작은 다시 실행하지 않았다. 최종 Sprite에 대한 47가지 출력 검사와 맵 EditMode 테스트 39개는 통과했다.
- 두 seed의 방 경계와 대표 방의 패턴을 프리뷰에서 확인했다. 이 검증은 대표 조합에 한정되며 모든 조합의 미관을 보증하지 않는다. 최종 배치 규칙은 다음 단계에서 확정한다.
- Player 조작 검증은 최대 8회 범위로 확인받았다. 위 검증 결과는 사용한 샘플·조합에 한정한다.
- Unity를 완전히 재시작한 직후 Pipeline 서버는 7800 포트 하나로 확인됐다. 스크립트 재컴파일 후 같은 Unity 프로세스에서 7800과 7801이 동시에 열리고, 이후 CLI 식별 파일이 사라지는 현상이 재현됐다. 서버가 중복되는 현상과 식별 파일 삭제의 직접적인 인과관계는 아직 확인되지 않았다.

## 방 배경·장식 제작 규칙

**상태:** 대표 프리뷰 확인과 제작 규칙 작성 완료. 실제 방 템플릿 확장은 다음 단계다.

### 제작 전

- 배경·장식 에셋과 용도는 정리했으나 지형·Player와의 실제 표시 순서, 프리팹의 지면 기준점과 방 경계의 배치 조건은 확정되지 않았다.

### 제작 후와 검증

- [방 지형·배경·장식 제작 규칙](Chunk_Generation/Work/Room_Art_Authoring_Guide.md)에 지형·출입구, 방별 배경, 장식, Sorting Layer·Order와 검증 기준을 정리했다.
- NovaDevs Demo 3를 임시 렌더링하고, 인접 `Horizontal` 방 두 개에 나무·수풀·돌·가로등과 실제 Player Sprite를 배치한 프리뷰를 확인했다. 가로등 루트와 실제 받침의 위치가 달라 받침 기준으로 정렬했다.
- Player와 Goblin Renderer는 `Player` 레이어에 있고 공통 지형은 `Default` 레이어 Order `0`이다. 현재 `BG_Far`·`BG_Mid`는 `Default` 앞에 있으며 `Deco`·`FG`는 조회 시 ID `0`으로 확인되어 별도 정렬 레이어로 사용하지 않았다.
- 프리뷰는 임시 씬에서 렌더링한 예시이며 실제 방 프리팹을 수정하지 않았다. 장식 Sprite의 겹침·가독성과 복합 프리팹의 조명은 템플릿 제작 시 확인해야 한다.

## 실제 방 템플릿 확장

**상태:** 좌우 방 2종, 하강 방과 상부 진입 방을 제작했다. 지형 그래픽의 가독성을 확인한 뒤 다른 방 유형과 지형을 확장한다.

### 제작 전

- 좌우 샘플은 평평한 통로와 내부 랜덤 패턴을 사용했다. 이전 배경 예시는 임시 화면에만 배치되어 실제 방 프리팹에 저장되지 않았다.

### 제작 후와 검증

- [Horizontal_Step](../../Assets/Prefabs/Map/RoomTemplates/NovaPrototype/Horizontal_Step.prefab)에 한 단 높이의 고정 지형과 나무·수풀 Sprite를 배치했다. 충돌 지형을 변경했으므로 통과 표시를 해제한 뒤 서→동·동→서 점프 통과와 착지를 실제 Player로 확인하고 다시 표시했다. 시작·Goal 연결은 검증 표시를 남기지 않았다.
- 기존 카탈로그를 복사한 Nova 시제품 카탈로그에 새 방을 등록하고 시제품 설정에 연결했다. 당시 기본 카탈로그와 기본 설정은 그대로였다. seed `0`에서 실제 방과 공통 지형 그래픽, 배경이 생성됨을 확인했다.
- Player 검증 중 가상 입력 장치와 콜백을 정리하고 입력 설정을 복원했다. 타이틀 씬과 시험 씬을 함께 연 상태에서 Audio Listener 중복 메시지가 반복되어 시험 씬의 Listener를 임시로 비활성화했다. 시험 씬은 저장하지 않고 닫았다.
- 새 방 선택·지형 Collider·장식 생성을 포함해 맵 EditMode 테스트 40개가 통과했다. 중앙 지형 아래의 어두운 그래픽 표현은 추가 검토가 필요하다.

### 숲 배경 재구성

- Demo 1~3을 실제 렌더링해 배치와 정렬을 확인했다. Demo 3은 수관·다양한 줄기·하층 수풀·난간을 겹치고, Demo 2의 사당 구간은 아치와 조형물을 중심으로 구성한다.
- `Horizontal_Step`의 장식을 Demo 3의 숲 구성을 참고해 수관 2개, 서로 다른 줄기와 하층 수풀로 교체했다. Player 프리팹 정지 자세를 포함한 실제 생성 방 프리뷰를 확인했다. 장식은 Collider가 없고 Renderer 경계는 방 범위 안에 있다.
- 고정 지형과 통과 조건은 변경하지 않았다. 기존 양방향 Player 검증을 유지하며 맵 EditMode 테스트 40개가 통과했다. 배경 변경 후 인접 방 전 조합의 화면과 실제 카메라 움직임은 아직 확인하지 않았다.

### 사당 배경 방

- Demo 2의 아치 두 조각, 사당 조형물과 받침의 위치·크기·정렬, Collider 및 조명을 확인했다. 사당 프리팹은 6개 조명을 포함하므로 시각 장식에는 Sprite를 사용했다.
- [Horizontal_Shrine](../../Assets/Prefabs/Map/RoomTemplates/NovaPrototype/Horizontal_Shrine.prefab)에 아치·조형물·받침과 숲 배경을 배치해 Nova 시제품 카탈로그에 등록했다. 생성된 seed `9`의 사당과 인접 숲 방을 Player 정지 자세와 함께 임시 카메라로 확인했다. 사당은 상호작용 오브젝트가 아니다.
- 숲 방과 고정 타일·Collider·통과 방향을 비교하고 사당 장식에 Collider가 없음을 확인했다. 실제 Player로 숲에서 사당에 진입해 단차를 넘은 뒤 반대 방향으로 돌아가고 착지했다. 가상 입력 장치·콜백과 임시 설정을 정리했다. 다른 인접 조합과 실제 카메라 움직임은 추가 확인이 필요하다.
- 맵 EditMode 테스트 41개가 통과했다. 추가한 검사는 두 방의 지형과 통과 방향 일치, seed `9`의 인접 생성과 사당 장식을 확인한다.

### 숲 하강 방

- 기존 `WestSouth`는 서쪽에서 들어와 남쪽으로 나가는 단일 통과 방향을 가진 평평한 방이다. 시작·Goal 마커와 내부 지형 변형은 없다.
- [Down_ForestStep](../../Assets/Prefabs/Map/RoomTemplates/NovaPrototype/Down_ForestStep.prefab)에 한 단 높이의 고정 지형을 추가하고 Demo 3에서 확인한 수관·줄기·수풀로 숲을 구성했다. 남쪽 구멍 주변은 수풀을 피했다.
- 기존 통과 표시를 해제하고 실제 Player의 서→남 점프·하강과 아래 `Arrival` 방 착지를 확인한 뒤 검증 표시를 켰다. 가상 입력과 임시 씬을 정리하고 설정을 복원했다.
- 시제품 카탈로그에 등록했으며 seed `0`에서 하강 방과 아래 방 연결을 생성·렌더링해 확인했다. 서로 다른 아래 방 전체 조합과 실제 카메라 움직임은 추가 확인이 필요하다.
- 하강 방 선택, 단차의 Grid Collider, 남쪽 출입구와 아래 방의 북쪽 연결을 포함해 맵 EditMode 테스트 42개가 통과했다.

### 폐허 회랑 상부 진입 방

- 기존 `NorthWest`는 북쪽에서 서쪽으로 나가는 단일 통과 방향을 가진 평평한 방이다. 시작·Goal 마커와 내부 지형 변형은 없다.
- [Arrival_RuinedGallery](../../Assets/Prefabs/Map/RoomTemplates/NovaPrototype/Arrival_RuinedGallery.prefab)에 북쪽 출입구 아래의 중간 착지 발판과 Demo 1·2를 참고한 조형물·부서진 기둥·수풀 배경을 추가했다.
- 기존 통과 표시를 해제하고 실제 Player가 위쪽 하강 방에서 내려와 중간 발판에 착지한 뒤 서쪽의 숲 방으로 이동하는 것을 확인하고 검증 표시를 켰다. 가상 입력과 임시 씬을 정리하고 설정을 복원했다. 이후 배경 Sprite 배치만 조정했다.
- 시제품 카탈로그에 등록하고 seed `0`에서 하강 방·폐허 회랑·서쪽 숲 방의 연결을 생성·렌더링해 확인했다. 다른 인접 방 조합과 실제 카메라 움직임은 추가 확인이 필요하다.
- 북쪽 진입·서쪽 출입구와 중간 발판의 Grid Collider, 인접 방의 연결을 포함해 맵 EditMode 테스트 43개가 통과했다.

### 실제 카메라 지형 가독성 점검

- seed `0`의 생성 맵을 임시 Play 씬에서 만들고 실제 Player·카메라 프리팹과 전역 조명으로 확인했다. 가상 키 입력으로 숲 하강 방의 단차를 넘어 폐허 회랑의 중간 발판에 착지했다. Player 위치는 `(74.44, -8.15)`이고 접지 상태였다.
- [점프 중 화면](Chunk_Generation/Work/Images/PlayerCameraStep.png)에서 단차의 검은 내부가 구멍처럼 보이며, 실제 해당 셀의 충돌 지형은 채워져 있다. 해당 부분에는 `Nova_Interior`와 `Corner_NW`·`Corner_NE`가 사용된다. [착지 화면](Chunk_Generation/Work/Images/PlayerCameraLanding.png)의 얇은 발판에도 검은 띠가 보인다. 내부 채움과 주변 모서리·발판 Sprite를 함께 검토한다.
- Player 입력 장치·콜백과 임시 씬을 정리하고 입력 설정을 복원했다. 확인 당시 Console의 현재 오류는 0개였다. 이 검사는 전체 게임 시작 흐름이나 다른 방·패턴 조합의 카메라 화면을 검증하지 않는다.

### 지형 내부 명암 보정

- NovaDevs 원본에도 검은 내부가 있으므로 RuleTile 규칙을 변경하지 않고 지형 전용 URP 2D 머티리얼로 어두운 색을 조명 계산 전에 보정했다. Sprite의 알파와 방의 충돌 지형은 유지한다. 머티리얼은 시제품 설정의 공통 그래픽 Tilemap에만 적용하며 설정하지 않으면 기본 Renderer를 사용한다.
- 실제 Player를 두 차례 같은 경로로 조작해 보정 강도를 비교하고 두 번 모두 아래 발판에 착지했다. 최종 색상 `(0.13, 0.12, 0.16)`의 [단차](Chunk_Generation/Work/Images/PlayerCameraStepAdjusted.png)와 [발판](Chunk_Generation/Work/Images/PlayerCameraLandingAdjusted.png)을 확인했다. 검증은 전역 조명과 seed `0` 한 경로에 한정된다.
- 맵 생성 결과와 미리보기 Undo·Redo에서 지정된 머티리얼이 적용되는지 확인했다. 맵 EditMode 테스트 44개가 통과했고 Player 가상 입력과 임시 씬·설정을 정리했다. 새 Console 오류는 없었다.
- 셰이더는 현재 URP `Sprite-Lit-Default`를 바탕으로 하므로 URP 갱신 시 원본과의 호환성을 확인해야 한다. 다른 조명과 방·패턴 조합의 가독성은 확장 작업에서 확인한다.

### 동쪽 진입 폐허 전망대

- 기존 `EastSouth`는 동쪽에서 진입해 남쪽으로 나가는 단일 통과 방향을 가진 방이다. [Down_RuinedOverlook](../../Assets/Prefabs/Map/RoomTemplates/NovaPrototype/Down_RuinedOverlook.prefab)에 진입 측 단차와 Demo 2·3을 참고한 수관·나무·수풀·난간·기둥을 배치했다. 배경에는 Collider가 없다.
- 실제 Player가 오른쪽 이웃 방에서 진입해 단차를 점프하고 남쪽으로 내려가 아래 폐허 회랑의 발판에 착지했다. 이 한 방향에 대한 검증을 기록하고 시제품 카탈로그에 등록했다. 검증 후 수관의 직선 경계를 가리도록 배경 Sprite만 조정했다.
- seed `3`의 계획에는 새 방이 선택됐지만 기존 `Legacy_EW2_Horizontal`의 Sprite Collider 타일 때문에 시제품 그래픽 생성이 실패했다. 해당 전환 방을 Nova 시제품 카탈로그에서만 제외한 뒤 같은 seed의 맵 16개 방 생성과 [전망대 화면](Chunk_Generation/Work/Images/RuinedOverlookGenerated.png)을 확인했다. 기존 기본 카탈로그와 전환 프리팹은 변경하지 않았다.
- 카탈로그의 고정 지형이 Grid Collider를 사용하는지 확인하는 회귀 검사를 추가했다. 시제품 카탈로그 변경으로 숲·사당 인접 조합 seed는 `14`로 바뀌었다. 실제 이동 검증은 오른쪽 숲 방과 아래 폐허 회랑의 한 조합에 한정된다.

### 북쪽 진입 사당 아트리움

- `NorthEast`의 북→동 연결을 바탕으로 [Arrival_ShrineAtrium](../../Assets/Prefabs/Map/RoomTemplates/NovaPrototype/Arrival_ShrineAtrium.prefab)에 중간 착지 발판과 Demo 2의 아치·조형물 배치를 참고한 사당 배경을 추가했다. 장식에는 Collider가 없다.
- 실제 Player가 위쪽 숲 하강 방에서 내려와 발판에 착지하고 동쪽 숲 방에 완전히 진입해 접지했다. 해당 통과 방향을 기록하고 Nova 시제품 카탈로그에 등록했다. 이후 사당 Sprite 받침의 지면 정렬만 조정했다.
- seed `0`의 맵 16개 방 생성과 [사당 아트리움 화면](Chunk_Generation/Work/Images/ShrineAtriumGenerated.png)을 확인했다. 렌더링은 생성 맵과 Player 정지 자세를 임시 카메라·전역 조명으로 확인한 결과다. 다른 이웃 방과 조명의 실제 이동 검증은 남아 있다.
- 맵 EditMode 테스트 47개가 통과했다. 입력 장치와 임시 씬을 정리하고 설정을 복원했으며 확인 당시 Console의 현재 오류는 0개였다.

### 북쪽 진입 연속 하강 방

- `NorthSouth`의 북→남 연결을 바탕으로 [DownThrough_RuinedShaft](../../Assets/Prefabs/Map/RoomTemplates/NovaPrototype/DownThrough_RuinedShaft.prefab)에 상부 착지 발판과 왼쪽 보조 발판을 만들었다. Demo 1·3을 참고해 수관, 폐허 조형물과 아치를 배치했다. 장식에는 Collider가 없다.
- 실제 Player가 위쪽 숲 하강 방에서 내려와 착지한 후 발판 오른쪽으로 내려가 남쪽 출입구를 통과해 아래 폐허 회랑에 착지했다. 이 통과 방향을 검증하고 Nova 시제품 카탈로그에 등록했다. 이후 배경 Sprite만 조정했다.
- seed `1`의 맵에서 같은 방이 `(2, 1)`과 `(2, 2)`에 연속 배치된다. [수직 통로 화면](Chunk_Generation/Work/Images/RuinedShaftGenerated.png)은 생성 맵을 임시 카메라와 Player 정지 자세로 렌더링한 결과다. 두 방을 연속으로 통과하는 실제 Player 검증은 수행하지 않았다.
- 연속 배치의 열린 출입구와 발판을 포함해 맵 EditMode 테스트 48개가 통과했다. 가상 입력과 임시 씬·설정을 정리했으며 확인 당시 Console의 현재 오류는 0개였다.

### 동쪽 출구 숲속 관문 시작 방

- 시작·Goal·선택 방 마커를 함께 가진 `CapEast`를 바탕으로 [Start_ForestGateway](../../Assets/Prefabs/Map/RoomTemplates/NovaPrototype/Start_ForestGateway.prefab)를 만들었다. 새 방은 시작 마커와 동쪽 출구만 사용하며 단차와 Demo 1·2를 참고한 숲·아치 배경을 가진다.
- 실제 Player가 시작 마커에서 생성돼 단차를 점프로 넘고 동쪽 숲 방에 완전히 진입해 착지했다. 동쪽 시작 출구를 검증해 Nova 시제품 카탈로그에 등록했다. 입력 장치와 임시 씬을 정리하고 설정을 복원했으며 확인 당시 Console의 현재 오류는 0개였다.
- seed `1`의 생성 맵에서 방 `(1, 0)`과 시작 좌표 `(24.5, 5)`를 확인했다. [시작 방 화면](Chunk_Generation/Work/Images/ForestGatewayGenerated.png)은 실제 생성 맵과 Player 정지 자세를 임시 카메라·전역 조명으로 렌더링한 결과다. 실제 이동 검증은 오른쪽 숲 방과의 한 조합에 한정된다.
- 시작 위치와 동쪽 이웃 연결을 포함해 맵 EditMode 테스트 49개가 통과했다.

### 서쪽 진입 폐허 사당 도착 방

- `CapWest`를 바탕으로 [Goal_RuinedSanctuary](../../Assets/Prefabs/Map/RoomTemplates/NovaPrototype/Goal_RuinedSanctuary.prefab)를 만들었다. 서쪽 진입구와 Goal 마커를 사용하고 Demo 2의 아치·숲과 받침으로 도착 지점을 구성했다.
- 실제 Player가 서쪽 숲 방에서 오른쪽 방향키로 진입해 Goal 트리거에 접촉한 것을 확인하고, 해당 진입 방향을 Nova 시제품 카탈로그에 등록했다. 입력 장치·임시 씬을 정리하고 설정을 복원했다. 전체 Run 클리어와 타이틀 복귀는 이번에 확인하지 않았다.
- seed `9`의 생성 맵에서 방 `(3, 3)`과 Goal 좌표 `(70.5, -55)`를 확인했다. [도착 방 화면](Chunk_Generation/Work/Images/GoalSanctuaryGenerated.png)은 생성 맵과 Player 정지 자세를 임시 카메라·전역 조명으로 렌더링한 결과다. 실제 이동 검증은 서쪽 숲 방과의 한 조합에 한정된다.
- 도착 방 선택, 서쪽 이웃과 Goal 배치를 포함해 맵 EditMode 테스트 50개가 통과했다. 검사 작성 중 어셈블리 참조 오류와 임시 렌더 텍스처 정리 오류를 수정했고 최종 확인 당시 Console의 현재 오류는 0개였다.

### 북쪽 진입 숲속 폐허 도착 방

- `CapNorth`를 바탕으로 [Goal_ForestDescent](../../Assets/Prefabs/Map/RoomTemplates/NovaPrototype/Goal_ForestDescent.prefab)를 만들었다. 북쪽 진입구 아래 중간 발판을 두고 Demo 3의 수관과 하층 식생에 폐허 조형물·Goal 받침을 더했다.
- 실제 Player가 위쪽 숲 하강 방에서 내려와 발판에 착지하고 왼쪽으로 이동해 Goal 트리거에 접촉한 뒤 바닥에 착지했다. 북쪽 진입을 Nova 시제품 카탈로그에 등록했다. 입력 장치와 임시 씬을 정리하고 설정을 복원했다. 전체 Run 클리어와 타이틀 복귀는 확인하지 않았다.
- seed `4`의 생성 맵에서 방 `(3, 3)`과 Goal 좌표 `(70.5, -55)`를 확인했다. [도착 방 화면](Chunk_Generation/Work/Images/GoalForestDescentGenerated.png)은 생성 맵과 발판 위 Player 정지 자세를 임시 카메라·전역 조명으로 렌더링한 결과다. 실제 이동 검증은 위쪽 숲 하강 방과의 한 조합에 한정된다.
- 도착 방 선택, 북쪽 이웃과 발판·Goal 배치를 포함해 맵 EditMode 테스트 51개가 통과했다. 확인 당시 Console의 현재 오류는 0개였다.

### 동쪽 진입 폐허 정원 도착 방

- `CapEast`를 바탕으로 [Goal_OvergrownCourt](../../Assets/Prefabs/Map/RoomTemplates/NovaPrototype/Goal_OvergrownCourt.prefab)를 만들었다. 동쪽 진입로에 낮은 단차를 두고 Demo 1의 나무와 울타리를 참고해 Goal 주변을 구성했다.
- 실제 Player가 동쪽 숲 방에서 진입해 단차를 점프로 넘고 Goal 트리거에 접촉했다. 동쪽 진입을 Nova 시제품 카탈로그에 등록하고 입력 장치와 임시 씬을 정리했으며 설정을 복원했다. 전체 Run 클리어와 타이틀 복귀는 확인하지 않았다.
- seed `0`의 생성 맵에서 방 `(1, 3)`과 Goal 좌표 `(27.5, -55)`를 확인했다. [도착 방 화면](Chunk_Generation/Work/Images/GoalOvergrownCourtGenerated.png)은 생성 맵과 단차 위 Player 정지 자세를 임시 카메라·전역 조명으로 렌더링한 결과다. 실제 이동 검증은 동쪽 숲 방과의 한 조합에 한정된다.
- 도착 방 선택, 동쪽 이웃과 단차·Goal 배치를 포함해 맵 EditMode 테스트 52개가 통과했다. 확인 당시 Console의 현재 오류는 0개였다.

### 서쪽 출구 사당 시작 방

- `CapWest`를 바탕으로 [Start_ShrineWest](../../Assets/Prefabs/Map/RoomTemplates/NovaPrototype/Start_ShrineWest.prefab)를 만들었다. 오른쪽 시작 위치와 낮은 단차를 두고 Demo 2의 사당 조형물과 아치로 배경을 구성했다.
- 실제 Player가 시작 마커에서 단차를 점프로 넘어 서쪽 숲 방에 완전히 진입하고 착지했다. 서쪽 출구를 Nova 시제품 카탈로그에 등록하고 입력 장치와 임시 씬을 정리했으며 설정을 복원했다. 검증 후 사당 배경의 받침 위치만 조정했다.
- seed `4`의 생성 맵에서 방 `(1, 0)`과 시작 좌표 `(35.5, 5)`를 확인했다. [시작 방 화면](Chunk_Generation/Work/Images/StartShrineWestGenerated.png)은 생성 맵의 시작 위치에 Player를 정지 자세로 두고 임시 카메라·전역 조명으로 렌더링한 결과다. 실제 이동 검증은 서쪽 숲 방과의 한 조합에 한정된다.
- 시작 방 선택, 서쪽 이웃과 단차·시작 위치를 포함해 맵 EditMode 테스트 53개가 통과했다. 확인 당시 Console의 현재 오류는 0개였다.

### 남쪽 출구 숲 시작 방

- `CapSouth`를 바탕으로 [Start_ForestDrop](../../Assets/Prefabs/Map/RoomTemplates/NovaPrototype/Start_ForestDrop.prefab)을 만들었다. 왼쪽 시작 위치와 낮은 단차를 두고 Demo 1의 숲과 울타리를 참고해 배경을 구성했다.
- 실제 Player가 시작 마커에서 단차를 점프로 넘고 남쪽 출구로 내려가 아래 폐허 회랑의 발판에 착지했다. 남쪽 출구를 Nova 시제품 카탈로그에 등록하고 입력 장치와 임시 씬을 정리했으며 설정을 복원했다.
- seed `10`의 생성 맵에서 방 `(1, 0)`과 시작 좌표 `(24.5, 5)`를 확인했다. [시작 방 화면](Chunk_Generation/Work/Images/StartForestDropGenerated.png)은 생성 맵의 시작 위치에 Player를 정지 자세로 두고 임시 카메라·전역 조명으로 렌더링한 결과다. 실제 이동 검증은 아래 폐허 회랑과의 한 조합에 한정된다.
- 시작 방 선택, 아래 이웃과 출구·단차·시작 위치를 포함해 맵 EditMode 테스트 54개가 통과했다. 확인 당시 Console의 현재 오류는 0개였다.

### 서쪽 입구 폐허 정원 선택 방

- 기존 `CapWest`의 평탄한 지형과 Goblin·아이템·빈 슬롯의 가중치를 유지해 [Optional_RuinedCourt](../../Assets/Prefabs/Map/RoomTemplates/NovaPrototype/Optional_RuinedCourt.prefab)를 만들고 Demo 1을 참고한 숲·울타리와 폐허 조형물을 배치했다. 기존 Goal과 공유하던 목표는 선택 방 전용 마커로 분리했다.
- 실제 Player가 빈 슬롯 상태의 방에 서쪽으로 진입해 목표 지점까지 이동하고 같은 입구로 돌아와 착지했다. 해당 왕복 방향을 Nova 시제품 카탈로그에 등록했다. 입력 장치와 임시 씬을 정리하고 설정을 복원했다. 실제 생성 분기 방을 통한 왕복, Goblin과의 전투 및 아이템 획득은 이 방에서 확인하지 않았다.
- 생성 맵의 seed `132`·`146`·`210`에서 각각 아이템·빈 슬롯·Goblin이 선택되고 슬롯에 맞게 생성되는 것을 확인했다. [선택 방 화면](Chunk_Generation/Work/Images/OptionalRuinedCourtGenerated.png)은 seed `132`의 생성 맵과 Player 정지 자세를 임시 카메라·전역 조명으로 렌더링한 결과다.
- 새 선택 방과 콘텐츠 세 가지 경우를 포함해 맵 EditMode 테스트 57개가 통과했다. 확인 당시 Console의 현재 오류는 0개였다.

### Chunk 문서 정리

- Chunk 내부 생성 문서를 [설명서와 AI 작업 계획](Chunk_Generation/README.md)으로 나눴다. 지형·Sprite 요구사항은 중복 내용을 정리하고 필요한 원본 Sprite 정보를 [그래픽 설계안](Chunk_Generation/Work/NovaDevs_Level_Art_Design.md)에 통합했다.

### 맵 작업 진행 현황 정리

- 전체 계획의 목표·구조와 진행 현황을 분리하고 [전체 진행](Map_Progress.md)에서 Map Assembly와 Chunk Generation의 세부 단계로 연결했다. 내부 지형 선택 기능의 완료와 샘플에서 확인한 검증 범위를 구분했다.
- Chunk 설명서는 [Chunk 내부 생성 폴더](Chunk_Generation/README.md) 바로 아래에 두고 작업 지침은 하위 폴더에 모았다. 문서와 이미지 참조를 이동한 경로에 맞게 갱신했다.
- 이후 진행 현황만 Chunk 폴더에 남기고 설명서와 작업 지침을 `Work` 하위 폴더로 모았다.

### Room Template 사용 관계 조사

- 기본·Nova 카탈로그와 방·공용 에셋의 직렬화된 참조, 씬·테스트의 직접 경로를 조사했다. 방별 출입구·역할과 정리 기준안은 [사용 관계 조사](Chunk_Generation/Work/Room_Template_Inventory.md)에 기록했다. 에셋 이동과 Unity 실행 검증은 수행하지 않았다.

### Room Template 폴더 정리

- Unity AssetDatabase로 기본 방 14개, Nova 시제품 방 14개, 패턴과 공용 타일·Sprite를 역할별 폴더로 정리했다. 방과 관련 에셋·폴더의 GUID 33개를 확인하고 테스트·문서의 직접 경로를 갱신했다. 기존 전환 방과 두 카탈로그 구성, 템플릿 ID는 유지했다.
- 기본·Nova 설정의 seed `0`, `1`, `42`, `132`에 대해 경로, 방 선택, 내부 지형·콘텐츠 선택과 시작·Goal을 포함한 생성 계획이 이동 전후 같았다. 두 카탈로그의 방 참조 45개와 Title·이동 샘플 씬 및 패턴·타일의 참조 7개를 확인했다. 맵 EditMode 테스트 57개가 통과했다.

### 기존 전환 방 제거

- 좌우 통과 `Legacy_EW2_Horizontal`과 세 방향 `Legacy_3Dir1_Arrival`을 두 카탈로그에서 제외하고 프리팹과 빈 `Converted` 폴더를 제거했다. `Base/Arrival`의 세 방향 분기와 `Patterns/Legacy_EW1_Platform`은 유지했다.
- 제외한 임시 카탈로그로 기본·Nova 설정 각각 seed `0~999`의 계획이 모두 성공하고 각 879개에서 선택 방이 추가되는 것을 확인했다. 후보가 바뀌어 기존 seed의 방 선택 결과는 달라질 수 있다. 선택 방 콘텐츠 세 가지를 확인하는 테스트의 seed를 갱신한 후 맵 EditMode 테스트 57개가 통과했다.

### Base·Nova Variant 전환 계획

- Base에 지형·내부 랜덤 조합과 슬롯을 두고, Nova Variant에 배경·장식을 배치하는 방향을 정했다. 대표 상속 검증 후 필요한 방 역할의 Variant를 구성하고, 카탈로그와 설정 참조를 전환한다. 현재 시제품은 독립 프리팹이며 전환 작업은 시작하지 않았다.

### 대표 Base·Nova Variant 검증

- `Base/Horizontal`을 부모로 `NovaVariants/Horizontal_Shrine`을 만들고 기존 사당 시제품의 배경을 추가했다. 새 Variant는 기존 카탈로그에 등록하지 않았다.
- 부모의 고정 지형·통과 방향·패턴과 확률 셀 상속을 확인했다. 임시 카탈로그 사본에서 300개 seed의 계획을 생성하고 내부 지형 네 조합을 실제 맵에 구성했다. 맵 EditMode 테스트 59개가 통과했다.
- 패턴과 확률 셀을 모두 적용한 Variant에서 실제 Player로 좌우 이동을 확인했다. 포커스가 없는 에디터에서는 키보드 입력이 전달되지 않아 Play 중 가상 게임패드 바인딩을 임시로 추가했다. 장치와 Play 환경을 정리했으며 Console의 현재 오류는 0개였다. 다른 세 지형 조합의 Variant 이동과 배경의 실제 화면 가독성은 확인하지 않았다.
- Base 사본의 확률 셀을 변경하자 Variant에도 변경이 반영됐지만 통과·조합 검증 표시는 유지됐다. Base의 지형이나 통로를 바꾼 뒤에는 수동으로 검증 표시를 해제하고 다시 확인해야 한다.
- Variant 저장 시 자동 기록된 `CompositeCollider2D` 내부 경로 덮어쓰기를 제거했다. 제거 후 생성한 Base와 Variant의 충돌 경로가 같고 충돌체 덮어쓰기가 없는지 확인하는 회귀 검사를 추가했다.
- 같은 seed·좌표에서 Base와 Variant의 지형 선택을 100회 비교한 결과 73회 달랐다. 현재 지형 난수가 각 템플릿 ID를 포함하기 때문이다. 방 선택은 적합한 Base 그룹을 먼저 고르고 그 안에서 Variant를 고르는 방향으로 정했으며, 생성기 변경은 다음 단계에 수행한다.

### Base 그룹 선택과 Variant 전환

- `RoomTemplate`과 계산용 스냅샷에 Base 그룹 ID를 추가하고 기존 Base 14개에 값을 설정했다. 그룹 ID가 비어 있는 방은 템플릿 ID를 사용한다. 같은 그룹의 유형과 출입구가 다르면 계획 입력 검증에서 거부한다.
- 주 경로는 조건에 맞는 Base 그룹과 그 안의 방을 순서대로 선택한다. 선택 방도 연결 위치를 정한 뒤 각 방의 그룹과 방을 선택한다. 그룹과 Variant 선택의 난수 흐름은 분리했다.
- 기존 숲 시제품 배경을 활용해 `Horizontal_Forest` Variant를 만들었다. 기존 Base와 사당·숲 Variant를 임시 카탈로그에서 비교한 결과 1,000개 seed 모두 계획이 생성됐고 Base 그룹과 분기 위치는 변하지 않았다. 숲 Variant는 실제 Player 이동과 화면 가독성 확인이 남았다.
- 기존 Nova 방 14개 중 12개는 출입구가 같은 기존 Base와 고정 지형 셀이 달랐다. 기존 Base를 우선 재사용하고 필요한 경우에만 Base를 추가하는 방향으로 전환한다.
- 새 그룹의 Variant가 이전 seed별 Base 구조나 분기 위치를 바꾸지 않는지와 잘못된 그룹·미검증 후보의 차단을 검사했다. 맵 EditMode 테스트 63개가 통과했다.
- 기존 Base 지형을 재사용하되 시작·Goal·선택 방의 역할을 구분하는 Base 5개를 추가했다. Nova 시제품에 대응하는 Variant 14개와 닫힌 방·분기 Variant 3개를 준비했다. 기존 카탈로그는 아직 전환하지 않았다.
- 시제품 대응 Variant의 실제 Player 검증 16개 중 14개를 확인한 뒤 사용자 요청으로 일시정지했다. 남은 두 경로는 북→남 연속 하강과 선택 방 왕복이다. 임시 Player·입력 장치를 정리하고 Play를 종료했다. 새 Variant들의 그래픽 가독성과 최종 카탈로그 전환 후 테스트는 남아 있다.
- 재개 후 `DownThrough_RuinedShaft`의 북→남 이동과 아래 방 착지, `Optional_RuinedCourt`의 서쪽 입장·목표 방문·서쪽 복귀를 실제 Player로 확인했다. 선택 방은 빈 콘텐츠 상태였으며 분기 방을 통한 왕복은 이 검사에 포함하지 않았다. 가상 입력과 임시 오브젝트를 정리했다.
- 17개 Variant만 담은 임시 카탈로그로 seed `0~999`의 계획이 모두 생성됐고 그중 720개에서 선택 방이 생성됐다. 20개 seed의 맵 빌드가 성공했고 17개 Variant가 모두 한 번 이상 선택됐다. 맵 EditMode 테스트 63개가 통과했다. 기존 카탈로그는 아직 전환하지 않았다.
- 임시 카메라·전역 조명으로 seed `0`의 [하강 분기 방](Chunk_Generation/Work/Images/NovaVariant_JunctionDown.png)과 seed `9`의 [도착 분기 방](Chunk_Generation/Work/Images/NovaVariant_JunctionArrival.png)을 확인했다. 하강 분기 방의 천장 배경에 직사각형 이음새가 보이고 인접 방에 나무 배치가 반복된다. 그래픽 마무리 단계에서 수정하며 실제 게임 조명과 모든 연결의 확인을 대신하지 않는다.

### 방 역할별 콘텐츠 배치

- 아이템·일반 몬스터·엘리트 몬스터 종류를 구분하고 주 경로와 분기가 모두 정해진 뒤 방 역할에 맞는 후보만 선택하도록 변경했다. 시작·Goal과 닫힌 방에서는 콘텐츠를 선택하지 않는다. 기존 Goblin 후보는 일반 몬스터로 분류해 선택 방에서 제외했다.
- `Base/WestSouth`에 Goblin·아이템 슬롯을 설정해 `NovaVariants/Down_ForestStep`도 상속하도록 했다. 다른 일반 방은 아직 슬롯이 없고 엘리트 프리팹은 준비 후 연결해야 한다. 슬롯 후보가 현재 역할에 하나도 없고 빈 가중치도 0이면 빈 상태로 둔다.
- Variant 17개로 구성한 임시 카탈로그에서 seed `0~999` 계획이 모두 성공했고 일반 방의 Goblin·아이템·빈 슬롯과 선택 방의 아이템·빈 슬롯을 확인했다. 맵 EditMode 테스트 66개가 통과했다.
- 생성 맵에서 실제 Player가 Goblin 추적 중 점프로 방을 통과해 아래 방에 착지했고, 별도 seed에서는 일반 방 아이템 획득과 남쪽 통과를 확인했다. 선택 방은 도착 분기 방에서 들어가 아이템을 획득하고 서쪽으로 복귀했다. 하강 분기 방의 구멍 위에서 시작한 다른 선택 방 검증은 Player가 떨어져 무효로 처리했으므로 그 연결의 실제 왕복은 확인되지 않았다. 엘리트 생성·행동과 나머지 방의 슬롯 조합도 미검증이다.

### Nova 카탈로그 전환

- 기본 설정·카탈로그의 GUID를 유지하면서 Base 전용 후보를 Nova Variant 17개로 교체하고 Nova 지형 RuleTile과 머티리얼을 연결했다. Title의 MapManager와 에디터 기본 설정이 같은 에셋을 계속 참조한다. 중복 시제품 설정·카탈로그는 참조를 확인한 뒤 제거했고 시제품 프리팹과 Variant의 부모 Base는 유지했다.
- 전환한 후보의 고유 ID·부모 상속과 1,000개 seed의 생성 계획을 확인했다. Title에서 실제 Run을 시작해 16개 방과 Player를 생성했고 서쪽 시작 방에서 가상 입력으로 이웃 방에 착지했다. 기존 독립 시제품의 지형·seed를 전제로 한 테스트는 현재 Variant와 생성 결과에 맞게 갱신했으며 맵 EditMode 테스트 67개가 통과했다.

### 그래픽과 내부 조합 확인

- NovaDevs Demo 3의 수관 배치를 다시 확인했다. `Junction_Down`, `Horizontal_Forest`, `Horizontal_Shrine`, `Down_ForestStep`에서 가까운 수관의 위치·크기와 방향을 조정해 직사각형 띠를 없앴다. 분기 방과 하강 방의 줄기 종류·위치와 수풀 간격을 조절해 대표 인접 방의 반복을 줄였다. [수정 후 분기 방](Chunk_Generation/Work/Images/NovaVariant_JunctionDown_Adjusted.png)은 seed `0` 생성 맵에 임시 카메라와 전역 조명을 적용한 화면이다.
- 실제 카탈로그의 지형 변형은 가로 Variant 두 종류에서 사용한다. seed `0`, `3`, `4`, `5`의 생성 맵에서 각각 패턴·확률 셀 네 조합을 렌더링해 확인했고, 맵 EditMode 테스트 67개가 통과했다. 배경에 Collider를 추가하지 않았고 지형은 변경하지 않아 이번에는 Player 이동을 반복하지 않았다. 같은 Variant의 연속 배치에서 장식이 반복되며 작은 확률 셀이 수관과 겹칠 때 대비가 낮은 점, 실제 게임 조명과 다른 방 연결 및 하강 분기 방의 선택 방 왕복은 추가 확인이 필요하다.

### Variant 장식의 지면 정렬

- `Horizontal_Shrine`과 `Start_ShrineWest`에서 사당 받침이 Base 바닥보다 약 0.66유닛 위에 있었다. 이전 시제품의 높은 바닥에 맞춘 장식 좌표를 Base 전환 후 재정렬하지 않은 것이 원인이었다. 받침·석상·잔을 함께 내려 상대 위치를 유지하고 받침 아래를 현재 바닥에 맞췄다.
- 카탈로그 Variant 17개의 고정 지형과 배경 Sprite를 좌표 및 임시 렌더링으로 확인했다. 다른 받침 위에 놓인 기둥 윗부분 등은 구조를 유지했다. 두 사당 방은 각각 seed `0`·`4`의 생성 맵에서 재확인했으며, [수정 화면](Chunk_Generation/Work/Images/ShrineGroundingCorrected.png)은 임시 카메라·전역 조명 결과다. 맵 EditMode 테스트 67개가 통과했다. 지형·Collider는 변경하지 않았고 이번에는 실제 Player 이동을 반복하지 않았다.

- 바닥에 닿는 나무·건축물·수풀이 떠 보이지 않도록 장식이 있는 16개 Variant의 지면 기준점 188개를 바닥보다 약 0.1유닛 낮췄다. 이미 일부 겹친 장식은 이동량을 줄였고 사당·기둥의 상대 배치를 유지했다. seed `0` 생성 맵의 사당과 숲 방을 임시 카메라·조명으로 확인했으며 [사당 화면](Chunk_Generation/Work/Images/ShrineGroundOverlap.png)을 기록했다. 맵 EditMode 테스트 67개가 통과했다. 지형·Collider는 변경하지 않았고 실제 Player 이동은 반복하지 않았다.
