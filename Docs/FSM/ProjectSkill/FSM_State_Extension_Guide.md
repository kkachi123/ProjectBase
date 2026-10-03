# FSM State 확장 구현 가이드

## 목적

이 문서는 ProjectRE FSM에 새 행동 State를 추가할 때 적용할 구현 규칙과 확인 절차를 정리한다.
다른 AI Agent 또는 개발자가 이 문서만 읽고도 기존 구조를 훼손하지 않고 State를 확장하는 것을 목표로 한다.

현재 FSM의 핵심 원칙은 다음과 같다.

> **FSM이 행동과 상태 전이를 결정하고, Animator는 FSM이 설정한 parameter를 표현한다.**

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
| `StateFactory` | State 인스턴스 생성, 의존성 주입, Transition 등록 순서 결정 |
| `AgentAnimator` / 전용 Animator | FSM 상태를 Animator parameter로 표현 |
| `AgentAnimationEventProxy` | Animation Event를 Controller/Event Source로 전달 |

State Dictionary는 `Type` 키를 사용한다.

```csharp
Dictionary<Type, AgentStateBase> states;
```

따라서 공용 기반 State를 확장하는 Agent는 **공용 기반 타입을 key와 전이 목적지로 유지**한다.

```csharp
states.Add(typeof(DashState), new PlayerDashState(...));
// DashTransition.NextStateType == typeof(DashState)
```

이 방식이면 공용 TransitionRule은 Agent별 파생 State를 알 필요가 없다.

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

---

## 3. 책임 경계 규칙

### 3.1 기존 State의 책임을 침범하지 않는다

새 행동은 기존 State 내부의 `if` 분기로 추가하지 않는다. 행동이 독립적인 진입·수행·종료 수명을 가지면 별도 State로 만든다.

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

공용 State는 모든 Agent가 공유할 최소 규칙만 가진다. 특정 Agent의 입력, Animator parameter, 스탯, 조작 방식에 직접 의존하지 않는다.

```text
DashState
 ├─ 공통: Dash Animation 수명, 공통 종료 Event, 기존 이동 정리
 ├─ PlayerDashState : DashState
 │   └─ 방향 고정, 거리, 스태미나, 무적 등 Player 규칙
 └─ MonsterDashState : DashState
     └─ 돌진 대상, 충돌 피해, AI 거리 판정 등 Monster 규칙
```

상속보다 Handler/인터페이스 주입이 나은 경우도 있다.

- 행동의 State 수명은 같고 실행 정책만 다르다: `IDashExecution` 같은 전략 Handler 주입을 우선 검토한다.
- 진입·종료·전이까지 크게 다르다: Agent별 파생 State를 사용한다.

---

## 4. 구현 순서

### Step 1 — 공용 계약 추가

필요한 입력 또는 기능만 작은 인터페이스로 추가한다.

```csharp
public interface IAgentDashInput
{
    event Action OnDashRequested;
}
```

- Input 구현체는 Event를 발행한다.
- State는 Input 구현체를 직접 참조하지 않는다.
- Event는 현재 State가 활성일 때만 `IEventTransitionRule.Subscribe()`로 구독한다.

### Step 2 — State 구현

새 State는 `AgentStateBase`를 상속한다.

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

### Step 3 — TransitionRule 구현

입력/Animation Event 기반 Rule은 `IEventTransitionRule`을 구현한다.

```csharp
public class XTransition : IEventTransitionRule
{
    public Type NextStateType => typeof(XState);

    public void Subscribe() { /* Event += Handler */ }
    public void Unsubscribe() { /* Event -= Handler, flag reset */ }
    public bool ShouldTransition(float deltaTime) { /* flag 검사 */ }
}
```

규칙:

- `Subscribe()`는 중복 구독을 막는다.
- `Unsubscribe()`에서 반드시 구독 해제와 전이 flag 초기화를 수행한다.
- Stamina 등 행동 불가 조건은 전이 flag를 소비하기 전에 검사하거나, 조건이 충족될 때만 true를 반환한다.
- `NextStateType`은 Agent별 파생 타입이 아니라 공용 기반 타입을 가리킨다.

### Step 4 — Factory 등록 및 우선순위 설정

```csharp
states.Add(typeof(XState), new XState(...));

states[typeof(SourceState)].AddTransition(new HitTransition(...));
states[typeof(SourceState)].AddTransition(new XTransition(...));
states[typeof(SourceState)].AddTransition(new NormalTransition(...));
```

`AgentStateBase`는 등록된 순서대로 Rule을 평가한다. 즉, **AddTransition 순서가 우선순위**다.

권장 판단 순서:

1. Death / Hit 등 강제 행동
2. 즉시 입력 행동(Dash 등)
3. 일반 물리 전이(낙하, 착지)
4. 일반 행동 입력

실제 우선순위는 기획 의도에 맞춰 결정하며, 기존 순서를 무심코 변경하지 않는다.

### Step 5 — Animator 계약 연결

1. `IAgentAnimator.cs`에 필요한 capability를 추가한다.
2. Agent 공용 parameter와 Agent별 parameter를 분리한다.
3. 전용 Animator에 parameter hash Dictionary와 API를 구현한다.
4. State의 `OnEnter`/`OnExit`에서 Animator API만 호출한다.
5. Animation 종료가 State 종료 조건이면 Clip에 `AgentAnimationEventProxy.OnAnimationEnd`를 연결하고 EndTransition으로 받는다.

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

### 상태 Bool 수명

FSM State가 Animator 상태 Bool의 소유자다.

```text
Jump/Fall Exit → IsJump = false, IsFall = false
Grounded Enter → IsGrounded = true
Attack Enter   → IsAttack = true
Dash Enter     → IsDash = true
```

상태 전환 시 이전 State의 Bool을 해제하지 않으면 서로 다른 Animator 전이가 동시에 충족될 수 있다.

---

## 6. 구현 후 확인 체크리스트

### 코드 및 FSM

- [ ] 새 State가 기존 State의 일반 동작을 직접 처리하지 않는다.
- [ ] 필요한 Input/Handler/Animator 의존성이 Factory에서 주입된다.
- [ ] `typeof(State)` key, `TransitionRule.NextStateType`, Factory 등록 타입이 일치한다.
- [ ] Event Rule이 중복 구독되지 않으며 State Exit에서 해제된다.
- [ ] 전이 flag, Trigger, 행동 예약 값이 State Exit에서 적절히 초기화된다.
- [ ] Transition 등록 순서가 의도한 우선순위와 일치한다.
- [ ] 공중/지상/피격/사망 등 경계 State에서 전이 허용 범위가 명확하다.

### Animator

- [ ] parameter 이름과 Animator API/SO 데이터 이름이 정확히 일치한다.
- [ ] 새 State의 진입·종료 조건이 FSM parameter 수명과 일치한다.
- [ ] 공통 출구는 상위 State Machine으로 집중되어 중복 전이가 없다.
- [ ] Clip 종료가 필요하면 Animation Event가 정확한 마지막 프레임에 있다.
- [ ] 기본 parameter 값이 초기 State와 충돌하지 않는다. 예: Grounded 시작 시 `IsFall` 기본값은 false.

### Unity 검증

- [ ] Unity 재컴파일 후 새 State/Transition 관련 compile error가 없다.
- [ ] Console에 Animator transition 무시 warning이 없다.
- [ ] FSM debug log와 실제 Animator State가 같은 순서로 바뀐다.

---

## 7. 역할 분담

### AI Agent가 담당할 작업

- 현재 FSM 및 유사 State 분석
- State 책임·공용화·상속/Handler 분리 설계
- C# State, TransitionRule, Factory, DI, Animator interface 구현
- Type-key 등록과 전이 우선순위 점검
- Animation Event routing과 Event 구독 수명 검증
- Unity Editor의 compile/console/Animator 연결 상태 확인
- 계획 문서와 리팩토링 로그 최신화

### 사용자가 담당할 수동 작업

- Animation Clip 제작 및 선택
- Animator parameter 생성과 기본값 설정
- Animator State/Sub-State Machine 배치
- Animator Transition 조건, Exit Time, Duration, 우선순위 조정
- Clip의 Animation Event 프레임 배치
- Play Mode 조작감, 타이밍, 시각 표현 검증
- 거리·속도·쿨다운·스태미나·무적·캔슬 규칙 등 게임플레이 결정

### 수동 테스트 결과로 전달할 정보

- FSM State와 Animator State가 불일치한 시점
- 재현 입력 순서와 행동 상태
- 문제가 나는 Clip/Transition 및 대략적인 재생 시점
- Console 오류 또는 warning 전체 내용

---

## 8. DashState 적용 예시

현재 Dash는 이 가이드의 기준 사례다.

```text
IAgentDashInput.OnDashRequested
  → DashTransition
  → typeof(DashState)
  → DashState
  → Dash Animation End
  → DashEndTransition
  → GroundedState 또는 FallState
```

현재 `DashState`는 공통 Animation 수명과 수평 속도 정지만 담당한다. 실제 Dash 거리, 속도, 방향 고정, 무적, 다회 사용 등의 실행 정책은 추후 Agent별 파생 State 또는 실행 Handler로 확장한다.
