# Dash Logic 구현 계획

## 1. 목표

기존 `DashState`의 Animator 진입·종료 처리 위에 실제 Dash 이동을 추가한다.

- Dash 입력 시 바라보는 방향으로 **X축만** 빠르게 일정 거리 이동한다.
- Dash 중 Y 위치와 수직 물리를 고정한다.
- 전방 벽에 닿으면 남은 거리를 이동하지 않고 즉시 종료한다.
- 공중에서는 착지 전까지 Dash를 1회만 허용한다.
- 거리 도달과 벽 충돌은 Animation Event가 아니라 물리 로직이 Dash 종료를 결정한다.

```text
Dash 입력
  → DashTransition: Dash 시작 가능 여부 확인
  → DashState Enter
  → DashState OnExecute → AgentDashHandler2D: X축 이동 / 벽 감지 / 거리 누적
  → DashEndTransition: 완료 여부 확인
  → GroundedState 또는 FallState
```

## 2. 현재 코드 기준 분석

| 대상 | 현재 책임 | Dash Logic 구현 시 변경 방향 |
| --- | --- | --- |
| `DashState` | 일반 수평 속도 정지, `IsDash` Animator 설정 | Handler 시작·실행·종료 호출과 Animator 수명 관리 |
| `DashTransition` | 입력 Event만 수신해 항상 DashState로 전이 | Handler의 시작 가능 여부를 통과할 때만 전이 |
| `DashEndTransition` | Animation End Event 후 Grounded/Fall 선택 | Handler의 거리 완료·벽 충돌 완료를 polling하여 Grounded/Fall 선택 |
| `AgentMotor2D` | 일반 이동, 점프, 넉백 | Rigidbody2D의 저수준 X축 이동 API만 제공하거나 Handler가 직접 사용하도록 최소 확장 |
| `PlayerStateFactory` | DashState 및 전이 조립 | Dash Handler를 State·Transition에 주입 |
| `Hero_Dash.anim` | Animation Event로 종료 전달 | Event 종료 의존을 제거하고, 실제 Dash 지속 시간과 Clip 표현 길이만 동기화 |

현재 `DashEndTransition`이 `OnAnimationEnded`를 구독하므로, Dash의 실제 거리와 Clip 길이가 달라지면 State 종료 시점도 달라진다. 따라서 Dash 이동 구현 단계에서는 **물리 완료가 FSM 종료의 단일 기준**이 되어야 한다.

## 3. 설계 결정

### 3.1 `AgentDashHandler2D`를 물리 실행 전담 Handler로 추가

`DashState`가 Rigidbody2D, Collider2D, LayerMask, 거리 누적을 모두 직접 관리하면 공용 State의 책임이 커진다. 대신 `AgentDashHandler2D`가 Dash 세션의 물리 상태를 관리한다.

```text
AgentDashHandler2D
  ├─ PlayerMotorData: 속도, 거리, 공중 Dash 허용 횟수
  ├─ Rigidbody2D / WallDetector
  ├─ 시작 방향, 누적 거리, 완료 사유
  ├─ 시작 가능 여부와 공중 Dash 사용 여부
  ├─ DashState가 호출하는 X축 이동·WallDetector 벽 감지 계산
  └─ 종료 시 gravityScale / velocity 복구
```

`DashState`는 `OnEnter()`에서 `BeginDash()`, `OnExecute()`에서 `ExecuteDash()`, `OnExit()`에서 `EndDash()`를 호출한다. Handler는 MonoBehaviour의 `FixedUpdate()`에서 독립적으로 실행하지 않으며, `DashEndTransition`은 `IsCompleted`만 확인한다. 이렇게 하면 이후 Player/Monster별 Dash 거리, 무적 시간, 충돌 피해를 Handler 파생형 또는 실행 정책으로 확장할 수 있다.

### 3.2 X축 고정 이동과 Y축 동결

Dash 시작 시 다음을 수행한다.

1. `AgentMotor2D`의 현재 바라보는 방향(`transform.localScale.x`)을 읽어 Dash 방향으로 고정한다.
2. Rigidbody2D의 Y 속도를 0으로 만들고, Dash 중 `gravityScale`을 0으로 보관·설정한다.
3. `DashState.OnExecute()`마다 Rigidbody2D의 X 속도만 `dashSpeed`로 갱신하고, 실제 이동 거리는 시작 위치와 현재 위치의 X 차이로 누적한다. 실제 Rigidbody 이동 단위는 물리 틱이므로 마지막 속도 보정에는 `Time.fixedDeltaTime`을 사용한다.
4. 종료 시 원래 `gravityScale`을 복구한다. 공중 Dash 종료 후에는 Y 속도 0에서 다시 FallState의 중력에 의해 낙하한다.

따라서 Dash 도중에는 상승·낙하하지 않으며, 공중 Dash가 끝난 뒤에만 다시 낙하한다.

### 3.3 벽 충돌 판정

Player에 추가된 `WallDetector`를 Dash의 전방 벽 감지에 사용한다. Dash 방향 자체가 현재 바라보는 방향으로 고정되므로, 기존 `IsWallInFront()`가 검사하는 방향도 Dash 방향과 일치한다. 별도 방향 인자 API는 추가하지 않는다.

```text
이동 예정 거리 = min(DashSpeed × fixedDeltaTime, 남은 Dash 거리)

WallDetector가 전방 벽을 감지
  → Rigidbody2D X 속도를 0으로 설정
  → 완료 사유 = WallHit
  → Dash 종료

WallDetector가 벽을 감지하지 않음
  → Rigidbody2D X 속도로 이동
  → 실제 X 위치 차이로 누적 거리 갱신
  → 목표 거리 도달 시 Dash 종료
```

- `WallDetector.obstacleMask`가 Dash를 중단시키는 벽 레이어를 소유한다. Dash Data에는 LayerMask를 중복으로 두지 않는다.
- DashState는 Dash 중 일반 이동을 실행하지 않으므로, Dash 시작 후 transform scale은 바뀌지 않는다. 따라서 Handler의 고정 방향과 WallDetector의 검사 방향이 유지된다.
- 벽을 통과하지 않는 물리 충돌은 Player의 Collider2D와 Rigidbody2D가 담당한다. 빠른 Dash 속도에서 충돌 누락이 발생하지 않도록 Rigidbody2D의 Collision Detection은 `Continuous`로 확인한다.
- `viewRange`는 이동량 예측용 값이 아니라 **Player 중심에서 전방 충돌면까지의 거리 + 작은 여유값**으로 설정한다. 예를 들어 Player Collider의 월드 기준 반폭이 `0.25`이면 `0.27 ~ 0.30` 정도로 설정한다. 이 값은 Dash 중 벽에 거의 닿았을 때만 종료 신호를 낸다.
- Raycast가 Trigger를 벽으로 인식하지 않도록 `Physics2D.queriesHitTriggers` 의존 여부를 확인하고, 필요하면 Trigger Layer를 `obstacleMask`에서 제외한다.
- 현재 API는 bool만 반환하므로 벽 충돌 프레임에는 이동하지 않고 종료한다. 벽면까지의 정확한 잔여 거리 이동이 필요해질 때만 `RaycastHit2D` 반환 API와 skinWidth를 추가한다.

### 3.4 공중 Dash 1회 제한

초기 구현은 복잡한 버퍼 없이 Handler 내부 상태 하나로 처리한다.

```text
Grounded 상태에서 Dash 요청
  → 항상 허용, 공중 Dash 사용 횟수 0으로 재설정

Airborne 상태에서 Dash 요청
  → 사용 횟수 < MaxAirDashCount(초기값 1)일 때만 허용
  → Dash 시작 승인 시 사용 횟수 증가

착지 후 다음 Dash 요청
  → Grounded 판정으로 사용 횟수 0 재설정
```

`maxAirDashCount`는 `PlayerMotorData`의 정수 값으로 둔다. 현재 Player는 `1`, 공중 Dash가 없는 Player 변형은 `0`으로 설정할 수 있다. Grounded/Fall/Jump의 입력 Event 등록 구조는 그대로 유지한다.

> 후속 정리: 현재 Handler는 `BeginDash()`에서 지상 시작 시 횟수를 초기화하며, `ExecuteDash()`에서도 Dash 도중 착지한 경우를 처리한다. 공중 Dash 후 Dash 없이 착지하고 다시 점프하는 경로까지 완전히 보장하려면, 다음 단계에서 `GroundedState.OnEnter()`가 Handler의 전용 공중 Dash 횟수 초기화 함수를 호출하도록 옮긴다.

### 3.5 Dash 진입 판별과 실행 수명 분리

Dash 요청을 받았을 때 Handler가 미리 이동 상태를 만들면, 아직 DashState에 진입하지 않았는데 실행 상태가 바뀌는 문제가 생긴다. 따라서 판별과 실행을 아래처럼 분리한다.

```text
DashTransition.ShouldTransition
  → 요청 Event가 있었는지 확인
  → Handler.CanStartDash()만 순수 판별
  → true일 때만 DashState 전이

DashState.OnEnter
  → Handler.BeginDash()
  → 방향·시작 위치 저장
  → 공중 Dash 사용 횟수 갱신
  → Y 고정 및 X축 Dash 시작

DashState.OnExecute
  → Handler.ExecuteDash()
  → 벽 감지·이동 거리·완료 상태 갱신

DashState.OnExit
  → Handler.EndDash()
  → X 속도 정지, gravityScale 복구
```

- `CanStartDash()`는 Handler 내부 상태를 바꾸지 않는다. 지상에서는 항상 true, 공중에서는 남은 공중 Dash 횟수만 확인한다.
- `BeginDash()`에서만 지상일 때 공중 Dash 횟수를 0으로 초기화하고, 공중 시작일 때 사용 횟수를 증가시킨다.
- 이 분리로 두 번째 공중 Dash는 State에 진입하지 않으며, 성공한 Dash만 실제 실행 상태를 시작한다.
- `BeginDash()`는 시작 가능 여부를 다시 판정하지 않는다. Dash 진입은 `DashTransition`의 `CanStartDash()` 통과를 전제로 한다.
- `_isDashing`은 별도 Handler 실행 주체가 있을 때만 필요한 중복 실행 방지 값이다. 현재는 DashState가 Handler 호출 수명을 완전히 소유하므로 사용하지 않는다.

### 3.6 상태 전이와 Animation Event 정리

```text
DashTransition
  OnDashRequested 수신
    → AgentDashHandler2D.CanStartDash()
    → true일 때만 DashState 전이

DashState
  Enter   → Handler.BeginDash(), IsDash = true
  Execute → Handler.ExecuteDash()
  Exit  → Handler.EndDash(), IsDash = false

DashEndTransition
  Handler.IsCompleted == true
    → GroundedDetector 기준 GroundedState / FallState
```

- `DashEndTransition`은 더 이상 `IEventTransitionRule`이 아니다. 일반 `ITransitionRule`로 변경한다.
- Dash Clip의 End Animation Event는 제거한다. Dash는 거리 또는 벽 충돌로 끝나야 한다.
- Dash 중 Hit 전이는 기존처럼 `DashEndTransition`보다 앞에 등록해 우선 처리한다.
- Attack, Jump, 재-Dash는 DashState에 전이 Rule을 등록하지 않는다. Dash 동작 중에는 취소되지 않는다.

## 4. 데이터와 인터페이스 계획

### 4.1 `PlayerMotorData : AgentMotorData`

공통 이동·점프·넉백 값은 `AgentMotorData`에 유지한다. Player 전용 Dash 수치는 이를 상속한 `PlayerMotorData`에 둔다. 현재 `Assets/Prefabs/Player/ScriptableObjects/PlayerMotorData.asset`은 파일명만 PlayerMotorData이고 Script GUID가 `AgentMotorData`를 가리킨다. 따라서 다음 클래스를 먼저 만든 뒤, 해당 asset의 Script가 새 클래스를 참조하도록 Unity Editor에서 갱신해야 한다.

| 필드 | 역할 |
| --- | --- |
| `dashSpeed` | X축 이동 속도 |
| `dashDistance` | 한 번의 Dash 목표 거리 |
| `maxAirDashCount` | 착지 전 허용되는 공중 Dash 횟수. Player는 1 |

벽 LayerMask와 감지 거리는 `WallDetector`가 소유한다. 쿨다운, Stamina, 무적 시간은 이번 범위에 넣지 않는다. 필요해지면 `CanStartDash()`의 판별 조건에 추가한다.

### 4.2 방향 계약

Dash는 입력 벡터와 무관하게 **진입 시 Agent가 바라보는 방향**으로만 이동한다. `AgentDashHandler2D`는 자신의 `transform.localScale.x` 부호를 Dash 시작 방향으로 저장한다. `WallDetector.IsWallInFront()`도 같은 Transform 기준의 시선 방향을 사용하므로 추가 방향 API가 필요 없다.

AI Agent도 이동으로 transform scale 방향만 갱신하면 동일한 Dash 로직을 사용할 수 있다.

## 5. 수정 대상

| 구분 | 파일 | 작업 |
| --- | --- | --- |
| 신규 | `Assets/Scripts/FSM/Player/SOData/PlayerMotorData.cs` | `AgentMotorData` 상속, Player Dash 속도·거리·공중 횟수 정의 |
| 신규 | `Assets/Scripts/FSM/Agent/Handler/AgentDashHandler2D.cs` | 시작 가능 판별, State 호출 기반 Dash 실행, WallDetector 확인, 완료·복구 처리 |
| 확인 | `Assets/Scripts/FSM/Agent/@Detector/WallDetector.cs` | 기존 `IsWallInFront()` 재사용. obstacleMask와 Collider 반폭 기준 viewRange 확인 |
| 확인 | Player Rigidbody2D | Dynamic Body, Collision Detection `Continuous` 설정 확인 |
| 수정 | `Assets/Scripts/FSM/Agent/StateControl/States/DashState.cs` | Handler 시작·종료 호출, Animator 수명 유지 |
| 수정 | `Assets/Scripts/FSM/Agent/StateControl/TransitionRules/DashTransition.cs` | `CanStartDash()`가 true일 때만 DashState로 전이 |
| 수정 | `Assets/Scripts/FSM/Agent/StateControl/TransitionRules/DashEndTransition.cs` | Animation Event 구독 제거, Handler 완료 polling 전이로 변경 |
| 수정 | `Assets/Scripts/FSM/Player/@Hub/PlayerController.cs` | Dash Handler 초기화·FactoryData 주입 |
| 수정 | `Assets/Scripts/FSM/Player/PlayerState/PlayerStateFactory.cs` | DashState/DashTransition/DashEndTransition 생성자 주입 갱신 |
| 수정 | `Assets/Prefabs/Player/Animations/Hero_Dash.anim` | Dash 종료용 Animation Event 제거 |
| 확인 | Player Prefab / `Hero_Anim.controller` | 기존 WallDetector·신규 Dash Handler·PlayerMotorData 할당, `IsDash == false` 종료 전이 및 Clip 표현 길이 확인 |

## 6. 구현 순서

### Phase 1 — 데이터·물리 기반 준비

- [x] `PlayerMotorData : AgentMotorData` 클래스를 추가하고 `dashSpeed`, `dashDistance`, `maxAirDashCount`를 정의한다.
- [x] 기존 공용 `PlayerMotorData.asset`은 `AgentMotorData.asset`으로 이름을 정리하고, 새 `PlayerMotorData.asset`이 PlayerController에 연결되도록 Unity Editor에서 갱신한다.
- [x] `AgentDashHandler2D`를 추가한다.
  - [x] Rigidbody2D·WallDetector 참조 검증
  - [x] Dash 방향 결정
  - [x] `CanStartDash()`의 순수 공중 Dash 횟수 판별
  - [x] `BeginDash()`의 공중 Dash 횟수 초기화·증가 및 실행 시작
  - [x] `EndDash()`의 X 속도·중력 복구
  - [x] gravityScale/Y velocity 저장·복구
  - [x] WallDetector 기반 X축 이동 승인과 거리 누적
  - [x] 거리 완료 / 벽 충돌 완료 사유 제공
  - [x] `FixedUpdate()` 제거 및 `DashState.OnExecute()` 호출 기반 `ExecuteDash()`로 변경
  - [x] 거리 종료 판정에 `0.001f` 허용 오차를 적용해 마지막 물리 좌표 오차로 DashState가 고정되는 문제를 방지
  - [x] `_isDashing` 제거: DashState가 Handler 실행 수명을 단독 소유하도록 정리

완료 기준: DashState가 한 번의 Dash 세션을 시작·실행·완료·복구하도록 Handler 호출 수명을 단독으로 제어한다.

### Phase 2 — FSM 종료 기준 교체

- [x] `DashTransition`이 `Handler.CanStartDash()`가 true일 때만 DashState로 전이하도록 변경한다.
- [x] `DashState`에 Handler를 주입하고 Enter/OnExecute/Exit에서 실행 수명을 제어한다.
- [x] `DashEndTransition`을 `ITransitionRule` polling 방식으로 전환한다.
- [x] Player Factory에 Handler 의존성을 전달한다.
- [x] Hit을 Dash 종료 Rule보다 앞에 유지하고, DashState Exit에서 Rigidbody 설정을 복구하도록 구현한다. Play Mode 검증은 Phase 4에서 진행한다.

완료 기준: DashState의 종료 원인이 Animation Event가 아닌 거리 또는 벽 충돌이다.

### Phase 3 — Animator·Prefab 연결

- [x] `Hero_Dash.anim`의 Dash 종료 Animation Event를 제거한다.
- [x] Animator의 Dash 종료 전이에 `IsDash == false` 조건을 추가한다.
- [x] Dash Clip 길이(약 0.417초)와 `dashDistance / dashSpeed`(5 / 12초)를 동기화한다.
- [x] Player 오브젝트에 Dash Handler와 PlayerMotorData를 연결한다.
- [x] 기존 WallDetector의 `obstacleMask`가 벽 레이어(bit 7)를 가리키는 것을 확인한다.
- [x] WallDetector의 `viewRange`를 Player Collider 반폭 0.5 + 여유값 0.05로 설정한다.
- [x] Player Rigidbody2D의 Collision Detection을 `Continuous`로 설정한다.

완료 기준: Animator 표현은 물리 Dash 상태를 따라가며, Clip End Event가 State를 종료시키지 않는다.

### Phase 4 — 검증

- [ ] 지상 정지 상태에서 Dash: 바라보는 방향으로 목표 거리만큼 X축 이동
- [ ] 이동 입력 중 Dash: 입력 방향과 무관하게 현재 바라보는 방향으로 이동
- [ ] Dash 중 Y 위치 변화 없음
- [ ] Dash가 끝난 뒤 GroundedState 일반 이동이 즉시 복구
- [ ] 벽 앞 Dash: 벽을 통과하지 않고 충돌 지점에서 종료
- [ ] Jump/Fall 중 첫 Dash: Y 고정 Dash 후 FallState로 복귀
- [ ] 착지 전 두 번째 공중 Dash: 전이하지 않음
- [ ] 착지 후 공중 Dash 횟수 초기화: 다음 점프에서 다시 1회 가능
- [ ] `GroundedState.OnEnter()` 기준 공중 Dash 횟수 초기화로 이동해, Dash 없이 착지한 뒤 재점프해도 다음 공중 Dash 1회를 보장
- [ ] Dash 중 Hit: HitState 우선 전이 후 gravityScale과 velocity가 정상 복구
- [ ] Dash Clip End Event를 제거한 뒤 Console에 누락된 Event/Animator parameter 오류 없음
- [ ] 벽 충돌 후 반대 방향 반복 Dash: 목표 거리 직전의 부동소수점 오차로 `DashState`가 고정되지 않음

## 7. 이번 범위에서 제외하는 항목

- Stamina 비용과 쿨다운
- Dash 무적 시간, 적 충돌 피해, 투사체 회피
- Dash 중 방향 전환 또는 입력으로 취소
- 공중 Dash 횟수 2회 이상, wall dash
- Player 전용 `PlayerDashState` 확장

이 항목들은 `AgentDashHandler2D.CanStartDash()`의 판별 정책 또는 `PlayerDashState : DashState` 확장 지점에 추가한다. 현재 단계에서는 공용 Dash 실행 규칙을 안정화한다.

## 8. 수동 작업 분담

### 코드 작업

- Handler, SO, State/Transition 의존성 및 물리·완료 처리 구현
- 기존 Dash Animation Event 의존 제거
- 컴파일 및 Console 오류 확인

### Unity Editor 수동 작업

- Player Dash Data asset 생성 및 수치 입력
- Player 오브젝트의 `AgentDashHandler2D`와 Data 연결
- 기존 WallDetector의 `obstacleMask`에 벽 레이어 지정 및 `viewRange` 확인
- `Hero_Dash.anim` Event 삭제
- Animator `IsDash` 진입/종료 전이 및 Clip 시각 길이 확인
- Play Mode에서 Phase 4 항목 수동 테스트

## 9. 완료 기준

- Dash의 거리·벽 충돌·공중 1회 제한이 Animator Event와 독립적으로 동작한다.
- DashState는 Handler 호출 수명(Enter/Execute/Exit), AgentDashHandler2D는 Dash 물리 계산·완료 판정이라는 책임 경계가 유지된다.
- Dash 종료 후 Rigidbody2D의 gravityScale과 Y 속도가 누수되지 않는다.
- 공용 Dash 구조이므로 Player 이외의 Agent도 Data·입력 구현만 교체하여 재사용할 수 있다.
