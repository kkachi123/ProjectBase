# PlayerInput UniRx 정리 계획

## 목표

- `PlayerInput`에서 **구독되지 않고 FSM이 매 프레임 읽기만 하는 입력값**은 UniRx `ReactiveProperty` 대신 일반 상태값 또는 소비형 요청으로 변경한다.
- 입력의 의미를 명확히 구분한다.
  - 홀드 상태: 현재 버튼을 누르고 있는가
  - 단발 요청: 한 번의 입력을 한 번만 처리해야 하는가
  - 이벤트: 상태를 저장하지 않고 즉시 처리해야 하는가

## 변경 전 점검

- [x] `PlayerInput` 외부에서 `JumpPressed`, `AttackPressed`, `InteractPressed`를 구독하는 코드를 다시 검색한다.
- [x] 공격 버튼 홀드 시 반복 공격이 의도된 동작인지 확인한다.
- [ ] 공중에서 누른 점프가 착지 후 자동 점프로 이어지는 점프 버퍼 동작이 필요한지 결정한다.

## 변수별 전환 대상

### `Horizontal`

현재 일반 `Vector2`로 관리되고 있다.

- [x] UniRx 전환 대상이 아니다.
- [ ] `GetMovementInput()`이 입력 차단 상태에서 `Vector2.zero`를 반환하는지 유지한다.

### `_jumpPressed`

현재 `ReactiveProperty<bool>`이지만 FSM이 `.Value`만 읽는다. 홀드 상태를 전이 조건에 직접 사용하면 점프 키를 누른 상태에서 `JumpState`가 재진입할 수 있다.

- [x] `ReactiveProperty<bool>`을 제거한다.
- [x] `bool IsJumpHeld`를 추가한다.
- [x] `bool _jumpRequested`를 추가한다.
- [x] `Jump.performed`에서 `IsJumpHeld = true`, `_jumpRequested = true`를 설정한다.
- [x] `Jump.canceled`에서 `IsJumpHeld = false`를 설정한다.
- [x] `TryConsumeJumpRequest()`를 추가해 요청을 한 번만 반환하고 즉시 초기화한다.
- [x] 입력 차단 시 홀드 상태와 대기 요청을 모두 초기화한다.
- [x] `IAgentJumpInput`을 `TryConsumeJumpRequest()` 및 필요 시 `IsJumpHeld` 중심으로 변경한다.
- [x] `JumpTransition`은 요청을 먼저 소비한 뒤 Grounded 조건을 확인한다.

완료 기준:

- [ ] 점프 키를 계속 눌러도 점프 impulse가 중첩되지 않는다.
- [ ] 공중에서 점프 키를 누른 뒤 착지해도 자동 점프하지 않는다. (점프 버퍼를 의도적으로 만들지 않는 경우)

### `_attackPressed`

현재 Player는 공격 버튼을 누른 동안 값을 유지하고, FSM이 `.Value`를 읽는다. 이 동작은 공격 홀드 반복에 사용할 수 있으나 ReactiveProperty의 구독 기능은 사용하지 않는다.

- [x] `ReactiveProperty<int>`을 제거한다.
- [x] `int HeldAttackType`을 추가한다. 기본값은 `0`이다.
- [x] Attack1/Attack2 `performed`에서 해당 공격 타입을 설정한다.
- [x] `canceled`에서 `HeldAttackType = 0`으로 초기화한다.
- [x] `IAgentCombatInput`을 `int HeldAttackType { get; }` 형태로 변경한다.
- [x] `AttackTransition`과 `AttackState`의 `.Value` 참조를 `HeldAttackType`으로 변경한다.
- [x] 스태미나 부족 시 `AttackTransition`이 AttackState 진입을 막는지 유지한다.

완료 기준:

- [ ] 공격 버튼 홀드 시 의도한 반복 공격만 실행된다.
- [ ] 공격 버튼을 놓으면 다음 공격 진입이 중단된다.
- [ ] 스태미나가 부족할 때 FSM State와 Animator 상태가 불일치하지 않는다.

### `_interactPressed`

`PlayerInteractionHandler`가 현재 값을 구독하고 있으므로, 다른 입력값과 달리 ReactiveProperty 사용은 동작상 가능하다. 그러나 상호작용은 상태가 아닌 단발 명령이므로 C# 이벤트가 더 명확하다.

- [x] `ReactiveProperty<bool>`을 제거한다.
- [x] `event Action OnInteractRequested`를 `PlayerInput`에 추가한다.
- [x] `Interact.performed`에서 `OnInteractRequested?.Invoke()`를 호출한다.
- [x] `PlayerInteractionHandler`에서 이벤트를 구독한다.
- [x] `OnDestroy` 또는 `OnDisable`에서 이벤트 구독을 해제한다.
- [ ] 상호작용 처리 중 입력 차단 규칙을 확인한다.

완료 기준:

- [ ] 상호작용 키 1회 입력당 `TryInteract()`가 최대 한 번 호출된다.
- [ ] 파괴·비활성화된 `PlayerInteractionHandler`가 이벤트를 계속 받지 않는다.

## 권장 적용 순서

1. [x] 인터페이스 변경안을 적용한다.
2. [x] `_jumpPressed`를 소비형 점프 요청으로 전환한다.
3. [x] `_attackPressed`를 `HeldAttackType` 일반 프로퍼티로 전환한다.
4. [x] `_interactPressed`를 `OnInteractRequested` 이벤트로 전환한다.
5. [x] `PlayerInput`의 불필요한 `using UniRx`를 제거한다.
6. [x] Unity Console 컴파일 오류를 확인한다.
7. [ ] 입력별 Play Mode 검증을 수행한다.

## 검증 체크리스트

- [ ] 정지 상태에서 점프
- [ ] 이동 상태에서 점프
- [ ] 점프 키 홀드 중 플랫폼 가장자리 통과
- [ ] 공중에서 점프 키 입력 후 착지
- [ ] Attack1 홀드
- [ ] Attack2 홀드
- [ ] 스태미나 부족 상태에서 공격 입력
- [ ] 상호작용 대상이 있을 때 상호작용
- [ ] 상호작용 대상이 없을 때 상호작용
- [ ] 입력 차단 상태 진입·해제

## UniRx 유지 대상

아래 값은 여러 시스템이 변화 자체를 구독할 가능성이 있으므로 UniRx 유지가 적합하다.

- `Health.CurrentHealth`
- `Health.IsDead`
- `Stamina.CurrentStamina`
- 체력·스태미나 UI 표시 값
