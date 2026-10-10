# FSM Monster Behavior 제작 가이드

기존 FSM Monster에 Unity Behavior Graph를 연결할 때 사용하는 AI 작업 지침. **BT는 입력을 결정하고, FSM은 실제 행동·애니메이션을 실행한다.** 현재 재사용 사례는 Grounded·Attack·Hit·Death를 사용하는 2D Grounded Monster다.

## 1. 작업 입력·소스 확인

- 먼저 [AGENTS.md](AGENTS.md)와 [Plan_Writing_Guide.md](Plan_Writing_Guide.md)를 읽는다. 변경 중인 파일·사용자 Graph 편집을 보존한다.
- 확인할 요구: 대상 Agent·Prefab, 입력 계약, 행동 목록·우선순위, 추적/상실/재인식 정책, 공격 선택·간격, 검증 씬과 설정값.
- Agent 자체 제작은 [FSM_Agent_Creation_Guide.md](FSM_Agent_Creation_Guide.md), 새 State는 [FSM_State_Extension_Guide.md](FSM_State_Extension_Guide.md)를 따른다. 판단 행동만 추가하는 경우 새 FSM State를 만들지 않는다.
- 아래 소스를 확인한 뒤 재사용·확장을 결정한다. 문서보다 현재 소스·작성 Graph·실행 Graph를 우선한다.

| 확인 대상 | 경로 |
| --- | --- |
| 조립·갱신·수명 | `Assets/Scripts/FSM/NPC/AIMonstor/@Behavior/MonsterBehaviorContext.cs` |
| 타입 지정 Blackboard | 같은 폴더의 `MonsterBehaviorBlackboard.cs` |
| 정보·판정·설정 | 같은 폴더의 `Handlers/` 4개 클래스 |
| 입력 실행 | 같은 폴더의 `Actions/` 6개 클래스 |
| 대상 연결·인식 | `Assets/Scripts/Managers/PlayerManager/ScenePlayerManager.cs`, `Assets/Scripts/FSM/@Detector/PlayerDetector.cs` |
| 입력·FSM 연결 | `Assets/Scripts/FSM/NPC/AIMonstor/Input/MonsterInput.cs`, 같은 Agent의 `@Hub/MonsterController.cs`, `MonsterState/MonsterStateFactory.cs` |
| 상위·하위 BT | `Assets/Prefabs/Monster/BasicMonsterBehavior.asset`, `MonsterCombatBehavior.asset`, `MonsterPatrolBehavior.asset` |

상세 API는 [Context 참조 문서](../MonsterBehaviorContext_Reference.md), 관계는 [클래스 다이어그램](../MonsterBehaviorContext_Class_Diagram.pdf)을 참고한다.

## 2. 책임과 조회 경로

| 계층 | 책임 |
| --- | --- |
| Context | 자기 Component 캐시, 의존성 주입, 초기화·갱신 순서·구독 수명, Handler 접근자 |
| Handler | 행동 설정·정보·판정·개체별 기록. Context·다른 Handler·Action 역참조 없음 |
| Blackboard wrapper | 변수 이름·타입·실행 참조를 Bind에서 한 번 확보. 속성으로 `.Value` 읽기·쓰기 |
| Graph | 순서·선택·우선순위·공통 중단·기본 대기 |
| Action | Handler 조회, 실행 목적지·타이머, Input 요청·정리 |
| FSM | 공격 가능 여부·State 전이·이동·피해·Animator·종료 처리 |

조회 경로는 `Graph의 Guard/Wait → Blackboard`, `커스텀 Action → Context.Handler`, `입력 실행 → Context.Input`으로 구분한다. Action별 거리·시간·bool 필드와 Context 단순 전달 getter를 늘리지 않는다.

현재 Blackboard:

| 변수 | 타입·사용 |
| --- | --- |
| Context | MonsterBehaviorContext. 모든 커스텀 Action의 유일한 연결 필드 |
| IsDead / HasValidTarget / LostTarget | bool. 우선순위 Guard |
| LostTargetWait | float. 기본 Wait |
| PatrolWaitMin / PatrolWaitMax | float. 기본 WaitRange |
| Self | Unity 기본 GameObject |

상위 8개, Combat은 Context·Self 2개, Patrol은 Context·Self·대기 범위 4개. 기본 노드가 직접 소비하는 값만 Blackboard에 추가한다. 공격 거리·HomeX·순찰 반경·요청 가능 시각은 Handler에서 관리하며 상태 bool을 별도 필드로 복제하지 않는다. `_isEngaged`는 최초 미감지와 교전 후 상실을 구분하는 이력이므로 유지한다.

## 3. Grounded Monster 기본 흐름

```text
반복 → 우선순위 Selector
├─ IsDead         → Stop
├─ HasValidTarget → Combat Subgraph
├─ LostTarget     → Stop → Wait(LostTargetWait) → Return
└─ Patrol Subgraph

Combat: Chase → Random(배후 접근 / Stop) → 공통 RequestAttack
Patrol: Patrol → Stop → WaitRange
```

- 사망 Guard: 최상위, LowerPriority. 교전·상실 Guard: Both. 조건은 Unity 기본 Variable Comparison 사용.
- 시야 상실은 교전을 중단한다. 대기·복귀·순찰 중 재인식하면 교전을 즉시 재개한다. 하위 On Start는 Repeat를 끄고 상위에서 반복한다.
- `LostTarget = _isEngaged && !HasValidTarget`. Chase 시작 시 교전 기록, 정상 복귀 완료 시 해제. 몬스터 자신의 최초 위치 이탈만으로 복귀시키지 않는다.
- 공통 Guard가 대상 유효성을 보장하는 경로에서만 Action의 중복 검사를 생략한다. Action을 다른 Graph에 배치할 때도 이 중단 계약을 확인한다.

### Chase·Attack 계약

- Chase가 `Target.DeltaX`와 `Attack.IsInAttackRange()`로 공격 거리 도착을 담당한다.
- Random은 배후 접근·Stop 중 하나만 실행한다. 배후 접근 성공 후 공통 RequestAttack, 시간 초과 시 Sequence 실패·그 회차 공격 요청 생략. 대체 Chase·Try In Order 없음.
- 배후 기준 방향은 OnStart에서 고정하고 Player의 현재 X는 계속 읽는다. 접근 거리 계수는 AttackDistance × 0.8, 종료는 ArrivalDistance 또는 제한 시간.
- RequestAttack은 방향 정렬 → 정지 → 요청 간격 대기 → 시각 기록 → 단발 입력 → Success. 공격 거리를 다시 검사하지 않는다.
- 방향 정렬은 기존 이동 입력을 사용하므로 짧은 수평 이동이 가능하다. FacingDeadZone 0.01은 방향 판정용이며 공격 거리·도착 허용 오차가 아니다.
- RequestAttack Success는 입력 발행 완료이지 실제 공격 시작·애니메이션 종료가 아니다. 입력 수락·공격 실행은 FSM 계약을 따른다. Graph가 공격 State를 직접 호출하지 않는다.
- 요청 시각은 Attack Handler의 개체별 필드에 보관한다. Subgraph 재진입·재인식으로 초기화하거나 공격 후 별도 Wait를 추가하지 않는다.

## 4. 참조·초기화·수명

- Awake: 자기 Component 캐시. Start: 실행 Blackboard Bind → Context 기록 → Handler 초기화 → 자기 사망 구독 → Player 주입·최초 판정. Context는 Graph보다 먼저 판정을 갱신한다.
- ScenePlayerManager의 Inspector Player를 Context가 읽고 Health·같은 GameObject의 몸통 Collider를 주입한다. Manager 누락은 초기화 시 안내 후 Context 비활성화. 자동 탐색·반복 재시도 없음.
- 지속 갱신은 생존·활성 → Collider 중심 XY 거리 → 범위 안에서만 Linecast → 양쪽 Collider 바닥 높이 판정. Component 검색·Overlap 탐색·대상 사망 구독 없음.
- 감지되면 Root 유지, 높이가 부적합하면 HasValidTarget만 false. 범위·시야·사망으로 선택 해제해도 주입 참조 유지. 몬스터 비활성화·사망의 Clear는 캐시까지 해제한다.
- OnEnable는 Start 전 호출을 구분한 뒤 구독 복구·현재 Player 재주입. OnDisable는 자기 구독·대상·이동 입력 정리. HomeX와 요청 가능 시각은 다시 초기화하지 않는다.
- Handler는 개체별 `[Serializable]` 일반 C# 객체로 Context가 소유한다. Handler 자체 Update·싱글톤·추가 인터페이스는 만들지 않는다.

## 5. 제작 순서·작성 기준

1. **재사용 결정:** 기본 행동·입력 계약이 같으면 기존 Context·Handler·Action·Graph를 재사용한다. 종별 수치·모션 차이를 위해 클래스를 복제하지 않는다.
2. **필요 기능 추가:** 정보·판정은 해당 Handler, 여러 정보의 조합·수명은 Context, 목적지·타이머·Input 실행은 Action에 둔다. 같은 코드 한 줄을 공유하려고 기반 Action을 추가하지 않는다.
3. **Graph 연결:** 큰 행동을 상위에, 세부 행동을 Subgraph에 배치한다. 커스텀 Action 필드는 Context 하나, 문구는 `[Context] 행동`처럼 짧게 작성한다. 기본 Condition·Wait·WaitRange를 우선 사용한다.
4. **연결 검증:** 작성·실행 Blackboard와 상하위 Context 연결, 조건·우선순위·Observer·Repeat를 확인한다. 기존 GUID·노드 ID·Inspector Override를 보존한다.
5. **실행 확인·문서 갱신:** 아래 수동 시나리오를 안내하고 관찰 결과만 기록한다. 최종 계획·참조 문서·주제별 로그를 실제 구현과 맞춘다.

코드는 `ProjectRE` namespace, Unity Behavior Action 노드는 기존 전역 namespace를 따른다. 함수 설명은 짧은 단답형 주석. Component 검색은 초기화·주입 시에만 수행한다. 검증은 초기화와 실제 수명 경계에 필요한 만큼만 추가한다. 도착 범위와 방향 사각 구간을 부동소수점 허용 오차와 혼동하지 않는다.

Graph·Scene·Prefab은 연결된 Unity CLI/MCP의 Editor API로 편집한다. 임시 Migration·Verification 스크립트는 만들지 않는다. 연결 불가 시 상태와 수동 작업 위치·값을 안내하고, 미저장 씬을 자동 저장·재로드하지 않는다.

## 6. 검증·적용 한계

- 컴파일·Console과 작성/실행 Graph 연결을 먼저 확인한다. Graph 검사 성공을 Play 회귀 성공으로 표현하지 않는다.
- 사용자가 같은 층에서 시야 진입·정지·이탈·재진입하도록 먼저 안내한다. AI는 활성 노드·HasValidTarget·LostTarget·IsDead·Console을 읽어 순찰, 거리 도착, 공격 선택·간격, 상실 대기·복귀 중단을 확인한다.
- 배후 도착/시간 초과, 복귀 도착/재인식 중단, 사망·비활성화의 입력 해제를 구분한다. OnEnd는 모든 종료에서 호출되며 `CompleteReturn()`은 Success에서만 실행한다.
- 미조작·미관찰 항목은 수동 확인 대기로 남긴다. 입력 주입·강제 이동·가상 타이머로 Play 결과를 만들지 않는다.

현재 전제: 등록된 씬 Player 한 명, Health·몸통 Collider 같은 GameObject, 생존 중 Component 교체 없음, 동일 층 수평 이동. 런타임 Player 교체·자동 장애물 우회·낙하 후 상층 복귀·비행·점프는 지원하지 않는다. 다른 FSM Monster가 요구하면 입력·탐색·이동 정책부터 별도 설계하고 기존 동작으로 지원된다고 가정하지 않는다.
