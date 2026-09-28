# Player Combo Attack 진입 책임 재정리 계획

## 목적

현재 콤보 공격의 **시작 가능 여부 판단**이 `PlayerAttackState.OnEnter()`에 들어가 있다. 이 때문에 상태가 진입한 뒤 실패를 판단하고, `PlayerAttackState`가 직접 `OnTransition`을 호출해 Grounded/Fall로 되돌리는 흐름이 생겼다.

이번 단계에서는 공격 시작 정책과 실패 처리를 재사용 가능한 `ComboAttackTransition`으로 이동한다. 현재 `PlayerComboAttackHandler`의 이름을 공용 의미의 `ComboAttackHandler`로 변경해 Transition에 직접 주입한다. `AttackState`는 공통 애니메이션 생명주기만, `PlayerAttackState`는 Player 전용 진입/실행 동작만 담당하도록 책임을 다시 분리한다.

## 현재 구조 점검

| 항목 | 현재 구현 | 판단 |
| --- | --- | --- |
| Event Rule 구독 해제 | `AgentStateBase.Exit()`가 Event Rule을 순회해 해제한 뒤 `OnExit()`을 호출 | 적절하다. 별도 `OnSubscribeEvent`/Controller 공통 해제 호출은 필요 없다. |
| 최초 AttackType 선택 | `AttackState.TryGetInitialAttackType(out int)`를 `PlayerAttackState`가 구현 | 선택된 값은 Base State의 `TryStartAttack` 호출에만 전달된다. State의 추상 계약으로 둘 이유가 약하다. |
| 시작 실패 처리 | `AttackState`가 `OnAttackStartFailed()`를 호출하고, Player State가 `OnTransition`을 직접 발행 | 상태 진입 실패를 상태가 복구하는 구조다. 전이를 통과시키기 전에 처리해야 한다. |
| 시작 후 Player 처리 | `OnAttackStarted(int)` 훅에서 지상 공격의 수평 이동을 중지 | `PlayerAttackState.OnEnter()` override로 표현하는 편이 호출 순서와 책임이 명확하다. |

## 결정 사항

1. `TryGetInitialAttackType(out int)`, `OnAttackStartFailed()`, `OnAttackStarted(int)`를 삭제한다.
2. `AttackState`는 입력 소비, 공격 타입 선택, 스태미나/공격 시작 실패를 판단하지 않는다.
3. `ComboAttackTransition`을 도입해 공격 요청 소비, AttackType 결정, `IAttackStarter.TryStartAttack()` 호출을 한 규칙 안에서 처리한다.
4. `PlayerComboAttackHandler`는 `ComboAttackHandler`로 이름을 변경한다. 콤보 타입 선택과 버퍼 초기화는 이 Handler가 직접 제공한다.
5. 시작 실패 시 Transition은 `false`를 반환한다. 현재 State는 유지되며, `PlayerAttackState`가 직접 상태 전이를 호출하지 않는다.
6. `AttackState.OnEnter()`는 공통 Animator 파라미터(`AttackType`, `IsAttack`)만 적용한다.
7. `PlayerAttackState.OnEnter()`는 `base.OnEnter()` 뒤 Player 전용 처리(지상 공격의 수평 이동 정지)를 수행한다.
8. `ComboAttackTransition`은 `ComboAttackHandler`만 참조한다. 이후 Combo Monster의 규칙이 실제로 달라질 때에만 Handler 설정값, 상속, 또는 별도 전략 분리를 검토한다. 현재는 인터페이스를 추가하지 않는다.

## 목표 흐름

```text
Grounded / Jump / Fall State
  └─ ComboAttackTransition.ShouldTransition()
      ├─ ComboAttackHandler.TryBeginAttack()
      │   └─ 입력 1회 소비 + 지상 1타 / 공중 3타 선택
      ├─ IAttackStarter.TryStartAttack(attackType)
      │   ├─ 성공: true 반환 → AttackState 전이
      │   └─ 실패: Combo 버퍼 정리 후 false 반환 → 현재 State 유지
      └─ AgentStateBase.OnTransition(typeof(AttackState))
          └─ PlayerAttackState.OnEnter()
              ├─ AttackState.OnEnter(): AttackType, IsAttack Animator 적용
              └─ Player 전용: 지상 공격이면 Motor 수평 이동 정지
```

`TryBeginAttack()`의 `out attackType`은 유지한다. 이 값은 Transition이 실제 공격 시작 명령에 전달할 결과이므로, 이전처럼 Base State의 템플릿 훅으로 존재하는 경우와 달리 의미가 명확하다.

`ComboAttackHandler`는 **입력 요청 소비, 최초 AttackType 선택, 실패/Exit 시 버퍼 초기화, 종료 Event 시 후속 콤보 판단**을 담당한다. 스태미나 소비와 실제 AttackType 적용은 계속 `IAttackStarter`/Controller의 책임이다. 현재 구현은 Player 규칙(지상 1타·공중 3타)을 사용하되, 이름과 주입 지점은 Combo Agent 공용으로 정리한다.

## 적용 순서

- [x] 1. 전이 규칙 책임 재배치
  - `PlayerComboAttackHandler` 파일·클래스·참조를 `ComboAttackHandler`로 변경한다.
  - `ComboAttackTransition`을 추가한다.
  - 생성자에서 `IAgentCombatInput`, `ComboAttackHandler`, `GroundDetector`, `IAttackStarter`를 받는다.
  - `ShouldTransition()`에서 `TryBeginAttack()`으로 요청을 소비하고 타입을 선택한다.
  - 선택한 타입으로 `TryStartAttack()`을 호출한다.
  - 실패 시 `ComboAttackHandler.ResetGroundCombo()`을 호출하고 `false`를 반환한다.
  - 성공 시 `true`를 반환하고, `NextStateType`은 기존과 같이 `typeof(AttackState)`로 유지한다.
  - `ComboAttackTransition`은 Player/Monster State나 Animator를 참조하지 않는다.

- [x] 2. AttackState 공통 생명주기 단순화
  - 생성자와 필드에서 `IAttackStarter` 의존성을 제거한다.
  - `OnEnter()`를 `protected virtual override`로 두고, 현재 `AgentCombatHandler.CurrentAttackType`을 Animator에 적용한 뒤 `IsAttack`을 활성화한다.
  - `TryGetInitialAttackType`, `OnAttackStartFailed`, `OnAttackStarted`와 관련 호출을 삭제한다.
  - `OnExit()`의 공격 타입/Animator 초기화 책임은 유지한다.

- [x] 3. PlayerAttackState 진입 처리로 이동
  - `TryGetInitialAttackType()` 및 `OnAttackStartFailed()` override를 제거한다.
  - `OnAttackStarted()` override를 제거한다.
  - `OnEnter()`를 override하여 먼저 `base.OnEnter()`를 호출한다.
  - 현재 AttackType이 공중 공격 타입이 아닐 때만 `Motor.StopHorizontal()`을 호출한다.
  - 콤보 입력 버퍼 수집(`OnExecute`)과 Exit 시 버퍼 정리(`OnExit`)는 유지한다.

- [x] 4. PlayerStateFactory 등록 변경
  - Grounded/Jump/Fall State에 등록한 기존 `AttackTransition`을 `ComboAttackTransition`으로 교체한다.
  - 세 규칙이 동일한 `ComboAttackHandler`와 `IAttackStarter`를 참조하도록 Factory Data/생성 순서를 정리한다.
  - State Dictionary의 키 `typeof(AttackState)`와 값 `PlayerAttackState` 등록 방식은 유지한다.

- [x] 5. 불필요한 공용 코드 제거 확인
  - 기존 `AttackTransition`은 콤보 규칙이 없는 Agent가 사용할 기본 공격 요청 감지 Rule로 유지한다.
  - `ComboAttackTransition`은 `AttackTransition`과 독립적으로 `ITransitionRule`을 구현하며, Combo Handler 기반의 시작 판정을 담당한다.
  - `AttackState` 및 `PlayerAttackState`에 남아 있는 직접 `OnTransition?.Invoke(...)` 호출이 없는지 확인한다.
  - 공격 시작 실패 후 `CurrentAttackType`과 Combo 버퍼가 남지 않는지 확인한다.
  - `ComboAttackTransition`에 Player State, Player Animator 의존성이 없는지 확인한다.

- [ ] 6. 검증
  - [x] C# 재컴파일 및 Console 오류를 확인한다. (2026-09-28: 컴파일 실패 없음, Console 오류 0건)
  - [ ] 지상 A 입력: 1타가 시작되는지 확인한다.
  - [ ] 공중 A 입력: 3타가 시작되는지 확인한다.
  - [ ] 스태미나 부족 또는 시작 불가 조건: AttackState로 전이하지 않고 기존 Grounded/Jump/Fall State가 유지되는지 확인한다.
  - [ ] Attack 종료 Event 직전 입력: 1 → 2 → 3 진행이 유지되는지 확인한다.
  - [ ] Attack 종료 후 다른 행동: `OnExit()`에서 콤보 버퍼가 초기화되는지 수동 확인한다.

## 완료 기준

- `AttackState`에는 공격 입력, 최초 AttackType 선택, 시작 실패 복구 훅이 없다.
- `PlayerAttackState`는 직접 `OnTransition`을 호출하지 않는다.
- Player의 지상/공중 공격 시작 정책은 `ComboAttackHandler`에, 공통 시작/실패 흐름은 `ComboAttackTransition`에 있다.
- 현재 단계에서는 인터페이스를 추가하지 않는다. Combo Monster의 규칙이 실제로 달라질 때, 공통 Handler의 설정화 또는 확장 구조를 판단한다.
- 공통 Attack Animator 설정은 `AttackState.OnEnter()`에, Player 전용 이동 정지는 `PlayerAttackState.OnEnter()`에 있다.
- 공격 시작 실패 시 상태 전이가 발생하지 않고 입력/콤보 상태만 안전하게 정리된다.
