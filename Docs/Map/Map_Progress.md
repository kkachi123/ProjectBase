# 맵 작업 진행 현황

전체 상태를 요약한다. 세부 단계의 완료 여부와 검증 범위는 각 영역의 진행 현황을 기준으로 갱신한다. 작업 목표와 구조는 [맵 재설계 계획](Spelunky_Chunk_Map_Plan.md), 작업 이력은 [맵 제작·리팩토링 로그](Map_Refactoring_Log.md)를 참고한다.

## Map Assembly — 맵 조합

- [x] **기본 맵 조합 구현:** 주 경로·방 선택, 계획 검증, 씬 생성과 Run 연결을 구현하고 대표 설정을 검증했다. 검증 범위와 조건은 [맵 조합 진행 현황](Map_Assembly/Map_Assembly_Progress.md)을 참고한다.
- [x] **여러 분기와 네 방향 방:** Base 20종의 실제 생성과 1,000개 seed의 계획·재현성을 확인했다. 대표 맵 두 개에서 각각 선택 방 세 곳을 왕복하고 Goal에 도달했다. [검증 결과](Map_Refactoring_Log.md#대표-맵-조합-검증).

## Chunk Generation — Chunk 내부 생성

현재 [프리팹 및 Chunk 리팩토링](Chunk_Generation/Work/Prefab_Chunk_Refactoring_Plan.md)을 진행한다. 목표 규칙과 자동 출구 문을 구현했고 다음 단계는 통합 검증이다. 실제 Player의 몬스터 목표 완료는 통합 검증에 남아 있다. [단계별 진행 현황](Chunk_Generation/Chunk_Generation_Progress.md#프리팹-및-chunk-리팩토링).

- [x] **내부 조합의 기본 기능:** 패턴·확률 셀과 선택 방 콘텐츠의 선택·적용을 구현했다. 내부 지형의 실제 조합 검증은 샘플 방에 한정된다.
- [x] **Base·Nova Variant 전환:** Base를 상속한 Nova Variant를 기본 카탈로그에 등록하고 씬·에디터·테스트 연결을 확인했다. 현재 카탈로그에는 21개가 있다.
- [x] **방 역할별 콘텐츠 배치:** 일반 경로 방과 선택 방의 후보 종류를 구분하고 대표 방에서 생성·이동·아이템 획득을 확인했다. 일반 방 슬롯은 현재 대표 Base 한 종류에 설정돼 있으며 엘리트 몬스터는 제작 후 연결한다.
- [x] **그래픽과 내부 조합 확인:** 수관 이음새와 대표 인접 방의 반복 배치를 정리하고, 두 가로 Variant의 내부 지형 조합과 대표 연결을 임시 화면으로 확인했다. 실제 게임 조명과 모든 방 연결은 미검증이며 작은 확률 셀의 대비와 같은 방의 연속 배치는 추가 확인이 필요하다.
- [ ] **방 구조와 함정 배치:** 화살 함정·투사체 풀과 Spikes·Fire·Saw·SpikeHead·SpikedBall을 구현해 대표 방에 확률 배치했다. FallingPlatform은 다층 방의 플랫폼 패턴으로 설정했다. 가시·가시 공·낙하 발판이 함께 생성된 다층 방의 아래 경로를 체력 10으로 통과했다. 상층 경로의 무피해 통과는 미확인이다. 용암은 보류하며 [세부 단계](Chunk_Generation/Chunk_Generation_Progress.md)를 참고한다.
- [ ] **방별 진행 조건:** `WestSouth`와 `Down`에서 실제 생성된 대상에 적용 가능한 목표 규칙 하나를 선택하고 주 출구에 공통 문을 자동 배치한다. 실제 Player의 스위치 완료·남쪽 문 통과와 목표 미완료 상태의 선택 방 왕복을 확인했다. 몬스터 처치 후 개방과 다른 출구의 실제 통과는 미확인이다. 정식 목표 UI는 [UI 작업](../UI/UI_Progress.md)으로 이월한다. [세부 계획](Chunk_Generation/Work/Room_Progression_Plan.md).
- [ ] **난이도별 출현 확률:** 마지막 단계에서 난이도 설정에 따라 몬스터·아이템·함정의 슬롯별 출현 확률을 조절한다.

세부 완료 상태, 남은 확인과 다음 작업은 [Chunk 내부 생성 진행 현황](Chunk_Generation/Chunk_Generation_Progress.md)을 기준으로 한다.
