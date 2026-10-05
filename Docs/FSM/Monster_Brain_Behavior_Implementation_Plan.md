# MonsterBrain Behavior 구현 계획

## 1. 범위

Goblin에 독립 판단 Component를 추가해 대기·추적·단발 공격·추적 제한·초기 위치 복귀를 구현한다. 기존 Grounded·Attack·Hit·Death FSM은 유지한다.

- 대상: `Assets/Prefabs/Monster/Goblin.prefab`
- 제작 기준: `Docs/FSM/ProjectSkill/FSM_Agent_Creation_Guide.md`
- 현재 단계: 구현·컴파일·Goblin Play 및 입력 판단 경계 검증 완료. 실제 스테이지 밸런스와 AIPlayer BehaviorGraph Play는 미검증.
- 제외: BehaviorTree 실제 제작, 순찰·도주·대상 기억, 다중 공격·콤보, 새 State·Transition, Jump·Fall·경로 탐색, 풀링.
- 기존 Controller·Input·Motor·Factory·State·Transition은 수정하지 않는다.
- 기존 Scene·아트·Clip·Animator·전투 SO·BasicMonster·AIPlayer 전용 코드와 사용자 변경을 보존한다. 로그·PDF·커밋은 별도 요청 시 진행한다.

## 2. 구조와 책임

```text
PlayerDetector + Health
  → MonsterBrain / 향후 BehaviorTree
  → MonsterInput.SetMovement / RequestAttack
  → 기존 GroundedState / AttackTransition
  → Handler·Motor·Animator
```

| 요소 | 책임 |
| --- | --- |
| PlayerDetector | 생존·가시 대상 탐색 |
| MonsterBrain | 거리·높이·요청 간격·복귀 판단, Input에만 명령 전달 |
| MonsterInput | 기존 이동 값 보관·공격 요청 발행 |
| MonsterController | 기존 초기화·Factory·FSM·Animation Event 처리 유지 |
| State·Handler·Motor | 기존 입력에 따른 행동 실행·방향 변경 |

- Brain과 Controller는 서로 참조하지 않는다. Controller가 Brain 초기화·Update 호출·공격 결과 통보를 담당하지 않는다.
- Brain에는 Controller·Motor·Animator·CombatHandler를 주입하지 않는다. Transform은 위치·기존 방향 조회만 하고 변경하지 않는다.
- State 조회·공격 시작 시각 공개·별도 방향 입력·새 인터페이스는 추가하지 않는다.
- 대기·추적·복귀는 SetMovement로, 공격은 RequestAttack으로 전달한다.
- 좌우 이동의 방향 변경은 기존 `AgentMotor2D.Move → private Turn`에 맡긴다. Turn 접근자와 실행 흐름은 유지한다.

## 3. MonsterBrain Component

신규 경로: `Assets/Scripts/FSM/NPC/AIMonstor/@Behavior/MonsterBrain.cs`

```csharp
private void Initialize(
    MonsterInput input,
    PlayerDetector detector,
    Health health);
```

- 일반 MonoBehaviour, namespace ProjectRE.
- 자체 Awake에서 같은 Root의 Input·Detector·Health를 얻어 Initialize에 전달한다. 별도 Bootstrap이나 Controller 연결은 만들지 않는다.
- Awake에서는 참조·설정만 준비하고, 판단은 기존 Health 초기화와 FSM Start 이후 Update에서 시작한다.
- Brain Update가 입력을 전달하고 Controller Update가 기존 FSM을 실행한다. DefaultExecutionOrder를 지정하고 기존 Script Execution Order와 대조한다.
- 최초 Start에서 초기 배치 x좌표를 저장한다. 비활성화·재활성화로 복귀 위치를 변경하지 않는다.
- Initialize에서 필수 참조와 설정을 검증한다. 잘못된 설정은 오류로 전달하고 정상 준비되지 않은 Brain은 판단하지 않는다.
- FSM Execute·FixedUpdate·Coroutine·공개 Tick을 추가하지 않는다.

| 보관 값 | 용도 |
| --- | --- |
| Input·Detector·Health | 명령 전달·대상 감지·자신의 생존 여부 조회 |
| attackDistance | 추적을 멈추고 공격을 요청하는 x축 거리 |
| maxAttackHeightDifference | 추적·공격 가능한 대상의 높이 차이 |
| attackRequestInterval | 공격 요청을 발행하는 최소 간격 |
| maxChaseDistance | 초기 위치 기준 x축 추적 제한 거리 |
| returnArrivalDistance | 초기 위치 복귀 완료 거리 |
| _homeX | 최초 배치 x좌표 |
| _isReturning | 복귀 완료 전 재추적을 막는 판단 플래그 |
| _nextAttackRequestTime | 다음 공격 요청 가능 시각 |

설정은 private SerializeField, 의존성·런타임 값은 private으로 둔다. 공격·피격 상태 복제, 공격 입력 버퍼, Controller 조회용 값을 만들지 않는다.

## 4. 행동 판단

| 순서 | 조건 | 전달할 입력 |
| --- | --- | --- |
| 1 | 자신이 사망함 | 이동 0, 공격 요청 없음 |
| 2 | 복귀 진행 중 | 초기 위치 방향 이동, 도착하면 이동 0 |
| 3 | Monster가 초기 위치의 추적 제한을 벗어남 | 복귀 시작 |
| 4 | 생존·가시 대상 없음 | 초기 위치에서 벗어났으면 복귀, 이미 도착했으면 이동 0 |
| 5 | 대상이 추적 제한 밖이거나 높이 조건 미충족 | 복귀 또는 초기 위치 대기 |
| 6 | 대상이 공격 거리 밖 | 대상 방향으로 x축 이동 |
| 7 | 공격 거리 안이지만 기존 바라보는 방향과 반대 | 대상 방향 이동만 전달, 해당 판단에서는 공격 요청 없음 |
| 8 | 공격 거리 안이며 방향 일치 | 이동 0, 요청 간격 완료 시 RequestAttack 1회 |

### 방향

- 추적·복귀 이동은 기존 Move에서 자동으로 방향을 바꾼다.
- 가까운 뒤쪽 대상은 좌우 이동 입력으로 먼저 방향을 맞춘 뒤 다음 판단에서 공격한다. 제자리 회전용 API는 추가하지 않는다.
- Root의 기존 localScale.x 부호를 조회해 방향을 확인한다. 같은 x좌표의 허용 오차 안에서는 기존 방향을 유지한다.
- 방향 정렬에는 정상 이동이 수반될 수 있다. 이동 없이 즉시 회전한다는 검증 조건은 두지 않는다.

### 공격 요청·실제 실행

- 기존 1번 AttackType·AttackTransition·AttackEndTransition·Animation Event를 유지한다.
- 요청 발행 후 `_nextAttackRequestTime = Time.time + attackRequestInterval`로 갱신한다.
- 이 간격은 **실제 공격 시작 간격이 아니라 Brain의 입력 발행 간격**이다. FSM 상태·공격 시작 여부를 조회하거나 추정하지 않는다.
- Attack·Hit 중에도 Brain은 환경에 따른 입력값을 갱신할 수 있다. 실제 이동·회전·전이는 현재 State와 기존 Rule이 처리한다.
- 현재 Attack·Hit에는 공격 요청 Rule이 구독되어 있지 않으므로 해당 시점의 새 요청을 Input이 예약하지 않는다. 다음 발행 시 조건을 다시 판단한다.
- 피격·사망은 기존 FSM 우선순위에 맡긴다. Brain이 모션을 취소하거나 Motor.StopHorizontal을 호출하지 않는다.
- 현재 GroundedState.OnEnter는 보관된 이동 입력을 바로 적용한다. 모션 종료 후에는 최신 판단 결과가 이어지도록 유지한다.
- 기존 AttackTransition의 시작 거절 플래그 유지 정책은 변경하지 않는다. 필수 공격 데이터는 Prefab 검증으로 확인하고, 런타임 공격 데이터 교체·차단의 재시도 정책은 제외한다.

### 추적 제한·복귀

- 제한 기준은 Monster와 대상 각각의 `abs(x - _homeX)`이다. 감지 반경과 추적 제한 거리를 구분한다.
- 제한 초과·대상 상실·높이 조건 미충족 시, 초기 위치에서 returnArrivalDistance보다 멀면 _isReturning을 설정한다.
- 복귀 중에는 대상 재감지·공격 요청을 중지하고 먼저 초기 위치에 도착한다.
- 복귀는 _homeX 방향의 이동 입력만 전달한다. 위치 강제 이동·y좌표 보정은 하지 않는다.
- 도착 범위 안이면 이동 0, _isReturning 해제. 다음 판단부터 대상 탐색을 재개한다.
- 피격 중에도 복귀 의도와 입력은 유지할 수 있으나 실제 움직임은 FSM에 맡긴다. 넉백을 덮어쓰지 않는다.
- 공격 거리·높이·복귀 도착에는 단위에 맞는 허용 오차를 적용한다. 복귀 범위는 이동 속도·물리 이동 폭을 고려해 왕복 떨림이 없는지 검증한다.
- maxChaseDistance는 returnArrivalDistance보다 크게 설정한다. 거리·요청 간격은 기존 공격 box·Collider·검증 구간을 보고 조정한다.

현재 Monster에는 지면 감지·Fall·경로 탐색이 없다. 같은 높이의 평평하고 왕복 가능한 구간을 대상으로 하며, 벽 우회·낭떠러지 회피·다른 플랫폼 복귀는 제외한다.

### 비활성화

- Brain.OnDisable에서 자신의 이동 명령을 0으로 정리한다. State·Animator·물리는 직접 변경하지 않는다.
- Brain을 꺼도 Controller와 진행 중 FSM 행동은 유지한다.
- 재활성화 시 초기 위치·복귀 의도·다음 요청 시각은 유지한다. Time.time 기준으로 요청 간격을 다시 확인한다.

## 5. PlayerDetector 보완

기존 IsTargetInView·Target·SerializeField 이름과 Target의 Collider Transform 계약을 유지한다.

- 부모 Health를 확인해 활성·생존 후보 중 가장 가까운 가시 Player를 선택한다. 후보가 없으면 Target은 null이다.
- Linecast는 자기·대상 Collider 중심을 사용한다. 자기 Collider가 없으면 기존 Transform 위치를 사용한다.
- Brain의 대상 거리·높이 계산은 부모 Health가 있는 Agent Root를 기준으로 통일한다.
- Gizmo에서는 감지 함수를 호출하지 않는다. 반경·마지막 감지 결과만 표시하고 unused dirToTarget은 삭제한다.
- Detector 자체 Update·NonAlloc 최적화는 추가하지 않는다. Brain의 대상 판단 시 한 번 호출한다.
- AIPlayer와 공유하므로 API·직렬화 참조·기존 대상 사용을 회귀 확인한다.

## 6. 변경 파일

경로는 프로젝트 루트 기준이다.

| 구분 | 경로 | 작업 |
| --- | --- | --- |
| 신규 | Assets/Scripts/FSM/NPC/AIMonstor/@Behavior/MonsterBrain.cs | 독립 판단 Component·추적·공격 요청·복귀 |
| 수정 | Assets/Scripts/FSM/@Detector/PlayerDetector.cs | 생존 후보 선택·Linecast·Gizmo 정리 |
| 수정 | Assets/Prefabs/Monster/Goblin.prefab | Root에 Brain·Detector, Mask·설정 연결 |

Controller·Input·Motor·Factory·FactoryData·기존 State·Transition에는 코드를 추가하지 않는다. Unity 에셋은 Editor API로 수정하고 Goblin Variant에만 저장한다. Visual의 실제 Animator·Proxy와 BasicMonster 상속을 유지한다.

## 7. 구현 순서

### 1 — 현재 코드·설정 확인

- [x] Git 사용자 변경·프로젝트 지침·현재 시그니처 확인.
- [x] Goblin 필수 데이터·1번 공격·Animator·Proxy·Layer 연결 확인.
- [x] 이동 입력 → GroundedState → Handler → Move·Turn 경로 확인.
- [x] 기존 공격 Rule의 구독·해제와 Attack·Hit 중 입력 처리 확인.
- [x] 검증용 거리·요청 간격·복귀 설정과 왕복 가능한 구간 기록.

### 2 — Detector

- [x] PlayerDetector의 생존 후보 선택·Linecast·Target 정리 구현.
- [x] Gizmo의 대상 갱신 부작용 제거.
- [x] AIPlayer 공유 호출부·직렬화 참조 회귀 확인. BehaviorGraph 실제 실행은 제외.

### 3 — 독립 MonsterBrain

- [x] 자체 Awake·Initialize·Start·Update·FSM보다 앞선 입력 실행 순서 구성.
- [x] Input·Detector·Health만 참조, Controller·Motor 상호 참조 미추가.
- [x] 대기·추적·기존 이동을 통한 방향 정렬·공격 요청 간격 구현.
- [x] 추적 제한·대상 상실 복귀·도착·재감지 구현.
- [x] 자신의 사망·OnDisable 이동 명령 정리 구현.
- [x] 직접 상태·물리·Animator 변경 및 공격 입력 버퍼 미추가 확인.

### 4 — Goblin 연결·검증

- [x] Editor API로 Goblin에 Brain·Detector·Mask·설정 연결.
- [x] Controller·Input·Motor·FSM 코드가 기존 상태인지 확인.
- [x] 컴파일·Console·Prefab 참조·Goblin Play 및 입력 판단 경계 검증. 결과는 10절에 기록.
- [x] 공유 Detector의 AIPlayer API·Prefab 참조 회귀 확인. BehaviorGraph Play는 미검증.
- [x] 임시 테스트 요소 정리, 사용자 Scene·무관한 변경 보존.
- [x] 실제 완료 항목만 체크하고 미검증·수동 조정 항목 기록.

## 8. 검증 항목

| 상황 | 기대 결과 |
| --- | --- |
| 최초 생성·대상 없음 | 기존 Grounded 대기, 초기 배치 위치 보관 |
| 좌·우 대상 추적 | 기존 이동 입력으로 이동·방향·MoveSpeed 변경 |
| 가까운 뒤쪽 대상 | 정상 이동으로 방향 정렬 후 공격 요청 |
| 공격 거리·요청 간격 충족 | 요청 발행, 실제 Attack 진입은 기존 Rule로 처리 |
| Attack·Hit 중 이동값·공격 요청 갱신 | 현재 State 수명 유지, 요청 버퍼·직접 물리 변경 없음 |
| OnFrame·End Event | 기존 피해·Grounded 복귀 유지 |
| 공격 요청과 피격·사망 동시 발생 | 기존 Death → Hit → Attack 우선순위 유지 |
| 대상 상실·사망·높이 차이·추적 제한 초과 | 복귀 입력 전달, 실제 움직임은 FSM에 따름 |
| 복귀 도중 Player 재등장 | 복귀 완료 전 재추적·공격 요청 없음 |
| 복귀 도중 피격 | 넉백 보존, Grounded 복귀 후 최신 이동 입력 적용 |
| 초기 위치 도착 | 허용 범위에서 정지, 다음 판단에 대상 탐색 재개 |
| Brain 비활성화·재활성화 | 이동 입력 정리, 초기 위치·요청 시각 유지 |
| Brain 없는 Monster | 기존 외부 이동·공격 요청 정상 |
| 실행 순서 | 참조·Health·FSM 준비 후 판단, 입력 전달 후 기존 FSM 실행 |
| Gizmo·공유 Detector | 런타임 대상 변경 없음, AIPlayer 회귀 없음 |

거리·방향·복귀 도착 경계도 테스트한다. 실제 Clip 재생의 타격·종료 Event를 검증하며, 수동 Event 호출·미실행 Play 검증을 성공으로 기록하지 않는다.

사용자 수동 확인: 방향 정렬의 이동량·공격 정지 거리·요청 간격·추적 제한 거리·복귀 도착 범위·경계 떨림.

## 9. 향후 BehaviorTree 전환

- Detector·Health·기존 MonsterInput·FSM을 재사용한다.
- 대기·추적·공격 요청·복귀 판단을 조건·행동 노드로 옮긴다.
- C# Brain을 비활성화한 뒤 BehaviorTree만 같은 Input에 명령을 전달한다.
- 판단 방식 교체를 위해 Controller·Motor·FSM에 전용 참조나 분기를 추가하지 않는다.

## 10. 구현·검증 결과

### 적용 내용

- MonsterBrain: Input·Detector·Health만 참조하는 독립 Component. 자체 Awake에서 의존성을 준비하고 Start에서 최초 배치 x좌표 저장.
- DefaultExecutionOrder(-100) 지정. 기존 MonsterController의 등록 순서 0과 대조. Controller 호출·별도 Tick·상태 조회 미추가.
- PlayerDetector: 부모 Health 기반 생존·활성·가시 후보 선택, 가장 가까운 Collider를 Target으로 유지. 대상 없음·사망·차폐 시 null 정리.
- Goblin Variant Root에 Brain·Detector 추가. BasicMonster, 기존 Visual Animator·Proxy·FSM·입력·전투 데이터 유지.
- 거리·높이·추적 제한·복귀 판정에 0.01 허용 오차 적용. 공격 요청 간격은 Time.time 기준이며 실제 공격 시작 간격과 구분.

### Goblin 검증 설정

| 설정 | 값 |
| --- | --- |
| 공격 거리 / 허용 높이 차이 | 1.8 / 0.75 |
| 공격 요청 간격 | 1초 |
| 초기 위치 기준 추적 제한 / 복귀 도착 거리 | 8 / 0.15 |
| 감지 반경 | 6 |
| 대상 / 장애물 Mask | Player(256) / Ground·Wall(192) |
| 기존 이동 속도 / Fixed Timestep | 6 / 0.02초 |
| 평지 1회 물리 이동 폭 / 복귀 판정 범위 | 약 0.12 / 0.16(도착 거리 + 허용 오차) |

이동 속도 6은 기존 사용자 변경을 유지한 값이며 Motor SO를 수정하지 않았다. 검증은 초기 위치 x=0, y=0, 지면 상단 y=0의 임시 평지에서 수행했다. 실제 스테이지의 공격 정지 거리·충돌·복귀 구간은 별도 조정 대상이다.

### 자동 검증

Unity Editor에서 임시 Play 환경과 메모리 컴파일 검증 코드를 사용했다. 총 38개 체크 통과: 실제 Play·Physics 검사 27개, 입력 판단 경계 검사 11개. 경계 검사는 private Update를 직접 호출했으므로 실제 FSM 재생 검사와 구분한다.

| 분류 | 확인 결과 |
| --- | --- |
| Detector | 부모 Health·자식 Collider Target 계약 유지. 가까운 생존 대상 선택, 사망·비활성·벽 차폐 시 Target 정리 |
| 생성·이동 | Health·최초 위치 초기화, 대상 없는 Grounded 대기, 좌우 추적과 기존 Motor 방향 변경 |
| 가까운 뒤쪽 대상 | 이동 입력으로 방향 정렬 후 공격 요청. 관측한 3회 요청 모두 올바른 방향 |
| 실제 Attack Clip | 수동 Animation Event 호출 없이 피해 2회(대상 HP 200 → 196), End 2회, Attack → Grounded 복귀 |
| 요청 간격 | 3회 발행 간 최소 약 1.001초. 실제 공격 시작 시각이 아닌 입력 발행 시각 측정 |
| 모션 중 입력 | Attack 중 이동 입력을 바꿔도 수평 이동 정지 유지. 종료 후 최신 이동 입력 적용. Attack 중 요청은 다음 공격으로 예약되지 않음 |
| 복귀·재감지 | 높이 차이·대상 사망·추적 제한 시 복귀. 복귀 중 재등장한 대상은 도착 전 재추적하지 않음. 관측 도착 x≈-0.1193 |
| 피격·사망 | 복귀 중 Hit 진입·실제 Clip 종료 후 복귀 지속. Hit 중 외부 수평 속도를 Brain이 덮어쓰지 않음. Hit·Death 우선순위와 실제 Death Clip 종료 후 파괴 유지 |
| 비활성화·외부 입력 | Brain 비활성화 시 이동 0. 재활성화 시 초기 위치·복귀 의도·다음 요청 시각 유지. Brain 제거 후 기존 외부 이동·공격·종료 경로 정상 |
| 허용 오차 | 공격 거리·높이·복귀 도착·대상 추적 제한의 경계 양쪽과 자신의 제한 초과·사망 판단 통과 |
| Prefab·공유 API | Goblin Variant·Animator 1개·Proxy 참조·Missing Script 0 확인. Player AI Prefab의 기존 Detector Mask·반경(4)·참조 유지, 호출부 컴파일 확인 |

컴파일 실패 및 이번 검증에서 추가된 오류 로그 없음. Console 기록에 남아 있는 기존 InGameUI.Awake의 NullReferenceException은 작업 전 오류이며 이번 범위에서 수정하지 않았다.

임시 검증 코드·오브젝트는 정리했다. 기존 InGame 씬을 Edit 모드로 복원했고 isDirty=false를 확인했다. Scene·BasicMonster·기존 Controller·Input·Motor·FSM·AIPlayer 전용 코드에는 변경을 저장하지 않았다.

### 남은 수동 확인

- [ ] 실제 InGame의 같은 높이·왕복 가능한 구간에 Goblin 배치 후 방향 정렬의 이동량·공격 정지 거리 확인.
- [ ] 요청 간격·추적 제한·복귀 도착 범위 조정, 실제 프레임 레이트·충돌 조건에서 경계 떨림 확인.
- [ ] AIPlayer BehaviorGraph 전체 Play 회귀 확인. 이번 작업에서는 공유 Detector 물리 검사·API·Prefab 참조만 검증.

벽 우회·낭떠러지 회피·다른 플랫폼 복귀·BehaviorTree 제작은 기존 제외 범위를 유지한다.
