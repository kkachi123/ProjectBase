# FSM Refactoring Source Log

> PDF 갱신을 위한 단일 원본 문서다. 코드 구조가 바뀌면 먼저 이 문서의 **변경 이력**, **현재 구조**, **검증 상태**를 갱신한다.
>
> 대상 범위: Player 직접 입력 기반 FSM. Monster Animator/Factory 이행은 별도 후속 작업이며, 현재 구현 완료로 간주하지 않는다.

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

전환은 State가 Controller를 직접 호출하는 방식에서 `ITransitionRule`을 평가하고 `OnTransition`을 발행하는 방식으로 옮겼다. Factory가 이 이벤트를 Controller의 전환 처리와 연결하고, 각 State에는 필요한 Rule만 조합한다. 단, 이 방식은 Player FSM을 중심으로 적용 중이며 Monster와 남아 있는 기존 State까지 전부 이행된 상태는 아니므로 **부분 적용**으로 기록한다.

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
- `AgentController`는 더 이상 구체 Animator 참조·초기화를 소유하지 않는다. Player와 이후 Monster Controller가 각 전용 Animator를 직접 초기화한다.
- `PlayerStateFactory`는 `GroundedState`, `JumpState`, `FallState`, `DashState`, `AttackState`, `HitState`, `DeathState`를 구성한다.
- State는 필요한 Handler, Input interface, Animator capability만 생성자로 받는다.
- 전이 조건은 `ITransitionRule` 구현체를 Factory에서 조합한다.

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

### Animator capability

| 클래스 | 책임 |
| --- | --- |
| `AgentAnimator` | Animator 참조, parameter hash 등록, 안전한 `SetBool` / `SetFloat` / `SetInteger`, 공통 Hit·Death 표현 |
| `PlayerAnimator` | Player 전용 Dictionary 및 Grounded·Jump·Fall·Dash·Attack·MoveSpeed·AttackType API |
| `AgentAnimationDataSO` | 공통 `IsHit`, `IsDeath` parameter 이름 |
| `PlayerAnimationDataSO` | Player 전용 parameter 이름 |

모든 Animator capability interface는 `IAgentAnimator.cs`에 둔다. State는 구체 Animator가 아닌 필요한 capability interface에 의존한다.

## 4. 주제별 리팩토링 이력

### 4.1 State Machine 기반과 의존성 주입

| 변경 | 이전 문제 | 적용 결과 |
| --- | --- | --- |
| State 기반 단순화 | `AgentStateBase<T>`, `GroundedAgentStateBase`, `AgentStateMachine<IAgentState>`의 역할이 분산 | 비제네릭 `AgentStateBase`와 `AgentController._currentState` 직접 관리로 수명 구조 단순화 |
| Factory 의존성 주입 | `AgentController`가 모든 State 기능을 제공해 God Object화 | Factory가 State별 최소 Handler·Input·Animator capability를 생성자로 주입 |
| Type 기반 State 식별 | 하나의 `StateType` enum에 Player/Monster 상태가 누적 | `Dictionary<Type, AgentStateBase>`로 Agent별 State 확장 충돌 완화 |
| Event Rule 수명 통합 | Controller와 State에 구독 해제 책임이 분산 | `AgentStateBase.Exit()`가 Event Rule 해제 후 `OnExit()`를 호출 |

### 4.2 입력 요청과 상태 전이

| 변경 | 이전 문제 | 적용 결과 |
| --- | --- | --- |
| `ITransitionRule` 도입 | State가 `_agent.ChangeState(...)`를 직접 호출해 조건·전환 실행이 결합 | Factory가 Rule을 조합하고 State가 `OnTransition(Type)`만 발행 |
| 입력 이벤트화 | 변화 구독이 없는 입력도 UniRx 값으로 보관하고, 공중 입력이 착지 뒤 재사용될 수 있음 | Jump·Attack·Interact·Dash 요청을 Event로 발행하고 활성 State의 Rule만 구독 |
| 전이 우선순위 명시 | 동일 프레임의 입력·물리 전이 결과가 등록 순서에 의존 | Factory의 Rule 등록 순서를 행동 우선순위로 관리 |
| 점프 경계 조건 보강 | 가장자리 및 재입력에서 비정상 재상승 | 점프 요청 소비와 모터 수직 속도 기반 Fall 전이 적용 |

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
| Dash를 별도 State로 분리 | 일반 지상 이동 State에 Dash 규칙이 섞일 위험 | `DashState`가 Dash Animation 수명과 기존 수평 속도 정리를 담당 |
| 공중 Dash 전이 허용 | GroundDetector 조건 때문에 지상에서만 Dash 가능 | Grounded·Jump·Fall에서 `DashTransition`을 구독하고, 종료 시 Grounded/Fall을 판정 |
| 공용 기반 타입 승격 | `PlayerDashState`가 공용 전이의 목적지여서 다른 Agent 확장이 어려움 | Agent 영역의 `DashState`를 `typeof(DashState)` key와 전이 목적지로 사용. Agent별 파생 State로 확장 가능 |

## 5. 검증 상태

| 항목 | 상태 | 근거 / 남은 확인 |
| --- | --- | --- |
| C# 컴파일 | 코드 변경 후 완료 | Combo·Dash 구조 변경 뒤 Unity 재컴파일 오류 없음 |
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
| DashState 공용화 | 코드·지상 수동 검증 완료 | `DashState` 공용 기반 State로 이동, Grounded/Jump/Fall Dash 진입과 Animation End 복귀 흐름 구성. 실제 Dash 거리·속도 정책은 보류 |
| 착지·피격 종료 복귀 | 미완료 | `GroundedState`와 `IsGrounded` 전환을 수동 확인 |

## 6. 보류 및 예정 사항

### 보류

- Monster Animator / MonsterStateFactory 분리 및 capability 기반 이행
- Legacy `AnimationDataSO` 제거: 기존 ScriptableObject 및 Prefab 직렬화 참조를 확인한 뒤 마이그레이션
- 모든 Monster에 Type 기반 상태 구성을 일괄 적용하는 작업
- 입력 이벤트화 이후 AI의 실제 공격 전이·입력 정책 통합: 현재는 새 `IAgentCombatInput` 계약에 맞춘 최소 호환 상태

### 다음 권장 순서

1. 최신 입력 이벤트화 이후 C# 컴파일을 확인하고, 공중 Jump 입력 뒤 착지 재점프·1→2→3·공중 3타·스태미나 부족을 수동 검증한다.
2. AgentAnimator 확장과 같이 base 및 확장이 필요한 Class를 확인하고 정리한다.
3. Player 수동 Play Mode 검증을 완료하고 위 표의 상태를 갱신한다.
4. Profiler를 통해 FSM 관리 방식이 Dictionary 기반으로 전환되면서 GC 부담이 줄었는지 확인한다.
5. 가장 단순한 Monster 하나를 선택한다.
6. 해당 Monster가 실제로 사용하는 행동 capability와 Animator parameter를 표로 정리한다.
7. 전용 Animator, AnimationDataSO, Factory를 함께 이행한다.
8. 기준 구현이 안정된 뒤 다른 Monster 계열로 확장한다.

## 7. PDF 갱신 규칙

### 변경 로그 PDF

`FSM_Refactoring_Change_Log.pdf`에는 다음을 반영한다.

1. 리팩토링 이전 구조와 Factory 기반 의존성 주입 전환
2. 변경 이력을 실제 적용 순서대로 기록
3. 이전 구조와 현재 구조의 역할 비교
4. 검증 완료/미완료와 보류 항목

### 포트폴리오 PDF

`FSM_Refactoring_Portfolio.pdf`에는 다음을 반영한다.

1. 사용자 체감 문제
2. Controller 중심 구조에서 Factory 주입으로 전환한 설계 판단
3. GroundedState, Animator 계약, capability 분리의 핵심 개선
4. Prefab Animator 바인딩 디버깅 사례
5. 검증 근거와 다음 검증 항목

### 업데이트 시 작성 원칙

- 구현되지 않은 Monster 확장 내용은 완료로 표현하지 않는다.
- 성능 수치나 체감 개선 정도는 측정값이 없으면 추정하지 않는다.
- 수동 테스트 전에는 ‘완료’ 대신 ‘코드/구성 완료, 수동 검증 필요’로 기록한다.
- 코드 변경이 발생하면 먼저 이 문서의 변경 이력과 검증 상태를 갱신한 뒤 PDF를 갱신한다.
