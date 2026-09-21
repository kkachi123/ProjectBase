using UnityEngine;

public class MonsterController : AgentController
{
    [SerializeField] private Collider2D _collider;

    protected override void Awake()
    {
        base.Awake();
        _collider = GetComponent<Collider2D>();
        _states = new MonsterStateFactory()
            .CreateStates(new MonsterStateFactoryData
            {
                Animator = _animator,
                MovementHandler = _movementHandler,
                MovementInput = _moveInput,
                CombatHandler = _combatHandler
            });
    }

    protected override void FixedUpdate() 
    {
        return;
    }

    #region State Animation Event
    public override void OnDeathFinished()
    {
        Destroy(gameObject);
    }
    #endregion
}
