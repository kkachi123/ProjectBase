# Agent 기반 Monster FSM 확장 계획

## 1. 목적과 분석 범위

이 문서는 `Assets/Scripts/FSM/NPC/`를 제외한 현재 FSM 구조를 기준으로, `AgentController` 계열을 기반으로 하는 기본 Ground Monster 확장 방향을 정리한다.

- Grounded·Attack·Hit·Death를 수행하는 첫 Monster 구성
- Detector, Behavior Tree, FSM의 책임 경계
- 공용 Factory, Controller, Input, Animator, TransitionRule의 확장 우선순위

## 2. 현재 구조 분석

```text
AgentController
  ├─ Dictionary<Type, AgentStateBase> _states
  ├─ _currentState / ChangeState(Type)
  ├─ AgentAnimationEventProxy 수신
  └─ Health / Motor / Combat / MovementHandler 초기화

GroundedAgentController
  └─ GroundDetector 갱신

PlayerStateFactory
  ├─ GroundedState
  ├─ JumpState / FallState
  ├─ PlayerAttackState
  ├─ HitState / DeathState
  └─ TransitionRule 조합
```

- `AgentStateBase`는 진입 시 Event Rule을 구독하고 종료 시 일괄 해제한다.
- `ITransitionRule`은 등록 순서대로 평가되므로, Factory의 등록 순서가 전이 우선순위다.
- `GroundedState`는 일반 지상 이동과 정지를 함께 처리한다.
- `AttackState`, `HitState`, `DeathState`는 capability interface만 요구한다.
- Player는 `typeof(AttackState)` 키에 `PlayerAttackState`를 등록해 공용 식별자와 Player 전용 구현을 연결한다.

### 현재 공통 리스크

| 항목 | 현재 상태 | 확장 전 조치 |
| --- | --- | --- |
| 초기 State | `AgentController.Start()`가 항상 `GroundedState`로 시작 | Factory 또는 Controller가 초기 State를 제공하도록 변경해 비지상 Agent도 지원 |
| 공용 Factory | `AgentStateFactory`와 PlayerStateFactory의 조립 구조가 분리 | 공통 Grounded Factory 기반을 설계 |
| 단발 공격 시작 | `AttackTransition`은 요청 이벤트만 전이하고 `IAttackStarter.TryStartAttack()`을 호출하지 않음 | 기본 AttackTransition이 공격 시작 성공 시에만 전이하도록 보완 |
| 피격 구독 | `GetHitTransition`이 UniRx `IDisposable`을 저장·Dispose하지 않음 | 재진입 중복 구독을 막도록 수명 관리 |
| 사망 구독 | `DeathTransition.Subscribe()`의 null 조건이 반대여서 구독되지 않을 가능성 | 조건·해제 처리 수정, 생존 State 전체의 Death 우선순위 검토 |
| Animator Event | AgentController가 Attack/Hit/Death 타입을 직접 분기 | 특수 State 증가 전 Event 종료 전달을 일반화 |
| GroundedAgentController | `_jumpInput`을 보관하지만 Controller 차원에서 사용하지 않음 | Grounded 공통과 점프 가능 Agent 계층 분리 여부 결정 |

## 3. 기본 Ground Monster 설계

### 3.1 권장 상속과 구성

일반 지상 Monster는 raw `AgentController`가 아니라 `GroundedAgentController`를 상속한다. GroundedState, AttackEndTransition, 지면 이탈·착지 전이가 GroundDetector를 필요로 하기 때문이다.

```text
MonsterController : GroundedAgentController, IAttackStarter
  ├─ MonsterAnimator
  ├─ GroundMonsterStateFactory
  ├─ MonsterAIInput
  └─ TargetDetector / TargetContext
```

| 범주 | 요소 | 목적 |
| --- | --- | --- |
| 물리 | Rigidbody2D, AgentMotor2D, GroundDetector | 지상 이동·낙하·방향 처리 |
| 생존 | Health, AgentImpactHandler | 피격·넉백·사망 |
| 전투 | AgentCombatHandler, AttackData 목록 | AttackType 1의 범위·피해 적용 |
| 애니메이션 | MonsterAnimator, AgentAnimationEventProxy, MonsterAnimationDataSO | Grounded/Attack/Hit/Death parameter와 Event 전달 |
| AI 입력 | MonsterAIInput | 이동 벡터 설정, `OnAttackRequested` 발행 |
| 인식 | TargetDetector + TargetContext | Target 존재·거리·방향·시야·마지막 위치 |
| Factory | GroundMonsterStateFactory | 필요한 State/Transition만 조립 |

### 3.2 기본 State 구성

```text
GroundedState
  ├─ Move: MonsterAIInput.GetMovementInput()
  ├─ Attack 요청: AttackState
  ├─ 피격: HitState
  ├─ 사망: DeathState
  └─ 지면 이탈: FallState (낙하 가능한 Monster만)

AttackState
  ├─ Animation Frame: AgentCombatHandler.PerformAttack()
  ├─ End Event: AttackEndTransition
  ├─ 피격: HitState
  └─ 사망: DeathState

HitState
  ├─ End Event: GroundedState
  └─ 사망: DeathState

DeathState
  └─ 이동·공격 정지, 사망 표현 유지
```

점프하지 않는 Monster라도 낙하 가능한 맵에서는 FallState와 LandTransition이 필요하다. 평면 위에서만 동작하는 Monster라면 Factory가 Grounded/Fall 구성을 선택해야 한다.

### 3.3 단발 공격 시작 정책

기본 Monster는 다음 흐름을 사용한다.

```text
AttackTransition
  → OnAttackRequested 수신
  → IAttackStarter.TryStartAttack(1)
  → 성공: AttackState 전이
  → 실패: 현재 State 유지
```

첫 Monster는 MonsterController가 단발 AttackType 1을 시작하도록 구현할 수 있다. 두 번째 공격 계열을 추가하기 전에는 `IAttackStartPolicy` 또는 `AgentAttackStartHandler`로 시작 정책을 분리한다.

## 4. Monster AI 권장 구조

### 권장: Behavior Tree + Detector + FSM

```text
TargetDetector
  → TargetContext 갱신

Behavior Tree
  → Idle / Patrol / Chase / Attack 의도 선택

MonsterAIInput
  → SetMovement(Vector2)
  → RequestAttack() → OnAttackRequested

FSM
  → Grounded 이동, Attack / Hit / Death 실행
  → Motor / Animator / Combat 처리
```

Behavior Tree는 `AgentController.ChangeState()`를 직접 호출하지 않는다. BT Action은 목표와 입력만 설정하고, FSM의 TransitionRule이 실제 State 진입을 결정한다.

| 방식 | 장점 | 한계 | 권장 용도 |
| --- | --- | --- | --- |
| Behavior Tree만 | 순찰·추적·거리 판단 표현이 쉬움 | Hit/Death/공격 종료 같은 강제 상태와 물리 제어가 복잡 | 비권장 |
| Detector → Transition 직접 전이 | 단순하고 즉시 반응 | 정책·기억·쿨다운이 늘면 Rule 폭증 | 피격·사망·지면 이탈 등 반응성 전이 |
| BT가 State 직접 전환 | 빠른 구현 | AI와 FSM의 전이 책임 중복 | 비권장 |
| **BT + Detector + FSM** | 관측·의사결정·실행 분리 | Context/Input 계층 필요 | **권장** |

첫 Monster의 BT는 TargetContext를 읽어 `Patrol`, `Chase`, `RequestAttack`만 결정한다. Attack 요청은 공격 중·거리·cooldown 조건을 통과할 때만 발행해 event spam을 막는다.

## 5. 공통 클래스 확장 방향

| 대상 | 현재 한계 | 권장 보완 |
| --- | --- | --- |
| `AgentStateFactory` | 공통 State 조립을 재사용하지 못함 | `GroundedAgentStateFactoryData`와 공통 생성 메서드를 만들고 Player/Monster Factory가 확장 |
| `AgentController` | Grounded 시작 고정, Event routing 직접 분기 | 초기 State 제공과 Animation End 전달 일반화 |
| `AttackTransition` | 공격 시작 검증 없음 | `IAttackStarter`와 기본 AttackType을 받아 성공 시만 전이 |
| `GetHitTransition` / `DeathTransition` | 구독 수명·조건 결함 가능성 | `IDisposable` 관리, 조건 수정, Death 우선순위 표준화 |
| `AgentMotorData` | Player Header와 기본 이동 값 중심 | Agent 공용 명칭으로 정리, 행동별 데이터는 선택적으로 분리 |
| `AgentStatData` | maxCombo/stamina가 모든 Agent에 필수처럼 보임 | 공용 생존 데이터와 Player/Combo 전투 데이터를 분리하거나 policy화 |
| `GroundedAgentController` | jump input 보관 책임 불명확 | 점프 가능 Agent 계층을 별도로 둘지 결정 |

### Animator 원칙

```text
AgentAnimator
  └─ Hit / Death 공통 parameter와 hash 관리

PlayerAnimator : AgentAnimator
  └─ Player가 실제 사용하는 capability 구현

MonsterAnimator : AgentAnimator
  └─ Monster가 실제 사용하는 Grounded / Attack / 특수 행동 capability만 구현
```

- 점프하지 않는 Monster에는 `IAirborneAnimation`, Jump/Fall parameter를 추가하지 않는다.
- 공격 타입 선택이 필요 없는 Monster에는 AttackType parameter를 강제하지 않는다.
- State Factory는 해당 capability를 제공하는 Agent에만 State를 등록한다.

## 6. 적용 순서

### Phase 0 — 공통 안정화

- [ ] GetHitTransition의 UniRx 구독 수명과 DeathTransition의 조건을 수정한다.
- [ ] 생존 State 전체의 Death 우선순위 표를 확정한다.
- [ ] AttackTransition이 `IAttackStarter.TryStartAttack()` 성공 시에만 전이하도록 보완한다.
- [ ] AgentStateFactory와 PlayerStateFactory의 역할을 정리하고 공통 Grounded Factory 기반을 설계한다.

### Phase 1 — 첫 Ground Monster

- [ ] MonsterController, MonsterAnimator, MonsterAnimationDataSO를 만든다.
- [ ] MonsterAIInput으로 이동 벡터와 공격 요청 이벤트를 제공한다.
- [ ] GroundMonsterStateFactory로 Grounded·Attack·Hit·Death와 필요한 Fall을 조립한다.
- [ ] TargetDetector와 TargetContext를 만든다.
- [ ] MonsterController에 단발 AttackType 1 시작 정책을 연결한다.

### Phase 2 — Behavior Tree 연결

- [ ] BT가 TargetContext를 읽어 Patrol/Chase/Attack 의도를 선택하도록 구성한다.
- [ ] BT Action은 MonsterAIInput만 조작하고 Controller.ChangeState를 직접 호출하지 않는다.
- [ ] Attack 요청 spam 방지를 위해 공격 중·cooldown·거리 조건을 Action에 둔다.
- [ ] Hit/Death/Ground-Fall이 BT와 무관하게 FSM에서 우선 처리되는지 확인한다.

### Phase 3 — 검증

- [ ] Monster: Patrol → Chase → Attack → Grounded 복귀
- [ ] Monster: Hit/Death가 BT 의도와 무관하게 즉시 적용
- [ ] Monster: Animator parameter 누락 경고와 Event 구독 누수 없음
- [ ] Play Mode 반복 진입/종료 후 UniRx·Action 구독 중복 없음

## 7. 완료 기준

- 기본 Monster는 PlayerInput, PlayerAnimator, ComboAttackHandler에 의존하지 않는다.
- Behavior Tree는 전이를 직접 소유하지 않고 AI Input/Context로 FSM에 의도를 전달한다.
- Hit, Death, Ground/Fall 같은 강제 반응은 FSM TransitionRule이 우선 처리한다.
- 공통 Factory, 공격 시작, Event 구독 수명 문제가 첫 Monster 확장 전에 정리된다.
