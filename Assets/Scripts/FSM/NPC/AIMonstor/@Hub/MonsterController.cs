namespace ProjectRE
{
using UnityEngine;

[RequireComponent(typeof(MonsterInput), typeof(MonsterAnimator))]
/// <summary>기본 네 State Monster 초기화·단발 공격 판정·사망 종료 처리.</summary>
public class MonsterController : GroundedAgentController
{
    private MonsterAnimator _monsterAnimator;
    [SerializeField] private GroundedMotorData _motorData;
    protected override GroundedMotorData MovementData => _motorData;

    protected override void Awake()
    {
        base.Awake();
        _monsterAnimator = GetComponent<MonsterAnimator>();
        _monsterAnimator.Initialize();
        _states = new MonsterStateFactory()
            .CreateStates(new MonsterStateFactoryData
            {
                MonsterAnimator = _monsterAnimator,
                Motor = _groundedMotor,
                MovementHandler = _movementHandler,
                MovementInput = _moveInput,
                CombatHandler = _combatHandler,
                CombatInput = CombatInput,
                Health = Health,
                AnimationEventSource = this,
                AttackStarter = this,
            });
    }

    protected override void Start()
    {
        base.Start();
        ChangeState(typeof(GroundedState));
    }

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
