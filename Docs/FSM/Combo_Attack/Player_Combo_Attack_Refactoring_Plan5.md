# Player Combo Attack 입력 이벤트 주입 계획

## 목적

`PlayerInput`에서 Combo 관련 요청 카운트와 소비 API를 제거하고, Input System 이벤트 발행만 담당하도록 정리한다.

Combo의 시작 대기 입력, 1→2→3 예약 입력, 마지막 타격 입력 폐기는 모두 `ComboAttackHandler`가 소유한다. `PlayerController`는 다른 Handler와 동일하게 `Initialize()`를 통해 입력 구독/해제 함수를 주입하고, Handler의 생명주기를 관리한다.

## 현재 구조와 한계

```text
PlayerInput
  ├─ _attackRequestCount
  ├─ HasAttackRequest / TryConsumeAttackRequest()
  └─ OnAttackRequestsCleared

ComboAttackHandler
  ├─ _groundComboStep
  ├─ _queuedAttackCount
  └─ _isAcceptingComboInput

ComboAttackTransition / PlayerAttackState / AttackEndTransition
  └─ IAgentCombatInput을 통해 PlayerInput의 요청 카운트를 직접 소비
```

현재 입력 대기열은 `PlayerInput`, 실제 Combo 예약은 `ComboAttackHandler`에 각각 있다. 마지막 3타의 요청 폐기처럼 Combo 상태가 필요한 판단을 Handler가 하려면 Input의 요청 큐를 반복 소비해야 하므로, 입력 상태와 Combo 정책의 경계가 분산된다.

`IsGroundComboActive`는 현재 코드베이스에서 선언 외 참조가 없으므로 이번 변경에서 제거한다.

## 결정 사항

| 항목 | 결정 |
| --- | --- |
| 공격 입력 전달 | `PlayerInput`은 `OnAttackRequested` 이벤트만 발행한다. |
| Handler 초기화 | `ComboAttackHandler.Initialize()`에 Action 기반 구독/해제 함수를 주입한다. |
| UniRx | 사용하지 않는다. Attack은 단발 신호이며, 현재 요구에는 스트림 조합·시간 연산·구독 체인이 필요 없다. |
| 입력 차단 | `PlayerInput`은 Input Block 발생 이벤트를 발행하고, Handler는 주입된 구독을 통해 Combo 상태를 초기화한다. |
| 초기 공격 대기열 | `ComboAttackHandler`가 보관한다. 기존처럼 여러 입력 요청을 보존해 동작 변화를 최소화한다. |
| Combo 예약 | `ComboAttackHandler`가 현재 타격의 허용 구간과 남은 수를 기준으로 직접 저장/폐기한다. |
| 공용 `IAgentCombatInput` | PlayerInput에서는 제거한다. AI/일반 AttackTransition에서 사용하는 기존 계약은 유지한다. |
| `AgentController.CombatInput` | Player에는 null이 될 수 있으나, Player Combo 경로에서 더 이상 참조하지 않는다. AI/일반 Agent의 기존 사용은 유지한다. |

## 목표 구조

```text
PlayerInput
  ├─ OnAttackRequested
  └─ OnInputBlocked

PlayerController
  └─ ComboAttackHandler.Initialize(
       attack 구독/해제 Action,
       input block 구독/해제 Action)

ComboAttackHandler
  ├─ _pendingStartAttackCount       // AttackState 밖에서 시작을 기다리는 입력
  ├─ _isAttackActive                // AttackState 활성 여부
  ├─ _groundComboStep               // 현재 지상 Combo 단계
  ├─ _queuedAttackCount             // 다음 Combo 예약 수
  ├─ _isAcceptingComboInput         // 다음 Combo 허용 구간
  ├─ RegisterAttackRequest()
  ├─ TryBeginAttack(isGrounded, out attackType)
  ├─ TryAdvanceGroundCombo(...)
  └─ Reset() / Deinitialize()

ComboAttackTransition
  └─ Handler의 시작 대기 입력을 확인·소비하고 AttackState 진입 여부 판단

PlayerAttackState / AttackEndTransition
  └─ IAgentCombatInput 없이 Handler의 Combo 상태만 사용
```

## 입력 처리 정책

| 시점 | `RegisterAttackRequest()` 동작 |
| --- | --- |
| AttackState 밖 | `_pendingStartAttackCount` 증가. 다음 `ComboAttackTransition`이 최초 공격 시작에 소비 |
| 지상 1타/2타의 허용 구간 | 남은 Combo 수만큼 `_queuedAttackCount` 증가 |
| 지상 1타/2타의 초과 입력 | 소비하지 않고 저장하지 않음. 즉시 폐기 |
| 지상 3타 | 저장하지 않음. 즉시 폐기 |
| 공중 3타 | 저장하지 않음. 즉시 폐기 |
| Input Block | 시작 대기 입력·Combo 예약·현재 Combo 상태를 모두 초기화 |

`_isAttackActive`가 필요하다. 공중 3타는 지상 Combo 단계가 `0`이므로, `_groundComboStep`만으로는 “AttackState 밖”과 “공중 3타 진행 중”을 구분할 수 없다.

## 수정 대상

| 파일 | 변경 |
| --- | --- |
| `Assets/Scripts/FSM/Player/Input/PlayerInput.cs` | `IAgentCombatInput` 구현, `_attackRequestCount`, `HasAttackRequest`, `TryConsumeAttackRequest`, `ClearAttackRequests`, `OnAttackRequestsCleared`를 제거한다. `OnAttackRequested`, `OnInputBlocked` 이벤트를 발행한다. |
| `Assets/Scripts/FSM/Player/PlayerState/ComboAttackHandler.cs` | 입력 구독 초기화/해제, 시작 대기열, Attack 활성 상태, 이벤트 수신 처리, Combo 예약/폐기, Reset을 구현한다. 미사용 `IsGroundComboActive`를 제거한다. |
| `Assets/Scripts/FSM/Player/@Hub/PlayerController.cs` | Awake에서 Action 기반 구독/해제 함수를 Handler에 주입하고, OnDestroy에서 `Deinitialize()`를 호출한다. 기존 `OnAttackRequestsCleared` 직접 구독은 제거한다. |
| `Assets/Scripts/FSM/Agent/StateControl/TransitionRules/ComboAttackTransition.cs` | `IAgentCombatInput` 생성자 의존성을 제거하고 Handler의 `TryBeginAttack()`만 사용한다. |
| `Assets/Scripts/FSM/Player/PlayerState/States/PlayerAttackState.cs` | `IAgentCombatInput` 필드/생성자 의존성과 매 프레임 polling 호출을 제거한다. AttackState 진입/종료에 맞춰 Handler 활성·정리 생명주기를 호출한다. |
| `Assets/Scripts/FSM/Agent/StateControl/TransitionRules/AttackEndTransition.cs` | `IAgentCombatInput` 생성자/필드를 제거하고 Handler의 다음 Combo 판단만 호출한다. |
| `Assets/Scripts/FSM/Player/PlayerState/PlayerStateFactory.cs` | Player Combo 관련 State/Transition 생성자에서 `data.CombatInput` 전달을 제거한다. |

`Assets/Scripts/FSM/Agent/Input/IAgentInput.cs`, `AgentStateFactory.cs`, `AttackTransition.cs`, AI Input 구현체는 변경하지 않는다. 이들은 Combo를 사용하지 않는 Agent의 polling 기반 공격 입력 계약을 유지한다.

## 적용 순서

- [ ] 1. PlayerInput을 이벤트 발행 전용으로 전환
  - `OnAttackRequested`와 Input Block 알림 이벤트를 추가한다.
  - Attack Input의 `performed` 시점에 `OnAttackRequested`를 발행한다.
  - 공격 요청 카운트와 `IAgentCombatInput` 구현을 제거한다.
  - Input Block 시 이동/점프 입력 정리 후 Input Block 이벤트를 발행한다.

- [ ] 2. ComboAttackHandler 입력 소유권 이전
  - `Initialize(Action<Action> subscribeAttack, Action<Action> unsubscribeAttack, ...)`와 `Deinitialize()`를 추가한다.
  - 입력 시작 대기열 `_pendingStartAttackCount`와 AttackState 상태 `_isAttackActive`를 추가한다.
  - `RegisterAttackRequest()`에서 AttackState 밖/허용 구간/비허용 구간을 구분해 저장 또는 폐기한다.
  - `TryBeginAttack()`은 Input 인자 없이 시작 대기열을 소비한다.
  - `TryAdvanceGroundCombo()`은 Input 인자 없이 예약된 `_queuedAttackCount`만 사용한다.
  - `Reset()`은 시작 대기열, Combo 예약, Attack 활성 상태를 모두 초기화한다.
  - 미사용 `IsGroundComboActive`를 삭제한다.

- [ ] 3. PlayerController 조립과 생명주기 갱신
  - `ComboAttackHandler.Initialize()`에 `PlayerInput`의 Attack/Input Block 이벤트 구독·해제 Action을 주입한다.
  - 기존 `OnAttackRequestsCleared += ResetGroundCombo` 연결을 제거한다.
  - `OnDestroy()`에서 Handler를 `Deinitialize()`해 이벤트 구독과 상태를 정리한다.

- [ ] 4. Player Combo FSM의 polling 의존성 제거
  - `ComboAttackTransition`에서 `IAgentCombatInput`을 제거한다.
  - `PlayerAttackState`에서 `_combatInput`과 매 프레임 `ProcessAttackRequests()` 호출을 제거한다.
  - `PlayerAttackState.OnEnter()`/`OnExit()`에서 Handler의 Attack 활성/종료 생명주기를 호출한다.
  - `AttackEndTransition`에서 `IAgentCombatInput`을 제거한다.
  - Factory Data와 생성자 호출을 맞춘다.

- [ ] 5. 컴파일·동작 검증
  - C# 재컴파일과 Console 오류를 확인한다.
  - 지상 1→2→3, 공중 3타, 스태미나 부족 시 AttackState 미진입을 확인한다.
  - 지상/공중 마지막 3타 중 연타가 다음 1타로 자동 재사용되지 않는지 확인한다.
  - 마지막 타격 종료 후 새 A 입력은 새 1타로 시작되는지 확인한다.
  - Dialogue/Input Block, 피격, 사망, Destroy 시 Handler의 이벤트 구독과 입력 상태가 남지 않는지 확인한다.

## 완료 기준

- PlayerInput에는 Combo 단계, 공격 요청 카운트, Combo 버퍼 관련 코드가 없다.
- ComboAttackHandler가 최초 공격 대기열과 Combo 예약 입력을 모두 소유한다.
- Handler는 Action 기반 의존성 주입으로 PlayerInput 이벤트를 구독하고, `Deinitialize()`에서 안전하게 해제한다.
- Player Combo State/Transition은 `IAgentCombatInput`을 참조하지 않는다.
- 마지막 3타·공중 3타 중 입력이 자동 1타 재시작으로 이어지지 않는다.
