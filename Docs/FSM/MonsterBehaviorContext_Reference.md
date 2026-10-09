# Monster Behavior · Handler · Blackboard · Action 연결 정리

작성 기준: 2026-10-07 책임 분리 후 소스와 BasicMonsterBehavior.asset. 2026-10-09 기본 조건·Subgraph 분리·AIPlayer 제거, 씬 전용 Player 연결·Target Handler 판정 통합, 현재 몬스터 시야 기준 추적·복귀 반영. 이전 버전의 검증 결과는 10~12절에 이력으로 보존.

현재 구현의 변수·함수·사용처를 설명하는 참고 문서. 기본값은 스크립트 선언값이며 Prefab·Scene Inspector 값과 Override는 별도 적용. 검증 이력은 10~12절, 현재 정책과 수동 확인 항목은 13절에 별도 기록.

## 1. 책임과 연결 구조

| 구성 요소 | 제작 목적 | 담당하지 않는 부분 |
|---|---|---|
| ScenePlayerManager | 현재 씬의 Inspector Player 참조 제공·씬 수명의 싱글톤 | 자동 탐색, 동적 등록·교체 이벤트, 씬 간 유지 |
| PlayerDetector | 주입 대상 몸통 중심까지의 거리·장애물 확인 | Player 탐색, Health 검색, 참조 캐시, 생존 판정, 구독·Blackboard 기록 |
| MonsterTargetBehaviorHandler | Player 참조 보관, 현재 시야·높이 판정, 선택 대상·Target·HasValidTarget 기록 | Manager 접근, Component 검색, 후보 목록·최근접 비교, 사망 구독 |
| MonsterBehaviorBlackboard | 변수 이름·타입·실행 변수 참조의 단일 접근 지점 | 행동 판단, 타이머, 공격 요청 시각 변경 |
| MonsterAttackBehaviorHandler | 공격·배후 접근 설정 전달, 수평 공격 거리 판정 | 높이·대상 유효 판정, 공격 입력, FSM 진입, 배후 이동 타이머 |
| MonsterChaseReturnBehaviorHandler | 초기 위치·교전·복귀 기록, 복귀·상실 대기 플래그 기록 | 이동 입력, 실제 대기 타이머 |
| MonsterPatrolBehaviorHandler | 순찰 반경·정지 시간 설정 전달 | 랜덤 목적지 선택, 이동, 정지 타이머 |
| MonsterBehaviorContext | 자기 참조 캐시, 씬 Player·Handler 주입, 갱신 순서, 자기 사망 정보 전달 | 대상 판정 조합, Manager 캐시·Player 변경 구독, 전체 플래그 일괄 기록 |
| Action | Handler 조회·행동 기록 변경, 이동·공격 입력, 목적지·타이머 관리 | FSM 직접 전환 |
| Variable Comparison (Unity 기본 조건) | 연결된 Blackboard bool과 true 비교 | 감지·거리 계산 |

```text
ScenePlayerManager.Player → Context.BindPlayer → Target Handler
                                                    ↓
PlayerDetector 현재 거리·시야 → 감지 대상 유지 / 높이로 HasValidTarget 판정
                                                    ↓
                        Context → ChaseReturn.UpdateState

Context + 각 Handler → MonsterBehaviorBlackboard → 기존 Graph
                                                   ↓
Action → Context.Attack / ChaseReturn / Target → MonsterInput
                                                   ↓
                                   기존 Controller · FSM · Motor · Animator
```

설정 Handler 4개는 Serializable 일반 C# 클래스이며 Context가 개체별 소유. wrapper도 일반 C# 클래스. 씬 Player 참조 제공용 ScenePlayerManager 외에 Component·인터페이스·상속 구조·자체 Update 추가 없음. Handler는 Context·Manager·Graph·다른 Handler를 역참조하지 않고 주입한 Component·숫자·wrapper만 사용.

## 2. MonsterBehaviorContext

소스: [MonsterBehaviorContext.cs](E:/Unity/Project/ProjectBase/Assets/Scripts/FSM/NPC/AIMonstor/@Behavior/MonsterBehaviorContext.cs)

### 2.1 클래스·필드·속성

DisallowMultipleComponent는 중복 Component 방지. DefaultExecutionOrder(-100)는 기본 순서 Component와 BehaviorGraphAgent(-50)보다 먼저 Start·Update를 실행하도록 지정. Graph의 Awake 초기화 후 Context Start에서 실행 변수를 연결하고, Agent Start에서 행동 실행 시작.

RequireComponent는 MonsterInput·PlayerDetector·Health·BehaviorGraphAgent 선언. 자기 몸통 Collider2D는 Awake에서 캐시하며 기존처럼 RequireComponent 목록에는 포함하지 않음.

| 변수·속성 | 제작 목적 | 사용처 |
|---|---|---|
| _attack / Attack | 공격 설정·수평 거리 판정 Handler 소유·공개 | Start 주입, 추적·공격 요청·배후 접근 Action |
| _chaseReturn / ChaseReturn | 추적·복귀 Handler 소유·공개 | Start에서 순찰·복귀 기준점 주입, 복귀 기록 갱신, 행동 Action |
| _patrol / Patrol | 순찰 설정 Handler 소유·공개 | Start에서 Graph 설정 전달. 현재 Action은 Blackboard 설정 사용 |
| _targetHandler / Target | 감지·추적 판정 Handler 소유·공개. Target Recognition Inspector 설정 | RefreshState 대상 갱신, 추적·공격 요청·배후 접근 Action의 Target.Root |
| _input | 자기 MonsterInput 캐시 | Blackboard Input 전달, 비활성화 시 이동 초기화 |
| _detector | 자기 감지 Component 캐시 | Target Handler에 주입 |
| _health | 자기 Health 캐시 | IsDead, 자기 사망 구독 |
| _agent | 자기 BehaviorGraphAgent 캐시 | Start에서 wrapper Bind |
| _bodyCollider | 자기 몸통 Collider 캐시 | Target.Initialize에 주입, 대상 높이 비교 기준 |
| _blackboard | 개체별 실행 변수 접근 | Context·Input 초기 설정, 자기 IsDead 기록. 초기화 전 null |
| _deathSubscription | 자기 사망 구독의 IDisposable | SubscribeDeath·OnDisable에서 교체·해제 |
| IsDead | 자기 Health.IsDead.Value 조회 | 대상 갱신 여부 판단. Graph는 구독으로 전달받은 동명 Blackboard bool 확인 |
| HasValidTarget | Target.HasValidTarget을 전달하는 읽기 전용 접근자. 별도 상태 저장 없음 | 교전 Condition, 추적·공격 요청·배후 접근·상실 대기 Action |

높이 설정 경로는 _attack._maxAttackHeightDifference에서 _targetHandler._maxTargetHeightDifference로 이동. 초기 위치 기준 추적 제한인 _chaseReturn._maxChaseDistance는 제거. 나머지 공격·순찰·복귀 도착·상실 대기 설정과 런타임 wrapper 유지.

### 2.2 함수와 실행 순서

| 함수 | 목적·처리 | 호출·사용처 |
|---|---|---|
| Awake() | 자기 Component만 캐시 | Unity 생명주기. 이 시점에는 Blackboard 설정을 기록하지 않음 |
| Start() | wrapper Bind → Context·Input 설정 → Handler 초기화 → 자기 사망 구독 → 씬 Player 주입·최초 판정 | 모든 Awake 이후 실행. HomeX는 이 시점의 X 유지 |
| OnEnable() | Start 이후 재활성화 시 자기 사망 구독 복구·씬 Player 재주입 | 첫 활성화에서는 wrapper가 null이므로 Start에 초기화 위임 |
| Update() | RefreshState 호출 | Handler 자체 Update를 추가하지 않음 |
| RefreshState() | Target.UpdateTarget() → ChaseReturn.UpdateState(HasValidTarget) | BindPlayer·Update. 사망 시 대상 Handler Clear |
| SubscribeDeath() | 기존 자기 구독 Dispose 후 Health.IsDead 구독 | Start·재활성화. 현재 값이 즉시 전달되고 이후 변경 시 Blackboard IsDead 기록 |
| BindPlayer() | ScenePlayerManager.Instance.Player의 Health·같은 GameObject의 몸통 Collider 주입 후 RefreshState | Start·재활성화. Manager·Inspector Player 누락은 한 번 알리고 Context 비활성화. Component 검색은 이 함수에서만 수행 |
| OnDisable() | 초기화 전이면 반환. 이후 자기 사망 구독 해제·Target.Clear·이동 입력 초기화 | 교전·복귀 기록과 HomeX 유지. 내부 참조를 다시 null 검사하지 않음 |

종합 판정은 Target Handler 내부에서 수행. 현재 몬스터 원점 기준 거리·시야 조건을 만족한 뒤 자기·대상 몸통 바닥의 높이 차이를 확인. 초기 X로 추적을 제한하지 않음. Context는 판정을 조합하지 않고 결과만 전달.

공격 거리 충족 여부는 종합 판정에 포함하지 않음. 추적·공격 요청 Action이 별도로 확인. 대상이 감지됐지만 높이가 부적합하면 Target.Root는 존재하고 HasValidTarget만 false.

기존 Context의 HomeX, AttackDistance, ArrivalDistance, NeedsReturn, NeedsLostTargetWait, TargetRoot 전달 속성과 BeginEngagement, BeginReturn, CompleteReturn, IsInAttackRange 전달 함수는 제거. PublishFlags, UpdateTargetReferences도 제거.

Player 전달은 ScenePlayerManager.Instance.Player만 사용. Context에 Manager 필드와 Player 변경 이벤트 구독 없음. 주입 직후 대상·추적 유효성·복귀 상태를 같은 프레임에 갱신. 기존 TestPlayerManager·PlayerSpawnManager·HUD는 변경하지 않지만, 이번 Monster 연결에서는 Title의 동적 등록 흐름을 지원하지 않음.

### 2.3 ScenePlayerManager 설정

소스: [ScenePlayerManager.cs](E:/Unity/Project/ProjectBase/Assets/Scripts/Managers/PlayerManager/ScenePlayerManager.cs)

- InGame의 빈 GameObject에 Component 하나를 배치하고 _player에 씬의 실제 PlayerController를 할당. Prefab 에셋을 할당하지 않음.
- Awake는 싱글톤만 등록. 중복은 한 번 알리고 중복 Component만 제거. DontDestroyOnLoad·검색·변경 이벤트 없음.
- OnDestroy는 자신이 등록한 Instance만 해제. Manager와 Player는 활성 상태로 씬 실행.
- Player Health는 Player Awake 이후 Context Start에서 확보. 런타임 Player 교체·재생성·생존 중 Component 교체 제외.
- 기존 Managers 없는 씬의 Player 사망 완료·상호작용은 별도 의존성이며 이번 인식 변경의 해결 범위가 아님.

## 3. MonsterBehaviorBlackboard

소스: [MonsterBehaviorBlackboard.cs](E:/Unity/Project/ProjectBase/Assets/Scripts/FSM/NPC/AIMonstor/@Behavior/MonsterBehaviorBlackboard.cs)

변수 이름은 wrapper의 private …Name 상수에만 보관. 아래 private 필드는 값의 복사본이 아니라 실행 Graph의 BlackboardVariable<T> 참조. 같은 행의 공개 속성은 해당 참조의 Value를 읽고 씀.

| 변수 이름·공개 속성 | private 참조 | 타입 | 기록 주체 | Graph 소비처 |
|---|---|---|---|---|
| Context | _context | MonsterBehaviorContext | Context Start | Stop 제외 6종 Action의 Context |
| Input | _input | MonsterInput | Context Start | 7종 Action의 Input |
| Target | _target | GameObject | Target Handler | 현재 Action 필드에 직접 연결 없음. Action은 Target.Root 조회 |
| HomeX | _homeX | float | ChaseReturn.Initialize | 현재 Action은 ChaseReturn.HomeX 조회 |
| IsDead | _isDead | bool | Context 자기 사망 구독 | 최우선 사망 Guard의 Flag |
| NeedsReturn | _needsReturn | bool | ChaseReturn | 복귀 Guard의 Flag |
| NeedsLostTargetWait | _needsLostTargetWait | bool | ChaseReturn | 상실 대기 Guard의 Flag |
| HasValidTarget | _hasValidTarget | bool | Target Handler | 교전 Guard의 Flag |
| AttackRequestInterval | _attackRequestInterval | float | Attack.Initialize | 공격 요청 Interval, 교전 마지막 Wait |
| RearApproachTimeout | _rearApproachTimeout | float | Attack.Initialize | 배후 접근 Timeout |
| LostTargetWait | _lostTargetWait | float | ChaseReturn.Initialize | 상실 대기 Seconds |
| PatrolRadius | _patrolRadius | float | Patrol.Initialize | 순찰 Radius |
| PatrolWaitMin | _patrolWaitMin | float | Patrol.Initialize | 순찰 후 WaitRange.Min |
| PatrolWaitMax | _patrolWaitMax | float | Patrol.Initialize | 순찰 후 WaitRange.Max |

| 함수 | 목적 |
|---|---|
| Bind(BehaviorGraphAgent agent) | 위 14개 실행 변수 참조를 한 번 확보 |
| GetVariable<T>(agent, variableName) | Bind 시점에만 누락·타입 불일치 확인. 개체 이름·변수 이름·기대 타입을 포함한 오류 전달 |

Handler·Context에서 문자열·SetVariableValue·GetVariable을 사용하지 않음. Graph 변수 이름 변경 시 wrapper 상수도 수정 필요. 런타임 Graph를 교체·재초기화하는 별도 기능은 이번 범위에 포함하지 않음.

NextAttackRequestTime은 wrapper에서 바인딩·초기화·갱신하지 않음. 공격 요청 Action의 기존 실행 기록 유지. Self도 wrapper 관리 대상이 아니며 Graph Agent의 기존 설정 유지.

## 4. PlayerDetector: 거리·시야 판정

소스: [PlayerDetector.cs](E:/Unity/Project/ProjectBase/Assets/Scripts/FSM/@Detector/PlayerDetector.cs)

Player 참조를 찾지 않고 전달받은 Collider의 거리·장애물만 판정. FindCandidates·OverlapCircleAll·결과 배열·playerMask 제거. 생존·활성 판정과 선택 대상 관리는 Target Handler 담당. 별도 Component·인터페이스 추가 없음.

| 변수·속성 | 목적·사용 |
|---|---|
| obstacleMask | 감지 원점과 후보 Collider 중심 사이 Linecast 장애물 확인 |
| offset | transform.position에 더할 감지 원점 Offset |
| viewRadius | 감지 반경. 기존 Inspector 필드·Gizmo 유지 |

| 함수 | 목적 |
|---|---|
| IsWithinRange(target, out Vector2 origin) | 현재 transform.position + offset에서 target.bounds.center까지의 XY 제곱 거리 <= viewRadius². 경계 포함, 추가 허용 오차 없음 |
| HasLineOfSight(origin, candidate) | 원점과 후보 bounds.center 사이 Linecast에 장애물이 없으면 true |
| OnDrawGizmos() | 기존 원점·반경을 빨간색으로 표시. 대상 선택 여부 색상 변경 없음 |

obstacleMask, offset, viewRadius의 직렬화 이름·Inspector 값은 유지. Gizmo는 기존 원점·반경 유지. Collider 표면이 반경에 들어왔더라도 중심이 밖이면 인식하지 않는 새 거리 기준 적용. 반경은 자동 조정하지 않음. Linecast는 생존·활성·거리 조건을 모두 만족한 대상에만 수행.

기존 호출자인 AIPlayerBrain·AIPlayerInput·AIPlayer 전용 Action 6개 및 해당 meta 제거. Player AI.prefab과 meta, SampleScene의 Player AI1·Player AI2 Prefab 인스턴스만 삭제. SampleScene과 직접 입력 Player, 공용 Detector·FSM·Animator·Clip·SO 및 Monster Graph 유지. 예전 AIPlayer Behavior Graph는 작업 전부터 에셋이 없는 상태.

## 5. MonsterTargetBehaviorHandler

소스: [MonsterTargetBehaviorHandler.cs](E:/Unity/Project/ProjectBase/Assets/Scripts/FSM/NPC/AIMonstor/@Behavior/Handlers/MonsterTargetBehaviorHandler.cs)

| 변수·속성 | 목적·사용 |
|---|---|
| _detector | Initialize에서 주입한 감지 Component |
| _bodyCollider | Initialize에서 주입한 자기 몸통 Collider. 높이 비교 기준 |
| _maxTargetHeightDifference | 기본 0.75. 몸통 바닥 기준 추적 허용 높이 차이. 기존 Goblin 값은 1 |
| _blackboard | 개체별 Target·HasValidTarget 기록용 wrapper |
| _cachedCollider | Context의 BindPlayer에서 주입받은 몸통 Collider. 범위·시야 인식 해제 후에도 유지 |
| _health | Context에서 Player.Health로 전달받은 Health. Handler 내부 Component 검색 없음 |
| Root | 현재 감지 대상의 Transform을 저장하는 읽기 전용 공개 속성. 선택 해제 후 null |
| BodyCollider | 인식 조건을 만족한 현재 선택 대상의 몸통 Collider. 높이 판정에서 현재 bounds 조회 |
| HasValidTarget | 생존·활성·현재 시야·높이가 모두 유효한 판정 결과. Context 접근자·Blackboard로 전달 |

| 함수 | 목적·소비처 |
|---|---|
| Initialize(detector, bodyCollider, blackboard) | Context Start에서 의존성만 저장. Target 초기 기록 없음 |
| BindPlayer(health, bodyCollider) | Clear 후 주입 참조 한 쌍을 한 번 검증. 누락은 오류 한 번 출력 후 미연결 유지 |
| UpdateTarget() | 캐시 Collider 소멸 검사 한 번 → 생존·활성·현재 거리·시야로 선택 → 높이로 HasValidTarget 기록 |
| IsWithinHeightRange() | 자기·대상 bounds.min.y 차이 <= _maxTargetHeightDifference. 내부 호출 전용 |
| SetTarget(isDetected) | 판정 bool로 Root·BodyCollider·Target 직접 선택. Blackboard 값 변경 시에만 기록. 주입 참조 유지 |
| Clear() | 선택 참조·Target·HasValidTarget·주입 Collider·Health 전체 초기화. Initialize 이후에만 호출 |

Inspector Player는 한 명이며 Health와 몸통 Collider는 같은 GameObject에 배치. Context가 초기화·재활성화 시 몸통 Collider를 확보. 부모 검색·물리 대상 탐색·후보 배열·최근접 비교·ReferenceEquals 없음.

주입 시 Health·Collider 한 쌍을 검증하므로 갱신·getter에서 두 참조를 반복 검사하지 않음. Player 파괴는 캐시 Collider의 Unity null 체크 하나로 처리. Handler에서 다시 검색하지 않으며 동적 Player 교체는 지원하지 않음.

범위 이탈·시야 상실·Player 사망·Collider 비활성화·GameObject 비활성화 시 Root·BodyCollider·Blackboard Target 해제 및 HasValidTarget false. _cachedCollider·_health는 유지하므로 같은 Player 재인식 시 탐색·Component 검색 없음. 몬스터 사망·비활성화는 Clear()로 참조까지 해제. 각 몬스터가 주입 참조 한 쌍을 개별 소유.

Player 사망과 선택·추적 판정은 다음 Context 갱신에서 반영. 높이 판정이 실패해도 감지된 Root·BodyCollider·Target은 유지. Collider bounds는 현재 월드 위치·크기를 반영. SetTarget의 조건부 할당은 미감지 상태를 표현하는 것이며 의존성 null 검증이 아님.

## 6. 행동별 설정 Handler

### 6.1 MonsterAttackBehaviorHandler

소스: [MonsterAttackBehaviorHandler.cs](E:/Unity/Project/ProjectBase/Assets/Scripts/FSM/NPC/AIMonstor/@Behavior/Handlers/MonsterAttackBehaviorHandler.cs)

| 변수·속성 | 기본값·목적 | 사용처 |
|---|---|---|
| _attackDistance / AttackDistance | 1.8. 수평 공격 거리 | 추적·공격 요청 판정, 배후 목적지 거리 |
| _attackRequestInterval / AttackRequestInterval | 1. 공격 요청 간격 | Initialize → Blackboard → RequestAttack.Interval·기본 Wait |
| _rearApproachTimeout / RearApproachTimeout | 1.5. 배후 접근 제한 시간 | Initialize → Blackboard → MoveBehindTarget.Timeout |

| 함수 | 목적·소비처 |
|---|---|
| Initialize(blackboard) | 공격 시간 설정 2개 기록. wrapper를 별도 필드로 보관하지 않음 |
| IsInAttackRange(deltaX) | Abs(deltaX) <= 공격 거리. Chase·RequestAttack이 직접 호출 |

높이 설정·판정과 자기 몸통 참조는 Target Handler로 이전. Attack Handler에는 공격 설정과 수평 거리 판정만 유지.

### 6.2 MonsterChaseReturnBehaviorHandler

소스: [MonsterChaseReturnBehaviorHandler.cs](E:/Unity/Project/ProjectBase/Assets/Scripts/FSM/NPC/AIMonstor/@Behavior/Handlers/MonsterChaseReturnBehaviorHandler.cs)

| 변수·속성 | 기본값·목적 | 사용처 |
|---|---|---|
| _returnArrivalDistance / ArrivalDistance | 0.15. 도착 범위 | 순찰·복귀·배후 접근 Action |
| _lostTargetWait / LostTargetWait | 2. 교전 대상 상실 후 정지 시간 | Initialize → Blackboard → WaitForTarget.Seconds |
| _blackboard | 실행 플래그·초기 설정 기록 | Initialize·PublishState |
| _isEngaged | 추적 시작 이력. 초기 false | BeginEngagement 기록, 상실 대기 필요 판정 |
| _isReturning | 복귀 시작부터 완료까지 유지. 초기 false | BeginReturn·CompleteReturn·UpdateState |
| HomeX | Start 시점 초기 X | 순찰·복귀 기준점, Blackboard 표현. 추적 거리 제한에는 사용하지 않음 |
| NeedsReturn | 복귀 시작부터 완료까지 유지 | Action 실패 판정·복귀 Guard |
| NeedsLostTargetWait | 교전 이력 있고 유효 대상 없고 복귀 필요 아님 | 상실 대기 Guard |

| 함수 | 목적·소비처 |
|---|---|
| Initialize(homeX, blackboard) | 기준점·wrapper 주입, HomeX·LostTargetWait 기록 |
| UpdateState(hasValidTarget) | 복귀 진행 여부로 NeedsReturn 유지. 교전 이력 && !유효 대상 && !복귀 필요로 NeedsLostTargetWait 계산. 두 플래그 기록 |
| BeginEngagement() | 교전 이력 기록. Chase.OnStart 직접 호출 |
| BeginReturn() | 복귀 유지·NeedsReturn true, NeedsLostTargetWait false, 즉시 두 플래그 기록. WaitForTarget·Return이 직접 호출 |
| CompleteReturn() | 교전·복귀 기록과 두 플래그 false, 즉시 기록. Return.OnUpdate 직접 호출 |
| PublishState() | 자신의 두 플래그만 wrapper에 반영. Context의 전체 PublishFlags 호출 대체 |

교전 중 현재 몬스터 시야·대상 생존·활성·높이 조건 상실은 대기 후 복귀. 초기 위치에서 Player나 몬스터가 멀어진 것만으로는 복귀하지 않음. HomeX는 순찰·복귀 목적지로만 유지. 복귀 시작·완료 직후 Graph가 이전 플래그를 읽지 않도록 같은 프레임 기록 유지.

### 6.3 MonsterPatrolBehaviorHandler

소스: [MonsterPatrolBehaviorHandler.cs](E:/Unity/Project/ProjectBase/Assets/Scripts/FSM/NPC/AIMonstor/@Behavior/Handlers/MonsterPatrolBehaviorHandler.cs)

| 변수·속성 | 기본값·목적 | 사용처 |
|---|---|---|
| _patrolRadius / PatrolRadius | 2. 초기 X 주변 목적지 선택 반경 | Blackboard → Patrol.Radius |
| _patrolWaitMin / PatrolWaitMin | 1. 순찰 후 정지 시간 최소값 | Blackboard → 기본 WaitRange.Min |
| _patrolWaitMax / PatrolWaitMax | 2. 순찰 후 정지 시간 최대값 | Blackboard → 기본 WaitRange.Max |
| Initialize(blackboard) | 위 설정 3개 기록 | Context Start 호출. wrapper를 별도 필드로 보관하지 않음 |

## 7. Action·Condition 사용처

Action의 기존 Context·Input·시간·반경 Blackboard 필드 및 노드 ID 유지. Handler는 기존 Context 참조를 통해 접근. 모든 이동 Action의 OnEnd는 Input.SetMovement(Vector2.zero) 유지.

| Action | 필드·기록 | 함수별 Handler·Context 사용 |
|---|---|---|
| [MonsterChaseAction](E:/Unity/Project/ProjectBase/Assets/Scripts/FSM/NPC/AIMonstor/@Behavior/Actions/MonsterChaseAction.cs) | Context, Input | OnStart: ChaseReturn.BeginEngagement. OnUpdate: HasValidTarget·ChaseReturn.NeedsReturn 확인 → Target.Root와 자기 X 비교 → Attack.IsInAttackRange면 Success, 아니면 이동 |
| [MonsterRequestAttackAction](E:/Unity/Project/ProjectBase/Assets/Scripts/FSM/NPC/AIMonstor/@Behavior/Actions/MonsterRequestAttackAction.cs) | Context, Input, Interval, NextRequestTime, FacingDeadZone=0.01 | OnStart: Running. OnUpdate: 유효 대상·복귀·공격 거리 확인 → 자기 localScale.x와 대상 방향 정렬 → 정지 → 요청 시각 확인·갱신 → RequestAttack |
| [MonsterMoveBehindTargetAction](E:/Unity/Project/ProjectBase/Assets/Scripts/FSM/NPC/AIMonstor/@Behavior/Actions/MonsterMoveBehindTargetAction.cs) | Context, Input, Timeout, _target, _targetFacing, _endTime | OnStart: Target.Root·시작 방향·제한 시각 저장. OnUpdate: 대상 교체·상실·복귀 필요 확인, Attack.AttackDistance로 배후 목적지 계산, ChaseReturn.ArrivalDistance·시간 제한 확인 후 이동 |
| [MonsterWaitForTargetAction](E:/Unity/Project/ProjectBase/Assets/Scripts/FSM/NPC/AIMonstor/@Behavior/Actions/MonsterWaitForTargetAction.cs) | Context, Input, Seconds, _endTime | OnStart: 정지·종료 시각 저장. OnUpdate: HasValidTarget이면 대기 취소, 만료 전 Running, 만료 후 ChaseReturn.BeginReturn·Success |
| [MonsterReturnAction](E:/Unity/Project/ProjectBase/Assets/Scripts/FSM/NPC/AIMonstor/@Behavior/Actions/MonsterReturnAction.cs) | Context, Input | OnStart: ChaseReturn.BeginReturn. OnUpdate: HomeX 방향 이동, ArrivalDistance 도착 시 CompleteReturn·Success |
| [MonsterPatrolAction](E:/Unity/Project/ProjectBase/Assets/Scripts/FSM/NPC/AIMonstor/@Behavior/Actions/MonsterPatrolAction.cs) | Context, Input, Radius, _destinationX | OnStart: ChaseReturn.HomeX ± Radius 랜덤 목적지 저장. OnUpdate: ChaseReturn.ArrivalDistance 도착 확인·이동 |
| [MonsterStopAction](E:/Unity/Project/ProjectBase/Assets/Scripts/FSM/NPC/AIMonstor/@Behavior/Actions/MonsterStopAction.cs) | Input | OnStart: 이동 입력 0·Success. Context 참조·타이머 없음. 변경 없음 |
| Variable Comparison (Unity 기본 조건) | Variable, Operator, ComparisonValue | 각 Guard의 Blackboard bool을 Equal 연산자로 true와 비교. Context·Handler 직접 참조 없음. 별도 프로젝트 조건 스크립트 없음 |

배후 목적지 = 대상 현재 X - 시작 때 저장한 방향 × Attack.AttackDistance × 0.8.
대상 위치는 계속 읽고 대상 방향은 시작 시점 값 유지. Y 이동·경로 탐색·벽 회피 추가 없음.

FacingDeadZone은 X가 거의 같을 때 방향 정렬 방지용이며 거리 비교 허용 오차가 아님. NextRequestTime은 동일 몬스터 Graph의 두 공격 요청 노드가 공유. 몬스터 개체 간 공유 아님.

RequestAttack 성공은 입력 발행이며 실제 FSM 진입 성공을 뜻하지 않음. 기존 [MonsterInput](E:/Unity/Project/ProjectBase/Assets/Scripts/FSM/NPC/AIMonstor/Input/MonsterInput.cs) → [AttackTransition](E:/Unity/Project/ProjectBase/Assets/Scripts/FSM/Agent/StateControl/TransitionRules/Attack/AttackTransition.cs) → [MonsterController](E:/Unity/Project/ProjectBase/Assets/Scripts/FSM/NPC/AIMonstor/@Hub/MonsterController.cs) 판단 경로 유지.

## 8. Graph 계층과 실행 흐름

Graph: [BasicMonsterBehavior.asset](E:/Unity/Project/ProjectBase/Assets/Prefabs/Monster/BasicMonsterBehavior.asset)

```text
반복 → 우선순위 Selector
├─ IsDead              → Stop
├─ NeedsReturn         → Return
├─ NeedsLostTargetWait → WaitForTarget
├─ HasValidTarget      → Run Subgraph: Monster Combat
└─ Run Subgraph: Monster Patrol
```

상위 Graph는 기존 행동 선택·중단 우선순위 담당. 복잡한 교전·순찰만 정적 Run Subgraph로 분리. 단일 Action인 Stop·Return·WaitForTarget은 상위에 유지.

| 하위 Graph | 내부 흐름 | 상위 Blackboard 연결 |
|---|---|---|
| [MonsterCombatBehavior.asset](E:/Unity/Project/ProjectBase/Assets/Prefabs/Monster/MonsterCombatBehavior.asset) | Chase → Random으로 배후 접근 후 공격 또는 직접 공격 선택 → Wait. 배후 접근 실패 시 기존 Chase 대체 유지 | Context, Input, AttackRequestInterval, NextAttackRequestTime, RearApproachTimeout |
| [MonsterPatrolBehavior.asset](E:/Unity/Project/ProjectBase/Assets/Prefabs/Monster/MonsterPatrolBehavior.asset) | Patrol → Stop → WaitRange | Context, Input, PatrolRadius, PatrolWaitMin, PatrolWaitMax |

하위 Graph의 On Start는 Repeat 해제. 상위 반복이 다음 행동 선택 담당. 하위 변수는 이름만 맞춘 복사값이 아닌 상위 실행 Blackboard의 동일 변수 객체로 연결. NextAttackRequestTime은 하위 Graph 재진입 시에도 유지. Self는 Unity의 기본 소유자 변수. Shared 옵션은 사용하지 않아 몬스터 개체별로 독립.

위 네 bool은 각 Guard의 Variable Comparison.Variable에 연결. Operator는 Equal, ComparisonValue는 true. 기존 MonsterFlagCondition은 기본 조건으로 대체 후 삭제. 상위 Blackboard 변수 이름·GUID·설정, 행동 우선순위, 기존 Action 노드 ID·필드 연결과 Guard의 Lower Priority 설정 유지. 분리된 Sequence 진입 위치에는 새 Run Subgraph 노드 추가.

실행 복제본 2개에서 개체별 변수 독립성·상하위 변수 참조 연결 및 사망에 따른 교전·순찰 Subgraph 중단과 Stop 이동 초기화 검증. 실제 몬스터의 이동·공격·복귀 Play 테스트는 별도 수동 확인 필요.

1. 참조 전달: 씬 Manager의 Inspector Player → Context.BindPlayer → Target Handler 주입·즉시 판정. 초기 연결·재활성화 때만 수행.
2. 대상 인식: 주입 Player 생존·활성 → 현재 몬스터 원점 기준 Detector 중심 거리 → 범위 안에서 시야 확인·Target 선택 → Target Handler 높이 판정 → wrapper HasValidTarget → 추적·공격 분기.
3. 대상 상실: 유효 판정 false → ChaseReturn의 교전 기록으로 상실 대기 활성화 → Action 타이머. 재인식하면 추적 재개, 만료하면 Handler BeginReturn의 즉시 플래그 기록으로 복귀.
4. 복귀 유지: 대기 만료 후 NeedsReturn이 상실 대기보다 우선. 복귀 중 대상을 재인식해도 복귀 완료 전 교전 분기로 전환하지 않음.
5. 복귀 완료: Handler CompleteReturn → 기록·플래그 즉시 초기화 → 다음 순찰·교전 선택 가능.
6. 자기 사망: Health 구독 → wrapper IsDead → 최우선 Stop. 다음 Context 갱신에서 선택 대상·주입 참조 전체 해제.

## 9. 초기화·수명·설정 유지 기준

- 시간·반경 Blackboard 설정은 Start의 Handler Initialize에서 전달. Inspector 런타임 값 변경을 자동 동기화하는 기능은 없음.
- 거리·높이 설정은 Handler 속성·함수 호출 시 현재 필드값 사용.
- HomeX는 최초 Start 시점 유지. 순찰·복귀 목적지에만 사용하고 추적 범위를 제한하지 않음. 비활성화·재활성화나 복귀 때 재기록하지 않음.
- 비활성화 시 자기 사망 구독 해제·주입 참조 전체 해제·이동 초기화. 재활성화 시 사망 구독 복구 후 씬 Player 재주입. Player 변경 이벤트 없음.
- 자기 사망 구독 Dispose는 해당 구독만 해제하며 Health의 ReactiveProperty 자체를 Dispose하지 않음. Target Handler에는 구독 없음.
- 각 Context의 Handler·wrapper, 각 Target Handler의 단일 Player 캐시는 개체별 독립.
- 순찰·복귀·배후 접근 도착은 기존 ArrivalDistance 범위 사용. 정확한 0 도착이나 추가 공용 거리 오차를 요구하지 않음.
- Editor API로 Goblin의 기존 높이 1을 Target Recognition으로 이전 완료. 상실 대기 1.5 등 나머지 설정 유지. 초기 위치 기준 최대 추적 거리 8 설정은 현재 코드에서 제거. InGame의 ScenePlayerManager에는 씬의 ProjectRE_Player Variant 할당. 이번 시야 기준 변경에서 Scene·Prefab·Graph 저장이나 설정 이전 없음.
- 테스트는 ScenePlayerManager와 Inspector Player가 있는 InGame 직접 실행. Title의 등록 흐름 연결은 추후 작업.
- Boss 패턴·지형 검사·AI 휴면·검사 주기 분산은 이번 변경에 포함하지 않음.
- Handler·Action API나 Blackboard 소유권 변경 시 해당 표와 실행 흐름을 함께 갱신.

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

## 13. 현재 몬스터 시야 기준 추적·복귀

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
