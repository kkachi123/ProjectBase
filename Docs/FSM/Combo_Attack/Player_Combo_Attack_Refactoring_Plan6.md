# Player Combo Attack 단순화 계획

## 1. 목적

현재 Player Combo는 `ComboAttackTransition`, `ComboAttackEndTransition`이 각각 최초 공격 준비와 다음 타격 실행까지 맡고 있다. 이 때문에 TransitionRule이 **상태 전이 판단**을 넘어 Combo 정책, Stamina 적용, Animator 파라미터 변경을 함께 책임져 구조가 복잡해졌다.

이번 작업의 목표는 다음과 같다.

- 공격 State의 출입은 공용 `AttackTransition`, `AttackEndTransition`으로 통일한다.
- `ComboAttackHandler`는 Player 공격 State가 활성화된 동안의 **입력 예약 여부**만 관리한다.
- 다음 Combo 실행은 `PlayerAttackState.TryHandleAttackFinished()`에서만 수행한다.
- `ComboAttackEndTransition`과 `ComboAttackTransition`을 제거한다.
- 일반 Agent의 단발 공격 경로는 Combo 구현과 분리한 채 유지한다.

이번 범위는 Combo 정책과 FSM 연결의 단순화다. 공격 데이터, Stamina 수치, Animator 클립 자체의 교체는 포함하지 않는다.

## 2. 현재 구조와 문제

```text
공격 입력
  └─ ComboAttackHandler.RegisterAttackRequest()
       ├─ AttackState 밖: 최초 공격 대기
       └─ AttackState 안: 다음 Combo 예약

ComboAttackTransition
  └─ TryBeginAttack()
       └─ PlayerController.TryStartAttack(type)
            └─ AttackState 진입

ComboAttackEndTransition (Animation End)
  └─ TryAdvanceGroundCombo()
       ├─ PlayerController.TryContinueAttack(type)
       ├─ Animator.SetAttackType(type)
       └─ 실패 시에만 Grounded/Fall 전이
```

문제는 다음과 같다.

- `ComboAttackTransition`이 입력 소비, 지상/공중 AttackType 선택, 실제 공격 가능 여부 확인까지 담당한다.
- `ComboAttackEndTransition`은 전이 규칙인데, 다음 공격 실행과 Animator 갱신까지 수행한다.
- 콤보의 다음 타격은 FSM State가 아닌 TransitionRule에서 실행되므로, `PlayerAttackState`의 책임 경계가 불명확하다.
- Handler가 최초 공격 대기, State 활성 상태, 현재 단계, 다음 타격 예약을 모두 보유해 필요 이상으로 상태가 많다.

## 3. 목표 구조

```text
Grounded / Jump / Fall
  └─ AttackTransition
       ├─ 공격 입력 Event 수신
       ├─ IAttackStarter.TryStartAttack() 성공 확인
       └─ AttackState 진입

PlayerAttackState
  ├─ OnEnter : ComboAttackHandler.BeginAttack()
  ├─ TryHandleAttackFinished (Animation End)
  │    └─ 예약 입력이 있으면 다음 Combo 타입을 한 번만 적용하고 AttackState 유지
  └─ OnExit : ComboAttackHandler.EndAttack() / 입력 예약 초기화

AttackState
  └─ AttackEndTransition (Animation End)
       └─ GroundedState 또는 FallState 전이
```

`AttackTransition`은 Combo를 알지 않는다. `AttackEndTransition` 역시 종료 목적지만 담당한다. Combo는 `PlayerAttackState` 내부의 Player 전용 행동이며, End Event에서 다음 Combo를 시작하지 못할 때만 공용 `AttackEndTransition`이 State를 종료한다.

## 4. 책임 분리 결정

| 구성 요소 | 변경 후 책임 | 책임에서 제외할 내용 |
| --- | --- | --- |
| `AttackTransition` | 공격 요청 Event 수신, 최초 공격 시작 가능 여부 확인, `AttackState` 진입 | Combo 단계·예약 입력·Animator 다음 타격 전이 |
| `AttackEndTransition` | Animation End Event가 전달된 뒤 Grounded/Fall 전이 | 다음 Combo 실행 여부 판단. PlayerAttackState가 Combo를 이어가면 종료 Event 자체가 발행되지 않음 |
| `ComboAttackHandler` | AttackState 활성 중 다음 공격 입력 1회 예약·소비·초기화 | State 전이, Stamina 사용, Animator 변경, Controller 호출 |
| `PlayerAttackState` | 초기/연속 공격의 Player 정책 실행, Handler 생명주기, Animation End에서 다음 타격 적용 | Input System Event 직접 구독, State 전이 직접 호출 |
| `PlayerController` | 최초 공격과 다음 공격의 실제 가능 여부·Stamina·`CurrentAttackType` 적용 | Combo 입력 버퍼 상태 보유 |

## 5. 필요한 계약 정리

### 5.1 최초 공격: 공용 AttackTransition 경로

현재 `AttackTransition`은 입력 Event를 받아 단순히 `true`를 반환한다. Player가 기존 `TryStartAttack(int)` 방식으로는 AttackType을 설정해야 하므로, 다음과 같이 공용 진입 계약을 정리한다.

- `IAttackStarter`를 `bool TryStartAttack()` 형태의 **최초 공격 시작 계약**으로 단순화한다.
- `AttackTransition`은 `IAgentCombatInput`, `IAttackStarter`를 주입받는다.
- Event를 소비한 뒤 `TryStartAttack()`이 성공할 때만 `AttackState`로 전이한다.
- Player의 `TryStartAttack()`은 지상이면 1타, 공중이면 현재 정책대로 공중 AttackType을 적용한다.
- Monster 등 단발 공격 Agent는 자신의 기본 AttackType을 같은 계약 안에서 적용한다.

이렇게 하면 최초 공격의 Stamina 검증과 AttackType 적용은 Transition의 공통 흐름에 포함되지만, Combo 전용 분기는 포함되지 않는다.

### 5.2 연속 공격: PlayerAttackState.TryHandleAttackFinished 경로

입력은 현재 공격 Animation이 끝나기 전에 `ComboAttackHandler`에 bool로 예약한다. 실제 다음 타격 적용은 `AgentController`가 End Event를 수신했을 때 `PlayerAttackState.TryHandleAttackFinished()`에서만 수행한다.

1. `ComboAttackHandler`에 예약된 입력이 있는지 확인한다.
2. 현재 AttackType이 지상 Combo의 마지막 단계가 아닌지 확인한다.
3. `IAttackComboStarter.TryContinueAttack(nextAttackType)`으로 Stamina·타입 적용을 요청한다.
4. 성공 시 `Attack()`을 호출해 현재 `CombatHandler.CurrentAttackType`을 Animator와 Stamina 처리로 적용하고 `true`를 반환한다.
5. `AgentController`는 `true`이면 `OnAnimationEnded`를 발행하지 않아 AttackState를 유지한다. `false`일 때만 공용 `AttackEndTransition`이 Grounded/Fall 전이를 수행한다.
6. 예약 플래그는 소비 시 즉시 초기화한다.

실패한 경우에도 해당 입력은 소비한다. Stamina 부족이나 마지막 단계에서 같은 예약 입력이 다음 공격의 1타로 남아 자동 재사용되는 것을 방지한다.

## 6. ComboAttackHandler 축소 설계

Handler는 입력 Event를 직접 구독하되, 다음 세 상태만 갖는다.

```csharp
private bool _isAttackActive;
private bool _hasQueuedComboInput;
private int _maxGroundComboStep;
```

| API | 역할 |
| --- | --- |
| `Initialize(IAgentCombatInput, int)` | 공격 입력 Event 구독, 최대 지상 Combo 단계 설정 |
| `BeginAttack()` | AttackState 진입 시 활성화하고 최초 입력 잔여값을 제거 |
| `TryConsumeNextAttack(int currentAttackType, out int nextAttackType)` | 활성 중 예약된 입력을 한 번 소비하고, 다음 지상 단계가 유효할 때만 타입 반환 |
| `EndAttack()` | AttackState 종료 시 비활성화 및 예약 입력 제거 |
| `Deinitialize()` | Event 구독 해제 및 상태 초기화 |

삭제 대상은 다음과 같다.

- `TryBeginAttack()` — 최초 공격 진입은 `AttackTransition`이 담당한다.
- `ActivateAttack()` — `BeginAttack()`으로 의미를 명확히 한다.
- `TryAdvanceGroundCombo()` — 다음 공격 실행은 `PlayerAttackState.TryHandleAttackFinished()`가 담당한다.
- `_hasPendingStartAttack`, `_groundComboStep`, `_isAcceptingComboInput` — Handler가 전이·진행 단계를 소유하지 않는다.

`RegisterAttackRequest()`는 계속 `private`이며, `_isAttackActive == true`일 때만 `_hasQueuedComboInput = true`로 기록한다. AttackState 밖의 최초 입력은 `AttackTransition`만 처리하므로 Handler가 보관하지 않는다.

## 7. Animator 시점 문제 해결: Animation End 단일 소비

`OnExecute()`에서 예약 입력을 즉시 소비하면 Animator가 Combo2 클립으로 실제 전이하기 전에도 `AttackType`이 2, 다시 3으로 앞서 변경될 수 있어 1 → 3타 건너뜀이 발생한다.

이를 별도 입력 수락 Event나 Animator State Hash 확인 없이 해결하기 위해, 다음 공격 타입 적용 시점을 **현재 공격의 End Animation Event**로 고정한다.

```text
공격 중 입력 → ComboAttackHandler에 bool 예약
Animation End → PlayerAttackState.TryHandleAttackFinished()
  ├─ 예약 입력 + 다음 공격 가능 → Attack() / true / AttackState 유지
  └─ 예약 없음 또는 실패 → false / OnAnimationEnded / AttackEndTransition
```

이 방식에서는 다음 Combo의 `AttackType`이 이전 Clip의 종료 시점에만 변경된다. 따라서 한 Animation 재생 중에는 최대 하나의 예약 입력만 유지되며, 1 → 3타를 앞서 지정하는 경로가 없다.

## 8. 파일별 수정 계획

| 파일 | 변경 |
| --- | --- |
| `Assets/Scripts/FSM/Player/PlayerState/ComboAttackHandler.cs` | 최초 공격/Animator/Controller 책임을 삭제하고, AttackState 내부 입력 예약 Handler로 축소한다. |
| `Assets/Scripts/FSM/Player/PlayerState/States/PlayerAttackState.cs` | `OnEnter`에서 Handler를 시작하고, `TryHandleAttackFinished()`에서 예약 입력을 소비해 다음 공격을 실행한다. `OnExit`에서 입력을 초기화한다. |
| `Assets/Scripts/FSM/Agent/StateControl/TransitionRules/Attack/AttackTransition.cs` | 공용 최초 공격 시작 계약을 사용해, 실제 시작 가능할 때만 `AttackState` 진입하도록 갱신한다. |
| `Assets/Scripts/FSM/Agent/StateControl/TransitionRules/Attack/AttackEndTransition.cs` | 코드 구조는 유지한다. Combo 의존성 없이 End Event 시 Grounded/Fall로 전이한다. |
| `Assets/Scripts/FSM/Agent/StateControl/TransitionRules/Attack/ComboAttack/ComboAttackTransition.cs` | 사용처 제거 후 삭제한다. |
| `Assets/Scripts/FSM/Agent/StateControl/TransitionRules/Attack/ComboAttack/ComboAttackEndTransition.cs` | 사용처 제거 후 삭제한다. |
| `Assets/Scripts/FSM/Player/PlayerState/PlayerStateFactory.cs` | Grounded/Jump/Fall에 `AttackTransition`을 등록하고, AttackState에는 `AttackEndTransition`을 등록한다. Combo 전용 전이 주입을 제거한다. |
| `Assets/Scripts/FSM/Agent/@Hub/IAgentController.cs` | 최초 공격과 연속 공격의 계약을 정리한다. `TryContinueAttack(int)`은 PlayerAttackState가 사용한다. |
| `Assets/Scripts/FSM/Player/@Hub/PlayerController.cs` | 단순화된 `TryStartAttack()`과 `TryContinueAttack(int)`을 제공한다. Handler 생성·초기화·해제는 유지한다. |
| `Assets/Scripts/FSM/Agent/@Hub/AgentController.cs` | `OnFrame`에서는 피격 판정만 수행하고, `End`에서 `AttackState.TryHandleAttackFinished()`의 반환값에 따라 공용 종료 Event를 발행한다. |
| `Assets/Prefabs/Player/Animations/Hero_Anim.controller` 및 Combo Clip | 각 Combo Clip End Event가 다음 Combo 처리 또는 공용 종료 처리로 정확히 연결되는지 검증한다. |

## 9. 적용 순서

- [x] 1. Animation End 처리 정책 확정
  - 다음 Combo 실행은 `OnExecute`가 아닌 현재 Clip의 End Event에서 한 번만 수행한다.
  - 별도 Combo 입력 수락 Event와 Animator State Hash 기반 잠금은 적용하지 않는다.

- [x] 2. 공용 공격 진입 경로 복구
  - `IAttackStarter`와 `AttackTransition`을 최초 공격 시작 계약으로 정리한다.
  - Player의 지상 1타·공중 공격 시작 정책을 `PlayerController`에 둔다.
  - `PlayerStateFactory`의 Grounded/Jump/Fall에 동일한 `AttackTransition`을 등록한다.

- [x] 3. ComboAttackHandler 축소
  - Handler에서 최초 공격 대기, 현재 단계, Animator·Starter 의존성을 제거한다.
  - AttackState 활성 중 입력 한 번을 예약·소비하는 API만 남긴다.
  - `Initialize`/`Deinitialize`의 Event 구독 생명주기를 유지한다.

- [x] 4. PlayerAttackState로 Combo 실행 이동
  - `OnEnter`에서 Handler 활성화 및 최초 입력 잔여값 초기화.
  - `TryHandleAttackFinished()`에서 예약 입력을 소비하고 다음 공격을 실행.
  - Combo 지속 시 `true`를 반환해 공용 종료 Event 발행을 막는다.
  - `OnExit`에서 모든 Combo 예약을 초기화.

- [x] 5. Combo 전용 Transition 제거
  - `ComboAttackTransition`, `ComboAttackEndTransition`의 Factory 등록을 제거한다.
  - 모든 사용처가 사라진 뒤 두 파일과 `.meta`를 삭제한다.
  - AttackState에는 공용 `AttackEndTransition`만 등록한다.

- [~] 6. 검증
  - [x] Unity C# 재컴파일 완료: 오류 없음.
  - [ ] 지상: 1타만 입력하면 End 후 Grounded로 복귀한다.
  - [ ] 지상: 각 타격 중 입력 1회로 1 → 2 → 3이 정상 전이한다.
  - [ ] 빠른 연타로 1 → 3을 건너뛰지 않는다. (End Event 단일 소비 방식 검증)
  - [ ] 마지막 타격 중 입력은 폐기되며, 종료 뒤 자동으로 새 1타가 시작되지 않는다.
  - [ ] 공중 공격은 기존 정책대로 단발이며 Combo 예약을 만들지 않는다.
  - [ ] Stamina 부족 시 AttackState 진입 또는 다음 타격 적용이 일관되게 실패한다.
  - [ ] Hit/Death/Attack 종료로 이탈하면 예약 입력이 남지 않는다.

## 10. 완료 기준

- Combo 전용 TransitionRule이 없다.
- Attack State의 진입·종료는 `AttackTransition`과 `AttackEndTransition`으로만 관리한다.
- `ComboAttackHandler`는 AttackState 밖의 최초 공격 입력이나 State 전이를 관리하지 않는다.
- 다음 Combo 실행 코드는 `PlayerAttackState.TryHandleAttackFinished()` 한 곳에만 있다.
- Animator 전이 중 빠른 연타가 Combo 단계를 건너뛰지 않는다.
