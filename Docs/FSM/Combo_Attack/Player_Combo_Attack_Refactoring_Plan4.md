# Combo Attack 종료 입력 정리 계획

## 목적

지상 Combo의 마지막 3타 또는 공중 3타가 재생되는 동안 입력한 Attack 요청이 다음 1타의 시작 입력으로 남는 문제를 해결한다.

목표는 **연계 불가능한 마지막 타격 중 입력은 폐기**하고, **AttackState를 완전히 종료한 뒤 새로 입력한 공격만 다음 1타로 허용**하는 것이다.

## 실제 코드 흐름

```text
PlayerInput.AttackInput()
  └─ _attackRequestCount++

PlayerAttackState.OnExecute()
  └─ ComboAttackHandler.CaptureBufferedRequests(combatInput)

Animation End Event
  └─ AttackEndTransition.ShouldTransition()
      └─ ComboAttackHandler.TryAdvanceGroundCombo(...)

AttackState 종료 후
  └─ Grounded / Jump / Fall의 ComboAttackTransition
      └─ ComboAttackHandler.TryBeginAttack()
          └─ TryConsumeAttackRequest()로 남은 요청을 새 1타 시작에 사용
```

## 원인

`ComboAttackHandler.CaptureBufferedRequests()`는 `_isAcceptingComboInput`이 `false`이면 즉시 반환한다.

현재 `_isAcceptingComboInput`이 `false`가 되는 경우는 다음과 같다.

| 상황 | 현재 Handler 상태 | 문제 |
| --- | --- | --- |
| 지상 Combo 3타 진입 후 | `_groundComboStep = 3`, `_isAcceptingComboInput = false` | 3타 중 입력이 `PlayerInput._attackRequestCount`에 남음 |
| 공중 AttackType 3 진입 후 | `TryBeginAttack()`이 `ResetGroundCombo()` 호출 | 입력 수집이 비활성이라 공중 공격 중 입력이 남음 |
| 마지막 타격 End Event 직전 | 더 이상 다음 타격이 없지만 입력 요청은 존재할 수 있음 | AttackState 종료 후 `ComboAttackTransition`이 남은 요청으로 새 1타를 시작 |

`ResetGroundCombo()`은 Handler 내부 값만 초기화한다. `PlayerInput._attackRequestCount`는 변경하지 않으므로, 종료 직전 요청이 자동 재시작의 원인이 된다.

## 결정 사항

1. `PlayerInput`과 `IAgentCombatInput`의 API는 변경하지 않는다.
   - 요청 큐의 소유자는 Input이다.
   - 어떤 입력을 유효하게 받을지는 Combo 상태를 아는 Handler가 판단한다.
2. `ComboAttackHandler`가 AttackState 활성 중 모든 공격 요청을 처리한다.
   - 1·2타의 입력 허용 구간: 다음 Combo 버퍼에 저장한다.
   - 3타·공중 3타의 입력 비허용 구간: 즉시 소비하고 폐기한다.
3. Animation End Event 처리 전에도 같은 처리 함수를 호출한다.
   - Update 이후 Event 직전에 들어온 요청이 남지 않도록 프레임 경계에서 한 번 더 폐기한다.

## 목표 동작

| 공격 구간 | Attack 입력 처리 | 결과 |
| --- | --- | --- |
| 지상 1타 | 최대 2개까지 버퍼링, 초과 요청은 폐기 | End Event에서 2타 또는 3타로 진행 |
| 지상 2타 | 최대 1개만 버퍼링, 초과 요청은 폐기 | End Event에서 3타로 진행 |
| 지상 3타 | 모든 요청 즉시 폐기 | 종료 후 자동 1타 재시작 없음 |
| 공중 3타 | 모든 요청 즉시 폐기 | 착지/종료 후 자동 1타 재시작 없음 |
| AttackState 종료 뒤 | 새 요청을 유지 | 다음 `ComboAttackTransition`이 정상적으로 새 1타 시작 |

## 수정 대상과 책임

| 파일 | 변경 | 책임 |
| --- | --- | --- |
| `Assets/Scripts/FSM/Player/PlayerState/ComboAttackHandler.cs` | `CaptureBufferedRequests()`를 `ProcessAttackRequests()`로 변경하고, 비허용 구간에서 `TryConsumeAttackRequest()`를 반복 호출하는 내부 폐기 로직 추가 | Combo 유효 구간 판단과 요청 폐기 |
| `Assets/Scripts/FSM/Player/PlayerState/States/PlayerAttackState.cs` | 실행 중 새 Handler 처리 함수를 호출하도록 변경 | AttackState 활성 프레임의 입력 처리 위임 |
| `Assets/Scripts/FSM/Agent/StateControl/TransitionRules/AttackEndTransition.cs` | 직접 변경하지 않는다. 기존의 `TryAdvanceGroundCombo()` 호출이 Handler 내부의 새 처리 함수를 거치도록 유지한다. | 종료 프레임 경계의 잔여 요청 방어 |

`PlayerInput.cs`, `IAgentCombatInput.cs`, `ComboAttackTransition.cs`, `PlayerController.cs`는 이번 수정 대상이 아니다.

## 적용 순서

- [x] 1. ComboAttackHandler의 요청 처리 API 명확화
  - `CaptureBufferedRequests()`를 `ProcessAttackRequests()`처럼 버퍼링과 폐기를 모두 표현하는 이름으로 변경한다.
  - `_isAcceptingComboInput == true`이면 기존처럼 남은 Combo 수만큼 `_queuedAttackCount`에 저장하고 초과 요청은 소비·폐기한다.
  - `_isAcceptingComboInput == false`이면 `TryConsumeAttackRequest()`를 반복 호출해 모든 요청을 소비·폐기한다.
  - 공중 3타와 지상 3타가 모두 이 비허용 경로를 타는지 확인한다.

- [x] 2. PlayerAttackState 호출부 갱신
  - `OnExecute()`에서 새 `ProcessAttackRequests()`를 호출한다.
  - `OnExit()`의 기존 Combo Handler 정리 흐름은 유지한다.

- [x] 3. AttackEndTransition 종료 경계 보강
  - `TryAdvanceGroundCombo()` 내부에서 새 요청 처리 함수를 먼저 호출한다.
  - 마지막 3타 End Event에서는 요청이 모두 폐기된 뒤 `false`를 반환해 Grounded/Fall 전이가 일어나도록 한다.
  - 1·2타 End Event에서는 이미 버퍼링된 요청만 사용해 다음 Animator 내부 전이를 실행하도록 유지한다.

- [x] 4. 코드 정리
  - 함수/필드 주석을 “입력 허용 구간에는 버퍼링, 비허용 구간에는 폐기” 기준으로 갱신한다.
  - `ComboAttackHandler` 외부에서 `PlayerInput`의 요청 카운트를 직접 조작하지 않는지 확인한다.

- [ ] 5. 검증
  - [x] C# 재컴파일 후 Console 오류를 확인한다. (2026-09-29: 컴파일 실패 없음, Console 오류 0건)
  - 지상 1타 중 입력: 2타로 진행되는지 확인한다.
  - 지상 2타 중 입력: 3타로 진행되는지 확인한다.
  - 지상 3타 중 연타: 종료 후 자동 1타가 시작되지 않는지 확인한다.
  - 공중 3타 중 연타: 착지/종료 후 자동 1타가 시작되지 않는지 확인한다.
  - 3타 종료가 완료된 뒤 새로 입력: 다음 1타가 정상 시작되는지 확인한다.
  - 입력 차단, 피격, 사망으로 AttackState가 중단될 때 기존 입력 정리 동작이 유지되는지 확인한다.

## 완료 기준

- 마지막 지상 3타와 공중 3타 중 입력한 Attack 요청이 다음 1타로 재사용되지 않는다.
- 1·2타의 유효 입력은 기존대로 다음 Combo로 진행한다.
- 마지막 타격이 끝난 뒤 새로 입력한 Attack 요청은 정상적으로 새 1타를 시작한다.
- 입력 폐기 정책은 `ComboAttackHandler`에만 존재하며, Input 계층은 요청 카운트의 생성·소비 API만 유지한다.
