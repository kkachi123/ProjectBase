# Monster Behavior · Handler · Blackboard · Action 연결 정리

현재 기준: Monster Blackboard·행동 흐름 단순화 적용. 10~15절은 이전 구조의 구현·검증 이력이며, 최신 적용 결과와 수동 확인 항목은 16절 기준.

적용 대상은 2D Grounded Monster. Grounded·Attack·Hit·Death FSM에 입력을 전달하며, 동일 층의 수평 이동을 기준으로 함. 비행·점프 경로·낙하 후 상층 복귀는 구현 범위가 아님.

스크립트 기본값과 Prefab·Scene Inspector 값은 구분. 기존 Inspector 필드·값·Override는 유지.

## 1. 책임과 연결 구조

| 구성 요소 | 책임 |
|---|---|
| ScenePlayerManager | Inspector에 지정한 씬 Player 참조 제공 |
| PlayerDetector | 주입 Collider 중심까지의 XY 거리·장애물 판정 |
| Target Handler | Player 참조, 생존·활성·거리·시야·높이 판정, Root·DeltaX 제공 |
| Attack Handler | 수평 공격 거리, 배후 제한 시간, 개체별 공격 요청 간격 관리 |
| ChaseReturn Handler | 최초 HomeX·도착 범위·교전 기록, LostTarget 갱신·복귀 완료 처리 |
| Patrol Handler | 순찰 반경 제공, 기본 WaitRange 설정 전달 |
| MonsterBehaviorBlackboard | Graph 분기·기본 대기에 필요한 타입 지정 실행 변수 접근 |
| MonsterBehaviorContext | 의존성 주입, 자기 사망 정보 전달, Handler 갱신 순서·접근자 제공 |
| Graph | 행동 순서·우선순위·공통 중단·대기/복귀 단계 관리 |
| Action | Context의 Handler 조회, 목적지·타이머 기록, 이동·공격 입력 발행 |

```text
ScenePlayerManager.Player → Context.BindPlayer → Target
PlayerDetector → Target.UpdateTarget → Blackboard.HasValidTarget
                                      ↓
                           ChaseReturn.UpdateState → LostTarget

Blackboard 상태·대기 설정 → Graph의 Guard / Wait / WaitRange
Context의 Handler        → 커스텀 Action의 정보·판정
Context.Input            → 이동·공격 요청 → 기존 FSM·Motor·Animator
```

Handler 4개와 wrapper는 개체별 일반 C# 객체. Handler는 Context·다른 Handler·Action을 역참조하지 않음. 별도 Component·인터페이스·기반 Action·Handler 자체 Update 추가 없음.

## 2. MonsterBehaviorContext

소스: [MonsterBehaviorContext.cs](E:/Unity/Project/ProjectBase/Assets/Scripts/FSM/NPC/AIMonstor/@Behavior/MonsterBehaviorContext.cs)

### 2.1 필드·접근자

| 필드·접근자 | 목적·사용처 |
|---|---|
| _attack / Attack | 공격 설정·판정·요청 시각. Chase·MoveBehindTarget·RequestAttack |
| _chaseReturn / ChaseReturn | 초기 위치·도착 범위·교전 기록. Chase·Patrol·MoveBehindTarget·Return |
| _patrol / Patrol | 순찰 반경. Patrol Action |
| _targetHandler / Target | 선택 대상·수평 거리. Chase·MoveBehindTarget·RequestAttack |
| _input / Input | 자기 MonsterInput 캐시. 모든 커스텀 Action의 입력 실행 |
| _detector | 자기 감지 Component. Target.Initialize에 주입 |
| _health | 자기 Health. 사망 구독의 원본 |
| _agent | 자기 BehaviorGraphAgent. 실행 Blackboard Bind |
| _bodyCollider | 자기 몸통 Collider. Target의 높이·수평 거리 기준 |
| _blackboard | Context·IsDead 기록과 초기화 경계 확인 |
| _deathSubscription | 자기 사망 구독 해제·재등록 |

Context에는 개별 거리·시간 설정을 대신 전달하는 getter 없음. Action은 필요한 Handler에 직접 접근.

### 2.2 생명주기·함수

| 함수 | 처리 |
|---|---|
| Awake | 자기 Component 한 번 캐시 |
| Start | wrapper Bind → Context 기록 → ChaseReturn·Patrol·Target 초기화 → 사망 구독 → 씬 Player 주입·최초 갱신 |
| OnEnable | 최초 활성화는 Start에 위임. 재활성화는 사망 구독 복구·Player 재주입 |
| BindPlayer | ScenePlayerManager.Instance.Player의 Health·같은 GameObject의 몸통 Collider 주입 후 RefreshState |
| Update / RefreshState | 자기 사망이면 Target.Clear, 아니면 UpdateTarget → ChaseReturn.UpdateState |
| SubscribeDeath | 이전 구독 Dispose 후 Health.IsDead 변경을 Blackboard.IsDead에 기록 |
| OnDisable | 초기화 경계 한 번 확인 → 사망 구독·대상 캐시 해제·이동 입력 초기화 |

DefaultExecutionOrder(-100)로 BehaviorGraphAgent(-50)보다 먼저 Start·Update 실행. Graph의 Awake 초기화 이후 실행 변수를 Bind하고, Graph Tick 전에 판정 갱신.

HomeX는 최초 Start에서만 기록. OnEnable에서 Handler를 다시 Initialize하지 않으며, 공격 요청 가능 시각도 유지.

### 2.3 씬 설정

- InGame의 ScenePlayerManager에 씬의 실제 PlayerController 인스턴스 할당.
- Manager·Inspector Player 누락은 초기화 때 한 번 알리고 Context 비활성화.
- Title 동적 등록, Player 교체·재생성, 생존 중 Health·Collider 교체는 지원 범위에서 제외.
- Player Health·몸통 Collider는 같은 GameObject에 배치.
- 기존 Managers가 필요한 UI·Player 사망 완료·상호작용 문제는 별도 범위.

## 3. MonsterBehaviorBlackboard

소스: [MonsterBehaviorBlackboard.cs](E:/Unity/Project/ProjectBase/Assets/Scripts/FSM/NPC/AIMonstor/@Behavior/MonsterBehaviorBlackboard.cs)

| 변수 | 기록 주체 | 소비처 |
|---|---|---|
| Context | Context.Start | 모든 커스텀 Action |
| IsDead | Context 자기 Health 구독 | 최우선 사망 Guard |
| HasValidTarget | Target.UpdateTarget / Clear | 교전 Guard, ChaseReturn.UpdateState |
| LostTarget | ChaseReturn.UpdateState / CompleteReturn | 상실 Guard |
| LostTargetWait | ChaseReturn.Initialize | 기본 Wait.SecondsToWait |
| PatrolWaitMin / PatrolWaitMax | Patrol.Initialize | 기본 WaitRange.Min / Max |
| Self | Unity 기본 소유자 설정 | Graph의 기본 변수. wrapper 관리 제외 |

Bind는 Self를 제외한 7개 실행 변수 참조를 한 번 확보. 이름은 wrapper의 상수에만 보관하고 Handler·Context에서 문자열이나 SetVariableValue를 사용하지 않음.

Graph 상태값의 Handler bool 복사본 없음. _isEngaged만 이전 교전 이력을 보관.

Blackboard Input·HomeX·AttackDistance·ArrivalDistance·AttackRequestInterval·NextAttackRequestTime·RearApproachTimeout·PatrolRadius와 NeedsReturn 제거. NeedsLostTargetWait는 GUID를 유지한 채 LostTarget으로 변경.

기존 미사용 Blackboard Target도 다시 추가하지 않음. 실제 대상 위치는 Context.Target.Root로 조회.

## 4. PlayerDetector

소스: [PlayerDetector.cs](E:/Unity/Project/ProjectBase/Assets/Scripts/FSM/@Detector/PlayerDetector.cs)

| 필드·함수 | 역할 |
|---|---|
| offset / viewRadius / obstacleMask | 기존 감지 원점·반경·장애물 설정 |
| IsWithinRange(target, out origin) | transform.position + offset에서 target.bounds.center까지 XY 제곱 거리 <= viewRadius² |
| HasLineOfSight(origin, target) | 원점에서 대상 중심까지 Linecast 장애물 확인 |
| OnDrawGizmos | 기존 빨간 감지 원 표시 |

거리·시야 정책 변경 없음. 범위 밖에서는 Linecast 미실행. 물리 Player 탐색·Overlap 배열·Component 검색·생존 판정·구독 없음. Collider 표면이 아닌 중심 거리 기준이며 추가 허용 오차 없음.

## 5. MonsterTargetBehaviorHandler

소스: [MonsterTargetBehaviorHandler.cs](E:/Unity/Project/ProjectBase/Assets/Scripts/FSM/NPC/AIMonstor/@Behavior/Handlers/MonsterTargetBehaviorHandler.cs)

| 필드·속성 | 목적·사용처 |
|---|---|
| _detector | 주입한 거리·시야 판정 Component |
| _bodyCollider | 자기 몸통 Collider. 높이와 자기 X 기준 |
| _maxTargetHeightDifference | 양쪽 bounds.min.y의 허용 높이 차이. 기본 0.75, Goblin 1 |
| _blackboard | HasValidTarget 직접 기록 |
| _cachedCollider / _health | 주입 Player 참조 한 쌍. 인식 해제 후에도 유지 |
| Root | 현재 감지된 Player Transform. 미감지 상태는 null |
| DeltaX | Root.position.x - 자기 몸통 Transform.position.x. 교전 Guard가 성립한 Action에서만 조회 |

| 함수 | 처리 |
|---|---|
| Initialize | Detector·자기 몸통·wrapper 주입 |
| BindPlayer | 기존 참조 Clear 후 Health·Collider 한 쌍 검증·보관 |
| UpdateTarget | Collider 소멸 확인 → 생존·활성 → 거리 → 시야 → Root 선택 → 높이를 포함한 HasValidTarget 기록 |
| IsWithinHeightRange | 양쪽 Collider 바닥 높이 차이 확인 |
| Clear | Root·HasValidTarget·Player 참조 전체 해제 |

미사용 공개 BodyCollider와 선택 전달용 SetTarget 제거. _cachedCollider는 Player, _bodyCollider는 자기 몸통으로 역할이 다르므로 유지.

범위·시야·생존·활성 조건 상실 시 Root만 해제하고 Player 캐시는 유지. 높이 부적합이면 Root는 유지하되 HasValidTarget은 false. 몬스터 사망·비활성화는 Clear로 캐시까지 해제.

## 6. 설정·판정 Handler

### 6.1 MonsterAttackBehaviorHandler

소스: [MonsterAttackBehaviorHandler.cs](E:/Unity/Project/ProjectBase/Assets/Scripts/FSM/NPC/AIMonstor/@Behavior/Handlers/MonsterAttackBehaviorHandler.cs)

| 필드·API | 목적·사용처 |
|---|---|
| _attackDistance / AttackDistance | 수평 공격 거리·배후 목적지 거리. Goblin 1.8 |
| _attackRequestInterval | 최소 요청 간격. Goblin 1초 |
| _rearApproachTimeout / RearApproachTimeout | 배후 접근 제한 시간. Goblin 1.5초 |
| _nextAttackRequestTime | 다음 요청 가능 시각. 비공개·개체별 기록 |
| IsInAttackRange(deltaX) | Abs(deltaX) <= _attackDistance. Chase에서만 공격 거리 확인 |
| CanRequestAttack | Time.time >= _nextAttackRequestTime 확인 |
| RecordAttackRequest | Time.time + 요청 간격 기록. 입력 발행 직전에 호출 |

Blackboard 초기화 의존성 제거. 같은 Handler를 모든 공격 경로가 사용하므로 재인식·Subgraph 재진입으로 요청 간격을 우회하지 않음. 실제 공격 가능 판정·실행은 FSM 담당.

### 6.2 MonsterChaseReturnBehaviorHandler

소스: [MonsterChaseReturnBehaviorHandler.cs](E:/Unity/Project/ProjectBase/Assets/Scripts/FSM/NPC/AIMonstor/@Behavior/Handlers/MonsterChaseReturnBehaviorHandler.cs)

| 필드·API | 목적·사용처 |
|---|---|
| HomeX | 최초 Start의 X. 순찰·복귀 Action |
| _returnArrivalDistance / ArrivalDistance | 도착 범위. Goblin 0.15. Patrol·Return·MoveBehindTarget |
| _lostTargetWait | 상실 대기 시간. 선언 기본 2초, Goblin 실제 1.5초 |
| _blackboard | HasValidTarget 조회·LostTarget 및 Wait 설정 기록 |
| _isEngaged | 최초 미감지와 교전 이후 상실 구분 |
| Initialize(homeX, blackboard) | HomeX 저장·LostTargetWait 전달·LostTarget 초기화 |
| UpdateState | LostTarget = _isEngaged && !HasValidTarget |
| BeginEngagement | Chase 시작 이력 기록 |
| CompleteReturn | 교전 이력·LostTarget 초기화 |

BeginReturn과 별도 복귀 상태 제거. Graph Sequence가 정지·대기·복귀 진행을 관리. 대기·복귀 중 재인식하면 Graph가 현재 행동을 중단하고 Chase를 재개.

### 6.3 MonsterPatrolBehaviorHandler

소스: [MonsterPatrolBehaviorHandler.cs](E:/Unity/Project/ProjectBase/Assets/Scripts/FSM/NPC/AIMonstor/@Behavior/Handlers/MonsterPatrolBehaviorHandler.cs)

| 필드·API | 목적·사용처 |
|---|---|
| _patrolRadius / Radius | 초기 X 주변 목적지 선택 반경. Goblin 2. Patrol Action |
| _patrolWaitMin / _patrolWaitMax | 순찰 정지 범위. Goblin 1~2초 |
| Initialize(blackboard) | 기본 WaitRange용 최소·최대 시간만 전달 |

## 7. Action·Condition

모든 커스텀 Action의 Blackboard 필드는 Context 하나. 대상 유효·복귀 여부 반복 검사는 공통 Graph Guard로 대체. 입력은 Context.Input을 사용하고 이동 Action의 OnEnd는 이동 입력 0 유지.

| Action | Handler 사용·실행 기록 |
|---|---|
| MonsterChaseAction | OnStart: ChaseReturn.BeginEngagement. OnUpdate: Target.DeltaX → Attack.IsInAttackRange → 완료 또는 이동 |
| MonsterMoveBehindTargetAction | Target.Root의 시작 방향·종료 시각 저장. DeltaX - 방향 × AttackDistance × 0.8로 접근. 도착 Success, 시간 초과 Failure이며 대체 Chase 없이 해당 공격 회차 종료 |
| MonsterRequestAttackAction | Target.DeltaX로 방향 정렬 → 정지 → Attack.CanRequestAttack 대기 → RecordAttackRequest → 입력 한 번 → Success |
| MonsterReturnAction | OnUpdate: HomeX·ArrivalDistance로 이동·도착 판정. OnEnd: 이동 정지, Success일 때만 CompleteReturn |
| MonsterPatrolAction | HomeX ± Patrol.Radius의 랜덤 목적지 한 번 저장, ArrivalDistance로 도착 확인 |
| MonsterStopAction | Context.Input에 정지 입력 후 Success |
| Variable Comparison | Guard의 IsDead·HasValidTarget·LostTarget을 true와 비교 |

MonsterWaitForTargetAction 삭제. 기본 Stop → Wait → Return으로 대체.

배후 Action의 _target 제거. _targetFacing·_endTime은 실행 기록으로 유지. 현재 Player X는 계속 읽고 접근 시작 시 방향은 유지. 순찰 _destinationX도 실행 기록으로 유지.

FacingDeadZone=0.01은 방향 정렬 사각 구간이며 도착 오차가 아님. AttackAction은 공격 거리를 재검사하지 않음. 공격 결정 이후 Player가 멀어져도 HasValidTarget이 유지되면 공격을 시도.

방향 정렬은 반대 방향을 바라볼 때만 이동 입력을 발행하는 현재 방식을 유지. Motor.Move가 방향·속도를 함께 적용하므로 정렬 중 짧은 수평 이동이 발생할 수 있음. 공격 거리까지 다시 추적하는 로직과는 구분.

RequestAttack Success는 입력 발행 완료. 공격 시작·피해·애니메이션 종료 성공을 의미하지 않으며 기존 MonsterInput → AttackTransition → MonsterController / FSM 경로 유지.

OnEnd는 성공·실패·중단 모두에서 호출. Return의 교전 이력 초기화는 CurrentStatus == Success일 때만 수행하며, 재인식·사망 중단 때는 수행하지 않음. 공격 요청 시각 기록은 실제 입력 발행 직전 OnUpdate에 유지. OnStart·OnEnd로 옮기면 요청하지 않은 실행·중단에서도 간격을 소비하게 됨.

## 8. Graph 계층·중단

```text
반복 → 우선순위 Selector
├─ IsDead         → Stop
├─ HasValidTarget → Combat Subgraph
├─ LostTarget     → Sequence: Stop → Wait(LostTargetWait) → Return
└─ Patrol Subgraph

Combat: Chase → Random → 공통 RequestAttack
                ├─ 배후 접근
                └─ Stop

Patrol: Patrol → Stop → WaitRange
```

- 사망 Guard는 가장 높은 우선순위의 LowerPriority Observer.
- 교전 Guard는 HasValidTarget == true, Abort Target Both. 대상 상실 시 교전 중단, 재인식 시 하위 대기·복귀·순찰 중단.
- LostTarget Guard도 Both. 복귀 완료로 상실 플래그가 해제되면 다음 행동 선택.
- 공통 중단으로 대상 없는 상태에서 교전 Action이 Root·DeltaX를 읽지 않도록 관리.
- 하위 On Start는 Repeat 해제. 상위 반복이 다음 행동 선택.
- Combat에는 Context·Self만 전달. Patrol에는 Context·Self·PatrolWaitMin·PatrolWaitMax 전달.
- 공격 후 Wait 제거. 공격 요청 시각은 Handler에서 유지.
- Random은 두 경로 중 하나만 실행. 배후 접근 Failure는 Sequence 실패로 전파되며 그 회차의 RequestAttack은 실행하지 않음. 상위 반복에서 Chase부터 다시 선택. Try In Order·대체 Chase 없음.
- 노드 문구는 [Context] 추적·배후 접근·공격 요청·복귀·순찰·정지.
- 기존 프로젝트 조건 대신 Unity 기본 Variable Comparison 유지.

## 9. 수명·설정·범위

- Inspector 필드 이름·값·Override 유지. 설정 getter는 현재 Handler 값을 조회하고 기본 대기 값만 최초 Initialize 때 Blackboard에 전달.
- HomeX와 공격 요청 시각은 재활성화·재인식·Subgraph 진입 때 초기화하지 않음.
- 생존·활성·시야·높이 기준, 입력·FSM·Motor·Animator 유지.
- 거리 비교는 기존 공격 거리·도착 범위 사용. 공용 추가 오차 없음.
- 각 Context의 Handler·wrapper·입력과 공격 요청 시각은 개체별 독립.
- 씬·Prefab 설정 이전이나 자동 저장 없음. Graph 3개만 Editor API로 수정·빌드·저장.
- 임시 Migration·Verification 스크립트 없음. Play는 조작 안내 후 사용자 수동 입력을 관찰.

## 10. 탐색 책임 이전 검증 이력

아래는 단일 Player 단순화 전의 다중 후보·구독 버전 검증 결과. 현재 구조에서 최근접 선택·후보 목록 캐시·대상 구독은 제거.

- Unity Edit Mode의 임시 독립 씬에서 19개 검증 항목 통과. 검증 씬·스크립트 제거.
- 자기 자신·사망·Health 없는 후보 제외, 최근접 선택, offset·장애물 판정 확인.
- 반복 감지 시 후보 캐시 재사용, 동일 Health의 다중 Collider 간 구독 유지, 대상 변경 후 이전 사망 구독 해제 확인.
- 시야 상실 시 선택 대상 해제 및 반경 내 후보 캐시 유지, 범위 이탈·파괴 시 캐시 제거, Clear 전체 정리와 개체별 독립성 확인.
- 남은 Player·Monster Prefab 5개와 열린 InGame 씬의 Missing Script 없음. 현재 컴파일·Console 오류 없음.
- 이번 작업에서 InGame·Goblin Prefab·Monster Graph 파일 변경 없음. SampleScene은 AIPlayer 인스턴스 2개만 제거.
- 실제 Play 모드의 추적·상실 대기·복귀·배후 접근·공격 회귀는 수동 확인 필요.

## 11. 단일 Player 대상 관리 단순화 검증 이력

아래는 등록 참조 주입 변경 전, 매 갱신 OverlapCircleAll과 ReferenceEquals를 사용하던 버전의 검증 결과. 현재 구조의 검증 결과가 아님.

- Handler는 주석·빈 줄 포함 134줄에서 63줄로 축소. 후보 Dictionary·삭제 List·중첩 클래스·최근접 비교·대상 사망 구독 제거.
- Unity Edit Mode의 임시 독립 씬에서 실제 Detector·Target Handler·Context·행동 Action을 호출한 33개 항목 통과. 검증 씬·스크립트 제거.
- 동일 Collider에서 Component 검색을 생략하는 경로, 범위 이탈·장애물·사망 후 캐시 유지와 재인식, Player 인스턴스 교체·파괴·Health 누락 처리 확인.
- Context의 높이·추적 범위 판정 및 상실 대기·재인식 취소·복귀 완료 전 재추적 방지 확인.
- Chase·WaitForTarget·Return·MoveBehindTarget·RequestAttack의 상태 반환·이동 입력·요청 간격 확인. 전체 Graph의 실제 Play 실행 결과를 의미하지 않음.
- 비활성화 시 캐시·선택 대상·이동 입력 전체 정리, 재활성화 시 참조 재확보, 자기 사망 구독 유지와 몬스터별 독립성 확인.
- 현재 컴파일·Console 오류 없음. 실제 Motor 이동·FSM 공격·Animator를 포함한 Play 모드 회귀는 수동 확인 필요.

## 12. 씬 전용 Player 연결·판정 통합 검증

아래는 초기 위치 기준 추적 제한 제거 전 버전의 검증 이력. 추적 범위 초과·자기 범위 초과 즉시 복귀는 당시 정책이며, 현재 시야 기준 복귀의 검증 결과가 아님.

- 독립된 임시 Edit Mode 씬에서 33개 판정 항목 통과. 중심 거리 경계·offset·시야, 높이·추적 범위 초과 시 선택 대상 유지, Player 비활성화·사망·파괴, 캐시 재사용·개체별 독립, 상실 대기·즉시 복귀·복귀 중 재추적 방지 확인.
- 실제 InGame에서 Manager의 Inspector Player 연결과 실행 Blackboard 일치 확인. Context Component 비활성화·재활성화 3회에서 대상·이동·사망 구독 정리 및 재주입 확인.
- 런타임 Player 위치·입력·물리 제약과 공격 피해 대상을 일시 조정한 통제 조건에서 실제 BT 이동 입력·Motor 이동·AttackState·Animator 연결, 대상 상실 후 정지·복귀·도착·기록 초기화 확인. 대기 설정 1.5초에 측정 약 1.506초. 일반 수동 플레이의 모든 상황을 검증한 결과는 아님.
- 실제 Monster GameObject 비활성화·재활성화 후 Player 참조·사망 구독 복구 및 Graph 실행 유지 확인. 대기 중 재인식의 실제 Play 회귀는 자동 관찰만으로 확정하지 않고 수동 확인 항목으로 유지.
- 컴파일 오류 없음. InGame 직접 실행의 InGameUI.Awake는 기존 Managers 부재로 NullReferenceException 발생. 대상 인식과 별개이며 사용자 지시에 따라 이번 수정 범위에서 제외.
- 임시 설정·검증 스크립트와 검증 씬 제거. Play 종료로 런타임 위치·피격·입력·물리 설정 변경 복원. BT·FSM·Animator 에셋 변경 없음.
- 이후 검증은 상황·조작·기대 결과 안내 → 사용자 수동 조작 → 실행 상태·Blackboard·Console 읽기 확인 순서. 강제 동작·측정용 Verification 코드 추가 금지.

## 13. 현재 몬스터 시야 기준 추적·복귀 이력

이 절은 행동 흐름 단순화 전 정책·수동 검증 이력. 복귀 중 재인식 시 복귀를 유지하던 정책은 현재 사용하지 않으며, 최신 흐름은 8·16절 기준.

수동 Play 관찰에서 Player가 현재 Goblin 감지 반경 안에 있어도 초기 X에서 8 이상 떨어지면 HasValidTarget이 false가 되어 추적을 멈추는 상황 확인. 요구사항에 맞춰 Player·몬스터 양쪽의 초기 위치 기준 거리 제한 제거.

- Target.UpdateTarget()은 현재 몬스터 시야와 기존 생존·활성·높이 조건으로만 판정. HomeX·MaxChaseDistance 인자와 IsWithinChaseRange 제거.
- ChaseReturn의 _maxChaseDistance·MaxChaseDistance·자기 Transform 캐시 제거. Initialize에는 순찰·복귀 목적지 HomeX와 wrapper만 주입.
- 시야를 잃으면 기존 LostTargetWait 후 BeginReturn. 대기 중 재인식은 취소, 복귀 시작 후에는 완료까지 복귀 유지.
- Goblin의 감지 반경 6·허용 높이 1·상실 대기 1.5초와 Graph 우선순위 유지. 시야에는 감지 거리와 장애물 여부가 모두 포함.
- Unity CLI 재컴파일 완료. recompile_status의 compilationFailed false·errors 빈 목록 및 Console의 현재 오류 0 확인.
- 사용자 수동 조작 후 현재 시야 기준 추적·시야 이탈 후 대기·복귀·대기 중 재인식 시 추적 재개 정상 동작 확인. 강제 동작·측정용 스크립트 추가 없음. 아래 조작 항목은 이후 회귀 확인에도 사용.

### 수동 조작과 기대 결과

1. 같은 층에서 Player를 Goblin 시야 안에 두고 조금씩 멀리 이동. 초기 위치에서 8 이상 멀어져도 현재 시야 안이면 계속 추적. 공격 거리 1.8 안에서는 정지·공격할 수 있음.
2. 현재 Goblin의 감지 원을 완전히 벗어나거나 장애물 뒤로 이동. Goblin이 1.5초 정지한 뒤 초기 위치로 복귀.
3. 복귀 시작 전인 1.5초 대기 중 다시 시야 안으로 이동. 대기를 취소하고 추적 재개.
4. 복귀 시작 후 다시 시야 안으로 이동. 복귀를 끝낸 뒤 다음 행동 선택.

높이·대상 사망·비활성화·Monster 사망 분기는 기존대로 유지. Managers 관련 UI·Player 사망 완료 오류는 별도 범위. 사용자가 조작하는 동안 실행 상태·Blackboard·Console을 읽어 확인하고 미관찰 항목은 통과로 기록하지 않음.

## 14. 미사용 Blackboard Target 정리와 추가 후보

### 적용 내용

- 추적·공격 요청·배후 접근 Action은 Context.Target.Root를 사용. 동일 GameObject를 기록하던 Blackboard Target에는 읽는 코드·노드·Subgraph 연결이 없음.
- MonsterBehaviorBlackboard의 Target 이름·변수 참조·속성·Bind 제거. Target Handler는 Root·BodyCollider만 선택하고 HasValidTarget 기록은 유지.
- Unity Editor API로 BasicMonsterBehavior의 작성용 Blackboard·RuntimeBlackboardAsset·실행 Graph Blackboard에서 Target 제거. 다른 변수 GUID·노드 ID·배치·필드 연결·우선순위 유지. 씬·Prefab·하위 Graph는 변경하지 않음.
- Unity 재컴파일 성공. 이번 정리 후 실제 Play 회귀는 아직 실행하지 않음. 13절의 사용자 수동 확인은 정리 이전 구조의 결과로 구분.

### 당시 추가 정리 후보

아래 표는 Target 제거 당시 조사 이력. HomeX·설정 getter의 최신 처리 결과는 15절 기준이며, 공개 BodyCollider만 별도 작업으로 유지.

| 항목 | 현재 사용처 확인 | 정리 범위 |
|---|---|---|
| Blackboard HomeX | ChaseReturn.Initialize에서 기록만 수행. 순찰·복귀 Action은 ChaseReturn.HomeX 직접 조회. Graph 노드·Subgraph 연결 없음 | Blackboard 변수·wrapper 참조·기록만 제거 가능. 실제 ChaseReturn.HomeX는 유지 필요 |
| Target Handler의 공개 BodyCollider | SetTarget에서 저장만 수행. 외부 조회 없음. 거리·시야·높이는 _cachedCollider 사용 | 선택 결과 속성·할당 제거 가능. 내부 Collider 캐시는 유지 필요 |
| Attack의 AttackRequestInterval·RearApproachTimeout 공개 getter | getter 조회 없음. 직렬화 필드는 Initialize에서 Blackboard로 전달되고 Action이 소비 | getter만 제거 가능. 설정 필드·Blackboard 연결 유지 필요 |
| ChaseReturn의 LostTargetWait 공개 getter | getter 조회 없음. 직렬화 필드는 상실 대기 Action의 Seconds로 전달 | getter만 제거 가능. 설정 필드·Blackboard 연결 유지 필요 |
| Patrol의 PatrolRadius·PatrolWaitMin·PatrolWaitMax 공개 getter 및 Context.Patrol | 외부 getter 조회 없음. _patrol.Initialize에서 Graph 순찰·정지 설정 전달 | 공개 접근자만 제거 가능. Handler·설정 필드·초기화 유지 필요 |

HasValidTarget·IsDead·NeedsReturn·NeedsLostTargetWait는 Graph 조건에서 사용. 요청 간격·순찰 설정도 Graph가 직접 읽으므로 C# getter가 미사용인 것과 데이터 자체가 미사용인 것을 구분.

## 15. 행동 설정·공유 상태 Blackboard 통일 이력

이 절은 Action 설정을 전부 Blackboard에 연결하던 이전 단계. 최신 Handler 조회·공통 Guard 구조의 검증 결과와 구분.

### 변경 내용

- 설정 조회를 Inspector → Handler.Initialize → 개체별 Blackboard → Action으로 통일. AttackDistance·ArrivalDistance 추가, 기존 HomeX를 순찰·복귀 Action과 Patrol Subgraph에 연결.
- Attack·Patrol 설정 getter와 Context.Attack·Patrol 제거. ChaseReturn의 HomeX·ArrivalDistance·LostTargetWait getter와 IsInAttackRange 호출 제거. Handler의 직렬화 필드·설정값·Prefab Override는 유지.
- NeedsReturn·NeedsLostTargetWait·HasValidTarget의 Handler 저장 속성과 Context.IsDead·HasValidTarget 전달 getter 제거. _isReturning 복사본 제거. _isEngaged는 대상 상실 판정에 필요한 교전 이력으로 유지.
- UpdateState()는 Blackboard HasValidTarget·NeedsReturn을 읽어 NeedsLostTargetWait에 직접 기록. BeginReturn·CompleteReturn은 같은 프레임에 Blackboard 플래그를 직접 변경하며 NeedsReturn을 매 프레임 재기록하지 않음.
- 자기 Health 사망 구독은 Blackboard IsDead의 원본 전달 경로로 유지. Target.Root와 참조 캐시, Context.ChaseReturn의 기록 변경 API, 사용하지 않는 공개 BodyCollider는 이번 범위에서 유지.
- 상위·Combat·Patrol의 작성용·실행용 Blackboard와 모든 반복 Action 노드·Subgraph 전달 필드를 Unity Editor API로 갱신. 임시 Migration·Verification 스크립트, 씬·Prefab 수정 없음.

### 정적·컴파일 확인

- 기존 변수 이름·타입·GUID, 기존 Action 노드 ID·필드 연결을 변경 전후 읽기 결과로 대조. 기존 연결 보존 및 신규 거리·상태 필드 연결 확인.
- 저장된 실행 Graph의 상위·Combat·Patrol 변수 수는 각각 17·10·8. wrapper는 Self·NextAttackRequestTime을 제외한 15개 참조 확보.
- 상위 실행 Graph 안의 Combat·Patrol 모듈이 상위와 같은 변수 객체를 사용하며, Chase 2개·RequestAttack 2개를 포함한 모든 프로젝트 Action 필드가 해당 모듈 Blackboard와 같은 객체를 읽는 것 확인. 값 복사나 이름만 같은 별도 상태 아님.
- Unity 컴파일 완료: compilationFailed false, 오류 목록 없음. Console의 현재 오류 0. 이전 버퍼 기록은 최신 오류와 구분.
- 최신 Play 회귀는 아직 수행하지 않음. 과거 절의 검증 결과를 이번 변경의 통과 근거로 사용하지 않음.

### 수동 조작과 확인 항목

1. ScenePlayerManager가 설정된 InGame에서 Play. Player를 시야 밖에 두고 순찰·정지 확인. HomeX는 최초 시작 위치, 도착은 ArrivalDistance 범위에서 종료.
2. 같은 층에서 시야 안으로 접근. 추적·배후 접근·공격과 요청 간격 확인. 실행 Blackboard의 AttackDistance·ArrivalDistance가 Context Inspector 설정과 일치하는지 확인.
3. 시야 밖 또는 장애물 뒤로 이동. 설정된 LostTargetWait 후 복귀 확인. 대기 중 재진입은 추적 재개, 복귀 중 재진입은 복귀 완료 후 다음 행동 선택.
4. Monster 비활성화·재활성화 후 Player 재연결 확인. HomeX가 재활성화 위치로 바뀌지 않아야 함. Player·Monster 사망 분기는 가능할 때 별도 확인.

사용자가 조작하고 실행 상태·Blackboard·Console을 읽어 관찰. 조작하지 않은 항목은 미검증으로 유지. 기존 Managers 관련 오류는 이번 범위에서 제외.

## 16. Monster Blackboard·행동 흐름 단순화

### 적용 완료

- [x] 미사용 BodyCollider·SetTarget 제거, Target.DeltaX 추가.
- [x] Context의 Input·Attack·ChaseReturn·Patrol·Target 접근자 연결.
- [x] NeedsReturn·NeedsLostTargetWait를 LostTarget 하나로 통합. _isEngaged 유지.
- [x] Blackboard를 Graph 분기·기본 대기 변수로 축소.
- [x] 공통 Both Guard와 Stop → Wait → Return 구성. 대기·복귀 중 재인식 시 교전 재개.
- [x] 모든 커스텀 Action 필드를 Context 하나로 축소, 반복 상태 검사 제거.
- [x] 교전 요청 노드 하나로 통합. Attack의 거리 재검사·공격 후 Wait 제거.
- [x] Attack Handler에 요청 가능 시각 이전, 행동 재진입 시 유지.
- [x] 짧은 노드 문구·참고 문서·리팩토링 로그 갱신.
- [x] 배후 분기의 Try In Order·대체 Chase 제거 확인. Random 하위는 배후 접근·Stop 두 개.
- [x] Return 완료 처리를 OnEnd의 Success 분기로 이동. 중단 시 교전 이력 유지.

### 정적·컴파일 확인

- Unity 컴파일 완료: compilationFailed false, errors 빈 목록. Console 현재 오류 0. 기존 에셋·플러그인·API 경고는 별도 범위.
- 작성용·실행용 Blackboard 변수 수: 상위 8, Combat 2, Patrol 4. wrapper 참조 7개.
- 남겨 둔 변수 GUID 유지. LostTarget은 기존 NeedsLostTargetWait GUID 사용.
- 모든 커스텀 Action의 Context 필드와 상위·하위 실행 변수의 동일 객체 연결 확인.
- 실행 Selector 순서: 사망 → 교전 → 상실 Sequence → 순찰. 교전·상실 Observer Both, 사망 LowerPriority 확인.
- 실행 상실 Sequence는 Stop·Wait·Return, 교전 Sequence는 Chase·Random·RequestAttack 확인. 요청 노드 1개·공격 후 Wait 없음.
- 후속 확인: 작성 Graph와 상위·하위 실행 Graph의 Random 하위는 MonsterMoveBehindTargetAction·MonsterStopAction 두 개. Combat의 대체 Chase·Selector 없음. Context 연결 유지.
- 기존 Goblin 설정 비교 일치: 공격 거리 1.8, 간격 1, 배후 제한 1.5, 도착 범위 0.15, 상실 대기 1.5, 순찰 반경 2, 정지 1~2, 높이 1.
- MonsterWaitForTargetAction과 meta 삭제. 사용 노드·실행 Graph에 삭제 타입 참조 없음. Git 복구 가능.
- 씬·Prefab·FSM·Animator는 변경하지 않음. 최신 실제 Play 회귀는 아직 수행하지 않음.

### 사용자 수동 조작·기대 결과

1. ScenePlayerManager가 설정된 InGame에서 Play. 최초 시야 밖에서는 순찰·정지만 수행하고 상실 대기·복귀가 발생하지 않아야 함.
2. 같은 층에서 시야 안으로 접근. 공격 거리까지 추적한 뒤 배후 또는 직접 경로로 공격 요청. 요청 간격은 최소 기존 1초 유지.
3. 교전 중 시야 밖이나 장애물 뒤로 이동. 정지 → 기존 1.5초 대기 → 최초 위치 복귀.
4. 1.5초 대기 중 시야 안으로 재진입. 즉시 대기를 중단하고 추적 재개.
5. 복귀 이동이 시작된 뒤 시야 안으로 재진입. 복귀 완료를 기다리지 않고 즉시 추적 재개.
6. 공격 거리까지 접근시킨 후 공격 요청 전 Player를 조금 멀리 이동하되 시야 유지. 공격 거리를 다시 검사하지 않고 공격을 시도.
7. 대상 없는 상태에서 복귀 완료 후 순찰 재개. 가능하면 Monster 사망·비활성화도 확인하여 이동 입력 잔류 여부 관찰.
8. 배후 접근 시간 초과 시 그 회차에는 공격 요청 없이 상위 반복 재시작. 배후 접근 성공 시 공통 요청으로 진행. 반대 방향에서는 이동 입력으로 정렬 후 요청하므로 짧은 이동 허용.

사용자 조작 중 Graph 활성 노드·HasValidTarget·LostTarget·IsDead·Console을 읽기만 하여 확인. 실제 관찰하지 않은 항목은 미검증으로 유지. Managers 관련 기존 오류는 제외.
