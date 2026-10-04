# Agent_Extenstion_Monster

## 1. 목적과 범위

현재 `AgentController` 기반 FSM을 확장하여 **기본 단발 공격 몬스터의 공용 실행 구조**를 만든다. 첫 제작 대상은 Goblin이며, 같은 행동을 사용하는 다른 몬스터는 공용 코드와 Animator Controller를 유지하고 몬스터별 에셋을 교체하여 제작한다.

이 문서는 다른 AI Agent가 현재 코드와 함께 읽고, 구현·에셋 연결·검증을 순서대로 수행할 수 있는 계획서다. 문서 작성 시점에는 구현하지 않은 상태다.

### 확정 범위

- 프로젝트: `E:/Unity/Project/ProjectBase`.
- 최초 대상 에셋: `Assets/@tempAssets/@Monster/Monsters_Creatures_Fantasy/Goblin/`.
- 기본 FSM State: `Grounded`, `Attack`, `Hit`, `Death`.
- 기본 공격: `AttackType = 1`, 요청 한 번당 단발 공격 한 번.
- `AgentController`, 공용 State·Handler·Input 계약·Factory 구조 활용.
- `MonsterAnimator : AgentAnimator`로 몬스터가 사용하는 기능만 구현.
- 공용 Animator Controller + 몬스터별 Animator Override Controller 사용.
- 공용 Monster 에셋과 완성 prefab은 `Assets/Prefabs/Monster/` 기준으로 배치. Goblin 전용 Sprite·Animation·Override·ScriptableObject 에셋은 `Assets/Prefabs/Monster/Goblin/`에 배치.
- Monster 스크립트는 `Assets/Scripts/FSM/NPC/AIMonstor/` 아래 역할별 폴더에 배치. 공용 Agent 기반 코드는 기존 Agent 경로 유지.
- 기존 Orc prefab은 완전히 삭제하고 신규 Monster prefab을 처음부터 제작. Orc의 계층·컴포넌트·에셋을 재사용하거나 이관하지 않음.
- 불필요한 기존 코드는 참조 정리 후 삭제 가능. 기존 구현을 보존하기 위한 별도 추상화는 추가하지 않음.
- C# namespace는 `ProjectRE`. 주석은 필요한 곳에 `<summary>`를 사용하고 단답형으로 작성.

### 이번 범위에서 제외

- Behavior Graph, Brain, Action 노드의 신규 설계·구현.
- 순찰, 추적, 타깃 선정, 거리 기반 자동 공격, 공격 쿨다운 등의 의사결정.
- Player의 콤보·스태미나·점프·Dash 기능 이식.
- Jump/Fall State, 공중 공격, 낭떠러지 회피.
- 넉백·무적·드롭·풀링·리스폰·보상 처리.
- 새로운 몬스터 종류마다 별도의 Controller·Factory·Animator 코드 생성.

> 이번 결과물은 행동을 **실행할 수 있는 몬스터**다. 스스로 행동을 선택하는 AI는 아직 없다. 검증에서는 외부 명령으로 이동·공격 요청을 전달한다.

## 2. 실제 코드베이스 점검 결과

### 2.1 그대로 활용할 기반

| 현재 요소 | 확인한 구조 | 이번 활용 |
| --- | --- | --- |
| `AgentController` | 공통 컴포넌트 초기화, `Dictionary<Type, AgentStateBase>`, Animation Event 전달 | Monster 실행 기반으로 유지 |
| `AgentStateBase` | Enter에서 Event Rule 구독, Exit에서 해제, Execute에서 전이 순차 평가 | State 수명·전이 체계 유지 |
| `AgentStateFactory<TData>` | 공통 Hit/Death 생성 → Agent별 추가·교체 → 전이 연결 | Monster Factory의 부모로 사용 |
| `StateFactoryData` | Animator·Motor·Input·Handler·Health·Event Source 주입 | Monster 전용 Data가 상속 |
| `GroundedState` | `IGroundedAnimation`, 이동 Handler, 이동 Input 사용 | Idle/Move 통합 상태로 재사용 |
| `AttackState` | 추상 클래스, 공격 타입·Animator 적용 및 Exit 초기화 | 단발 공격용 파생 State 등록 |
| `HitState` / `DeathState` | Hit/Death capability만 요구 | 재사용 또는 필요한 부분만 확장 |
| `AttackTransition` | 공격 요청 후 `IAttackStarter.TryStartAttack()` 성공 시 전이 | Player와 동일한 공용 진입 Rule 사용 |
| `AgentAnimationEventProxy` | OnFrame·End를 Controller로 전달 | 동일한 이벤트 경로 사용 |
| `AgentAnimator` | Hit/Death, 공용 parameter hash 등록·설정 함수 | 공통 책임 그대로 유지 |

`AgentController.Start()`의 초기 State는 현재 `typeof(GroundedState)`다. 이번 네 상태 구성에는 맞으므로 초기 상태 선택 구조까지 변경하지 않는다.

### 2.2 기존 AIMonstor 구현의 차이와 선행 문제

| 파일·요소 | 현재 상태 | 필요한 조치 |
| --- | --- | --- |
| `AIMonstor/@Hub/MonsterController.cs` | Animator 초기화·공격 시작 판정이 없고 Factory에 일부 의존성만 전달 | 새 기본 몬스터 구조로 재작성 |
| `AIMonstor/MonsterState/MonsterStateFactory.cs` | 부모 Factory 미상속, Attack 등록 주석 처리, Transition 연결 없음 | 상속형 Factory와 네 상태 전이 구성 |
| 위 Factory의 `(IGroundedAnimation)data.Animator` | 현재 `AgentAnimator`는 `IGroundedAnimation` 미구현 | `MonsterAnimator`를 명시적으로 주입 |
| `AIMonstor/Input/AIMonsterInput.cs` | 이동 벡터와 공격 이벤트는 제공하지만 `Attack(int)`의 숫자는 공격 선택에 사용되지 않음 | 방향 명령·단발 공격 요청 API로 단순화 |
| `AttackEndTransition` | 지면 상태에 따라 Grounded/Fall 선택 | 네 상태 몬스터용 고정 복귀 지원 필요 |
| `GetHitTransition` | UniRx Subscribe 반환값 미보관, Unsubscribe에서 Dispose하지 않음 | 활성 State의 구독 수명 보장 |
| `DeathTransition` | `if (IsDead != null) return;`로 정상 의존성의 구독 차단, Pairwise로 이미 사망한 값도 놓칠 수 있음 | 현재 사망 값을 직접 판정하는 Rule로 정리 |

공용 Transition 수정은 Player에도 영향을 준다. Monster 검증과 별도로 Player 회귀 검증을 수행한다. **공용 전이의 기존 문제를 고치는 것과 Player의 전이 순서를 바꾸는 것은 구분한다.**

### 2.3 기존 Orc 삭제 범위

`Assets/Prefabs/Monster/Orc/OrcAI.prefab`은 삭제 대상으로 확정한다. 기존 Orc와 신규 Monster의 호환성·GUID 유지·컴포넌트 이관은 설계하지 않는다.

확인된 기존 prefab 참조는 다음과 같다.

- `Assets/Scenes/SampleScene.unity`.
- `Assets/Prefabs/@Data/Map/GridChunkSettings/GridMapGenerationSettings1.asset`.

이 참조는 기존 Orc를 유지하기 위한 조건이 아니라 **삭제 후 Missing Reference를 남기지 않기 위한 정리 대상**이다. 해당 Scene의 기존 Orc 인스턴스와 맵 설정의 Orc 등록 항목을 제거한다. 신규 Goblin을 같은 사용처에 자동으로 치환하지 않으며, 배치는 신규 제작·검증 단계에서 별도로 처리한다.

기존 `Assets/Prefabs/Monster/` 전체를 점검하여 신규 구조에서 필요 없어진 파일은 참조 정리 후 삭제한다. Orc 전용 Clip·데이터와 기존 Monster Controller도 이 정리 대상에 포함한다. 다른 대상이 사용 중인 에셋과 원본 아트 패키지는 함께 삭제하지 않는다. 이번 문서 작성 단계에서는 실제 prefab·코드·에셋을 삭제하지 않는다.

기존 `Docs/FSM/Agent_Extension_Plan.md`에는 Behavior 설계와 이전 Factory·Attack 계약이 포함되어 있다. 이번 구현에서는 **현재 C# 코드와 이 문서의 범위를 우선**하며, 해당 문서의 Behavior 작업을 함께 수행하지 않는다.

## 3. 목표 구조와 책임

```text
외부 명령 / 검증 도구
  └─ MonsterInput
       ├─ SetMovement(Vector2) → IAgentMovementInput
       └─ RequestAttack()     → IAgentCombatInput.OnAttackRequested

MonsterController : AgentController
  ├─ MonsterAnimator : AgentAnimator
  ├─ MonsterStateFactory : AgentStateFactory<MonsterStateFactoryData>
  ├─ GroundedState
  ├─ MonsterAttackState : AttackState
  ├─ MonsterHitState : HitState
  └─ DeathState

공용 Animator Controller
  └─ 몬스터별 Animator Override Controller
       └─ Idle / Move / Attack / Hit / Death Clip 교체
```

### 3.1 Controller

`MonsterController`는 이번에는 `AgentController`를 직접 상속한다.

- 공통 초기화는 `base.Awake()`를 통해 수행.
- MonsterAnimator 조회·Initialize 후 Factory에 주입.
- Factory에 Motor, MovementHandler, MovementInput, CombatHandler, CombatInput, Health, AnimationEventSource, AttackStarter 전달.
- `FixedUpdate()`는 의도적으로 빈 구현. 별도의 Behavior나 Detector 갱신을 넣지 않음.
- `TryStartAttack()`은 생존 여부와 공격 데이터 1번의 유효성을 검사한 후 `CombatHandler.ApplyAttackType(1)` 적용.
- 공격 Animation·피해 적용·State 전환은 여기서 직접 실행하지 않음.
- `OnDeathFinished()`는 사망 Clip 종료 후 해당 몬스터 오브젝트 제거. 보상·풀링은 별도 작업.
- 필수 데이터·Animator·Proxy·Input 누락은 초기화 단계에서 대상 오브젝트와 누락 항목을 명확하게 보고.

`GroundedState`는 현재 지면 감지 자체가 아니라 기본 이동·정지의 실행 상태다. 네 상태 프로필에서는 지면 이탈을 별도 State로 표현하지 않는다. 첫 검증은 평지에서 수행하며, 낙하·착지 표현이 필요한 몬스터는 추후 별도 프로필로 확장한다.

### 3.2 Input

`MonsterInput`은 `IAgentMovementInput`, `IAgentCombatInput`만 구현한다.

| API·멤버 | 역할 |
| --- | --- |
| private 이동 벡터 | 마지막 외부 이동 명령 보관 |
| `GetMovementInput()` | 현재 이동 명령 조회 |
| `SetMovement(Vector2)` | x축 이동 의도 설정. y 입력은 0으로 제한 |
| `RequestAttack()` | `OnAttackRequested` 이벤트 한 번 발행 |
| `OnAttackRequested` | 공용 AttackTransition이 구독하는 공격 입력 계약 |

- 입력 컴포넌트는 AgentController·State·Animator·CombatHandler를 참조하지 않음.
- 공격 타입, 공격 가능 여부, 자동 반복, 입력 버퍼, 콤보, 타깃 판단을 넣지 않음.
- 공격 중에는 AttackTransition이 구독되지 않으므로 추가 요청을 예약하지 않음.
- 이동 명령은 외부에서 0을 보내기 전까지 유지. Attack/Hit 동안 실행만 중단되고 Grounded 복귀 후 마지막 명령을 다시 사용.
- 일반 입력값에 UniRx를 새로 도입하지 않음. 공격은 기존 Action 계약 그대로 사용.

현재 `AIMonsterInput`을 이 이름·API로 정리하는 경우 기존 Behavior 코드의 `Move`·`Attack` 호출과 타입 참조를 먼저 정리한다. 호환용 함수·어댑터를 무조건 영구 추가하지 않는다.

### 3.3 State와 이동 정책

| Dictionary key | 등록 인스턴스 | 책임 |
| --- | --- | --- |
| `typeof(GroundedState)` | `GroundedState` | 이동 명령 적용, Idle/Move 표현 |
| `typeof(AttackState)` | `MonsterAttackState` | 공격 진입 시 수평 속도 정지, 공용 단발 공격 수명 |
| `typeof(HitState)` | `MonsterHitState` | 피격 진입 시 잔여 수평 이동 정지, 공용 피격 표현 |
| `typeof(DeathState)` | `DeathState` | 공격 타입 초기화, 이동 정지, 사망 표현 |

`AttackState`는 현재 추상 클래스이므로 `new AttackState(...)`로 생성할 수 없다. 파생 State를 등록하되 공용 Type key를 유지한다.

```csharp
states.Add(typeof(AttackState), new MonsterAttackState(...));
states[typeof(HitState)] = new MonsterHitState(...);
```

- MonsterAttackState는 `ICombatAnimation`, AgentCombatHandler, AgentMotor2D 주입.
- OnEnter에서 공용 `base.OnEnter()` 호출과 `StopHorizontal()` 수행.
- 공용 OnExit의 공격 타입·Animator 초기화를 유지.
- MonsterHitState는 공용 피격 진입 후 `StopHorizontal()` 수행.
- MonsterHitState는 부모 Factory가 만든 HitState를 **전이 연결 전에 교체**.
- Attack/Hit의 빈 OnExecute를 불필요하게 재구현하지 않음.
- 첫 몬스터에는 AgentImpactHandler를 연결하지 않음. 향후 넉백을 추가하면 Hit 진입의 속도 정지와 넉백 적용 순서를 별도로 설계.

새 State를 추가하는 것이 아니라 같은 네 상태의 실행 정책을 확장하는 구조다. 다른 기본 공격 몬스터도 이 State 인스턴스 구성을 그대로 사용한다.

### 3.4 Factory와 의존성 주입

```text
MonsterStateFactoryData : StateFactoryData
  └─ MonsterAnimator 접근·주입 속성

MonsterStateFactory : AgentStateFactory<MonsterStateFactoryData>
  ├─ AddCommonStates        : 부모의 Hit / Death 생성
  ├─ AddAgentStates         : Grounded / MonsterAttack 추가, Hit 교체
  └─ ConfigureTransitions  : 최종 객체에 전이 연결
```

MonsterAnimator 속성은 Player의 현재 방식처럼 부모 `Animator` 참조와 연결한다. Hit/Death가 받는 AgentAnimator와 전용 State가 받는 MonsterAnimator는 **동일한 객체**다.

Factory에는 Behavior, Unity 오브젝트 탐색, 타깃 선택, 입력 발생, Animator Clip 제작 코드를 넣지 않는다. 상태 인스턴스 생성과 의존성 주입, 전이 조립만 담당한다.

## 4. 전이 구성과 공용 Rule 정리

### 4.1 Monster 전이 순서

| 현재 상태 | AddTransition 등록 순서 | 결과 |
| --- | --- | --- |
| Grounded | Death → GetHit → Attack | 사망·피격이 일반 공격보다 우선 |
| Attack | Death → GetHit → AttackEnd | 피격·사망으로 공격 중단 가능 |
| Hit | Death → GetHitEnd | 피격 중 사망 또는 모션 종료 후 복귀 |
| Death | 없음 | 종료 이벤트까지 유지 |

DeathTransition을 Hit에만 넣지 않는다. 어떤 생존 상태에서든 치명타를 받으면 다음 FSM 평가에서 바로 Death로 전환한다.

각 State의 이벤트 기반 Rule은 별도 인스턴스로 생성한다. 특히 GetHitTransition 객체를 여러 State가 공유하지 않는다.

### 4.2 GetHitTransition

- Subscribe 반환 `IDisposable` 보관.
- 중복 Subscribe 방지.
- Unsubscribe에서 해당 구독 Dispose 후 참조와 전이 flag 초기화.
- 다른 소비자의 Health 구독이나 ReactiveProperty 자체는 Dispose하지 않음.
- State 재진입 시 이전 피격 flag가 남지 않는지 확인.

### 4.3 DeathTransition

현재 사망 여부는 지속되는 상태값이므로 별도 이벤트 flag가 필요하지 않다.

- `ITransitionRule`로 구현.
- 기존 생성자의 `IReadOnlyReactiveProperty<bool>` 의존성 유지.
- `ShouldTransition()`에서 현재 `IsDead.Value` 검사.
- 불필요한 Pairwise, TriggerTransition, Subscribe/Unsubscribe, 예약 flag 제거.
- 이미 사망한 상태에서 Rule이 평가되어도 true 반환.
- 기존 Player Factory의 생성자 호출은 유지. Player의 Rule 등록 순서는 이번에 임의 변경하지 않음.

### 4.4 AttackEndTransition

Monster에는 FallState가 없으므로 기존 지면 의존형 생성자를 그대로 연결하면 안 된다.

다음 두 사용 방식을 지원하도록 기존 Rule을 최소 확장한다.

```csharp
// 기존 Player: 실제 지면 상태에 따른 복귀.
new AttackEndTransition(eventSource, groundDetector);

// 기본 Monster: 등록된 상태로 고정 복귀.
new AttackEndTransition(eventSource, typeof(GroundedState));
```

- 기존 GroundDetector 생성자와 Player 동작 유지.
- 고정 복귀 생성자는 명시적인 Type을 받아 반환.
- 공통 Animation End 구독·해제 로직 재사용.
- 입력이나 다음 공격 실행 로직을 종료 Rule에 넣지 않음.
- 누락된 GroundDetector를 고정 복귀로 조용히 처리하는 null fallback은 사용하지 않음.
- Factory 검증에서 모든 NextStateType이 등록된 key인지 확인.

GetHitEndTransition은 현재 Grounded 고정 복귀이므로 그대로 활용한다.

### 4.5 오브젝트 제거 시 구독 정리

현재 AgentController에는 현재 State를 종료하는 파괴 시점 정리가 없다. 몬스터가 Animation End 이외의 이유로 제거되어도 구독이 남지 않도록 다음 수명을 정리한다.

- Controller 파괴 시 현재 State Exit 수행.
- 등록된 State의 OnTransition 연결 해제.
- 파생 Controller가 종료 처리를 확장할 수 있도록 정리 메서드의 상속 관계 유지.
- 이미 종료한 State를 반복 Exit하지 않도록 현재 State 참조 수명 정리.

풀링용 Disable/Enable 재개 정책은 이번 범위에서 설계하지 않는다.

## 5. MonsterAnimator와 데이터 분리

```text
AgentAnimator
  └─ IHitAnimation / IDeathAnimation
     공통 Dictionary, parameter 등록·설정 함수

MonsterAnimator : AgentAnimator
  └─ IGroundedAnimation / ICombatAnimation
     Monster 전용 Dictionary와 기능별 API

MonsterAnimationDataSO : AgentAnimationDataSO
  └─ Grounded / Attack / MoveSpeed / AttackType 이름
```

| parameter | 타입·기본값 | 소유·역할 |
| --- | --- | --- |
| IsHit | Bool / false | Agent 공통 피격 표현 |
| IsDeath | Bool / false | Agent 공통 사망 표현 |
| IsGrounded | Bool / false | GroundedState 진입·종료. 실제 센서 값과는 별개 |
| IsAttack | Bool / false | AttackState 진입·종료 |
| MoveSpeed | Float / 0 | Grounded Idle/Move Blend Tree 입력 크기 |
| AttackType | Int / 0 | 공용 ICombatAnimation 계약. 이번 공격은 1 |

- 모든 애니메이션 인터페이스 선언은 기존 `IAgentAnimator.cs`에 유지.
- 별도 IMonsterAnimator 또는 추가 공격 시작 인터페이스 생성 금지.
- MonsterAnimator에는 Jump, Fall, Dash, Combo interface·Dictionary를 넣지 않음.
- 전용 SetGrounded·SetMoveSpeed·SetAttack·SetAttackType은 부모의 공용 generic 설정 함수 활용.
- AgentAnimator에 Monster 전용 parameter를 다시 합치지 않음.
- 공통·전용 parameter 모두 Initialize 이후 사용.
- SO에는 변수 역할을 단답형 주석으로 기재.
- 유효 parameter의 이름과 타입이 실제 Controller와 일치하는지 초기 설정 시 검증.

현재 단발 Attack Animation은 하나이므로 AttackType 분기용 Blend Tree나 공격 Sub-State Machine은 만들지 않는다. AttackType parameter는 공용 공격 계약과 일치시키되 값이 다른 공격을 자동 실행하지 않는다.

## 6. 공용 Animator와 Override 계약

### 6.1 공용 Controller

신규 `Assets/Prefabs/Monster/Animations/BasicMonster_Anim.controller`를 처음부터 만든다. 기존 `Monster_Anim.controller`의 IsIdle/IsMove·다중 Orc 공격 구성은 이관하지 않는다. 기존 Controller는 사용 참조를 정리한 뒤 불필요하면 삭제한다.

```text
Base Layer
  ├─ Grounded (Default)
  │    └─ 1D Blend Tree / MoveSpeed
  │         ├─ 0 : Idle slot
  │         └─ 1 : Move slot
  ├─ Attack : Attack slot
  ├─ Hit    : Hit slot
  └─ Death  : Death slot
```

| Animator 전이 | 조건 | 설정 기준 |
| --- | --- | --- |
| Grounded → Attack | IsAttack == true | Has Exit Time 없음 |
| Attack → Grounded | IsAttack == false AND IsGrounded == true | FSM 종료 이후 복귀 |
| Hit → Grounded | IsHit == false AND IsGrounded == true | FSM 종료 이후 복귀 |
| Any State → Hit | IsHit == true AND IsDeath == false | 자기 재진입 금지 |
| Any State → Death | IsDeath == true | 자기 재진입 금지, 우선 확인 |

- 초기 Transition Duration은 0. 픽셀 스프라이트 교체와 FSM 전환 시점을 명확하게 맞춤.
- Attack/Hit에서 normalizedTime만으로 Grounded에 자동 복귀하지 않음.
- State 종료는 Animation Event → FSM 전이 → parameter 변경 순서.
- Death에는 복귀 전이 없음.
- 한 Transition의 조건은 AND. OR가 필요해지는 확장에서는 별도 Transition 사용.
- 이번에는 단일 Attack State이므로 불필요한 Sub-State Machine을 추가하지 않음.

### 6.2 몬스터별 Override

공용 Controller에는 안정적인 다섯 Clip slot을 두고, 각 Override Controller에서 다음 항목을 모두 교체한다.

| 공용 slot | Goblin용 Clip |
| --- | --- |
| Idle | Goblin_Idle.anim |
| Move | Goblin_Move.anim |
| Attack | Goblin_Attack.anim |
| Hit | Goblin_Hit.anim |
| Death | Goblin_Death.anim |

- 공용 Controller의 parameter·State·Transition은 몬스터별로 복제하지 않음.
- 기본 prefab은 템플릿이며 모든 slot을 연결한 몬스터 prefab variant로 검증.
- 공용 `BasicMonster.prefab`과 완성된 `Goblin.prefab`은 `Assets/Prefabs/Monster/` 루트에 배치. Goblin의 Override Controller와 교체 Clip은 `Assets/Prefabs/Monster/Goblin/Animations/`에 배치.
- 교체 Clip에는 필요한 Animation Event를 직접 설정. 원본 slot의 Event가 자동 승계된다고 가정하지 않음.
- 이벤트 프레임은 교체 Clip의 길이·공격 유효 프레임에 맞춰 설정.
- SpriteRenderer 애니메이션 binding path는 모든 몬스터에서 동일하게 유지.
- Runtime Animator는 Visual Child에 한 개만 배치. Root에 중복 Animator를 추가하지 않음.

**Override Controller만으로 바뀌는 것은 애니메이션 Clip이다.** 몸체 크기, Collider, 스탯, 이동 속도, 공격 범위까지 바꾸려면 prefab variant와 데이터 에셋도 조정해야 한다. 다만 같은 기본 행동이라면 C# 코드와 FSM·Animator 전이 구조는 변경하지 않는다.

### 6.3 Animation Event 계약

| Clip | 필요한 이벤트 | 결과 |
| --- | --- | --- |
| Idle / Move | 없음 | Grounded에서 계속 표현 |
| Attack | 유효 공격 프레임에 OnAnimationOnFrame 1회 | 현재 AttackType의 피해 판정 |
| Attack | 마지막 재생 구간에 OnAnimationEnd 1회 | AttackEndTransition 활성화 |
| Hit | 마지막 재생 구간에 OnAnimationEnd 1회 | GetHitEndTransition 활성화 |
| Death | 마지막 재생 구간에 OnAnimationEnd 1회 | OnDeathFinished 실행 |

Idle/Move는 Loop, Attack/Hit/Death는 non-loop로 설정한다. 종료 Event 이전에 Animator가 자동으로 다른 Clip으로 넘어가 Event가 누락되지 않게 한다.

공격 피해는 AttackState 진입이 아니라 OnFrame에서만 적용한다. End에서는 공격을 실행하지 않는다. 피격·사망으로 공격이 중단되면 이후 공격 Frame 이벤트가 피해를 적용하지 않는지도 검증한다.

## 7. Goblin 에셋 제작과 prefab

### 7.1 확인된 원본 상태

Unity Editor에서 확인한 렌더 파이프라인은 URP다. Goblin PNG는 모두 Sprite/Multiple, PPU 100, Bilinear, Compressed, Mipmap off 상태다. 원본 Goblin 폴더에는 Animation Clip·Controller·prefab은 없다.

| 원본 PNG | 이미지 크기 | 현재 분리 Sprite 수 | 이번 사용 |
| --- | --- | --- | --- |
| Idle.png | 600 × 150 | 4 | Idle |
| Run.png | 1200 × 150 | 8 | Move |
| Attack.png | 1200 × 150 | 8 | 기본 단발 공격 |
| Take Hit.png | 600 × 150 | 6 | Hit, 분할 교정 필요 |
| Death.png | 600 × 150 | 4 | Death |
| Attack2.png | 1200 × 150 | 10 | 제외 |
| Attack3.png | 1800 × 150 | 13 | 제외 |
| Bomb_sprite.png | 1900 × 100 | 52 | 제외 |

자동 tight 분할로 작은 이펙트 조각이 별도 Sprite가 되고, 프레임별 rect·중심 pivot도 달라져 있다. 현재 Sprite 개수를 그대로 Animation 프레임 개수로 사용하지 않는다.

### 7.2 작업용 Sprite와 Clip

- 원본 패키지를 보존하기 위해 필요한 다섯 시트의 작업용 복사본을 `Assets/Prefabs/Monster/Goblin/Sprites/`에 생성.
- 다섯 Animation Clip과 Goblin Override Controller는 `Assets/Prefabs/Monster/Goblin/Animations/`에 배치.
- 시트 배치 확인 후 150 × 150 cell 기준으로 분할. 예상 프레임: Idle 4, Move 8, Attack 8, Hit 4, Death 4.
- 몸체와 이펙트를 같은 cell에 포함. 자동 tight 분할 결과를 이름 순으로만 재사용하지 않음.
- 공통 cell 좌표의 동일한 발 기준 pivot 설정. 시트별·프레임별 몸체 중심으로 pivot을 재계산하지 않음.
- Point, Mipmap off, Uncompressed 적용. PPU는 기존 100을 초기 기준으로 유지하고 실제 크기를 확인.
- Animation에는 SpriteRenderer.sprite 곡선만 사용. Root Transform 이동·scale 곡선은 넣지 않음.
- Clip FPS와 공격 유효 프레임은 시각 검증 후 확정. 게임플레이 수치처럼 근거 없이 확정하지 않음.
- 화면 전체의 카메라·URP·품질 설정 변경은 이번 작업에 포함하지 않음.

### 7.3 prefab 계층

```text
BasicMonster / Goblin (Root)
  ├─ MonsterController
  ├─ MonsterInput
  ├─ MonsterAnimator
  ├─ AgentMotor2D
  ├─ AgentCombatHandler
  ├─ Health
  ├─ Rigidbody2D
  ├─ Collider2D
  └─ Visual
       ├─ SpriteRenderer
       ├─ Animator
       └─ AgentAnimationEventProxy
```

- MonsterAnimator의 실제 `_anim`은 Visual의 Animator 참조.
- Controller의 Proxy도 동일한 Visual 컴포넌트 참조.
- Rigidbody2D는 Dynamic, 회전 Z 고정. x 이동은 Motor, y는 기존 물리 처리.
- Root scale 크기는 1 기준. AgentMotor2D가 방향 전환 시 Root scale을 ±1로 설정하므로 종별 크기 보정은 Visual에서 수행.
- Collider는 Root에 배치하여 Health를 동일 오브젝트에서 찾을 수 있게 구성.
- 공격 대상 LayerMask는 Player만 포함하도록 확인. 이미 존재하는 Player/Enemy Layer 활용.
- 피해 판정 box의 offset·size는 Root 좌표 기준으로 실제 Goblin 몸체·무기 위치에 맞춤.
- SpriteRenderer sorting layer·order·material은 현재 플레이 환경과 일치시킴.
- GroundDetector, PlayerDetector, BehaviorGraphAgent, Brain은 신규 기본 prefab에 넣지 않음.
- 완성 prefab 저장 경로는 `Assets/Prefabs/Monster/BasicMonster.prefab`, `Assets/Prefabs/Monster/Goblin.prefab`. Goblin 전용 Stat/Motor 데이터는 `Assets/Prefabs/Monster/Goblin/ScriptableObjects/`에 배치.

## 8. 변경 파일과 삭제 후보

### 8.1 에셋·스크립트 배치 기준

```text
Assets/Prefabs/Monster/
  ├─ BasicMonster.prefab                 # 공용 템플릿 prefab
  ├─ Goblin.prefab                       # 완성 Monster prefab variant
  ├─ Animations/
  │    ├─ BasicMonster_Anim.controller   # 공용 Animator Controller
  │    └─ Slots/                        # 공용 다섯 Clip slot
  ├─ ScriptableObjects/
  │    └─ MonsterAnimationData.asset     # 공용 parameter 이름 데이터
  └─ Goblin/
       ├─ Sprites/                      # Goblin 작업용 Sprite 시트
       ├─ Animations/                   # Goblin Clip·Override Controller
       └─ ScriptableObjects/            # Goblin Stat/Motor 등 종별 데이터

Assets/Scripts/FSM/NPC/AIMonstor/
  ├─ @Hub/
  │    ├─ MonsterController.cs
  │    └─ MonsterAnimator.cs
  ├─ Input/
  │    └─ MonsterInput.cs
  ├─ SOData/
  │    └─ MonsterAnimationDataSO.cs
  └─ MonsterState/
       ├─ MonsterStateFactory.cs
       ├─ MonsterStateFactoryData.cs
       └─ States/
            ├─ MonsterAttackState.cs
            └─ MonsterHitState.cs
```

- `Common/`을 별도 에셋 기준 경로로 만들지 않음. 공용 에셋은 Monster 루트 아래 역할별 폴더 사용.
- 완성 Monster prefab은 Monster 루트에 모으고, 종별 지원 에셋은 해당 종의 하위 폴더에 배치. Goblin prefab을 Goblin 전용 에셋 폴더 안에 저장하지 않음.
- `MonsterAnimator.cs` 같은 컴포넌트 구현은 스크립트 경로에, `.controller`·`.overrideController` 같은 Animator 에셋은 prefab 에셋 경로에 배치.
- `MonsterAnimationDataSO.cs`는 ScriptableObject 타입 정의. 공용 parameter 이름을 담는 `.asset`은 Monster 공용 ScriptableObjects 폴더에, Goblin에만 필요한 데이터 `.asset`은 Goblin/ScriptableObjects 폴더에 배치.
- 이번 Monster 스크립트는 Goblin 전용이 아니라 기본 네 상태 몬스터 공용. Goblin 전용 Controller·Factory·State 스크립트를 별도로 복제하지 않음.
- GetHitTransition·DeathTransition·AttackEndTransition·AgentController는 Player도 사용하는 공용 기반이므로 기존 `Assets/Scripts/FSM/Agent/` 경로에 유지.
- 기존 Monster 파일은 필요 여부와 직렬화 참조를 확인한 뒤 유지·삭제를 구분. 새 경로에 없는 기존 파일을 일괄 삭제하지 않음.

### 8.2 변경 파일 목록

| 분류 | 대상 | 작업 |
| --- | --- | --- |
| 재작성 | AIMonstor/@Hub/MonsterController.cs | 공용 네 상태 몬스터 초기화·공격 시작·사망 처리 |
| 재작성 | AIMonstor/MonsterState/MonsterStateFactory.cs | AgentStateFactory 상속·DI·전이 조립 |
| 대체 | AIMonstor/Input/AIMonsterInput.cs → MonsterInput.cs | 기존 Input 제거 후 외부 명령 Input 제작. 레거시 호출 호환 불필요 |
| 추가 | AIMonstor/@Hub/MonsterAnimator.cs | Grounded·Attack capability 구현 |
| 추가 | AIMonstor/SOData/MonsterAnimationDataSO.cs | 공통 + 몬스터 parameter 이름 |
| 추가 | AIMonstor/MonsterState/States/MonsterAttackState.cs | 공용 Attack 확장, 수평 이동 정지 |
| 추가 | AIMonstor/MonsterState/States/MonsterHitState.cs | 공용 Hit 확장, 잔여 수평 이동 정지 |
| 수정 | Agent/StateControl/TransitionRules/GetHitTransition.cs | IDisposable 수명 관리 |
| 수정 | Agent/StateControl/TransitionRules/DeathTransition.cs | 현재 사망 값 기반 판정 |
| 수정 | Agent/StateControl/TransitionRules/Attack/AttackEndTransition.cs | 고정 복귀 생성자 지원 |
| 수정 | Agent/@Hub/AgentController.cs | 파괴 시 State·구독 정리 |
| 에셋 추가 | Prefabs/Monster/BasicMonster.prefab, Goblin.prefab | 공용 템플릿과 완성 Goblin variant |
| 에셋 추가 | Prefabs/Monster/Animations/, ScriptableObjects/ | 공용 Controller·Clip slot·parameter 데이터 |
| 에셋 추가 | Prefabs/Monster/Goblin/ | Goblin Sprite 작업본·Clip·Override·Stat/Motor 데이터. 완성 prefab은 Monster 루트 |
| 삭제 | Prefabs/Monster/Orc/OrcAI.prefab | 기존 prefab 완전 삭제. Scene 인스턴스·맵 등록 항목 정리 |
| 삭제 | AIMonstor/@Behavior/OrcBrain.cs 및 Monster 전용 Actions 4개 | Orc 전용 참조 제거 후 삭제. 신규 Behavior 구현 없음 |
| 삭제 후보 | Prefabs/Monster/의 불필요한 기존 파일 | Orc 전용 Clip·SO·Monster_Anim.controller 등 참조 정리 후 불필요한 에셋 삭제 |

`AIMonstor` 폴더 철자 교정까지 한 번에 확대하지 않는다. 필요한 클래스 변경과 참조 안전성이 먼저다.

### 삭제 안전 기준

- C# 참조뿐 아니라 MonoScript GUID의 prefab·scene·Graph·데이터 참조까지 확인.
- 기존 Orc prefab·전용 코드의 호환성 보존이나 신규 구조 이관은 하지 않음.
- Orc prefab 사용 Scene의 인스턴스와 맵 데이터의 등록 항목 제거. null 참조만 남기는 방식은 피함.
- Unity Editor에서 prefab·에셋 삭제 시 `.meta`도 함께 처리. 계층·데이터·직렬화 참조는 Editor API로 정리.
- 새 Controller·Factory·Input은 기존 MonoScript GUID를 유지해야 한다는 제약 없이 제작 가능.
- Orc 전용 Clip·데이터·Controller는 남은 참조가 없을 때만 삭제. 다른 종류의 Agent가 사용하는 공용 파일은 보존.
- Monster 폴더 전체의 기존 파일 정리는 개별 사용 여부 확인 후 수행. 새로 추가한 Goblin 폴더·사용자 에셋을 기존 정리 대상으로 간주하지 않음.
- AIPlayer, NPC 공용 Actions, Unity Behavior 패키지, 제3자 원본 에셋은 삭제하지 않음.
- Graph는 신규 설계하지 않음. 폐기되는 Orc 전용 참조만 정리하며, 다른 Agent가 사용하는 Graph까지 영향을 주면 해당 항목만 분리하여 보고.

## 9. 순차 실행 체크리스트

각 단계는 구현·검증 후에만 완료 체크한다. 미실행 검증을 완료로 표시하지 않는다.

### Phase 0 — 범위·참조 확정

- [x] 현재 Agent·Player·AIMonstor 코드와 Goblin 임포트 상태 확인.
- [x] 네 상태·단발 공격·Behavior 제외 범위 정리.
- [ ] 작업 시작 시 git 변경사항과 적용 대상 instruction 파일 재확인.
- [ ] 변경·삭제 대상의 C#·GUID·prefab·scene·Graph 참조 목록 확보.
- [ ] 기존 Monster 폴더 파일의 유지·삭제 목록과 8.1의 신규 배치 경로 확인.
- [ ] 기존 Orc Scene 인스턴스·맵 등록 항목 제거 후 OrcAI.prefab 삭제.
- [ ] 신규 구현에 이관할 Orc 구조·에셋·호환 API가 없음을 확인.
- [ ] 사용할 검증 Scene 확정. 기존 활성 Scene을 임의 저장하거나 교체하지 않음.

### Phase 1 — 공용 전이와 수명 안정화

- [ ] GetHitTransition 구독 Dispose·중복 구독·flag 초기화 구현.
- [ ] DeathTransition을 현재 값 판정형 ITransitionRule로 정리.
- [ ] AttackEndTransition에 고정 Type 복귀 지원. 기존 Player 생성자 유지.
- [ ] Controller 파괴 시 현재 State 및 OnTransition 연결 정리.
- [ ] Unity 컴파일 및 Player의 Attack 종료·피격·사망 회귀 확인.

### Phase 2 — Monster Input·Animator 계약

- [ ] Orc 전용 Brain·Action 코드와 참조 정리. Input 교체와 같은 단계에서 컴파일 단절 방지.
- [ ] MonsterInput 외부 이동·공격 요청 API 구현.
- [ ] MonsterAnimationDataSO 작성.
- [ ] MonsterAnimator 구현, 부모의 parameter 설정 함수 재사용.
- [ ] Jump/Fall/Dash/Combo·Behavior 의존성 없는지 확인.
- [ ] 기존 AIMonsterInput 변경에 따른 참조 처리와 컴파일 확인.
- [ ] Monster 스크립트가 AIMonstor 기준의 @Hub/Input/SOData 분류에 배치되었는지 확인.

### Phase 3 — State·Factory·Controller

- [ ] MonsterAttackState·MonsterHitState 구현.
- [ ] MonsterStateFactoryData에서 공통·전용 Animator 참조 연결.
- [ ] MonsterStateFactory 상속 적용, 정확히 네 Type key 등록.
- [ ] Death·Hit·일반 행동 순서로 전이 연결.
- [ ] MonsterController 의존성 초기화·TryStartAttack·OnDeathFinished 구현.
- [ ] 필수 참조 누락 검사, 모든 목적 State key 등록 검사.
- [ ] 컴파일 및 이벤트 구독·해제 수명 확인.
- [ ] Factory·FactoryData·State가 AIMonstor/MonsterState 아래 역할별로 배치되었는지 확인.

### Phase 4 — Goblin Sprite·Animation Clip

- [ ] Goblin/Sprites에 다섯 시트 작업용 복사본 생성·분할·공통 pivot 적용.
- [ ] Point·Uncompressed·PPU 설정 확인.
- [ ] Goblin/Animations에 Idle/Move/Attack/Hit/Death Clip 제작.
- [ ] Loop 설정과 SpriteRenderer binding path 확인.
- [ ] Attack OnFrame 및 Attack/Hit/Death End Event 배치.
- [ ] 프레임별 발 위치·이펙트·공격 타이밍 수동 확인.

### Phase 5 — 공용 Animator·Override

- [ ] Monster/Animations에 공용 BasicMonster_Anim Controller와 다섯 slot 구성.
- [ ] Grounded Blend Tree와 네 Animation State 연결.
- [ ] parameter 이름·타입·기본값·Transition 우선순위 확인.
- [ ] Goblin/Animations에 Animator Override Controller 제작 후 모든 slot 연결.
- [ ] 교체 Clip 이벤트가 실제로 수신되는지 확인.

### Phase 6 — prefab·데이터 연결

- [ ] Monster 루트에 BasicMonster.prefab과 완성 Goblin.prefab variant 제작.
- [ ] Visual Animator·Proxy와 Root MonsterAnimator 참조 연결.
- [ ] Monster/ScriptableObjects에 공용 AnimationData, Goblin/ScriptableObjects에 StatData·MotorData 생성. AttackData 1개 이상 연결.
- [ ] Collider·공격 box·LayerMask·sorting·크기 확인.
- [ ] Brain·BehaviorGraphAgent·불필요 Detector 없는지 확인.

### Phase 7 — 동작 검증

- [ ] 외부 명령으로 좌·우 이동과 정지 확인.
- [ ] 이동 중 공격 요청 → 수평 이동 정지 → Grounded 복귀 확인.
- [ ] 요청 한 번으로 공격 한 번, 추가 요청 없이 자동 반복하지 않음 확인.
- [ ] 공격 중 추가 요청이 종료 후 공격으로 예약되지 않음 확인.
- [ ] OnFrame에서만 피해 발생, 자신·같은 팀 제외 확인.
- [ ] Grounded/Attack의 피격 → Hit → Grounded 확인.
- [ ] Grounded/Attack/Hit의 치명타 → Death 직접 전환 확인.
- [ ] Death End 이후 오브젝트 제거·구독 정리 확인.
- [ ] 반복 State 진입과 생성·제거 시 중복 구독·이전 flag 잔류 없음 확인.
- [ ] FSM과 Animator 상태 일치, parameter·Event·Missing Script 경고 없음 확인.
- [ ] 공용 파일 수정에 대한 Player 회귀 결과 기록.

### Phase 8 — 삭제 잔여물 확인·인수인계

- [ ] 기존 Orc prefab·전용 Input·Brain·Actions가 남지 않았는지 확인.
- [ ] Monster 폴더의 기존 파일 중 불필요한 Orc Clip·데이터·Controller 등을 참조 정리 후 삭제.
- [ ] 완성 prefab·공용 에셋·Goblin 전용 에셋·Monster 스크립트가 8.1의 분류 경로에 배치되었는지 확인.
- [ ] 기존 Orc를 위한 호환 함수·이관 코드가 신규 Monster에 포함되지 않았는지 확인.
- [ ] 컴파일·prefab·scene·Graph 참조 재검증.
- [ ] 완료 작업·수동 검증·미완료 항목을 이 문서에 기록.
- [ ] 사용자가 요청한 경우에만 Refactoring_Log 최신화·git commit 수행.

## 10. AI Agent 실행 지침과 제작 명령

### 실행 원칙

1. 현재 소스가 이 문서 작성 이후 변경되었는지 확인한 뒤 진행. 오래된 코드 가정으로 덮어쓰지 않음.
2. 코드 수정은 단계별 최소 범위로 수행. 에셋 편집은 연결된 Unity Editor의 CLI/MCP·Editor API로 수행.
3. Editor가 연결되어 있으면 scene·prefab·controller·asset YAML을 직접 수정하지 않음.
4. Sandbox의 탐색 실패를 Editor 미실행으로 단정하지 않음. 승인된 범위의 연결 재확인 후 판단.
5. 사용자 편집과 dirty Scene은 보존. 신규 에셋 경로 충돌 시 기존 파일을 임의 덮어쓰지 않음.
6. Behavior 로직·패키지·AIPlayer로 작업을 확장하지 않음.
7. 단발 공격에 Player 전용 타입·스태미나·콤보를 주입하지 않음.
8. 외부 명령 API만 구현. 검증 편의를 위해 런타임 자동 공격·키보드 조작 코드를 기본 prefab에 남기지 않음.
9. 거리·속도·시간을 비교하는 로직이 추가될 경우 용도에 맞는 허용 오차 사용. float의 정확한 0 도달에 완료 전이를 의존하지 않음.
10. 컴파일·정적 연결 확인과 실제 Play Mode·시각 검증을 구분하여 보고.
11. 에셋·스크립트 경로는 8.1의 배치 기준 준수. 완성 prefab은 Monster 루트, Goblin 지원 에셋은 Goblin 하위, Monster 스크립트는 AIMonstor 역할별 폴더에 배치.

### 최초 구현 명령 예시

> `Docs/FSM/Agent_Extenstion_Monster.md`를 읽고 Phase 0부터 순서대로 구현해 주세요. 기존 Orc prefab과 전용 코드는 삭제하고, Goblin 에셋으로 신규 기본 단발 공격 몬스터를 처음부터 제작해 주세요. Orc 구조는 재사용·이관하지 말고 삭제로 남는 Scene·맵 참조만 정리해 주세요. Behavior는 구현하지 말고 Player·AIPlayer·공용 코드의 관련 없는 동작은 보존해 주세요. 완료한 체크리스트만 표시하고 수동 확인이 필요한 단계는 별도로 알려 주세요.

### 공용 구조 완성 후 다른 몬스터 제작 명령 예시

> 기본 네 상태 Monster 템플릿으로 `{몬스터명}`을 제작해 주세요. 에셋 폴더는 `{경로}`이고 Idle/Move/Attack/Hit/Death 소스는 `{목록}`입니다. 공용 Controller·Factory·State·Animator 코드는 유지하고, 작업용 Sprite·Clip·Override Controller·prefab variant·데이터 에셋을 구성해 주세요. Behavior는 추가하지 말고 누락된 Clip이나 결정이 필요한 수치는 먼저 알려 주세요.

### 추가 제작 시 확인할 입력

- 몬스터 이름, 원본 에셋 폴더, 결과물 경로.
- 다섯 Animation Clip 또는 Sprite 시트 대응표.
- PPU·발 pivot·Visual 크기·Collider 기준.
- 체력, 이동 속도, 공격 피해·offset·size.
- Clip FPS, 공격 유효 프레임, 종료 이벤트 위치.
- Player/Enemy LayerMask와 검증 Scene.

비행, 다중 공격, 콤보, 돌진 등 행동 프로필이 달라지면 Override만으로 해결한다고 가정하지 않는다. 기존 네 상태 템플릿의 확장 계획을 별도로 작성한다.

## 11. 완료 기준과 역할 분담

### 완료 기준

- 동일한 네 FSM State와 공용 Animator 구조로 기본 단발 공격 몬스터 제작 가능.
- 몬스터 종류별 Controller·Factory·State 복제 불필요.
- 공통 Hit/Death와 Monster Grounded/Attack Animator 책임 분리.
- 공격 시작 판정은 Transition 경유, 공격 표현은 State, 피해 적용은 OnFrame으로 구분.
- Monster 종료 Rule이 미등록 FallState를 반환하지 않음.
- 오브젝트 수명과 State 수명에 맞춰 Action·UniRx 구독 정리.
- 새 Goblin에 Behavior 관련 컴포넌트·로직 없음.
- 기존 Orc 제거·잔여 참조 정리·관련 없는 에셋 보존·Player 회귀 결과 명시.

### AI Agent 담당

- 코드 분석·구현, 의존성 주입·Type key·전이 순서 검증.
- Editor 도구로 Sprite/Clip/Animator/prefab/데이터 구성.
- 컴파일·Console·직렬화 참조·Animation Event 연결 확인.
- 실행 가능한 외부 명령 기반 검증 및 체크리스트 갱신.

### 사용자 확인

- Goblin 크기·발 위치·모션 표현과 공격 유효 프레임.
- 이동·피격·공격 중단·사망 표현의 실제 타이밍.
- 체력·속도·피해·공격 범위 등 밸런스 값 확정.
- 기존 Orc와 무관한 Graph·에셋에도 변경이 필요한 경우 해당 범위 판단.

수동 검증을 AI가 확인하지 못한 경우 완료로 표시하지 않고, 재현 방법과 확인 항목을 전달한다.
