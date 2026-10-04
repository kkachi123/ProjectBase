# FSM Agent 제작 가이드

## 1. 목적과 사용 방법

이 문서는 다른 AI가 ProjectRE의 현재 코드베이스를 읽고, 사용자 요구사항에 맞는 새로운 FSM Agent를 제작하기 위한 실행 가이드다. 여기서 **게임 Agent**는 Player·Monster 같은 게임 오브젝트를, **작업 AI**는 이를 구현하는 도구 사용 주체를 의미한다.

사용자는 Agent의 행동·에셋·작업 범위를 지정한다. 작업 AI는 기존 구조에서 재사용할 요소와 확장할 요소를 판단하고, 코드·데이터·Animator·Prefab을 연결한 뒤 검증 결과를 전달한다.

### 다른 AI에게 전달할 시작 명령

> `Docs/FSM/ProjectSkill/FSM_Agent_Creation_Guide.md`를 먼저 읽고 현재 소스를 확인해 주세요. 아래 Agent 요구사항에 따라 기존 Controller·Factory·State·Transition·Animator를 재사용할 수 있는지 판단한 뒤 제작해 주세요. 작업 범위 밖의 코드와 사용자 변경은 보존하고, 검증한 항목과 수동 확인이 필요한 항목을 구분해 주세요.

### 실행 전 원칙

1. 사용자 지시와 적용 가능한 프로젝트 지침을 먼저 확인한다.
2. 문서의 경로·시그니처·구현 상태를 현재 소스와 대조한다. 이 문서는 자동 갱신되지 않는다.
3. 기존 계획 문서에는 과거 상태나 미구현 제안이 포함될 수 있다. 제안을 현재 구현으로 취급하지 않는다.
4. 에셋만 추가하면 되는 Agent에 전용 Controller·Factory·인터페이스를 불필요하게 생성하지 않는다.
5. 사용자가 계획만 요청하면 구현하지 않는다. 제작을 요청한 경우 승인된 범위 안에서 구현·검증한다.

## 2. 사용자에게 필요한 요구사항

사용자가 클래스 설계나 의존성 주입 목록을 직접 작성할 필요는 없다. 다음 항목으로 행동과 제작 범위를 확정한다.

| 항목 | 사용자가 전달할 내용 | 작업 AI의 판단 |
| --- | --- | --- |
| Agent 목적 | 이름, 역할, 기존 Agent와 행동이 같은지 | 기존 프로필 재사용 또는 신규 확장 |
| 원본 에셋 | Sprite·모델·Clip 경로, 사용할 모션 | 모션 대응표와 누락 에셋 확인 |
| State | 사용할 State, 시작 State, 전용 행동 | 공용 State와 전용 State 구분 |
| 전이 | 진입·종료 조건, 중단 가능 여부, 우선순위 | State별 Transition 등록표 작성 |
| 조작 | 직접 입력·외부 명령·AI 판단 중 선택 | Input 계약 및 명령 API 구성 |
| AI 범위 | 판단 로직도 구현할지, Behavior 사용 여부 | 판단 계층 포함 여부 확정 |
| 공격 | 공격 종류, 콤보 여부, 이동 허용, 판정 시점 | 공격 시작·실행·종료 계약 구성 |
| 이동·물리 | 지상·공중, 이동 축, 중력, 충돌 방식 | Motor·Detector·Handler 적합성 확인 |
| 사망 | 삭제·비활성화·유지, 재사용 여부 | 종료 처리 및 구독 수명 설계 |
| 데이터 | 체력·속도·피해·공격 범위 또는 임시값 허용 | 기존 SO 재사용 및 전용 데이터 필요성 판단 |
| 저장 위치 | 스크립트·공용 에셋·종별 에셋·Prefab 경로 | 프로젝트 배치 규칙 적용 |
| 작업 범위 | 코드·에셋·Scene 배치·문서·삭제·커밋 여부 | 허용된 변경 범위 확정 |
| 검증 | 테스트 Scene, 수동 작업 분담 | 자동·수동 확인 항목 구분 |

### 미지정 항목 처리

- 기존 프로필과 동일하다고 지정한 경우 기존 동작을 유지하고, 적용한 기본값을 보고한다.
- 수치가 없으면 임시값 사용 허용 여부를 확인한다. 허용된 임시값은 조정 대상 목록에 남긴다.
- 누락된 핵심 모션, 초기 State, 공격 중단 정책처럼 결과를 바꾸는 사항은 확인한다. 안전하게 진행 가능한 분석·계획 작업은 계속한다.
- AI 판단 방식이 미정이면 자동 추적·순찰·공격을 임의로 추가하지 않는다. 외부 명령 수신부와 판단 계층을 구분한다.
- 기존 파일 삭제, 기존 Scene 저장, 범위 밖의 공용 계약 변경, Git 커밋·푸시는 제작 요청만으로 자동 포함하지 않는다. 요청 범위를 확인한다.

## 3. 먼저 읽을 실제 코드와 에셋

경로는 프로젝트 루트 기준이다. 파일이 이동되었으면 파일명·클래스명으로 현재 위치를 찾아 읽는다.

| 순서 | 경로 | 확인 목적 |
| --- | --- | --- |
| 1 | `Assets/Scripts/FSM/Agent/@Hub/AgentController.cs` | 공통 초기화, 시작 State, 실행 루프, 전환·파괴 수명 |
| 2 | `Assets/Scripts/FSM/Agent/StateControl/AgentStateFactory.cs` | `StateFactoryData`와 제네릭 Factory의 구성 순서 |
| 3 | `Assets/Scripts/FSM/Agent/StateControl/AgentStateBase.cs` | Enter·Execute·Exit와 Rule 구독 수명 |
| 4 | `Assets/Scripts/FSM/Agent/StateControl/TransitionRules/ITransitionRule.cs` | 조건 Rule·이벤트 Rule 계약 |
| 5 | `Assets/Scripts/FSM/Agent/Input/IAgentInput.cs` | 이동·공격·점프·Dash 입력 계약 |
| 6 | `Assets/Scripts/FSM/Agent/@Hub/IAgentController.cs` | 공격 시작 판정 계약 `IAttackStarter` |
| 7 | `Assets/Scripts/FSM/Agent/@Hub/AgentAnimator.cs`, `IAgentAnimator.cs` | 공통 Animator 기능·capability와 공용 설정 함수 |
| 8 | `Assets/Scripts/FSM/Agent/Handler/AgentAnimationEventProxy.cs` | 실제 Clip Event 수신 함수와 Controller 전달 |
| 9 | `Assets/Scripts/FSM/Agent/Handler/AgentCombatHandler.cs` | 공격 타입·데이터·실제 피해 판정 |
| 10 | `Assets/Scripts/FSM/Agent/Move/2D/AgentMotor2D.cs`, `Assets/Scripts/FSM/Agent/Handler/AgentMovementHandler2D.cs` | 이동 축·방향·속도 적용 |
| 11 | `Assets/Scripts/FSM/Agent/Combat/`, `Assets/Scripts/FSM/Agent/SOData/` | Health·Stamina·공통 데이터 |
| 12 | `Assets/Scripts/FSM/Agent/StateControl/States/`, `Assets/Scripts/FSM/GroundedAgent/StateControl/States/` | 공용 행동의 실제 책임과 생성자 |
| 13 | `Assets/Scripts/FSM/GroundedAgent/@Hub/GroundedAgentController.cs`, `Assets/Scripts/FSM/GroundedAgent/Detector/GroundDetector.cs` | 지면 감지를 사용하는 확장 기반 |
| 14 | `Assets/Scripts/FSM/NPC/AIMonstor/` | 기본 네 State Monster의 완료된 재사용 사례 |
| 15 | `Assets/Prefabs/Monster/BasicMonster.prefab`, `Goblin.prefab`, `Animations/BasicMonster_Anim.controller` | 템플릿·완성 Variant·공용 Animator 계약 |

점프·Dash·콤보처럼 추가 행동을 요청한 경우 `Assets/Scripts/FSM/Player/`와 관련 Handler·Transition·실제 Animator도 읽는다. Player의 모든 기능을 새 Agent로 복사하지 않는다.

### 참고 문서

- `Docs/FSM/ProjectSkill/FSM_State_Extension_Guide.md`: 새 State의 책임·전이·Animator 연결 기준.
- `Docs/FSM/Agent_Extenstion_Monster.md`: 기본 Monster·Goblin 제작 사례와 에셋 배치 기준. 파일명의 `Extenstion`은 실제 경로 표기다.
- `Docs/FSM/Refactoring_Log/FSM_Refactoring_Source_Log.md`: 변경 이유와 리팩토링 히스토리.

기존 State 확장 가이드의 Dash 예시는 현재 소스와 차이가 있다. 현재 Dash는 `DashState → AgentDashHandler2D` 실행과 완료 판정으로 종료하며, Animation End에 의존하지 않는다. Handler는 현재 `PlayerMotorData`를 요구하므로 이름만 보고 모든 Agent에 바로 사용할 수 있다고 가정하지 않는다.

## 4. 새 클래스가 필요한지 먼저 판단

| 요구사항 | 우선 선택 | 새로 만들 수 있는 요소 |
| --- | --- | --- |
| 기본 Monster와 행동이 같고 외형·수치만 다름 | 기존 Monster 프로필 재사용 | 종별 Clip·Override·데이터·Prefab Variant |
| State 수명은 같고 진입·종료 세부 동작만 다름 | 공용 State의 파생 구현 또는 기존 Handler 재사용 | 필요한 전용 State·Factory 구성 |
| 새로운 독립 행동이나 전이 규칙 필요 | 공용 가능성 검토 후 행동 확장 | State·Transition·Handler·Animator 기능 |
| 초기화·사망·사용 기능 조합이 다른 Agent | `AgentController` 계열 확장 | 전용 Controller·FactoryData·Factory |
| 지면 감지·점프·낙하 사용 | `GroundedAgentController` 적합성 검토 | 필요한 Input·Detector·State 연결 |
| 비행·3D 이동·풀링 등 기존 실행 정책과 다름 | 별도 실행·수명 설계 | 관련 기반 확장과 전용 구현 |

현재 `MonsterController : AgentController`는 지면 감지를 사용하지 않는다. 반면 `PlayerController : GroundedAgentController`는 지면 감지 기반이다. 지상 캐릭터라는 이유만으로 반드시 `GroundedAgentController`를 선택하지 않는다. 실제 착지·점프·낙하 요구사항으로 결정한다.

`AgentController`는 현재 2D Motor·Combat에 의존한다. 3D Agent 요청을 현재 Monster 템플릿의 에셋 교체만으로 구현할 수 있다고 판단하지 않는다.

## 5. 현재 FSM 계약과 책임 경계

```text
직접 입력 / 외부 명령 / 별도 판단 계층
  → Input 계약
  → TransitionRule 조건 평가
  → AgentStateBase.OnTransition(Type)
  → AgentController.ChangeState(Type)
  → 이전 State.Exit → 새 State.Enter
  → State.Execute → Handler / Motor / Animator

Clip Animation Event
  → AgentAnimationEventProxy
  → AgentController
  → 공격 피해 판정 / 종료 이벤트 / 사망 종료
```

### Controller

- 공통 컴포넌트·Handler 초기화, 현재 State 보유, Type 기반 전환, Animation Event 전달을 담당한다.
- 개별 State의 행동 코드를 Controller에 모아두고 State가 Controller 함수를 호출하는 과거 구조로 되돌리지 않는다.
- `AgentController.Start()`는 현재 `GroundedState`로 시작한다. 다른 시작 State가 필요하면 이벤트 연결을 유지하는 초기 진입 방식을 설계한다. `base.Start()`로 Grounded에 진입한 직후 다른 State에 들어가는 이중 진입을 기본 해법으로 삼지 않는다.
- `Update()`가 `Execute(Time.deltaTime)`를 호출한다. 파생 Controller·Handler에 중복 실행 루프를 추가하지 않는다.
- 파생 Controller의 `FixedUpdate()`는 실제 필요한 Detector·물리 갱신만 담당한다. `GroundedAgentController.FixedUpdate()`를 확장하면 기존 지면 갱신을 보존한다.
- `AgentController.FixedUpdate()`는 abstract이므로 직접 상속한 구체 Controller는 구현해야 한다. 기본 Monster처럼 추가 갱신이 없으면 빈 구현을 사용한다.
- 기본 `OnDeathFinished()`는 빈 함수다. 사용자 요구에 맞는 사망 후 삭제·비활성화·유지 정책을 구체 Controller에서 연결한다.

### State와 Transition

- State는 행동의 진입·수행·정리를, Transition은 전이 조건과 목적지를 담당한다.
- `Enter()`는 이벤트 Rule 구독 후 `OnEnter()`를 호출한다.
- `Execute()`는 Rule을 등록 순서대로 평가한다. 전이가 발생하면 해당 호출에서는 이전 State의 `OnExecute()`를 실행하지 않는다.
- `Exit()`는 이벤트 Rule 해제 후 `OnExit()`를 호출한다. 동일 구독을 Controller의 `ChangeState()`에서 다시 관리하는 구조를 만들지 않는다.
- State 자체의 행동 이벤트 구독은 `OnEnter()`·`OnExit()`에서 대칭으로 관리한다.
- Agent별 기능을 기존 Grounded·Attack 내부의 무관한 분기로 끼워 넣지 않는다. 독립적인 수명이 있으면 별도 State를 검토한다.

### Input과 Handler

- Input은 입력값·요청 전달만 담당한다. 전이·피해·콤보 진행 등 행동 정책을 Input에 넣지 않는다.
- 이동은 `IAgentMovementInput.GetMovementInput()` 값, 공격은 현재 `IAgentCombatInput.OnAttackRequested`의 인자 없는 `Action`이다.
- Jump·Dash 요청도 각각 이벤트 계약이다. 필요한 입력만 구현하고 Factory로 전달한다.
- Handler는 행동 실행이나 완료 판정 결과를 제공한다. Handler가 직접 `ChangeState()`를 호출하는 구조는 피한다.
- 의존성은 현재 방식에 맞춰 생성자 또는 `Initialize(...)`로 주입한다. 구체 Input을 State가 검색하거나 전역 Controller를 찾게 하지 않는다.

### 공격의 현재 계약

1. `AttackTransition`이 공격 요청을 수신한다.
2. Rule 평가에서 `IAttackStarter.TryStartAttack()`으로 시작 가능 여부를 확인한다.
3. 가능하면 Controller가 유효한 공격 타입을 CombatHandler에 설정한다.
4. `AttackState.OnEnter()`가 설정된 타입으로 공격 Animation에 진입한다.
5. `OnFrame` Event 수신 시 현재 State가 `AttackState` 계열이면 `PerformAttack()`으로 피해를 적용한다.
6. 종료 Event는 EndTransition의 플래그를 설정하고, 이후 Rule 평가에서 다음 State로 전환한다.
7. `AttackState.OnExit()`가 공격 타입과 Animator 공격 값을 정리한다.

`ApplyAttackType()`은 타입 설정이지 피해 적용이 아니다. 시작 판정에 피해 적용을 섞지 않는다. 공격 데이터 인덱스는 `attackDatas[attackType - 1]`이다.

현재 기본 Monster는 1번 공격만 선택한다. `Action<int>` 공격 요청과 다중 공격 Animator 분기는 아직 공용 입력 계약에 적용되지 않았다. 다중 공격 요구가 있으면 Player를 포함한 기존 호출부 영향을 분석하고 별도 확장 범위를 확정한다.

Player 콤보는 현재 같은 Attack FSM State 안에서 Animator의 `ComboTrigger`로 세부 모션을 진행한다. 삭제된 Combo Handler 구조를 재도입하거나 다른 Agent에 복사하지 않는다.

## 6. Controller·Factory 구현 기준

### 초기화 순서

1. 필요한 StatData·MotorData·AnimationEventProxy 등 필수 참조를 확인한다. 공통 Awake가 데이터를 사용하므로 누락 참조를 사용하기 전에 확인한다.
2. `base.Awake()`로 Health·CombatHandler·MovementHandler·Input을 초기화한다.
3. 전용 Animator를 초기화하고 필요한 전용 Handler·컴포넌트 의존성을 주입한다.
4. FactoryData에 공통·전용 의존성을 전달한다.
5. Factory가 반환한 Dictionary를 `_states`에 할당한다.
6. 시작 State 진입 전 필요한 감지값·데이터의 준비 여부를 확인한다.

`RequireComponent`만으로 SO·Animator Controller·Clip Event·직렬화 참조까지 자동 연결된다고 가정하지 않는다. 공격 기능이 필요한 Agent의 `CombatInput`·`AttackStarter`가 null이 아닌지 확인한다.

### Factory 구성

새 프로필을 만드는 경우 `AgentStateFactory<TData>`를 상속한다. 실행 순서는 다음과 같다.

```text
AddCommonStates → AddAgentStates → ConfigureTransitions
```

- 기본 `AddCommonStates()`는 Hit·Death를 생성한다. 해당 State가 없는 Agent를 요청했다면 필요한 범위에서 구성을 재정의한다.
- `AddAgentStates()`에서 전용 State를 추가하거나 공통 State를 교체한다. 전이를 붙인 뒤 State 객체를 교체하여 연결을 잃지 않는다.
- `ConfigureTransitions()`에서 최종 State 객체에 Rule을 등록한다. 등록 순서가 우선순위다.
- Factory는 State·Rule 구성을 담당한다. `OnTransition → ChangeState` 연결은 Controller의 시작 수명에서 담당한다.
- 전용 FactoryData는 `StateFactoryData`를 상속한다. 전용 Animator 접근자가 필요하면 공통 `Animator` 참조를 전달하는 형태로 만들고, 서로 다른 두 참조를 별도로 보관하지 않는다.

### Type 키와 파생 State

```csharp
states.Add(typeof(AttackState), new MonsterAttackState(...));
// AttackTransition.NextStateType == typeof(AttackState)
```

공용 행동을 확장한 경우 공용 기반 타입을 Dictionary 키로 유지한다. 새로운 독립 행동이면 그 행동의 State 타입을 키로 사용한다.

- 키와 값이 같은 클래스일 필요는 없지만, 값은 키가 나타내는 행동의 호환 구현이어야 한다.
- 모든 전이 목적지가 Dictionary에 등록되어야 한다.
- Grounded·Fall처럼 실행 중 바뀌는 목적지는 해당 Rule이 반환할 수 있는 모든 타입을 검증한다. 한 번의 `NextStateType` 조회로 동적 목적지 전체를 검증했다고 판단하지 않는다.
- `AttackState`는 현재 abstract이므로 직접 생성하지 않는다. 필요한 구체 구현을 재사용하거나 파생한다.

### 종료·재사용 수명

- Rule의 `Subscribe()`는 중복을 방지하고, `Unsubscribe()`는 자신의 구독과 예약 플래그만 정리한다.
- UniRx 구독은 반환된 `IDisposable`을 보관하고 해제한다. 다른 구독 전체를 제거하지 않는다.
- 파생 Controller가 `OnDestroy()`를 재정의하면 공통 State 종료·전환 이벤트 해제를 보존한다.
- 현재 공통 정리는 `OnDestroy()` 기준이다. 풀링·비활성화는 별도 수명 요구다. 비활성화만으로 구독·Health·Animator·State·속도가 모두 초기화된다고 가정하지 않는다.
- 거리·속도 종료 판정에는 단위와 규모에 맞는 허용 오차를 둔다. 현재 사례는 Dash 남은 거리 `<= 0.001f`, 착지 수직 속도 `<= 0.01f`다. 모든 float 비교에 같은 상수를 기계적으로 적용하지 않는다.

## 7. 기본 네 State Monster 재사용 기준

다음 조건이면 기존 Monster 프로필을 먼저 재사용한다.

- Grounded·Attack·Hit·Death 사용.
- 외부 이동·단발 공격 요청 수신.
- 기본 공격 타입 1 사용.
- Attack·Hit 진입 시 수평 이동 정지.
- 공격·피격 종료 후 Grounded 복귀.
- 사망 모션 종료 후 오브젝트 삭제.
- 점프·낙하·Dash·스태미나·콤보·판단 AI 미사용.

현재 전이 순서는 다음과 같다.

| State | 등록 순서대로 평가하는 전이 |
| --- | --- |
| Grounded | Death → Hit → Attack |
| Attack | Death → Hit → AttackEnd(Grounded 고정) |
| Hit | Death → HitEnd(Grounded 고정) |
| Death | 다른 State로 나가는 Rule 없음 |

`GetHitEndTransition`은 현재 Grounded 고정 복귀다. 비행·공중 피격을 요청하면 이 계약을 그대로 사용해도 되는지 확인한다.

`GroundedState`라는 이름과 Animator `IsGrounded`는 현재 기본 이동 State의 표현이다. 기본 Monster에는 GroundDetector가 없으며, GroundedState 진입 자체가 실제 지면 접촉을 보장하지 않는다.

### 외부 명령 API

- `MonsterInput.SetMovement(Vector2)`: x를 -1~1로 제한하고 y 입력 제외.
- `MonsterInput.RequestAttack()`: 인자 없는 단발 공격 요청 발행.
- 공격 중 이동 요청이 바뀌어도 Grounded 실행은 중단되어 있다. Grounded 복귀 시 현재 이동값이 다시 적용될 수 있으므로 의도에 맞는지 테스트한다.
- 공격 중 새 공격을 예약하는 기능은 기본 프로필에 없다. 자동 반복·입력 버퍼를 임의로 추가하지 않는다.

### 템플릿 주의사항

`BasicMonster.prefab`은 종별 StatData·MotorData가 비어 있는 템플릿이다. 바로 실행 가능한 완성 Agent로 취급하지 않는다. Variant에 종별 데이터·Sprite·Override를 연결한다. `Goblin.prefab`을 완료된 연결 사례로 확인한다.

## 8. Animator·Animation Event 연결 기준

### 코드 책임 분리

- `AgentAnimator`는 Hit·Death의 공통 capability와 Dictionary·공용 설정 함수만 담당한다.
- 전용 Animator는 사용하는 capability만 구현하고 해당 parameter Dictionary와 의미별 API를 보유한다.
- Bool·Float·Int·Trigger 설정 동작은 AgentAnimator의 제네릭 함수로 재사용한다. enum 종류별 동일 설정 함수를 다시 복제하지 않는다.
- 모든 애니메이션 인터페이스는 `Assets/Scripts/FSM/Agent/@Hub/IAgentAnimator.cs`에 둔다. `IPlayerAnimator.cs` 같은 별도 파일을 다시 만들지 않는다.
- 공통 이름 데이터는 `AgentAnimationDataSO`, 전용 이름 데이터는 필요한 파생 SO로 분리한다.
- Animator parameter 이름뿐 아니라 Bool·Float·Int·Trigger 타입까지 확인한다.

### Controller 재사용 여부

현재 기본 Monster Controller는 네 Animation State와 Grounded의 Idle·Move Blend Tree를 사용한다. 다섯 교체 슬롯은 Idle·Move·Attack·Hit·Death다.

- 같은 행동 구조는 Animator Override Controller로 Clip을 교체한다.
- Override는 Clip 교체이지 State·parameter·Transition 추가가 아니다. 새 행동이나 다중 공격 분기가 필요하면 기반 Controller 확장을 검토한다.
- Animator `AttackType`이 존재한다고 다중 공격 분기가 이미 구현되어 있다고 판단하지 않는다.
- 상태 Bool은 State Enter·Exit 수명에 맞춰 설정·해제한다. 이전 Bool·Trigger가 남아 다른 전이를 충족하지 않게 한다.
- Sub State Machine의 공통 출구는 필요할 때 상위에서 관리한다. 기본 단일 Attack 모션에도 불필요한 Sub State Machine을 강제하지 않는다.
- 한 Transition의 조건은 AND다. OR 의도는 별도 Transition 등 실제 지원되는 구성으로 표현한다.
- Has Exit Time·Duration·Interruption 설정이 FSM 전환과 표현을 어긋나게 하거나 종료 Event를 누락시키지 않는지 확인한다.

### Clip Event 계약

| Clip / 목적 | 실제 수신 함수 | 처리 |
| --- | --- | --- |
| Attack 유효 타격 시점 | `AgentAnimationEventProxy.OnAnimationOnFrame()` | 현재 AttackState 계열일 때 피해 판정 |
| Attack 종료 | `AgentAnimationEventProxy.OnAnimationEnd()` | `OnAnimationEnded` → AttackEnd Rule |
| Hit 종료 | `AgentAnimationEventProxy.OnAnimationEnd()` | `OnAnimationEnded` → HitEnd Rule |
| Death 종료 | `AgentAnimationEventProxy.OnAnimationEnd()` | 현재 DeathState일 때 `OnDeathFinished()` |

- 실제 Animation을 재생하는 Animator와 Proxy를 같은 GameObject에 연결한다.
- 교체 Clip의 Event를 직접 확인한다. 원본 슬롯의 Event가 Override Clip에 자동 승계된다고 가정하지 않는다.
- End가 필요한 모션은 실제 재생 경로에서 Event에 도달해야 한다. Exit Time으로 먼저 빠져나가면 Event가 실행되지 않을 수 있다.
- 콤보처럼 다음 모션으로 이어지는 경로와 최종 종료 경로는 구분한다. 모든 세부 모션이 별도 FSM State일 필요는 없다.
- 공격 중 피격·사망 후 도착한 오래된 Event가 현재 행동을 잘못 종료하거나 피해를 적용하지 않는지 검증한다.

## 9. Prefab·데이터·에셋 배치

### 기본 Monster 배치 규칙

```text
Assets/Prefabs/Monster/
├─ BasicMonster.prefab                 # 공용 템플릿
├─ Goblin.prefab                       # 기존 완성 Variant
├─ {AgentName}.prefab                  # 추가 완성 Monster
├─ Animations/                         # 공용 Controller·교체 슬롯
├─ ScriptableObjects/                  # 공용 이름 데이터
└─ {AgentName}/
   ├─ Animations/                      # 종별 Clip·Override
   ├─ ScriptableObjects/               # 종별 Stat·Motor 데이터
   └─ Sprites/                         # 필요한 작업용 Sprite
```

Monster 스크립트는 기존 `Assets/Scripts/FSM/NPC/AIMonstor/`의 `@Hub`·`Input`·`MonsterState`·`SOData` 분류를 따른다. `AIMonstor`는 실제 폴더명이다. 새로운 종류라는 이유로 종마다 공용 스크립트를 복사하지 않는다.

Monster가 아닌 Agent의 결과 경로는 사용자 지시와 기존 해당 영역의 분류를 따른다. 모든 Agent를 Monster 폴더에 넣지 않는다.

### 기본 Prefab 구성

```text
Root
├─ Controller / Input / 전용 Animator 어댑터
├─ Rigidbody2D / Collider2D / AgentMotor2D
├─ Health / AgentCombatHandler
├─ 필요한 전용 Handler·Detector만 추가
└─ Visual
   ├─ SpriteRenderer
   ├─ 실제 Unity Animator
   └─ AgentAnimationEventProxy
```

- `MonsterAnimator` 같은 어댑터 컴포넌트와 Unity `Animator` 컴포넌트를 혼동하지 않는다.
- 단일 Visual을 사용하는 기본 구성은 실제 Unity Animator 하나를 사용한다. Root·Visual에 중복 배치되어 다른 Animator를 제어하지 않는지 확인한다.
- Collider·Health 배치와 CombatHandler의 `TryGetComponent` 판정 방식이 맞는지 확인한다.
- 공격 대상 LayerMask로 자기 자신·아군·비대상을 제외한다. Layer를 임의로 새로 만들기 전에 기존 설정을 확인한다.
- 현재 Motor는 방향 전환 시 Root scale을 x=±1, y=z=1로 설정한다. 외형 크기 조절은 Visual 측에서 하고, 공격 offset의 방향도 함께 확인한다.
- PPU·pivot·Visual scale·Collider·공격 box를 함께 확인한다. Goblin의 크기·Clip 길이·FPS를 다른 에셋에 고정 적용하지 않는다.
- SO에 공용 필드가 있다고 스태미나·콤보 기능까지 활성화되는 것은 아니다. 실제 요구와 코드 사용 여부를 확인한다.
- 신규 저장 경로의 기존 파일과 GUID를 임의 덮어쓰지 않는다. 원본 외부 에셋은 가능한 보존하고 필요한 작업용 복사본을 사용한다.

## 10. 작업 도구·변경 범위

- 시작 전에 Git 변경 상태와 관련 파일의 사용자 편집을 확인한다.
- Unity 작업 도구가 제공되면 해당 도구 지침을 읽고 연결된 프로젝트가 현재 작업 프로젝트인지 확인한다.
- Scene·Prefab·Animator·Clip·SO 등 Unity 직렬화 에셋 편집은 연결된 Editor의 CLI/MCP·Editor API로 수행한다. YAML 직접 수정으로 GUID·참조를 연결하지 않는다.
- Editor 연결 실패와 CLI 탐색·인증·권한 실패를 구분한다. 탐색 결과가 비었다고 Editor가 꺼졌다고 단정하지 않는다. 제한된 권한은 정상 승인 절차를 사용한다.
- Editor 작업이 불가능하면 가능한 소스 분석·계획·코드 작업을 진행하고, 에셋 연결의 미완료 범위를 명시한다. 검증하지 못한 연결을 완료로 표시하지 않는다.
- Sprite 제작·분할·픽셀 렌더링 등 별도 작업이 포함되면 사용 가능한 관련 작업 지침을 적용한다.
- 사용자 Scene의 dirty 상태·Play 상태·기존 배치를 보존한다. 테스트 오브젝트·임시 파일은 작업 종료 시 정리한다.
- 기존 Orc 삭제는 이전 Goblin 제작의 개별 범위였다. 이번 Agent 요청에서도 기존 Agent 삭제가 허용된 것으로 해석하지 않는다.
- 허용된 삭제도 코드·Prefab·Scene·데이터 참조를 먼저 확인한다. 패키지·Behavior·Player 등 범위 밖 요소를 함께 정리하지 않는다.
- C# namespace는 관련 프로젝트 방식인 `ProjectRE`를 따른다. 주석은 높임말 없이 간결하게 작성하고, 클래스·API 설명에 필요한 `<summary>`를 사용한다.

## 11. 단계별 제작 체크리스트

아래 체크리스트를 해당 Agent의 계획 문서에 복사하여 사용한다. 이 공용 가이드 자체를 개별 제작의 완료표로 갱신하지 않는다.

### Phase 0 — 요구사항·현재 상태 확인

- [ ] 사용자 요구사항과 허용 변경·삭제 범위 확인.
- [ ] 현재 소스·유사 Agent·사용자 변경 확인.
- [ ] 에셋과 Idle·Move·Attack·Hit·Death 등 필요한 모션 대응표 작성.
- [ ] 기존 프로필 재사용 또는 신규 확장 결정과 이유 기록.
- [ ] State 목록·초기 상태·전이 순서·종료 정책 확정.
- [ ] 임시 수치·검증 Scene·수동 작업 분담 확정.

### Phase 1 — 설계 및 코드

- [ ] 변경·추가·재사용 파일 목록 작성.
- [ ] 필요한 Input·Controller·Handler·Animator 계약만 구성.
- [ ] FactoryData 의존성 주입 및 공통→전용→전이 구성 순서 적용.
- [ ] 공용 Type 키·전용 State 호환성·모든 목적지 확인.
- [ ] 구독·예약 입력·Animator 값·물리 값의 종료 정리 확인.
- [ ] 별도 AI·미요청 기능·불필요한 인터페이스 미추가 확인.

기존 Monster를 에셋만 교체하는 경우 새 코드가 필요 없는 항목은 근거와 함께 해당 없음으로 기록한다.

### Phase 2 — 에셋·Prefab

- [ ] Sprite·Clip 제작 또는 기존 모션 연결.
- [ ] Loop·공격 유효 프레임·End Event 확인.
- [ ] 공용 Controller 재사용 또는 승인된 확장.
- [ ] Override의 모든 필수 슬롯 연결.
- [ ] Stat·Motor·AnimationData 연결 및 공격 데이터 범위 확인.
- [ ] Prefab Variant·Visual·Proxy·실제 Animator 참조 연결.
- [ ] Collider·LayerMask·sorting·pivot·크기 확인.

### Phase 3 — 검증

- [ ] 새 코드 컴파일과 관련 Console 오류·경고 확인.
- [ ] Prefab Missing Script·null 참조·중복 Animator 확인.
- [ ] 최초 배치·재시작에서 정상 초기 State 진입 확인.
- [ ] 요청 행동·중단·종료·반복 실행 확인.
- [ ] 오브젝트 파괴·재사용 정책에 맞는 수명 정리 확인.
- [ ] 변경된 공용 코드의 기존 Agent 회귀 확인.
- [ ] 사용자 수동 확인 항목과 미검증 범위 전달.

### Phase 4 — 인수인계

- [ ] 변경 파일·새 Agent 사용법·외부 명령 API·데이터 조정 위치 요약.
- [ ] 요청된 계획 문서의 실제 완료 항목만 체크.
- [ ] 임시 테스트 요소 제거와 관련 없는 변경 보존 확인.
- [ ] 요청된 경우에만 리팩토링 로그·PDF·Git 커밋 수행.

## 12. 검증 시나리오와 완료 기준

### 기본 Monster 검증

| 시나리오 | 확인할 결과 |
| --- | --- |
| 최초 생성 | 데이터·Input·Animator·Factory 준비 후 Grounded 진입 |
| 좌·우·정지 명령 | 이동값·방향·MoveSpeed·Idle/Move 표현 일치 |
| 공격 1회 요청 | Attack 진입, 수평 이동 정지, 타입 1 설정 |
| 공격 진입 직후 | 유효 타격 Event 전에는 피해 미적용 |
| 유효 타격 Event | 설정 대상·범위·피해 적용, 비대상 제외 |
| 공격 종료 | End Rule 평가 후 Grounded 복귀와 공격 값 정리 |
| 공격 중 추가 요청 | 미요청 자동 반복이나 입력 잔류 없음 |
| 비치명적 피격 | 허용 State에서 Hit 진입, End 후 복귀 |
| 공격 중 피격·사망 | 중단 후 늦은 Event의 오동작 없음 |
| 치명적 피해 | 해당 프로필의 우선순위대로 Death 진입 |
| 사망 종료 | 지정한 삭제·비활성화·유지 정책 실행 |
| 반복 실행 | 이벤트·플래그·Trigger·속도·공격 타입 잔류 없음 |

풀링·다중 공격·공중 행동 등 추가 요구사항은 별도 시나리오를 추가한다. 기본 Monster의 결과를 새로운 프로필의 검증으로 대체하지 않는다.

### 자동 확인과 수동 확인 구분

- 정적 코드 점검: Type 키·생성자·데이터 범위·구독 정리 확인.
- Editor 확인: 컴파일·직렬화 참조·parameter·Event·Override 연결 확인.
- Play 확인: 실제 입력·명령·타격·전이·종료 흐름 확인.
- 사용자 수동 확인: 모션 표현·발 위치·조작감·공격 타이밍·크기·밸런스 확정.

컴파일 성공만으로 행동·Animator 전환 성공을 보고하지 않는다. Console의 기존 오류와 새 작업의 오류를 구분한다. 임시로 컴포넌트를 보완한 테스트가 원본 Prefab의 정상 실행을 증명하지는 않는다.

기본 완료 기준은 **사용자가 지정한 에셋으로 완성 Prefab을 생성하고, 명령→State→Animation→종료가 연결되며, 필요한 의존성이 누락되지 않은 상태**다. 수동 확인 대기·Editor 연결 불가 등 미완료 사항은 별도로 표시한다.

## 13. 사용자 요청 템플릿

다음 양식은 기본 Monster 사례다. 다른 Agent는 해당 요구사항에 맞게 변경한다.

```text
Docs/FSM/ProjectSkill/FSM_Agent_Creation_Guide.md를 읽고
현재 코드베이스를 기준으로 새 FSM Agent를 제작해 주세요.

[Agent]
- 이름: {AgentName}
- 역할: 기본 단발 공격 Monster
- 기존 Monster와 동일한 행동 구조를 우선 재사용
- 원본 에셋: {에셋 폴더 경로}
- 사용할 모션: Idle={경로}, Move={경로}, Attack={경로},
                Hit={경로}, Death={경로}

[행동]
- State: Grounded, Attack, Hit, Death
- 시작 State: Grounded
- 조작: 외부 이동·공격 명령만 제공. AI 판단 로직은 제외.
- 공격: 1종 단발 공격. 공격 중 수평 이동 정지.
- 공격 중 피격·사망 가능. 사망 우선.
- Attack·Hit 종료 후 Grounded 복귀.
- Death Animation 종료 후 오브젝트 삭제. 풀링은 제외.

[데이터·에셋]
- 수치: {지정값 / 임시값 사용 허용}
- 공격 대상: {Layer}, 충돌 대상: {Layer}
- 기존 공용 Monster Animator·Override·Prefab Variant 활용
- 완성 Prefab: Assets/Prefabs/Monster/{AgentName}.prefab
- 고유 Clip·Override·데이터: Assets/Prefabs/Monster/{AgentName}/
- 검증 Scene: {Scene 경로 / 기존 Scene 보존하고 임시 환경 사용}

[작업 범위]
- 코드·에셋·Prefab 연결과 가능한 동작 검증까지 진행.
- 기존 Controller·Factory·State·Transition 재사용 우선.
- 필요한 경우에만 전용 클래스 추가.
- 계획 문서: {경로}. 단계 완료 시 체크.
- 기존 파일 삭제와 기존 Scene 저장은 하지 않음.
- 수동 테스트 항목과 조정할 임시값을 알려 주세요.
- 리팩토링 로그 갱신: {요청 / 제외}
- Git 커밋: {요청 / 제외}. 푸시는 제외.
```

## 14. 작업 AI의 최종 보고 형식

최종 보고에는 다음 내용만 실제 결과에 맞춰 정리한다.

1. 제작 결과: 완성 Prefab 위치, 사용한 프로필, 추가 코드 필요 여부.
2. 변경 사항: 재사용·추가·수정·삭제 파일과 이유.
3. 사용 방법: Scene 배치, 연결할 데이터, 외부 명령 API.
4. 검증 결과: 정적·Editor·Play 확인 각각의 결과와 관련 로그.
5. 사용자 확인: 시각·타이밍·수치 조정과 재현 가능한 테스트 순서.
6. 미완료·제외 범위: 판단 AI, 확장 기능, 미연결 에셋, 문서·커밋 상태.

실제 실행하지 않은 테스트, 적용하지 않은 확장안, 사용자가 확인해야 할 항목을 완료로 표현하지 않는다.
