# Player 단일 입력 콤보 공격 리팩토링 계획

## 목표

Player의 지상 공격 입력을 `Attack1(A)` / `Attack2(S)`의 직접 AttackType 지정 방식에서 단일 `Attack(A)` 입력 기반 콤보 방식으로 변경한다.

- 지상에서 A를 처음 누르면 `AttackType 1`을 실행한다.
- 지상 공격 중 A를 추가 입력하면 다음 종료 시 `1 → 2 → 3` 순서로 이어진다.
- 공중에서 A를 누르면 항상 `AttackType 3`을 한 번 실행하며 지상 콤보 진행도는 사용하지 않는다.
- Attack 모션 재생 중에는 Move·Jump를 포함한 다른 행동 State로 전이하지 않는다.
- 각 공격의 종료 Animation Event 이전에 입력된 A 요청만 다음 콤보 버퍼로 인정한다.
- 종료 Event 시 버퍼가 있으면 FSM을 이탈하지 않고 다음 Combo Animation으로 진행한다. 버퍼가 없으면 AttackState를 종료하며 이때 버퍼를 초기화한다.
- `Attack` Sub State Machine의 Entry에서는 지상 첫 타격(`Hero_combo1`)과 공중 공격(`Hero_combo3`)만 선택한다. `AttackType 2`의 직접 진입 전이는 제거하고, `Hero_combo2`는 1타 콤보 진행으로만 진입한다.

```text
Grounded + A
  → AttackType 1
  → (A 입력 버퍼) AttackType 2
  → (A 입력 버퍼) AttackType 3
  → Grounded

Jump / Fall + A
  → AttackType 3
  → Fall 또는 Grounded

Hero_combo1 / Hero_combo2 종료 Event 직전 A 입력
  → Combo 버퍼 기록
  → 종료 Event에서 다음 Combo Animation으로 진행

종료 Event 시 버퍼 없음 / Hero_combo3 종료
  → AttackState.Exit()
  → Combo 버퍼 초기화
  → GroundedState 또는 FallState
```

## 현재 구조와 변경 이유

현재 `PlayerInput`은 `Attack1`, `Attack2` 입력이 각각 `HeldAttackType = 1`, `HeldAttackType = 2`를 설정한다. `AttackTransition`과 `AttackState`는 이 값을 읽어 공격 종류를 직접 선택한다.

이 방식은 공격 종류 선택이 입력 키에 고정되어 있으며, 콤보 순서·입력 버퍼·마지막 타격 종료 판단을 표현할 위치가 없다. 또한 버튼을 누르고 있는 상태를 기준으로 공격을 반복할 가능성이 있어, 단발 입력 기반 콤보와 맞지 않는다.

## 결정 사항

| 항목 | 결정 |
| --- | --- |
| 입력 | `Attack1`/`Attack2`를 단일 `Attack` Action으로 통합하고 A 키만 바인딩한다. S 키의 Attack2 바인딩은 제거한다. |
| 입력 표현 | `HeldAttackType`을 제거하고, 누를 때마다 누적되는 소비형 공격 요청으로 전환한다. |
| 버퍼 크기 | 지상 콤보의 남은 타격 수만큼만 요청을 저장한다. 1타 중 최대 2회, 2타 중 최대 1회만 추가 입력을 보관한다. |
| 지상 콤보 | 첫 진입은 항상 1타이며, 입력 버퍼가 있으면 Attack Sub State Machine 내부에서 1타 → 2타 → 3타로 전이한다. |
| 공중 공격 | 공중에서 시작하면 항상 3타다. 공중 Attack 중 입력은 지상 콤보 버퍼에 저장하지 않는다. |
| Attack 모션 잠금 | AttackState 재생 중에는 Move·Jump Transition을 등록하지 않는다. 피격처럼 명시적으로 허용한 강제 전이와 Attack 종료 Event만 평가한다. |
| 콤보 입력 유효 구간 | 현재 Combo Animation의 종료 Animation Event 전까지다. Event 이후의 새 A 입력은 다음 Grounded/Fall 상태에서 새 공격 1회로 처리한다. |
| 콤보 진행 | Hero_combo1·2의 종료 Event 시 유효 버퍼가 있으면 FSM을 유지하고 다음 Combo Animation으로 내부 전이한다. |
| 버퍼 초기화 | Combo가 이어지지 않고 AttackState가 종료될 때 `PlayerAttackState.Exit()`에서 버퍼와 진행도를 일괄 초기화한다. |
| 종료 목적지 | 지상 Attack 종료는 `GroundedState`, 공중 Attack 종료는 `FallState`로 복귀한다. |
| Animator | Entry는 지상 첫 타격 `Hero_combo1`과 공중 공격 `Hero_combo3`만 선택한다. `AttackType 2` Entry 전이는 제거하며, 지상 콤보는 `Hero_combo1 → Hero_combo2 → Hero_combo3` 내부 전이로만 진행한다. |
| State 분리 | `AttackState`는 공통 공격 생명주기의 추상 기반으로 유지하고, Player 규칙은 `PlayerAttackState : AttackState`에 둔다. |
| FSM 식별자 | Attack 관련 Transition의 `typeof(AttackState)`는 유지한다. Player Factory는 `typeof(AttackState)` 키에 `PlayerAttackState` 인스턴스를 등록한다. |

## 목표 책임 구조

```text
PlayerInput
  └─ A 입력을 소비형 AttackRequest로 기록

PlayerComboAttackHandler (Player 전용)
  ├─ 현재 지상 콤보 단계(0~3) 관리
  ├─ 대기 중인 다음 공격 요청 수 관리
  ├─ 다음 AttackType 결정: 1 → 2 → 3
  └─ 콤보 완료·공중 공격·피격 같은 강제 중단·AttackState 종료 시 진행도 초기화

AttackTransition / AttackState
  ├─ 시작 가능한 AttackRequest를 소비
  ├─ 공통 AttackState는 공격 시작/정리와 Animator 전달을 담당
  ├─ PlayerAttackState가 PlayerComboAttackHandler로 첫 AttackType을 결정
  ├─ PlayerAttackState 유지 중 다음 입력을 Combo 버퍼에 반영
  └─ Animator에 AttackType → IsAttack → Combo 진행 파라미터를 전달

AttackEndTransition
  ├─ Animation End Event 수신
  ├─ Combo 버퍼가 있으면 다음 Combo Animation 진행을 요청하고 FSM을 유지
  └─ Combo 버퍼가 없으면 GroundedState/FallState로 복귀
```

`AgentCombatHandler`는 현재 공격의 판정 데이터와 `CurrentAttackType`만 유지한다. Player 전용 콤보 단계·버퍼는 Agent 공통 Handler에 넣지 않고 Player 전용 Handler로 분리한다.

```text
AgentStateBase
  └─ AttackState (abstract, 공통 공격 생명주기 / FSM 식별자)
      └─ PlayerAttackState (지상 콤보, 공중 3타, Player 이동 정책)

PlayerStateFactory
  └─ { typeof(AttackState), new PlayerAttackState(...) }
```

Dictionary의 Key는 전이 Rule이 찾는 논리적 상태 식별자다. 따라서 `AttackTransition.NextStateType = typeof(AttackState)`를 변경하지 않고, 해당 Key가 반환하는 실제 구현체만 `PlayerAttackState`로 교체한다. `PlayerAttackState`는 `AttackState`를 상속하므로 `AgentController`의 공격 Animation Event 식별도 기존 `is AttackState` 조건과 호환된다.

## 적용 순서

- [x] 1. 현재 공격 흐름과 Animator Event 기준 기록
  - `Hero_combo1`, `Hero_combo2`, `Hero_combo3`의 공격 판정/종료 Animation Event 위치를 확인한다.
  - 1타·2타의 종료 Event가 다음 콤보 입력을 판정하기에 적절한지 확인한다.
  - `AttackEndTransition`의 현재 고정 목적지(`GroundedState`)를 기록한다.

- [x] 2. Input Action을 단일 Attack으로 정리
  - `Assets/Scripts/FSM/Player/Input/PlayerInputCommands.inputactions`에서 `Attack1`을 `Attack`으로 변경한다.
  - A 키 바인딩을 유지하고 `Attack2` Action 및 S 키 바인딩을 제거한다.
  - Unity Input System에서 C# wrapper를 재생성한다. 생성 파일인 `PlayerInputCommands.cs`는 직접 수정하지 않는다.
  - `PlayerInput`의 등록 코드를 `gamePlay.Attack.performed` 하나로 교체한다.

- [x] 3. 소비형 공격 요청 API 설계 및 적용
  - `IAgentCombatInput`의 `HeldAttackType`을 `TryConsumeAttackRequest()` 및 필요한 요청 조회 API로 교체한다.
  - `PlayerInput`은 버튼을 누를 때 요청 수를 증가시키고, Input Block 시 요청을 초기화한다.
  - 요청 수 상한은 무한 연타 누적을 막고 현재 콤보의 남은 단계 수를 넘지 않도록 Player 콤보 Handler에서 제어한다.
  - 버튼 `canceled`에 의존하는 Attack 해제 로직은 제거한다.

- [x] 4. Player 전용 콤보 Handler 추가
  - `PlayerComboAttackHandler`(가칭)를 Player FSM 범위에 추가한다.
  - 지상 콤보 단계(0: 미진행, 1~3: 현재 타격)와 다음 타격 요청 버퍼를 관리한다.
  - API 예시는 `TryBeginGroundCombo()`, `TryQueueNextGroundAttack()`, `TryAdvanceGroundCombo()`, `ResetGroundCombo()`로 구성한다.
  - 공중 공격을 시작하거나 콤보 3타가 끝나거나 피격 같은 강제 중단이 발생하면 지상 콤보 진행도와 버퍼를 초기화한다.
  - 현재 공격의 종료 Event 이후에는 새 요청을 다음 콤보로 누적하지 않도록 입력 유효 구간을 닫는 API를 제공한다.

- [x] 5. 공통 AttackState와 PlayerAttackState로 책임 분리
  - 기존 `AttackState`를 `abstract` 공통 기반으로 정리한다.
  - 공통 기반에는 CombatHandler를 통한 공격 시작/초기화, Animator의 `AttackType → IsAttack` 전달, Exit 정리만 둔다.
  - `PlayerAttackState : AttackState`를 추가하고 지상 콤보 진행, 공중 3타 강제, Player 이동 정지 정책을 이동한다.
  - `AgentController`의 `is AttackState` Animation Event 식별은 상속 관계로 그대로 유지되는지 확인한다.

- [x] 6. 시작 전이와 PlayerAttackState를 콤보 선택 방식으로 변경
  - `AttackTransition`은 지상/공중 여부와 소비 가능한 AttackRequest를 기준으로 AttackState 진입을 판단한다.
  - `PlayerAttackState`는 직접 입력 타입을 읽지 않고 Combo Handler가 결정한 타입을 사용한다.
  - 지상 첫 공격은 1타로 시작하고, PlayerAttackState를 유지한 채 이후 입력을 Combo 버퍼에 기록한다.
  - 공중 시작은 기존 규칙처럼 `AttackType 3`을 선택한다.
  - 스태미나 검사와 소비는 실제 선택된 AttackType 기준으로 수행한다.
  - 공격 시작 실패 시 Combo Handler의 단계/버퍼가 남지 않도록 롤백한다.
  - `PlayerAttackState.Exit()`은 Animator, 현재 공격 타입, Combo Handler의 버퍼·진행도를 함께 초기화한다.
  - Combo가 계속되는 경우에는 PlayerAttackState를 Exit하지 않고 Animator 내부 전이만 수행한다.

- [x] 7. 종료 Animation Event 기반 콤보 판정 구성
  - Hero_combo1·2의 종료 Event 이전에 입력된 A 요청만 Combo Handler 버퍼에 기록한다.
  - 종료 Event 시 버퍼가 있으면 Combo Handler가 다음 AttackType을 확정하고, CombatHandler와 Animator parameter를 갱신한다.
  - Animator는 갱신된 Combo 진행 parameter 또는 AttackType 조건으로 `Hero_combo1 → Hero_combo2 → Hero_combo3` 내부 전이를 수행한다.
  - Combo 진행이 확정된 경우에는 AttackEndTransition이 FSM 종료 요청을 발행하지 않는다.
  - 버퍼가 없거나 Hero_combo3의 종료 Event가 발생한 경우에만 AttackEndTransition이 `GroundedState` 로 전이한다.
  - 종료 Event가 처리된 뒤의 새 공격 입력은 Combo Handler에 저장하지 않고, 다음 Grounded/Fall State에서 새 공격 시작 요청으로 처리한다.

- [x] 8. Attack Sub State Machine 내부 콤보 전이 및 종료 규칙 구성
  - 기존 Entry의 `AttackType 2 → Hero_combo2` 직접 전이를 제거한다.
  - Entry에는 지상 첫 공격용 `Hero_combo1`과 공중 공격용 `Hero_combo3` 진입 경로만 남긴다.
  - `Hero_combo1 → Hero_combo2`, `Hero_combo2 → Hero_combo3` Animator 전이를 추가한다.
  - 각 전이는 Combo 버퍼가 준비된 경우에만, 해당 클립의 콤보 허용 구간 또는 Exit Time에서 동작하도록 구성한다.
  - 1타·2타에서 버퍼가 없으면 해당 공격의 종료 Animation Event가 Attack 종료를 요청한다.
  - 다음 Combo 전이가 확정된 경우에는 이전 Combo의 종료 Event가 FSM 종료를 요청하지 않도록 Event 시점과 전이 조건을 조정한다.
  - 3타 종료, 버퍼 없음, 공중 공격 종료 시에만 `AttackEndTransition`이 FSM 공격 종료를 처리한다.
  - 종료 시 `GroundedState` 또는 `FallState`를 현재 Ground 판정으로 선택한다.
  - Event 구독/해제는 기존 `IEventTransitionRule` 규칙을 유지하며, 재진입마다 중복 구독되지 않게 검증한다.

- [x] 9. Factory와 Controller 의존성 연결
  - `PlayerController`가 PlayerComboAttackHandler를 보유·초기화한다.
  - `PlayerStateFactoryData`에 필요한 Player 전용 콤보 의존성만 추가한다.
  - `PlayerStateFactory`에서 `{ typeof(AttackState), new PlayerAttackState(...) }` 형태로 등록한다.
  - Attack 관련 State/Transition에 같은 콤보 Handler 인스턴스를 주입한다.
  - `AttackTransition`의 전이 대상 `typeof(AttackState)`는 유지한다.
  - `AttackEndTransition`은 Combo Handler를 통해 계속/종료를 판단하고, 지상/공중 종료 목적지를 결정할 수 있게 확장하되 Event 구독·해제 책임은 유지한다.
  - Monster Factory와 Agent 공통 State에는 Player 콤보 의존성을 추가하지 않는다.

- [x] 10. Animator 및 코드 검증
  - `Attack` Sub State Machine의 Entry가 지상 첫 공격은 `Hero_combo1`, 공중 공격은 `Hero_combo3`만 선택하며 `AttackType 2` 직접 진입 경로가 없는지 확인한다.
  - `Hero_combo1 → Hero_combo2 → Hero_combo3` 내부 전이가 종료 Event 전 입력된 Combo 버퍼 조건에서만 발생하는지 확인한다.
  - 첫 진입 때 `AttackType`을 먼저 설정하고 `IsAttack`을 설정하는 순서를 유지한다.
  - `PlayerAttackState.Exit()`의 AttackType 및 Combo 버퍼 초기화가 실제 콤보 종료 시에만 수행되는지 확인한다.
  - C# 컴파일 오류, 누락된 Input Action wrapper 참조, Console 오류를 확인한다.

- [ ] 11. 수동 테스트
  - 지상 A 1회: 1타만 실행 후 종료한다.
  - 지상 A 2회: 1타 → 2타가 실행된다.
  - 지상 A 3회 연타: 1타 → 2타 → 3타가 실행된다.
  - 지상 A 4회 이상: 3타 이후 새 입력 없이는 1타가 자동 재시작하지 않는다.
  - 콤보 입력이 없는 경우 각 타격 후 Grounded로 정상 복귀하고 Combo Handler가 초기화된다.
  - Jump/Fall 중 A: 항상 3타가 실행되고, 종료 후 공중이면 Fall로 복귀한다.
  - 공중 공격 중 A 연타: 지상 콤보 버퍼가 생성되지 않는다.
  - 지상 1타·2타·3타 중 Move/Jump 입력: Attack 모션은 중단되지 않고 끝까지 재생된다.
  - Hero_combo1·2 종료 Event 이전 A 입력: 다음 타격으로 이어지고 PlayerAttackState의 Enter/Exit가 다시 호출되지 않는다.
  - 종료 Event 이후 Move·Jump 입력: 이미 Combo Handler가 초기화된 상태에서 일반 Grounded/Fall 입력으로 처리된다.
  - 종료 Event와 A 입력이 같은 프레임에 발생할 때 입력 유효 구간의 경계가 일관되게 처리되는지 확인한다.
  - 스태미나 부족, 피격, 사망, Input Block 시 버퍼·AttackType·Event 구독이 남지 않는다.

## 완료 기준

- A 키 하나만으로 지상 1 → 2 → 3 콤보가 순서대로 실행된다.
- AttackType은 입력 키가 아니라 Player 콤보 진행도와 공중 여부로 결정된다.
- 공중 공격은 항상 3타이고 지상 콤보와 섞이지 않는다.
- Attack 모션 중에는 Move/Jump로 모션이 끊기지 않으며, Combo가 이어지지 않고 PlayerAttackState가 종료될 때 이전 콤보 진행도와 대기 입력이 제거된다.
- Animator Entry는 지상 첫 타격 또는 공중 3타만 선택하고, Sub State Machine 내부 전이로 지상 1 → 2 → 3 콤보를 재생한다. `Hero_combo2`는 Entry에서 직접 진입하지 않는다.
- FSM State, Animator State, `CurrentAttackType`, 입력 버퍼가 공격 종료·중단 후 일관되게 초기화된다.

## 제외 범위

- 차지 공격, 방향별 공격
- Combo 클립 간 Blend Tree
- Monster 전용 콤보 규칙 구현
- 공격 데이터·피해량·히트박스 규칙 재설계
