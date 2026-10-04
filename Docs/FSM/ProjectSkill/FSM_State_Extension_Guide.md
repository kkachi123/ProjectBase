# FSM State 확장 구현 가이드

## 목적

이 문서는 ProjectRE의 **기존 특정 Agent에 새 행동 State를 추가하거나 공용 State를 Agent별로 확장**할 때 사용하는 작업 지침이다.
다른 AI Agent 또는 개발자가 이 문서와 현재 소스를 함께 읽고, 기존 책임과 수명을 유지하며 State를 확장하는 것을 목표로 한다.

- 적용: 기존 Player·Monster 등의 행동 추가, 공용 State의 파생 구현, 관련 Input·Transition·Handler·Animator 연결.
- 제외: 새로운 Agent 전체 제작, 미요청 AI 판단 로직 추가, 범위 밖 공용 구조 개편.
- 새 Agent 전체 제작은 `Docs/FSM/ProjectSkill/FSM_Agent_Creation_Guide.md`를 사용한다.
- 이 문서의 예시는 자동으로 최신화되지 않는다. 경로·시그니처·현재 구현 여부를 소스와 대조하고, 설계 예시를 구현된 기능으로 취급하지 않는다.

현재 FSM의 핵심 원칙은 다음과 같다.

> **FSM이 행동과 상태 전이를 결정하고, Animator는 FSM이 설정한 parameter를 표현한다.**

하나의 FSM State 안에서 여러 세부 모션을 표현할 수 있다. 예를 들어 현재 Player 콤보는 같은 AttackState 안에서 Animator의 ComboTrigger로 진행한다. 모든 Clip을 별도 FSM State로 만들라는 의미가 아니다.

---

## 0. 작업 시작 절차

### 대상과 요구사항 확인

1. 확장할 Agent·Controller·Prefab과 추가하거나 변경할 행동을 확인한다.
2. 진입 가능한 State, 종료 조건과 목적지, 피격·사망·다른 입력에 의한 중단 가능 여부를 확정한다.
3. 기존 State·Transition·Handler·Animator capability로 해결 가능한지 먼저 판단한다.
4. 코드·에셋·Scene 배치·문서 중 작업 범위와 수동 작업 분담을 확인한다.
5. 관련 파일의 사용자 변경을 확인하고, 범위 밖 코드·Prefab·Scene을 보존한다.

사용자가 계획만 요청하면 구현하지 않는다. 행동 규칙을 바꾸는 미지정 사항은 확인하고, 안전하게 진행 가능한 분석은 계속한다.

### 먼저 읽을 소스

경로는 프로젝트 루트 기준이다. 대상 Agent에 해당하는 파일을 선택하고, 이동된 파일은 현재 위치를 찾아 읽는다.

| 확인 대상 | 실제 경로 또는 선택 기준 |
| --- | --- |
| 공통 실행·전환 | `Assets/Scripts/FSM/Agent/@Hub/AgentController.cs` |
| 지면 감지 기반 | 필요한 경우 `Assets/Scripts/FSM/GroundedAgent/@Hub/GroundedAgentController.cs` |
| State 수명 | `Assets/Scripts/FSM/Agent/StateControl/AgentStateBase.cs` |
| Factory·공통 DI 데이터 | `Assets/Scripts/FSM/Agent/StateControl/AgentStateFactory.cs`에 두 클래스가 함께 정의됨 |
| 대상 Factory·FactoryData | Player: `Assets/Scripts/FSM/Player/PlayerState/PlayerStateFactory.cs`; 기본 Monster: `Assets/Scripts/FSM/NPC/AIMonstor/MonsterState/` |
| 입력·Rule 계약 | `Assets/Scripts/FSM/Agent/Input/IAgentInput.cs`, `Assets/Scripts/FSM/Agent/StateControl/TransitionRules/ITransitionRule.cs` |
| Animator 계약 | `Assets/Scripts/FSM/Agent/@Hub/AgentAnimator.cs`, `Assets/Scripts/FSM/Agent/@Hub/IAgentAnimator.cs`와 대상 전용 Animator·AnimationDataSO |
| 유사 행동 | `Assets/Scripts/FSM/Agent/StateControl/States/`, `Assets/Scripts/FSM/GroundedAgent/StateControl/States/`, 대상 Agent 전용 State·Handler |
| 직렬화 연결 | 대상 Prefab의 컴포넌트·데이터, 실제 Animator Controller·Override·Clip·Animation Event |

대상 Controller의 초기화부터 FactoryData 주입·State 등록·전이·실제 Animator까지 연결을 따라간다. 클래스 이름만 보고 다른 Agent에 같은 기능이 있다고 가정하지 않는다.

### 작업 결과에 남길 내용

- 재사용·추가·수정 파일과 각 책임.
- 출발 State별 진입 조건·우선순위, 새 State의 수행·종료·중단 규칙.
- FactoryData·Controller 초기화에 추가할 의존성.
- Animator parameter·모션·Event 변경과 수동 확인 항목.
- 컴파일·정적 연결·Play 확인 결과 및 미검증 범위.

---

## 1. 현재 구조 요약

```text
PlayerInput / Agent Input
  → IAgent...Input Event
  → ITransitionRule
  → AgentStateBase.OnTransition(Type)
  → AgentController.ChangeState(Type)
  → AgentStateBase.Enter / Execute / Exit
  → Animator parameter 및 Animation Event
```

| 요소 | 책임 |
| --- | --- |
| `AgentStateBase` | State 수명(`Enter`, `Execute`, `Exit`)과 Event Transition 구독·해제 |
| `ITransitionRule` | 전이 가능 여부와 목적 State Type 판단 |
| `IEventTransitionRule` | 입력/Animation Event 등의 구독·해제 수명 관리 |
| `AgentController` | 현재 State 보유, `Type` 키로 State 전환, Animation End Event 전달 |
| `AgentStateFactory<TData>` / 대상 Factory | 공통·Agent별 State 생성·교체, 의존성 주입, Transition 등록 순서 결정 |
| `AgentAnimator` / 전용 Animator | FSM 상태를 Animator parameter로 표현 |
| `AgentAnimationEventProxy` | Animation Event를 Controller/Event Source로 전달 |

State Dictionary는 `Type` 키를 사용한다.

```csharp
Dictionary<Type, AgentStateBase> states;
```

따라서 공용 기반 State를 확장하는 Agent는 **공용 기반 타입을 key와 전이 목적지로 유지**한다.

현재 MonsterStateFactory에 등록된 실제 사례:

```csharp
states.Add(typeof(AttackState), new MonsterAttackState(data.MonsterAnimator, data.CombatHandler, data.Motor));
// AttackTransition.NextStateType == typeof(AttackState)
```

이 방식이면 공용 TransitionRule은 Agent별 파생 State를 알 필요가 없다.

새 독립 행동이면 그 행동의 State 타입을 key로 사용한다. 현재 Player의 Dash 등록은 `typeof(DashState)`와 `new DashState(...)`이며, `PlayerDashState`는 구현되어 있지 않다.

현재 State 수명은 다음 순서다.

- `Enter()`: Event Rule 구독 → `OnEnter()`.
- `Execute()`: 등록 순서대로 Rule 평가 → 전이가 없을 때만 `OnExecute(deltaTime)`.
- `Exit()`: Event Rule 해제 → `OnExit()`.

전이가 발생한 Execute 호출에서는 이전 State의 행동을 계속 실행하지 않는다. Controller의 기존 Update 실행과 별개로 Handler에 중복 실행 루프를 추가하지 않는다.

---

## 2. 새 State 추가 전 결정할 사항

구현 전 아래 항목을 먼저 결정한다.

1. 행동의 상태 수명
   - 어떤 입력/조건으로 진입하는가?
   - Execute 동안 매 프레임 처리할 규칙이 있는가?
   - 어떤 조건 또는 Animation Event로 종료하는가?

2. 공용 여부
   - 여러 Agent가 공유할 최소 책임이 있는가?
   - Agent별 수치·판정·입력 방식만 다른가?
   - 그렇다면 공용 `XState`와 Agent별 `PlayerXState`, `MonsterXState` 확장을 고려한다.

3. 전이 가능 범위
   - 어떤 기존 State에서 진입 가능한가?
   - 행동 중 Attack, Jump, Hit, Death 등으로 취소 가능한가?
   - 같은 프레임에 여러 Rule이 충족되면 어떤 행동이 우선인가?

4. Animator 계약
   - Bool, Int, Float, Trigger 중 어떤 parameter가 필요한가?
   - Animation End Event가 FSM 종료에 필요한가?
   - Animator 내부 전이와 FSM State 전이가 같은 흐름을 표현하는가?

5. 입력·데이터·실행 정책
   - 거절된 입력은 버리는가, 일정 조건까지 예약하는가? 미요청 입력 버퍼는 추가하지 않는다.
   - 필요한 데이터·Detector·Handler가 대상 Agent에 있는가?
   - Update·FixedUpdate 중 어디서 어떤 갱신을 수행하는가? 같은 행동을 두 루프에서 중복 실행하지 않는다.

---

## 3. 책임 경계 규칙

### 3.1 기존 State의 책임을 침범하지 않는다

행동이 독립적인 진입·수행·종료 수명을 가지면 별도 State로 만든다. 기존 행동의 작은 설정 차이까지 모두 새 State로 나누거나, 독립 행동을 기존 State 내부의 무관한 `if` 분기로 추가하지 않는다.

아래 코드는 책임 경계 설명용 의사 코드다. 실제 Motor API에 복사하는 구현 예시가 아니다.

```csharp
// 피해야 할 예: GroundedState가 일반 이동과 Dash를 동시에 담당
if (_dashInputPressed)
{
    _motor.Move(direction, dashSpeed);
    _animator.SetDash(true);
    return;
}

HandleNormalMove();
```

```csharp
// 권장: GroundedState는 일반 이동만, DashState는 Dash만 담당
// GroundedState → DashTransition → DashState
```

판단 질문:

> 이 코드가 새 행동이 없는 상황에서도 계속 필요한가?

- 그렇다: 기존 State 또는 공용 Handler의 책임일 가능성이 높다.
- 아니다: 새 State 또는 새 행동 전용 Handler의 책임이다.

### 3.2 공용 State와 Agent별 확장

공용 State는 여러 Agent가 공유할 최소 규칙을 담당한다. 특정 Agent의 입력 구현체·전용 parameter·고유 규칙을 불필요하게 참조하지 않는다. 현재 클래스 이름이 공용이라고 의존성까지 완전히 공용화되었다고 단정하지 않는다.

현재 구현과 향후 확장 예시는 구분한다.

| 구분 | 내용 |
| --- | --- |
| 현재 구현 | `DashState`가 `AgentDashHandler2D`의 Begin·Execute·End를 호출하고 Animator Bool 수명을 관리 |
| 현재 의존성 | DashHandler는 `PlayerMotorData`·GroundDetector·WallDetector를 주입받음 |
| 향후 설계 예시 | `PlayerDashState`, `MonsterDashState`, `IDashExecution`은 현재 구현되지 않은 선택지 |

다음은 필요할 경우에만 적용할 **가상 확장 구조**다. 문서에 등장한다는 이유로 클래스·인터페이스를 생성하지 않는다.

```text
DashState
 ├─ 공통: Dash 진입·실행·종료 수명과 표현
 ├─ PlayerDashState : DashState
 │   └─ 방향 고정, 거리, 스태미나, 무적 등 Player 규칙
 └─ MonsterDashState : DashState
     └─ 돌진 대상, 충돌 피해, AI 거리 판정 등 Monster 규칙
```

상속보다 Handler/인터페이스 주입이 나은 경우도 있다.

- 행동의 State 수명은 같고 실행 정책만 다르다: 기존 Handler·데이터 주입으로 충분한지 먼저 확인한다. 실제 다형성이 필요한 경우에만 `IDashExecution` 같은 전략 계약을 검토한다.
- 진입·종료·전이까지 크게 다르다: Agent별 파생 State를 사용한다.

### 3.3 종료 판정과 허용 오차

물리 값·누적 거리의 정확한 0 도달만으로 종료를 판단하면 부동소수점 오차로 State에 남을 수 있다. 단위·규모·게임플레이 의도에 맞는 허용 오차를 적용한다.

현재 소스의 사례:

- `AgentDashHandler2D`: 남은 거리 `<= 0.001f`이면 완료.
- `LandTransition`: 지면 감지와 `VerticalVelocity <= 0.01f`로 착지 전이 판정.

모든 비교에 같은 상수를 적용하거나 상승 중 착지를 허용할 정도로 오차를 크게 잡지 않는다. 경계값·작은 잔여값·반복 동작을 테스트한다. 새 행동의 종료 방식은 Animation Event·시간·물리 완료 중 실제 요구에 맞게 선택한다.

---

## 4. 구현 순서

### Step 1 — 기존 계약 재사용 또는 필요한 계약 추가

기존 입력·기능 계약으로 해결 가능한지 먼저 확인한다. 부족한 기능만 추가하며, 미사용 capability를 Agent에 일괄 할당하지 않는다.

다음은 `IAgentInput.cs`에 이미 존재하는 실제 계약이다. 동일 인터페이스를 새 파일에 중복 정의하지 않는다.

```csharp
public interface IAgentDashInput
{
    event Action OnDashRequested;
}
```

- Input 구현체는 Event를 발행한다.
- State는 Input 구현체를 직접 참조하지 않는다.
- Event Transition은 해당 Rule이 등록된 State가 활성일 때 `Subscribe()`로 요청을 수신한다.
- State 내부 행동용 구독이 별도로 필요하면 `OnEnter()`·`OnExit()`에서 대칭으로 관리한다. 지속적인 Detector·Handler 구독은 초기화·종료 수명에 맞춰 따로 관리한다.

### Step 2 — State 구현

새 독립 State는 `AgentStateBase`를 상속한다. 공용 행동의 세부 구현을 확장하면 기존 공용 State를 상속한다.

아래 `XState`는 가상 이름의 수명 스켈레톤이며 현재 프로젝트 클래스가 아니다. 실제 의존성은 생성자로 주입한다.

```csharp
public class XState : AgentStateBase
{
    protected override void OnEnter() { }
    protected override void OnExecute(float deltaTime) { }
    protected override void OnExit() { }
}
```

수명별 책임:

| 구간 | 담당 내용 |
| --- | --- |
| `OnEnter` | Animator 진입 parameter, 초기 속도/방향, Handler 시작 |
| `OnExecute` | 해당 행동 중에만 필요한 이동·판정·시간 처리 |
| `OnExit` | Animator parameter 해제, Trigger 정리, Handler 종료 |

행동 중단 경로에서도 속도·중력·예약 값 등의 정리가 실행되는지 확인한다. 파생 State가 공통 진입·종료 함수를 override하면 필요한 `base` 동작을 보존한다.

### Step 3 — TransitionRule 구현

입력/Animation Event 기반 Rule은 `IEventTransitionRule`을 구현한다.

시간·Detector·Handler 완료 값을 직접 검사하는 Rule은 `ITransitionRule`로 충분하다. 현재 DashEndTransition도 이벤트 Rule이 아니다.

아래는 가상 Rule 스켈레톤이다. 이벤트 연결은 생략되어 있으며 실제 입력·수명에 맞춰 구현해야 한다.

```csharp
public class XTransition : IEventTransitionRule
{
    public Type NextStateType => typeof(XState);
    private bool _shouldTransition;

    public void Subscribe() { /* Event += Handler */ }
    public void Unsubscribe()
    {
        // Event -= Handler
        _shouldTransition = false;
    }
    public bool ShouldTransition(float deltaTime) => _shouldTransition;
}
```

규칙:

- `Subscribe()`는 중복 구독을 막는다.
- `Unsubscribe()`에서 반드시 구독 해제와 전이 flag 초기화를 수행한다.
- Stamina·사용 횟수 등 시작 조건을 충족할 때만 전이를 허용한다. 실패한 요청을 폐기할지 예약할지는 확정한 정책을 따른다. 현재 Dash는 평가 시 요청을 소비하며, AttackTransition과 예약 수명이 동일하다고 가정하지 않는다.
- 공용 State의 파생 구현으로 갈 때 `NextStateType`은 공용 기반 key를 가리킨다. 독립 행동이면 그 행동에 등록한 key를 사용한다.
- Rule 구독·해제는 `AgentStateBase.Enter()`·`Exit()`에서 이미 호출한다. Controller의 ChangeState에 같은 구독 관리를 추가하지 않는다.
- UniRx를 사용하는 Rule은 자신의 `IDisposable`을 보관·해제한다. 다른 구독을 함께 제거하지 않는다.

### Step 4 — Factory 등록 및 우선순위 설정

대상 Agent가 이미 사용하는 `AgentStateFactory<TData>` 파생 Factory와 FactoryData를 확장한다. State 하나를 추가하기 위해 Controller·Factory를 새로 복제하지 않는다.

```text
AddCommonStates(data, states)
  → AddAgentStates(data, states)
  → ConfigureTransitions(data, states)
```

| 단계 | 수정 기준 |
| --- | --- |
| `AddCommonStates` | 현재 기본 구현은 Hit·Death 생성. 요청과 관련 없는 공통 구성을 유지 |
| `AddAgentStates` | 새 State 생성 또는 공용 State를 전용 구현으로 교체 |
| `ConfigureTransitions` | 최종 State 객체에 진입·종료·중단 Rule 연결 |
| 대상 FactoryData | 필요한 의존성만 추가. 공통 데이터는 `StateFactoryData`에서 재사용 |
| 대상 Controller 초기화 | 필요한 컴포넌트·Handler를 초기화한 후 FactoryData에 전달 |

State 교체는 전이 연결 전에 수행한다. 전이를 붙인 객체를 나중에 교체하여 Rule을 잃지 않는다. 전용 Animator 접근자가 필요하면 공통 `Animator` 참조를 전달하는 형태를 유지하고 이중 참조를 만들지 않는다.

다음은 현재 PlayerStateFactory의 Dash 연결 발췌다. 이미 있는 등록을 다시 추가하는 구현 지시가 아니다.

```csharp
// AddAgentStates의 실제 등록.
states.Add(typeof(DashState), new DashState(data.PlayerAnimator, data.DashHandler));

// ConfigureTransitions의 실제 Grounded 진입 순서 일부.
states[typeof(GroundedState)].AddTransition(new GetHitTransition(data.Health.CurrentHealth));
states[typeof(GroundedState)].AddTransition(new DashTransition(data.DashInput, data.DashHandler));

// Dash의 실제 종료 Rule.
states[typeof(DashState)].AddTransition(new DashEndTransition(data.DashHandler, data.GroundDetector));
```

실제 피격 Rule 이름은 `GetHitTransition`이다. `HitTransition`·`NormalTransition` 같은 가상 이름을 현재 구현으로 취급하지 않는다.

`AgentStateBase`는 등록된 순서대로 Rule을 평가한다. 즉, **AddTransition 순서가 우선순위**다.

권장 판단 순서:

1. Death / Hit 등 강제 행동
2. 즉시 입력 행동(Dash 등)
3. 일반 물리 전이(낙하, 착지)
4. 일반 행동 입력

실제 우선순위는 기획 의도에 맞춰 결정하며, 기존 순서를 무심코 변경하지 않는다.

- 모든 출발 State에 무조건 새 전이를 추가하지 않는다. 허용한 State에만 연결한다.
- 모든 목적 State가 Dictionary에 등록되는지 확인한다. Grounded·Fall 등 동적 목적지는 반환 가능한 모든 타입을 확인한다.
- `state.OnTransition → Controller.ChangeState` 연결은 Controller의 기존 Start 수명에서 수행한다. Factory는 Rule 구성을 담당하며 같은 이벤트를 중복 연결하지 않는다.
- 공용 Factory·Rule을 수정했다면 Player·Monster 등 기존 사용처의 영향을 확인한다.

### Step 5 — Animator 계약 연결

1. `IAgentAnimator.cs`의 기존 capability를 재사용하고, 부족한 기능만 해당 파일에 추가한다. 별도 `IPlayerAnimator.cs`는 생성하지 않는다.
2. Agent 공용 parameter와 Agent별 parameter를 분리한다. 현재 AgentAnimator의 공통 capability는 Hit·Death다.
3. 대상 전용 Animator에 필요한 parameter hash Dictionary와 의미별 API를 추가한다. 설정 동작은 AgentAnimator의 제네릭 Register·SetBool·SetFloat·SetInteger·Trigger·ResetTrigger를 재사용한다.
4. State의 Animator 값 변경은 주입받은 capability API를 통해 수행한다. 다른 행동·Handler 호출을 금지하는 의미는 아니다.
5. Animation 종료가 State 종료 조건이면 Clip에 `AgentAnimationEventProxy.OnAnimationEnd`를 연결하고 EndTransition으로 받는다.

전용 AnimationDataSO의 parameter 이름·타입과 실제 Animator를 함께 갱신한다. Animator Override는 Clip 교체이지 State·parameter·Transition 추가가 아니므로, 새 행동에 필요한 기반 Controller 분기를 따로 확인한다.

Animator는 행동을 판정하지 않는다. FSM이 State 전환을 결정하고, Animator는 해당 State를 표현한다.

---

## 5. Animator 구성 원칙

### 공통 출구

Jump/Fall처럼 Sub-State Machine 내부에 여러 Animation State가 있고 공통적으로 외부 행동으로 나가야 할 때는 다음 구조를 사용한다.

```text
Hero_Jump 내부
  Hero_jump / Hero_fall_start / Hero_fall_loop
    └─ IsJump == false AND IsFall == false → Exit

상위 Base Layer
  Hero_Jump Exit
    ├─ IsGrounded → Grounded
    ├─ IsAttack   → Attack
    └─ IsDash     → Dash
```

- 내부 State는 공중 내부 Animation 흐름만 관리한다.
- 외부 목적지는 상위 State Machine Transition에서 한 번만 결정한다.
- Animator 조건은 한 Transition 안에서 AND로 평가된다.
- OR 조건은 별도 Transition으로 만들어야 한다.
- 위 구조는 Jump/Fall의 공통 출구 사례다. 내부 모션이 하나인 새 행동까지 Sub State Machine으로 만들 필요는 없다.

### 상태 Bool 수명

FSM State가 Animator 상태 Bool의 소유자다.

```text
Jump Exit     → IsJump = false
Fall Exit     → IsFall = false
Grounded Enter → IsGrounded = true
Attack Enter   → IsAttack = true
Dash Enter     → IsDash = true
```

상태 전환 시 이전 State의 Bool을 해제하지 않으면 서로 다른 Animator 전이가 동시에 충족될 수 있다.

### Animation Event와 실제 재생 경로

- 실제 Unity Animator와 AgentAnimationEventProxy의 연결을 확인한다. 전용 Animator 어댑터와 Unity Animator 컴포넌트는 서로 다르다.
- 단일 Visual 구성에서 Root·Child에 실제 Animator가 중복 배치되어 다른 인스턴스를 제어하지 않는지 확인한다.
- Override로 교체한 Clip의 Event를 직접 확인한다. 원본 슬롯의 Event가 자동 승계된다고 가정하지 않는다.
- End가 필요한 경우 의도한 종료 시점에 실제 Event를 수신해야 한다. 정확한 마지막 프레임이라는 이유만으로 안전하다고 판단하지 않는다.
- Has Exit Time·Duration·Interruption 설정으로 End 전에 빠져나가거나, 중단된 Clip의 Event가 다른 State에 영향을 주지 않는지 확인한다.
- Handler 완료·시간·물리 판정으로 종료하는 행동에는 종료용 Animation Event를 무조건 추가하지 않는다.

---

## 6. 구현 후 확인 체크리스트

### 코드 및 FSM

- [ ] 대상 Controller·Factory·FactoryData·Animator·Input과 유사 행동의 현재 소스를 확인했다.
- [ ] 실제 구현·가상 설계 예시를 구분하고 불필요한 클래스·인터페이스를 추가하지 않았다.
- [ ] 새 State가 기존 State의 일반 동작을 직접 처리하지 않는다.
- [ ] 필요한 Input/Handler/Animator 의존성이 Factory에서 주입된다.
- [ ] 필요한 컴포넌트·SO·직렬화 참조가 초기화되고 null 의존성이 주입되지 않는다.
- [ ] State 추가·교체 후 최종 객체에 Transition이 연결된다.
- [ ] Type key·Rule 목적지·State 구현이 호환되며 모든 동적 목적지가 등록된다.
- [ ] Event Rule이 중복 구독되지 않으며 State Exit에서 해제된다.
- [ ] State 자체 이벤트·Handler 지속 구독도 각 수명에 맞춰 정리된다.
- [ ] 전이 flag, Trigger, 행동 예약 값이 State Exit에서 적절히 초기화된다.
- [ ] Transition 등록 순서가 의도한 우선순위와 일치한다.
- [ ] 공중/지상/피격/사망 등 경계 State에서 전이 허용 범위가 명확하다.
- [ ] 입력 거절·재진입 시 요청 예약·소비가 지정한 정책과 일치한다.
- [ ] 물리 값·누적 거리 종료 판정에 적절한 허용 오차와 경계값 검증을 적용했다.
- [ ] Update·FixedUpdate·Handler에서 같은 행동을 중복 실행하지 않는다.
- [ ] 중단·파괴 경로에서 속도·중력·행동 설정이 정리된다. 풀링이 범위라면 별도 재사용 수명도 확인했다.

### Animator

- [ ] parameter 이름과 Animator API/SO 데이터 이름이 정확히 일치한다.
- [ ] 새 State의 진입·종료 조건이 FSM parameter 수명과 일치한다.
- [ ] 여러 내부 모션의 공통 출구가 필요한 경우 상위에서 외부 목적지를 관리한다.
- [ ] Clip 종료가 필요하면 의도한 시점의 Animation Event를 실제 재생 경로에서 수신한다.
- [ ] 기본 parameter 값이 초기 State와 충돌하지 않는다. 예: Grounded 시작 시 `IsFall` 기본값은 false.
- [ ] 실제 Animator 참조·Override 교체 Clip·Proxy가 올바르게 연결된다.

### Unity 검증

- [ ] Unity 재컴파일 후 새 State/Transition 관련 compile error가 없다.
- [ ] Console에 Animator transition 무시 warning이 없다.
- [ ] FSM debug log와 실제 Animator State가 같은 순서로 바뀐다.
- [ ] 최초 진입·정상 종료·피격/사망 중단·반복 재진입을 확인했다.
- [ ] 수정한 공용 코드의 기존 사용 Agent 회귀를 확인했다.
- [ ] 정적 점검·Editor 연결·Play 확인·사용자 수동 확인 결과를 구분했다.

컴파일 성공만으로 실제 행동 완료를 보고하지 않는다. 도구 연결 실패나 미실행 테스트는 미검증으로 기록하고, 새 작업과 무관한 기존 오류는 구분한다.

---

## 7. 역할 분담

### AI Agent가 담당할 작업

- 대상 Agent의 현재 FSM 및 유사 State 분석
- State 책임·공용화·상속/Handler 분리 설계
- C# State, TransitionRule, Factory, DI, Animator interface 구현
- Type-key 등록과 전이 우선순위 점검
- Animation Event routing과 Event 구독 수명 검증
- Unity Editor의 compile/console/Animator 연결 상태 확인
- 요청된 계획 문서와 리팩토링 로그 최신화

### 요청 범위·도구 연결에 따라 담당을 정할 작업

- Animation Clip 제작 및 선택
- Animator parameter 생성과 기본값 설정
- Animator State/Sub-State Machine 배치
- Animator Transition 조건, Exit Time, Duration, 우선순위 조정
- Clip의 Animation Event 프레임 배치

사용자가 Animator·Clip 제작을 직접 하겠다고 지정하면 AI는 필요한 parameter·전이 조건·Event 이름·검증 항목을 전달한다. 에셋 제작까지 요청했고 Editor 도구가 연결되면 AI가 해당 범위에서 구성·검증한다. 항상 사용자 작업으로 고정하지 않는다.

### 사용자가 확정·확인할 사항

- 거리·속도·쿨다운·스태미나·무적·캔슬 등 게임플레이 규칙과 최종 수치.
- Play Mode 조작감·공격 타이밍·시각 표현·밸런스.
- AI가 수행하지 못한 수동 테스트의 재현 결과.

### Editor 작업과 범위 보존

- Scene·Prefab·Animator·Clip·SO 변경은 연결된 Unity Editor의 CLI/MCP·Editor API로 수행한다. YAML을 직접 편집해 참조를 연결하지 않는다.
- Editor가 연결되지 않으면 가능한 소스·계획 작업과 남은 에셋 작업을 구분해 보고한다. 탐색·권한 실패만으로 Editor 미실행을 단정하지 않는다.
- 사용자 Scene의 dirty 상태·기존 배치를 보존한다. 임시 테스트 요소는 작업 후 정리한다.
- 요청하지 않은 기존 Agent 삭제·Behavior 구현·PDF 제작·Git 커밋·푸시로 범위를 확장하지 않는다.
- C# 주석은 높임말 없이 간결하게 작성하고, 설명이 필요한 클래스·API에는 `<summary>`를 사용한다.

### 수동 테스트 결과로 전달할 정보

- FSM State와 Animator State가 불일치한 시점
- 재현 입력 순서와 행동 상태
- 문제가 나는 Clip/Transition 및 대략적인 재생 시점
- Console 오류 또는 warning 전체 내용

---

## 8. DashState 적용 예시

현재 구현된 Dash는 **Handler 실행·완료 판정으로 종료하는 State**의 기준 사례다.

```text
IAgentDashInput.OnDashRequested
  → DashTransition: 요청 수신·CanStartDash 확인
  → typeof(DashState)
  → DashState.Enter: BeginDash·IsDash true
  → DashState.Execute: ExecuteDash
  → 거리 도달 또는 전방 벽 감지: Handler.IsCompleted true
  → 다음 Rule 평가에서 DashEndTransition: 목적 State 판정
  → AgentController.ChangeState
  → DashState.Exit: EndDash·IsDash false
  → GroundedState 또는 FallState.Enter
```

Handler가 OnExecute 중 완료를 표시하면 이후 Rule 평가에서 전이가 발생한다. ChangeState는 이전 DashState.Exit을 수행한 뒤 목적 State.Enter를 호출한다.

| 요소 | 현재 책임 |
| --- | --- |
| `DashTransition` | 시작 가능 여부 판단. 실제 Dash 시작은 하지 않음 |
| `DashState.OnEnter` | Handler 시작 및 Dash Animation Bool 설정 |
| `DashState.OnExecute` | Handler의 이동·완료 판정 실행 |
| `DashState.OnExit` | Handler 종료로 속도 초기화·중력 복구, Bool 해제 |
| `AgentDashHandler2D` | 방향 고정·X 이동·수직 운동/중력 정지·거리/벽 완료·공중 횟수 관리 |
| `DashEndTransition` | Handler 완료 검사 후 지면 상태에 따른 목적 State 반환 |
| `GroundDetector.OnGroundedChanged` | true 수신 시 Handler가 공중 Dash 횟수 초기화 |

Dash Animation End는 종료 조건이 아니다. DashHandler는 자체 Update·FixedUpdate 루프 없이 DashState에서 실행하며, 현재 거리 제한 속도 계산에는 `Time.fixedDeltaTime`을 사용한다. 새 행동은 이 시간 사용을 무조건 복사하지 말고 실제 물리 갱신과 실행 위치를 함께 확인한다.

현재 Handler의 데이터 타입은 `PlayerMotorData`다. 다른 Agent에 Dash를 추가할 경우 데이터·Detector·공중 횟수·종료 목적지가 맞는지 검토한다. 무적·스태미나·추적 돌진 등 미구현 정책을 현재 공용 기능으로 취급하지 않는다.

---

## 9. 다른 AI에게 전달할 요청과 완료 보고

### State 확장 요청 템플릿

```text
Docs/FSM/ProjectSkill/FSM_State_Extension_Guide.md를 읽고
현재 소스를 기준으로 아래 기존 Agent의 State를 확장해 주세요.

- 대상 Agent·Controller·Prefab: {이름과 경로}
- 추가 또는 확장 행동: {State와 행동 설명}
- 진입 가능한 State와 입력·조건: {목록}
- 수행 중 로직: {이동·시간·판정 등}
- 정상 종료 조건과 목적 State: {조건과 목적지}
- 피격·사망·다른 입력 중단 정책: {허용 범위와 우선순위}
- 데이터·모션: {사용할 에셋과 수치 / 임시값 허용 여부}
- Animator·Clip 작업: {AI 제작 / 사용자가 제작}
- 작업 방식: {계획만 / 구현·검증까지}
- 계획 문서·리팩토링 로그 갱신: {경로 / 제외}

기존 State·Transition·Handler·capability 재사용을 우선하고,
필요한 경우에만 추가해 주세요. 대상 FactoryData와 Factory를 통해
의존성과 전이를 연결하고, 범위 밖 Agent·사용자 변경은 보존해 주세요.
실제 확인한 결과와 수동 테스트가 필요한 항목을 구분해 주세요.
```

### 완료 보고

1. 대상 Agent와 추가·변경된 행동, 재사용·수정·추가 파일.
2. 진입·수행·종료·중단 흐름과 전이 우선순위.
3. 데이터·컴포넌트·Animator·Event 연결과 사용 방법.
4. 정적·Editor·Play 검증 결과 및 기존 Agent 회귀 영향.
5. 수동 테스트 순서, 조정할 수치, 남은 작업과 제외 범위.

이 문서는 재사용 지침이므로 체크리스트를 해당 작업 계획에 복사해 사용한다. 공용 가이드의 체크리스트를 개별 작업의 완료표로 갱신하지 않는다.
