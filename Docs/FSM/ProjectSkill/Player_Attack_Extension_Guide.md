# Player 공격 추가 가이드

사용자가 공격 모션을 지정하고 다음처럼 요청하면 사용하는 AI 작업 지침이다.

> `[Animation 경로]`의 해당 애니메이션을 기반으로 PlayerAttack을 추가할 거야.

지정된 모션 확인부터 공격 SO·전략 등록·Animator 전이·Animation Event·Prefab 연결까지 진행한다. 공용 FSM 수명과 근접 타격은 재사용하고, Player의 선택·비용·콤보 정책은 Player 영역에 유지한다.

## 1. 작업 입력과 소스 확인

- 먼저 `Docs/FSM/ProjectSkill/AGENTS.md`, `Plan_Writing_Guide.md`와 Git 변경사항을 확인한다. 사용자 수정·삭제·미저장 Scene을 보존한다.
- 지정한 AnimationClip의 Sprite 순서·프레임률·길이·Loop·기존 Event·바인딩 경로·공유 사용처를 확인한다. Sprite만 지정했다면 기존 Slice·pivot·PPU·Sprite ID를 보존하며 필요한 Clip을 만든다.
- 기존 모션 교체인지, 콤보 단계 추가인지, 별도로 선택할 새 공격인지 구분한다. 별도 공격의 발동 입력·선택 조건이 없으면 먼저 확인한다. 현재 A 입력은 지상 GroundCombo·공중 AirAttack만 선택한다.
- 타격 시점·횟수, 피해·범위·Stamina, 이동 유지·종료 모션을 확인한다. 모션만 보고 피해 방식이나 새 조작키를 임의로 확정하지 않는다. 미지정 수치는 유사 공격의 초기안을 제시하고 결과를 바꾸는 결정만 확인한다.
- 모션만 달라지고 실행 방식이 같으면 기존 전략을 재사용한다. 새 State·입력·공용 인터페이스를 자동으로 추가하지 않는다.

아래 경로는 `Assets/Scripts/FSM/` 기준이다. 문서보다 현재 소스를 우선한다.

| 확인 대상 | 파일 |
| --- | --- |
| 설정·전략 생성·선택 | `Player/Attack/PlayerAttackHandler.cs` |
| 실행 계약·기존 전략 | `Player/Attack/IPlayerAttackStrategy.cs`, `PlayerGroundComboAttack.cs`, `PlayerAirAttack.cs` |
| 공격 SO | `Player/SOData/PlayerAttackDataSO.cs` |
| 초기화·State 연결 | `Player/@Hub/PlayerController.cs`, `Player/PlayerState/PlayerStateFactory.cs`, `Player/PlayerState/States/PlayerAttackState.cs` |
| 공용 State·전이 수명 | `Agent/StateControl/AgentStateBase.cs`, `Agent/StateControl/States/AttackState.cs`, `Agent/StateControl/TransitionRules/Attack/AttackTransition.cs`, `AttackEndTransition.cs` |
| Event·타격·Animator 계약 | `Agent/Handler/AgentAnimationEventProxy.cs`, `AgentMeleeHitHandler2D.cs`, `Agent/@Hub/IAgentAnimator.cs` |
| Parameter 연결 | `Player/@Hub/PlayerAnimator.cs`, `Player/SOData/PlayerAnimationDataSO.cs` |
| 입력·선택 정책 | `Player/Input/PlayerInput.cs`, `PlayerInputCommands.inputactions`, `Player/Attack/PlayerAttackHandler.cs` |
| 범위 편집 | `Assets/Editor/AgentCombat/AgentCombatEditor.cs`, `AgentMeleeHitHandler2DEditor.cs` |

Animator·Clip은 `Assets/Prefabs/Player/Animations/`의 `Hero_Anim.controller`와 기존 `Hero_Attack*.anim`을 기준으로 확인한다. 이름이 같은 지상 3타·공중 State는 계층 경로로 구분한다.

## 2. 지정 모션에서 시작하는 작업 순서

| 순서 | 작업·완료 결과 |
| --- | --- |
| 1. 모션·요구 확인 | 지정 Clip의 공격·회복 구간, 사용할 역할·발동 방식·타격 시점 결정 |
| 2. 설정·전략 구성 | 단계별 SO와 실행 전략 준비, 역할별 AttackDefinition에 모션 선택값·SO 배열 등록 |
| 3. 선택 경로 연결 | Initialize의 생성 분기와 TryStartAttack의 선택 정책 연결. 기존 역할 교체·단계 추가라면 불필요한 새 역할 없음 |
| 4. Animator 연결 | Attack 내부의 진입점·모션·콤보/종료 분기·공통 출구 연결 |
| 5. Event·Prefab 연결 | 타격·종료 Event와 실제 Animator·Proxy·Handler·SO 참조 연결 |
| 6. 검증 | 컴파일·설정 확인 후 사용자 조작으로 진입·타격·종료·중단 확인 |

기존 공유 Clip의 Event를 바꾸면 다른 공격도 영향을 받는다. 기존 동작과 다른 Event가 필요하면 Player용 Clip을 분리하고 새 경로에만 연결한다. 공격에 불필요한 Transform·Collider 곡선이 포함되지 않았는지 확인한다.

## 3. 책임과 실행 계약

| 구성 | 책임 |
| --- | --- |
| PlayerController | Component 확보·의존성 주입·Factory 구성 |
| PlayerAttackHandler | Inspector 역할/모션 선택값/SO 목록, 개체별 전략 생성, 선택·시작 가능 판정, 모션 타입 적용·비용 소비 |
| PlayerAttackState | 공통 Attack Bool과 Handler의 진입·타격·종료 연결 |
| IPlayerAttackStrategy 구현체 | 공격별 실행·타격·입력 구독·Trigger·Preview 정리 |
| AgentMeleeHitHandler2D | 전달받은 AttackData로 수평 사각형 피해·넉백 적용, 공용 범위 계산·Gizmo 표시 |
| Animator | 모션 선택, 콤보 입력 수락·순서 및 종료 모션 분기 |

전략은 일반 C# 객체로 개체마다 생성한다. `IPlayerAttackStrategy`는 `StartData`, `Begin()`, `OnAttackFrame(int attackStep)`, `End()`만 사용한다.

```text
PlayerInput.OnAttackRequested
→ 현재 State의 AttackTransition
→ PlayerAttackHandler.TryStartAttack(): 전략·모션 선택 및 시작 비용 확인
→ PlayerAttackState.OnEnter
→ Handler.BeginAttack(): 모션 타입 적용·전략 Begin·비용 1회 소비
→ base.OnEnter(): Attack Bool 활성화

Clip OnFrame → Proxy → Controller → 현재 AttackState → Handler → 전략 타격
Clip End → OnAnimationEnded → AttackEndTransition → State.Exit → 전략 End
```

- 시작 판정에서는 실행·입력 구독·Stamina 소비를 하지 않는다. 성공한 선택을 State 진입부터 Exit까지 유지한다.
- 시작 판정에서 전략과 역할별 모션 선택값을 함께 고정한다. Begin에서 지면 상태를 다시 판단하지 않으며 착지하더라도 공격 중 전략·모션값을 바꾸지 않는다. 비용은 시작 SO 기준으로 State 진입당 한 번만 소비한다.
- AttackState는 공통 Bool 활성화·해제 및 종료 시 모션 타입 0 초기화를 담당한다. PlayerAttackHandler의 Animator 필드는 모션 타입 적용에 사용한다.
- 전략은 State를 직접 전환하지 않는다. 정상 종료·피격·사망·파괴는 기존 Exit 경로로 정리한다. 비활성화·풀링 재사용 수명은 별도 요구가 있을 때 설계한다.
- Handler는 타격 Handler를 보관하지 않고 전략 생성 시 전달한다. 입력 구독·ComboTrigger·Gizmo Preview의 정리는 사용하는 전략의 End에서 수행한다.
- `AttackDefinitions` 공개 접근자는 Combat Editor 조회용이다. 직렬화된 `_attackDefinitions`는 런타임 전략 생성에도 사용한다.

## 4. 기존 역할·데이터 규칙

| 역할 | 설정 | 실행 |
| --- | --- | --- |
| GroundCombo | 지상 1·2·3타 SO 배열, AttackDefinition 모션 선택값 1 | 진입 시 수평 정지, 공격 중 입력으로 ComboTrigger 설정, 단계별 SO 타격 |
| AirAttack | 공중 SO 하나, AttackDefinition 모션 선택값 3 | 이동·중력 유지, 각 타격 Event에서 동일 SO 적용, 콤보 입력 구독 없음 |

- 역할 목록 순서는 선택에 영향을 주지 않는다. **역할 내부의 지상 SO 순서는 콤보 단계와 대응**하므로 임의로 바꾸지 않는다.
- PlayerAttackDataSO는 피해·범위·비용을 담은 `AttackData Data`만 보관한다. 모션 선택값은 SO가 아니라 `AttackDefinition.AnimatorAttackType`에 역할별로 설정한다. 런타임 상태·구독·타이머는 SO에 넣지 않는다.
- 공격 SO는 `Assets/Prefabs/Player/ScriptableObjects/Attacks/`에 배치한다. 기존 지상 3타와 공중 공격은 별도 SO다.
- Player Stats의 공격 목록은 사용하지 않는다. 새 Player 공격을 기존 `AgentStatData.attackDatas`에 중복 등록하지 않는다. Monster의 기존 StatData 목록은 별도 경로로 유지한다.
- 모션 선택값은 콤보 단계 번호가 아니다. 현재 지상 콤보 중 AttackType은 1을 유지한다.

## 5. 추가 방식별 작업

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
4. AttackDefinition의 AnimatorAttackType을 Animator의 해당 공격 진입 조건과 맞추고, 전략의 StartData는 시작 비용의 원본으로 사용한다. 입력 구독은 Begin에서 등록하고 End에서 해제한다.
5. 타격은 OnAttackFrame에서 처리한다. 근접 공격이면 PerformAttack(data)와 공용 GetHitBox를 사용한다. Preview가 필요하면 전략에서 설정하고 End에서 해제한다.
6. Factory의 `typeof(AttackState)` 키와 기존 PlayerAttackState·AttackTransition·AttackEndTransition을 우선 재사용한다. 별도 State나 지속 실행 계약은 실제 요구가 있을 때만 확장한다.

현재 계약은 진입·타격 Event·종료용이며 AttackState.OnExecute는 비어 있다. 돌진·지속 판정·투사체가 필요하면 해당 실행 요구부터 설계한다. Clip 추가만으로 그 동작이 생기지는 않는다.

## 6. Animator 설정

### Parameter·모션 선택

| Parameter | 타입 | 연결 |
| --- | --- | --- |
| `IsAttack` | Bool | 공용 AttackState 진입 true·종료 false |
| `AttackType` | Int | Handler.BeginAttack에서 AttackDefinition.AnimatorAttackType 적용. Exit에서 0 초기화 |
| `ComboTrigger` | Trigger | 콤보 전략의 공격 입력으로 설정, 전이에서 소비·End에서 잔여 Trigger 초기화 |
| `IsGrounded`, `IsFall` | Bool | Attack 상위의 지상·낙하 복귀 분기 |
| `IsHit`, `IsDeath` | Bool | 기존 상위 Any State 중단 경로 |

- 실제 할당된 PlayerAnimationDataSO의 Parameter 이름과 Controller의 이름·타입을 맞춘다. 위 이름은 현재 기본값이며 이름 변경 시 코드의 매핑도 확인한다.
- 모션 선택값은 AttackDefinition에만 설정한다. 기존 GroundCombo는 1, AirAttack은 3이다. 독립 모션 경로에는 충돌하지 않는 양의 정수를 배정하고 Entry 조건에 같은 값을 사용한다. 0은 종료 초기화값이다.
- 콤보 단계를 추가해도 AttackType을 단계 번호로 변경하지 않는다. 지상 1·2·3타는 계속 1이며 타격 단계는 Event 정수로 구분한다.

### Attack 진입·배치

현재 구조는 다음과 같다. Attack·Hero_Combo1은 별도 Animator Layer가 아니라 Sub State Machine이다.

```text
Base Layer
└─ Attack
   ├─ Entry: IsAttack=true, AttackType=1 → Hero_Combo1/Hero_Attack1
   ├─ Entry: IsAttack=true, AttackType=3 → Hero_Attack3 (공중)
   ├─ Hero_Combo1: 공격 1→2→3 및 각 End 모션
   └─ 공중 공격 → 공중 End 모션
```

1. 기존 외부 상태의 `IsAttack == true` 진입 경로를 재사용한다. 새 모션마다 Base Layer에 직접 진입 전이를 복제하지 않는다.
2. 기존 모션 교체는 대응 State의 Motion을 변경하고 진입값을 유지한다. 독립 공격은 Attack 내부에 State 또는 필요한 Sub State Machine을 추가한다.
3. 독립 공격의 Entry 조건은 `IsAttack == true` AND `AttackType == 해당 모션값`으로 연결한다. 콤보는 첫 공격으로 진입한다. Entry 조건 누락·값 중복·의도하지 않은 Default State 진입을 확인한다.
4. Handler의 SetAttackType이 공용 SetAttack(true)보다 먼저 호출되는 현재 순서를 유지한다. 전략과 모션값은 시작 판정에서 함께 고정한다.

### 콤보·종료 전이

| 경로 | 조건·설정 |
| --- | --- |
| 콤보 공격 → 다음 공격 | `ComboTrigger` AND `AttackType == 콤보 진입값`. 대응 End 전이보다 위에 배치 |
| 콤보 공격 → 해당 End | 별도 조건 없는 fallback. 다음 콤보가 선택되지 않으면 실행 |
| 마지막 공격 → 해당 End | 조건 없는 완료 경로. 첫 공격으로 자동 반복하지 않음 |
| End → 내부 Exit | `IsAttack == false`, Has Exit Time 끔, Duration 0 |
| 상위 Attack → 기본 행동 | 기존 `IsGrounded == true` / `IsFall == true` 공통 전이 재사용 |

- 현재 공격→다음 공격과 공격→End는 모두 Has Exit Time 켬·Exit Time 1·Duration 0이다. 기존 경로는 유지하고, 신규 모션은 실제 타격·회복 구간에 맞춰 시점을 정한다. 다음 공격 전이만 즉시 전환으로 만들면 연속 입력에서 모션을 건너뛸 수 있다.
- 단발 공격은 공격 Clip 자체에서 종료하거나, 제공된 회복 Clip을 End State로 분리한다. 별도 End 모션을 임의로 요구하지 않는다. 최종 종료 Clip의 Event가 발생하기 전에 다른 경로로 빠져나가지 않도록 연결한다.
- 콤보 실행 Clip에는 종료 Event를 넣지 않는다. 성공 시 다음 공격으로 이어지고 실패·최종 공격만 End Clip에서 종료한다.
- Hero_Combo1 내부 End는 IsAttack false에서 내부 Exit로 나가고, Attack에 등록된 조건 없는 Sub State Machine 출구로 Attack Exit까지 전달된다. 새로 중첩한 그룹도 내부 Exit만 만들고 상위 출구 연결을 빠뜨리지 않는다.
- 새 공격의 최종 State도 IsAttack false에서 해당 그룹의 Exit로 나가게 한다. AttackType 0으로 바뀐 것만으로 Animator가 자동 종료되지는 않는다.
- 피격·사망은 기존 상위 Any State 전이로 중단한다. 각 공격 State마다 Hit·Death·Grounded·Fall 목적지 전이를 복제하지 않으며, 기존 중단 우선순위·Duration을 유지한다.

## 7. Animation Event·종료·에셋 연결

| Event | 수신·처리 |
| --- | --- |
| 타격 | `OnAnimationOnFrame(int intParameter)` → Controller.OnAnimationEvent → AttackState.OnAttackFrame |
| 현재 지상 1·2·3타 | 각각 정수 1·2·3. 3타의 모든 타격 Event는 3 |
| 공중 타격 | 전략이 단계 인자를 무시하고 모든 타격 Event 처리 |
| 종료 | `OnAnimationEnd()` → AttackEndTransition. Player는 지면에 따라 Grounded 또는 Fall 복귀 |

- 지정 모션의 실제 타격 프레임에 OnAnimationOnFrame Event를 배치한다. 타격 횟수와 Event 개수를 맞춘다. 새로운 전략의 정수 해석은 명시하고 기존 지상 전략에는 반드시 유효한 1부터 시작하는 SO 단계 번호를 전달한다.
- 최종 종료 Clip의 타격·회복이 끝나는 시점에 OnAnimationEnd Event를 한 번 배치한다. 타격·종료 Event 순서와 전이 중 누락·중복을 확인하고 공격 Clip은 특별한 요구가 없으면 Loop를 끈다.
- End Event는 FSM 전이 요청이다. AttackEndTransition → State.Exit → IsAttack false·AttackType 0 → Animator Exit의 순서를 유지한다. Animator의 Exit만 연결해 FSM 종료 Event를 생략하지 않는다.
- Proxy·Controller의 `intParameter`는 일반 정수 전달용이며, 공격 경로에서만 `attackStep`으로 해석한다. 현재 OnFrame 처리 대상은 AttackState 계열이다.
- 실제 Unity Animator와 Proxy는 같은 GameObject에 연결한다. 기존 함수 이름·Event 시점을 임의로 바꾸지 않는다.
- 타격 Handler의 `_targetLayer`, 기존 SO 값·GUID·Prefab Override를 보존한다. 역할별 SO와 AnimatorAttackType은 PlayerController의 `_attackHandler._attackDefinitions`에 설정한다.
- 지정·신규 Clip과 End Clip은 `Assets/Prefabs/Player/Animations/`, 공격 SO는 `Assets/Prefabs/Player/ScriptableObjects/Attacks/`, 전용 전략은 `Assets/Scripts/FSM/Player/Attack/`에 배치한다. 기존 사용자 경로·공유 에셋은 필요 없이 이동하지 않는다.
- Scene·Prefab·Animator·Clip·SO는 Unity Editor API로 변경한다. CLI는 샌드박스 밖에서 실행한다. 미저장 Scene을 자동 저장·재로드하지 않으며, 연결 불가 시 수동 설정 위치·값을 안내한다.
- 코드 주석은 짧은 단답형으로 작성한다. 과도한 throw 검증·중복 null 체크·임시 Migration·Verification 스크립트를 추가하지 않는다.

## 8. 검증과 보고

- [ ] 컴파일·삭제 API 참조·Missing Script·SO/Animator/Proxy/Layer 연결 확인.
- [ ] 역할 목록 순서와 무관한 전략·모션 선택, AttackDefinition 모션값과 Animator 진입 조건, 지상 SO 순서·Event 단계 대응 확인.
- [ ] 지정한 모션이 의도한 입력으로 재생되고, 콤보 전이 우선순위·Exit Time·Duration 및 내부/상위 Exit 연결 확인.
- [ ] 시작 가능 판정에서 실행·비용 소비가 없고, 성공한 진입에서만 Stamina 한 번 소비.
- [ ] 지상 단계별 피해·범위, 공중 다단 타격·이동·중력·착지 후 전략 고정 확인.
- [ ] 타격·종료 Event의 누락·중복과 연속 입력 시 단계 건너뜀·자동 반복·FSM/Animator 불일치 없음.
- [ ] 종료·피격·사망·반복 공격 후 구독·ComboTrigger·Preview 잔류 없음.
- [ ] Gizmo·Combat Editor·실제 타격 범위 및 Player 이동·Jump·Dash·Goblin 회귀 확인.

Play 검증 전 사용자 조작과 기대 결과를 먼저 안내하고 State·Animator·Stamina·Console을 읽어 확인한다. 강제 입력·피해·상태 전환 코드는 만들지 않는다. 컴파일 성공과 실제 동작 확인을 구분해 보고한다.

완료 보고에는 역할·발동 입력, 지정 모션과 연결한 Animator 경로·모션값, SO·Event 정수/시점, 변경 파일, 검증 결과와 필요한 수동 설정만 남긴다. Editor 연결이 안 되면 미완료 연결 위치·조건·값을 구체적으로 안내하고 완료로 표시하지 않는다.

기존 Agent/Flying 계획의 AgentCombatHandler·범용 전략 예시는 현재 API와 다르다. Player 작업에 재도입하지 말고 위 소스와 이 가이드를 기준으로 한다.
