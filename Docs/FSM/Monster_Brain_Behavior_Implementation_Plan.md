# MonsterBrain 기반 Behavior 구현 계획

## 1. 목적과 현재 단계

기존 `Goblin.prefab`의 네 State FSM을 유지하고, 작은 C# `MonsterBrain`을 추가해 대상 감지 → 추적 → 단발 공격을 자동 수행한다.

이번 요청의 결과물은 **계획 문서**다. 아래 코드·Prefab 변경과 테스트는 아직 수행하지 않았다. 구현·검증한 항목만 이후 완료 체크한다.

- 기준 프로젝트: `E:/Unity/Project/ProjectBase`
- 대상 Prefab: `Assets/Prefabs/Monster/Goblin.prefab`
- 기본 Monster 제작 문서: `Docs/FSM/Agent_Extenstion_Monster.md`
- 제작 기준: `Docs/FSM/ProjectSkill/FSM_Agent_Creation_Guide.md`
- 기준 확인일: 2026-10-05

기존 Monster 제작 문서의 Behavior 제외 항목은 **기본 FSM 제작 당시의 범위**다. 이번 계획은 완성된 기본 Monster 위에 판단 계층을 추가하는 후속 작업이며, 이전 제작 결과를 수정하거나 Behavior가 이미 구현된 것으로 기록하지 않는다. `Agent_Extension_Plan.md`의 과거 클래스·계약 제안보다 현재 소스와 본 계획을 우선한다.

## 2. 구현 범위와 결정사항

### 이번에 구현할 기능

- 장애물에 가려지지 않은 생존 Player 감지.
- 대상이 없으면 대기, 같은 높이의 대상이 멀면 x축 추적.
- 공격 거리 안에서는 정지하고 대상 방향으로 단발 공격 요청.
- 실제 Attack 진입을 기준으로 공격 간격 관리.
- Attack·Hit·Death 중 새로운 이동·공격 의사결정 중지.
- 대상 이탈·사망·Brain 비활성화 시 이전 이동 명령 정리.
- 기존 `AttackTransition`·`AttackEndTransition`·공격 Animation Event 재사용.

### 이번에 구현하지 않을 기능

- Unity Behavior Graph, Blackboard, 커스텀 Action 노드.
- 순찰, 도주, 마지막 위치 기억, 경계 상태, 복수 공격 선택.
- 새로운 FSM State·Transition, 콤보, 공격 입력 버퍼.
- Jump·Fall·Dash·낭떠러지 회피·플랫폼 간 경로 탐색.
- 새 Brain 인터페이스, AI 전용 공용 Controller 계층, 전용 설정 SO.
- AIPlayer·기존 Behavior Graph 리팩토링과 패키지 변경.
- Goblin Sprite·Clip·Animator·Override·전투 수치 변경.
- 기존 Scene 저장·맵 등록, 리팩토링 로그·PDF 갱신, Git 커밋·푸시.

### 설계 결정

| 항목 | 선택 | 이유 |
| --- | --- | --- |
| 판단 구현 | `MonsterBrain : MonoBehaviour` 한 클래스 | Inspector 설정·비활성화를 지원하면서 작은 정책 유지 |
| 실제 실행 루프 | MonsterController가 `Tick(deltaTime)` 호출 | Brain과 FSM의 Update 실행 순서 고정 |
| 의존성 | `Initialize(...)`로 구체 컴포넌트 주입 | 기존 프로젝트 초기화 방식과 통일, 불필요한 Action·인터페이스 제외 |
| 판단 결과 | MonsterInput의 기존 이동·공격 API | 입력과 State 실행의 책임 유지 |
| 추적·대기 | 기존 GroundedState에서 실행 | 별도 Chase·Idle FSM State 불필요 |
| 공격 타입 | 기존 1번 공격 유지 | 다중 공격 입력 계약 변경 제외 |
| 쿨다운 기준 | 실제 AttackState 진입 시점 | 거절된 요청에 공격 간격이 소비되지 않음 |
| 설정 위치 | Brain·Detector의 private SerializeField | 초기에는 별도 데이터 계층 불필요 |
| Prefab 적용 | Goblin Variant에 Brain·PlayerDetector 추가 | BasicMonster는 외부 명령 기반 템플릿으로 유지 |

## 3. 실제 코드베이스 확인 결과

| 파일 | 현재 역할·제약 | 이번 계획 |
| --- | --- | --- |
| `Assets/Scripts/FSM/NPC/AIMonstor/@Hub/MonsterController.cs` | AgentController 직접 상속, 네 State 구성. TryStartAttack은 생존·1번 데이터 판정과 타입 설정 | 선택적 Brain 초기화·Tick 연결·실제 공격 진입 통보 |
| `Assets/Scripts/FSM/NPC/AIMonstor/Input/MonsterInput.cs` | SetMovement는 x 입력만 보관, RequestAttack은 인자 없는 Action 발행 | 그대로 재사용 |
| `Assets/Scripts/FSM/NPC/AIMonstor/MonsterState/MonsterStateFactory.cs` | Grounded: Death → Hit → Attack. Attack: Death → Hit → End. Hit: Death → End | State·Rule·등록 순서 유지 |
| `Assets/Scripts/FSM/Agent/@Hub/AgentController.cs` | 현재 State는 protected. Update가 Execute 호출. ChangeState가 Exit·Enter 수행 | 읽기 전용 `IsState<TState>()` 추가 |
| `Assets/Scripts/FSM/Agent/Move/2D/AgentMotor2D.cs` | Move 내부에서 private Turn 호출. 정지한 상태의 방향 전환 API 없음 | 기존 Turn을 public으로 개방해 이동 없이 방향 정렬 |
| `Assets/Scripts/FSM/@Detector/PlayerDetector.cs` | 원형 감지·Linecast·Target 제공. 첫 후보 선택, Gizmo에서 감지 함수를 호출해 Target 변경 | 기존 API를 유지하며 대상 선택·생존 판정·Gizmo 부작용 정리 |
| `Assets/Scripts/FSM/GroundedAgent/StateControl/States/GroundedState.cs` | 이동 입력으로 Motor·MoveSpeed 갱신 | 대기·추적 실행에 재사용 |
| `Assets/Scripts/FSM/NPC/AIMonstor/MonsterState/States/MonsterAttackState.cs` | AttackState 확장, 진입 시 수평 속도 정지 | 그대로 재사용 |
| `Assets/Scripts/FSM/NPC/AIMonstor/MonsterState/States/MonsterHitState.cs` | HitState 확장, 진입 시 수평 속도 정지 | 그대로 재사용 |
| `Assets/Scripts/FSM/Agent/StateControl/TransitionRules/Attack/AttackTransition.cs` | 요청 플래그 수신 후 TryStartAttack 성공 시 전이. 플래그는 Unsubscribe에서 정리 | 그대로 재사용. Brain이 별도 전이를 만들지 않음 |
| `Assets/Scripts/FSM/Agent/StateControl/TransitionRules/Attack/AttackEndTransition.cs` | 실제 End Event 수신 후 Monster는 Grounded 고정 복귀 | 그대로 재사용 |
| `Assets/Scripts/FSM/Agent/Combat/Health.cs` | CurrentHealth·IsDead 제공 | Brain은 현재 생존 값 조회, 새 UniRx 구독 불필요 |
| `Assets/Scripts/FSM/Agent/Handler/AgentCombatHandler.cs` | 공격 타입·box 범위·OnFrame 피해 적용 | 변경하지 않음 |

앞선 Editor 확인에서 Goblin Root에는 MonsterController·Input·Animator 어댑터·Motor·Health·Combat·Rigidbody2D·Collider가 있고, Visual에는 실제 Animator·SpriteRenderer·AnimationEventProxy가 있었다. Brain·Detector는 없었다. 구현 시작 시 현재 연결을 다시 확인한다.

주의: 기본 Monster의 GroundedState는 **기본 이동 State**다. 실제 지면 접촉을 판정하지 않으며, Monster에는 GroundDetector·FallState가 없다. 첫 검증은 평평한 동일 플랫폼에서 수행한다.

## 4. 목표 구조와 책임

```text
PlayerDetector
  └─ 대상 탐색·생존·장애물 확인 → Target

MonsterBrain
  ├─ 현재 State·대상 거리·높이·공격 간격 확인
  ├─ Motor.Turn: Grounded에서 공격 전 방향 정렬만 수행
  └─ MonsterInput.SetMovement / RequestAttack

MonsterController
  ├─ Brain Initialize / Tick 호출
  ├─ 기존 FSM Execute
  └─ 실제 Attack 진입 결과 → Brain.NotifyAttackStarted

MonsterInput → AttackTransition → MonsterAttackState
  └─ 기존 OnFrame 피해 적용 / End Event 복귀
```

### 책임 경계

- Detector는 관측만 수행한다. 공격 여부·State 전이를 결정하지 않는다.
- Brain은 행동 의도·AI 공격 간격을 관리한다. `ChangeState()`·TryStartAttack·ApplyAttackType·PerformAttack·Animator 설정을 직접 호출하지 않는다.
- 방향 정렬은 Motor의 기존 방향 기능만 사용한다. Brain이 Transform scale·Rigidbody 속도를 직접 수정하지 않는다.
- Input은 값·요청을 전달한다. 감지·쿨다운·상태 검사 코드를 넣지 않는다.
- Controller는 의존성 주입·실행 순서·State 결과 전달을 맡는다. 추적·거리 판단 코드를 Controller에 모으지 않는다.
- State·Transition은 기존 실행과 우선순위를 유지한다. 타깃 검색이나 AI 쿨다운을 공용 AttackTransition에 넣지 않는다.

## 5. MonsterBrain 구성

### 파일과 초기화

- 신규 파일: `Assets/Scripts/FSM/NPC/AIMonstor/@Behavior/MonsterBrain.cs`
- namespace: `ProjectRE`
- Unity.Behavior Action 노드가 아닌 일반 MonoBehaviour다.
- 자체 `Update()`·`FixedUpdate()`·Coroutine 루프를 만들지 않는다.

초기화 시그니처는 다음 구성을 사용한다. 모두 실제로 사용하는 의존성이며 Func·Action 래핑이나 새 인터페이스를 추가하지 않는다.

```csharp
public void Initialize(
    MonsterController controller,
    MonsterInput input,
    PlayerDetector detector,
    AgentMotor2D motor);

public void Tick(float deltaTime);
public void NotifyAttackStarted();
```

### 보관할 값

| 종류 | 값 | 용도 |
| --- | --- | --- |
| 의존성 | Controller | 생존 값·읽기 전용 State 판별 |
| 의존성 | Input | 이동 명령·공격 요청 전달 |
| 의존성 | Detector | 현재 감지 대상 |
| 의존성 | Motor | 정지 상태에서 대상 방향 정렬 |
| Inspector | attackDistance | x축 추적을 멈추고 공격을 요청하는 거리 |
| Inspector | maxAttackHeightDifference | 다른 높이의 대상에게 공격·무리한 추적 제한 |
| Inspector | attackInterval | 실제 공격 시작 사이의 최소 간격 |
| 런타임 | remainingAttackInterval | 남은 공격 간격 |

- 함수 내부 값은 private, 외부 호출 API만 public으로 둔다.
- 대상은 해당 Tick의 지역 변수와 Detector.Target을 활용한다. 초기 구현에 별도 TargetContext·Blackboard·Queue·입력 예약 플래그를 만들지 않는다.
- 현재 State로 충분한 `_isAttacking`·`_isDead` 값을 Brain에 중복 보관하지 않는다.
- Initialize에서 null·유효 설정·1번 공격 데이터 존재 여부를 확인한다. 실패 시 명확한 오류를 전달하고 정상 초기화되지 않은 Brain을 Tick하지 않도록 연결한다.
- 초기화 여부만 보관하는 bool을 불필요하게 추가하지 않는다. 필수 참조와 호출 수명으로 관리한다.
- 주석은 간결한 단답형으로 작성하고, 클래스·외부 API에 필요한 summary만 추가한다.

### 수치 정책

본 문서에서는 최종 수치를 확정하지 않는다. 구현 시 검증용 값과 조정 근거를 기록하고, 사용자가 Inspector에서 조정한다.

- 감지 반경은 PlayerDetector의 기존 viewRadius를 사용한다.
- attackDistance는 Goblin 1번 AttackData의 offset·size와 양쪽 Collider를 함께 보고 설정한다. AI 요청 거리와 실제 피해 범위를 동일한 개념으로 취급하지 않는다.
- 높이 기준은 두 Root의 기준점·발 위치를 확인한다. Collider 중심과 발 기준 좌표를 혼용하지 않는다.
- 공격 간격은 시작 기준이다. Clip보다 짧아도 Attack 상태에서는 새 공격이 발생하지 않으며, 길면 Grounded 복귀 후 남은 간격 동안 대기한다.
- 쿨다운은 `Mathf.Max(0f, remaining - deltaTime)`로 감소시켜 음수 누적을 막고 `<= 0f`로 완료 판정한다. clamp로 0을 보장하는 경우 임의 epsilon을 추가하지 않는다.
- 거리·높이 경계에는 단위와 크기에 맞는 작은 허용 오차를 둔다. 필요 이상으로 공격 범위를 확대하거나 모든 float 비교에 동일한 오차를 강제하지 않는다.

## 6. Tick의 판단 순서

| 순서 | 조건 | 명령·처리 |
| --- | --- | --- |
| 1 | 매 Tick | 남은 공격 간격 감소 |
| 2 | 사망 또는 GroundedState가 아님 | 이동 입력 zero, 감지·방향 변경·공격 요청 중지 |
| 3 | 생존·시야 유효 대상 없음 | 이동 입력 zero, 대기 |
| 4 | 높이 차이가 허용 범위 밖 | 이동 입력 zero, 대기. 플랫폼 이동 시도 없음 |
| 5 | x축 거리가 공격 거리 밖 | 대상 방향으로 x=±1 이동 입력 |
| 6 | 공격 거리 안 | 이동 입력 zero, 대상 쪽 방향 정렬 |
| 7 | 거리 안이고 간격 완료 | RequestAttack 1회 발행 |

추가 정책:

- Attack·Hit에서는 이동 **명령값만** zero로 정리한다. Brain이 Motor.StopHorizontal을 반복 호출해 기존 넉백·물리를 덮어쓰지 않는다.
- 공격 시작 후 대상이 움직이거나 사라져도 Brain이 현재 공격을 강제로 취소하지 않는다. 실제 타격 시점의 범위 판정으로 명중 여부를 결정한다.
- Attack 중 방향은 고정한다. 매 프레임 타깃을 따라 회전시켜 타격 방향이 바뀌지 않게 한다.
- x축 차이가 방향 판별 허용 오차 이내면 현재 방향을 유지한다. 거의 같은 위치에서 좌·우가 반복 전환되지 않게 한다.
- 첫 구현은 가시 대상 중 가장 가까운 대상 선택을 사용한다. 대상 기억·선택 고정은 제외한다.
- 대상이 사라졌는데 마지막 이동 명령이 유지되지 않도록 모든 대기 경로에서 zero를 전달한다.

## 7. Controller 연결과 실제 공격 진입 확인

### 7.1 선택적 Brain 초기화

MonsterController.Awake에서 기존 base 초기화·Animator·Factory 구성을 보존한 뒤, 같은 Root의 Brain과 PlayerDetector를 확인하고 Initialize한다.

- Brain이 없는 Monster는 기존 외부 명령 방식 그대로 동작한다.
- MonsterController에 Brain을 강제하는 RequireComponent를 추가하지 않는다.
- Brain이 있는데 Detector가 없거나 설정이 잘못되면 조용히 자동 판단을 생략하지 말고 오류를 전달한다.
- MonsterController.Awake에서 주입하고, Start의 최초 Grounded 진입 후 Tick을 시작한다.
- 초기화 중 `FindObjectOfType`·태그 검색·전역 PlayerController 접근을 사용하지 않는다.

### 7.2 실행 순서

MonsterController.Update를 확장한다.

```text
초기화된 Brain이 활성화된 경우 Tick(Time.deltaTime)
  → base.Update()
  → 기존 State.Execute에서 Transition 평가·행동 실행
```

- Brain은 Grounded에서 이미 구독 중인 AttackTransition에 요청한다.
- 같은 Controller Update의 FSM 평가에서 요청을 처리한다.
- base.Update는 한 번만 호출한다. Brain이 FSM Execute를 다시 호출하지 않는다.
- Brain이 없거나 비활성화되어도 FSM은 계속 실행한다.
- Hit·Death가 같은 프레임에 충족되면 기존 Factory 순서에 따라 공격보다 먼저 처리한다.

### 7.3 Attack 진입 통보

MonsterController.ChangeState를 재정의하되, 기존 전환은 base.ChangeState에 맡긴다.

1. 전환 전 Attack 계열인지 읽기 전용으로 확인한다.
2. base.ChangeState(stateType)를 호출한다.
3. **비-Attack → 실제 Attack 계열 진입**이 확인되면 Brain.NotifyAttackStarted를 호출한다.
4. Brain은 remainingAttackInterval을 attackInterval로 설정한다.

- 요청 타입 이름만 보고 성공으로 판단하지 않는다. 미등록 목적지·거절된 요청에는 통보하지 않는다.
- 일반적인 상태 재설정·같은 Attack 유지에는 시작 간격을 반복 갱신하지 않는다.
- 통보는 Controller와 Brain 사이의 작은 직접 호출로 구성한다. 초기 버전에 공용 OnStateChanged 이벤트·추가 인터페이스를 만들지 않는다.
- Brain은 NotifyAttackStarted에서 공격 적용이나 State 전이를 수행하지 않는다.

### 7.4 State 판별

AgentController에 아래 읽기 전용 함수를 추가한다.

```csharp
public bool IsState<TState>() where TState : AgentStateBase
    => _currentState is TState;
```

- Dictionary·현재 State 참조를 외부에 공개하지 않는다.
- typeof(AttackState)와 `_currentState.GetType()`의 정확한 일치로 비교하지 않는다. 실제 값은 MonsterAttackState이며 Player도 파생 AttackState를 사용한다.
- 공통 실행·전환·종료 수명은 변경하지 않는다.

## 8. Detector와 방향 기능의 최소 보완

### PlayerDetector

기존 `IsTargetInView()`·`Target`·SerializeField 이름을 유지한다. 이 컴포넌트는 AIPlayerBrain도 사용하므로 호출 계약과 직렬화 값을 보존한다.

- 반경 내 후보 중 활성 상태이며 생존한 Player를 고른다. Health는 후보의 부모 계층까지 확인한다.
- 장애물 Linecast를 통과한 후보 중 가장 가까운 대상을 선택한다.
- Goblin Root가 발 위치인 점을 고려해 Linecast 시작·종료를 지면 위의 몸체 기준으로 확인한다. 자기 Collider가 있으면 bounds.center를 시작점, 대상 Collider의 bounds.center를 종료점으로 사용하고, 자기 Collider가 없을 때만 기존 Transform 위치를 시작점으로 사용한다. 발 위치 사이의 선이 지형에 닿아 같은 플랫폼의 Player를 항상 차폐 대상으로 판단하지 않게 검증한다.
- Player의 복수 Collider가 후보에 나타나도 피해·공격 요청을 Collider 수만큼 발행하지 않는다.
- 기존 Target의 Collider Transform 전달 계약은 임의로 Root Transform으로 바꾸지 않는다. Brain에서 높이 기준점이 필요한 경우 부모 Health가 있는 Agent Root를 별도로 확인한다.
- 후보가 없으면 Target을 null로 정리한다. 기존 대상이 파괴·비활성화·사망한 경우에도 즉시 제외한다.
- OnDrawGizmos는 감지 함수를 호출하지 않고 반경과 마지막 감지 결과만 표시한다. Scene View가 런타임 Target을 변경하지 않게 한다.
- 사용하지 않는 dirToTarget 지역 변수는 정리한다.
- 첫 구현은 기존 OverlapCircleAll을 유지한다. NonAlloc 버퍼·별도 감지 스케줄러는 프로파일링 후 검토한다.
- 감지는 Brain.Tick에서 필요한 Grounded 구간에 한 번 호출한다. Detector에 별도 Update를 추가하지 않는다.

공유 Detector의 후보 선택·사망 제외 변경은 AIPlayer에도 영향을 줄 수 있다. 관련 호출부 회귀를 확인하며, AIPlayerBrain의 unrelated 로직까지 리팩토링하지 않는다.

### AgentMotor2D

- 기존 private Turn(Vector2)를 public으로 개방한다.
- Move가 기존과 동일하게 Turn을 호출하도록 유지한다.
- Brain은 Grounded에서 공격 전 Turn을 호출하고, 속도 변경은 State에 맡긴다.
- SetFacing·FaceTarget 등의 동등한 새 함수를 추가하지 않는다.
- FacingDirection 캐시 변수를 다시 만들지 않는다. 현재 Root scale 방향 규칙을 유지한다.
- Player 이동·방향·Dash 회귀를 확인한다. Visual 크기는 변경하지 않는다.

## 9. 입력·수명·요청 잔류 정책

- MonsterInput의 Action 계약과 SetMovement는 변경하지 않는다.
- Brain은 Attack·Hit·Death에서 RequestAttack을 호출하지 않는다. 다음 공격을 미리 예약하지 않는다.
- 공격 종료 후 대상·거리·간격을 **다시 판단**해 새로운 요청을 발행한다. 이는 자동 의사결정이지 공격 입력 버퍼가 아니다.
- AttackTransition 플래그는 기존 State.Exit의 Unsubscribe에서 해제한다. Brain이 직접 Rule 플래그를 정리하지 않는다.
- TryStartAttack은 Health·데이터 확인과 타입 선택이라는 기존 책임을 유지한다. 거기에 AI 거리·쿨다운 코드를 섞지 않는다.
- Initialize에서 정상적인 1번 공격 데이터를 확인한다. 데이터 누락으로 TryStartAttack이 계속 실패하는 요청 스팸을 정상 동작으로 허용하지 않는다.
- 현재 AttackTransition은 Grounded에서 시작이 거절되면 요청 플래그를 유지한다. 이번에 이를 수정하지 않으며, 실행 중 공격 데이터를 교체·차단하는 기능은 범위 밖이다. 해당 기능이 추가되면 실패 요청의 폐기·재시도 정책을 별도로 설계한다.
- Brain.OnDisable은 주입된 Input이 존재할 때 이동 명령을 zero로 정리한다. 직접 State.Exit이나 Animation 초기화를 호출하지 않는다.
- Brain을 끄는 것은 **자동 판단을 끄는 것**이다. 진행 중 Attack·Hit와 FSM·물리는 계속 실행한다.
- 간격은 Brain이 활성화된 Tick에서 감소한다. 비활성화로 간격을 즉시 초기화하지 않는다.
- 새 UniRx·Action 구독이 없으므로 Brain에 CompositeDisposable·등록/해제 계층을 추가하지 않는다.
- GameObject 파괴 시 기존 AgentController.OnDestroy의 State·Rule 정리를 유지한다. 풀링·Root 비활성화 후 FSM 재초기화는 이번 범위에서 보장하지 않는다.

## 10. 변경 파일과 Prefab 적용

| 구분 | 경로 | 변경 내용 |
| --- | --- | --- |
| 신규 | `Assets/Scripts/FSM/NPC/AIMonstor/@Behavior/MonsterBrain.cs` | 작은 판단 클래스·Inspector 설정·Tick·공격 시작 결과 처리 |
| 수정 | `Assets/Scripts/FSM/NPC/AIMonstor/@Hub/MonsterController.cs` | 선택적 Initialize, Brain → FSM 실행 순서, 실제 Attack 진입 통보 |
| 수정 | `Assets/Scripts/FSM/Agent/@Hub/AgentController.cs` | 읽기 전용 IsState<TState> |
| 수정 | `Assets/Scripts/FSM/Agent/Move/2D/AgentMotor2D.cs` | 기존 Turn 접근자 변경 |
| 수정 | `Assets/Scripts/FSM/@Detector/PlayerDetector.cs` | 가장 가까운 생존 대상·읽기 전용 Gizmo 표시 |
| 수정 | `Assets/Prefabs/Monster/Goblin.prefab` | Root에 Brain·PlayerDetector 추가, 값·Mask 연결 |
| 갱신 | `Docs/FSM/Monster_Brain_Behavior_Implementation_Plan.md` | 구현 체크·검증 결과·남은 조정값 기록 |
| 생성 동반 | 신규 C#·@Behavior 폴더의 `.meta` | Editor가 생성한 GUID 유지 |

다음은 변경하지 않는다.

- BasicMonster.prefab, MonsterInput, Factory·FactoryData·기존 State·Rule.
- MonsterAnimator·AnimationData·Goblin Clip·Override·Stat/Motor SO.
- Player·AIPlayer의 전용 코드, 원본 외부 아트 에셋.

Prefab 연결 기준:

- Brain과 Detector는 Goblin Root에 추가한다. 실제 Unity Animator·Proxy는 Visual의 기존 한 개를 유지한다.
- Detector의 playerMask는 실제 Player Collider Layer, obstacleMask는 기존 지형·벽 Layer와 대조한다.
- 자기 자신·Enemy·배경 Sprite만 있는 오브젝트는 대상에서 제외한다.
- Brain attackDistance·높이 기준을 기존 공격 box와 실제 Collider에 맞춰 조정한다.
- Variant 수정은 Goblin에만 저장한다. 공용 템플릿으로 Apply하지 않는다.
- Scene 자동 배치·맵 설정 등록은 하지 않는다. Play 검증이 필요하면 원래 Scene을 보존하는 임시 환경을 사용한다.

## 11. 순차 구현 체크리스트

### Phase 0 — 현재 소스·참조 재확인

- [ ] Git 상태·사용자 변경·프로젝트 지침 확인.
- [ ] 본 문서의 함수·클래스·경로를 현재 소스와 대조.
- [ ] Goblin Variant·실제 Animator·Proxy·1번 AttackData·Layer 연결 확인.
- [ ] PlayerDetector 공유 호출부와 AgentMotor2D 이동·방향 사용처 확인.
- [ ] 검증용 거리·높이·간격 값 및 테스트 위치 기록.

### Phase 1 — 최소 공통 API와 Detector

- [ ] AgentController에 읽기 전용 IsState<TState> 추가.
- [ ] AgentMotor2D의 기존 Turn을 public으로 변경하고 Move 동작 유지.
- [ ] PlayerDetector의 가장 가까운 생존 후보 선택·Target null 정리 구현.
- [ ] Gizmo의 Target 갱신 부작용 제거.
- [ ] 발 위치·Collider 중심과 Linecast를 대조해 같은 플랫폼 감지가 지형에 가려지지 않는지 확인.
- [ ] 기존 SerializeField·Target Transform 계약 보존 확인.
- [ ] 컴파일과 공유 컴포넌트 회귀 확인.

### Phase 2 — 작은 MonsterBrain

- [ ] @Behavior/MonsterBrain.cs 추가, ProjectRE namespace 적용.
- [ ] Initialize 의존성·필수 설정·1번 공격 데이터 검증 구현.
- [ ] 간격 감소·State 제한·감지·높이·거리 판단을 순서대로 구현.
- [ ] 추적·정지·공격 전 방향 정렬·단발 요청 구현.
- [ ] NotifyAttackStarted에서 간격 설정만 수행.
- [ ] OnDisable에서 이동 명령 정리.
- [ ] 자체 Update·직접 전이·중복 상태 bool·새 인터페이스가 없는지 확인.

### Phase 3 — MonsterController 연결

- [ ] 기존 Awake 흐름 뒤 선택적 Brain.Initialize 연결.
- [ ] 활성 Brain.Tick → base.Update 순서로 Update 구성.
- [ ] 실제 비-Attack → Attack 진입 뒤 NotifyAttackStarted 전달.
- [ ] Brain이 없는 기존 Monster의 외부 명령 방식 유지 확인.
- [ ] Factory의 Death → Hit → Attack 우선순위 보존 확인.
- [ ] 구독·State Execute·공격 피해 중복 호출이 없는지 확인.

### Phase 4 — Goblin Variant 연결

- [ ] Editor API로 Goblin에 Brain·PlayerDetector 추가.
- [ ] Player·장애물 Mask와 검증용 설정 연결.
- [ ] Variant 상속·GUID·Visual Animator·Proxy 연결 보존 확인.
- [ ] BasicMonster·Scene·맵 설정·Goblin 모션·전투 수치를 변경하지 않았는지 확인.

### Phase 5 — 검증과 인수인계

- [ ] 컴파일·Console·Prefab 참조 검사.
- [ ] 아래 자동/Play 검증 시나리오 수행.
- [ ] 공용 Detector·Motor·State 판별 변경의 Player/AIPlayer 회귀 확인.
- [ ] 임시 오브젝트·테스트 파일 정리, 원래 Scene 상태 보존 확인.
- [ ] 사용자 수동 확인과 아직 미검증인 항목 분리.
- [ ] 실제 완료 항목만 체크하고 결과·수치·제한사항 기록.

## 12. 검증 시나리오

| 시나리오 | 기대 결과 | 확인 방식 |
| --- | --- | --- |
| Goblin 최초 생성 | 기존 Grounded 진입 후 판단 시작, 초기화 순서 오류 없음 | Editor / Play |
| Player 없음 | 이동 입력 zero, Idle 표현, 공격 요청 없음 | Play |
| 좌·우 감지 대상 | 대상 방향 추적, MoveSpeed·방향 일치 | Play |
| 대상이 시작부터 뒤쪽·공격 거리 안 | 이동 없이 올바른 방향으로 공격 | Play / 수동 |
| 거리는 가깝고 높이는 다름 | 추적·공격 중지, 플랫폼 간 이동 시도 없음 | Play |
| 벽이 사이에 있음 | 시야 제외, 이전 이동 명령 zero | Play |
| 대상 감지 범위 이탈·삭제·비활성화·사망 | Target 정리, 추적·새 공격 중지 | Play |
| 복수 유효 Player | 가장 가까운 생존·가시 후보 선택 | Detector 검증 |
| Player 복수 Collider | Brain의 요청·이동 판단이 Collider 수로 중복되지 않음 | Detector / Play |
| 공격 진입 | 1번 타입, 수평 속도 정지, 실제 진입 때 간격 시작 | Play |
| 공격 OnFrame / End | 기존 프레임 피해·Grounded 복귀 유지 | 실제 Clip Play |
| 요청·피격·사망 동시 충족 | 기존 Death → Hit → Attack 순서 유지 | Play |
| Attack·Hit 중 대상 방향 변경 | 새 요청·방향 변경 없음, 넉백 덮어쓰기 없음 | Play |
| 공격 종료 후 간격 남음 | Grounded에서 대기, 완료 후 대상 조건 재평가 | Play |
| 공격 중 대상 이탈 | 현재 모션 정상 종료, 이후 자동 공격 없음 | Play |
| 공격 종료 뒤 대상 유지 | 간격·조건 충족 시 새 요청으로 반복. 입력 버퍼 사용 없음 | Play |
| Brain만 비활성화 | 이동 명령 정리, FSM·진행 중 모션은 유지 | Play |
| Brain 없음 | 기존 외부 SetMovement·RequestAttack 정상 동작 | Play |
| Scene Gizmo on/off | 런타임 대상 선택 결과가 Gizmo로 바뀌지 않음 | Editor / Play |
| 거리·높이 경계·거의 같은 x좌표 | 불필요한 방향 떨림·정확한 float 일치 의존 없음 | 경계값 테스트 |
| 실제 파생 State | MonsterAttackState를 AttackState 계열로 정상 판별 | 코드 / Play |
| Player 회귀 | 이동·방향·Jump/Dash·공격 흐름 유지 | 관련 Play / 수동 |
| AIPlayer 회귀 | 기존 PlayerDetector API·직렬화 참조·대상 사용 유지 | 코드 / 가능한 Play |

검증한 Console의 새 오류와 기존 오류를 구분한다. Editor 연결 실패·미실행 Play 검증은 성공으로 기록하지 않는다. 임시 preview·End Event 수동 호출은 실제 게임 입력·실제 Clip Event 재생 검증을 대체하지 않는다.

### 사용자 수동 확인

- [ ] Goblin이 공격 범위에 도달했을 때 정지 위치·무기 범위가 자연스러운지 확인.
- [ ] 가까운 Player가 뒤에 있을 때 공격 방향 확인.
- [ ] 공격 간격·추적 속도·Idle/Move 표현이 의도에 맞는지 확인.
- [ ] 대상 이탈·피격·사망 후 불필요한 추적·자동 공격이 남지 않는지 확인.
- [ ] 거리 경계에서 좌·우·Idle/Move가 반복해서 떨리지 않는지 확인.

속도·공격 box·Clip 타이밍 조정이 추가로 필요하면 사용자와 범위를 확인한다. Brain 구현을 이유로 기존 SO·모션을 임의 변경하지 않는다.

## 13. 완료 기준과 이후 확장

### 완료 기준

- 새 판단 클래스는 MonsterBrain 하나이며 기존 네 State·Attack 입출력 유지.
- 감지·판단·입력·실행 책임이 분리되어 있음.
- Brain은 외부 전이·피해·Animator 제어를 하지 않음.
- 실제 Attack 진입으로 공격 간격이 시작됨.
- Hit·Death 우선순위와 공격 종료 Event 유지.
- 대상 이탈·사망·Brain 비활성화 후 이동 명령 잔류 없음.
- Goblin에만 자동 판단 적용, BasicMonster 외부 명령 방식 보존.
- 공용 변경의 회귀·수동 대기·미검증 결과를 구분해 기록.

### 이후 확장 시점

- 순찰만 추가: 대상이 없는 분기의 정책 확장부터 검토. 바로 PatrolState를 추가하지 않는다.
- 도주·경계·대상 기억·여러 공격 선택 증가: C# 판단 코드가 복잡해지는 시점에 Behavior Graph 전환 검토.
- Graph 전환 시 감지 → 입력 → FSM 연결과 기존 State는 재사용하고, **의사결정 소유자는 한 곳**으로 둔다. C# Brain과 Graph가 동시에 이동·공격을 결정하게 하지 않는다.
- 플랫폼 이동·낭떠러지·점프가 필요: 현재 Grounded 이름만으로 대응 가능하다고 가정하지 않고 Detector·이동·Fall 등 별도 계획 작성.
- 다중 공격이 필요: int AttackType 입력·Animator 분기·시작 판정 정책을 별도 계획으로 확장. 이번 1번 단발 정책에 미리 구현하지 않는다.

### 구현 요청 예시

> `Docs/FSM/Monster_Brain_Behavior_Implementation_Plan.md`와 현재 소스를 확인하고 Phase 0부터 순서대로 구현해 주세요. 작은 C# MonsterBrain으로 Goblin의 감지·추적·단발 공격을 연결하고, 기존 네 State·AttackTransition·End Event를 유지해 주세요. Behavior Graph·순찰·새 State·기존 Scene 저장·전투 수치 변경·Git 커밋은 하지 말고, 실제 완료 항목만 체크하고 수동 확인 항목을 알려 주세요.
