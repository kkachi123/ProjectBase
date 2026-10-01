# StateMachineBehaviour 기반 FSM 전환 분석

## 1. 문서 목적

현재 Player FSM은 C# State와 `ITransitionRule`이 게임플레이 상태를 관리하고, Animator는 State가 설정한 parameter를 바탕으로 표현을 선택한다.

이 문서는 Unity `StateMachineBehaviour`(이하 SMB)를 중심으로 상태 구조를 다시 구성했을 때의 형태, 확장성과 유지보수 비용을 비교한다. 대상은 현재 Player 직접 입력 기반 FSM이며, Monster AI의 세부 구현은 예시 수준으로만 다룬다.

## 2. 현재 구조

```text
Input / Detector / Health / Animation Event
              │
              ▼
ITransitionRule ──► AgentStateBase ──► AgentController.ChangeState(Type)
                           │
                           ├─ Movement / Combat / Stamina / Handler
                           └─ IAgentAnimator parameter 설정
                                         │
                                         ▼
                                   Animator Controller
```

### 현재 책임

| 요소 | 현재 책임 |
| --- | --- |
| `AgentStateBase` | State Enter / Execute / Exit, 전이 Rule 구독·해제, 전이 요청 발행 |
| `ITransitionRule` | 입력, Ground, Health, Animation End 등 전이 조건 평가 |
| `AgentController` | 현재 State 보관, Type 기반 State 전환, Animation Event 수신·분배 |
| `PlayerStateFactory` | Player State와 전이 Rule에 필요한 의존성 조립 |
| `AttackState` / `PlayerAttackState` | 공격 진입, 공격 타입 적용, Combo 종료 시점 정책 |
| Animator | Blend Tree 및 Sub State Machine으로 시각적 표현과 Clip 간 전이 담당 |
| `AgentAnimationEventProxy` | 정확한 타격 Frame·Clip End를 코드 FSM에 전달 |

현재 공격 Combo는 특히 다음 원칙을 사용한다.

```text
공격 입력 → ComboAttackHandler에 다음 입력 1회 예약
현재 Clip End → PlayerAttackState.TryHandleAttackFinished()
  ├─ 다음 공격 가능: AttackState 유지 + 다음 AttackType 적용
  └─ 불가능: OnAnimationEnded → AttackEndTransition → Grounded/Fall
```

즉, **게임플레이 상태의 기준은 C# FSM**이고 Animator는 결과를 표현한다.

## 3. SMB를 전면 상태 머신으로 사용할 경우

SMB는 Animator Controller의 State 또는 Sub State Machine에 붙으며, `OnStateEnter`, `OnStateUpdate`, `OnStateExit` 같은 Animator 생명주기 callback을 제공한다.

전면 전환 시 구조는 아래처럼 바뀐다.

```text
Input / Detector / Health
          │
          ▼
Animator Parameter 설정
          │
          ▼
Animator Transition Condition
          │
          ▼
Animator State
  └─ StateMachineBehaviour
       ├─ OnStateEnter : 이동/전투 시작
       ├─ OnStateUpdate : 입력·Ground·타이머 판단
       └─ OnStateExit : 이동/전투 종료 및 다음 parameter 설정
```

### 클래스 구조 예시

```text
AgentController
  ├─ AgentMotor2D / AgentCombatHandler / Health 보유
  ├─ Animator parameter 입력만 수행
  └─ AnimatorStateContext 제공

Animator Controller
  ├─ GroundedBehaviour : 이동 처리
  ├─ JumpBehaviour : 점프 처리
  ├─ FallBehaviour : 낙하 처리
  ├─ PlayerAttackBehaviour : 공격·Combo 처리
  ├─ HitBehaviour : 피격 처리
  └─ DeathBehaviour : 사망 처리

AnimatorStateContext
  ├─ Controller / Motor / Combat / Input / Detector 접근 제공
  └─ AnimatorStateBehaviour가 필요한 capability를 조회하는 Bridge
```

이 방식에서는 현재의 `AgentStateBase`, `PlayerStateFactory`, 상당수 `ITransitionRule`이 없어지거나 Animator Transition 조건과 SMB 내부 조건문으로 흡수된다.

## 4. 구조 변경 비교

| 관점 | 현재 C# FSM | SMB 전면 전환 |
| --- | --- | --- |
| 상태의 기준 | C# `AgentStateBase` | Animator Controller State |
| 전이 조건 | `ITransitionRule` 객체 조합 | Animator Condition + SMB 내부 조건문 |
| State 생성·조립 | Factory의 명시적 의존성 주입 | Animator asset에 Behaviour 부착, 런타임 Context 탐색 |
| 전이 우선순위 | Factory 등록 순서로 코드 확인 가능 | Animator 전이 순서, Any State, Exit Time, parameter 조건을 함께 확인 필요 |
| 공격 프레임 | Animation Event → Controller → State | Animation Event 유지 또는 SMB의 normalized time 추적 |
| Combo 지속 | PlayerState가 End Event 결과로 종료 Event 발행 여부 결정 | Attack SMB가 Exit/다음 State 전이 여부를 결정 |
| 디버깅 위치 | State, Rule, Factory, Controller | Animator Graph, Behaviour, parameter 변화, Context 연결 |
| 데이터 재사용 | 같은 State/Rule을 다른 Factory에서 조합 가능 | Animator Controller 구조가 같은 경우에만 재사용이 쉬움 |

## 5. 새로운 Agent와 State 확장 관점

### 5.1 새로운 State: 예시 Dash

| 항목 | C# FSM | SMB 전면 전환 |
| --- | --- | --- |
| 구현 시작점 | `DashState`, `DashTransition`, Factory 등록 | Animator에 Dash State 추가, DashBehaviour 부착, parameter·전이 추가 |
| 게임 규칙 | State 생성자와 TransitionRule에 명시 | Behaviour 내부 또는 Animator Condition에 분산 |
| 우선순위 | Rule 등록 순서로 검토 | Any State, 전이 우선순위, Interruption Source, Exit Time을 함께 검토 |
| 애니메이션 없는 Dash | State만 구현하고 Animator 표현 생략 가능 | 별도 Animator State 또는 우회 규칙이 필요 |
| 테스트 | State/Rule 단위 테스트 및 Play Mode | Animator Controller까지 포함한 Play Mode 검증 비중이 큼 |

Dash처럼 이동 거리, 쿨다운, 무적, Stamina, Ground 조건이 늘어날 행동은 C# FSM이 확장 지점과 데이터 흐름을 더 명확히 보여준다.

### 5.2 새로운 Agent: Monster

| 상황 | C# FSM | SMB 전면 전환 |
| --- | --- | --- |
| 기본 Ground/Hit/Death Monster | 공용 State/Rule을 Factory에서 재사용하고 Monster 전용 의존성만 주입 | Animator Graph와 Behaviour를 Monster Controller마다 복제·조정해야 할 가능성이 큼 |
| Jump 없는 Monster | Factory에서 JumpState/Rule을 등록하지 않음 | Animator Graph에서 Jump 전이·Behaviour를 제거 또는 미사용 관리 |
| 공격 종류가 다른 Monster | AttackState 확장 또는 공격 정책 주입 | Animator Entry, Sub State Machine, Behaviour 분기까지 개별 조정 |
| AI 행동 선택 | Behavior Tree / Detector 결과를 Input·TransitionRule로 변환 | Behavior Tree가 Animator parameter를 직접 제어하게 되기 쉬움 |
| 비슷한 애니메이션을 공유하는 여러 Monster | State/Rule/Handler는 공유하고 Animator만 교체 가능 | SMB는 공유 가능하지만 Graph 구조와 parameter 이름의 호환성을 지속 보장해야 함 |

SMB 전면 전환은 **Animator Graph가 거의 동일한 캐릭터 군**에는 빠를 수 있다. 반면 현재 프로젝트처럼 Player와 Monster의 이동·공격·입력 정책이 달라질 예정이라면, Animator asset이 게임 규칙의 중심이 되는 비용이 빠르게 커진다.

## 6. 유지보수상 장단점

### SMB 전면 전환의 장점

- Animator State 진입/종료와 시각 효과, 사운드, 단순한 VFX를 같은 asset에서 확인할 수 있다.
- Clip 또는 Animator State와 정확히 1:1인 연출 로직은 찾기 쉽다.
- 단순 NPC가 동일한 Animator Graph를 공유한다면 Behaviour 재사용이 가능하다.
- `OnStateEnter`/`OnStateExit`는 Animation Event를 별도 Clip에 배치하지 않고도 상태 생명주기 신호를 제공한다.

### SMB 전면 전환의 비용과 위험

- State가 MonoBehaviour처럼 scene component가 아니므로 의존성 주입 경로가 불명확해진다. Controller, Input, Combat, Detector를 `GetComponent` 또는 별도 Context Bridge로 찾아야 한다.
- Animator Controller가 게임 규칙·전이 우선순위·표현을 동시에 보유한다. 코드 리뷰와 diff에서 조건 변경을 추적하기 어렵다.
- Any State, Exit Time, Interruption Source, Blend Tree 전이와 SMB callback 순서를 함께 고려해야 한다.
- Animator 전이 도중 `OnStateExit`가 호출될 수 있으므로, 공격 종료와 피격 취소를 같은 종료 callback으로 취급하면 잘못된 Stamina 소비·State 정리가 발생할 수 있다.
- State 없는 논리 행동, 서버 권위 로직, Headless 테스트, Animator가 없는 Monster에서 재사용성이 낮다.
- Player/Monster의 parameter 이름과 Graph 구조가 달라질수록 Behaviour가 구체 Animator 구현에 결합된다.

## 7. 권장안: C# FSM 유지 + SMB를 Animation Lifecycle Adapter로 한정

현재 프로젝트에는 SMB 전면 전환보다 하이브리드 구조를 권장한다.

```text
Input / Detector / Health
          │
          ▼
C# FSM (State + TransitionRule + Factory)  ← 게임플레이 상태의 기준
          │
          ▼
Animator parameter
          │
          ▼
Animator State + StateMachineBehaviour
          │
          ▼
AnimationLifecycleRelay
          │
          ├─ C# FSM에 진입/종료/중단 정보 전달
          └─ VFX / SFX / Camera 연출 호출
```

### SMB에 맡길 범위

- 특정 Animator State의 진입·종료 연출
- Footstep, VFX, SFX, Camera impulse 같은 presentation callback
- Animator State가 정상 종료됐는지, 다른 State에 의해 중단됐는지 구분해 Relay에 전달
- 필요할 경우 Blend Tree·Sub State Machine 진입의 관찰

### C# FSM에 유지할 범위

- Grounded / Jump / Fall / Attack / Hit / Death의 게임플레이 상태
- Input, Detector, Health, Stamina, Cooldown, 이동·전투 적용
- State 전이 우선순위와 취소 규칙
- Player Combo 예약과 다음 공격 가능 여부
- Player / Monster Factory 조립 및 capability 의존성 주입

### 현재 Animation Event와의 선택 기준

| 신호 | 권장 수단 | 이유 |
| --- | --- | --- |
| 공격 Hit Frame | Animation Event 유지 | Clip 내부의 정확한 프레임 시점이 중요 |
| 공격 Clip End | Animation Event 유지 또는 SMB Relay | Combo의 정확한 종료 frame이 중요하면 Event가 단순함 |
| Animator State 진입/이탈 연출 | SMB | State 단위 표현 생명주기에 적합 |
| 피격에 의한 중단 구분 | SMB Relay 보조 | 단순 Clip End Event만으로는 정상 종료와 중단 구분이 어려움 |

## 8. 하이브리드 도입 시 권장 클래스 구조

```text
AgentStateMachine (기존 AgentController + AgentStateBase)
  ├─ 게임플레이 State 관리
  └─ IAnimationLifecycleSource 구독

AnimationLifecycleRelay : MonoBehaviour
  ├─ Animator가 있는 GameObject 또는 부모에 배치
  ├─ StateMachineBehaviour에서 lifecycle signal 수신
  └─ OnStateEntered / OnStateExited / OnStateInterrupted 발행

AgentStateMachineBehaviour : StateMachineBehaviour
  ├─ Animator State ID / semantic event를 Relay로 전달
  └─ Controller, Combat, Input을 직접 조작하지 않음

PlayerAttackState
  ├─ Combo 정책·Stamina·AttackType 유지
  └─ 필요할 때 Relay 또는 기존 Animation Event를 구독
```

핵심은 SMB가 `PlayerController`나 `AgentCombatHandler`를 직접 찾고 조작하지 않는 것이다. SMB는 Animator의 생명주기를 **전달만** 하고, 실제 게임 규칙은 기존 State가 판단한다.

## 9. 도입하지 않는 경우에도 적용 가능한 개선

- `AnimEventType`을 Hit, End처럼 의미 단위로 유지하고, Animator clip 이름이나 hash를 Controller에 직접 노출하지 않는다.
- `AttackState.TryHandleAttackFinished()`처럼 Animation End 이후 정책이 필요한 경우 State의 virtual hook을 사용한다.
- 현재 Controller의 State별 Animation Event 분기는 장기적으로 `IAgentAnimationEventHandler` 같은 capability로 분리할 수 있다.
- SMB가 정말 필요한 연출부터 한 개 State에만 도입해, Animator callback 순서와 중단 케이스를 검증한다.

## 10. 최종 판단

| 판단 항목 | 결론 |
| --- | --- |
| SMB 전면 전환 | 권장하지 않음 |
| C# FSM 유지 | 권장 |
| SMB 제한적 도입 | 권장 — Animator State 단위 연출·중단 관찰용 Adapter |
| Attack Hit/Combo End의 즉시 교체 | 권장하지 않음 — 현재 Animation Event 기반 처리와 수동 검증을 우선 |
| Monster 확장성 | C# Factory + State/Rule 조합이 더 유리 |

현재 FSM은 Factory 주입, 공용 Rule, Player 전용 확장 State로 이미 게임 규칙의 경계를 만들고 있다. 이를 SMB 전면 구조로 바꾸면 코드 FSM의 단순성은 줄어들지만, 그 대가로 Animator Graph가 게임 규칙의 중심이 된다.

따라서 신규 Agent와 State가 계속 추가될 현재 단계에서는 **전면 전환보다 C# FSM을 유지하고, SMB는 표현 생명주기 Adapter로 제한하는 전략**이 유지보수와 확장성 면에서 더 적절하다.

## 11. 검증 체크리스트

- [ ] SMB를 도입할 특정 표현 요구가 Animation Event만으로 해결되지 않는지 확인
- [ ] 정상 Animator State Exit와 Hit/Death에 의한 중단 Exit를 구분할 필요가 있는지 확인
- [ ] `AnimationLifecycleRelay`가 Hero child Animator와 Player root Controller 사이에서 정상 참조되는지 확인
- [ ] 동일 SMB를 Player와 Monster가 공유할 때 parameter·Graph 구조 의존성이 없는지 확인
- [ ] Play Mode에서 Attack → Hit, Attack → Fall, Combo 지속·종료 순서가 기존 C# FSM과 동일한지 확인
