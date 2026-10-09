# Player 콤보 공격 리팩토링 요약

키별로 공격 종류를 선택하던 구조를 단일 입력 콤보로 전환하고, 입력·FSM·Animator의 책임과 공격 종료 흐름을 단계적으로 정리했다.

## 주요 개선 과정

| 단계 | 핵심 내용 |
| --- | --- |
| 단일 입력 콤보 도입 | A/S 키로 공격 종류를 지정하던 방식에서 A 키 하나로 지상 1 → 2 → 3타를 연결하도록 변경. 공중 공격은 단발 동작으로 분리. |
| 책임과 수명 정리 | 공통 공격 처리와 Player 전용 규칙을 분리. 공격 시작 가능 여부를 State 진입 전에 판단하고, 이벤트 구독·해제를 State의 Enter/Exit 수명으로 통일. |
| 잔여 입력·연타 오류 보완 | 마지막 타격 중 입력이 다음 공격을 자동 시작하거나, 빠른 연타로 콤보 단계를 건너뛰는 문제에 대응. 입력 허용 구간·폐기·소비 시점을 정리. |
| 공격 입력 계약 통일 | Player와 AI가 공용 공격 요청 이벤트를 사용하도록 변경. Player의 공용 입력 참조 누락과 복잡한 이벤트 주입 구조를 정리. |
| 콤보 구조 단순화 | 콤보 전용 Transition을 제거하고 공용 공격 진입·종료 전이를 재사용. 이후 현재 구조에서는 별도 Combo Handler도 제거하고, PlayerAttackState의 입력을 Animator의 ComboTrigger로 전달. |
| 공격·마무리 모션 분리 | 공격 Clip에는 타격 이벤트, End Clip에는 종료 이벤트를 배치. 콤보 성공 시 다음 공격으로 바로 연결하고, 연계가 끝나면 마무리 모션 후 FSM 종료. |

## 최종 구조와 의미

- **FSM**: 하나의 PlayerAttackState에서 콤보 전체를 유지하고, 공용 Transition으로 공격 진입·종료를 처리한다.
- **Animator**: ComboTrigger와 전이 시점으로 세부 타격·마무리 모션을 연결한다. State 종료 시 남은 Trigger를 초기화한다.
- **개선 의미**: 입력 누적과 종료 경계 문제를 다루면서, 공통 공격 구조를 유지하고 Player 전용 동작을 확장하는 책임 구분을 정리했다.

초기 Combo Handler·AttackType 증가 설계는 개선 과정의 중간 단계다. 현재 구조와 동일한 구현으로 소개하지 않는다. Plan7에는 에셋·정적 검증과 독립 Animator 평가 기록이 있으며, 실제 Player의 Play 모드 종합 검증은 별도 확인 항목으로 남아 있다.

참고: [Plan1](Player_Combo_Attack_Refactoring_Plan1.md), [Plan2](Player_Combo_Attack_Refactoring_Plan2.md), [Plan3](Player_Combo_Attack_Refactoring_Plan3.md), [Plan4](Player_Combo_Attack_Refactoring_Plan4.md), [Plan5](Player_Combo_Attack_Refactoring_Plan5.md), [Plan6](Player_Combo_Attack_Refactoring_Plan6.md), [Plan7](Player_Combo_Attack_Refactoring_Plan7.md).
