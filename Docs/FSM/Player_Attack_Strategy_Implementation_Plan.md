# Player 공격 전략·공용 근접 타격 Handler 분리 계획

## 목표

Player의 공격 설정·선택·시동을 전용 Handler로 모으고 지상 콤보와 공중 공격을 분리한다. 공용 타격 Handler는 전달받은 데이터로만 피해를 적용하며, Monster의 기존 StatData·Behavior·FSM을 유지한다.

## 구성과 실행 계약

| 구성 | 책임 |
| --- | --- |
| `PlayerAttackDataSO` | 피해·범위·Stamina 비용 보관 |
| `PlayerAttackHandler : IAttackStarter` | 역할별 SO 목록·모션 선택값, 개체별 전략 생성·선택, 시작 비용·실행 수명 관리 |
| `IPlayerAttackStrategy` | `StartData`, `Begin()`, `OnAttackFrame(int)`, `End()` |
| `PlayerGroundComboAttack` | 수평 정지, 공격 중 콤보 입력 구독·Trigger 정리, 단계별 타격 |
| `PlayerAirAttack` | 이동·중력 유지, 모든 타격 Event에 단일 공중 데이터 적용 |
| `AgentMeleeHitHandler2D` | 수평 사각형 범위 계산, 피해·넉백, 표시 전용 Preview |
| `AttackState` 및 파생 State | 공통 Bool·모션 초기화, 전용 공격의 진입·타격·종료 연결 |

- 역할은 `GroundCombo`, `AirAttack`. 설정 목록의 순서 대신 Dictionary 역할 키로 선택한다.
- `AttackDefinition`에 Role·AnimatorAttackType·SO 배열을 보관한다. 모션값은 지상 1·공중 3으로 설정하고 콤보 단계별 SO에는 저장하지 않는다.
- `TryStartAttack()`은 지면·시작 SO의 Stamina를 확인하고 성공 시 전략·모션값을 함께 선택한다. 실행·구독·소비는 State 진입 후 수행한다.
- 선택 전략·모션값은 Exit까지 고정하고 Begin에서 지면을 다시 판단하지 않는다. 시작 비용은 진입당 한 번만 소비한다.
- 콤보 순서·입력 수락은 기존 Animator가 결정한다. 별도 버퍼·카운터·Tick·Phase·Wrapper는 추가하지 않는다.
- 지상 Event 인자 1·2·3은 각 SO를 사용한다. 0·음수·배열 범위 초과는 무시하며, 공중 전략은 인자를 무시한다.
- Exit에서 구독·미소비 Trigger·Preview·선택 결과를 정리한다. 피격·사망·파괴도 동일한 Exit 경로를 사용한다.
- Monster는 기존 첫 공격 데이터·모션 선택값 1·수평 정지를 유지한다.
- `typeof(AttackState)` 키, Transition·End Event·Animator 전이·Clip 시점은 유지한다.

## 핵심 단계

- [x] Editor API로 기존 Prefab·Variant·Scene의 StatData·대상 Layer·Override·Clip Event 확보.
- [x] Player SO·Handler·전략 및 공용 타격·State·정수 Event 전달 코드 연결.
- [x] Combat Editor의 Player SO/Monster StatData 편집 경로와 공용 범위 계산 연결.
- [x] 지상 1·2·3타와 공중 SO 생성·연결 및 기존 값 비교. 지상·공중 3번 데이터는 별도 SO로 생성.
- [x] 지상 1·2·3타의 타격 Event 인자를 각각 1·2·3으로 설정. Monster 인자 0 유지.
- [x] 값·참조 확인 후 Player Stats의 공격 목록만 비우기. Monster 목록 유지.
- [x] SO의 모션 선택값을 AttackDefinition으로 이전. 지상 1·공중 3, SO 수치·GUID·목록 순서·Prefab Override 보존.
- [x] 컴파일·GUID·참조·Editor·Gizmo 연결 확인. 실제 표시·편집 조작은 수동 확인 대기.
- [ ] 사용자 수동 Play 검증.

타격 스크립트 GUID와 `_targetLayer` 직렬화 이름을 유지한다. Controller의 기존 `_combatHandler` 필드 이름도 참조·Override 보존을 위해 유지한다. Unity 에셋은 Editor API로만 변경하며 미저장 Scene을 자동 저장·재로드하지 않는다. 임시 Migration·Verification 스크립트는 작성하지 않는다.

## 검증 기준

- 역할 목록 순서와 무관한 선택, 개체별 전략 상태 독립.
- Stamina 부족 시 진입 차단, 성공한 진입에서 지상 첫 SO·공중 SO 비용 한 번 소비.
- 지상 각 단계 피해·범위 및 3타의 여러 타격 Event, 공중 다단 타격·이동·중력 유지.
- 공격 중 착지해도 전략 유지, 종료·중단·반복 진입 후 구독·Trigger·표시 잔류 없음.
- Gizmo·Combat Editor·실제 타격 범위 일치, Undo·Scene Handle·Animation Preview 유지.
- Missing Script·누락 참조·삭제 API·컴파일 오류 없음.
- Player 이동·Jump·Dash 및 Goblin의 피해·공격 간격·Behavior·상태 전이 유지.

Play 확인은 사용자 조작 전에 상황·조작 방법·기대 결과를 안내한 후 상태·Console을 읽어 진행한다. Flying·Boss·범용 공격 프레임워크는 구현하지 않는다.

## 확인 결과와 수동 조작

- SO 4개의 값이 기존 각 공격 데이터와 정확히 일치. 지상 3타와 공중 SO는 별도 GUID.
- Player·Variant·BasicMonster·Goblin의 대상 Layer·Prefab Override 보존, Missing Script 0개.
- InGame·SampleScene의 Player에 지상 SO 3개·공중 SO 1개가 상속됨. Scene 저장·재로드 없음.
- Player Prefab·Variant·열린 InGame의 AttackDefinition 모션값 지상 1·공중 3 확인. SO에서 모션 필드 제거 후 기존 피해·범위·비용·GUID와 Prefab Override 보존 확인.
- 컴파일 성공. 기존 InGameUI/Managers 오류는 이번 범위에서 변경하지 않음.

| 조작 | 기대 결과 |
| --- | --- |
| 지상에서 A 한 번, 이후 A 연타 | 단발 종료 및 1→2→3 콤보. 각 단계 SO의 범위·피해 사용 |
| ←/→ 이동 중 Space 후 A | 공중 공격 중 이동·중력 유지, 여러 타격 Event 처리 |
| 공중 공격 중 착지 | 공격 종료 전까지 공중 전략 유지 |
| 공격 중 피격·사망 후 다시 공격 | 구독·ComboTrigger·범위 표시 잔류 없음 |
| Player/Goblin 타격 Handler Inspector에서 Combat Editor 열기 | Player 역할별 SO / Monster StatData 편집, Undo·Scene Handle·Animation Preview 유지 |
| Player 이동·점프·Left Shift Dash, Goblin과 교전 | 기존 동작·공격 요청 간격·FSM 전이 유지 |

Stamina 부족·진입당 1회 소비와 3타 다중 타격은 사용자 조작 중 런타임 State·Stamina·Console을 읽어 확인한다. 강제 입력·피해·상태 전환 코드는 사용하지 않는다.
