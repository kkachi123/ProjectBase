# Agent·Grounded·Flying 분리 및 공용 공격 기반 리팩토링 계획

## 1. 목표와 범위

Agent의 공통 FSM·물리 실행과 지상·비행 이동 정책을 분리한다. 기존 Player·Goblin 동작을 보존하면서, 이후 Flying Monster가 사용할 공용 공격 전략·돌진·투사체·범위 표시를 먼저 제작한다.

이 계획을 완료한 뒤 [Flying Monster FSM 제작 계획](Flying_Monster_FSM_Implementation_Plan.md)을 실행한다. Flying 전용 State·Input·Animator·Clip·종별 SO·Prefab은 이 단계에서 만들지 않는다.

구현 전 [프로젝트 지침](ProjectSkill/AGENTS.md), [Agent 제작 가이드](ProjectSkill/FSM_Agent_Creation_Guide.md), [State 확장 가이드](ProjectSkill/FSM_State_Extension_Guide.md)를 확인한다.

### 현재 코드에서 분리할 의존성

아래 경로는 `Assets/Scripts/FSM/` 기준이다.

| 현재 구성 | 변경 |
| --- | --- |
| `Agent/@Hub/AgentController.cs`에서 지상 이동 Handler 생성·Grounded 시작 | 이동 Handler 생성은 각 프로필로 이전. 초기 ChangeState는 구체 Controller의 Start에서 호출 |
| `Agent/Handler/AgentMovementHandler2D.cs`에서 지상 이동·공중 수평 이동·점프 처리 | `GroundedMovementHandler2D`로 이름·경로 변경. 자유 비행 함수는 추가하지 않음 |
| `Agent/SOData/AgentMotorData.cs`에 일반 이동·점프 수치 혼재 | 공통·Grounded·Flying 데이터로 분리 |
| `StateFactoryData.MovementHandler`와 `DeathState`가 지상 Handler 요구 | 프로필별 FactoryData로 분리. 공용 Death는 정지 인터페이스 사용 |
| `GroundedAgentController`가 지면 감지·점프 입력 관리 | 기존 지면 감지 구조 유지. 지상 이동 Handler 초기화 추가 |
| `GetHitEndTransition`이 Grounded 복귀를 고정 | 생성자로 복귀 Type 주입 |

## 2. 책임과 의존성

### Controller·이동 프로필

```text
AgentController                  공통 초기화·FSM·Animation Event 전달
├─ GroundedAgentController       지상 이동 Handler·기존 지면 감지
│  ├─ PlayerController           Jump·Dash·Player 공격
│  └─ MonsterController          지상 이동·지면 감지·기본 공격
└─ FlyingAgentController         비행 이동 Handler 초기화
   └─ FlyingMonsterController    후속 계획에서 제작
```

- `AgentController`는 Health·Combat·Motor 초기화와 기존 State 전환 수명을 유지한다. `_movementHandler`와 해당 생성 코드만 프로필로 옮긴다.
- 공통 Start에서는 `OnTransition += ChangeState` 연결만 유지하고 초기 ChangeState를 삭제한다. InitialStateType은 추가하지 않는다.
- PlayerController·MonsterController는 Start를 override하여 **base.Start() 이후** `ChangeState(typeof(GroundedState))`를 한 번 호출한다. 후속 FlyingMonsterController는 같은 순서로 Flight에 진입한다. 프로필 기반 Controller의 Start에는 초기 전환을 넣지 않는다.
- `GroundedAgentController`는 `GroundedMovementHandler2D`를 소유한다. `_groundDetector`·`_jumpInput`·`IsGrounded`·RequireComponent·지면 갱신 FixedUpdate는 현재 위치와 동작을 유지한다.
- `MonsterController : GroundedAgentController`로 변경한다. 지상 Handler·GroundDetector 초기화를 기반 클래스에서 재사용하고 Monster에 같은 초기화 코드를 반복하지 않는다. 기존의 빈 FixedUpdate override는 제거하여 기반 클래스의 지면 갱신을 사용한다.
- `FlyingAgentController`는 abstract 기반으로만 추가한다. `FlyingMotorData`와 `FlyingMovementHandler2D`를 연결하며, State·공격·실행 루프는 추가하지 않는다.
- 공통 Update의 State.Execute 호출을 중복하지 않는다. Handler 자체 Update·FixedUpdate는 만들지 않는다.

### 이동 데이터와 Handler

```text
AgentMotorData                   공통 knockbackForce
├─ GroundedMotorData             moveSpeed·airMoveSpeed·jumpForce
│  └─ PlayerMotorData             기존 Dash 수치
└─ FlyingMotorData               flyMoveSpeed
```

| 구성 | 계약 |
| --- | --- |
| `GroundedMovementHandler2D` | 기존 Move·AirMove·Jump 동작 보존. `airMoveSpeed`는 점프·낙하 중 X축 제어 속도 |
| `FlyingMovementHandler2D` | 입력 크기를 1로 제한하고 `flyMoveSpeed`로 XY 이동. Jump·지면 감지 없음 |
| `AgentMotor2D` | 기존 Move·Jump·StopHorizontal·Knockback 유지. 범용 `Move2D(Vector2, float)`·전체 속도 정지 `Stop()` 추가 |
| `AgentImpactHandler` | 공통 `AgentMotorData.knockbackForce` 사용 유지 |

Motor에는 비행 상태·비행 데이터·`isFlying` 분기를 넣지 않는다. Move2D의 좌우 표현은 기존 Turn을 재사용하며, 별도 FacingDirection 저장 필드는 만들지 않는다.

`Agent/Handler/IAgentMovementStop.cs`에 `void Stop()` 하나만 선언한다. GroundedMovementHandler2D는 StopHorizontal, FlyingMovementHandler2D는 Motor.Stop으로 구현한다. State는 정지할 축을 판단하지 않는다.

### Factory와 공통 State

- `StateFactoryData`에는 Animator·Motor·공통 입력·Combat·Health·EventSource·AttackStarter와 `MovementStop : IAgentMovementStop`을 둔다.
- `GroundedStateFactoryData : StateFactoryData`에 지상 MovementHandler를 둔다. Player·Monster의 FactoryData는 이를 상속한다.
- `FlyingStateFactoryData : StateFactoryData`에 비행 MovementHandler를 둔다. 구체 FlyingMonster FactoryData는 후속 계획에서 추가한다.
- 프로필의 MovementHandler 속성은 공통 MovementStop 참조를 타입 지정으로 읽고 설정한다. 동일 Handler를 두 필드에 중복 저장하지 않는다.
- `AgentStateFactory<TData>`의 `AddCommonStates → AddAgentStates → ConfigureTransitions` 순서를 유지한다. 별도 Grounded/Flying Factory 기반 클래스는 추가하지 않는다.
- `DeathState(IDeathAnimation animator, IAgentMovementStop movementStop)`로 변경한다. OnEnter에서는 사망 표현과 Stop 호출만 수행하며 Motor·CombatHandler를 직접 참조하지 않는다.
- 기존 Death의 ResetAttackType은 제거한다. 공격 중 사망 시 이전 AttackState.OnExit에서 공격·전략을 정리하는 기존 전환 수명을 사용한다. FlyingDeath는 base의 XY 정지를 재사용하고 Collider 비활성화만 추가한다.
- `GetHitEndTransition(EventSource, Type returnStateType)`로 변경하고 기존 Player·Monster 호출부에 Grounded를 명시한다. 이벤트 구독·해제와 플래그 초기화는 유지한다.
- `AttackTransition`은 그대로 재사용한다. `AttackEndTransition`의 지정 Type 복귀와 기존 Player용 지면 판정 생성자도 유지한다.
- Grounded·Jump·Fall의 Handler 타입을 갱신한다. 지상 전용 `JumpTransition`, `JumpFallTransition`, `LandTransition`, `GroundedFallTransition`은 GroundedAgent 영역으로 이동하되 클래스·판정식·기존 허용 오차를 바꾸지 않는다.
- Player/Goblin의 State Type 키·Rule 평가 순서·Animator·BT는 변경하지 않는다. 기존 Player 전용 Dash를 공용 돌진으로 재사용하거나 이번에 재설계하지 않는다.

## 3. 먼저 제작할 공용 공격 기반

공격 실행은 **전략 패턴**으로 구성한다. `AgentAttackAction2D`를 단일 abstract 전략 계약으로 사용하고, 같은 역할의 인터페이스·Resolver·전략 Factory는 추가하지 않는다.

전략은 설정 가능한 Component로 제작하되, Actor의 State가 명시적으로 호출한다. 별도 `AgentAttackHandler2D`는 만들지 않는다. 선택된 전략의 수명은 이를 사용하는 AttackState가 관리한다.

### 추가할 공격 관련 클래스: 9개

| 클래스 | 책임 |
| --- | --- |
| `AgentAttackAction2D` | 타입 등록, Begin·Execute·OnFrame·End 및 표현용 단계 조회 계약 |
| `AgentContinuousRushAttack2D` | 준비 후 돌진 구간 연속 판정. 첫 유효 타격·벽·거리 종료 후 마무리 단계 제공 |
| `AgentFrameRushAttack2D` | 첫 OnFrame에서 돌진, 두 번째 OnFrame에서 사각형 타격 한 번. 최종 End에서 정리 |
| `AgentProjectileAttack2D` | 조준선 표시, OnFrame에서 투사체 한 발 발사, 준비 정리 |
| `AgentRushHandler2D` | 고정 XY 방향 이동·벽 검사·실제 이동 거리·속도/중력 정리. 피해 판정 없음 |
| `AgentAttackRangeIndicator2D` | 전달된 좌표로 Corridor·Box·AimLine 표시와 Hide만 수행 |
| `AgentAttackHitUtility2D` | 방향 지정 사각형 계산·판정과 공통 피해·넉백 전달. 타격 성공 여부 반환 |
| `AgentProjectileDataSO` | Prefab·속도·수명·판정 반경·장애물 Layer 설정 |
| `AgentProjectile2D` | 독립 이동·충돌·수명·소멸 모션. 피해 한 번 처리 |

모든 클래스는 Grounded/Flying 어느 Agent에도 주입 가능한 계약으로 제작한다. FlyingController·Input·Animator·MotorData를 참조하지 않는다. 종별 모션과 실행 연결은 후속 계획에서 처리한다.

### 실행과 Event 계약

- Begin에서 방향·공격 데이터·Event 순번·단계를 초기화한다. Execute는 State.OnExecute에서 호출하며 OnFrame은 선택된 전략으로 전달한다. End는 정상 종료와 피격·사망 중단 모두에서 실행한다.
- 실행 전략은 FSM 전환이나 Animator 값을 직접 변경하지 않는다. 준비·실행·마무리 결과만 제공하고 State가 표현한다.
- `AgentRushHandler2D`는 Rigidbody·몸통 Collider·Motor를 초기화 때 확보한다. 지상 Agent에서도 같은 XY 돌진을 쓸 수 있도록 돌진 중 중력을 일시 중지하고 종료 시 원래 값을 복구한다.
- 이동 거리에는 `0.001` 허용 오차와 마지막 이동량 보정을 적용한다. 실제 Rigidbody 변위와 계산상 이동량을 혼동하지 않고, 이동 경로의 몸통 크기로 벽·타격을 검사한다. 벽보다 뒤의 대상에 피해를 주지 않는다.
- 지속 돌진은 최초 유효 피해 한 번에 종료한다. FrameRush는 타격 Event를 한 번 소비하며 벽에서는 이동만 정지한다.
- `AgentAnimationEventProxy`·`AnimEventType`은 변경하지 않는다. 공용 전략은 기존 OnFrame을 순서대로 해석하며 최종 FSM 종료는 기존 End Rule이 담당한다. Loop Clip의 반복 OnFrame을 사용하지 않는다.

### 피해·표시·투사체 데이터

- AgentStatData의 AttackData를 피해·offset·size 원본으로 유지한다. AgentCombatHandler에는 유효한 공격 타입이 적용된 동안 사용하는 읽기 전용 `CurrentAttackData`·`TargetLayer`만 추가한다. `_datas`·`_targetLayer`는 비공개로 유지한다.
- 기존 PerformAttack의 좌우 offset·0도 사각형·피해/넉백 동작은 보존한다. CombatHandler에 방향 지정 overload는 추가하지 않는다.
- `AgentAttackHitUtility2D.CalculateBox()`에서 고정 방향의 전방·수직 축으로 offset·회전 각도·사각형을 계산하고, FrameRush의 표시와 타격에서 같은 결과를 사용한다. 범위 판정도 이 Utility에서 처리한다.
- 공통 피해 전달은 Utility로 추출하여 기존 PerformAttack·돌진·투사체에서 사용한다. 전략의 Begin 또는 발사 시 필요한 데이터를 복사하며, 입력·콤보·전략 선택을 기존 CombatHandler에 넣지 않는다.
- 투사체 속도·수명·반경·장애물 Layer는 SO에만 설정한다. 피해·대상 Layer·소유자·방향은 발사 시 전달하며 피해 수치를 복사한다. 공유 SO를 런타임에 수정하지 않는다.
- 투사체는 경로에서 먼저 만나는 유효 대상 또는 벽에서 정지한다. 소유자를 제외하고, 충돌/수명 종료 후 추가 판정을 막은 뒤 소멸 모션 종료에서 제거한다. 이미 발사된 개체는 Actor의 중단과 독립적이다.
- 소멸 Clip은 AgentProjectile2D의 수신 함수로 종료한다. 기존 Actor용 Proxy나 TrapBullet의 풀링·Tag·자동 발사 정책은 바꾸지 않는다.
- Indicator는 FSM·공격 타입·피해·대상을 참조하지 않는다. ShowCorridor·ShowBox·ShowAimLine·Hide만 제공하며, 실행 측에서 동일 판정 좌표를 전달한다.

## 4. 파일 배치와 설정 보존

| 영역 | 결과 경로 |
| --- | --- |
| Grounded 프로필 | `Assets/Scripts/FSM/GroundedAgent/`의 `@Hub/`, `Handler/`, `SOData/`, `StateControl/` |
| Flying 기반 프로필 | `Assets/Scripts/FSM/FlyingAgent/`의 `@Hub/`, `Handler/`, `SOData/`, `StateControl/` |
| 공용 공격 전략·돌진·표시·피해 | `Assets/Scripts/FSM/Agent/Handler/Attack/` |
| 공용 정지 계약 | `Assets/Scripts/FSM/Agent/Handler/IAgentMovementStop.cs` |
| 공용 투사체 코드·데이터 클래스 | `Assets/Scripts/Projectile/Agent/` |

기존 MonoBehaviour 파일·직렬화 필드를 유지하고, 이동하는 파일은 meta를 함께 옮겨 GUID를 보존한다. `_motorData`, `_groundDetector` 등 기존 직렬화 이름은 변경하지 않는다.

`BasicMonster.prefab` Root에 GroundDetector를 추가하여 Goblin Variant에서도 사용한다. 상속 변경만으로 기존 Prefab의 필수 Component가 자동 추가됐다고 가정하지 않고 Editor에서 실제 연결을 확인한다. `groundLayers`는 사용하는 지면 Layer로 설정하고, `playerFootPos`·`rayBoxSize`는 몸통 Collider 바닥에 맞춰 종별 Variant에서 조정한다. 기존 필드 이름은 유지한다.

### MotorData 이전 순서

1. 기존 Player·Goblin 데이터 값과 관련 Prefab/Scene Override를 확인한다.
2. 기존 `AgentMotorData.cs`와 meta를 **GroundedMotorData로 이름·경로 변경하여 기존 Script GUID를 보존**한다. 공통 knockbackForce를 가진 새 AgentMotorData를 추가하고 GroundedMotorData가 상속하도록 구성한다.
3. PlayerMotorData는 GroundedMotorData 상속으로 변경한다. 같은 이름의 필드를 상위·하위에 중복 선언하지 않는다.
4. 기존 GoblinMotorData는 원래 Script GUID를 통해 GroundedMotorData로 연결한다. SO 자체 GUID·Prefab 참조를 교체하지 않는다. Editor에서 재임포트 후 실제 타입·값을 확인한다.

| 기존 SO | 보존할 현재 값 |
| --- | --- |
| PlayerMotorData | Move 5, AirMove 4, Jump 8, Knockback 3, DashSpeed 12, DashDistance 5, AirDashCount 1 |
| GoblinMotorData | Move 6, AirMove 3, Jump 8, Knockback 3 |

Unity 에셋 변경은 CLI/MCP의 Editor API로 처리하며 YAML을 직접 수정하지 않는다. 미저장 Scene은 자동 저장·재로드하지 않는다. Editor 접근이 안 되면 에셋 이전을 완료 처리하지 않고 필요한 수동 확인 위치를 안내한다.

## 5. 적용 순서

- [ ] **데이터 분리**: GUID·설정값 보존 절차에 따라 Agent/Grounded/Flying 데이터와 Player 상속을 구성한다.
- [ ] **이동·Controller 분리**: 지상 Handler 이름·경로·참조 갱신, 비행 Handler·abstract 기반 추가, Monster의 Grounded 상속과 BasicMonster/Goblin GroundDetector 연결.
- [ ] **공통 FSM 의존성 정리**: 프로필 FactoryData·정지 인터페이스 기반 Death·구체 Controller의 Start·Hit 복귀 Type·지상 Rule 경로를 적용한다.
- [ ] **공용 공격 기반 제작**: 9개 클래스, Combat 최소 확장, 공유 사각형 계산·피해 전달을 연결한다. Flying 모션·Prefab은 생성하지 않는다.
- [ ] **기존 Agent 회귀 확인**: 데이터·Prefab 참조·컴파일·Player/Goblin 수동 동작을 확인한다.
- [ ] **후속 작업 인계**: 완료된 API·파일 위치를 Agent/State 제작 가이드에 반영하고, 아래 완료 기준을 확인한다.

이 계획에는 구현된 작업이 아직 없다. 단계 완료 시 해당 체크를 갱신하고, 검증하지 못한 항목은 별도 표시한다.

## 6. 검증과 Flying 착수 조건

- Player·Goblin이 기존 초기 상태·전이 순서·이동 속도로 동작한다. Monster는 기반 클래스의 Handler 초기화를 재사용하고, 추가한 GroundDetector가 실제 지면에서 IsGrounded를 갱신한다.
- 모든 구체 Controller가 base.Start로 전환 이벤트를 먼저 연결한 뒤 최초 State에 한 번 진입한다. 초기 전환 누락·중복 구독이 없다.
- Player의 점프·낙하·Dash·콤보·피격·사망과 Goblin의 이동·공격·피격·사망이 유지된다. 기존 Dash/Land 허용 오차를 변경하지 않는다.
- 공통 Controller·FactoryData·DeathState가 지상 MovementHandler를 요구하지 않는다. Flying 이동에 GroundedMotorData나 airMoveSpeed를 사용하지 않는다.
- Death는 IDeathAnimation·IAgentMovementStop만 주입받는다. 지상 사망은 Y 속도를 유지하고 비행 사망은 XY를 정지한다. 공격 중 사망에서도 이전 State.Exit으로 타입·표시·전략이 정리된다.
- SO 타입·값·GUID·Prefab/Scene 참조·Override에 손실이나 Missing Script가 없다.
- 공용 공격 9개 클래스가 컴파일되고 Flying 전용 타입 없이 초기화·실행·중단 계약을 제공한다. **새 공격의 실제 Play 검증은 후속 Flying 연결 단계에서 수행한다.**
- Proxy·기존 Animator·BT·Managers·TrapBullet·원본 Sprite 설정에 범위 밖 변경이 없다.

공격 클래스의 예정 관계와 실행 의존성은 [공격 구조 클래스 다이어그램](Agent_Attack_Class_Diagram.pdf)을 참고한다. 신규 전략과 Flying 연결은 구현 예정이며 현재 코드의 완료 결과가 아니다.

Play 검증은 조작·기대 결과를 먼저 안내한 뒤 사용자가 조작하고 AI가 상태·Console을 확인한다. 임시 Migration·자동 Verification 코드는 작성하지 않는다.

**데이터 이전·컴파일·기존 Player/Goblin 회귀 확인까지 완료된 후에만 Flying 제작 계획으로 넘어간다.**
