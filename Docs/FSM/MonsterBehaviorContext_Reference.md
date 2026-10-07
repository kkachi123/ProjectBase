# MonsterBehaviorContext · Handler · Action 연결 정리

작성 기준: 2026-10-07 현재 작업 트리의 소스와 저장된 `BasicMonsterBehavior.asset`.

현재 구현을 설명하는 참고 문서. 리팩토링 계획이나 Play 모드 검증 결과가 아님. 아래 기본값은 스크립트 선언값이며, Prefab·Scene Inspector 값과 Override는 별도 적용.

## 1. 제작 목적과 책임 분리

`MonsterBehaviorContext`는 몬스터가 감지한 대상과 행동 설정을 Behavior Graph에 전달하는 연결 Component. 이동·공격 실행이나 FSM 상태 전환을 직접 수행하지 않음.

| 구성 요소 | 제작 목적 | 담당하지 않는 부분 |
|---|---|---|
| `MonsterBehaviorContext` | 자기·대상 참조 캐시, Handler 초기화, 판정 조합, Blackboard 전달 | 이동 실행, 공격 실행, 대기 타이머 |
| `MonsterAttackBehaviorHandler` | 공격·배후 접근 설정, 수평 공격 거리·허용 높이 판정 | 공격 입력 발행, 공격 State 진입, 배후 이동 타이머 |
| `MonsterChaseReturnBehaviorHandler` | 초기 위치, 교전·복귀 기록, 추적 제한·상실 대기 필요 판정 | 이동 입력, 실제 상실 대기 시간 경과 처리 |
| `MonsterPatrolBehaviorHandler` | 순찰 반경·정지 시간 설정 보관 | 목적지 선택, 순찰 이동, 정지 타이머 |
| Action | Context·Blackboard를 읽고 이동·공격 입력 전달. 행동별 목적지·타이머 관리 | 공통 감지 판정, FSM 직접 전환 |
| `MonsterFlagCondition` | 연결된 Blackboard bool 확인 | 감지·거리 계산, Action 실행 |
| `MonsterInput` | 이동 명령 보관, 단발 공격 요청 Event 발행 | 행동 선택, 공격 가능 여부 판정 |

```text
PlayerDetector + Health
          ↓
MonsterBehaviorContext ← 개체별 Handler 3개
          ├─ 읽기 전용 속성·함수 → Action
          └─ Blackboard → Condition / Action / 기본 대기 노드
                                      ↓
                                 MonsterInput
                                      ↓
                         기존 Controller · FSM · Motor · Animator
```

Handler는 `[Serializable]` 일반 C# 클래스. Context의 직렬화 필드로 개체별 보유. 별도 Component나 자체 `Update()` 없음. Context·Graph·다른 Handler를 역참조하지 않음.

이 문서의 **직접 사용**은 C# 호출, **간접 사용**은 Context 전달 함수 또는 Blackboard 연결을 거친 사용을 의미.

## 2. MonsterBehaviorContext

소스: [MonsterBehaviorContext.cs](E:/Unity/Project/ProjectBase/Assets/Scripts/FSM/NPC/AIMonstor/@Behavior/MonsterBehaviorContext.cs)

### 2.1 클래스 설정

| 설정 | 목적 |
|---|---|
| `DisallowMultipleComponent` | 같은 GameObject에 Context 중복 추가 방지 |
| `DefaultExecutionOrder(-100)` | 기본 실행 순서 Component보다 초기화·갱신을 먼저 수행하도록 순서 지정. 모든 Component보다 먼저 실행한다는 의미는 아님 |
| `RequireComponent` | `MonsterInput`, `PlayerDetector`, `Health`, `BehaviorGraphAgent` 의존 Component 선언 |
| `Header` | Inspector에서 공격, 추적·복귀, 순찰 설정 그룹 구분 |

자기 몸통 `Collider2D`는 `Awake()`에서 공격 Handler에 주입. 현재 `RequireComponent` 목록에는 포함되지 않음.

### 2.2 Handler 필드

| 변수 | 제작 목적 | 실제 사용 경로 |
|---|---|---|
| `_attack` | 공격 설정과 거리·높이 판정 보관 | `Awake()` 설정 전달, `Update()` 높이 판정, `AttackDistance`·`IsInAttackRange()` 전달 → 추적·공격 요청·배후 접근 Action |
| `_chaseReturn` | 초기 위치·교전·복귀 상태 관리 | `Start()` 초기화, `Update()` 제한 판정, 교전·복귀 전달 함수 → 추적·복귀·상실 대기 Action 및 관련 Condition |
| `_patrol` | 순찰 설정 보관 | `Awake()` → `PatrolRadius`, `PatrolWaitMin`, `PatrolWaitMax` Blackboard → 순찰 Action·기본 `WaitRangeAction` |

### 2.3 자기·대상 참조 변수

| 변수 | 제작 목적 | 설정·사용 지점 | Action·Condition 연결 |
|---|---|---|---|
| `_input` | 이 몬스터의 `MonsterInput` 캐시 | `Awake()`에서 확보, Blackboard `Input`에 전달. `OnDisable()`에서 이동 초기화 | 모든 Monster Action이 Blackboard `Input`으로 사용 |
| `_detector` | 대상 감지 Component 캐시 | `Awake()`에서 확보, `Update()`에서 시야 확인 | `HasValidTarget`·`TargetRoot`를 통해 대상 관련 Action·Condition에 간접 전달 |
| `_health` | 자기 사망 여부 확인 | `Awake()`에서 확보, `IsDead`에서 읽음 | Blackboard `IsDead` → 사망 분기의 `MonsterFlagCondition` → `MonsterStopAction` |
| `_agent` | 실행 Graph의 Blackboard 접근 | `Awake()`에서 확보, `SetVariableValue()`·`GetVariable()`에 사용 | Action·Condition의 변수 연결을 준비. 입력 실행은 하지 않음 |
| `_target` | Detector가 반환한 Transform 및 변경 여부 기록 | `UpdateTargetReferences()`에서 이전 대상과 비교 | 대상 변경 때만 Health·Collider 참조 재검색. Action에 직접 노출하지 않음 |
| `_targetHealth` | 감지 대상의 생존 확인 및 행동 기준 Transform 확보 | 대상 변경 시 `GetComponentInParent<Health>()`로 캐시 | `HasValidTarget`, `TargetRoot` → 추적·공격 요청·배후 접근·상실 대기 |
| `_targetBodyCollider` | 대상 몸통의 월드 바닥 높이 계산 | 대상 Health GameObject의 `Collider2D` 캐시 → `_attack.IsWithinHeightRange()` | 높이 판정 → `HasValidTarget` → 대상 유효 Condition·상실 대기 분기 |

`_target`은 감지된 Collider의 Transform일 수 있음. `TargetRoot`는 그 대상에서 찾은 **Health Component가 붙은 Transform**. 반드시 최상위 `transform.root`를 의미하지 않음.

Context의 자기 Component 검색은 `Awake()`에 위치. 대상 Component 검색은 대상 Transform이 바뀔 때만 수행. 단, 호출되는 `PlayerDetector.IsTargetInView()` 내부의 후보 대상 검색까지 없어졌다는 의미는 아님.

### 2.4 Blackboard 참조 변수

모두 `Start()`에서 실행 Blackboard의 변수 참조를 캐시하고, `PublishFlags()`에서 `.Value` 갱신. 별도 판정값을 중복 보관하는 필드가 아니라 Graph가 읽을 변수의 참조.

| 변수 | Blackboard 이름 | 전달 값 | 사용 Action·Condition |
|---|---|---|---|
| `_deadVariable` | `IsDead` | `IsDead` | 사망 Guard의 `MonsterFlagCondition.Flag` → `MonsterStopAction` |
| `_returnVariable` | `NeedsReturn` | `NeedsReturn` | 복귀 Guard의 `MonsterFlagCondition.Flag` → `MonsterReturnAction` |
| `_lostVariable` | `NeedsLostTargetWait` | `NeedsLostTargetWait` | 상실 대기 Guard의 `MonsterFlagCondition.Flag` → `MonsterWaitForTargetAction` |
| `_targetValidVariable` | `HasValidTarget` | `HasValidTarget` | 교전 Guard의 `MonsterFlagCondition.Flag` → 추적·공격 분기 |
| `_targetVariable` | `Target` | `TargetRoot.gameObject` 또는 `null` | 현재 연결된 Monster Action은 이 변수를 직접 읽지 않고 `Context.TargetRoot` 사용 |

### 2.5 공개 속성

| 속성 | 제작 목적·값의 출처 | 직접 사용 | 간접 사용 |
|---|---|---|---|
| `HomeX` | 추적·복귀 Handler가 저장한 초기 X 좌표 | `MonsterPatrolAction.OnStart()`, `MonsterReturnAction.OnUpdate()` | `Start()`에서 Blackboard `HomeX`에도 전달. 현재 Action은 Context 속성으로 읽음 |
| `AttackDistance` | 공격 Handler의 수평 공격 거리 | `MonsterMoveBehindTargetAction.OnUpdate()`의 배후 목적지 계산 | 추적·공격 요청의 거리 판정은 이 속성 대신 `IsInAttackRange()` 사용 |
| `ArrivalDistance` | 추적·복귀 Handler의 도착 인정 범위 | `MonsterPatrolAction`, `MonsterReturnAction`, `MonsterMoveBehindTargetAction`의 `OnUpdate()` | 순찰·복귀·배후 도착 판정에 같은 범위 사용 |
| `IsDead` | 자기 `_health.IsDead.Value` | Context `Update()`·`PublishFlags()` | Blackboard `IsDead` → `MonsterFlagCondition` |
| `HasValidTarget` | 감지·생존·초기 위치 기준 대상 추적 범위·높이 조건을 조합한 결과 | 추적·공격 요청·배후 접근·상실 대기 Action | Blackboard `HasValidTarget` → `MonsterFlagCondition`; 상실 대기 필요 판정에도 사용 |
| `NeedsReturn` | 복귀 진행 중이거나 자기 초기 위치 제한 초과 | 추적·공격 요청·배후 접근 Action에서 실패 판정 | Blackboard `NeedsReturn` → 복귀 Condition; 상실 대기 필요 판정에도 사용 |
| `NeedsLostTargetWait` | 교전 경험이 있고 유효 대상이 없으며 복귀 필요 상태가 아닌지 확인 | Context `PublishFlags()` | Blackboard 값 → 상실 대기 Condition. 대기 Action 자체는 이 속성을 직접 읽지 않음 |
| `TargetRoot` | `_targetHealth.transform` 또는 `null` | 추적·공격 요청·배후 접근 Action에서 위치·방향 확인. Context에서 추적 범위 판정 | Blackboard `Target` 전달 |

`HasValidTarget == true`는 **공격 거리 안에 있음**을 의미하지 않음. 공격 거리 판단은 추적·공격 요청 Action에서 별도 수행.

`TargetRoot != null`과 `HasValidTarget == true`도 같은 의미가 아님. 대상이 감지됐지만 높이·추적 범위 조건을 벗어나면 Transform은 남고 유효 판정만 false가 될 수 있음.

### 2.6 함수

| 함수 | 제작 목적·처리 내용 | 호출 주체 | 연결된 Action·Condition |
|---|---|---|---|
| `Awake()` | 자기 참조 캐시, 공격 Handler에 몸통 Collider 주입, Context·Input·설정값 Blackboard 전달 | Unity 생명주기 | 전체 Action의 참조·설정 준비 |
| `Start()` | 추적·복귀 Handler에 자기 Transform·초기 X 주입, `HomeX` 전달, 동적 Blackboard 참조 캐시 | Unity 생명주기 | 순찰·복귀 기준점, Condition의 동적 플래그 갱신 준비 |
| `Update()` | 살아 있을 때 Detector 확인 → 대상 캐시 갱신 → 유효 대상 판정 → 자기 추적 제한 갱신 → Blackboard 반영 | Unity 생명주기 | 네 플래그 Condition과 대상 관련 Action에 최신 판정 제공 |
| `UpdateTargetReferences(Transform target)` | 대상이 변경됐을 때만 Health·몸통 Collider 캐시 갱신. 감지 해제 시 참조 초기화 | `Update()` | 대상 관련 Action에 `TargetRoot` 제공, 높이·생존 판정 준비 |
| `BeginEngagement()` | Handler에 교전 시작 기록 | `MonsterChaseAction.OnStart()` | 이전에 추적한 대상 상실과 평상시 순찰을 구분 |
| `BeginReturn()` | 복귀 상태 기록 후 즉시 `PublishFlags()` 호출 | `MonsterWaitForTargetAction.OnUpdate()`, `MonsterReturnAction.OnStart()` | 대기 만료 후 복귀 Guard 활성화, 복귀 중 재추적 방지 |
| `CompleteReturn()` | 교전·복귀 기록 초기화 후 즉시 `PublishFlags()` 호출 | `MonsterReturnAction.OnUpdate()` | 복귀 Guard 해제, 이후 대상 유효 여부에 따라 교전·순찰 선택 가능 |
| `IsInAttackRange(float deltaX)` | 공격 Handler의 수평 거리 판정 전달 | `MonsterChaseAction.OnUpdate()`, `MonsterRequestAttackAction.OnUpdate()` | 추적 종료·공격 요청 거리 조건 통일 |
| `PublishFlags()` | 사망·복귀·상실 대기·대상 유효·대상 GameObject를 실행 Blackboard에 반영 | `Update()`, `BeginReturn()`, `CompleteReturn()` | `MonsterFlagCondition`의 판단 입력 갱신 |
| `OnDisable()` | 자기 이동 입력을 0으로 초기화 | Unity 생명주기 | 비활성화 시 남은 이동 명령 제거. Handler 기록·타이머 초기화 함수는 아님 |

## 3. MonsterAttackBehaviorHandler

소스: [MonsterAttackBehaviorHandler.cs](E:/Unity/Project/ProjectBase/Assets/Scripts/FSM/NPC/AIMonstor/@Behavior/Handlers/MonsterAttackBehaviorHandler.cs)

공격 관련 **설정·판정**을 보관하는 관리자. 공격 요청을 보내거나 공격 애니메이션을 직접 시작하지 않음.

### 3.1 변수·속성

| 변수·속성 | 코드 기본값 | 제작 목적 | 사용 경로·Action·Condition |
|---|---|---|---|
| `_attackDistance` | `1.8f` | 수평 공격 거리 기준 | `IsInAttackRange()` → Context → 추적·공격 요청 Action. `AttackDistance` → 배후 접근 목적지 |
| `_maxAttackHeightDifference` | `0.75f` | 자기·대상 몸통 Collider 바닥 높이 차이의 허용값 | `IsWithinHeightRange()` → Context `HasValidTarget` → 대상 유효 Condition 및 상실 대기 판단 |
| `_attackRequestInterval` | `1f` | 공격 입력 요청 간격 설정 | `AttackRequestInterval` → Blackboard 동명 변수 → 공격 요청 Action `Interval`, 교전 마지막 기본 `WaitAction` |
| `_rearApproachTimeout` | `1.5f` | 배후 접근을 시도할 최대 시간 설정 | `RearApproachTimeout` → Blackboard 동명 변수 → 배후 접근 Action `Timeout` |
| `_bodyCollider` | 주입 전 `null` | 자기 몸통의 월드 바닥 높이 참조 | Context `Awake()` → `Initialize()`; 높이 판정에서 사용 |
| `AttackDistance` | `_attackDistance` 전달 | 배후 목적지 계산에 필요한 거리 공개 | Context 동명 속성 → `MonsterMoveBehindTargetAction` |
| `AttackRequestInterval` | `_attackRequestInterval` 전달 | Graph에 넘길 요청 간격 공개 | Context `Awake()` → Blackboard → 공격 요청 Action·기본 대기 노드 |
| `RearApproachTimeout` | `_rearApproachTimeout` 전달 | Graph에 넘길 접근 제한 시간 공개 | Context `Awake()` → Blackboard → 배후 접근 Action |

높이 비교 기준은 `transform.position.y`나 Sprite 크기가 아니라 `Collider2D.bounds.min.y`. Collider의 월드 크기·Offset·Transform Scale이 반영된 바닥 위치 사용.

### 3.2 함수

| 함수 | 제작 목적·판정식 | 직접 호출 | 최종 사용 |
|---|---|---|---|
| `Initialize(Collider2D bodyCollider)` | 자기 몸통 Collider 의존성 주입 | Context `Awake()` | 높이 판정 준비 |
| `IsInAttackRange(float deltaX)` | `Abs(deltaX) <= _attackDistance` | Context 동명 전달 함수 | 추적 도착, 공격 요청 가능 거리 확인 |
| `IsWithinHeightRange(Collider2D targetBodyCollider)` | `Abs(대상 바닥 Y - 자기 바닥 Y) <= _maxAttackHeightDifference` | Context `Update()` | 유효 대상·상실 대기 분기 판단 |

거리 판정에는 별도의 공용 `DistanceTolerance`를 더하지 않음. 공격 방향 정렬의 `FacingDeadZone`은 공격 요청 Action의 별도 역할.

## 4. MonsterChaseReturnBehaviorHandler

소스: [MonsterChaseReturnBehaviorHandler.cs](E:/Unity/Project/ProjectBase/Assets/Scripts/FSM/NPC/AIMonstor/@Behavior/Handlers/MonsterChaseReturnBehaviorHandler.cs)

어디까지 추적할지, 이미 교전했는지, 복귀를 완료할 때까지 유지할지를 기록하는 관리자. 실제 이동·대기 타이머는 Action 담당.

### 4.1 변수·속성

| 변수·속성 | 코드 기본값 | 제작 목적 | 사용 경로·Action·Condition |
|---|---|---|---|
| `_maxChaseDistance` | `8f` | 초기 위치에서 허용할 수평 범위 | 대상 위치 비교 → `HasValidTarget`; 자기 위치 비교 → `NeedsReturn`. 각각 대상 유효·복귀 Condition에 전달 |
| `_returnArrivalDistance` | `0.15f` | 목적지에 도착했다고 인정할 수평 범위 | `ArrivalDistance` → Context → 순찰·복귀·배후 접근 Action |
| `_lostTargetWait` | `2f` | 교전 대상 상실 후 대기 시간 설정 | `LostTargetWait` → Blackboard → `MonsterWaitForTargetAction.Seconds` |
| `_owner` | 주입 전 `null` | 몬스터 자신의 현재 위치 참조 | Context `Start()`에서 주입, `UpdateState()`의 자기 범위 판정 |
| `_isEngaged` | `false` | 추적을 시작한 교전 기록 | 추적 Action → `BeginEngagement()`; 상실 대기 필요 판정에 사용. 복귀 완료 시 false |
| `_isReturning` | `false` | 복귀 시작 이후 완료까지 복귀 유지 | 대기 만료·복귀 시작 → `BeginReturn()`; `UpdateState()`에서 복귀 플래그 유지. 복귀 완료 시 false |
| `HomeX` | 초기화 전 `0f` | 초기 위치의 X 기록 | Context `Start()`에서 저장 → 대상·자기 범위 판정, 순찰·복귀 기준점 |
| `ArrivalDistance` | `_returnArrivalDistance` 전달 | 도착 범위 공개 | Context 동명 속성 → 순찰·복귀·배후 접근 Action |
| `LostTargetWait` | `_lostTargetWait` 전달 | 상실 대기 시간 공개 | Context `Awake()` → Blackboard → 상실 대기 Action |
| `NeedsReturn` | 초기 `false` | 자기 범위 초과 또는 복귀 진행 상태 | Context 속성·Blackboard → 복귀 Condition. 추적·공격 요청·배후 접근 중단 판단에도 사용 |

`_maxChaseDistance`의 기준은 **몬스터 초기 X 위치**. 몬스터와 대상 사이의 현재 거리와 다름.

`_isEngaged`는 현재 공격 State인지 나타내는 값이 아님. 추적을 시작한 이력이 있어 대상 상실 대기가 필요한지를 구분하는 기록.

### 4.2 함수

| 함수 | 제작 목적·처리 내용 | 직접 호출 | Action·Condition 연결 |
|---|---|---|---|
| `Initialize(Transform owner, float homeX)` | 자기 Transform·초기 X 저장 | Context `Start()` | 순찰·복귀·추적 제한 기준 준비 |
| `IsWithinChaseRange(float targetX)` | `Abs(targetX - HomeX) <= _maxChaseDistance` | Context `Update()` | 대상 유효 판정 → 교전·상실 대기 분기 |
| `UpdateState()` | `NeedsReturn = _isReturning \|\| Abs(자기 X - HomeX) > _maxChaseDistance` | Context `Update()` | 자기 제한 초과는 즉시 복귀. 대상 높이·거리 조건 상실만으로 즉시 복귀하지 않음 |
| `NeedsLostTargetWait(bool hasValidTarget)` | `_isEngaged && !hasValidTarget && !NeedsReturn` | Context 동명 속성 | Blackboard → 상실 대기 Condition |
| `BeginEngagement()` | `_isEngaged = true` | Context 전달 함수 ← 추적 Action `OnStart()` | 교전 후 상실 대기를 활성화할 기록 |
| `BeginReturn()` | `_isReturning = true`, `NeedsReturn = true` | Context 전달 함수 ← 상실 대기 Action·복귀 Action | 복귀 분기 활성화, 복귀 완료 전 재추적 억제 |
| `CompleteReturn()` | `_isReturning`, `_isEngaged`, `NeedsReturn` 모두 false | Context 전달 함수 ← 복귀 Action `OnUpdate()` | 복귀·상실 대기 기록 종료 |

Handler 자체는 Blackboard를 갱신하지 않음. `BeginReturn()`·`CompleteReturn()` 호출 뒤 같은 프레임 Blackboard 반영은 Context의 전달 함수가 담당.

## 5. MonsterPatrolBehaviorHandler

소스: [MonsterPatrolBehaviorHandler.cs](E:/Unity/Project/ProjectBase/Assets/Scripts/FSM/NPC/AIMonstor/@Behavior/Handlers/MonsterPatrolBehaviorHandler.cs)

현재 순찰 실행 함수 없이 설정값만 보관. 목적지·타이머를 Handler로 옮긴 구조가 아님.

| 변수·속성 | 코드 기본값 | 제작 목적 | Action 연결 |
|---|---|---|---|
| `_patrolRadius` | `2f` | 초기 X를 중심으로 순찰 목적지를 뽑을 반경 | `PatrolRadius` → Context `Awake()` → Blackboard → `MonsterPatrolAction.Radius` |
| `_patrolWaitMin` | `1f` | 순찰 후 랜덤 정지 시간의 최소값 | `PatrolWaitMin` → Blackboard → 기본 `WaitRangeAction.Min` |
| `_patrolWaitMax` | `2f` | 순찰 후 랜덤 정지 시간의 최대값 | `PatrolWaitMax` → Blackboard → 기본 `WaitRangeAction.Max` |
| `PatrolRadius` | `_patrolRadius` 전달 | 순찰 반경 공개 | 순찰 Action의 목적지 선택 |
| `PatrolWaitMin` | `_patrolWaitMin` 전달 | 랜덤 대기 최소 시간 공개 | 기본 범위 대기 노드 |
| `PatrolWaitMax` | `_patrolWaitMax` 전달 | 랜덤 대기 최대 시간 공개 | 기본 범위 대기 노드 |

## 6. Blackboard → Action·Condition 연결표

Graph 소스: [BasicMonsterBehavior.asset](E:/Unity/Project/ProjectBase/Assets/Prefabs/Monster/BasicMonsterBehavior.asset)

| Blackboard 변수 | 값을 설정하는 곳 | 실제 읽는 곳·필드 | 제작 목적 |
|---|---|---|---|
| `Context` | Context `Awake()` | Stop을 제외한 6종 Monster Action의 `Context` | 판정·대상·초기 위치·기록 변경 함수 접근 |
| `Input` | Context `Awake()` | 7종 Monster Action의 `Input` | 이동·공격 요청 전달 |
| `HomeX` | Context `Start()` | 현재 연결된 Action 필드에는 직접 연결 없음 | 초기 X의 Blackboard 표현. 순찰·복귀 Action은 `Context.HomeX` 사용 |
| `Target` | Context `PublishFlags()` | 현재 연결된 Action 필드에는 직접 연결 없음 | 감지 대상 Health GameObject의 Blackboard 표현. 대상 Action은 `Context.TargetRoot` 사용 |
| `IsDead` | Context `PublishFlags()` | 사망 Guard의 `MonsterFlagCondition.Flag` | 최우선 정지 분기 조건 |
| `NeedsReturn` | Context `PublishFlags()` | 복귀 Guard의 `MonsterFlagCondition.Flag` | 복귀 분기 조건 |
| `NeedsLostTargetWait` | Context `PublishFlags()` | 상실 대기 Guard의 `MonsterFlagCondition.Flag` | 대상 상실 후 정지 대기 분기 조건 |
| `HasValidTarget` | Context `PublishFlags()` | 교전 Guard의 `MonsterFlagCondition.Flag` | 추적·공격 분기 조건 |
| `AttackRequestInterval` | Context `Awake()` | 두 공격 요청 노드의 `Interval`, 교전 마지막 기본 `WaitAction.SecondsToWait` | 요청 간격 및 교전 분기 재실행 전 대기 |
| `NextAttackRequestTime` | 공격 요청 Action `OnUpdate()` | 두 공격 요청 노드의 `NextRequestTime` | 다음 공격 요청 허용 시각. 동일 개체의 두 공격 분기가 같은 변수 사용 |
| `LostTargetWait` | Context `Awake()` | 상실 대기 Action `Seconds` | 대상 상실 후 대기 길이 |
| `PatrolRadius` | Context `Awake()` | 순찰 Action `Radius` | 랜덤 목적지 선택 범위 |
| `PatrolWaitMin` | Context `Awake()` | 기본 `WaitRangeAction.Min` | 순찰 정지 시간 최소값 |
| `PatrolWaitMax` | Context `Awake()` | 기본 `WaitRangeAction.Max` | 순찰 정지 시간 최대값 |
| `RearApproachTimeout` | Context `Awake()` | 배후 접근 Action `Timeout` | 배후 접근 제한 시간 |
| `Self` | Context에서는 설정하지 않음 | 현재 연결된 Monster Action·Flag Condition에서 직접 사용 없음 | Graph에 선언된 자기 GameObject 변수 |

`NextAttackRequestTime`은 Context가 캐시하거나 매 프레임 덮어쓰지 않음. Action이 갱신하는 실행 기록. 현재 Graph의 공유 변수 목록은 비어 있으므로, 여기서 공유는 서로 다른 몬스터가 아니라 **한 몬스터 실행 Graph 안의 두 공격 노드 사이**를 의미.

## 7. Action별 사용 목적과 실행 함수

Action의 `Context`, `Input`, 시간·반경 필드는 Blackboard 연결을 통해 값을 받음. Handler 인스턴스를 직접 전달받지 않음.

### 7.1 MonsterChaseAction

소스: [MonsterChaseAction.cs](E:/Unity/Project/ProjectBase/Assets/Scripts/FSM/NPC/AIMonstor/@Behavior/Actions/MonsterChaseAction.cs)

목적: 유효한 대상을 수평 공격 거리까지 추적.

| 함수 | 사용하는 Context 요소 | 처리 |
|---|---|---|
| `OnStart()` | `BeginEngagement()` | 교전 시작 기록, Running 반환 |
| `OnUpdate()` | `HasValidTarget`, `NeedsReturn`, `TargetRoot`, `transform`, `IsInAttackRange()` | 대상 상실·복귀 필요 시 Failure. 공격 거리 도착 시 Success. 나머지는 대상 방향 이동 입력 |
| `OnEnd()` | Context 사용 없음 | `Input.SetMovement(Vector2.zero)`로 도착·실패·중단 시 이동 초기화 |

자체 목적지·타이머 필드 없음. 대상 위치를 매 갱신 읽음. 현재 Graph에는 기본 추적과 배후 접근 실패 대체용으로 같은 Action 타입의 노드가 두 개 존재.

### 7.2 MonsterRequestAttackAction

소스: [MonsterRequestAttackAction.cs](E:/Unity/Project/ProjectBase/Assets/Scripts/FSM/NPC/AIMonstor/@Behavior/Actions/MonsterRequestAttackAction.cs)

목적: 대상 방향을 이동 입력으로 맞추고, 요청 간격에 따라 단발 공격 입력 발행.

| 변수·함수 | 목적·사용 |
|---|---|
| `FacingDeadZone = 0.01f` | 대상과 X 위치가 거의 같은 경우 불필요한 방향 정렬 방지. 거리 조건에 더하는 허용 오차가 아님 |
| `Interval` | Blackboard `AttackRequestInterval` 연결. 다음 요청 허용 시각 계산 |
| `NextRequestTime` | Blackboard `NextAttackRequestTime` 연결. 공격 경로가 바뀌어도 요청 간격 기록 유지 |
| `OnStart()` | Running 반환. 즉시 공격 요청하지 않음 |
| `OnUpdate()` | `HasValidTarget`, `NeedsReturn`, `TargetRoot`, `transform`, `IsInAttackRange()` 사용. 대상·거리 확인 → `transform.localScale.x`로 자기 방향 확인 → 필요 시 좌우 이동 입력 → 정지 → 시간 확인 → 허용 시각 갱신·`Input.RequestAttack()` → Success |
| `OnEnd()` | 방향 정렬을 위해 남긴 이동 입력 초기화 |

`RequestAttack()` 성공은 공격 요청을 보냈다는 의미. 실제 공격 가능 여부·FSM 진입은 `AttackTransition`과 `MonsterController.TryStartAttack()`이 판단. 이 Action은 공격 애니메이션 종료를 기다리지 않음.

### 7.3 MonsterMoveBehindTargetAction

소스: [MonsterMoveBehindTargetAction.cs](E:/Unity/Project/ProjectBase/Assets/Scripts/FSM/NPC/AIMonstor/@Behavior/Actions/MonsterMoveBehindTargetAction.cs)

목적: 대상 배후의 수평 목적지로 접근. 실패하면 Graph의 다음 Selector 자식인 일반 추적으로 대체.

| 변수·함수 | 목적·사용 |
|---|---|
| `Timeout` | Blackboard `RearApproachTimeout` 연결 |
| `_target` | 접근 시작 시 `Context.TargetRoot` 저장. 접근 중 대상 교체 확인 |
| `_targetFacing` | 시작 시 대상 `localScale.x`의 부호 저장. 접근 중 방향 재선택 제외 |
| `_endTime` | `Time.time + Timeout.Value`로 이번 접근 제한 시각 저장 |
| `OnStart()` | `HasValidTarget` 확인 후 대상·방향·종료 시각 저장 |
| `OnUpdate()` | `HasValidTarget`, `NeedsReturn`, `TargetRoot`, `AttackDistance`, `ArrivalDistance`, `transform` 사용. 목적지까지 이동. 도착 Success, 대상 상실·교체·복귀 필요·시간 초과 Failure |
| `OnEnd()` | 도착·실패·중단 시 이동 입력 초기화 |

배후 목적지: `대상 현재 X - 시작 때 저장한 대상 방향 × AttackDistance × 0.8f`.

대상 위치는 계속 따라가지만 방향은 시작 시점 값을 유지. 현재 코드에는 Y 이동, 경로 탐색, 벽 회피가 없음.

### 7.4 MonsterWaitForTargetAction

소스: [MonsterWaitForTargetAction.cs](E:/Unity/Project/ProjectBase/Assets/Scripts/FSM/NPC/AIMonstor/@Behavior/Actions/MonsterWaitForTargetAction.cs)

목적: 교전 대상이 유효하지 않게 됐을 때 정지 대기. 재인식 시 취소, 만료 시 복귀 시작.

| 변수·함수 | 목적·사용 |
|---|---|
| `Seconds` | Blackboard `LostTargetWait` 연결 |
| `_endTime` | `Time.time + Seconds.Value`로 이번 상실 대기 종료 시각 저장 |
| `OnStart()` | 이동 정지, 대기 종료 시각 기록 |
| `OnUpdate()` | `HasValidTarget`이면 Success로 대기 종료. 아직 시간이 남으면 Running. 만료 시 `Context.BeginReturn()` 후 Success |
| `OnEnd()` | 대기 종료·중단 시 이동 입력 초기화 |

`NeedsLostTargetWait`의 bool 판정은 Handler·Context, 시간을 실제로 세는 책임은 이 Action에 위치.

### 7.5 MonsterReturnAction

소스: [MonsterReturnAction.cs](E:/Unity/Project/ProjectBase/Assets/Scripts/FSM/NPC/AIMonstor/@Behavior/Actions/MonsterReturnAction.cs)

목적: 초기 X 위치로 복귀. 자체 타이머·목적지 필드 없음.

| 함수 | 사용하는 Context 요소 | 처리 |
|---|---|---|
| `OnStart()` | `BeginReturn()` | 복귀 진행 기록 |
| `OnUpdate()` | `HomeX`, `ArrivalDistance`, `transform`, `CompleteReturn()` | 초기 X 방향으로 이동. 도착 범위에 들어오면 교전·복귀 기록 초기화 후 Success |
| `OnEnd()` | Context 사용 없음 | 완료·중단 시 이동 입력 초기화 |

`HasValidTarget`을 직접 확인하지 않음. 복귀 유지 플래그와 Graph 우선순위로 복귀 완료 전 재추적 방지.

### 7.6 MonsterPatrolAction

소스: [MonsterPatrolAction.cs](E:/Unity/Project/ProjectBase/Assets/Scripts/FSM/NPC/AIMonstor/@Behavior/Actions/MonsterPatrolAction.cs)

목적: 초기 위치 주변에서 랜덤 X 목적지를 선택하고 이동.

| 변수·함수 | 목적·사용 |
|---|---|
| `Radius` | Blackboard `PatrolRadius` 연결 |
| `_destinationX` | `Context.HomeX + Random.Range(-Radius.Value, Radius.Value)`로 이번 순찰 목적지 저장 |
| `OnStart()` | 목적지 1회 선택 |
| `OnUpdate()` | `Context.transform.position.x`, `ArrivalDistance` 사용. 도착 Success, 나머지는 목적지 방향 이동 |
| `OnEnd()` | 완료·우선순위 중단 시 이동 입력 초기화 |

순찰 후 정지 시간은 이 Action에 없음. Graph의 `MonsterStopAction`과 기본 `WaitRangeAction`으로 분리.

### 7.7 MonsterStopAction

소스: [MonsterStopAction.cs](E:/Unity/Project/ProjectBase/Assets/Scripts/FSM/NPC/AIMonstor/@Behavior/Actions/MonsterStopAction.cs)

| 변수·함수 | 목적·사용 |
|---|---|
| `Input` | 이동 정지를 전달할 Blackboard 참조. Context 필드 없음 |
| `OnStart()` | `Input.SetMovement(Vector2.zero)` 후 Success 반환 |

현재 Graph에서는 사망 분기와 순찰 이동 후 정지에 사용. 이 Action 자체가 시간을 세거나 사망 애니메이션을 실행하지 않음.

## 8. MonsterFlagCondition과 실제 Graph 분기

소스: [MonsterFlagCondition.cs](E:/Unity/Project/ProjectBase/Assets/Scripts/FSM/NPC/AIMonstor/@Behavior/Conditions/MonsterFlagCondition.cs)

| 변수·함수 | 제작 목적 |
|---|---|
| `Flag` | 이 Condition 인스턴스가 확인할 Blackboard bool 연결 |
| `IsTrue()` | 연결된 `Flag.Value` 그대로 반환 |

Context·Handler를 직접 참조하지 않음. 같은 Condition 타입을 네 Guard에 배치하고 다른 플래그를 연결.

| 우선순위 | Condition의 `Flag` | 실행 분기 | Context·Handler에서 만든 조건 |
|---|---|---|---|
| 1 | `IsDead` | `MonsterStopAction` | 자기 Health 사망 |
| 2 | `NeedsReturn` | `MonsterReturnAction` | 자기 초기 위치 제한 초과 또는 복귀 진행 |
| 3 | `NeedsLostTargetWait` | `MonsterWaitForTargetAction` | 교전 이력 있음 + 유효 대상 없음 + 복귀 필요 아님 |
| 4 | `HasValidTarget` | 추적 → 랜덤 공격 행동 → 기본 대기 | 감지·생존·대상 추적 범위·허용 높이 충족 |
| 5 | 별도 Flag Condition 없음 | 순찰 → 정지 → 랜덤 시간 대기 | 위 분기에서 실행할 행동이 없는 경우의 기본 행동 |

저장된 Graph의 실제 행동 연결만 표시한 구조:

```text
반복 실행 → 우선순위 Selector
├─ IsDead              → Stop
├─ NeedsReturn         → Return
├─ NeedsLostTargetWait → WaitForTarget
├─ HasValidTarget      → Sequence
│  ├─ Chase
│  ├─ Random
│  │  ├─ Sequence
│  │  │  ├─ Selector: MoveBehindTarget → 실패 시 Chase
│  │  │  └─ RequestAttack
│  │  └─ RequestAttack
│  └─ Wait(AttackRequestInterval)
└─ Sequence
   ├─ Patrol
   ├─ Stop
   └─ WaitRange(PatrolWaitMin, PatrolWaitMax)
```

Graph가 분기 우선순위·랜덤 선택·실패 대체를 구성. Context와 Handler는 해당 분기를 선택하는 판정·설정·기록을 제공. Condition은 플래그만 확인.

## 9. 대표 실행 흐름

### 9.1 대상 인식 → 추적 → 공격

1. Context `Update()`에서 Detector를 통해 대상 인식.
2. 대상 생존·초기 위치 기준 추적 범위·높이 조건을 조합해 `HasValidTarget` 갱신.
3. `PublishFlags()` → Blackboard `HasValidTarget` → 교전 Condition.
4. `MonsterChaseAction.OnStart()` → Context `BeginEngagement()` → Handler `_isEngaged = true`.
5. 추적 Action이 `IsInAttackRange()`까지 이동 입력 전달.
6. Graph가 일반 공격 또는 배후 접근 후 공격 선택.
7. 공격 요청 Action이 대상·거리·방향·요청 시간을 확인하고 `MonsterInput.RequestAttack()` 발행.
8. 기존 `AttackTransition`이 요청을 받아 `MonsterController.TryStartAttack()` 판단 후 FSM 공격 진입.

공격 입력 경로 소스: [MonsterInput.cs](E:/Unity/Project/ProjectBase/Assets/Scripts/FSM/NPC/AIMonstor/Input/MonsterInput.cs), [AttackTransition.cs](E:/Unity/Project/ProjectBase/Assets/Scripts/FSM/Agent/StateControl/TransitionRules/Attack/AttackTransition.cs), [MonsterController.cs](E:/Unity/Project/ProjectBase/Assets/Scripts/FSM/NPC/AIMonstor/@Hub/MonsterController.cs), [MonsterStateFactory.cs](E:/Unity/Project/ProjectBase/Assets/Scripts/FSM/NPC/AIMonstor/MonsterState/MonsterStateFactory.cs).

### 9.2 대상 상실 → 대기 → 복귀

1. 시야 상실, 대상 사망, 허용 높이 초과 또는 대상 초기 위치 기준 추적 범위 초과로 `HasValidTarget = false`.
2. 교전 기록이 있고 자기 복귀 필요 조건은 없으면 `NeedsLostTargetWait = true`.
3. 상실 대기 Condition → `MonsterWaitForTargetAction.OnStart()`에서 정지·종료 시각 기록.
4. 유효 대상 재인식 시 대기를 끝내고 Graph의 교전 분기 선택 가능.
5. 재인식 없이 대기 만료 시 Context `BeginReturn()` → Handler 복귀 기록 → 즉시 Blackboard 갱신.
6. 복귀 Condition → `MonsterReturnAction` 실행.
7. `HomeX`의 `ArrivalDistance` 안에 도착하면 Context `CompleteReturn()` → 교전·복귀 기록 초기화.

몬스터 자신의 초기 위치 제한 초과는 `NeedsReturn = true`이므로 상실 대기보다 높은 우선순위로 즉시 복귀. 사망은 그보다 높은 정지 분기.

### 9.3 대상 없음 → 순찰 → 정지

교전 이력이 없는 상태에서 대상이 없으면 상실 대기를 실행하지 않음. 순찰 Action이 목적지 선택·이동을 수행하고, Stop으로 입력을 0으로 만든 뒤 기본 WaitRange가 설정 범위의 랜덤 시간 대기.

## 10. 설정값을 읽는 시점과 사용 시 주의점

| 구분 | 현재 동작 |
|---|---|
| Blackboard로 복사하는 시간·반경 설정 | Context `Awake()`에서 전달. 이후 Handler Inspector 값을 바꾸는 것만으로 Blackboard가 자동 동기화되는 코드는 없음 |
| Context·Handler에서 직접 읽는 거리·높이 설정 | 해당 속성·판정 함수 호출 때 Handler 값 사용 |
| 초기 위치 | Context `Start()` 시점 X 저장. 복귀할 때마다 현재 위치로 재설정하지 않음 |
| 도착 범위 | 순찰·복귀·배후 접근에서 `ArrivalDistance` 사용. 정확한 0 도착이나 추가 공용 거리 오차를 요구하지 않음 |
| 공격 거리 | 몬스터·대상 Health Transform의 수평 위치 차이 사용. Collider 표면 간 거리는 아님 |
| 높이 차이 | 자기·대상 몸통 Collider의 월드 바닥 위치 차이 사용 |
| 배후 위치 | 대상 방향은 접근 시작 때 고정, 위치는 접근 중 계속 확인 |
| 비활성화 | Context `OnDisable()`은 이동 입력만 초기화. 교전·복귀 기록을 모두 초기화하는 함수가 아님 |
| 입력과 FSM | Behavior는 입력을 발행. 실제 이동·공격 적용과 State 전이는 기존 FSM 책임 |

현재 구현에서는 `HomeX`, `Target`, `Self` Blackboard 변수를 연결된 Monster Action·Condition이 직접 읽지 않음. 이는 현재 사용 경로의 설명이며, 이 문서에서 삭제를 제안하거나 코드를 변경한 것은 아님.

## 11. 문서 갱신 기준

- Context·Handler 변수 또는 함수가 바뀌면 해당 목록과 사용 경로 갱신.
- Action이 참조하는 속성·함수가 바뀌면 Action별 표와 공개 속성 사용처 갱신.
- Graph의 Blackboard 연결·분기 순서가 바뀌면 연결표와 Graph 구조 갱신.
- 설정 기본값 변경은 선언값과 실제 Prefab·Scene Override를 구분해서 기록.
- 새로운 Handler나 Action 추가 시 입력 실행·판정·타이머의 소유 위치를 함께 기록.
