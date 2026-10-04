namespace ProjectRE
{
using System;
using UnityEngine;

[RequireComponent(typeof(MonsterInput), typeof(MonsterAnimator))]
/// <summary>기본 네 State Monster 초기화·단발 공격 판정·사망 종료 처리.</summary>
public class MonsterController : AgentController
{
    private MonsterAnimator _monsterAnimator;

    protected override void Awake()
    {
        if (_statData == null || _motorData == null || _animationEventProxy == null)
            throw new InvalidOperationException($"{name}: StatData, MotorData and AnimationEventProxy are required.");

        base.Awake();
        _monsterAnimator = GetComponent<MonsterAnimator>();
        _monsterAnimator.Initialize();
        _states = new MonsterStateFactory()
            .CreateStates(new MonsterStateFactoryData
            {
                MonsterAnimator = _monsterAnimator,
                Motor = _motor,
                MovementHandler = _movementHandler,
                MovementInput = _moveInput,
                CombatHandler = _combatHandler,
                CombatInput = CombatInput,
                Health = Health,
                AnimationEventSource = this,
                AttackStarter = this,
            });
    }

    protected override void FixedUpdate() { }

    /// <summary>생존·기본 공격 데이터 판정 및 1번 공격 타입 설정.</summary>
    public override bool TryStartAttack()
    {
        if (Health.IsDead.Value || !_combatHandler.CanApplyAttackType(1))
            return false;

        _combatHandler.ApplyAttackType(1);
        return true;
    }

    #region State Animation Event
    public override void OnDeathFinished()
    {
        Destroy(gameObject);
    }
    #endregion
}
}
