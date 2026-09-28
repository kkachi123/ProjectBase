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
- `PlayerStateFactory`는 `GroundedState`, `JumpState`, `FallState`, `AttackState`, `HitState`, `DeathState`를 구성한다.
- State는 필요한 Handler, Input interface, Animator capability만 생성자로 받는다.
- 전이 조건은 `ITransitionRule` 구현체를 Factory에서 조합한다.

### 지상 행동과 Animator

- 기존 `IdleState`, `MoveState`, `IdleToMoveTransition`은 제거했다.
- `GroundedState`가 지상 정지와 이동을 함께 처리한다.
- `Hero_Anim.controller`의 `Grounded` 1D Blend Tree가 `MoveSpeed`로 Idle/Move 표현을 선택한다.
- 점프, 낙하, 공격, 피격, 사망처럼 제어 규칙이 다른 행동만 별도 FSM State로 유지한다.

### 공격 Sub State Machine 진입

- 외부 상태(`Grounded`, `Hero_jump`, `Hero_fall_start`, `Hero_fall_loop`)는 개별 Combo State가 아닌 `Attack` Sub State Machine으로만 진입한다.
- `Attack` Sub State Machine의 `Entry`가 `IsAttack`과 `AttackType` 조건을 함께 평가해 `Hero_combo1`, `Hero_combo2`, `Hero_combo3` 중 첫 공격 애니메이션을 결정한다.
- `AttackState.OnEnter()`는 `AttackType`을 먼저 기록한 뒤 `IsAttack`을 활성화한다. Animator가 Sub State Machine에 진입하는 순간 올바른 분기 값이 준비되도록 순서를 보장한다.
- FSM은 공격 상태 진입과 종료를, Animator는 공격 종류별 클립 선택을 담당한다. Combo 종류를 추가할 때 외부 상태 전이를 늘릴 필요가 없다.

### 공중 Sub State Machine 진입

- `Grounded`는 `Hero_jump`, `Hero_fall_start` 클립에 직접 전이하지 않고 `Hero_Jump` Sub State Machine으로만 진입한다.
- `Hero_Jump`의 `Entry`가 `IsJump` 또는 `IsFall` 조건으로 각각 `Hero_jump`, `Hero_fall_start`를 선택한다.
- `JumpState`와 `FallState`는 진입 시 반대 공중 Bool을 먼저 해제해 두 Entry 조건이 동시에 성립하지 않도록 한다.

### Input

| 입력 | 현재 표현 | 이유 |
| --- | --- | --- |
| Jump | `TryConsumeJumpRequest()` | 한 입력을 한 번만 처리하고 홀드와 단발 요청을 구분 |
| Attack | `HeldAttackType` | 공격 버튼 홀드 여부/종류를 매 프레임 읽음 |
| Interact | `OnInteractRequested` 이벤트 | 상태 보관보다 단발 명령 전달이 명확 |
| Health / Stamina | UniRx 유지 | UI 등 여러 시스템이 값 변화를 구독할 수 있음 |

### Animator capability

| 클래스 | 책임 |
| --- | --- |
| `AgentAnimator` | Animator 참조, parameter hash 등록, 안전한 `SetBool` / `SetFloat` / `SetInteger`, 공통 Hit·Death 표현 |
| `PlayerAnimator` | Player 전용 Dictionary 및 Grounded·Jump·Fall·Attack·MoveSpeed·AttackType API |
| `AgentAnimationDataSO` | 공통 `IsHit`, `IsDeath` parameter 이름 |
| `PlayerAnimationDataSO` | Player 전용 parameter 이름 |

모든 Animator capability interface는 `IAgentAnimator.cs`에 둔다. State는 구체 Animator가 아닌 필요한 capability interface에 의존한다.

## 4. 변경 이력

| 단계 | 변경 | 문제 / 이유 | 결과 |
| --- | --- | --- | --- |
| 01 | State 기반·상태 머신 단순화 | `AgentStateBase<T>`, `GroundedAgentStateBase`, `AgentStateMachine<IAgentState>`가 역할을 나눔 | 비제네릭 `AgentStateBase`와 Controller의 `_currentState` 직접 관리로 구조를 단순화 |
| 02 | StateFactory 기반 의존성 주입 | `AgentController`가 모든 State용 기능을 제공 | State별 책임과 의존성이 생성자에 드러남 |
| 03 | `ITransitionRule` 도입 | State 내부의 `_agent.ChangeState` 호출에 전이 조건·이벤트 처리가 결합 | Factory에서 전이 규칙을 조합하고 `OnTransition`으로 전환 요청. Player 기준 부분 적용 |
| 04 | Type 기반 State 관리 | 공용 `StateType` enum에 Player/Monster 상태가 누적 | 캐릭터별 상태 확장 간 충돌 부담 완화 |
| 05 | PlayerInput UniRx 정리 | 구독되지 않는 입력까지 ReactiveProperty 사용 | 입력을 홀드·소비형 요청·이벤트로 구분 |
| 06 | `GroundedState` 통합 | Idle/Move가 입력마다 FSM을 왕복 | 지상 이동 전이 수 감소, 표현은 Blend Tree로 이관 |
| 07 | Animator 전이 계약 정리 | FSM과 Animator가 서로 다른 상태를 표현 | 종료 목적지를 Grounded로 단일화, `IsGrounded` 도입 |
| 08 | 점프 경계 조건 보강 | 가장자리/재입력 시 비정상 재상승 | 점프 요청 소비와 하강 시 Fall 전이 조건 적용 |
| 09 | Agent/Player Animator 분리 | 공통 Animator에 Player parameter가 누적 | 공통 기능과 Player 전용 capability·SO 분리 |
| 10 | Prefab Animator 바인딩 수정 | Root Animator와 Hero child Animator가 이중 구성 | `PlayerAnimator`가 실제 Hero child Animator를 제어 |
| 11 | Attack Sub State Machine Entry 분기 | 외부 상태가 `Hero_combo1/2/3`에 직접 전이해 FSM과 Animator의 공격 선택 책임이 분산 | 외부 상태는 `Attack`으로만 진입하고, Entry가 `IsAttack` + `AttackType`으로 첫 Combo를 선택 |
| 12 | Hero_Jump Sub State Machine Entry 분기 | `Grounded`가 점프/낙하 시작 클립에 직접 전이 | 외부 상태는 `Hero_Jump`으로만 진입하고, Entry가 `IsJump` / `IsFall`로 시작 클립을 선택 |

## 5. 검증 상태

| 항목 | 상태 | 근거 / 남은 확인 |
| --- | --- | --- |
| C# 컴파일 | 완료 | 최종 구조 변경 후 컴파일 실패 없음 |
| Hero Animator parameter 계약 | 완료 | `PlayerAnimationDataSO`와 Animator parameter 이름을 기준으로 등록 |
| Prefab Animator 바인딩 | 완료 | `Player`, `ProjectRE_Player Variant` 모두 Hero child Animator 연결 |
| Grounded Blend Tree 구성 | 완료 | `MoveSpeed`로 Idle/Move 표현 |
| 점프 경계 코드 보강 | 완료 | 소비형 점프 요청 및 하강 시 Fall 조건 반영 |
| 점프 경계 체감 검증 | 미완료 | 가장자리 접근·공중 재입력·홀드 조작을 수동 Play Mode로 반복 확인 |
| Attack Sub State Machine 진입 분기 | 완료 | 외부 직접 Combo 전이 제거, `Attack` Entry의 `AttackType` 분기 구성 완료 |
| 공격 홀드 상태/Animator 동기화 | 완료 | Grounded/Fall 공격 진입, 종료 후 Grounded 복귀, 홀드 반복 흐름을 수동 Play Mode에서 확인 |
| Hero_Jump Sub State Machine 진입 분기 | 코드/구성 완료, 수동 검증 필요 | Jump/Fall Bool 상호 배제 및 Entry 분기 구성 완료. 점프와 낙하 시작 흐름을 Play Mode에서 확인 필요 |
| 착지·피격 종료 복귀 | 미완료 | `GroundedState`와 `IsGrounded` 전환을 수동 확인 |

## 6. 보류 및 예정 사항

### 보류

- Monster Animator / MonsterStateFactory 분리 및 capability 기반 이행
- Legacy `AnimationDataSO` 제거: 기존 ScriptableObject 및 Prefab 직렬화 참조를 확인한 뒤 마이그레이션
- 모든 Monster에 Type 기반 상태 구성을 일괄 적용하는 작업

### 다음 권장 순서

1. AgentAnimator 확장과 같이 base 및 확장이 필요한 Class를 확인하고 정리한다.
2. Player 수동 Play Mode 검증을 완료하고 위 표의 상태를 갱신한다.
3. Profiler를 통해 FSM 관리 방식이 Dictionary 기반으로 전환되면서 GC 부담이 줄었는지 확인한다.
4. 가장 단순한 Monster 하나를 선택한다.
5. 해당 Monster가 실제로 사용하는 행동 capability와 Animator parameter를 표로 정리한다.
6. 전용 Animator, AnimationDataSO, Factory를 함께 이행한다.
7. 기준 구현이 안정된 뒤 다른 Monster 계열로 확장한다.

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
