# Player Combo Attack 공격·종료 클립 분리 계획

## 1. 목적과 작업 범위

`Hero_Anim.controller`의 공격 실행 모션과 종료 모션을 분리한다. 콤보 성공 시 다음 공격으로 바로 이어지고, 콤보 입력이 없거나 마지막 공격이면 해당 `_end` 클립을 재생한 뒤 Animation End Event로 FSM을 종료한다.

- 지상 콤보 순서: `Attack1 → Attack2 → Attack3` 유지.
- 지상 콤보 진행 중 `AttackType == 1` 유지. 숫자를 1·2·3으로 증가시키지 않는다.
- 공중 공격: `AttackType == 3`의 단발 공격 유지.
- 공격 클립: 공격 시트만 사용하고 `OnAnimationOnFrame` 유지, `OnAnimationEnd` 제거.
- 종료 클립: 대응하는 `_end` 시트 사용, `OnAnimationEnd`만 배치.
- FSM: 공격 실행·콤보·종료 모션 동안 동일한 `PlayerAttackState` 유지.
- 변경 대상: Player 공격 AnimationClip과 Animator 전이. 공격 동작 C# 수정 없음. 별도로 사용자가 상태 전환 Debug 로그를 주석 처리한 변경은 커밋에 포함.

이 문서는 현재 소스와 에셋을 기준으로 작성한 Plan7이다. Plan6의 `ComboAttackHandler`, `TryHandleAttackFinished()`, AttackType 증가 방식은 과거 설계이며, 이번 작업에서 복구하거나 적용하지 않는다. 기존 계획 문서는 변경하지 않는다.

현재 상태: 1~4단계 에셋 구현·정적 검증 완료. 독립 Animator 자동 평가 8개 시나리오 통과. 실제 Player의 Play 모드·수동 조작 검증은 미실행. 아래 작업 체크는 실제 적용 및 검증 후 갱신한다.

## 2. 현재 코드·에셋 확인 결과

### 2.1 점검 기준 파일

| 구분 | 경로 |
| --- | --- |
| Animator | `Assets/Prefabs/Player/Animations/Hero_Anim.controller` |
| 공격·종료 클립 | `Assets/Prefabs/Player/Animations/Hero_Attack*.anim` |
| 원본 시트 | `Assets/@tempAssets/@Player/male_hero_free/individual_sheets/male_hero-combo_*.png` 및 대응 `.meta` |
| Player 공격 상태 | `Assets/Scripts/FSM/Player/PlayerState/States/PlayerAttackState.cs` |
| 공통 공격 상태 | `Assets/Scripts/FSM/Agent/StateControl/States/AttackState.cs` |
| 공격 종료 전이 | `Assets/Scripts/FSM/Agent/StateControl/TransitionRules/Attack/AttackEndTransition.cs` |
| 이벤트 수신 | `Assets/Scripts/FSM/Agent/Handler/AgentAnimationEventProxy.cs` |
| 이벤트 전달·공격 시작 | `Assets/Scripts/FSM/Agent/@Hub/AgentController.cs`, `Assets/Scripts/FSM/Player/@Hub/PlayerController.cs` |
| Animator API·FSM 구성 | `Assets/Scripts/FSM/Player/@Hub/PlayerAnimator.cs`, `Assets/Scripts/FSM/Player/PlayerState/PlayerStateFactory.cs` |

구현 시작 시 현재 파일을 다시 확인한다. 문서 작성 이후 사용자가 변경한 클립·전이·이벤트를 이전 상태로 덮어쓰지 않는다.

### 2.2 현재 클립

모든 대상 클립은 12 FPS, Loop Time 해제 상태다.

| 파일 | Sprite 구성 | 현재 길이 | 현재 이벤트 | 상태 |
| --- | --- | --- | --- | --- |
| `Hero_Attack1.anim` | 공격 3프레임 + 종료 3프레임 | 0.5초 | 타격 0초, 종료 약 0.4167초 | 공격·종료 혼합 |
| `Hero_Attack2.anim` | 공격 5프레임 | 약 0.4167초 | 타격 0.25초, 종료 약 0.4167초 | 공격 부분은 이미 분리 |
| `Hero_Attack3.anim` | 공격 12프레임 | 1초 | 타격 0.25·0.4167·0.6667·0.9167초, 종료 1초 | 공격 부분은 이미 분리 |
| `Hero_Attack2_end.anim` | 종료 4프레임 | 약 0.3333초 | 종료 0.25초 | 파일 존재, Animator 미연결 |
| `Hero_Attack3_end.anim` | 종료 6프레임 | 0.5초 | 종료 약 0.4167초 | 파일 존재, Animator 미연결 |
| `Hero_Attack1_end.anim` | 미생성 | — | — | 신규 생성 필요 |

### 2.3 현재 Animator와 입력 처리

```text
Base Layer
└─ Attack
   ├─ Hero_Combo1                   // 지상: AttackType == 1
   │  └─ Hero_Attack1 → Hero_Attack2 → Hero_Attack3
   └─ Hero_Attack3                  // 공중: AttackType == 3
```

- 지상 `Hero_Attack1 → Hero_Attack2`: `ComboTrigger` + `AttackType == 1`, Has Exit Time 활성화, Exit Time 약 0.539.
- 지상 `Hero_Attack2 → Hero_Attack3`: 동일 조건, Has Exit Time 활성화, Exit Time 약 0.810.
- 각 공격 State의 현재 Exit 조건: `IsAttack == false`, Has Exit Time 해제.
- 지상 3타와 공중 공격은 서로 다른 Animator State지만 같은 `Hero_Attack3.anim`을 공유한다.
- `PlayerAttackState`가 활성화된 동안 공격 입력은 `SetComboTrigger()`를 호출한다.
- `PlayerAttackState.OnExit()`에서 입력 구독 해제 및 `ResetComboTrigger()`를 수행한다.
- `AttackEndTransition`은 클립 이름이 아닌 `OnAnimationEnded` 수신 여부로 종료를 판단한다.

따라서 종료 이벤트를 `_end` 클립으로 이동해도 기존 코드로 동일한 종료 흐름을 사용할 수 있다.

## 3. 목표 구조

### 3.1 Animator 구성

```text
Base Layer / Attack
├─ Hero_Combo1
│  ├─ Hero_Attack1 ── 콤보 성공 ──→ Hero_Attack2 ── 콤보 성공 ──→ Hero_Attack3
│  │       │                              │                              │
│  │       └─ 입력 없음 → Hero_Attack1_End │                              │
│  │                                      └─ 입력 없음 → Hero_Attack2_End │
│  │                                                                     └─→ Hero_Attack3_End
│  └─ 각 End → Hero_Combo1의 Exit → Attack의 Exit
│
├─ Hero_Attack3                     // 기존 공중 공격 State
│  └─ Hero_Attack3_End → Attack의 Exit
└─ Attack의 상위 복귀 전이 → Grounded 또는 Hero_Jump
```

End Animator State 이름은 `Hero_AttackN_End`, AnimationClip 파일명은 기존 규칙에 맞춰 `Hero_AttackN_end.anim`으로 통일한다. 지상·공중의 `Hero_Attack3_End`는 서로 다른 State이며 동일한 종료 클립을 공유한다. 두 경로를 합치기 위한 추가 Sub State Machine 리팩토링은 하지 않는다.

### 3.2 FSM 종료 흐름

```text
공격 입력 → ComboTrigger 설정
  ├─ 분기 시점에 콤보 조건 충족 → 다음 공격 클립 / PlayerAttackState 유지
  └─ 미충족 또는 마지막 공격 → 대응 End 클립 / PlayerAttackState 유지
       → OnAnimationEnd
       → AgentAnimationEventProxy.OnAnimationEnd()
       → AgentController.OnAnimationEnded
       → AttackEndTransition
       → GroundedState 또는 FallState로 FSM 전환
       → PlayerAttackState.OnExit(): ComboTrigger 초기화
       → AttackState.OnExit(): IsAttack = false, 공격 타입 초기화
       → End Animator State의 Exit 및 상위 복귀
```

Animation Event 수신과 FSM 전환은 동일 시점의 직접 호출이 아니다. 종료 전이 규칙이 다음 FSM 평가에서 이벤트 플래그를 확인하는 기존 흐름을 유지한다.

End 재생 중 입력은 현재 코드처럼 ComboTrigger를 설정할 수 있으나, End에서 다음 공격으로 이어지는 전이는 만들지 않는다. 미소비 Trigger는 State 종료 시 초기화된다. 별도 입력 차단 이벤트나 Combo 버퍼를 추가하지 않는다.

## 4. 클립 수정 규칙

| 파일 | 계획 |
| --- | --- |
| `Hero_Attack1.anim` | `male_hero-combo_1.png`의 기존 공격 3프레임만 유지. 종료 시트 프레임 제거. 길이·Stop Time을 공격 구간에 맞춰 조정. 타격 이벤트 유지, 종료 이벤트 제거. |
| `Hero_Attack1_end.anim` | `male_hero-combo_1_end.png`의 실제 Sprite를 시트 순서대로 배치해 신규 생성. 종료 이벤트 1개 배치. |
| `Hero_Attack2.anim` | 기존 공격 프레임·길이·타격 이벤트 유지. 종료 이벤트만 제거. |
| `Hero_Attack2_end.anim` | 기존 클립 재사용. 종료 이벤트 1개와 비반복 설정 확인. |
| `Hero_Attack3.anim` | 기존 공격 12프레임·길이·타격 이벤트 4개 유지. 종료 이벤트만 제거. |
| `Hero_Attack3_end.anim` | 기존 클립 재사용. 지상 최종타와 공중 공격 양쪽의 End State에 연결. |

### 4.1 프레임 순서와 타이밍

- 12 FPS와 기존 SpriteRenderer 바인딩 경로 유지.
- 실제 Sprite 참조를 Editor에서 조회한다. 파일ID나 GUID를 임의로 작성하지 않는다.
- Sprite 이름에 번호 누락이 있다. 존재하지 않는 `_end_3`, `_end_5` 등을 만들어 참조하지 않는다.
- Attack1 End 원본의 실제 Sprite는 `_end_0`, `_end_1`, `_end_2`, `_end_4`의 4개다. 신규 End 클립은 이 4개를 시트 순서대로 사용한다.
- 현재 Attack1에는 End의 첫 3개만 들어 있다. 마지막 `_end_4`를 포함하므로 종료 모션 길이가 기존 포함 구간과 달라지는 점을 기록하고 수동 확인한다.
- Attack2 원본에는 6개 Sprite가 있지만 현재 공격 클립은 `_1`, `_2`, `_3`, `_4`, `_7`의 5개를 사용한다. 이번에는 기존 선택을 유지하며 `_0`를 임의로 추가하지 않는다.
- Attack1 공격 클립은 3프레임을 각 1/12초 유지하는 약 0.25초 구간을 초기 기준으로 한다. 마지막 프레임의 표시 시간이 0이 되지 않도록 실제 길이·Stop Time 확인.
- 신규 Attack1 End는 4프레임, 약 0.3333초를 초기 기준으로 한다. 기존 End2·End3의 프레임 배치와 타이밍은 우선 유지한다.
- 공격·종료 사이에 캐릭터 위치가 튀면 먼저 Sprite 순서와 기존 Pivot을 확인한다. 원본 시트 재슬라이싱·Pivot·PPU·Import 설정 변경은 이번 범위에서 제외한다.

### 4.2 Animation Event

- 공격 클립의 `OnAnimationOnFrame` 개수·발생 시각 보존. 종료 모션에 타격 이벤트 추가 금지.
- 공격 클립의 `OnAnimationEnd`는 모두 제거. 지상·공중 공유 Attack3도 포함.
- End 클립마다 `OnAnimationEnd` 정확히 1개 유지.
- 신규 End1의 이벤트는 기존 End 클립과 같은 마지막 Sprite 진입 시점부터 검증한다. 4프레임·12 FPS 기준 초기 위치는 0.25초.
- End의 마지막 Sprite는 FSM 평가까지 짧게 표시될 수 있다. 복귀 포즈가 보이는지 확인하고, 필요 시 해당 클립 내부의 이벤트 시점을 후반으로 조정한다. 클립 끝 경계에 고정해 이벤트 수신을 보장했다고 판단하지 않는다.
- 종료 이벤트 전에 End를 자동으로 빠져나가는 무조건 Exit 전이를 만들지 않는다.

## 5. Animator 전이 설정

### 5.1 정상 콤보·종료 전이

| 위치 | 전이 | 조건 | Has Exit Time | 초기 Exit Time | 우선순위 |
| --- | --- | --- | --- | --- | --- |
| 지상 | Attack1 → Attack2 | ComboTrigger + AttackType == 1 | 켬 | 1.0 | 1 |
| 지상 | Attack1 → Attack1_End | 없음 | 켬 | 1.0 | 2 |
| 지상 | Attack2 → Attack3 | ComboTrigger + AttackType == 1 | 켬 | 1.0 | 1 |
| 지상 | Attack2 → Attack2_End | 없음 | 켬 | 1.0 | 2 |
| 지상 | Attack3 → Attack3_End | 없음 | 켬 | 1.0 | — |
| 공중 | Attack3 → Attack3_End | 없음 | 켬 | 1.0 | — |
| 지상·공중 각 End | End → 해당 Sub State Machine Exit | IsAttack == false | 끔 | 사용하지 않음 | — |

위 표의 `AttackN`은 각 경로의 `Hero_AttackN` State를 의미한다.

- 동일 공격에서 콤보와 End 전이는 같은 분기 시점으로 설정한다. 콤보 조건을 만족하면 상위 순서의 다음 공격 전이를 사용하고, 그렇지 않으면 End 전이를 사용한다.
- End 진입은 Trigger의 false 조건으로 구현하지 않는다. 조건 없는 Has Exit Time 전이를 대체 경로로 사용한다.
- Attack → End에 `IsAttack == false`를 걸지 않는다. End 이벤트를 받아야 FSM이 종료되어 IsAttack이 false가 되므로 해당 조건은 순환 대기를 만든다.
- 초기 전이 Duration = 0, Offset = 0, Interruption Source = None. 원본 공격 프레임과 End 첫 프레임을 누락 없이 확인하는 기준이다.
- 기존 콤보 Duration 약 0.052·0.071초와 Offset을 그대로 복사하지 않는다. 초기 값으로 검증한 뒤 필요한 경우에만 Animator 안에서 조정하고 최종 값을 기록한다.
- 1.0은 공격 구간을 끝까지 재생한 뒤 분기하기 위한 초기 값이다. 실제 콤보 감각은 수동 검증 후 조정하되 다음 공격·End의 분기 시점은 일치시킨다.
- Attack1 길이가 0.5초에서 약 0.25초로 바뀌므로 기존 Exit Time 약 0.539를 그대로 유지하면 분기 시점이 약 0.269초에서 0.135초로 앞당겨진다. 이전 정규화 값을 자동 재사용하지 않는다.
- Has Exit Time을 해제한 ComboTrigger 즉시 전이로 되돌리지 않는다. 빠른 연타로 1 → 3이 건너뛰는 회귀를 방지한다.

### 5.2 기존 진입·종료·중단 경로 유지

- 외부 State → Attack Sub State Machine 진입 방식 유지.
- Attack Entry의 `IsAttack == true` + `AttackType == 1/3` 분기 유지. AttackType 2 Entry 추가 없음.
- `Hero_Combo1`의 기본 State와 기존 진입 구조 유지.
- `Hero_Combo1 Exit → Attack Exit`의 상위 종료 경로 유지.
- `Attack → Grounded`의 IsGrounded 조건 및 `Attack → Hero_Jump`의 IsFall 조건 유지.
- 초기 적용 시 외부 FSM 종료 대응용으로 유지했던 공격 State의 `IsAttack == false → Exit` 4개는 사용자가 불필요함을 확인해 제거했다. 현재 정상 종료는 대응 End를 거친 뒤 End State의 Exit에서 처리한다. 기존 Any State의 Hit·Death 중단 경로는 유지한다.
- 기존 Any State의 Hit·Death 우선 처리와 관련 전이 유지. End State에서도 같은 상위 중단 경로가 적용되는지 확인.
- End에서 다음 공격, Entry 또는 자기 자신으로 돌아가는 전이 추가 금지.

## 6. 파일별 변경 범위

| 경로 | 작업 |
| --- | --- |
| `Assets/Prefabs/Player/Animations/Hero_Attack1.anim` | 공격·종료 분리, 길이 조정, 종료 이벤트 제거 |
| `Assets/Prefabs/Player/Animations/Hero_Attack1_end.anim` 및 `.meta` | 신규 클립 생성, Unity가 meta 생성 |
| `Assets/Prefabs/Player/Animations/Hero_Attack2.anim` | 종료 이벤트 제거 |
| `Assets/Prefabs/Player/Animations/Hero_Attack3.anim` | 종료 이벤트 제거 |
| `Assets/Prefabs/Player/Animations/Hero_Attack2_end.anim` | 재사용·이벤트 검증. 수정 필요 시 해당 클립만 조정 |
| `Assets/Prefabs/Player/Animations/Hero_Attack3_end.anim` | 재사용·이벤트 검증. 수정 필요 시 해당 클립만 조정 |
| `Assets/Prefabs/Player/Animations/Hero_Anim.controller` | 지상 End State 3개·공중 End State 1개 추가, 분기·Exit 전이 구성 |

수정하지 않는 대상:

- `AttackState`, `PlayerAttackState`, `AttackTransition`, `AttackEndTransition`.
- Controller, Input, Animator API, 인터페이스, StateFactory와 ScriptableObject.
- 공격 타입별 데미지·범위·Stamina와 이동 제어 정책.
- Jump·Dash·Grounded Animator 구조, Monster Animator, Prefab·Scene.
- 기존 백업 Controller와 원본 PNG·TextureImporter·Sprite meta.
- 리팩토링 Log·PDF·Git commit은 별도 요청 시 수행.

지상 콤보는 계속 CombatHandler의 AttackType 1 데이터를 사용한다. 시각적인 2·3타에 맞춰 데이터나 Stamina를 변경하는 작업은 포함하지 않는다.

## 7. 순서별 적용 체크리스트

### 1단계 — 구현 전 재점검

- [x] Git 변경 목록과 Editor의 미저장 에셋·Scene 확인. 사용자 변경 보존.
- [x] 프로젝트를 명시해 Unity CLI/MCP의 실제 명령 응답 확인. status 탐색 결과만으로 연결 불가 판정 금지.
- [x] Play 모드 종료 후 에셋 작업 진행. 미저장 작업을 임의로 저장·폐기하지 않음.
- [x] 현재 Controller의 전이 순서·Motion·Exit Time·Duration·Offset, 대상 Clip의 Sprite·길이·이벤트 기록.
- [x] 원본 Sprite와 기존 End2·End3를 Editor에서 조회해 참조 검증.
- [x] 기존 에셋의 GUID 유지 및 작업 전후 변경 비교 방법 확보.

### 2단계 — 클립 분리·이벤트 정리

- [x] Unity Editor API로 `Hero_Attack1_end.anim` 생성. 실제 End Sprite 4개와 이벤트 1개 설정.
- [x] Attack1에서 End Sprite와 종료 이벤트 제거. 공격 3프레임·타격 이벤트·길이 검증.
- [x] Attack2·Attack3의 종료 이벤트 제거. 기존 공격 Sprite·타격 이벤트 보존.
- [x] End2·End3의 Motion, 길이, 종료 이벤트·비반복 설정 검증.
- [x] 공격 클립 End 이벤트 0개, 각 End 클립 End 이벤트 1개, End 타격 이벤트 0개 확인.

### 3단계 — Animator End 경로 연결

- [x] `Hero_Combo1` 내부에 지상 End State 3개 생성·클립 할당.
- [x] Attack1·Attack2의 콤보 전이 우선순위 및 무조건 End 대체 전이 설정.
- [x] 지상 Attack3 → Attack3_End 자동 전이 설정.
- [x] Attack 바로 아래에 공중 Attack3_End State 생성, 공중 Attack3 연결. 같은 End3 클립 재사용.
- [x] 각 End의 IsAttack false → Exit 전이 설정.
- [x] 상위 Sub State Machine 복귀 경로 유지. 기존 공격 State의 직접 Exit 4개는 후속 사용자 정리로 제거, 각 End의 Exit 유지.
- [x] Entry·Any State·AttackType·ComboTrigger 파라미터에 의도하지 않은 변경 없음 확인.

### 4단계 — 저장·정적 검증

- [x] 변경한 에셋만 저장. 관계없는 미저장 Scene·에셋 일괄 저장 금지.
- [x] Motion 누락, 유효하지 않은 Sprite 참조, Missing Animation Event 수신 함수 없음 확인.
- [x] 지상·공중 경로 모두 End까지 도달 가능한지 Editor API로 전이 목록 확인.
- [x] 이벤트 시간과 실제 Clip 길이·전이 시점이 일치하는지 확인.
- [x] Git diff의 에셋·신규 meta 범위 확인. 공격 동작 C# 변경 없음. 후속 사용자 변경인 상태 전환 Debug 로그 주석 처리도 확인해 커밋에 포함.

### 5단계 — Play 모드·수동 검증

- [ ] 아래 테스트 수행. 확인하지 못한 항목은 완료 표시하지 않고 수동 확인 필요로 보고.
- [ ] 성공·실패·공중 공격에서 FSM State와 Animator State가 의도대로 동기화되는지 확인.
- [x] 최종 Exit Time·Duration·Offset·클립 길이·Event 시각 기록. 현재 적용 값은 11.1에 기록. 수동 테스트 후 조정 시 재기록 필요.
- [x] 테스트용 오브젝트·로그·스크립트를 남기지 않음. 기존 Scene 변경 보존.

`.anim`, `.controller`, `.meta`는 연결된 Unity Editor API로 생성·변경한다. YAML을 직접 편집하거나 기존 GUID를 교체하지 않는다. 연결·권한 문제가 있으면 에셋 변경을 보류하고 남은 작업을 보고한다.

## 8. 동작 검증 시나리오

| 시나리오 | 기대 결과 |
| --- | --- |
| 지상에서 공격 1회 | Attack1 → Attack1_End → Grounded. Attack2 미진입 |
| Attack1 중 공격 입력, Attack2에서는 추가 입력 없음 | Attack1 → Attack2 → Attack2_End → Grounded |
| Attack1·Attack2 중 각각 다음 공격 입력 | Attack1 → Attack2 → Attack3 → Attack3_End → Grounded |
| 빠른 연타 | 각 공격의 정해진 분기 시점에만 전이. 1 → 3 건너뛰기 없음 |
| 콤보 성공 시 이벤트 관찰 | Attack1·Attack2에서 End 이벤트 없음, 중간 FSM Exit/Enter 없음 |
| 콤보 실패 시 이벤트 관찰 | 해당 End에서 종료 이벤트 1회, FSM 종료 1회 |
| 공중 공격 | 공중 Attack3 → 공중 Attack3_End → 지면 여부에 따라 Grounded 또는 Fall |
| 공중 공격 중 착지 | End 이벤트 수신 시 현재 지면 상태 기준으로 Grounded 복귀 |
| Attack3·각 End에서 추가 공격 입력 | 다음 콤보·자동 새 1타 없음. 종료 시 잔여 Trigger 초기화 |
| 콤보 분기 직전·직후 입력 | 직전의 유효 입력만 다음 타로 진행. End 진입 후 입력으로 콤보 재개 없음 |
| 공격·End 중 피격 | 기존 Hit 경로 유지. 잔여 Trigger·이전 End 이벤트로 잘못 복귀하지 않음 |
| 반복 공격 및 다른 행동 후 재공격 | End에서 고정되지 않고 새 공격은 올바른 지상/공중 Entry로 시작 |
| 타격 Event·Stamina 비교 | 기존 공격 클립의 타격 횟수·시각과 현재 데이터·소모 정책 유지 |
| 프레임·포즈 확인 | 공격 마지막 프레임, End 첫·마지막 프레임 및 Sprite 위치에 부자연스러운 누락 없음 |

End2·End3는 기존에 재생되지 않았으므로 연결 후 정상 공격 종료까지 시간이 늘어난다. 공격 직후 이동·점프 입력이 다시 가능한 시점도 End 이벤트에 맞춰 늦어진다. 별도 Delay나 Recovery FSM State를 추가하지 않고, 이번에 요청한 종료 모션 구간으로 처리한다.

## 9. 완료 기준과 보고

- 모든 공격 클립이 공격 시트만 사용하고 종료 이벤트를 포함하지 않는다.
- 각 종료 클립은 대응 End 시트와 종료 이벤트 1개만 사용한다.
- 지상 1·2타는 콤보 성공 시 End를 생략하고 다음 공격으로 진행한다.
- 콤보 실패·최종타·공중 공격은 대응 End 모션을 거쳐 FSM을 종료한다.
- 공중 공격의 종료 이벤트 제거로 AttackState가 고정되는 문제가 없다.
- 기존 FSM·Input·공격 데이터 코드를 변경하지 않는다.
- 완료 보고에는 변경한 클립·Animator State·전이 설정과 자동/수동 검증 결과를 구분해 기재한다.
- 수동 검증이 남으면 구현 완료와 동작 검증 완료를 구분해 보고한다.

## 10. 참고

- 현재 프로젝트의 코드·Animator·AnimationClip·Sprite meta가 구현 판단의 우선 기준.
- [Unity Animation transitions](https://docs.unity3d.com/6000.0/Documentation/Manual/class-Transition.html): Has Exit Time, 정규화 시간, Duration, Offset 및 전이 조건.

## 11. 적용 결과와 남은 검증

### 11.1 현재 적용 값

Unity Editor API로 대상 에셋만 생성·저장. 기존 에셋 GUID 유지. 실제 연결 포트 7802, 에셋 작업 시 Play 모드 해제 상태. `InGame.unity` 및 Attack 외부 Animator 구성·파라미터 유지.

| 클립 | 프레임 수 | 길이 | 타격 이벤트 | 종료 이벤트 |
| --- | --- | --- | --- | --- |
| Attack1 | 3 | 0.25초 | 0초 1개 유지 | 제거 |
| Attack2 | 5 | 약 0.4167초 | 0.25초 1개 유지 | 제거 |
| Attack3 | 12 | 1초 | 기존 시각 4개 유지 | 제거 |
| Attack1 End | 4 | 약 0.3333초 | 없음 | 0.25초 1개 |
| Attack2 End | 4 | 약 0.3333초 | 없음 | 기존 0.25초 1개 유지 |
| Attack3 End | 6 | 0.5초 | 없음 | 기존 약 0.4167초 1개 유지 |

- 지상 End State 3개, 공중 End State 1개 추가. 공중·지상 End3은 같은 클립 공유.
- 콤보·Attack → End 전이: Has Exit Time 활성화, Exit Time 1.0, Duration 0, Offset 0, Interruption Source None.
- Attack1·Attack2의 현재 전이 순서: 다음 공격 → 대응 End. 초기 적용 때 남겼던 직접 Exit는 후속 사용자 정리로 제거. 지상 3타·공중 공격의 직접 Exit도 제거.
- 각 End → Exit: `IsAttack == false`, Has Exit Time 해제, Duration 0, Offset 0.
- End2·End3 파일 자체는 수정 없이 재사용. 공격 동작 C#·Prefab·Scene·원본 Sprite 및 Import 설정 변경 없음. 별도로 사용자가 `AgentController.ChangeState()`의 `Debug.Log`를 주석 처리한 변경을 커밋에 포함.
- Unity 저장 과정에서 Attack1의 직렬화 버전·커브 메타데이터 자동 갱신. 신규 Controller 항목의 빈 문자열 공백은 Unity 직렬화 형식 그대로 유지하며 YAML을 직접 정리하지 않음.

### 11.2 자동 검증 결과

저장된 Controller를 독립 Preview Scene의 `AnimatorControllerPlayable`로 평가. 임시 검증 파일은 Assets 외부에서 메모리 컴파일 후 제거하고, 테스트 오브젝트·PlayableGraph·생성한 Preview Scene도 정리.

**검증 한계:** Edit 모드의 이 평가에서는 Animation Event가 자동 발행되지 않음. End 클립에 기록된 이벤트 시점에 기존 `AgentAnimationEventProxy.OnAnimationEnd()`를 수동 호출하고, 실제 `AttackEndTransition`의 수신·평가 후 FSM 종료에 해당하는 Animator 파라미터 변경을 모의 적용. 따라서 아래 통과 결과는 Animator 경로와 종료 규칙 연결 검증이며, 실제 Player 입력·지면 감지·Stamina·Play 모드 이벤트 발생 검증을 대신하지 않음.

| 시나리오 | 확인한 경로 | 결과 |
| --- | --- | --- |
| 지상 단발 | Attack1 → End1 → Grounded | 통과 |
| 지상 2타 후 종료 | Attack1 → Attack2 → End2 → Grounded | 통과 |
| 지상 전체 콤보 | Attack1 → Attack2 → Attack3 → End3 → Grounded | 통과 |
| 빠른 반복 Trigger | 1·2·3 순서 유지, End3 종료 | 통과 |
| End 중 추가 Trigger | End1 유지 후 종료, 새 1타 자동 진입 없음 | 통과 |
| 공중 공격·지상 복귀 | 공중 Attack3 → End3 → Grounded | 통과 |
| 공중 공격·낙하 복귀 | 공중 Attack3 → End3 → Hero_fall_start | 통과 |
| 공격 중 Hit 신호 | Attack1 → Hero_hit, End 신호 없음 | 통과 |

위 8개 평가는 초기 적용 상태인 직접 Attack → Exit 제거 전에 수행한 결과다. 이후 사용자가 직접 Exit 4개를 제거하고 문제없음을 확인했다. 제거 후 현재 에셋의 정상 End·상위 복귀·Any State 경로는 정적으로 확인했으며, 같은 자동 평가를 재실행했다고 기록하지 않는다.

정적 검증: 6개 클립의 Sprite 참조·수신 함수·이벤트 범위·12 FPS·비반복 설정 정상. 기존 타격 이벤트의 개수와 시각 보존. 기존 GUID·파라미터·Attack 외부 구성·활성 Scene 및 저장 상태 동일. 검사 종료 시 컴파일 실패·현재 Console 오류 없음, 후속 검사 구간에 새 경고·오류 없음.

### 11.3 사용자 수동 확인 필요

- [ ] 실제 Player 단발·2타·3타·빠른 연타 시 해당 End에서 종료 이벤트가 1회 발생하고 FSM과 Animator가 함께 복귀.
- [ ] 공중 공격·공격 도중 착지 후 Grounded/Fall 복귀 정상.
- [ ] 최종타·End 중 입력이 다음 새 1타로 남지 않음. 반복 공격·다른 행동 후 재공격 정상.
- [ ] Attack·End 중 피격 시 기존 Hit/Death 중단 경로와 후속 상태 정상.
- [ ] 공격 마지막 프레임 및 End 첫·마지막 포즈 확인. 신규 End1의 마지막 `_end_4` 표시와 종료 이벤트 시점 확인.
- [ ] 기존 타격·Stamina 동작 유지. End 모션 추가에 따른 이동·점프 재개 시점과 콤보 분기 타이밍 확인.
