# Player·Monster 공격 SO 전략 전환 계획

## 목표

공격 수치와 실행 로직을 공격별 ScriptableObject에 통합한다. Handler는 장착·선택·실행 수명을 관리하고, 공격 종류별 생성 분기를 제거한다.

## 구조와 유지 조건

- Player는 `PlayerAttackSO`와 지상 콤보·공중 파생 SO, Monster는 `MonsterAttackSO`와 일반·배후 파생 SO를 사용한다. 두 영역을 묶는 범용 공격 기반 클래스는 추가하지 않는다.
- Player SO는 단계별 `AttackData[]`, Monster SO는 타격 데이터·공격 거리·공격 간격을 보관한다. 배후 접근 제한 시간은 배후 SO에만 둔다.
- 배후 SO는 근접 SO를 상속하여 타격·모션·종료 처리를 재사용한다. 별도 하위 SO·실행 전략 객체는 생성하지 않는다.
- Player 장착은 역할·Animator 진입값·SO로 구성한다. Monster는 Animator 진입값·SO 목록으로 구성하고, 거리와 기존 공격 간격을 만족하는 항목 중 동일 확률로 선택한다.
- Handler 초기화 때 각 장착 SO를 한 번 복제하고 의존성을 주입한다. 런타임 참조·Status·타이머·구독은 복제본에만 보관한다. 원본 수정은 다음 초기화부터 반영한다.
- SO는 `Initialize`·`Begin`·타격 Event·`End` 계약으로만 실행한다. Monster의 지속 행동은 기존 State Execute에서 호출한다. SO의 Unity 생명주기 콜백에서 공격을 시작하지 않는다.
- State 진입 전략 고정, Stamina 진입당 한 번 소비, 지상 유효 단계 1~3, 공중 다단 타격, Monster 현재/다음 공격 분리와 공격 간격 정책을 유지한다.
- Controller 파괴 시 기존 State Exit 후 Handler.Dispose에서 실행 복제본을 제거한다. 비활성화·재활성화의 기존 정책은 유지한다.
- FSM·전이 우선순위·Animator·Clip·Event 시점·공용 타격 계산은 변경하지 않는다.

## 적용 순서

- [x] 기존 Player 4개·Goblin 2개 SO 수치, Prefab·Variant·Scene의 장착값과 Override 확인.
- [x] 공격 SO 기반 클래스와 네 구현 추가, Handler·Controller 실행 수명 연결.
- [x] 새 SO에 기존 데이터 복사 후 Editor API로 장착 참조 이전.
- [x] Combat Editor의 단계 선택·SO 편집·Undo·Handle·Animation Preview 연결.
- [x] 참조 검증 후 기존 데이터 SO·전략 클래스·인터페이스 제거.
- [x] 공격 확장 가이드·최신 구조 설명·리팩토링 로그 갱신.
- [x] 컴파일·참조·설정 보존 확인.
- [ ] 사용자 조작으로 Player·Goblin 회귀 및 개체별 실행 상태 독립 확인.

## 에셋 이전

- Player 지상 1·2·3타는 콤보 SO 하나에 순서대로 복사하고 공중 데이터는 별도 SO에 보관한다. Goblin 일반·배후는 각각 독립 SO로 생성한다.
- 새 전략 에셋은 새 GUID를 사용한다. 기존 수치·모션 진입값·Prefab Override의 의미를 보존한다.
- Combat Editor는 원본 SO를 편집하며 실행 복제본에 자동 동기화하지 않는다.
- Unity 에셋은 Editor API로 편집한다. 미저장 Scene은 자동 저장·재로드하지 않는다. 연결 불가 시 수동 이전 위치·값을 안내한다.
- 임시 Migration·Verification 스크립트는 만들지 않는다. 기존 PDF 재제작은 제외한다.

## 검증 기준

- 같은 원본 SO를 장착한 개체의 주입 참조·Status·접근 제한 시간이 독립적이고 원본에는 실행 상태가 남지 않는다.
- 공격 종료·피격·사망·파괴 후 입력 구독·Trigger·범위 표시·이동·실행 복제본이 남지 않는다.
- Player 단계별 피해·범위·공중 타격 횟수·Stamina와 Goblin 선택·배후 실패·상실·공격 간격이 유지된다.
- 새 Monster 공격 SO를 Handler 종류 분기 수정 없이 장착할 수 있다.
- Combat Editor·Gizmo·실제 타격 범위가 일치하며 Missing Script·누락 설정·컴파일 오류가 없다.
- Play 검증은 조작 방법과 기대 결과를 먼저 안내하고 사용자 조작 중 상태·Animator·Console을 확인한다.

## 설정 확보 결과

- InGame은 Edit Mode·저장 상태. 공격 SO를 직접 참조하는 에셋은 Player.prefab과 Goblin.prefab 두 개.
- ProjectRE_Player Variant와 InGame의 두 개체에는 공격 설정 Override 없음. Goblin은 BasicMonster의 Variant로 공격 2개 장착을 Override.
- Player Animator 진입값은 지상 1·공중 3, Goblin 일반·배후 모두 1. 기존 타격·거리·시간 수치는 Editor API로 확보.

## 검증 결과와 수동 확인

- Unity 최종 컴파일 성공. Player·Variant·BasicMonster·Goblin 및 열린 InGame의 Missing Script 0.
- Player 지상 3단계·공중 데이터와 Goblin 타격·거리·간격·배후 제한을 원본과 비교하여 모두 일치 확인.
- Player·Variant의 역할/모션값과 Goblin의 장착 Override 유지. 열린 Scene 인스턴스도 새 SO 참조를 상속하며 InGame dirty=false.
- 삭제 타입·구형 SO GUID의 소스·Prefab·Scene·에셋 잔여 참조 없음. git diff --check 통과.
- 현재 Console groundTruth 오류 0. Pipeline 이력에는 이전 Editor Assert 2개·이번 대규모 읽기 조회의 5초 timeout 1개가 남아 있으며 공격 코드 오류와 구분. 로그를 지우지 않음.
- 실제 Play·Editor GUI 조작·복제본 수명 검증은 아래 사용자 조작으로 확인 대기. 코드 구현을 해당 검증 완료로 표시하지 않음.

| 사용자 조작 | 확인할 결과 |
|---|---|
| InGame Play 후 지상 공격 단발·연타, 공중 공격 | 1→2→3 콤보·단계별 범위·기존 공중 타격, 시작 시 Stamina 한 번 소비 |
| 공격 중 피격 후 재공격 | Hit 종료 복귀·입력 구독·잔여 Trigger·Preview 정상 정리 |
| Goblin 공격 거리 진입 후 반복 관찰 | 일반 공격 또는 배후 이동 후 공격, 정상 종료 후 기존 1초 간격 |
| 배후 준비 중 시야 이탈·재진입, Goblin Disable/Enable | 상실 대기·복귀·재추적과 기존 수명 유지, SO 재생성 없음 |
| 필요 시 Edit Mode에서 Goblin을 수동 복제한 뒤 Play | 두 개체의 대상·상태·타이머 독립, 사망 제거 후 해당 실행 SO 제거. 임시 복제는 저장하지 않고 정리 |
| Combat Editor에서 역할·단계 선택, 범위 편집·Undo·Preview | 원본 설정·Handle·타격 범위 일치. Play 중 원본 수정은 현재 실행본에 자동 반영하지 않음 |
