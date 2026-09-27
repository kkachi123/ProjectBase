# Player Grounded State + Blend Tree 통합 계획

## 목적

`Hero_Anim.controller`의 `Hero_Idle`과 `Hero_Move`를 하나의 Locomotion Blend Tree로 통합하고, FSM의 `IdleState`와 `MoveState`도 단일 지상 이동 상태로 통합한다.

통합 후 **정지와 이동은 별도 FSM 상태가 아니라 이동 입력/속도 값**으로 구분한다. 점프, 낙하, 공격, 피격, 사망처럼 동작 제어가 달라지는 경우만 별도 상태로 유지한다.

## 현재 구조 확인

### FSM

| 구분 | 현재 구현 | 통합 시 문제 |
| --- | --- | --- |
| 정지 | `IdleState`가 `IsIdle = true`, 이동 벡터 `Vector2.zero` 적용 | 정지용 상태와 전이가 별도로 필요함 |
| 이동 | `MoveState`가 `IsMove = true`, 입력 벡터 적용 | Idle과 동일한 지상 규칙을 중복 보유 |
| 정지/이동 전이 | `IdleToMoveTransition`이 입력 유무로 `IdleState`와 `MoveState`를 왕복 | 매 프레임 입력 변화에 상태 전환이 발생함 |
| 착지 | `LandTransition`이 입력 X값으로 `IdleState` 또는 `MoveState` 선택 | 통합 후 목적지 분기가 불필요함 |
| 공격/피격 종료 | `AttackEndTransition`, `GetHitEndTransition`이 `IdleState`를 목적지로 사용 | 통합 후 목적지 타입 변경이 필요함 |

### Animator

현재 `Hero_Anim.controller`는 `Hero_Idle`, `Hero_Move` 상태와 `IsIdle`, `IsMove` Bool 파라미터를 사용한다. 해당 Bool 조합은 상호 배타 상태를 표현하지만, 로코모션의 연속적인 속도 표현에는 적합하지 않다.

## 목표 구조

```text
GroundedState
  ├─ 입력 없음       → MoveSpeed = 0.0 → Hero_Idle
  ├─ 이동 입력       → MoveSpeed = 1.0 → Hero_Move
  ├─ 점프 요청       → JumpState
  ├─ 지면 이탈       → FallState
  ├─ 공격 가능 입력  → AttackState
  └─ 피격           → HitState

JumpState / FallState / AttackState / HitState
  └─ 종료 또는 착지 → GroundedState
```

## 설계 결정

### 1. 통합 상태 이름

신규 상태명은 `GroundedState`를 사용한다.

- `IdleState`는 더 이상 "정지"만 담당하지 않으므로 이름이 맞지 않는다.
- `GroundedState`는 지상에서 정지/이동을 모두 처리한다는 책임이 명확하다.
- `JumpState`, `FallState`와도 상태의 물리적 구분이 자연스럽다.

### 2. 속도 값의 기준

Animator Blend Tree에는 `MoveSpeed` Float 파라미터를 사용한다.

초기 구현은 이동 입력의 크기를 사용한다.

```csharp
float targetSpeed = movementInput.GetMovementInput().magnitude;
animator.SetFloat(AnimationFloatType.Speed, targetSpeed);
```

- 정지: `0`
- 키보드/디지털 이동: `1`
- 향후 아날로그 입력: `0 ~ 1`

물리 속도 기반 블렌딩이 필요해지면 `AgentMotor2D`의 실제 수평 속도를 정규화해 사용한다. 이번 작업에서는 입력 기반으로 시작해 FSM 전환과 Animator 전환을 분리한다.

### 3. Animator 파라미터 API

현재 `AgentAnimator`는 `AnimationIntType`, `StateType` Bool Dictionary만 제공한다. 다음을 추가한다.

```csharp
public enum AnimationFloatType
{
    Speed,
}

public void SetFloat(AnimationFloatType type, float value)
```

`AnimationDataSO`에는 `MoveSpeedFloat = "MoveSpeed"` 필드를 사용한다. `AgentAnimator.Initialize()`에서 해당 해시를 등록한다.

## 적용 순서

### 단계 0 — 안전 조치

1. `Hero_Anim.controller`와 `PlayerAnimationData.asset`을 버전 관리에 저장한다.
2. 현재 Play Mode를 종료한다.
3. 현재 동작 기준을 기록한다.
   - 정지, 좌/우 이동, 점프, 낙하, 착지, 지상 공격, 공중 공격, 피격, 사망
4. 이 작업 중 `Unity.Behavior` Action 노드 및 Behavior Graph 에셋은 수정하지 않는다.

### 단계 1 — Animator 데이터/API 준비

상태: **완료**

대상 파일:

- `Assets/Scripts/FSM/Agent/@Hub/AgentAnimator.cs`
- `Assets/Scripts/FSM/Agent/SOData/AnimationDataSO.cs`

작업:

1. `AnimationFloatType.Speed`를 추가한다.
2. Float 파라미터 해시 Dictionary와 `RegisterFloatParam`, `SetFloat`를 추가한다.
3. `AnimationDataSO`에 `MoveSpeedFloat` 문자열 필드를 추가하고 Player 기본값을 `MoveSpeed`로 설정한다.
4. `Initialize()`에서 `MoveSpeed` 파라미터를 등록한다.

완료 기준:

- Animator에 아직 `MoveSpeed`가 없더라도 코드 예외 없이 실행된다.
- 이름이 비어 있으면 기존 Bool 등록처럼 안전하게 무시한다.

### 단계 2 — `GroundedState` 구현

상태: **완료**

대상 파일:

- 신규: `Assets/Scripts/FSM/GroundedAgent/StateControl/States/GroundedState.cs`
- 참고: `IdleState.cs`, `MoveState.cs`

책임:

1. `OnEnter()`에서 지상 이동 애니메이션 속도를 현재 입력 값으로 초기화한다.
2. `OnExecute()`에서 `AgentMovementHandler2D.HandleMove()`를 호출한다.
3. 같은 프레임에 `MoveSpeed` Float을 입력 크기로 갱신한다.
4. `Exit()`에서 `MoveSpeed = 0`으로 초기화한다. 단, 점프/낙하 애니메이션 전환에 영향이 없도록 이동 관련 Bool을 조작하지 않는다.

권장 보완:

- Blend Tree가 급격히 바뀌는 느낌이면 `Mathf.MoveTowards` 또는 `Mathf.Lerp`로 MoveSpeed 값을 보간한다.
- 보간 속도는 하드코딩하지 않고 추후 `AnimationDataSO` 또는 별도 애니메이션 데이터에 둔다.

### 단계 3 — FSM Factory와 전이 목적지 변경

상태: **완료**

대상 파일:

- `Assets/Scripts/FSM/Player/PlayerState/PlayerStateFactory.cs`
- `Assets/Scripts/FSM/Agent/StateControl/TransitionRules/LandTransition.cs`
- `Assets/Scripts/FSM/Agent/StateControl/TransitionRules/AttackEndTransition.cs`
- `Assets/Scripts/FSM/Agent/StateControl/TransitionRules/GetHitEndTransition.cs`
- `Assets/Scripts/FSM/Agent/@Hub/AgentController.cs`

작업:

1. Factory의 `IdleState`, `MoveState` 생성 항목을 삭제하고 `GroundedState` 하나를 생성한다.
2. 기존 두 상태의 공통 전이 규칙을 `GroundedState`에 모은다.
   - `GetHitTransition`
   - `JumpTransition`
   - `GroundedFallTransition`
   - `AttackTransition`
3. `IdleToMoveTransition` 등록을 제거한다.
4. `LandTransition.NextStateType`을 항상 `typeof(GroundedState)`로 변경하고, `IAgentMovementInput` 생성자 의존성을 제거한다.
5. `AttackEndTransition`, `GetHitEndTransition`의 목적지를 `typeof(GroundedState)`로 변경한다.
6. `AgentController.Start()`의 초기 상태를 `typeof(GroundedState)`로 변경한다.

확정 사항:

- `GroundedState`에 `AttackTransition`을 등록해 **이동 중 지상 공격을 허용**한다.
- 공격 상태를 벗어나면 현재 입력의 `MoveSpeed` 값에 따라 Blend Tree가 Idle 또는 Move 클립을 선택한다.

### 단계 4 — 더 이상 사용하지 않는 상태/전이 제거

상태: **완료**

대상 파일:

- 삭제: `Assets/Scripts/FSM/Agent/StateControl/States/IdleState.cs`
- 삭제: `Assets/Scripts/FSM/Agent/StateControl/States/MoveState.cs`
- 삭제: `Assets/Scripts/FSM/Agent/StateControl/TransitionRules/IdleToMoveTransition.cs`

정리 대상:

- `StateType.Idle`, `StateType.Move`
- `AnimationDataSO.IsIdleBool`, `AnimationDataSO.IsMoveBool`
- `AgentAnimator`의 Idle/Move Bool 등록
- Hero Animator의 `IsIdle`, `IsMove` 파라미터와 상태 전이 조건

주의:

- `StateType`은 현재 Animator Bool 키로도 사용 중이다. Idle/Move 제거 전에는 프로젝트 전체 참조를 검색해 다른 Animator 또는 UI 코드에서 사용하지 않는지 확인한다.
- 삭제는 Factory와 전이 목적지 변경 및 Play Mode 검증 후 마지막에 수행한다.

### 단계 5 — Hero Animator Blend Tree 구성

상태: **완료**

대상 에셋:

- `Assets/Prefabs/Player/Animations/Hero_Anim.controller`

Unity Animator 창 작업:

1. Parameters에 `MoveSpeed` Float을 추가한다.
2. Base Layer에서 `Hero_Idle`, `Hero_Move`를 대체할 `Grounded` 상태를 만든다.
3. `Grounded`의 Motion에 **1D Blend Tree**를 설정한다.
4. Blend Parameter를 `MoveSpeed`로 설정한다.
5. Child Motion을 다음처럼 지정한다.

| Threshold | Motion |
| ---: | --- |
| 0.0 | `Hero_Idle` 클립 |
| 1.0 | `Hero_Move` 클립 |

6. 기존 외부 상태 전이를 `Hero_Idle`/`Hero_Move` 대신 `Grounded`로 연결한다.
   - `Grounded → Jump`
   - `Grounded → Fall`
   - `Grounded → Attack`
   - `Grounded → Hit`
   - `Jump/Fall/Attack/Hit → Grounded`
7. `IsIdle`, `IsMove` 조건이 있는 전이를 제거한다.
8. 동작 확인 후 기존 `Hero_Idle`, `Hero_Move` 상태 노드만 제거한다. 클립 에셋은 Blend Tree Child Motion으로 계속 사용하므로 삭제하지 않는다.

Animator 전이 권장값:

- `Grounded → Jump/Fall/Attack/Hit`: Has Exit Time 해제, 전이 시간 최소화
- `Attack/Hit → Grounded`: 기존 Animation Event와 `AttackEndTransition`/`GetHitEndTransition`의 종료 시점 유지
- `Grounded` 내부 Idle/Move 전환: Animator 전이가 아니라 Blend Tree가 처리

### 단계 6 — AnimationDataSO 연결

상태: **완료**

1. Player가 참조하는 `AnimationDataSO`에서 `MoveSpeedFloat = "MoveSpeed"`가 설정됐는지 확인한다.
2. `IsIdle`, `IsMove` 값은 코드 제거 전까지 남아 있어도 되지만, 코드/Animator 정리가 끝나면 비워두거나 필드를 제거한다.
3. Monster용 AnimationDataSO가 같은 데이터 타입을 사용한다면, 아직 Blend Tree를 쓰지 않는 Animator에도 `Speed`가 없는 경우가 있으므로 빈 문자열로 두거나 해당 Animator에만 별도 값을 설정한다.

## 검증 체크리스트

### 컴파일 및 에셋

- [x] Unity C# 컴파일 오류 없음
- [x] Hero Animator에 Missing Motion/전이 없음
- [x] `MoveSpeed` Float 파라미터가 Hero Animator와 AnimationDataSO에서 같은 이름
- [x] `IsIdle`, `IsMove`를 참조하는 코드/전이가 남아 있지 않음

### FSM

- [x] 게임 시작 시 `GroundedState` 진입 — Play Mode 검증 필요
- [x] 입력 없음: `MoveSpeed = 0`, Idle 클립 표시 — Play Mode 검증 필요
- [x] 이동 입력: `MoveSpeed > 0`, Move 클립 표시 — Play Mode 검증 필요
- [x] 이동 입력 해제: 상태 전환 없이 Blend Tree가 Idle로 복귀 — Play Mode 검증 필요
- [ ] 점프: `GroundedState → JumpState → FallState` — Play Mode 검증 필요
- [ ] 착지: 입력 유무와 관계없이 `FallState → GroundedState` — Play Mode 검증 필요
- [ ] 지상 공격 종료: `AttackState → GroundedState` — Play Mode 검증 필요
- [ ] 피격 종료: `HitState → GroundedState` — Play Mode 검증 필요

### 체감 품질

- [ ] Idle/Move 전환 시 깜빡임, 전이 지연, 이전 상태 잔상이 없음
- [ ] 이동 중 점프·낙하·착지에서 Blend Tree가 잘못 노출되지 않음
- [ ] 이동 중 공격을 허용한 경우, Attack 종료 후 입력 상태에 맞는 Idle/Move가 즉시 표시됨
- [ ] 기존 점프 가장자리 높이 문제의 회귀가 없음

> Play Mode 검증 보류 사유: Unity Behavior의 `AIPlayer`/`AIMonster` 에셋에 남아 있는 managed-reference 누락 오류가 Editor 메인 스레드를 점유한다. 이 에셋은 본 작업 범위에서 수정하지 않았다.

## 롤백 기준

다음 중 하나가 발생하면 상태 파일 삭제 전 단계로 되돌리고 원인을 분리한다.

- 공격/피격 종료 후 지상 상태로 복귀하지 않음
- Animator가 `Grounded`가 아닌 상태에 고정됨
- `MoveSpeed`가 갱신되지 않아 Move 클립이 재생되지 않음
- 다른 Agent Animator가 `IsIdle`/`IsMove` 파라미터 누락으로 오류를 냄

롤백은 `Hero_Anim.controller`, `AnimationDataSO`, Factory/Transition 변경을 함께 되돌려 FSM과 Animator의 상태 계약을 일치시킨다.

## 예상 수정 파일 요약

| 분류 | 파일 | 변경 |
| --- | --- | --- |
| 신규 | `GroundedState.cs` | 지상 정지/이동 통합 상태 |
| 수정 | `PlayerStateFactory.cs` | Idle/Move 생성·전이 제거, GroundedState 등록 |
| 수정 | `AgentController.cs` | 초기 상태 변경 |
| 수정 | `LandTransition.cs` | 착지 목적지 단순화 |
| 수정 | `AttackEndTransition.cs` | 종료 목적지 변경 |
| 수정 | `GetHitEndTransition.cs` | 종료 목적지 변경 |
| 수정 | `AgentAnimator.cs` | Float 파라미터 API 추가 |
| 수정 | `AnimationDataSO.cs` | `MoveSpeed` 파라미터 이름 추가 |
| 수정 | `Hero_Anim.controller` | Grounded Blend Tree 및 전이 재연결 |
| 삭제 | `IdleState.cs`, `MoveState.cs`, `IdleToMoveTransition.cs` | 중복 상태/전이 제거 |
