# FSM Agent 제작 가이드

새 FSM Agent를 제작할 때 사용하는 AI 작업 지침이다. **현재 소스를 기준으로 기존 프로필을 재사용하고, 요구사항에 필요한 부분만 확장한다.** State 구현·전이·Animator 공통 규칙은 [State 확장 가이드](FSM_State_Extension_Guide.md)를 따른다.

## 1. 작업 입력과 범위

- 확인할 요구사항: Agent 이름·역할, 원본 에셋·모션, State·시작 State, 진입·종료·중단 조건과 우선순위.
- 확인할 실행 정책: 직접 입력·외부 명령·AI 판단, 이동·공격 방식, 사망 처리·풀링, 데이터 수치, 결과 경로·검증 Scene.
- 기존 프로필과 같으면 해당 동작을 재사용한다. 결과를 바꾸는 미지정 정책·누락 모션·임시값 허용 여부만 확인하며, 가능한 분석은 계속한다. 판단 AI·입력 버퍼·콤보를 임의로 추가하지 않는다.
- 사용자 지시·프로젝트 지침·Git 변경 상태를 먼저 확인한다. 계획만 요청하면 구현하지 않으며, 사용자 변경과 범위 밖 파일을 보존한다.

## 2. 필요한 소스부터 읽기

아래 경로는 `Assets/Scripts/FSM/` 기준이다. 이동된 파일은 클래스명으로 찾고, 문서의 시그니처·예시를 현재 소스와 대조한다.

| 확인 대상 | 경로 |
| --- | --- |
| 초기화·전환·수명 | `Agent/@Hub/AgentController.cs` |
| State·Factory·DI | `Agent/StateControl/AgentStateBase.cs`, `AgentStateFactory.cs`(같은 폴더) |
| 입력·공격 시작 | `Agent/Input/IAgentInput.cs`, `Agent/@Hub/IAgentController.cs` |
| Animator·Clip Event·피해 | `Agent/@Hub/AgentAnimator.cs`, `IAgentAnimator.cs`(같은 폴더), `Agent/Handler/AgentAnimationEventProxy.cs`, `AgentMeleeHitHandler2D.cs`(같은 폴더) |
| 기본 Monster 재사용 사례 | `NPC/AIMonstor/@Hub/`, `MonsterState/`, `Attack/`, `Handler/`, `SOData/` |
| 추가 행동이 있을 때 | `GroundedAgent/`, `Player/`의 해당 State·Factory·Handler |

에셋은 `Assets/Prefabs/Monster/BasicMonster.prefab`, `Goblin.prefab`, `Animations/BasicMonster_Anim.controller`를 확인한다. 지상 일반 Monster는 [현재 제작 가이드](FSM_Grounded_Monster_Creation_Guide.md)를 따른다. 과거 계획·가상 설계를 구현된 기능으로 취급하지 않는다.

## 3. 재사용 또는 확장 결정

| 요구사항 | 선택 |
| --- | --- |
| 기본 Monster와 행동이 같고 외형·수치만 다름 | 기존 Controller·Factory·State·Animator + 종별 데이터·Clip·Override·Prefab Variant |
| 같은 행동의 실행·진입·종료만 다름 | 기존 Handler·데이터 주입 또는 필요한 파생 State |
| 독립적인 새 행동·전이 필요 | State·Rule·Handler·Animator 기능을 필요한 만큼 추가 |
| 초기화·시작·사망·실행 정책 자체가 다름 | Controller·FactoryData·Factory 확장 검토 |

`AgentController`는 FSM 실행·Health·Animation Event를 관리하며 Input·Motor·근접 타격을 강제하지 않는다. `MonsterController : AgentController`는 지상 Motor·Handler·GroundDetector를 직접 구성하고, Player는 GroundedAgentController를 사용한다. 상속과 실행 도구 재사용은 구분한다. 비행·Boss·완전한 풀링은 별도 요구를 확인한다.

### 기본 Monster 계약

- State: PatrolWait·Patrol·Chase·LostTargetWait·Return·Attack·Hit·Death. 시작 PatrolWait.
- MonsterInput·BT 없이 대상 Handler를 갱신하고 State·Rule에서 판단·행동한다.
- 공격은 Handler 목록에 모션값·MonsterAttackSO를 장착한다. 개체별 복제본으로 실행하며 Goblin은 일반·배후 두 SO에 같은 모션값 1을 사용한다. 역할 enum·종류별 생성 분기는 추가하지 않는다.
- Hit에서 수평 정지, Death 모션 종료 후 삭제. 공격·피격 종료 후 현재 상황으로 다음 행동을 판단한다.
- GroundDetector는 FixedUpdate에서 갱신한다. 새 지상 일반 Monster는 기존 흐름·코드를 재사용한다.

| State | 전이 평가 순서 |
| --- | --- |
| 순찰·대기·추적·복귀 | Death → Hit → 대상·시간·도착·공격 조건 |
| Attack | Death → Hit → 전략 완료·실패·대상 상실 결과 |
| Hit | Death → HitEnd 후 상황 재판정 |
| Death | 출구 없음 |

점프·Dash·콤보·공중 피격 복귀는 이 기본 프로필과 다른 요구다. 같은 모션을 사용하는 다른 공격 전술과 새 Animator 모션 분기는 구분한다.

## 4. 코드 제작 순서

1. **필수 참조 확인**: StatData·MotorData·Proxy 등 공통 Awake에서 사용할 데이터를 먼저 확인한다. `RequireComponent`는 SO·Animator·직렬화 참조를 연결하지 않는다.
2. **초기화**: 공통 Awake 후 자기 Component·Animator를 초기화한다. Player는 Input·공격 Handler를, Monster는 최초 Start에서 대상·전략·HomeX를 준비한 뒤 전용 FactoryData를 구성한다. Input·AttackStarter는 필요한 Player 경로에서만 주입한다.
3. **Factory 구성**: `AddCommonStates → AddAgentStates → ConfigureTransitions`. 기본 공통 State는 Hit·Death다. State 추가·교체를 끝낸 뒤 최종 객체에 Rule을 등록한다. Type 키·전이 수명은 State 가이드를 따른다.
4. **시작·실행 연결**: State 구성을 마친 뒤 base.Start가 전환 이벤트를 연결한다. 시작 State는 각 Controller에서 선택한다. Monster는 대상 갱신 후 base.Update를 한 번 호출한다. 직접 상속한 Controller는 FixedUpdate를 구현하고 필요한 접지 갱신을 보존한다.
5. **종료 정책 연결**: 기본 `OnDeathFinished()`는 빈 함수다. 요구한 삭제·비활성화·유지 정책을 구현한다. `OnDestroy()`를 재정의하면 공통 State Exit를 먼저 수행한 뒤 공격 Handler.Dispose로 실행 SO 복제본을 제거한다. 풀링은 비활성화·재활성화 시 구독·Health·State·Animator·속도 초기화를 별도 설계한다.

### 공격 흐름

```text
Player: 입력 → AttackTransition → PlayerAttackHandler.TryStartAttack
→ PlayerAttackState → 전략 Begin/Frame/End
Monster: 대상·거리·간격 → MonsterAttackTransition → TryPrepareAttack
→ MonsterAttackState → 일반/배후 전략 → 실제 종료 결과 → 다음 행동
```

모션 선택값은 Handler의 장착 설정에 둔다. Player는 역할별 PlayerAttackSO 하나와 내부 단계별 AttackData 배열, Monster는 장착 MonsterAttackSO 목록을 사용한다. 원본 설정은 초기화 때 실행 SO로 복제하고 런타임 상태는 개체별로 보관한다. 공용 AgentMeleeHitHandler2D에는 선택된 AttackData를 전달하며 타격·Gizmo·Editor가 같은 범위를 계산한다.

## 5. 에셋·Prefab 연결

기본 Monster는 다음 배치를 따른다. `AIMonstor`는 실제 스크립트 폴더명이다. 다른 Agent는 해당 영역의 기존 분류를 따른다.

| 결과물 | 경로 |
| --- | --- |
| 공용 템플릿·Animator·이름 데이터 | `Assets/Prefabs/Monster/BasicMonster.prefab`, `Animations/`, `ScriptableObjects/`(같은 Monster 폴더) |
| 완성 Variant | `Assets/Prefabs/Monster/{AgentName}.prefab` |
| 종별 Clip·Override·Stat·Motor·작업용 Sprite | `Assets/Prefabs/Monster/{AgentName}/` 아래 `Animations/`, `ScriptableObjects/`, `Sprites/` |

- `BasicMonster.prefab`은 Stat·Motor 데이터가 비어 있는 템플릿이다. `Goblin.prefab`을 연결 사례로 삼고 Variant에 종별 참조를 채운다.
- Root에는 Controller·Animator 어댑터·Motor·Rigidbody2D·Collider2D·Health·근접 타격·GroundDetector·PlayerDetector를 둔다. Monster에는 Input·BT를 추가하지 않는다. Visual에는 SpriteRenderer·실제 Unity Animator·Proxy를 둔다.
- 같은 행동이면 Override로 Idle·Move·Attack·Hit·Death Clip을 교체한다. 새 State·parameter·분기는 기반 Animator Controller를 확장해야 한다.
- Collider의 피해 수신 컴포넌트와 CombatHandler의 `TryGetComponent` 판정, 대상 LayerMask·공격 범위·방향을 확인한다. PPU·pivot·Visual scale·Collider를 함께 조정한다. 현재 Motor는 Root scale을 x=±1, y=z=1로 설정하므로 외형 크기는 Visual에서 조절한다.
- 원본 에셋·기존 GUID를 보존한다. Scene·Prefab·Animator·Clip·SO 편집은 연결된 Unity Editor의 CLI/MCP·Editor API로 수행하고 YAML 참조를 직접 수정하지 않는다. 사용자 Scene의 dirty·Play 상태와 기존 배치를 보존한다.

### Clip Event 계약

실제 Unity Animator와 Proxy는 같은 GameObject에 연결한다. Override Clip의 Event는 직접 확인한다.

| 시점 | Proxy 수신 함수 | 처리 |
| --- | --- | --- |
| 공격 유효 타격 | `OnAnimationOnFrame(intParameter)` | 현재 AttackState 계열에서 전략 타격. Monster 준비 중은 무시 |
| Attack·Hit 종료 | `OnAnimationEnd()` | Monster 공격은 전략 완료·간격 기록, Hit·Player 공격은 기존 종료 Event/Rule |
| Death 종료 | `OnAnimationEnd()` | `OnDeathFinished()` |

## 6. 완료 확인과 보고

- [ ] 컴파일·Console, Missing Script·null 참조, 실제 Animator·Proxy·Override·SO 연결 확인.
- [ ] 최초 진입, 좌·우·정지, 공격 전 피해 없음·타격 시 피해, 정상 종료·피격·사망·반복 실행 확인.
- [ ] 공격 중 추가 요청·늦은 Clip Event·복귀 후 이동값·구독/Trigger/속도 잔류 확인.
- [ ] 공용 코드 변경 시 기존 Agent 회귀 확인. 임시 테스트 요소 정리.
- [ ] 정적·Editor·Play 결과와 미검증·사용자 수동 확인(모션·타이밍·크기·밸런스)을 구분.

완료 보고는 **Prefab 위치·재사용/변경 파일·명령 API/데이터 조정 위치·검증 결과·남은 작업**만 요약한다. 컴파일 성공을 동작 검증으로 표현하지 않는다. Editor 연결이 안 되면 가능한 코드 작업을 진행하고 미연결 에셋을 명시한다.

이 공용 가이드를 개별 작업의 완료표로 갱신하지 않는다. 문서·로그·삭제·Scene 저장·커밋·푸시는 요청 범위에 따르며, C#은 기존 `ProjectRE` namespace와 간결한 주석·필요한 `<summary>`를 따른다.
