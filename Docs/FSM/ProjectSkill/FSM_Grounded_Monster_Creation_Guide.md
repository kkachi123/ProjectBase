# 지상 일반 Monster FSM 제작 가이드

대기·순찰 → 인식 → 접근·공격 → 다음 행동을 공유하는 2D 지상 일반 Monster 제작용. Player 입력·BT·HFSM·Flying·Boss는 이 프로필에 끼워 넣지 않는다.

## 1. 작업 전 확인

- AGENTS.md와 Plan_Writing_Guide.md를 읽고 사용자 변경을 보존한다.
- 종 이름, Sprite·Idle/Move/Attack/Hit/Death 모션, Stat·Motor·행동·공격 수치, 장착 전략, 검증 씬을 확인한다.
- MonsterController·MonsterStateFactory·MonsterTargetHandler·MonsterAttackHandler와 전략 인터페이스를 읽는다.
- 연결 예시는 BasicMonster.prefab과 Goblin.prefab. 현재 구현은 [분리 후 구조](../Agent_Seperation/Monster_FSM_After.md) 참고.
- 현재 기준으로 컴파일·연결 확인까지 완료됐으며 새 FSM의 실제 Play 회귀는 수동 확인 대상이다.

## 2. 재사용 경계

- MonsterController는 AgentController 직접 상속. GroundedMotor2D·GroundedMovementHandler2D·GroundDetector 재사용.
- 공통 흐름이 같으면 Controller·State를 복제하지 않고 BasicMonster의 Variant로 제작.
- State: PatrolWait·Patrol·Chase·LostTargetWait·Return·Attack·Hit·Death. 시작 PatrolWait.
- Factory 순서: 공통 State → 전용 State → Death/Hit/행동 Rule. 실제 전이는 ITransitionRule만 결정.
- MonsterInput·MonsterBehaviorContext·Blackboard·BT는 추가하지 않음.
- ScenePlayerManager에 씬의 실제 Player를 할당. 대상은 시작·재활성화 시 주입하며 매 프레임 검색하지 않음.
- HomeX는 최초 Start 위치. 초기 위치와의 거리로 추적을 중단하지 않음.

## 3. 공격 장착·확장

Inspector의 MonsterAttackHandler에 역할·AnimatorAttackType·MonsterAttackDataSO를 등록한다. 같은 역할은 한 번 등록하고, 목록 순서로 선택하지 않는다.

| 역할 | 구현 |
|---|---|
| Melee | MonsterMeleeAttack: 정지·방향 정렬·모션·Frame 타격 |
| RearMelee | MonsterRearMeleeAttack: 시작 방향 기준 배후 접근 후 Melee 실행 |

- 조건 충족 전략 중 동일 확률 선택. 선택과 실행은 TryPrepareAttack / BeginAttack으로 분리.
- 새 실제 공격 동작은 IMonsterAttackStrategy와 역할을 추가하고 Handler.Initialize의 생성 분기에 연결.
- 계약: Data·Status, Begin, Execute(float), OnAttackFrame(int), OnAnimationEnd, End.
- 전략은 State를 전환하지 않고 Preparing/Attacking/Completed/Failed/TargetLost 결과만 기록.
- 지속 처리는 MonsterAttackState.OnExecute에서 호출. 전략 자체 Update·Coroutine 없음.
- End는 정상·피격·사망·Disable·Destroy에 필요한 이동·표시·구독 정리.
- 런타임 상태·타이머는 개체별 객체에 보관. 공유 SO 변경 금지.
- 정상 공격 End부터 다음 간격 적용. 실패·중단을 정상 완료로 기록하지 않음.

## 4. 에셋 연결

1. Monster 공용 BasicMonster.prefab의 종별 Variant를 Assets/Prefabs/Monster/{종}.prefab에 생성.
2. 종별 Animations·ScriptableObjects·Sprites는 Assets/Prefabs/Monster/{종}/ 아래 배치.
3. Stat·GroundedMotor·MonsterBehaviorDataSO와 역할별 MonsterAttackDataSO 할당. Basic 템플릿의 비어 있는 Stat·Motor·공격 목록을 완성해야 실제 실행 가능.
4. PlayerDetector의 반경·offset·obstacleMask와 Collider 설정. Root의 Detector는 하나만 유지.
5. Visual의 실제 Animator·SpriteRenderer·Proxy 하나를 MonsterAnimator와 Controller에 연결.
6. AnimatorOverride로 기존 Idle·Move·Attack·Hit·Death Clip 교체. 새 모션 분기는 기반 Animator도 확장.
7. 타격 Frame Event는 OnAnimationOnFrame(intParameter), 종료는 OnAnimationEnd. Monster 근접 공격의 기존 인자 0 유지.
8. 공격 모션 시작 전에는 IsAttack을 켜지 않음. 준비 이동은 Grounded/MoveSpeed 사용.
9. Agent Combat Editor는 MonsterAttackHandler의 역할별 SO를 편집. 타격·Gizmo·Scene Handle은 동일 범위 계산 사용.

공격 데이터 원본은 MonsterAttackDataSO다. 새 Monster에 StatData.attackDatas를 중복 연결하지 않는다. SO의 AttackDistance는 진입 거리이며 AttackData.size·offset은 실제 타격 범위다.

## 5. 검증·완료 보고

- Unity 컴파일·Missing Script·필수 참조·SO·목적지 Type 등록·Rule 순서·Event 확인.
- 사용자에게 조작 상황과 기대 결과를 먼저 안내하고 Play 중 State·Animator·Console 관찰.
- 순찰·인식·거리·일반/배후 공격·상실 대기·복귀·재인식·피격·사망·반복 진입 확인.
- Player 회귀와 이동·Preview·이벤트 구독 잔류 확인.
- 미저장 씬 자동 저장·재로드, 임시 Migration·Verification 스크립트, 자동 조작 금지.
- 실제 확인한 것과 수동 확인 대기를 구분해서 보고. 커밋·PDF 재생성은 별도 요청 범위.
