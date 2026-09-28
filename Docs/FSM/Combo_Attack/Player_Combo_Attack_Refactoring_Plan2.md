# Player Combo Attack 후속 리팩토링 계획

## 목적

현재 Player 콤보 공격 구현에서 불필요한 간접 호출을 줄이고, 상태 종료·Combat 책임·공격 가능 여부 검증을 명확히 분리한다.

이번 단계에서는 `PlayerAttackState`를 유지한다. 다만 이후 콤보를 사용하는 Monster가 추가될 때 공통 계층으로 추출할 수 있도록 의존 방향과 인터페이스 경계를 정리한다.

## 결정 사항

| 항목 | 결정 |
| --- | --- |
| AttackTransition 시작 타입 | `Func<int>`와 시작 타입 인자를 모두 제거한다. Transition은 소비되지 않은 Attack 요청만 확인하고, 지상 1타·공중 3타 선택은 PlayerAttackState의 PlayerComboAttackHandler가 결정한다. |
| Event Rule 정리 | 이번 단계에서 `AgentStateBase.Exit()`가 Event Rule 전체 구독 해제와 공통 종료를 담당하도록 전환한다. 하위 State는 `OnExit()`만 구현하며, `AgentController.ChangeState()`의 직접 구독 해제 호출은 제거한다. |
| Combo 타입 변경 책임 | `AgentCombatHandler.TryAdvanceAttackType()`처럼 순서를 강제하는 Combo 규칙은 공용 Handler에서 제거한다. 공용 Handler는 현재 공격 타입 보관·공격 판정만 담당한다. |
| Combo 확장 | 지금은 `PlayerAttackState`와 `PlayerComboAttackHandler`를 유지한다. 두 번째 콤보 Agent가 필요해질 때 `ComboAttackState` 및 공통 Combo Handler 계약으로 추출한다. |
| 공중 공격 종료 | `AttackEndTransition`이 Grounded/Fall을 직접 선택하는 현재 방식을 유지한다. Grounded를 한 프레임 경유하지 않는다. |
| 공격 실행 API | `CanStartAttack`은 제거하고, 최초 공격은 `TryStartAttack`, 후속 콤보는 `TryContinueAttack`으로 명명한다. 두 함수는 모두 성공 시 스태미나 소비와 AttackType 적용까지 완료하는 명령형 API다. State는 이 두 `Try` API만 호출한다. |
| CombatHandler 판별 API | `AgentCombatHandler`에는 공격 타입 데이터 범위와 타입 적용 가능 여부만 확인하는 공통 `CanApplyAttackType(int attackType)` 하나를 둔다. `1 → 2 → 3` 순서, 지상 여부, 스태미나 정책은 포함하지 않는다. |
| 문서화 | `PlayerComboAttackHandler`의 상태 변수·입력 버퍼·종료 Event 처리 함수에 의도 중심 주석을 추가한다. |

## 목표 구조

```text
AgentStateBase
  ├─ Enter()
  ├─ Execute()
  ├─ Exit()                    // Event Rule 전체 구독 해제 + OnExit 호출
  └─ OnExit()                  // State별 Animator/Handler 정리

AttackState                    // 공통 공격 생명주기
  └─ PlayerAttackState         // Player의 지상 1타 / 공중 3타 정책
      └─ PlayerComboAttackHandler
          ├─ 입력 버퍼 관리
          ├─ 1 → 2 → 3 순서 결정
          └─ 종료 Event 시 다음 타격 진행 여부 결정

AgentCombatHandler
  ├─ CanApplyAttackType(attackType) // 공용 타입 유효성·적용 가능 여부
  ├─ CurrentAttackType 보관
  └─ PerformAttack()

PlayerController 또는 Player 전용 실행 계층
  ├─ TryStartAttack(attackType)    // 최초 공격: 정책 확인 + 스태미나 소비 + 타입 적용
  └─ TryContinueAttack(attackType) // 후속 공격: Player 콤보 정책 확인 + 스태미나 소비 + 타입 적용
```

향후 Monster도 동일한 콤보 규칙을 사용할 때만 아래 구조로 확장한다.

```text
AttackState
  └─ ComboAttackState
      ├─ PlayerAttackState
      └─ MonsterComboAttackState
```

`ComboAttackState`는 두 구현체가 실제로 동일한 입력 유효 구간·버퍼·종료 Event 규칙을 공유할 때만 만든다. 단순히 공격이 여러 개인 Monster에는 적용하지 않는다.

## 적용 순서

- [x] 1. AttackTransition의 시작 타입 간소화
  - `Func<int> _requestedAttackType`을 제거한다.
  - 공격 확정 API가 Transition에서 분리되었으므로 `int requestedAttackType`도 보관하지 않는다.
  - Transition은 Attack 요청 존재만 판단하며, 지상/공중 타입 선택은 PlayerComboAttackHandler가 담당한다.

- [x] 2. State 종료 공통 흐름으로 전환
  - 모든 `AgentStateBase` 하위 State의 기존 `Exit()` 구현을 목록화한다.
  - 각 구현을 Event Rule 구독 해제와 State별 정리 로직으로 구분한다.
  - `AgentStateBase.Exit()`에서 `UnsubscribeEventRules()`를 호출하고 `OnExit()`를 호출하는 구조로 전환한다.
  - 하위 State의 `Exit()`는 `OnExit()`로 변경한다.
  - 같은 변경에서 `AgentController.ChangeState()`의 직접 `UnsubscribeEventRules()` 호출을 제거한다.
  - Event Rule이 자신을 먼저 해제해도 전체 해제가 안전하도록 `Unsubscribe()`의 idempotent 특성을 유지한다.

- [x] 3. Combo 타입 변경 책임 분리
  - `AgentCombatHandler.TryAdvanceAttackType()`를 제거 대상으로 지정한다.
  - 공용 Handler에는 `CanApplyAttackType(int attackType)` 하나를 둔다. 이 함수는 공격 데이터 범위와 타입 적용 가능 여부만 확인하며, 콤보 순서·지상 여부·스태미나는 판단하지 않는다.
  - `PlayerComboAttackHandler`가 다음 타입을 결정하고, Player 전용 실행 계층이 해당 타입의 시작·연속 실행 정책과 실제 적용을 담당하도록 설계한다.
  - Monster 콤보가 필요해질 때 재사용 가능한 최소 계약(`IComboAttackExecutor` 등)을 추출한다. 현재 단계에서는 인터페이스를 선제 생성하지 않는다.

- [x] 4. 공격 시작 API 단일화
  - `IAttackStarter.CanStartAttack()`과 `PlayerController.CanStartAttack()`을 제거한다.
  - 최초 공격과 후속 콤보의 명령형 API를 각각 `TryStartAttack(int attackType)`, `TryContinueAttack(int attackType)`으로 통일한다.
  - 두 `Try` 함수는 공통으로 스태미나 검사·소비와 `AgentCombatHandler.CanApplyAttackType()` 검증을 수행한 뒤 AttackType을 적용한다.
  - `TryStartAttack()`은 CurrentAttackType이 비어 있는 최초 공격만 허용한다.
  - `TryContinueAttack()`은 PlayerComboAttackHandler가 결정한 다음 타입만 허용한다. `1 → 2 → 3` 순서 자체는 CombatHandler가 아닌 Player 콤보 정책에서 보장한다.
  - `AttackTransition`은 공격 요청 존재 여부만 판단하고, 실제 공격 확정은 `PlayerAttackState.OnEnter()`의 `TryStartAttack()` 호출에서 수행한다.
  - 후속 콤보는 종료 Event 처리에서 `TryContinueAttack(nextAttackType)`으로 확정한다.
  - 공격 확정 실패 시 `PlayerAttackState`가 Grounded/Fall로 안전하게 복귀하는 현재 보호 로직을 유지한다.

- [x] 5. PlayerComboAttackHandler 주석 보강
  - `_groundComboStep`, `_queuedAttackCount`, `_isAcceptingComboInput`에 각 상태 값의 범위와 생명주기를 설명하는 주석을 작성한다.
  - `TryBeginAttack()`에는 지상 1타와 공중 3타를 분기하는 이유를 작성한다.
  - `CaptureBufferedRequests()`에는 남은 콤보 수를 넘는 입력을 버리는 이유와 입력 유효 구간을 설명한다.
  - `TryAdvanceGroundCombo()`에는 End Event가 발생해도 FSM을 이탈하지 않고 Animator 내부 전이로 진행하는 이유를 작성한다.
  - `ResetGroundCombo()`에는 State Exit, 피격, Input Block에서 호출되는 정리 함수임을 작성한다.

- [ ] 6. 검증
  - C# 재컴파일 후 오류가 없는지 확인한다.
  - Grounded/Jump/Fall AttackTransition이 각각 1/3 타입을 고정 인자로 전달하는지 확인한다.
  - Attack 종료·피격·사망·Input Block 이후 Event Rule 구독과 콤보 버퍼가 남지 않는지 확인한다.
  - 지상 1→2→3, 공중 3타, 스태미나 부족, 종료 Event 직후 신규 입력을 수동 테스트한다.

## 완료 기준

- `AttackTransition`에 `Func<int>`나 시작 타입 인자가 없다.
- State 종료 시 Event Rule 구독 해제가 한 곳에서 일관되게 수행된다.
- `AgentCombatHandler`에 Combo 순서를 강제하는 규칙이 없다.
- `CanStartAttack`은 없으며, 최초·후속 공격을 확정하는 명령형 API는 각각 `TryStartAttack`, `TryContinueAttack`으로 통일된다.
- `AgentCombatHandler`의 공용 판별 함수는 `CanApplyAttackType` 하나이며 Combo 순서를 강제하지 않는다.
- PlayerComboAttackHandler의 입력 버퍼와 End Event 처리 의도가 코드 주석으로 확인된다.
- 공중 Attack 종료는 Grounded를 경유하지 않고 FallState로 복귀한다.
