# FSM 개발 진행 현황

Player, 지상 Monster, Flying Monster와 Boss 1종의 개발·검증 상태와 다음 작업을 정리한다. 작업 방법은 Guide를, 상세 변경·검증 이력은 [리팩토링 로그](Refactoring_Log/FSM_Refactoring_Source_Log.md)를 기준으로 한다.

> 기준: 2026-11-08 소스·에셋 확인과 기존 검증 기록. 실제 작업 전에는 현재 코드와 에셋을 다시 확인한다.

## 현재 상태

| 대상 | 상태 | 현재 구성·다음 작업 |
| --- | --- | --- |
| 기존 FSM | 진행 중 | 공용 State·Transition·Factory와 Player·지상 Monster 구성이 있음. 현재 연결과 기존 검증 이력을 대조한다. |
| Player | 구현 완료/검증 필요 | Grounded·Jump·Fall·Dash·Attack·Hit·Death 구성. 이동·공격·중단·복귀를 현재 에셋 기준으로 회귀 검증한다. |
| 지상 Monster | 구현 완료/검증 필요 | 공용 판단 FSM과 Goblin·Skeleton Variant가 있음. 두 몬스터의 전체 행동·범위·밸런스를 확인한다. |
| Flying Monster | 진행 중 | Flying Motor·이동 Handler·추상 Controller·FactoryData가 있음. 구체 행동·공격·에셋·검증은 별도 설계와 제작이 필요하다. |
| Boss 1종 | 미시작 | 보스 FSM 조사 자료가 있음. 대상·패턴·전이·중단·사망 요구사항을 정하고 제작한다. |
| 통합 마무리 | 미시작 | 공용 코드 영향, Agent 간 회귀와 문서·설정 연결을 확인한다. |

상태는 **미시작·진행 중·구현 완료/검증 필요·완료·보류**를 사용한다. 구현·연결 확인과 실제 Play 검증은 구분하며, 필요한 검증이 남아 있으면 완료로 표시하지 않는다.

## 단계별 작업

### 1. 기존 FSM 확인 — 진행 중

목표: 이후 작업의 기준이 되는 현재 구현과 검증 상태를 확인한다.

- [x] Player·지상 Monster의 Factory와 Flying 이동 기반을 소스에서 확인한다.
- [x] Skeleton Variant·공격 에셋과 기존 검증 이력을 확인한다.
- [ ] 작업 시작 시 대상 Prefab·Animator·Animation Event와 관련 로그를 현재 상태에 맞게 대조한다.

완료 기준: 대상별 실제 연결과 남은 검증을 확인하고 후속 단계의 기준을 갱신한다.

### 2. Player 마무리 — 구현 완료/검증 필요

목표: 직접 입력과 FSM·Animator가 정상 실행과 중단 후에도 일치한다.

- [ ] 지상 이동, Jump·Fall·착지, Dash 반복·벽 접촉·공중 Dash 착지 초기화를 확인한다.
- [ ] 지상 콤보 1→2→3, 공중 공격, End Event·이동 재개와 공격 입력 정리를 확인한다.
- [ ] 공격·Dash 중 피격과 사망, Hit 종료 후 복귀, Stamina·속도·Trigger 정리를 확인한다.
- [ ] 지상 단발·2타·3타·빠른 연타와 분기 직전/직후 입력에서 올바른 End 모션과 종료 Event가 한 번 발생하는지 확인한다.
- [ ] 최종타·End 중 입력 누수, 공중 공격 중 착지, 피격 중단, 종료 포즈와 이동·점프 재개 시점을 확인한다.
- [ ] 단계별 피해·범위와 공중 다단 타격·진입당 Stamina를 확인하고, Combat Editor의 SO 선택·Handle·Undo·Preview와 실제 타격 범위를 비교한다.

완료 기준: 현재 Player Prefab과 Animator로 정상·반복·중단 흐름을 Play에서 확인하고 발견한 문제를 해결한다.

### 3. 지상 Monster 마무리 — 구현 완료/검증 필요

목표: Goblin·Skeleton이 공용 FSM을 사용하면서 각 공격 설정과 모션에 맞게 동작한다.

- [x] Skeleton의 대상 인식·추적과 피격에 따른 체력 감소를 관찰했다. 일반·배후 공격 재생은 사용자 확인을 받았다.
- [ ] Goblin·Skeleton의 순찰·추적·대상 상실 대기·복귀·재인식을 현재 FSM에서 확인한다.
- [ ] 일반·배후 공격의 타격 시점·범위·간격과 피격·사망 중단 및 종료 후 전이를 확인한다.
- [ ] Skeleton과 Goblin의 Collider·공격 범위·모션 속도·밸런스를 확인하고 필요한 종별 설정을 조정한다.
- [ ] 같은 공격 SO를 장착한 두 개체의 참조·실행 상태·타이머가 독립적인지, 원본에 실행 상태가 남지 않고 개체 제거 시 해당 복제본이 정리되는지 확인한다.
- [ ] 배후 준비 중 대상 상실·재인식과 Disable/Enable 시 SO 재사용, 정상 종료 후 공격 간격을 확인한다.

완료 기준: 두 Variant의 정상·상실·중단·사망 흐름과 개체별 공격 실행을 Play에서 확인한다.

### 4. Flying Monster 제작 — 진행 중

목표: 공중 이동과 요구된 공격을 수행하는 Flying Monster를 제작한다.

- [x] 기존 비행 이동 기반과 Flying Eye 원본 에셋이 있다.
- [ ] 대상 행동·상태·전이·공격 및 피격·사망 정책을 현재 구조 기준으로 확정하고 개별 계획을 작성한다.
- [ ] 필요한 State·Transition·공격 실행과 종별 Sprite·Animation·SO·Prefab을 구성한다.
- [ ] 공중 이동·공격·벽·중단·복귀를 검증하고 Player·지상 Monster 회귀를 확인한다.

완료 기준: 확정된 행동과 공격이 Prefab에서 동작하고 정상·중단 흐름을 Play에서 확인한다.

### 5. Boss 1종 제작 — 미시작

목표: 패턴 선택부터 피격·사망까지 동작하는 보스 한 종류를 완성한다.

- [ ] 대상 에셋, 전투 시작·패턴·상태 전이·대상 인식·피격·사망 조건을 정한다.
- [ ] 요구사항에 맞춰 FSM·공격 실행·Animator·데이터 및 Prefab을 설계하고 제작한다.
- [ ] 패턴 전환·피격/사망 중단·판정 정리와 전투 종료를 검증한다.

완료 기준: 확정된 보스 전투의 시작부터 패턴 수행·중단·사망까지 Play에서 확인한다. 구체적인 패턴과 구현 방식은 별도 설계에서 결정한다.

### 6. 통합 마무리 — 미시작

목표: 완성한 Agent들이 함께 동작하고 후속 작업을 위한 문서가 현재 구조를 반영한다.

- [ ] 공용 State·전이·공격·Animator 변경에 따른 Player와 각 Monster의 회귀를 확인한다.
- [ ] Prefab·SO·Animation Event·필수 참조와 Console을 확인한다.
- [ ] 현재 검증 결과와 남은 문제를 로그에 기록하고 관련 Guide·진행 현황을 갱신한다.

완료 기준: 각 대상의 필수 검증과 공용 변경 회귀가 끝나고 미해결 사항이 정리된다.

## 보류·의존성

- **Managers 연결:** InGame 직접 실행 중 InGameUI 초기화와 Player 사망 처리 오류가 관찰됐다. 해당 경로 검증 전 현재 재현 여부와 실행 환경을 확인하고 필요한 경우 별도 수정한다.
- **Player Dash 참조 이력:** 과거 로그에는 Player Variant의 DashHandler 누락이 기록되어 있으나 현재 Variant에 컴포넌트가 있다. 현재 Prefab 초기화와 Play 동작을 다시 확인한다.
- **Flying·Boss 세부 요구사항:** 공격·상태·전이 및 보스 패턴은 해당 작업에서 확정한다. 조사 자료와 과거 제안은 완성된 구현으로 간주하지 않는다.

## 관련 문서와 갱신

- [AI 협업 개발 절차](../AI_Assisted_Development_Workflow.md) · [Plan 작성 기준](../Skill/Plan_Guide.md)
- [FSM Agent 제작](ProjectSkill/FSM_Agent_Creation_Guide.md) · [State 확장](ProjectSkill/FSM_State_Extension_Guide.md) · [지상 Monster 제작](ProjectSkill/FSM_Grounded_Monster_Creation_Guide.md) · [Player 공격 확장](ProjectSkill/Player_Attack_Extension_Guide.md)
- [FSM 리팩토링 로그와 공격 SO 전환 이력](Refactoring_Log/FSM_Refactoring_Source_Log.md) · [Boss FSM 조사 기록](Hollow_Knight_Boss_Research/Research_Notes.md)

각 작업을 마치면 이 문서의 상태·체크리스트·다음 작업을 갱신한다. 상세 변경과 관찰 근거는 로그에 남기고, 반복 작업 절차가 바뀌면 해당 Guide를 갱신한다. 확인하지 않은 결과는 미검증으로 유지한다.
