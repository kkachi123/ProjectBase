# FSM State 확장 가이드

기존 Agent에 새 행동을 추가하거나 공용 State를 파생 구현할 때 사용하는 AI 작업 지침이다. 새 Agent 전체 제작은 [Agent 제작 가이드](FSM_Agent_Creation_Guide.md)를 사용한다.

**FSM이 행동과 전이를 결정하고, Animator는 parameter로 표현한다.** 하나의 FSM State가 여러 Clip을 사용할 수 있으며, Clip마다 State를 만들 필요는 없다. 문서보다 현재 소스를 우선한다.

## 1. 구현 전 확인

- 대상 Agent·Controller·Prefab, 새 행동의 진입 가능한 State와 입력·조건.
- 수행 로직, 정상 종료 조건·목적지, 피격·사망·다른 행동에 의한 중단과 우선순위.
- 입력 거절 시 폐기·예약 정책, 필요한 데이터·모션, 코드·Animator·Clip 작업 범위와 검증 환경.

결과를 바꾸는 미지정 정책은 확인하고 가능한 분석은 계속한다. 기존 State·Rule·Handler·입력·Animator 기능으로 해결할 수 있으면 재사용한다. 계획만 요청하면 구현하지 않으며, 미요청 AI 판단·입력 버퍼·범위 밖 구조 개편을 추가하지 않는다.

## 2. 필요한 소스 읽기

아래 경로는 `Assets/Scripts/FSM/` 기준이다. 대상 Controller의 초기화부터 FactoryData → Factory → State·Rule → 실제 Animator까지 추적한다.

| 확인 대상 | 경로 |
| --- | --- |
| 실행·전환·구독 수명 | `Agent/@Hub/AgentController.cs`, `Agent/StateControl/AgentStateBase.cs` |
| 공통 Factory·DI | `Agent/StateControl/AgentStateFactory.cs`(StateFactoryData 포함) |
| 대상 Factory | `Player/PlayerState/PlayerStateFactory.cs` 또는 `NPC/AIMonstor/MonsterState/` |
| 입력·Rule 계약 | `Agent/Input/IAgentInput.cs`, `Agent/StateControl/TransitionRules/ITransitionRule.cs` |
| Animator·Event 계약 | `Agent/@Hub/AgentAnimator.cs`, `IAgentAnimator.cs`(같은 폴더), `Agent/Handler/AgentAnimationEventProxy.cs` |
| 유사 행동·실제 연결 | 관련 공용/전용 State·Handler, 대상 Animator·AnimationDataSO·Prefab·Controller·Override·Clip |

이름이 공용이어도 데이터·Detector 의존성까지 공용이라고 가정하지 않는다. 과거 계획·가상 클래스·삭제된 구조를 현재 구현으로 취급하지 않는다.

## 3. 지켜야 할 FSM 계약

| 요소 | 책임·수명 |
| --- | --- |
| Input | 입력값·요청 전달. 전이·피해·행동 정책은 넣지 않음 |
| TransitionRule | 진입/종료 조건과 `NextStateType` 판단 |
| State | 행동의 진입·수행·정리. 독립 행동을 기존 State의 무관한 분기로 끼워 넣지 않음 |
| Handler·Motor | 실행·완료 결과 제공. 직접 `ChangeState()`를 호출하지 않음 |
| Controller | 현재 State 보유, 이전 Exit → 새 Enter, Execute·Animation Event 전달 |
| Factory | State 생성·교체, DI, Rule 등록 순서 결정 |

`AgentStateBase`가 관리하는 순서:

```text
Enter: 이벤트 Rule Subscribe → OnEnter
Execute: Rule을 등록 순서대로 평가 → 전이가 없을 때만 OnExecute
Exit: 이벤트 Rule Unsubscribe → OnExit
```

- **AddTransition 순서가 우선순위다.** 허용한 출발 State에만 Rule을 붙이고, 기존 순서는 요구가 있을 때만 바꾼다.
- 이벤트 Rule 구독을 Controller의 ChangeState에서 다시 관리하지 않는다. State 자체 행동 이벤트는 OnEnter·OnExit에서 대칭으로 관리한다.
- 지속적인 Detector·Handler 구독은 초기화·종료 수명에 맞춰 따로 해제한다.
- 공통 Update가 Execute를 호출한다. Handler·Update·FixedUpdate에 같은 행동의 실행 루프를 중복 추가하지 않는다.
- 의존성은 생성자·Initialize·FactoryData로 주입한다. State가 구체 Input이나 전역 Controller를 검색하게 하지 않는다.

### Type 키

공용 State의 파생 구현은 기반 타입을 Dictionary 키와 전이 목적지로 유지한다.

```csharp
states.Add(typeof(AttackState), new MonsterAttackState(data.MonsterAnimator, data.CombatHandler, data.Motor));
// AttackTransition.NextStateType == typeof(AttackState)
```

새 독립 행동은 해당 State 타입을 키로 사용한다. 모든 목적지를 등록하고, Grounded/Fall처럼 동적 목적지는 반환 가능한 타입을 모두 확인한다. `AttackState`는 abstract이므로 직접 생성하지 않는다.

## 4. 구현 순서

1. **구현 형태 선택**: 설정·실행 정책 차이는 기존 Handler·데이터 주입으로 해결 가능한지 먼저 확인한다. 진입·종료 규칙이 다르면 파생 State, 독립 수명이면 새 State를 만든다. 실제 필요가 없는 클래스·인터페이스는 추가하지 않는다.
2. **입력·Rule 연결**: 기존 입력 계약을 재사용한다. 이벤트 수신은 `IEventTransitionRule`, 시간·Detector·Handler 완료 검사만 필요하면 `ITransitionRule`을 사용한다. Subscribe는 중복을 막고 Unsubscribe는 자신의 구독·예약 플래그를 정리한다. UniRx는 자신의 IDisposable만 해제한다. 시작 실패 시 요청 소비 정책은 기존 Rule과 요구사항을 확인한다.
3. **State 구현**: OnEnter에서 Handler 시작·초기 방향/속도·Animator 값을 설정하고, OnExecute에서 해당 행동만 실행한다. OnExit는 정상 종료와 중단 모두에서 Handler·속도·중력·Bool·Trigger·예약 값을 정리한다. 파생 override는 필요한 base 동작을 보존한다.
4. **DI·Factory 연결**: 기존 대상 Controller에서 필요한 컴포넌트·Handler를 초기화한 뒤 대상 FactoryData에 전달한다. `AddCommonStates → AddAgentStates → ConfigureTransitions` 순서를 유지한다. State 추가·교체를 완료한 뒤 최종 객체에 Rule을 연결한다. 전용 Animator 접근자는 공통 Animator 참조를 사용하고 이중 참조를 만들지 않는다.
5. **Animator·에셋 연결**: 아래 계약에 따라 parameter·AnimationDataSO·Clip·Event를 함께 맞추고 실제 Prefab 참조를 확인한다.

기본 AddCommonStates는 Hit·Death를 만든다. `OnTransition → ChangeState`는 Controller의 기존 Start에서 연결하므로 Factory에서 중복 연결하지 않는다. State 하나를 추가하기 위해 Controller·Factory를 복제하지 않는다.

종료 방식은 Clip Event·시간·물리/Handler 완료 중 요구에 맞게 선택한다. 물리 값·누적 거리에는 단위와 규모에 맞는 허용 오차를 사용하고 경계값을 확인한다. Controller의 OnDestroy 재정의 시 공통 정리를 보존한다. 풀링은 별도 재사용 수명이 필요하다.

## 5. Animator·Animation Event 계약

- Animator 인터페이스는 `Agent/@Hub/IAgentAnimator.cs`에 모은다. 기존 capability를 재사용하며 별도 IPlayerAnimator 파일을 만들지 않는다.
- AgentAnimator의 공통 기능은 Hit·Death다. 전용 Animator가 필요한 parameter·API를 보유하고, Register·SetBool·SetFloat·SetInteger·Trigger·ResetTrigger는 공통 제네릭 함수를 재사용한다. State는 주입받은 capability API로 Animator 값을 변경한다.
- parameter 이름·Bool/Float/Int/Trigger 타입·기본값을 AnimationDataSO와 실제 Animator에서 일치시킨다. 상태 Bool은 Enter에서 설정하고 Exit에서 해제한다.
- Override는 Clip 교체만 한다. 새 State·parameter·분기는 기반 Controller에 추가한다. Transition 하나의 조건은 AND이며, OR는 별도 Transition으로 표현한다.
- 여러 내부 모션의 공통 출구가 필요하면 내부 State → Exit, 상위 State Machine → 외부 목적지로 구성한다. 단일 모션에 Sub State Machine을 강제하지 않는다.
- 실제 Unity Animator와 Proxy를 같은 GameObject에 연결하고 제어 대상 Animator의 중복·잘못된 참조를 확인한다. 전용 Animator 어댑터와 Unity Animator는 서로 다른 컴포넌트다.

| Clip 목적 | Proxy 함수·FSM 처리 |
| --- | --- |
| 공격 타격 | `OnAnimationOnFrame()` → 현재 AttackState 계열에서 피해 판정 |
| Attack·Hit 등 Event로 종료하는 행동 | `OnAnimationEnd()` → OnAnimationEnded → 해당 End Rule |
| Death 종료 | `OnAnimationEnd()` → OnDeathFinished |

Override Clip의 Event는 자동 승계를 가정하지 말고 직접 확인한다. Has Exit Time·Duration·Interruption 때문에 End 전에 빠져나가거나, 중단된 Clip의 늦은 Event가 현재 행동에 영향을 주는지 검증한다. Handler 완료로 종료하는 행동에는 종료용 Event를 강제하지 않는다.

## 6. 현재 구현에서 주의할 사례

- **Dash**: `DashTransition → DashState(BeginDash·ExecuteDash·EndDash) → DashEndTransition`. Handler의 거리 도달·벽 감지 완료 후 다음 Rule 평가에서 Grounded/Fall로 전환한다. Animation End로 종료하지 않는다.
- **Dash 의존성**: AgentDashHandler2D는 현재 PlayerMotorData·GroundDetector·WallDetector를 요구한다. Execute에서 사용하는 시간과 물리 갱신 위치를 확인하고 다른 Agent에 그대로 복사하지 않는다. PlayerDashState·MonsterDashState·IDashExecution은 현재 구현이 아니다.
- **요청 소비**: DashTransition은 평가 시 요청을 소비하고, AttackTransition은 시작 실패 시 요청을 유지하다 Unsubscribe에서 정리한다. 두 Rule의 예약 정책이 같다고 가정하지 않는다.
- **Player 콤보**: 같은 Attack FSM State 안에서 Animator의 ComboTrigger로 세부 모션을 진행한다. 삭제된 Combo Handler 구조를 재도입하지 않는다.

## 7. 검증과 완료 보고

- [ ] 컴파일·Console, 필수 컴포넌트·SO·DI·Prefab 참조, Type 키·모든 목적지·전이 순서 확인.
- [ ] 최초 진입·정상 종료·입력 거절·피격/사망 중단·반복 재진입과 경계값 확인.
- [ ] 구독·플래그·Trigger·속도·중력 잔류, 중복 실행, 늦은 Clip Event 확인.
- [ ] 실제 FSM/Animator 흐름·Override/Event 재생 경로, 공용 코드 변경 시 기존 Agent 회귀 확인.
- [ ] 정적·Editor·Play 결과와 미검증·사용자 수동 확인을 구분하고 임시 테스트 요소 정리.

보고는 **변경 파일·진입/수행/종료/중단 흐름·연결/사용법·검증 결과·남은 작업**만 요약한다. 컴파일 성공만으로 실제 동작을 완료 처리하지 않는다. 이 공용 가이드를 개별 작업의 완료표로 갱신하지 않는다.

사용자 변경·Scene의 dirty/Play 상태·기존 배치를 보존한다. Scene·Prefab·Animator·Clip·SO는 연결된 Unity Editor의 CLI/MCP·Editor API로 편집하고 YAML 참조를 직접 수정하지 않는다. Editor 작업이 불가능하면 가능한 소스 작업을 진행하고 미연결·미검증 범위를 보고한다. 문서·로그·삭제·Scene 저장·커밋·푸시는 요청 범위에 따른다.
