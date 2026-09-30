# Player Combo Attack 공용 이벤트 입력 계약 전환 계획

## 목적

Player Combo 공격은 `OnAttackRequested` 이벤트를 통해 입력을 전달하고 있으나, `PlayerInput`이 `IAgentCombatInput`을 구현하지 않아 `AgentController.CombatInput`이 null이 되는 문제가 있다.

이번 작업은 Player의 이벤트 기반 공격 흐름을 유지하면서 `IAgentCombatInput`을 공용 공격 이벤트 계약으로 전환한다. `ComboAttackHandler`는 해당 인터페이스를 직접 주입받아 구독·해제하고, 공격 입력 버퍼와 Combo 정책의 단일 소유자가 된다.

AI의 세부 공격 로직은 추후 별도 리팩토링 대상으로 둔다. 단, 인터페이스 변경으로 인한 컴파일 호환성은 이번 작업 범위에서 유지한다.

## 현재 구조와 문제

```text
PlayerInput
  └─ OnAttackRequested 이벤트 발행

ComboAttackHandler
  ├─ Action<Action> 4개를 통해 이벤트 구독/해제 주입
  └─ Combo 입력 버퍼 관리

AgentController
  └─ GetComponent<IAgentCombatInput>()
      └─ PlayerInput이 구현하지 않아 Player에서는 null
```

문제점은 다음과 같다.

- `AgentController`가 공용 공격 입력을 보유하지만 Player만 `CombatInput`이 null이다.
- PlayerController가 공격/Input Block 이벤트의 등록·해제 Action을 네 개 전달해 조립 코드가 장황하다.
- Combo 입력 상태는 Handler가 관리하는데, 입력 계약은 Player 전용 이벤트와 AI 전용 polling으로 분리되어 있다.
- `OnInputBlocked`는 새 입력 차단 외의 의미로 사용되지 않는다. 이미 예약된 Combo를 취소하지 않는 현재 의도에는 불필요하다.

## 결정 사항

| 항목 | 결정 |
| --- | --- |
| 공용 공격 입력 계약 | `IAgentCombatInput`을 `OnAttackRequested` 이벤트 기반 계약으로 전환한다. |
| PlayerInput | `IAgentCombatInput`을 다시 구현하고, Attack Input의 `performed`에서 이벤트를 발행한다. |
| ComboAttackHandler 초기화 | `Initialize(IAgentCombatInput combatInput)` 한 개의 의존성만 받는다. |
| 구독 생명주기 | Handler가 `Initialize()`에서 직접 구독하고 `Deinitialize()`에서 직접 해제한다. |
| 입력 버퍼 소유 | 최초 공격 대기, 다음 Combo 예약, 초과 입력 폐기는 모두 Handler가 소유한다. |
| 최대 지상 Combo 단계 | `ComboAttackHandler.Initialize(IAgentCombatInput, int maxGroundComboStep)`에서 주입한다. Player는 `3`, 단발 공격 Agent는 `1`을 전달할 수 있다. |
| Input Block | `OnInputBlocked`와 관련 구독은 제거한다. 이후 입력만 차단하며, 이미 예약된 Combo는 정상 마무리한다. |
| AI 범위 | AI의 세부 동작 변경은 후속 작업으로 둔다. 이번에는 새 공용 계약을 만족하도록 컴파일 호환성을 유지한다. |

## 목표 구조

```text
PlayerInput : IAgentCombatInput
  └─ OnAttackRequested

AgentController
  └─ CombatInput = GetComponent<IAgentCombatInput>()
      └─ Player에서도 유효한 공용 입력 참조

PlayerController
  └─ ComboAttackHandler.Initialize(CombatInput)

ComboAttackHandler
  ├─ OnAttackRequested 직접 구독 / 해제
  ├─ 최초 공격 대기 입력
  ├─ 지상 Combo 예약 입력
  ├─ 공중·마지막 타격 입력 폐기
  └─ ComboAttackTransition / AttackEndTransition에 판단 결과 제공
```

## Combo 입력 정책

| 시점 | `ComboAttackHandler` 처리 |
| --- | --- |
| AttackState 밖 | 최초 공격 시작 대기 상태로 저장 |
| 지상 1타·2타 허용 구간 | 다음 타격 예약 상태로 저장 |
| 지상 1타·2타 중 중복 입력 | 다음 타격 예약이 이미 있으면 폐기 |
| 지상 3타 진행 중 | 즉시 폐기 |
| 공중 3타 진행 중 | 즉시 폐기 |
| AttackState Exit | 현재 Combo 상태와 예약 입력을 초기화 |
| Input Block | 새 입력만 차단한다. 이미 저장된 Combo 예약은 유지한다. |

각 타격은 다음 타격 한 번만 예약할 수 있다. 따라서 입력을 카운트로 누적하지 않고, Handler 내부의 bool 예약 상태로 관리한다.

## 상태 및 전이 흐름

```text
OnAttackRequested
  → ComboAttackHandler.RegisterAttackRequest()
  → AttackState 밖: 최초 공격 대기
  → AttackState 안: 다음 Combo 예약 또는 폐기

ComboAttackTransition
  → Handler의 최초 공격 대기 확인
  → PlayerController.TryStartAttack()
  → AttackState 진입

AttackEndTransition
  → Handler의 다음 Combo 예약 확인
  → PlayerController.TryContinueAttack()
  → 성공: Animator 내부 1 → 2 → 3 전이
  → 실패: GroundedState 또는 FallState 전이
```

이벤트는 Input System callback 시점에 즉시 Handler에 전달되므로, Animation End Event와 State Update 실행 순서 사이에 입력이 누락되는 polling 문제를 피할 수 있다.

## 수정 대상

| 파일 | 변경 |
| --- | --- |
| `Assets/Scripts/FSM/Agent/Input/IAgentInput.cs` | `IAgentCombatInput`을 공격 요청 이벤트 계약으로 전환한다. 필요 없는 polling API의 제거·이관 범위를 정리한다. |
| `Assets/Scripts/FSM/Player/Input/PlayerInput.cs` | `IAgentCombatInput`을 구현한다. `OnInputBlocked`를 제거하고 `OnAttackRequested`만 발행한다. |
| `Assets/Scripts/FSM/Player/PlayerState/ComboAttackHandler.cs` | 입력 인터페이스와 최대 지상 Combo 단계를 `Initialize()`에서 주입받아 직접 구독·해제한다. Action 4개 주입을 제거하고, bool 기반 최초/Combo 예약 상태를 관리한다. |
| `Assets/Scripts/FSM/Player/@Hub/PlayerController.cs` | `ComboAttackHandler.Initialize(CombatInput)`만 호출한다. 이벤트 등록 람다와 Input Block 관련 코드를 제거한다. |
| `Assets/Scripts/FSM/Agent/StateControl/TransitionRules/ComboAttackTransition.cs` | Handler가 보유한 최초 공격 대기 상태로만 전이 여부를 판단하도록 유지·정리한다. |
| `Assets/Scripts/FSM/Player/PlayerState/States/PlayerAttackState.cs` | AttackState 진입/종료에 맞춰 Handler의 Combo 생명주기를 알린다. |
| `Assets/Scripts/FSM/Agent/StateControl/TransitionRules/AttackEndTransition.cs` | 이벤트 기반 Handler의 예약 상태를 확인해 다음 Combo 또는 상태 이탈을 결정한다. |
| `Assets/Scripts/FSM/Agent/StateControl/TransitionRules/AttackTransition.cs` | `IAgentCombatInput` 계약 변경에 맞춰 후속 이벤트 전이 전환 대상으로 표시한다. |
| `Assets/Scripts/FSM/NPC/AIPlayer/AIPlayerInput.cs` | 새 인터페이스 계약을 만족하도록 최소 호환 변경만 적용한다. 세부 AI 공격 정책은 후속 작업으로 미룬다. |
| `Assets/Scripts/FSM/NPC/AIMonstor/Input/AIMonsterInput.cs` | 새 인터페이스 계약을 만족하도록 최소 호환 변경만 적용한다. 세부 AI 공격 정책은 후속 작업으로 미룬다. |

## 접근 범위 정리

| 요소 | 접근 범위 | 이유 |
| --- | --- | --- |
| `ComboAttackHandler.RegisterAttackRequest()` | `private` | `IAgentCombatInput.OnAttackRequested`에 Handler 내부에서만 등록한다. |
| `ComboAttackHandler.Initialize()` | `public` | PlayerController가 Handler의 의존성을 조립한다. |
| `ComboAttackHandler.Deinitialize()` | `public` | PlayerController의 Destroy 생명주기에서 구독을 해제한다. |
| `ComboAttackHandler.TryBeginAttack()` | `public` | ComboAttackTransition이 최초 공격 전이를 판단한다. |
| `ComboAttackHandler.TryAdvanceGroundCombo()` | `public` | AttackEndTransition이 다음 타격을 판단한다. |
| `ComboAttackHandler.Reset()` | `public` | PlayerAttackState Exit 및 공격 시작 실패 경로가 호출한다. |

`ComboAttackHandler` 클래스는 Factory, State, Transition 사이에서 전달되는 구성 요소이므로 현재 `public`을 유지한다. 외부에서 불필요하게 호출되는 이벤트 수신 함수만 `private`으로 축소한다.

## 적용 순서

- [x] 1. 공용 공격 입력 계약 전환
  - `IAgentCombatInput`에 `OnAttackRequested` 이벤트를 정의한다.
  - 기존 polling API의 유지 또는 제거 범위를 `AttackTransition` 및 AI 구현체 기준으로 확정한다.
  - PlayerInput이 `IAgentCombatInput`을 구현하도록 복구한다.

- [x] 2. PlayerInput 단순화
  - `OnInputBlocked`를 제거한다.
  - `SetInputBlocked(true)`는 이동·점프·향후 공격 이벤트만 차단하도록 유지한다.
  - Attack Input의 `performed`에서 `OnAttackRequested`를 발행한다.

- [x] 3. ComboAttackHandler 구독 및 버퍼 소유권 정리
  - `Initialize(IAgentCombatInput combatInput, int maxGroundComboStep)`과 `Deinitialize()`를 구현한다.
  - Handler 내부에서 `OnAttackRequested`를 직접 구독·해제한다.
  - 최초 공격 대기와 다음 Combo 예약을 bool 상태로 관리한다.
  - 마지막 지상 3타·공중 3타·중복 입력을 즉시 폐기한다.
  - `RegisterAttackRequest()`를 `private`으로 축소한다.

- [x] 4. Player FSM 조립 갱신
  - PlayerController에서 `ComboAttackHandler.Initialize(CombatInput, maxGroundComboStep)`만 호출한다.
  - 기존 Action 4개 주입과 Input Block 구독을 제거한다.
  - ComboAttackTransition, PlayerAttackState, AttackEndTransition의 Handler API 호출을 새 상태 모델에 맞춘다.

- [x] 5. 공용/AI 컴파일 호환 정리
  - `AttackTransition`과 AI Input 구현체가 변경된 `IAgentCombatInput` 계약을 만족하도록 최소 수정한다.
  - AI 공격의 실제 전이·버퍼 정책은 변경하지 않는다.

- [ ] 6. 검증
  - C# 재컴파일과 Console 오류를 확인한다.
  - 지상 1 → 2 → 3, 공중 3타, 스태미나 부족 시 State 미진입을 확인한다.
  - 지상·공중 마지막 타격 중 연타가 다음 1타로 자동 재사용되지 않는지 확인한다.
  - 마지막 타격 종료 뒤 새 입력은 새 1타로 시작되는지 확인한다.
  - Input Block 중 새 입력만 차단되고 기존 예약 Combo는 마무리되는지 확인한다.
  - Destroy 시 `OnAttackRequested` 구독이 해제되는지 확인한다.

## 완료 기준

- Player의 `AgentController.CombatInput`은 null이 아니다.
- Player와 이후 AI는 동일한 `IAgentCombatInput.OnAttackRequested` 계약을 사용한다.
- ComboAttackHandler가 공격 입력 이벤트를 직접 구독·해제하고, 최초 공격 및 Combo 예약의 유일한 소유자다.
- PlayerController는 Handler에 Action 4개를 전달하지 않고 `CombatInput` 하나만 조립한다.
- `OnInputBlocked`와 관련 구독이 없다.
- 입력 차단 이후 새 공격 입력은 발생하지 않지만, 이미 예약된 Combo는 정상적으로 종료된다.
