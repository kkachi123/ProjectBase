# Player Dash State 구현 계획

## 1. 목적과 범위

Player의 기존 `Dash` Input Action을 FSM의 별도 `DashState`로 연결한다.

이번 작업은 **State 추가, 전이, Animation 적용, 입력 이벤트 연결**을 우선한다. Dash 속도, 지속 시간, 쿨다운, 스태미나 비용, 무적 시간 등 수치와 게임플레이 튜닝 변수의 할당은 보류한다.

```text
이번 범위
  Dash 요청 → DashState 진입 → Dash Animation → End Event → Grounded/Fall 복귀

보류 범위
  Dash 이동 거리·속도·지속 시간·쿨다운·Stamina·무적·공중 Dash
```

## 2. 현재 구조와 제약

### 2.1 현재 이용 가능한 요소

- `PlayerInputCommands.inputactions`에는 `gamePlay/Dash` Button과 Left Shift 바인딩이 이미 존재한다.
- `PlayerInput`은 Jump/Attack과 같은 Input System callback 등록 패턴을 사용한다.
- `AgentStateBase`는 `IEventTransitionRule`을 State 진입 시 구독하고 종료 시 해제한다.
- `GroundedState`는 일반 지상 이동, `JumpState`/`FallState`는 공중 이동을 담당한다.
- `PlayerAnimator`는 capability interface와 parameter hash Dictionary를 통해 Player 전용 Animator parameter를 제어한다.
- `AgentAnimationEventProxy`는 Animation Event를 AgentController로 전달한다.

### 2.2 선행 확인 사항

현재 `AgentController.OnAnimationEvent()`는 Attack의 End Event에서 `AttackState.TryHandleAttackFinished()`를 먼저 호출한다. Player Combo가 이어지면 이 함수는 `true`를 반환하고 `OnAnimationEnded`를 발행하지 않는다. Combo가 끝났을 때만 `false`를 반환해 공용 `AttackEndTransition`이 종료를 처리한다.

Dash가 Animation End Event로 종료되려면 이 Attack 전용 정책을 유지한 채, Dash를 포함한 일반 종료 State에도 `OnAnimationEnded`를 전달하도록 분기해야 한다.

```text
OnAnimationEvent(OnFrame)
  └─ AttackState → PerformAttack()

OnAnimationEvent(End)
  ├─ AttackState → TryHandleAttackFinished()
  │    ├─ true  : 다음 Combo 시작, 종료 Event 미발행
  │    └─ false : OnAnimationEnded 발행
  ├─ DeathState → OnDeathFinished()
  └─ HitState / PlayerDashState 등
       → OnAnimationEnded 발행
```

`DashEndTransition`은 기존 `IAnimationEventSource.OnAnimationEnded`를 구독한다. DashState에서만 이 Rule이 활성화되므로 Attack·Hit의 End Event와 직접 간섭하지 않는다.

## 3. 설계 결정

### 3.1 Dash는 별도 State

`GroundedState`는 일반 이동/정지와 Move Blend Tree만 관리한다. Dash는 전용 Animation의 시작·종료와 향후 특수 이동 규칙을 가지므로 별도 `DashState`로 분리한다.

```text
GroundedState
  └─ OnDashRequested → DashState

DashState
  └─ Animation End → GroundedState 또는 FallState
```

이번 단계에서 DashState는 **전용 State와 Animation 수명만 확립**한다. Dash 속도는 설정하지 않으며, 상태 진입 시 이전 일반 이동 속도가 남지 않도록 수평 이동만 정지한다.

### 3.2 Player 전용 구현

초기 구현은 Player만 사용하므로 `PlayerDashState`를 만든다.

- 공용 식별 키: `typeof(DashState)`를 사용하지 않는다. 현재 Type-key 방식은 해당 Type의 실제 State 인스턴스가 필요하다.
- 권장: 신규 추상/공용 `DashState`를 만들지 않고 `typeof(PlayerDashState)`를 key·전이 목적지로 사용한다.
- 다른 Agent가 Dash를 실제로 필요로 할 때 공용 `DashState`와 `IDashExecution` 추출 여부를 판단한다.

이 선택은 현재 Player 전용 Dash를 위해 공용 추상화를 앞당기지 않는 원칙을 따른다.

### 3.3 입력 요청 계약

```csharp
public interface IAgentDashInput
{
    event Action OnDashRequested;
}
```

PlayerInput이 `IAgentDashInput`을 구현한다.

```text
gamePlay.Dash.performed
  → PlayerInput.OnDashRequested
  → DashTransition (GroundedState 활성 중에만 구독)
  → PlayerDashState
```

공중 State에는 DashTransition을 등록하지 않는다. 따라서 공중 입력은 버퍼에 남지 않고 즉시 폐기된다. 공중 Dash 지원은 보류한다.

### 3.4 Animator 계약

`IAgentAnimator.cs`에 `IDashAnimation` capability를 추가한다.

```csharp
public interface IDashAnimation
{
    void SetDash(bool value);
}
```

Player 전용 확장:

| 대상 | 변경 |
| --- | --- |
| `PlayerAnimationBoolType` | `Dash` 항목 추가 |
| `PlayerAnimationDataSO` | `IsDashBool` parameter 이름 추가 |
| `PlayerAnimator.Initialize()` | Dash hash 등록 |
| `PlayerAnimator` | `SetDash(bool)` 구현 |
| `Hero_Anim.controller` | `IsDash` Bool, Dash State/Clip, 진입·종료 전이, End Animation Event 추가 |

Animator 전이 규칙:

```text
Grounded → Dash      : IsDash == true, Has Exit Time 해제
Dash → Grounded/Fall : PlayerDashState Exit에서 IsDash == false
```

Animator는 Dash 종료를 스스로 판단하지 않는다. Dash clip의 End Animation Event가 FSM의 `DashEndTransition`을 깨우고, FSM이 Grounded/Fall 목적지를 선택한다.

## 4. 신규·수정 대상

| 분류 | 파일/에셋 | 변경 |
| --- | --- | --- |
| 수정 | `Agent/Input/IAgentInput.cs` | `IAgentDashInput` 추가 |
| 수정 | `Player/Input/PlayerInput.cs` | `IAgentDashInput` 구현, Dash callback·OnDashRequested 발행 |
| 참고 | `Player/Input/PlayerInputCommands.inputactions` | 이미 있는 Dash Action 사용. 필요한 경우 Input System에서 재생성 |
| 수정 | `Agent/StateControl/TransitionRules/ITransitionRule.cs` | 변경 없음. 기존 IEventTransitionRule 재사용 |
| 신규 | `Player/PlayerState/States/PlayerDashState.cs` | Dash Animator 진입/종료, 수평 이동 정지 |
| 신규 | `Agent/StateControl/TransitionRules/DashTransition.cs` | Dash 요청 Event 구독 및 PlayerDashState 전이 |
| 신규 | `Agent/StateControl/TransitionRules/DashEndTransition.cs` | Animation End Event 수신 후 Grounded/Fall 선택 |
| 수정 | `Player/PlayerState/PlayerStateFactory.cs` | PlayerDashState 등록, GroundedState에 DashTransition, DashState에 종료·피격 전이 등록 |
| 수정 | `Agent/@Hub/IAgentAnimator.cs` | IDashAnimation 추가 |
| 수정 | `Player/@Hub/PlayerAnimator.cs` | Dash parameter Dictionary/API 구현 |
| 수정 | `Player/SOData/PlayerAnimationDataSO.cs` | IsDash Bool 이름 추가 |
| 수정 | `Agent/@Hub/AgentController.cs` | Attack의 `TryHandleAttackFinished()` 우선 흐름은 유지하고, PlayerDashState의 End Event에는 `OnAnimationEnded`를 전달하도록 종료 routing 일반화 |
| 수정 | `Hero_Anim.controller` | Dash parameter, State, 전이, End Event 구성 |

`PlayerInputCommands.cs`는 자동 생성 파일이므로 직접 수정하지 않는다.

## 5. 전이 구성과 우선순위

GroundedState의 Rule 등록 순서는 아래를 기본으로 한다.

```text
1. DeathTransition          : 사망 최우선
2. GetHitTransition         : 피격이 Player 입력보다 우선
3. DashTransition           : Dash 요청
4. JumpTransition           : Jump 요청
5. AttackTransition         : Attack 요청
6. GroundedFallTransition   : 지면 이탈
```

단, DeathTransition의 구독 조건과 전 State 등록 방식은 별도 Agent 확장 계획의 선행 안정화 항목이다. Dash 작업 시 해당 문제가 해결되지 않았다면 기존 규칙과 충돌하지 않는 최소 등록만 하고, 사망 우선순위 변경은 함께 섞지 않는다.

DashState의 전이:

```text
1. GetHitTransition  → HitState
2. DashEndTransition → GroundedState 또는 FallState
```

Attack/Jump 입력은 DashState에 등록하지 않는다. 이번 단계의 Dash는 Animation 종료 전까지 입력으로 취소되지 않는다.

## 6. 구현 순서

### Phase 1 — 입력과 Animator capability

- [x] `IAgentDashInput`을 추가한다.
- [x] PlayerInput에 `OnDashRequested`와 Dash callback 등록을 추가한다.
- [x] PlayerAnimator capability, enum, SO parameter 이름을 추가한다.
- [ ] Hero Animator에 `IsDash` parameter와 Dash State를 구성한다.

완료 기준: Grounded 상태에서 Dash 입력이 이벤트로 발행되고, PlayerAnimator가 Dash parameter를 안전하게 설정할 수 있다.

### Phase 2 — Dash State와 전이

- [x] PlayerDashState를 만든다.
  - OnEnter: 일반 수평 이동 정지, `SetDash(true)`
  - OnExecute: 이번 단계에서는 속도·거리 처리 없음
  - OnExit: `SetDash(false)`
- [x] DashTransition을 Event Rule로 만든다.
- [x] DashEndTransition을 Event Rule로 만들고 GroundDetector로 복귀 목적지를 선택한다.
- [x] AgentController의 Animation End routing을 갱신한다.
  - AttackState의 `TryHandleAttackFinished()`가 `true`이면 종료 Event를 발행하지 않는다.
  - Attack Combo가 끝났을 때, HitState, PlayerDashState에서는 `OnAnimationEnded`를 발행한다.

완료 기준: DashState는 Player Input과 Animator End Event만으로 진입·종료된다.

### Phase 3 — Player Factory와 Animator 연결

- [x] PlayerStateFactory에 `typeof(PlayerDashState)` 키로 State를 등록한다.
- [x] GroundedState에 DashTransition을 등록한다.
- [x] PlayerDashState에 GetHitTransition과 DashEndTransition을 등록한다.
- [ ] Hero Animator의 Dash clip 마지막에 `AgentAnimationEventProxy.OnAnimationEnd` Event를 추가한다.
- [ ] Dash Animator 전이가 FSM parameter 설정과 같은 방향으로 동작하는지 확인한다.

완료 기준: `Grounded → PlayerDashState → Grounded/Fall`과 Animator 표현이 동일한 순서로 진행된다.

### Phase 4 — 검증

- [ ] 지상 Left Shift: GroundedState에서 PlayerDashState로 한 번만 전이
- [ ] Dash Animation 종료: GroundedState로 복귀
- [ ] Dash 중 지면 이탈: Animation 종료 후 FallState로 복귀
- [ ] Dash 중 Attack/Jump 입력: 상태 전이가 발생하지 않음
- [ ] Dash 중 피격: HitState가 Dash 종료보다 우선
- [ ] 공중 Left Shift: Dash 요청이 착지 후 재사용되지 않음
- [ ] Animator Console에 Dash parameter 누락 오류 없음

## 7. 보류 항목

### 현재 Editor 연결 보류

- [ ] Unity Pipeline 서버가 현재 도달 불가하므로 `Hero_Anim.controller`의 `IsDash` parameter, Dash State, 전이를 아직 생성하지 않았다.
- [x] Dash Clip은 `Assets/Prefabs/Player/Animations/Hero_Dash.anim`으로 확정했다.

Pipeline 재연결 후 Unity Editor에서 `Hero_Dash.anim`을 Dash State의 Motion으로 연결하고 마지막 프레임에 `AgentAnimationEventProxy.OnAnimationEnd` Event를 추가한다. 이후 Phase 3의 Animator 항목과 Phase 4 수동 검증을 진행한다. 실행 중인 Editor의 Animator YAML은 직접 수정하지 않는다.

다음은 이번 구현에 넣지 않는다.

- Dash 속도, 거리, 지속 시간, 쿨다운의 수치 또는 ScriptableObject 필드
- Dash 중 이동 처리 및 방향 고정 규칙
- Stamina 비용, 무적 프레임, 적 충돌/피해
- 공중 Dash, 다회 Dash, wall dash
- Monster Dash 공용화

위 항목은 State·전이·Animator 흐름이 검증된 후 별도 `DashExecution` 또는 `AgentDashHandler2D` 계획으로 확장한다.

## 8. 완료 기준

- Dash는 GroundedState의 일반 이동 코드에 분기문으로 추가되지 않는다.
- Dash 입력은 Event Transition을 통해 GroundedState에서만 수신된다.
- PlayerDashState는 Dash Animator parameter와 Animation End Event 수명을 소유한다.
- Animator 종료 Event가 FSM의 DashEndTransition을 통해 Grounded/Fall 전이를 결정한다.
- 수치 튜닝·이동 성능 구현 없이도 Dash의 State/Animator 계약을 Play Mode에서 검증할 수 있다.
