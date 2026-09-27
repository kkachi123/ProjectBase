# 확장 가능한 Agent Animator 리팩토링 계획

## 목표

Player 전용 Animator 파라미터와 Monster별 Animator 파라미터를 하나의 범용 enum에 계속 추가하지 않는다. 공통 기능은 `AgentAnimatorBase`에 유지하고, 캐릭터가 실제로 지원하는 행동 기능만 Animator·데이터 SO·FSM Factory에 등록하는 구조로 전환한다.

이 문서는 설계와 적용 순서만 정의한다. 체크 항목은 실제 작업과 검증이 끝난 뒤에만 완료 처리한다.

## 현재 구조와 문제

현재 `AgentAnimator`는 `StateType`, `AnimationIntType`, `AnimationFloatType` enum을 Dictionary 키로 사용해 Animator parameter hash를 관리한다.

- `StateType`은 FSM 상태의 의미와 Animator Bool parameter의 의미를 함께 갖고 있다.
- Player에 필요한 `Grounded`, `Jump`, `Fall`, `Attack`, `MoveSpeed`, `AttackType`이 공용 Animator에 추가되어 있다.
- Monster마다 지원 행동이 다르면, 사용하지 않는 parameter와 비어 있는 SO 필드가 늘어난다.
- `JumpState` 등 상태를 생성하지 않는 Monster도 공용 Animator API에는 해당 기능이 존재하게 된다.
- parameter 이름만 비어 있는 상태로 실행되면 Animator 표현만 누락되는 조용한 오류가 생길 수 있다.

## 설계 원칙

1. Animator가 지원하지 않는 행동의 FSM State/Transition은 Factory에서 생성하지 않는다.
2. 공용 상태는 구체 클래스가 아니라 필요한 기능 인터페이스에만 의존한다.
3. Animator parameter hash Dictionary는 기능 소유 Animator 내부에 둔다.
4. parameter 이름이 비어 있는 것으로 기능 유무를 판단하지 않는다.
5. Player, Monster 구분은 구현체와 SO 구성의 구분이고, 상태 재사용 여부는 기능 인터페이스가 결정한다.
6. Animator 전이는 FSM이 설정하는 명시적 parameter 값으로만 제어한다. Animator가 다른 행동 상태로 독자 전이하지 않는다.
7. 확장되는 Animator스크립트들은 각 Agent 폴더별 @Hub에 위치한다. (예: Player/@Hub/PlayerAnimator.cs, Monster/@Hub/GroundMonsterAnimator.cs)

## 목표 구조

```text
AgentAnimatorBase
├─ 공통 Animator 참조 / parameter hash 등록 / 안전한 parameter 설정
├─ 공통 표현 기능
│  ├─ IHitAnimation
│  └─ IDeathAnimation
│
├─ PlayerAnimator
│  ├─ IGroundedAnimation
│  ├─ IAirborneAnimation
│  └─ ICombatAnimation
│
├─ GroundMonsterAnimator
│  ├─ IGroundedAnimation
│  └─ ICombatAnimation
│
├─ JumpMonsterAnimator
│  ├─ IGroundedAnimation
│  ├─ IAirborneAnimation
│  └─ ICombatAnimation
│
└─ FlyingMonsterAnimator
   ├─ IFlyingAnimation
   └─ ICombatAnimation (필요한 경우)
```

### 기능 인터페이스 초안

```csharp
public interface IHitAnimation
{
    void SetHit(bool value);
    void SetDeath(bool value);
}

public interface IGroundedAnimation
{
    void SetGrounded(bool value);
    void SetMoveSpeed(float value);
}

public interface IAirborneAnimation
{
    void SetJump(bool value);
    void SetFall(bool value);
}

public interface ICombatAnimation
{
    void SetAttack(bool value);
    void SetAttackType(int attackType);
}
```

`IHitAnimation`은 추후 `IHitAnimation`과 `IDeathAnimation`으로 더 나눌 수 있다. 현재는 대다수 Agent가 피격과 사망을 같이 사용한다고 가정해 한 인터페이스로 시작한다.

## Animator parameter Dictionary 분리

공용 enum 하나를 확장하지 않고, 각 구현체가 자신의 parameter enum과 Dictionary를 갖는다.

```text
AgentAnimatorBase
└─ AgentAnimationBoolType
   ├─ Hit
   └─ Death

PlayerAnimator
├─ PlayerAnimationBoolType
│  ├─ Grounded
│  ├─ Jump
│  ├─ Fall
│  └─ Attack
├─ PlayerAnimationFloatType
│  └─ MoveSpeed
└─ PlayerAnimationIntType
   └─ AttackType

MonsterAnimator 계열
└─ 각 계열이 실제 사용하는 Bool / Float / Int enum만 정의
```

- 서로 다른 enum의 값이 모두 `0`부터 시작해도 Dictionary 인스턴스가 분리되어 있으므로 충돌하지 않는다.
- `AgentAnimatorBase`는 hash 등록, `Animator.SetBool/SetFloat/SetInteger` 호출 같은 저수준 동작만 제공한다.
- 상태는 enum이나 hash를 직접 다루지 않고, `SetGrounded`, `SetAttackType` 같은 의미 기반 API만 호출한다.

## Animation Data SO 구조

```text
AgentAnimationDataSO
├─ IsHitBool
└─ IsDeathBool

PlayerAnimationDataSO : AgentAnimationDataSO
├─ IsGroundedBool
├─ IsJumpBool
├─ IsFallBool
├─ IsAttackBool
├─ MoveSpeedFloat
└─ AttackTypeInt

GroundMonsterAnimationDataSO : AgentAnimationDataSO
├─ IsGroundedBool
├─ MoveSpeedFloat
├─ IsAttackBool             (공격 가능한 경우)
└─ AttackTypeInt            (여러 공격 모션이 있는 경우)

FlyingMonsterAnimationDataSO : AgentAnimationDataSO
├─ FlySpeedFloat
├─ IsChaseBool              (필요한 경우)
└─ IsAttackBool             (필요한 경우)
```

- `AgentAnimationDataSO`에는 모든 Agent가 실제로 공유하는 parameter만 둔다.
- Player/Monster 전용 SO에는 해당 구현체가 등록하는 parameter만 둔다.
- parameter 이름의 기본값은 제공하되, 빈 문자열을 기능 비활성화 수단으로 사용하지 않는다.

## FSM State 및 Factory 구성 원칙

| FSM State | 필요한 Animator 기능 | Factory 등록 조건 |
|---|---|---|
| `HitState`, `DeathState` | `IHitAnimation` | 피격/사망을 표현하는 Agent |
| `GroundedState` | `IGroundedAnimation` | 지상 이동형 Agent |
| `JumpState`, `FallState` | `IGroundedAnimation` + `IAirborneAnimation` | 점프 가능한 Agent |
| `AttackState` | `ICombatAnimation` | 공격 가능한 Agent |
| `FlyState` (향후) | `IFlyingAnimation` | 비행형 Agent |

`PlayerStateFactory`와 `MonsterStateFactory`는 공통 Factory 기반 클래스를 사용하되, 각 캐릭터가 제공하는 Animator capability에 따라 State와 Transition을 추가한다.

```text
PlayerStateFactory
└─ Grounded + Jump + Fall + Attack + Hit + Death

GroundMonsterStateFactory
└─ Grounded + Attack + Hit + Death

JumpMonsterStateFactory
└─ Grounded + Jump + Fall + Attack + Hit + Death

FlyingMonsterStateFactory
└─ Fly + Attack + Hit + Death
```

## 적용 단계

### 1. 현황 확정

- [ ] Player Animator parameter와 각 State의 호출 관계를 표로 작성한다.
- [ ] 현재 Monster Controller별 지원 행동을 분류한다: 지상 이동, 점프/낙하, 공격, 비행, 피격, 사망.
- [ ] Monster Animator Controller별 실제 parameter와 전이를 수집한다.
- [ ] 재사용할 공통 상태와 캐릭터 전용 상태를 확정한다.

완료 기준: 각 Controller가 지원하는 FSM State와 Animator capability가 표로 대응된다.

### 2. 공통 기반 도입

- [x] 기존 `AgentAnimator`의 공통 Animator 참조, hash 등록, 안전한 set 동작을 `AgentAnimatorBase`로 이동한다.
- [x] `IHitAnimation`과 필요 시 `IDeathAnimation`등 필요한 인터페이스를 'IAgentAnimator'에 정의한다.
- [x] 공통 Bool parameter enum과 공통 `AgentAnimationDataSO`를 만든다.
- [x] 기존 `HitState`, `DeathState`를 공통 capability 인터페이스에 의존하도록 변경한다.

완료 기준: 피격/사망만 필요한 Agent가 Player 관련 parameter 없이 Animator를 초기화할 수 있다.

### 3. Player Animator 분리

- [x] `PlayerAnimator`와 Player 전용 Bool/Float/Int enum을 만든다.
- [x] `PlayerAnimationDataSO`를 만들고 기존 Player animation data를 마이그레이션한다.
- [x] `IAgentAnimator.cs`에 `IGroundedAnimation`, `IAirborneAnimation`, `ICombatAnimation`을 정의하고 `PlayerAnimator`가 구현한다.
- [x] `GroundedState`, `JumpState`, `FallState`, `AttackState`가 필요한 capability 인터페이스를 받도록 생성자와 Factory data를 변경한다.
- [x] `PlayerController`의 serialized Animator 참조를 `PlayerAnimator`로 교체한다.
- [x] `Hero_Anim.controller`의 parameter 이름과 Player SO 등록값을 검증한다.

완료 기준: Player는 기존과 동일한 Grounded Blend Tree, 점프/낙하, 공격, 피격, 사망 흐름을 유지한다.

### 4. Monster 계열 분리

- [ ] Monster를 행동 조합별 계열로 나눈다: 일반 지상형, 점프형, 비행형 등.
- [ ] 첫 번째 대상 Monster에 `GroundMonsterAnimator` 또는 `JumpMonsterAnimator`를 적용한다.
- [ ] 전용 `MonsterAnimationDataSO`를 만든다.
- [ ] 해당 MonsterStateFactory가 지원 행동에 맞는 State/Transition만 만들게 한다.
- [ ] 다른 Monster Controller에 같은 계열을 순차 적용한다.

완료 기준: 점프하지 않는 Monster에는 `JumpState`, `FallState`, Jump/Fall parameter가 존재하지 않는다.

### 5. Animator Controller 전이 정리

- [ ] 각 Animator Controller의 모든 parameter가 대응 Animator 구현체와 SO에 존재하는지 확인한다.
- [ ] 상태 종료를 위한 불명확한 generic Bool 전이를 제거한다.
- [ ] 상태 진입 API가 설정하는 명시적 parameter를 전이 조건으로 사용한다.
- [ ] FSM이 관리하지 않는 행동 간 직접 전이(예: `Fall → Jump`)를 제거하거나 명확한 전용 FSM 규칙으로 이관한다.

완료 기준: Animator 상태 변화와 FSM 현재 상태가 대응하며, Animator가 독자적으로 행동 상태를 바꾸지 않는다.

### 6. 이전 구조 제거 및 문서화

- [ ] 기존 `AgentAnimator`와 공용 `StateType` 기반 parameter Dictionary 사용처를 모두 제거한다.
- [ ] 더 이상 사용하지 않는 AnimationData 필드와 enum을 제거한다.
- [ ] prefab serialized reference와 ScriptableObject reference를 확인한다.
- [ ] Controller/Animator capability 표와 확장 가이드를 `Docs/FSM`에 갱신한다.

완료 기준: 신규 캐릭터 추가 시 필요한 Animator capability, SO, Factory만 선택해 구성할 수 있다.

## 검증 체크리스트

- [x] C# 컴파일 오류가 없다.
- [ ] Player: Idle/Move Blend Tree가 `MoveSpeed`로 정상 보간된다.
- [ ] Player: Grounded → Jump → Fall → Grounded가 FSM과 Animator에서 동일한 순서로 진행된다.
- [ ] Player: Attack/Hit 종료 뒤 `IsGrounded` 기반으로 Grounded 상태에 복귀한다.
- [ ] Player: Fall 중 Animator가 독자적으로 Jump 모션으로 전이하지 않는다.
- [ ] 지상형 Monster: 없는 parameter 경고 없이 Idle/Move/Attack/Hit/Death가 동작한다.
- [ ] 점프형 Monster: Jump/Fall capability가 있는 경우에만 공중 상태가 생성된다.
- [ ] 비행형 Monster: Grounded 관련 parameter 또는 State 없이 비행 표현이 동작한다.
- [ ] Inspector에서 모든 Controller의 Animator 및 AnimationDataSO 참조가 유효하다.

## 리스크와 대응

| 리스크 | 대응 |
|---|---|
| 한 번에 모든 Animator를 분리해 prefab 참조가 끊김 | Player를 첫 마이그레이션 대상으로 삼고, Monster는 계열별로 단계 적용한다. |
| 공용 상태가 특정 Animator 구현체에 다시 결합됨 | State 생성자는 구체 Animator 대신 capability 인터페이스를 받도록 유지한다. |
| 기존 Animator parameter와 SO 이름이 불일치 | 각 단계에서 Animator parameter 목록과 SO 등록값을 Editor로 비교 검증한다. |
| 기능 없는 Monster에도 State가 생성됨 | Factory가 capability/행동 구성에 따라 State를 조건부 등록하도록 한다. |
| 빈 parameter 문자열이 조용히 실패함 | 기능 유무는 SO 문자열이 아니라 구현체와 Factory 구성으로 결정한다. |

## 결정이 필요한 항목

- [ ] `Hit`와 `Death`를 하나의 `IHitAnimation`에 둘지, `IHitAnimation` / `IDeathAnimation`으로 분리할지 결정한다.
- [ ] 공격 종류 선택(`AttackType`)을 모든 공격형 Agent의 공통 capability로 볼지, Player 전용으로 둘지 결정한다.
- [ ] Monster 계열을 Animator 클래스 단위로 나눌지, 하나의 `MonsterAnimator`에 capability module을 조합할지 결정한다.
- [ ] 기존 `AnimationDataSO`를 상속 구조로 마이그레이션할지, 새 SO를 만든 뒤 prefab 참조를 교체할지 결정한다.

## 권장 시작점

Player의 현재 기능이 가장 명확하므로 **2단계 → 3단계**를 먼저 완료한다. 이후 현재 사용 중인 Monster 중 가장 단순한 지상 이동/공격형 하나를 선택해 4단계의 기준 구현체로 삼는다. 이 기준 구현이 안정된 뒤 점프형·비행형 Monster로 확장한다.
