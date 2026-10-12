# Flying Monster FSM 제작 계획

## 1. 목표와 시작 조건

Flying Eye 원본 Sprite로 Flight·Attack·Hit·Death FSM과 세 공격을 연결한다. 전용 스크립트·Clip·Animator·SO·공용 비행 템플릿·종별 Prefab Variant까지 제작하며 행동트리는 이후 작업으로 분리한다.

**[Agent·Grounded·Flying 리팩토링 계획](Agent_Ground_Flying_Refactoring_Plan.md)의 완료 기준을 충족한 뒤 시작한다.** 해당 계획에서 만든 비행 이동 기반과 공용 공격 9개 클래스는 재사용하며 이 문서에서 다시 제작하지 않는다.

구현 전 [프로젝트 지침](ProjectSkill/AGENTS.md), [Agent 제작 가이드](ProjectSkill/FSM_Agent_Creation_Guide.md), [State 확장 가이드](ProjectSkill/FSM_State_Extension_Guide.md)를 확인한다. 아래 클래스는 제작 예정이며 현재 구현된 기능으로 취급하지 않는다.

## 2. 전용 스크립트와 FSM

| 제작 대상 | 책임 |
| --- | --- |
| `FlyingMonsterController : FlyingAgentController` | Animator·공격 전략·Factory 초기화, 시작 판정, Event 전달, 사망 후 제거 |
| `FlyingMonsterInput` | XY 이동값·요청 공격 타입·요청 방향 전달. 이동 실행·피해·콤보 없음 |
| `FlyingMonsterStateFactory`·`FlyingMonsterStateFactoryData` | 프로필 의존성 주입, State 추가·교체, Rule 순서 등록 |
| `FlightState` | 공중 정지·XY 이동을 하나의 State로 처리 |
| `FlyingAttackState : AttackState` | 선택된 공용 공격 전략의 Begin·Execute·OnFrame·End와 표현 연결 |
| `FlyingHitState : HitState` | 진입 시 XY 정지 후 공통 피격 표현 |
| `FlyingDeathState : DeathState` | 공통 정지 인터페이스의 XY 정지 재사용·몸통 Collider 비활성화·사망 표현 |
| `FlyingMonsterAnimator : AgentAnimator` | Flight·Attack·AttackPhase와 공통 Hit·Death 표현 |
| `FlyingMonsterAnimationDataSO : AgentAnimationDataSO` | 전용 Animator parameter 이름 정의 |

`FlyingMovementHandler2D`, `FlyingMotorData`, `FlyingAgentController`, `FlyingStateFactoryData`, 공용 공격 전략·돌진·투사체·표시 클래스는 선행 계획의 결과물이다.

### 입력·시작 계약

```csharp
void SetMovement(Vector2 movement);
void RequestAttack(int attackType, Vector2 direction);
```

- 공용 `IAgentMovementInput`, `IAgentCombatInput.OnAttackRequested`를 구현한다. 인자 없는 공용 Action 계약은 변경하지 않는다.
- 요청 타입·방향은 Input에 보관한다. Flight에서 같은 프레임에 여러 요청이 오면 마지막 요청을 사용한다.
- Controller.TryStartAttack은 생존·공격 데이터·등록 전략 존재를 확인하고 CombatHandler에 공격 타입을 적용한다. 전략 실행은 시작하지 않는다.
- State 진입 시 요청 방향을 정규화하여 복사하고 종료까지 고정한다. 0이면 Root scale의 기존 좌우 시선 방향을 사용한다.
- Attack·Hit 중 요청은 예약하지 않는다. 공격 입력 구독은 Flight의 AttackTransition에만 둔다.
- FlyingMonsterController.Start에서 base.Start()로 전환 이벤트를 연결한 뒤 `ChangeState(typeof(FlightState))`를 한 번 호출한다. InitialStateType은 사용하지 않는다. GroundDetector·Jump·Fall·Dash는 추가하지 않는다.

### Type 키와 전이 우선순위

| Dictionary 키 | 등록 객체 | Rule 평가 순서 |
| --- | --- | --- |
| `typeof(FlightState)` | FlightState | Death → Hit → AttackTransition |
| `typeof(AttackState)` | FlyingAttackState | Death → Hit → AttackEndTransition(Flight) |
| `typeof(HitState)` | FlyingHitState | Death → GetHitEndTransition(Flight) |
| `typeof(DeathState)` | FlyingDeathState | 출구 없음 |

공통 Factory에서 만든 Hit·Death를 전용 파생 객체로 교체한 뒤 Rule을 등록한다. FactoryData의 MovementHandler로 주입한 FlyingMovementHandler2D를 공통 MovementStop 계약으로도 사용한다. FlyingDeath에는 IDeathAnimation·IAgentMovementStop·몸통 Collider만 전달하며 Motor·CombatHandler를 추가 주입하지 않는다. Update.Execute·State의 Rule 구독/해제는 기존 흐름을 사용한다.

## 3. 전략 연결과 공격 동작

Controller 초기화에서 `Dictionary<int, AgentAttackAction2D>`를 한 번 구성하여 FlyingAttackState에 주입한다. 타입 1·2·3을 각각 아래 전략 Component에 연결한다. 전략 선택용 별도 Handler·Resolver·Factory는 추가하지 않는다.

```text
AttackTransition → Controller.TryStartAttack → FlyingAttackState
                                              ├─ 1: AgentContinuousRushAttack2D
                                              ├─ 2: AgentFrameRushAttack2D
                                              └─ 3: AgentProjectileAttack2D
```

- OnEnter: 공통 Attack 표현 설정, 요청 방향 복사, 선택 전략 Begin.
- OnExecute: 선택 전략 Execute와 표현 단계 갱신. 전략 종류별 실행 switch는 넣지 않는다.
- OnFrame: 선택 전략에 전달하고 변경된 표현 단계를 반영.
- OnExit: 선택 전략 End·표시/속도/단계 정리 후 base의 공격 타입·Bool 초기화.
- 피격·사망은 모든 공격을 중단한다. 준비 중 중단하면 발사·타격하지 않으며, 이미 발사된 투사체는 유지한다.

### Attack1: 지속 판정 돌진

- 준비 모션 동안 이동·피해 없음. Corridor로 고정 방향·거리·판정 반경 표시.
- 준비 종료의 첫 OnFrame에서 돌진·연속 피해 판정을 시작하고 표시를 숨긴다.
- 첫 유효 타격은 피해 한 번 적용 후 정지한다. 벽 또는 목표 거리 도달도 돌진을 종료한다.
- 전략의 마무리 단계 결과로 AttackPhase를 변경하고 마무리 Clip을 재생한다. 해당 End Event에서 Flight로 복귀한다.
- 돌진 Loop에는 OnFrame·End Event를 넣지 않는다. 거리·벽·타격 검사는 공용 전략과 RushHandler를 사용한다.

### Attack2: 이동 중 지정 시점 단발 공격

- 원본 Attack2 Clip 전체를 한 번 재생한다. 정지 상태의 근접 공격으로 변경하지 않는다.
- 첫 OnFrame에서 돌진 시작, 두 번째 OnFrame에서 현재 위치·고정 방향의 사각형 피해 판정 한 번.
- 벽에 닿으면 이동만 정지한다. 지정 타격 Event와 모션은 계속 진행하며 최종 End에서 이동·공격을 종료한다.
- Box 표시가 현재 이동 위치를 따라가도록 하고 타격 Event에서 숨긴다. 타격 성공 여부와 관계없이 해당 Event를 소비한다.

### Attack3: 직선 투사체

- 발사 전까지 고정 방향의 AimLine 표시. 길이는 속도×수명이며 벽 위치로 제한한다.
- 첫 OnFrame에서 SO 설정으로 투사체 한 발 발사. 근접 사각형 판정은 실행하지 않는다.
- 발사 후 표시를 숨기고 Actor의 End Event에서 Flight로 복귀한다.
- 투사체의 이동·피해·벽/수명 종료·소멸은 AgentProjectile2D가 독립적으로 처리한다.

### Animation Event 전달

- 기존 Proxy의 `OnAnimationOnFrame()`과 `OnAnimationEnd()`만 사용한다. RushStart 함수·새 AnimEventType은 추가하지 않는다.
- FlyingController.OnAnimationEvent는 **OnFrame만 현재 FlyingAttackState로 전달하고 반환**한다. base의 근접 PerformAttack을 동시에 호출하지 않는다.
- End는 base 흐름에 전달하여 Attack/Hit 종료 Rule 또는 OnDeathFinished를 사용한다.
- OnFrame 순번은 전략마다 Begin에서 초기화한다. Attack2의 두 번째 OnFrame은 **타격 시점**이며, 모든 공격의 두 번째 OnFrame을 FSM 종료로 해석하지 않는다.

## 4. Animation·SO·Prefab

### 원본과 Clip

원본: `Assets/@tempAssets/@Monster/Monsters_Creatures_Fantasy/Flying eye/`

기존 PPU 100·Slice·중앙 pivot·Sprite ID·Point 필터·mipmap 비활성 설정을 유지한다. Camera·렌더링 파이프라인·프로젝트 Quality 설정은 변경하지 않는다. Sprite 배열 반환 순서에 의존하지 않고 이름의 숫자로 프레임을 정렬한다. 아래 index는 0부터 시작한다.

| Clip | 원본 프레임·Loop | Event |
| --- | --- | --- |
| Flight | Flight 0–7, Loop | 없음 |
| Hit / Death | Take Hit / Death 0–3, 비반복 | 최종 End |
| Attack1 준비 | Attack 0–4, 비반복 | 준비 종료 OnFrame |
| Attack1 돌진 | Attack 5–7, Loop | 없음 |
| Attack1 마무리 | Flight 0–1, 비반복 | 최종 End |
| Attack2 | Attack2 0–7, 비반복 | index 2 OnFrame(이동), index 5 OnFrame(타격), 최종 End |
| Attack3 | Attack3 0–5, 비반복 | index 3 OnFrame(발사), 최종 End |
| 투사체 비행 / 소멸 | projectile_sprite 0–2 / 3–7, 비행만 Loop | 소멸 Clip 종료 수신 함수 |

Actor Clip Event는 Visual의 Proxy가, 투사체 소멸 Event는 해당 Animator와 같은 GameObject의 AgentProjectile2D가 수신한다.

### Animator

- Flight는 Idle·Move 공용 Loop 하나다. 같은 모션을 위한 MoveSpeed·BlendTree는 추가하지 않는다.
- `IsFlight`, `IsAttack`, 공통 `IsHit`·`IsDeath` Bool과 `AttackType`·`AttackPhase` int를 사용한다.
- AttackType은 1·2·3 모션 선택, AttackPhase는 Attack1의 준비 0·돌진 1·마무리 2 표현이다. 종료 시 0으로 초기화한다.
- Attack Sub State Machine 내부에서 모션을 선택한다. Attack1은 단계로 연결하고 Attack2·3은 각각 단일 Clip 경로를 사용한다.
- 내부 공통 출구는 IsAttack 해제 후 Exit, 상위에서 Flight·Hit·Death로 연결한다. 공격이 정상 종료되기 전에 자동 Flight로 전환하지 않는다.
- 전환 Duration은 0으로 시작한다. 피격·사망 중단과 반복 진입에서 이전 Clip의 늦은 Event를 확인한다.
- 필요한 `IFlightAnimation`·`IAttackPhaseAnimation`은 기존 `IAgentAnimator.cs`에 추가한다. AgentAnimator가 이 capability를 구현하게 하지 않고 FlyingMonsterAnimator만 구현한다. 공통 Set 함수는 재사용한다.

### 프로토타입 설정

| 설정 원본 | 값 |
| --- | --- |
| AgentStatData | 체력 10, 공격별 피해 2·3·2, Stamina 소비 0 |
| FlyingMotorData | flyMoveSpeed 3 |
| 지속 돌진 전략 | 속도 8, 거리 3, 판정 반경 0.3 |
| FrameRush 전략·AttackData | 속도 5, size (0.8, 0.8), offset (0.2, 0) |
| AgentProjectileDataSO | 속도 6, 수명 3초, 반경 0.08, 투사체 Prefab·장애물 Layer |
| Clip | 12 FPS |

투사체 Component에 SO의 수치를 중복 저장하지 않는다. Attack2 표시·타격은 AgentAttackHitUtility2D.CalculateBox의 같은 결과를 사용한다. CombatHandler의 CurrentAttackData·TargetLayer는 읽기 용도로만 사용한다. 대상 Layer는 Player, 장애물은 기존 Ground·Wall을 사용한다.

### 결과 배치와 Component

| 결과물 | 경로 |
| --- | --- |
| Flying 전용 코드 | `Assets/Scripts/FSM/NPC/FlyingMonster/`의 `@Hub/`, `Input/`, `FlyingMonsterState/`, `SOData/` |
| 비행 템플릿 | `Assets/Prefabs/Monster/BasicFlyingMonster.prefab` |
| 공용 비행 Animator·표시 Material | `Assets/Prefabs/Monster/Animations/`, `Materials/` |
| FlyingEye 종별 Clip·데이터·투사체·Override | `Assets/Prefabs/Monster/FlyingEye/`의 `Animations/`, `ScriptableObjects/`, `Projectiles/` |
| 완성 Variant | `Assets/Prefabs/Monster/FlyingEye.prefab` |

- Root: Dynamic Rigidbody2D, 중력 0, 회전 고정, 몸통 CircleCollider2D, Motor·Health·Combat·Flying Controller/Input/Animator 어댑터·공격 전략·RushHandler·Indicator.
- Visual: SpriteRenderer·실제 Unity Animator·Proxy 하나. 외형 크기는 Visual에서 조절하고 Root scale은 기존 좌우 시선 표현에 사용한다.
- Flight 진입·정지, Attack/Hit/Death 진입 때 XY 속도 정리를 확인한다. FlyingDeath는 base.OnEnter의 MovementStop.Stop으로 XY를 정지하고 몸통 Collider를 끈다. 별도 중복 정지 코드는 추가하지 않으며, 모션 종료 후 Controller가 제거한다.
- 투사체 Prefab: AgentProjectile2D·몸통 Collider·물리 이동 구성·SpriteRenderer·실제 Animator. 소유자 충돌 제외·소멸 후 추가 타격 차단을 연결한다.

에셋은 Unity CLI/MCP의 Editor API로 작성한다. 미저장 Scene·기존 사용자 변경을 보존하며, 임시 Migration 코드는 만들지 않는다.

## 5. 제작 순서

- [ ] **선행 완료 확인**: 비행 이동 프로필·공용 공격 API·데이터 이전·기존 Agent 회귀 결과 확인.
- [ ] **Flying FSM 구성**: Input·Controller·Factory/Data·Flight·전용 Attack/Hit/Death·Animator/Data 연결.
- [ ] **전략 등록과 Event 연결**: 타입별 공용 전략 DI, State 수명·방향 고정·표시/피해·OnFrame 전달 적용.
- [ ] **에셋 제작**: Clip·Event·Animator·SO·투사체·템플릿·FlyingEye Variant 연결.
- [ ] **수동 입력 검증**: 아래 절차를 안내하고 사용자 조작 중 State·Animator·Console 확인.
- [ ] **정리**: 검증 완료 후 임시 입력 스크립트·meta·부착 Component 제거. 미검증 항목 보고.

## 6. 수동 검증과 완료 기준

사용자가 요청한 임시 입력용 `TestFlyingMonsterBehavior`만 제작한다. Awake에서 Flying Input·Camera를 캐시하고 Update에서 I/J/K/L 이동, 1/2/3 공격, 마우스 월드 XY 조준을 전달한다. State·Motor·Health를 직접 조작하거나 자동 측정하지 않는다.

Game View 포커스와 테스트 개체 배치·필요 Layer를 먼저 안내한다. 피격·사망은 실제 Player 공격으로 확인한다. 테스트 완료 전에는 입력용 Component를 제거하지 않는다.

| 조작·상황 | 기대 결과 |
| --- | --- |
| 최초 실행·I/J/K/L·대각선·입력 해제 | Flight 진입, 중력 낙하 없음, 대각선 속도 증가 없음, 정지 |
| 1 입력 후 마우스 이동 | 요청 방향 고정. 준비 중 피해 없음. 돌진만 연속 판정 |
| Attack1 대상·벽·최대 거리 | 먼저 발생한 조건에서 정지, 피해 한 번, 마무리 후 Flight |
| 2 입력·이동 경로에서 타격 | 실제 돌진, 지정 Event에서만 피해 한 번, Box와 중심·크기·방향 일치 |
| Attack2 벽 충돌 | 이동만 정지하고 타격 Event·모션 종료 정상 진행 |
| 3 입력·투사체가 대상/벽에 충돌·수명 만료 | 한 발·피해 한 번, 정지 후 소멸 모션, 최종 제거 |
| 공격 준비/돌진 중 피격·사망 | XY 이동·표시·단계 정리, 준비 중 발사 취소. 발사된 투사체는 유지 |
| Attack·Hit 중 추가 요청·반복 재진입 | 예약 공격·이전 Event 순번·중력/속도·표시 잔류 없음 |
| 기존 Player·Goblin 조작 | 기존 공격·이동·점프·Dash·콤보·피격·사망 유지 |

컴파일·Console·Missing Script·SO/Prefab 참조·Type 키·Animator parameter·Event 연결을 함께 확인한다. 공용 공격의 빠른 이동 경로·마지막 거리 보정도 실제 Play에서 확인한다.

행동트리·자동 대상 탐색·유도·관통·풀링·반복 피해·기존 Managers 수정은 제외한다. 완료 보고에는 결과 Prefab, 설정 위치, 검증 결과와 남은 수동 작업을 구분하며 컴파일 성공만으로 Play 완료 처리하지 않는다.
