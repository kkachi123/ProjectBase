# Attack Sub State Machine Entry 분기 리팩토링 계획

## 목표

`Hero_Anim.controller`의 기존 `Attack` Sub State Machine은 유지한다. 다만 `Grounded`, `Fall` 등 외부 상태가 `Hero_combo1`, `Hero_combo2`, `Hero_combo3`에 직접 진입하는 구조를 제거하고, 외부 상태는 `Attack` Sub State Machine으로만 진입하도록 단순화한다.

실제 Combo 선택은 `Attack` Sub State Machine의 `Entry` 전이가 `AttackType`을 판별하여 담당한다.

```text
변경 전
Grounded / Fall / Jump
    └─ IsAttack + AttackType 조건
        └─ Hero_combo1 / Hero_combo2 / Hero_combo3

변경 후
Grounded / Fall / Jump
    └─ IsAttack
        └─ Attack Sub State Machine
            └─ Entry + AttackType 조건
                └─ Hero_combo1 / Hero_combo2 / Hero_combo3
```

## 유지할 책임

| 요소 | 유지할 책임 |
| --- | --- |
| `AttackState` | 공격 시작 가능 여부를 확인하고, Animator에 공격 종류와 시작 신호를 전달한다. |
| `AttackType` | 이번 AttackState에서 재생할 이산적인 공격 종류를 선택한다. |
| `Attack` Sub State Machine | 선택된 AttackType에 해당하는 Combo Animation을 재생한다. |
| `AttackEndTransition` | Animation Event 수신 후 FSM을 `GroundedState`로 복귀시킨다. |
| `Grounded` Blend Tree | 이동 속도에 따른 Idle/Move 보간만 담당한다. 공격 종류 선택에는 사용하지 않는다. |

## 적용 순서

- [ ] 1. 현재 Animator 구조 백업 및 전이 목록 기록
  - `Grounded`, `Hero_jump`, `Hero_fall_start`, `Hero_fall_loop`에서 각 Combo State로 향하는 `IsAttack + AttackType` 전이를 확인한다.
  - `Attack` Sub State Machine 내부의 Combo State, 종료 Animation Event, `IsGrounded` 복귀 전이를 기록한다.
  - 공격 중 피격·사망 전이가 Any State에서 계속 동작하는지 확인한다.

- [ ] 2. Animator 파라미터와 코드 호출 순서 확인
  - `PlayerAnimationDataSO`의 `IsAttackBool`, `AttackTypeInt`가 각각 `IsAttack`, `AttackType`을 참조하는지 확인한다.
  - `AttackState.OnEnter()`에서 `SetAttackType(currentAttackType)`을 먼저 호출하고, 이어서 `SetAttack(true)`를 호출하도록 정렬한다.
  - 이유: Animator가 `IsAttack`을 평가하는 첫 프레임 이전에 Entry 전이가 사용할 AttackType을 확정한다.

- [ ] 3. Attack Sub State Machine에 Entry 분기 구성
  - `Attack`의 기본 State에 의존하지 않도록 Entry Transition을 사용한다.
  - `AttackType == 1` → `Hero_combo1` Entry Transition을 추가한다.
  - `AttackType == 2` → `Hero_combo2` Entry Transition을 추가한다.
  - `AttackType == 3` → `Hero_combo3` Entry Transition을 추가한다.
  - 각 전이는 `IsAttack == true` 조건도 함께 사용해, 공격 종료 중 잘못 재진입하지 않도록 한다.
  - Entry 전이 우선순위가 명확히 `1 → 2 → 3`으로 보이도록 정렬한다.

- [ ] 4. 외부 상태의 개별 Combo 직접 전이 제거
  - `Grounded`, `Hero_jump`, `Hero_fall_start`, `Hero_fall_loop`에서 Combo State를 목적지로 지정한 전이를 제거한다.
  - 동일 출발 상태마다 `IsAttack == true` 조건만 가진 전이를 하나씩 추가하고 목적지를 `Attack` Sub State Machine으로 지정한다.
  - 공중 공격은 PlayerController에서 AttackType을 `3`으로 강제하는 기존 규칙을 유지한다.
  - 전이 Duration은 현재 즉시 전이 의도를 유지하도록 `0`으로 설정한다.

- [ ] 5. 공격 종료 및 FSM 동기화 점검
  - 각 Combo 종료 Animation Event가 `AgentAnimationEventProxy`를 통해 `AttackEndTransition`까지 전달되는지 확인한다.
  - 이벤트 수신 시 FSM이 `AttackState → GroundedState`로 전이하는지 확인한다.
  - `AttackState.Exit()`에서 `IsAttack = false`, `AttackType = 0`, CombatHandler 공격 타입 초기화가 수행되는지 확인한다.
  - `GroundedState.OnEnter()`가 `IsGrounded = true`와 MoveSpeed 초기화를 수행하는지 확인한다.

- [ ] 6. 컴파일 및 Animator 검증
  - C# 컴파일 오류와 Animator Controller의 Missing Motion/Transition 경고를 확인한다.
  - Animator Parameters에 `IsAttack`(Bool), `AttackType`(Int)가 유지되는지 확인한다.
  - Attack Sub State Machine의 Entry에 AttackType별 전이가 정확히 세 개인지 확인한다.

- [ ] 7. 수동 테스트
  - 지상 Attack 1, 2를 각각 입력해 정확한 Combo Animation으로 한 번에 진입하는지 확인한다.
  - Falling 중 공격 입력 시 `Hero_combo3`으로만 진입하는지 확인한다.
  - 공격 종료 후 State와 Animator가 모두 Grounded로 복귀하는지 확인한다.
  - 공격 버튼 유지 시 `Grounded → Attack → Grounded → Attack` 반복에서 Animator와 FSM이 불일치하지 않는지 확인한다.
  - 공격 도중 피격·사망 시 Attack 종료 이벤트 구독이 남지 않는지 확인한다.

## 완료 기준

- 외부 상태는 `Attack` Sub State Machine만 목적지로 사용한다.
- Combo Animation 선택 규칙은 Attack Sub State Machine의 Entry 전이에만 존재한다.
- `AttackType`은 Blend Tree 파라미터로 변환하지 않고 Int 파라미터로 유지한다.
- 지상/공중 공격, 종료, 반복 입력에서 FSM State와 Animator State가 동일한 흐름을 보인다.

## 제외 범위

- `AttackType` 기반 Blend Tree 도입
- Combo 간 자연스러운 보간 또는 공격 모션 블렌딩
- Monster Animator와 공용 Attack Animator 구조 변경
- 공격 판정, 스태미나 소모, Combo 규칙 자체의 재설계
