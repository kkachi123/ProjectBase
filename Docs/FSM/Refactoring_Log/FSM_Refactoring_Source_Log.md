# FSM Refactoring Source Log

> PDF 갱신을 위한 단일 원본 문서다. 코드 구조가 바뀌면 먼저 이 문서의 **변경 이력**, **현재 구조**, **검증 상태**를 갱신한다.
>
> 대상 범위: Player 직접 입력 기반 FSM과 기본 네 상태 Monster 실행 구조. Goblin 기준 Controller·Input·Animator·Factory·prefab 제작을 완료했다. Behavior 의사결정과 다중 공격 타입 입력은 아직 구현하지 않았다.

## 1. 리팩토링 이전 구조

초기 FSM은 **클래스별 책임을 구분하고, 새 Agent를 추가해도 유지보수 가능한 구조로 확장한다**는 목표로 설계했다. State 전환마다 새 객체를 만드는 방식은 GC와 전환 시점 비용이 늘어날 수 있다고 판단해 채택하지 않았다. 대신 필요한 State를 시작 시점에 생성해 Dictionary에 보관하고, 전환 시 재사용하도록 구성했다.

초기 구현에서는 `AgentController`가 State 생성·보관·전환과 함께 이동, 입력, 전투, Animator 접근에 필요한 기능을 폭넓게 제공했다. 이후 이동·전투 등 기능을 Handler 클래스로 분리했지만, State가 필요한 기능에 도달하는 경로는 여전히 Controller 또는 많은 interface 묶음에 의존하는 형태였다. 즉, 기능 구현은 분리되었어도 State의 실제 의존성이 명확하게 드러나지 않았다.

State 기반도 행동 범주에 따라 계층화되어 있었다. 일반 상태는 `AgentStateBase<T>`를, 지상 상태는 `GroundedAgentStateBase`를 기반으로 구현했다. 상태 전환은 각 State가 `_agent.ChangeState(...)`를 직접 호출했고, `AgentStateMachine<IAgentState>`가 현재 State와 전환을 관리했다.

이 구조는 다음 문제를 만들었고, 이후 리팩토링의 첫 출발점이 되었다.

1. State별 필요 기능과 조정 책임이 `AgentController`에 계속 누적되어 God Object 성격이 커졌다.
2. State 생성자만으로 필요한 Handler·Input·Animator 기능을 파악하기 어려웠다.
3. Handler를 주입하기 위해 interface를 계속 추가하면, State의 책임을 설명하기 위한 interface 수와 조합이 불필요하게 늘어났다.

```text
State -> AgentController -> Input / Movement / Combat / Animator
```

| 구성 요소 | 초기 역할 | 문제점 |
| --- | --- | --- |
| 상태 기반 | `AgentStateBase<T>` + `GroundedAgentStateBase`로 행동 범주별 기반 클래스를 분리 | generic 타입과 지상 전용 기반이 State 구현에 전파되어 구조 이해와 확장이 복잡해짐 |
| 상태 전환 | State 내부에서 `_agent.ChangeState(...)`를 직접 호출 | 전이 조건과 전환 실행이 State에 함께 있어 규칙 재사용·조합이 어려움 |
| 상태 머신 | `AgentStateMachine<IAgentState>`가 현재 State와 전환을 관리 | Controller, StateMachine, State 사이 전환 책임의 경계가 분산됨 |
| State Dictionary | 필요한 State를 미리 생성하고 재사용 | GC 부담을 줄이는 대신 State 구성 책임을 명확히 관리해야 함 |
| `AgentController` | State 관리와 기능 접근의 중심 | State별 필요 기능이 Controller에 누적되어 God Object 성격이 커짐 |
| Handler | 이동·전투 등 세부 기능 분리 | 구현은 분리됐지만 State의 접근 경로가 여전히 Controller/interface 묶음에 의존 |
| 각 State | Controller 또는 주입된 interface를 통해 기능 호출 | 생성자만으로 실제 의존성을 파악하기 어려움 |
| `StateType` | Player와 Monster 상태를 하나의 enum으로 식별 | 캐릭터별 상태가 같은 enum에 계속 누적됨 |
| `IdleState` / `MoveState` | 입력 유무에 따라 분리된 지상 행동 | 연속적인 이동 표현까지 FSM 전환으로 처리 |
| Animator | `IsIdle`, `IsMove` Bool 중심 전이 | FSM 상태와 Animator 표현이 강하게 결합됨 |
| PlayerInput | 다수 입력값을 `ReactiveProperty`로 보관 | 변화 구독이 없는 입력까지 반응형 값으로 관리 |

## 2. 첫 구조 전환: State 기반·전환 책임 단순화와 Factory 주입

첫 번째 리팩토링은 State 계층, 전환 방식, 상태 머신 책임을 동시에 단순화하는 작업이었다.

| 구분 | 리팩토링 전 | 1차 변경 | 적용 상태 |
| --- | --- | --- | --- |
| 상태 기반 | `AgentStateBase<T>` + `GroundedAgentStateBase` | 비제네릭 `AgentStateBase` 하나로 통합 | 적용 |
| 상태 전환 | State 내부에서 `_agent.ChangeState` 직접 호출 | `ITransitionRule`을 순차 평가하고 `OnTransition` 발행 | 부분 적용 |
| 상태 머신 | `AgentStateMachine<IAgentState>` | `AgentController`가 `_currentState`를 직접 보유 | 적용 |

`AgentStateBase` 통합으로 State의 공통 생명주기와 전이 규칙 등록 방식을 한 곳에 모았다. `AgentStateMachine<IAgentState>`를 제거한 뒤에는 `AgentController`가 State Dictionary와 `_currentState`를 직접 관리하여, 현재 State의 소유자와 전환 실행 지점을 명확하게 했다.

전환은 State가 Controller를 직접 호출하는 방식에서 `ITransitionRule`을 평가하고 `OnTransition`을 발행하는 방식으로 옮겼다. Factory는 각 State에 필요한 Rule을 조합하고, Controller가 State의 전환 이벤트를 `ChangeState`에 연결한다. 위 표의 **부분 적용**은 1차 변경 당시의 상태다. 이후 Player에 이어 기본 Monster 네 상태도 이 방식으로 이행했으며, 모든 NPC·AI 계열까지 일괄 이행했다고 해석하지 않는다.

Handler를 분리한 뒤에는 필요한 Handler를 interface로 묶어 주입하는 방식도 사용했다. 그러나 기능이 늘수록 interface가 과도하게 생기고, State별로 실제 필요한 Handler 조합을 명확히 구분하기 어려웠다.

이를 해결하기 위해 Controller는 Factory에 생성 데이터를 전달하고, Factory가 각 State에 필요한 Handler, Input, Animator capability만 생성자로 주입하도록 변경했다.

```text
Before
State -> _agent.ChangeState / AgentStateMachine
State -> AgentController 또는 넓은 interface 묶음 -> 기능 접근

After
ITransitionRule -> State.OnTransition -> AgentController 전환
Controller -> StateFactory -> State(필요한 의존성만 주입)
```

이 전환은 이후 `ITransitionRule`, capability interface, `PlayerAnimator` 분리의 공통 기반이다. State는 구체 Controller 전체나 범용 interface 묶음이 아니라 자신의 책임에 필요한 최소 의존성만 가져야 한다.

## 3. 현재 구조 요약

### State와 Factory

- `AgentController`는 State Dictionary를 `Type` 키로 보관하고 현재 State 전환을 관리한다.
- `AgentController`는 더 이상 구체 Animator 참조·초기화를 소유하지 않는다. PlayerController와 MonsterController가 각 전용 Animator를 직접 초기화한다.
- `AgentStateFactory<TData>`가 공통 State 생성 → Agent별 State 추가·교체 → 전이 연결 순서를 관리한다. PlayerStateFactory와 MonsterStateFactory가 이를 상속한다.
- `PlayerStateFactory`는 `GroundedState`, `JumpState`, `FallState`, `DashState`, `AttackState`, `HitState`, `DeathState`를 구성한다.
- `MonsterStateFactory`는 `GroundedState`, `AttackState`, `HitState`, `DeathState` 네 Type key를 구성한다. Attack·Hit의 값은 각각 MonsterAttackState·MonsterHitState로 확장하되 공용 key를 유지한다.
- State는 필요한 Handler, Input interface, Animator capability만 생성자로 받는다.
- 전이 조건은 `ITransitionRule` 구현체를 Factory에서 조합한다.
- State Exit에서 활성 Event Rule을 해제하고, Controller 파괴 시 현재 State Exit와 모든 `OnTransition` 연결을 정리한다.

### 지상 행동과 Animator

- 기존 `IdleState`, `MoveState`, `IdleToMoveTransition`은 제거했다.
- `GroundedState`가 지상 정지와 이동을 함께 처리한다.
- `Hero_Anim.controller`의 `Grounded` 1D Blend Tree가 `MoveSpeed`로 Idle/Move 표현을 선택한다.
- 점프, 낙하, 공격, 피격, 사망처럼 제어 규칙이 다른 행동만 별도 FSM State로 유지한다.

### 공격 Sub State Machine 진입

- 외부 상태(`Grounded`, `Hero_jump`, `Hero_fall_start`, `Hero_fall_loop`)는 개별 Combo State가 아닌 `Attack` Sub State Machine으로만 진입한다.
- `Attack` Sub State Machine의 `Entry`는 최초 지상 공격인 `Hero_combo1`과 공중 공격인 `Hero_combo3`만 선택한다. Combo 2·3은 Animator 내부 전이로만 이어진다.
- 공용 `AttackTransition`이 최초 공격 입력을 받고 `TryStartAttack()` 성공 시에만 AttackState에 진입한다. Player는 이 시점에 지상 1타 또는 공중 3타를 `CombatHandler`에 준비한다.
- `AttackState.OnEnter()`는 준비된 `CurrentAttackType`으로 Animator 공격 진입을 적용한다.
- `AttackType`은 최초 공격 진입점만 결정한다. 지상 Combo는 `1`, 공중 단일 공격은 `3`을 사용한다.
- 지상 Combo의 1→2→3 순서는 Animator 내부의 `ComboTrigger` 전이로만 진행하며, AttackState는 Combo 중 재진입하지 않는다.

### Combo Attack 책임 분리

- `IAgentCombatInput`은 `OnAttackRequested` 이벤트를 제공하는 공용 공격 입력 계약이다. PlayerInput도 이 계약을 구현하므로 `AgentController.CombatInput`은 Player에서 null이 아니다.
- 기본 `AttackTransition`은 최초 공격 요청을 구독하고 `TryStartAttack()` 성공 시에만 AttackState로 진입하는 공용 이벤트 전이다.
- `ComboAttackTransition`, `ComboAttackEndTransition`은 제거했다. TransitionRule은 다시 State 진입·종료 판단만 담당한다.
- `AttackState`는 준비된 공격 타입을 Animator에 적용하고 공통 정리를 담당한다.
- `PlayerAttackState`는 진입 시 지상 공격의 수평 이동을 멈추고, AttackState 동안의 공격 입력을 `IComboAnimation.SetComboTrigger()`로 전달한다.
- Animator는 각 Combo 전이의 `Has Exit Time`, Exit Time, Duration으로 입력 수락 시점과 연결 타이밍을 제어한다.
- `AttackEndTransition`은 마지막 Combo Clip의 Animation End Event를 받아 Grounded/Fall로 복귀한다.

### 공중 Sub State Machine 진입

- `Grounded`는 `Hero_jump`, `Hero_fall_start` 클립에 직접 전이하지 않고 `Hero_Jump` Sub State Machine으로만 진입한다.
- `Hero_Jump`의 `Entry`가 `IsJump` 또는 `IsFall` 조건으로 각각 `Hero_jump`, `Hero_fall_start`를 선택한다.
- `JumpState`와 `FallState`는 진입 시 반대 공중 Bool을 먼저 해제해 두 Entry 조건이 동시에 성립하지 않도록 한다.

### Animator 공통 전이 구조

- 기존에는 `Hero_jump`, `Hero_fall_start`, `Hero_fall_loop` 등 각 Animation State에 Grounded·Attack·Dash 전이를 개별로 등록했다. 새 외부 행동을 추가할 때마다 공중 Animation State 전체에 전이를 반복해야 했다.
- `Hero_Jump` 내부 State는 공통 종료 조건인 `IsJump == false AND IsFall == false`만 만족하면 `Exit`로 이동하도록 변경했다.
- 상위 Base Layer의 `Hero_Jump` State Machine Transition이 Exit 뒤의 목적지를 결정한다. `IsGrounded → Grounded`, `IsAttack → Attack`, `IsDash → Dash`로 한 곳에서 라우팅한다.
- Attack도 외부 State가 개별 Combo Clip을 직접 목적지로 지정하지 않고 `Attack` Sub State Machine을 거쳐 Entry와 상위 전이에서 진입·종료 흐름을 처리한다.
- 이 구조로 공중/공격 내부 Animation State는 자체 Animation 흐름만 관리하고, 행동 State를 벗어나는 공통 전이 규칙은 상위 State Machine에 집중됐다.

### Input

| 입력 | 현재 표현 | 이유 |
| --- | --- | --- |
| Jump | `OnJumpRequested` 이벤트 + `IsJumpHeld`(현재 미사용) | GroundedState의 JumpTransition이 구독 중일 때만 점프 요청을 수신해 공중 입력이 착지 뒤 재사용되지 않음. `IsJumpHeld`는 가변 점프 요구가 생길 때 사용 여부를 재검토 |
| Attack | `OnAttackRequested` 이벤트 | Input은 요청만 발행한다. 비공격 State는 `AttackTransition`이 최초 공격을 처리하고, AttackState는 `ComboTrigger`로 Animator 내부 Combo 전이를 요청한다. |
| Interact | `OnInteractRequested` 이벤트 | 상태 보관보다 단발 명령 전달이 명확 |
| Health / Stamina | UniRx 유지 | UI 등 여러 시스템이 값 변화를 구독할 수 있음 |

### Dash 실행과 착지 초기화

- `DashState`의 Enter / Execute / Exit가 Handler의 BeginDash / ExecuteDash / EndDash를 호출한다. Handler 자체의 Update·FixedUpdate로 별도 실행하지 않는다.
- `PlayerMotorData : AgentMotorData`에 Dash 속도·거리·공중 횟수를 분리한다. 현재 Handler 데이터 의존성은 PlayerMotorData이며, 모든 Agent용 데이터로 일반화한 상태는 아니다.
- Dash 진입 시 시선 방향을 확정하고 수직 속도·중력을 정지한다. 실행 중 목표 거리 또는 WallDetector 감지로 완료하고, Exit에서 속도와 중력을 정리한다.
- `DashEndTransition`은 Handler의 완료 값만 확인하고 지면 상태에 따라 Grounded/Fall을 선택한다. Animation End Event가 종료 조건은 아니다.
- GroundDetector의 `OnGroundedChanged`가 true로 바뀌면 Handler가 공중 Dash 횟수를 초기화한다. 지상 Dash 재진입이 초기화 조건이 아니다.
- Dash 남은 거리 `<= 0.001f`, LandTransition 수직 속도 `<= 0.01f`처럼 용도별 허용 오차를 둔다. 정확한 float 0 도달을 완료 조건으로 요구하지 않는다.

### 기본 Monster 실행 구조

- `MonsterController : AgentController`는 공통 초기화와 MonsterAnimator 초기화, Factory 주입, 단발 공격 시작 판정을 담당한다. 기본 공격 타입은 현재 1번으로 고정한다.
- `MonsterInput`은 마지막 x축 이동 명령을 보관하고 `RequestAttack()`으로 매개변수 없는 `OnAttackRequested`를 발행한다. Behavior·타깃 선택·공격 버퍼·자동 반복은 포함하지 않는다.
- MonsterAttackState와 MonsterHitState는 공용 State를 상속해 진입 시 수평 이동만 정지한다. 공용 GroundedState와 DeathState는 그대로 사용한다.
- Monster 전이는 등록 순서로 사망 → 피격 → 일반 행동을 평가한다. 공격·피격 종료는 등록된 GroundedState로 복귀하며, FallState는 구성하지 않는다.
- `BasicMonster_Anim.controller`는 Grounded Idle/Move Blend Tree와 단일 Attack·Hit·Death로 구성한다. 현재 AttackType parameter는 존재하지만 다중 공격 Entry 분기는 없다.
- `BasicMonster.prefab`은 종별 데이터를 비워 둔 템플릿이고, `Goblin.prefab`은 Clip Override와 Stat/Motor 데이터를 연결한 완성 variant다.
- Root의 MonsterAnimator가 Visual의 실제 Animator 한 개를 제어한다. Visual의 AgentAnimationEventProxy가 공격 유효 프레임·종료 이벤트를 Controller에 전달한다.
- 같은 네 상태·단발 공격을 사용하는 다른 몬스터는 Clip·Override·데이터·prefab variant를 교체한다. 다른 행동이나 다중 공격은 별도 확장 대상이다.

### Animator capability

| 클래스 | 책임 |
| --- | --- |
| `AgentAnimator` | Animator 참조, parameter hash 등록, 안전한 `SetBool` / `SetFloat` / `SetInteger`, 공통 Hit·Death 표현 |
| `PlayerAnimator` | Player 전용 Dictionary 및 Grounded·Jump·Fall·Dash·Attack·MoveSpeed·AttackType API |
| `MonsterAnimator` | Monster 전용 Grounded·Attack·MoveSpeed·AttackType Dictionary와 API, 실제 parameter 이름·타입 검증 |
| `AgentAnimationDataSO` | 공통 `IsHit`, `IsDeath` parameter 이름 |
| `PlayerAnimationDataSO` | Player 전용 parameter 이름 |
| `MonsterAnimationDataSO` | Monster 전용 Grounded·Attack·MoveSpeed·AttackType parameter 이름 |

모든 Animator capability interface는 `IAgentAnimator.cs`에 둔다. State는 구체 Animator가 아닌 필요한 capability interface에 의존한다.

## 4. 주제별 리팩토링 이력

### 4.1 State Machine 기반과 의존성 주입

| 변경 | 이전 문제 | 적용 결과 |
| --- | --- | --- |
| State 기반 단순화 | `AgentStateBase<T>`, `GroundedAgentStateBase`, `AgentStateMachine<IAgentState>`의 역할이 분산 | 비제네릭 `AgentStateBase`와 `AgentController._currentState` 직접 관리로 수명 구조 단순화 |
| Factory 의존성 주입 | `AgentController`가 모든 State 기능을 제공해 God Object화 | Factory가 State별 최소 Handler·Input·Animator capability를 생성자로 주입 |
| Factory 생성 순서 공용화 | 개별 Factory마다 공통 State 생성과 전이 연결을 중복할 수 있음 | AgentStateFactory<TData> 상속으로 공통 생성 → 전용 추가·교체 → 최종 객체 전이 연결 순서를 고정 |
| Type 기반 State 식별 | 하나의 `StateType` enum에 Player/Monster 상태가 누적 | `Dictionary<Type, AgentStateBase>`로 Agent별 State 확장 충돌 완화 |
| Event Rule 수명 통합 | Controller와 State에 구독 해제 책임이 분산 | `AgentStateBase.Exit()`가 Event Rule 해제 후 `OnExit()`를 호출 |

### 4.2 입력 요청과 상태 전이

| 변경 | 이전 문제 | 적용 결과 |
| --- | --- | --- |
| `ITransitionRule` 도입 | State가 `_agent.ChangeState(...)`를 직접 호출해 조건·전환 실행이 결합 | Factory가 Rule을 조합하고 State가 `OnTransition(Type)`만 발행 |
| 입력 이벤트화 | 변화 구독이 없는 입력도 UniRx 값으로 보관하고, 공중 입력이 착지 뒤 재사용될 수 있음 | Jump·Attack·Interact·Dash 요청을 Event로 발행하고 활성 State의 Rule만 구독 |
| 전이 우선순위 명시 | 동일 프레임의 입력·물리 전이 결과가 등록 순서에 의존 | Factory의 Rule 등록 순서를 행동 우선순위로 관리 |
| 점프 경계 조건 보강 | 가장자리 및 재입력에서 비정상 재상승 | 점프 요청 소비와 모터 수직 속도 기반 Fall 전이 적용 |
| float 완료 판정 허용 오차 | 착지·Dash 종료에서 정확한 0 조건 때문에 전환이 누락될 수 있음 | LandTransition의 0.01f 수직 속도, Dash의 0.001f 남은 거리처럼 물리량별 허용 오차 사용 |

### 4.3 이동·지상 상태 통합

| 변경 | 이전 문제 | 적용 결과 |
| --- | --- | --- |
| `GroundedState` 통합 | Idle/Move가 입력마다 FSM을 왕복 | 지상 정지·이동은 하나의 State에서 처리하고 Animator Blend Tree로 표현 |
| `IsGrounded` 계약 도입 | 지상 복귀 Animation 전이가 일관되지 않음 | Grounded 진입/종료 책임과 Animator 전이 조건을 명확히 분리 |

### 4.4 Animator 구조와 전이 조건 리팩토링

| 변경 | 이전 문제 | 적용 결과 |
| --- | --- | --- |
| Agent/Player Animator 분리 | 공용 Animator에 Player parameter가 누적 | 공통 Hit·Death와 Player 전용 capability·SO·Dictionary를 분리 |
| 실제 Animator 바인딩 수정 | Root와 Hero child Animator가 이중으로 존재 | `PlayerAnimator`가 실제 Hero child Animator를 제어 |
| Attack Sub-State Machine 진입 | 외부 State가 `Hero_combo1/2/3`을 직접 목적지로 지정 | 외부 State는 `Attack`만 진입하고 Entry가 `IsAttack` + `AttackType`으로 최초 Clip 선택 |
| Jump Sub-State Machine 진입 | Grounded가 Jump/Fall 시작 Clip에 직접 전이 | 외부 State는 `Hero_Jump`만 진입하고 Entry가 `IsJump` / `IsFall`로 시작 Clip 선택 |
| **공통 Exit 전이 집중** | Jump/Fall/Combo 내부 State마다 Grounded·Attack·Dash 전이를 반복 | 내부 State는 공통 Bool 해제 조건으로 Exit, 상위 Sub-State Machine이 Grounded·Attack·Dash 목적지를 한 번만 라우팅 |
| Combo timing을 Animator로 이관 | End Event·입력 버퍼·Handler가 Combo 순서를 중복 관리 | `ComboTrigger`와 Has Exit Time·Exit Time·Duration이 1→2→3 수락 시점과 전이 타이밍을 관리 |

### 4.5 공격과 Combo 책임 단순화

| 변경 | 이전 문제 | 적용 결과 |
| --- | --- | --- |
| 최초 공격 진입 공용화 | Player State가 진입 가능 여부를 판단하고 직접 상태 전이를 호출 | 공용 `AttackTransition`이 `TryStartAttack()` 성공 시에만 AttackState 진입 |
| Combo Handler·End 흐름 제거 | Combo 전용 Transition/Handler가 다음 공격 실행까지 담당해 구조가 복잡 | AttackType은 최초 진입점만 선택하고, Combo는 PlayerAttackState 입력 → Animator 내부 전이로 진행 |
| Animation End 종료 단일화 | Combo 진행과 State 종료의 End Event 책임이 혼재 | 마지막 Combo Clip만 `AttackEndTransition`으로 Grounded/Fall 복귀 |
| Trigger 정리 | 마지막 타격에서 미소비 ComboTrigger가 다음 공격에 누수될 수 있음 | `PlayerAttackState.Exit()`에서 ComboTrigger 초기화 |

### 4.6 Dash State 확장 기반

| 변경 | 이전 문제 | 적용 결과 |
| --- | --- | --- |
| Dash를 별도 State로 분리 | 일반 지상 이동 State에 Dash 규칙이 섞일 위험 | `DashState`가 Dash 실행·Animation·속도·중력 복구 수명을 담당 |
| 공중 Dash 전이 허용 | GroundDetector 조건 때문에 지상에서만 Dash 가능 | Grounded·Jump·Fall에서 `DashTransition`을 구독하고, 종료 시 Grounded/Fall을 판정 |
| 공용 기반 타입 승격 | `PlayerDashState`가 공용 전이의 목적지여서 다른 Agent 확장이 어려움 | Agent 영역의 `DashState`를 `typeof(DashState)` key와 전이 목적지로 사용. Agent별 파생 State로 확장 가능 |
| 이동·완료 판단 분리 | Animation 종료 이벤트와 실제 이동 완료를 혼동할 수 있음 | Handler가 거리·벽 감지와 완료 값 관리, DashEndTransition은 완료 여부와 복귀 목적지만 판정 |
| 실행 주체 통일 | Handler의 Unity 파이프라인과 State 실행 수명이 분리 | DashState.OnExecute에서만 ExecuteDash 호출. 불필요한 지속 실행 flag 제거 |
| 데이터 확장 | Agent 공통 이동 데이터에 Player Dash 수치가 누적될 수 있음 | PlayerMotorData에 Dash 속도·거리·공중 횟수 배치 |
| 착지 이벤트 초기화 | 공중 Dash 사용 횟수가 지상 Dash 진입 때만 초기화 | GroundDetector 상태 변경 이벤트 구독으로 실제 착지 시 초기화, 재초기화·파괴 시 구독 해제 |

### 4.7 기본 Monster 확장과 Goblin 제작

Player를 중심으로 정리한 FSM·Animator·Factory 구조를 기본 네 상태 Monster에 실제 적용했다. 기존 Orc를 신규 구조로 이관하지 않고, 전용 코드와 에셋을 제거한 뒤 Goblin을 처음부터 제작했다. 목적은 AI 의사결정이 아니라 **기본 행동 실행과 종별 에셋 교체의 기준 구현**을 만드는 것이다.

| 클래스·구성 | 이전 구조·문제 | 현재 구조·변경 결과 |
| --- | --- | --- |
| MonsterController | Animator 초기화·공격 시작 판정 누락, Factory 의존성 전달 불완전 | AgentController 기반 초기화, MonsterAnimator.Initialize, 필요한 의존성 주입, 타입 1 단발 공격 판정 |
| AIMonsterInput → MonsterInput | Behavior용 Move·Attack API와 타입 인자가 있었지만 공격 선택에 사용하지 않음 | 외부 SetMovement·RequestAttack으로 교체. 이동 명령 보관과 단발 이벤트 전달만 수행 |
| MonsterStateFactory | 독립 클래스, Attack 등록 주석 처리, 전이 미구성 | AgentStateFactory<MonsterStateFactoryData> 상속, 네 상태 생성·교체·전이 조립과 목적지 key 검증 |
| MonsterStateFactoryData | Factory 파일 내부의 빈 파생 데이터 | 별도 파일로 분리. 부모 Animator와 동일 객체인 MonsterAnimator 접근·주입 속성 추가 |
| MonsterAnimator | 공용 AgentAnimator만으로 Grounded capability를 제공할 수 없음 | Agent 공통 Hit·Death를 상속하고 Monster Grounded·Attack capability와 전용 Dictionary 구현 |
| MonsterAnimationDataSO | 신규 Monster 전용 parameter 계약 없음 | AgentAnimationDataSO 상속. Grounded·Attack·MoveSpeed·AttackType 이름과 역할 주석 추가 |
| MonsterAttackState | 추상 AttackState를 직접 생성할 수 없고 이동 속도가 남을 수 있음 | AttackState 상속, OnEnter에서 공용 공격 진입 후 StopHorizontal 수행 |
| MonsterHitState | 공용 HitState만으로 잔여 수평 속도 정지 보장 불가 | HitState 상속, 피격 진입 후 StopHorizontal 수행 |
| GroundedState·DeathState | 별도 Monster 복제를 만들 위험 | 공용 State 재사용. Attack·Hit 파생 인스턴스도 공용 Type key로 등록 |
| Monster 전이 | 행동·피격·사망 목적지 연결 없음 | Grounded: Death→Hit→Attack, Attack: Death→Hit→AttackEnd, Hit: Death→HitEnd, Death: 전이 없음 |
| 공용 Animator·prefab | 기존 Orc 전용 구성과 결합 | 네 상태 BasicMonster Controller·다섯 Clip slot·BasicMonster 템플릿 제작 |
| Goblin 에셋 | 신규 실행 구조 없음 | 다섯 Sprite 시트·Clip·Override·Stat/Motor 데이터와 Goblin prefab variant 제작 |

추가 State는 새로운 행동 종류가 아니라 공용 Attack·Hit의 실행 정책을 확장한 두 파생 클래스다. 새로운 Transition 클래스나 IMonsterAnimator 인터페이스는 추가하지 않았다.

#### 에셋 교체 기준과 삭제 정리

- 공용 Controller·Clip slot·AnimationData와 완성 prefab은 `Assets/Prefabs/Monster/` 아래 배치한다. Goblin Sprite·Clip·Override·Stat/Motor 지원 에셋은 `Assets/Prefabs/Monster/Goblin/`에 배치한다.
- Monster 스크립트는 `Assets/Scripts/FSM/NPC/AIMonstor/`의 @Hub·Input·SOData·MonsterState 역할별 경로에 둔다. 공용 Agent State·Rule은 기존 Agent 경로를 유지한다.
- 원본 Goblin 아트는 보존하고 작업용 시트만 분할·설정한다. Point·Uncompressed·PPU 100·공통 발 pivot을 적용했다. Clip FPS·Collider·공격 범위는 최종 밸런스가 아니라 검증 초기값이다.
- 기존 OrcAI prefab, Orc 전용 Clip·SO, 이전 Monster_Anim Controller, OrcBrain·Monster Actions 4개·AIMonsterInput과 해당 meta를 삭제했다. AIPlayer·NPC 공용 Action·Unity Behavior 패키지는 유지했다.
- SampleScene의 Orc 인스턴스와 연결 참조를 제거했다. 맵 설정의 monsterPrefab·spikeTrapPrefab에 들어 있던 Orc 참조를 비웠으며 Goblin으로 자동 치환하지 않았다. SampleScene 저장에 따른 Editor 직렬화 갱신도 변경 파일에 포함된다.
- BasicMonster 템플릿에 종별 Stat/Motor 데이터가 없다는 점과, Behavior가 없어 자동 이동·공격하지 않는다는 점을 인수인계에 명시했다.

#### 이번에 적용하지 않은 공격 확장안

현재 `IAgentCombatInput.OnAttackRequested`는 매개변수 없는 Action이고 MonsterController는 공격 타입 1만 준비한다. `RequestAttack(int)`·`Action<int>`·`TryStartAttack(int)` 및 Attack Sub State Machine의 다중 Entry 분기는 논의된 **후속 설계**이며 이 구현·커밋의 완료 항목이 아니다. Player의 최초 공격 타입 선택과 ComboTrigger 구조도 이번 Monster 제작으로 변경하지 않았다.

### 4.8 공용 전이와 구독 수명 안정화

Monster 제작 과정에서 공용 Rule의 기존 수명 문제를 함께 정리했다. Player와 Monster가 같은 Rule을 사용하므로 기존 Player 생성자·전이 순서는 유지하고 회귀 동작을 별도로 확인했다.

| 변경 파일 | 이전 문제 | 적용 결과 |
| --- | --- | --- |
| GetHitTransition | Subscribe 반환값 미보관, Unsubscribe가 flag만 초기화하여 구독 누적 가능 | IDisposable 보관, 중복 Subscribe 방지, 자신의 구독만 Dispose하고 참조·flag 초기화 |
| DeathTransition | 정상 참조를 차단하는 구독 조건과 Pairwise 기반 flag로 이미 사망한 값 누락 가능 | ITransitionRule로 단순화, 현재 IsDead.Value를 직접 판정. 구독·flag 제거 |
| AttackEndTransition | GroundDetector 기준 복귀만 제공해 FallState가 없는 Monster에 부적합 | 기존 Player용 생성자 유지, 명시적인 Type 고정 복귀 생성자 추가. End 구독·해제 재사용 |
| AgentController | 오브젝트 파괴 시 활성 State와 State 전환 연결 정리 없음 | OnDestroy에서 현재 State 참조 해제·Exit 후 finally에서 모든 OnTransition 연결 해제 |

GetHitTransition의 Dispose는 해당 Rule이 만든 구독만 종료한다. Health ReactiveProperty 자체나 UI 등 다른 소비자의 구독을 해제하지 않는다. Disable/Enable 풀링 수명 정책은 추가하지 않았다.

## 5. 검증 상태

| 항목 | 상태 | 근거 / 남은 확인 |
| --- | --- | --- |
| C# 컴파일 | Monster 제작 단계 완료 | 공용 Rule·Monster 코드 추가 후 Unity 컴파일 오류 없음. 아래 Player prefab 초기화 문제는 별도 런타임 설정 문제 |
| Hero Animator parameter 계약 | 완료 | `PlayerAnimationDataSO`와 Animator parameter 이름을 기준으로 등록 |
| Prefab Animator 바인딩 | 완료 | `Player`, `ProjectRE_Player Variant` 모두 Hero child Animator 연결 |
| Grounded Blend Tree 구성 | 완료 | `MoveSpeed`로 Idle/Move 표현 |
| 점프 요청 이벤트 전환 | 코드 완료, 수동 검증 필요 | GroundedState의 JumpTransition 구독 중에만 요청을 수신하도록 변경. 공중 Jump 입력 뒤 착지 재점프 여부 확인 필요 |
| Attack Sub State Machine 진입 분기 | 완료 | 외부 직접 Combo 전이 제거, `Attack` Entry의 `AttackType` 분기 구성 완료 |
| 공격 홀드 상태/Animator 동기화 | 완료 | Grounded/Fall 공격 진입, 종료 후 Grounded 복귀, 홀드 반복 흐름을 수동 Play Mode에서 확인 |
| Combo Attack 코드 구조 | 완료 | Combo Handler·입력 버퍼·End Event 기반 다음 타격 시작을 제거. 최초 Attack은 AttackTransition, Combo 연결은 Animator 내부 전이로 분리 |
| Combo 1→2→3 진행 | 수동 검증 완료 | `ComboTrigger`와 각 전이의 Has Exit Time / Exit Time / Duration을 사용해 1→3 건너뜀 없이 진행 확인 |
| Combo 종료 뒤 입력 초기화 | 완료 | 별도 입력 버퍼를 제거했으므로 Combo 종료 후 저장된 요청이 남지 않음 |
| AttackEndTransition 분리 | 완료 | 마지막 Combo의 Animation End Event가 공용 AttackEndTransition을 통해 Grounded/Fall 복귀를 처리 |
| Hero_Jump Sub State Machine 진입 분기 | 코드/구성 완료, 수동 검증 필요 | Jump/Fall Bool 상호 배제 및 Entry 분기 구성 완료. 점프와 낙하 시작 흐름을 Play Mode에서 확인 필요 |
| Animator 공통 Exit 전이 | 수동 검증 완료 | 공중 내부 State의 `IsJump == false AND IsFall == false → Exit`와 상위 `Hero_Jump → Grounded / Attack / Dash` 라우팅 동작 확인 |
| DashState 공용화·실행 로직 | 코드 구현 완료, 기존 상태 전이 수동 확인 | Handler의 거리·벽 기반 완료, State 실행 호출, 착지 이벤트 횟수 초기화 구현. 최신 Player 연속 Dash·착지 수동 검증은 별도 확인 |
| 착지·피격 종료 복귀 | Player 전체 수동 흐름 미완료 | 공용 Rule 회귀 검사와 전체 게임 조작 검증을 구분. Grounded·IsGrounded의 전체 수동 흐름 재확인 필요 |
| Monster 네 상태·전이 등록 | 제작 단계 검증 완료 | 정확히 네 Type key와 파생 Attack·Hit 인스턴스, 모든 전이 목적지 등록 확인 |
| Goblin 이동·단발 공격 | 제작 단계 Play Mode 검증 완료 | 좌·우 이동·정지·방향 전환·y 입력 제외, 공격 진입 시 수평 정지, 실제 Clip OnFrame에서 대상 체력 10→8 및 End 후 Grounded 복귀 확인 |
| 공격 요청 누수·피격 반복 | 제작 단계 검증 완료 | 공격 중 추가 요청이 다음 공격으로 예약되지 않음. 12회 반복 공격·피격에서 이전 flag 잔류 없음 |
| Monster 피격·사망 | 제작 단계 Play Mode 검증 완료 | Grounded/Attack→Hit→Grounded, Grounded/Attack/Hit의 치명타→Death 직접 전환, 실제 Death End 후 제거 확인 |
| Action·UniRx 구독 수명 | 제작 단계 검증 완료 | GetHit 중복 구독·Dispose·재진입, 이미 true인 사망 값, 파괴된 객체의 입력·State 전환 연결 해제 확인 |
| Goblin prefab·Override | 제작 단계 연결 검증 완료 | Missing Script 없음, 실제 Animator 1개, 다섯 Override slot·Clip Animation Event 연결 확인 |
| 공용 Rule의 Player 회귀 | 조건부 검증 완료 | 테스트 객체에만 누락된 DashHandler 보완 후 지상/공중 AttackEnd·Hit 복귀·기존 Hit 우선 사망 순서 확인. 원본 Player prefab의 정상 초기화까지 보장하지 않음 |
| 기존 Orc 잔여 참조 | 제작 단계 검사 완료 | 삭제 대상 C# 이름·Orc prefab GUID 잔여 참조 없음. 원본 아트·관련 없는 Agent 에셋 보존 |
| Goblin 시각·밸런스 | 수동 검증 필요 | 발 위치·크기·Clip FPS·공격 프레임·Collider·공격 범위·체력·피해 조정 필요 |

Monster 실행 검증은 원래 InGame 씬을 보존한 임시 Play Mode 씬에서 외부 명령과 실제 Animator Clip 이벤트로 수행했다. 제작 후 원래 씬을 복원했다. 수동 화면 검증까지 통과한 것으로 기록하지 않는다. 이 표는 제작 단계에서 확보한 결과이며 로그 갱신만으로 테스트를 재실행한 것은 아니다.

Player 회귀 검사에서는 저장된 `ProjectRE_Player Variant.prefab`에 AgentDashHandler2D가 없어 PlayerController.Awake의 초기화 오류가 발생했다. 당시 InGame 인스턴스에는 해당 컴포넌트가 있었으며, 원본 prefab은 수정하지 않고 테스트 객체에만 보완했다. 기존 설정 문제는 Monster 구현과 분리해 보류한다. Play Mode 진입 중 Pipeline 요청 한 번은 메인 스레드 시간 제한으로 실행되지 않았고, 진입 완료 후 재실행한 Monster 검증은 성공했다.

## 6. 보류 및 예정 사항

### 보류

- Goblin 시각·공격 타이밍·밸런스 수동 검증과 필요한 수치 조정
- 기본 Monster의 다중 공격 요청·Animator 분기: int attackType을 입력부터 실행까지 전달하는 후속 확장안. 현재 미적용
- Monster의 Behavior·Brain·타깃 선택·순찰·추적 등 의사결정 계층
- 비행·낙하·공중 공격·콤보 등 다른 행동 프로필과 다른 Monster 계열의 이행
- Player prefab의 기존 AgentDashHandler2D 누락과 전체 수동 회귀 확인
- 풀링·리스폰의 Disable/Enable 수명 정책
- PDF 산출물 재생성: 이번 최신화는 원본 MD 기준이며 기존 두 PDF는 갱신하지 않음

### 다음 권장 순서

1. 현재 단발 Goblin 기준 구현과 로그를 먼저 확정한다. 다중 공격 계획을 이미 적용한 것으로 기록하지 않는다.
2. 후속 공격 확장 시 요청 타입·실행 타입의 책임, Player 타입 선택 정책, AttackData와 Animator 공격 분기 대응을 계획으로 확정한다.
3. 확정된 계획에 따라 IAgentCombatInput·AttackTransition·공격 시작 판정·Monster Animator를 함께 변경하고 Player 회귀를 확인한다.
4. Goblin 크기·모션·타격 프레임·Collider와 밸런스를 수동 검증한다.
5. Player prefab 설정 문제와 최신 Jump·Combo·Dash 전체 수동 검증을 별도 작업으로 진행한다.
6. 같은 행동 프로필의 다음 몬스터는 공용 코드 복제 없이 에셋·Override·variant·데이터로 제작한다. 다른 프로필은 먼저 확장 범위를 정한다.
7. Behavior 의사결정은 별도 요청 이후 설계한다. 현재 명령 실행 구조에 자동 행동을 임의 추가하지 않는다.
8. 성능·GC 개선 수치는 Profiler 측정 이후에만 문서에 기록한다.

## 7. PDF 갱신 규칙

### 변경 로그 PDF

`FSM_Refactoring_Change_Log.pdf`에는 다음을 반영한다.

1. 리팩토링 이전 구조와 Factory 기반 의존성 주입 전환
2. 변경 이력을 State·입력·Animator·공격·Dash·Monster·구독 수명 등 주제별로 기록하고 각 주제 안에서 변경 전후와 적용 과정을 설명
3. 이전 구조와 현재 구조의 역할 비교
4. 검증 완료/미완료와 보류 항목

### 포트폴리오 PDF

`FSM_Refactoring_Portfolio.pdf`에는 다음을 반영한다.

1. 사용자 체감 문제
2. Controller 중심 구조에서 Factory 주입으로 전환한 설계 판단
3. GroundedState, Animator 계약, capability 분리의 핵심 개선
4. Prefab Animator 바인딩 디버깅 사례
5. 검증 근거와 다음 검증 항목
6. Goblin 제작으로 확인한 공용 Factory·State·Animator 확장과 코드 복제 없는 에셋 교체 기준

### 업데이트 시 작성 원칙

- 기본 단발 Monster 제작과 아직 구현하지 않은 다중 공격·Behavior·다른 행동 프로필을 구분한다.
- 성능 수치나 체감 개선 정도는 측정값이 없으면 추정하지 않는다.
- 수동 테스트 전에는 ‘완료’ 대신 ‘코드/구성 완료, 수동 검증 필요’로 기록한다.
- 코드 변경이 발생하면 먼저 이 문서의 변경 이력과 검증 상태를 갱신한 뒤 PDF를 갱신한다.
