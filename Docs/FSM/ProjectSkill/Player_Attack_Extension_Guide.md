# Player 공격 추가 가이드

현재 Player 공격에 새로운 타격 데이터·콤보 단계·공격 전략을 추가할 때 사용하는 AI 작업 지침이다. 공용 FSM 수명과 근접 타격은 재사용하고, Player의 선택·비용·콤보 정책은 Player 영역에 유지한다.

## 1. 작업 입력과 소스 확인

- 먼저 `Docs/FSM/ProjectSkill/AGENTS.md`, `Plan_Writing_Guide.md`와 Git 변경사항을 확인한다. 사용자 수정·삭제·미저장 Scene을 보존한다.
- 요구사항: 공격 역할, 사용 입력·선택 조건, 모션, 타격 횟수·시점, 피해·범위·Stamina 비용, 정상 종료·피격 중단, 공중 이동 유지 여부.
- 기존 공격의 설정 변경인지, 지상 콤보 단계 추가인지, 새로운 실행 전략인지 먼저 구분한다. 새 State·입력·공용 인터페이스를 자동으로 추가하지 않는다.

아래 경로는 `Assets/Scripts/FSM/` 기준이다. 문서보다 현재 소스를 우선한다.

| 확인 대상 | 파일 |
| --- | --- |
| 설정·전략 생성·선택 | `Player/Attack/PlayerAttackHandler.cs` |
| 실행 계약·기존 전략 | `Player/Attack/IPlayerAttackStrategy.cs`, `PlayerGroundComboAttack.cs`, `PlayerAirAttack.cs` |
| 공격 SO | `Player/SOData/PlayerAttackDataSO.cs` |
| 초기화·State 연결 | `Player/@Hub/PlayerController.cs`, `Player/PlayerState/PlayerStateFactory.cs`, `States/PlayerAttackState.cs` |
| 공용 State·전이 수명 | `Agent/StateControl/AgentStateBase.cs`, `States/AttackState.cs`, `TransitionRules/Attack/AttackTransition.cs`, `AttackEndTransition.cs` |
| Event·타격·Animator 계약 | `Agent/Handler/AgentAnimationEventProxy.cs`, `AgentMeleeHitHandler2D.cs`, `Agent/@Hub/IAgentAnimator.cs` |
| 범위 편집 | `Assets/Editor/AgentCombat/AgentCombatEditor.cs`, `AgentMeleeHitHandler2DEditor.cs` |

## 2. 책임과 실행 계약

| 구성 | 책임 |
| --- | --- |
| PlayerController | Component 확보·의존성 주입·Factory 구성 |
| PlayerAttackHandler | Inspector 역할/SO 목록, 개체별 전략 생성, 선택·시작 가능 판정, 모션 타입 적용·비용 소비 |
| PlayerAttackState | 공통 Attack Bool과 Handler의 진입·타격·종료 연결 |
| IPlayerAttackStrategy 구현체 | 공격별 실행·타격·입력 구독·Trigger·Preview 정리 |
| AgentMeleeHitHandler2D | 전달받은 AttackData로 수평 사각형 피해·넉백 적용, 공용 범위 계산·Gizmo 표시 |
| Animator | 모션 선택, 콤보 입력 수락·순서 및 종료 모션 분기 |

전략은 일반 C# 객체로 개체마다 생성한다. `IPlayerAttackStrategy`는 `StartData`, `Begin()`, `OnAttackFrame(int attackStep)`, `End()`만 사용한다.

```text
PlayerInput.OnAttackRequested
→ 현재 State의 AttackTransition
→ PlayerAttackHandler.TryStartAttack(): 전략 선택·시작 비용 확인
→ PlayerAttackState.OnEnter
→ Handler.BeginAttack(): 모션 타입 적용·전략 Begin·비용 1회 소비
→ base.OnEnter(): Attack Bool 활성화

Clip OnFrame → Proxy → Controller → 현재 AttackState → Handler → 전략 타격
Clip End → OnAnimationEnded → AttackEndTransition → State.Exit → 전략 End
```

- 시작 판정에서는 실행·입력 구독·Stamina 소비를 하지 않는다. 성공한 선택을 State 진입부터 Exit까지 유지한다.
- 착지하더라도 공격 중 전략을 바꾸지 않는다. 비용은 시작 SO 기준으로 State 진입당 한 번만 소비한다.
- AttackState는 공통 Bool 활성화·해제 및 종료 시 모션 타입 0 초기화를 담당한다. PlayerAttackHandler의 Animator 필드는 모션 타입 적용에 사용한다.
- 전략은 State를 직접 전환하지 않는다. 정상 종료·피격·사망·파괴는 기존 Exit 경로로 정리한다. 비활성화·풀링 재사용 수명은 별도 요구가 있을 때 설계한다.
- Handler는 타격 Handler를 보관하지 않고 전략 생성 시 전달한다. 입력 구독·ComboTrigger·Gizmo Preview의 정리는 사용하는 전략의 End에서 수행한다.
- `AttackDefinitions` 공개 접근자는 Combat Editor 조회용이다. 직렬화된 `_attackDefinitions`는 런타임 전략 생성에도 사용한다.

## 3. 기존 역할·데이터 규칙

| 역할 | 설정 | 실행 |
| --- | --- | --- |
| GroundCombo | 지상 1·2·3타 SO 배열, 모션 선택값 1 | 진입 시 수평 정지, 공격 중 입력으로 ComboTrigger 설정, 단계별 SO 타격 |
| AirAttack | 공중 SO 하나, 모션 선택값 3 | 이동·중력 유지, 각 타격 Event에서 동일 SO 적용, 콤보 입력 구독 없음 |

- 역할 목록 순서는 선택에 영향을 주지 않는다. **역할 내부의 지상 SO 순서는 콤보 단계와 대응**하므로 임의로 바꾸지 않는다.
- PlayerAttackDataSO는 `AttackData Data`와 `int AnimatorAttackType`을 보관한다. 런타임 상태·구독·타이머는 SO에 넣지 않는다.
- 공격 SO는 `Assets/Prefabs/Player/ScriptableObjects/Attacks/`에 배치한다. 기존 지상 3타와 공중 공격은 별도 SO다.
- Player Stats의 공격 목록은 사용하지 않는다. 새 Player 공격을 기존 `AgentStatData.attackDatas`에 중복 등록하지 않는다. Monster의 기존 StatData 목록은 별도 경로로 유지한다.
- 모션 선택값은 콤보 단계 번호가 아니다. 현재 지상 콤보 중 AttackType은 1을 유지한다.

## 4. 추가 방식별 작업

### 기존 공격의 수치·범위 변경

1. 해당 역할의 PlayerAttackDataSO를 수정한다. 공유 에셋인지 확인하여 다른 개체에 대한 영향을 구분한다.
2. 타격 Handler Inspector의 Combat Editor에서 역할·단계 SO를 선택한다. Undo·Scene Handle·Animation Preview를 유지한다.
3. `GetHitBox()`와 실제 타격이 같은 계산을 사용하는지 확인한다. 수치 변경만으로 Controller·State·Transition을 수정하지 않는다.

### 지상 콤보 단계 추가

1. 새 단계 SO를 생성하여 GroundCombo의 Attacks 배열에 추가한다.
2. 해당 Clip·콤보 전이·실패 시 End 모션을 Animator에 연결하고, 타격 Event에 새 단계 번호를 넣는다.
3. 지상 전략은 `1.._attacks.Length`만 처리한다. **0·음수·범위 초과는 무시**하며 인자 0을 첫 공격으로 해석하지 않는다.
4. 같은 단계에서 여러 타격 Event가 필요하면 모두 같은 단계 번호를 사용한다. 단계별 Stamina 추가 소비는 현재 정책에 포함되지 않는다.

### 새로운 공격 전략 추가

1. `Player/Attack/`에 필요한 의존성만 받는 `IPlayerAttackStrategy` 구현체를 추가한다. 새 Component·Factory·Resolver·Wrapper는 요구가 없으면 만들지 않는다.
2. 새 역할이면 PlayerAttackRole을 추가하고 Handler.Initialize의 전략 생성 분기와 TryStartAttack의 선택 정책을 함께 갱신한다. **현재 두 역할의 분기를 그대로 두고 Inspector 항목만 추가하면 새 전략으로 연결되지 않는다.**
3. 기존 역할의 실행 방식 교체라면 해당 역할의 생성 코드만 교체한다. 전략 클래스 Type·설정 목록 인덱스로 선택하지 않는다.
4. 전략의 StartData를 시작 모션·비용의 원본으로 사용한다. 입력 구독은 Begin에서 등록하고 End에서 해제한다.
5. 타격은 OnAttackFrame에서 처리한다. 근접 공격이면 PerformAttack(data)와 공용 GetHitBox를 사용한다. Preview가 필요하면 전략에서 설정하고 End에서 해제한다.
6. Factory의 `typeof(AttackState)` 키와 기존 PlayerAttackState·AttackTransition·AttackEndTransition을 우선 재사용한다. 별도 State나 지속 실행 계약은 실제 요구가 있을 때만 확장한다.

## 5. Animation Event·종료·에셋 연결

| Event | 수신·처리 |
| --- | --- |
| 타격 | `OnAnimationOnFrame(int intParameter)` → Controller.OnAnimationEvent → AttackState.OnAttackFrame |
| 현재 지상 1·2·3타 | 각각 정수 1·2·3. 3타의 모든 타격 Event는 3 |
| 공중 타격 | 전략이 단계 인자를 무시하고 모든 타격 Event 처리 |
| 종료 | `OnAnimationEnd()` → AttackEndTransition. Player는 지면에 따라 Grounded 또는 Fall 복귀 |

- Proxy·Controller의 `intParameter`는 일반 정수 전달용이며, 공격 경로에서만 `attackStep`으로 해석한다. 현재 OnFrame 처리 대상은 AttackState 계열이다.
- 실제 Unity Animator와 Proxy는 같은 GameObject에 연결한다. 기존 함수 이름·Event 시점을 임의로 바꾸지 않는다.
- 지상 콤보는 기존 ComboTrigger·HasExitTime 전이로 연결한다. 성공 시 다음 공격, 실패·최종 공격 시 대응 End Clip을 거쳐 종료한다. 공격 실행 Clip에 종료 Event를 중복 추가하지 않는다.
- 타격 Handler의 `_targetLayer`, 기존 SO 값·GUID·Prefab Override를 보존한다. 역할별 SO는 PlayerController의 `_attackHandler._attackDefinitions`에 연결한다.
- Scene·Prefab·Animator·Clip·SO는 Unity Editor API로 변경한다. CLI는 샌드박스 밖에서 실행한다. 미저장 Scene을 자동 저장·재로드하지 않으며, 연결 불가 시 수동 설정 위치·값을 안내한다.
- 코드 주석은 짧은 단답형으로 작성한다. 과도한 throw 검증·중복 null 체크·임시 Migration·Verification 스크립트를 추가하지 않는다.

## 6. 검증과 보고

- [ ] 컴파일·삭제 API 참조·Missing Script·SO/Animator/Proxy/Layer 연결 확인.
- [ ] 역할 목록 순서와 무관한 선택, 지상 SO 순서·Event 단계 대응 확인.
- [ ] 시작 가능 판정에서 실행·비용 소비가 없고, 성공한 진입에서만 Stamina 한 번 소비.
- [ ] 지상 단계별 피해·범위, 공중 다단 타격·이동·중력·착지 후 전략 고정 확인.
- [ ] 종료·피격·사망·반복 공격 후 구독·ComboTrigger·Preview 잔류 없음.
- [ ] Gizmo·Combat Editor·실제 타격 범위 및 Player 이동·Jump·Dash·Goblin 회귀 확인.

Play 검증 전 사용자 조작과 기대 결과를 먼저 안내하고 State·Animator·Stamina·Console을 읽어 확인한다. 강제 입력·피해·상태 전환 코드는 만들지 않는다. 컴파일 성공과 실제 동작 확인을 구분해 보고한다.

기존 Agent/Flying 계획의 AgentCombatHandler·범용 전략 예시는 현재 API와 다르다. Player 작업에 재도입하지 말고 위 소스와 이 가이드를 기준으로 한다.
