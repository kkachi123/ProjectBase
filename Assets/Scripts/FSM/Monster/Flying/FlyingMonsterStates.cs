namespace ProjectRE
{
    using System;
    using UnityEngine;

    /// <summary>개체별 비행 행동 참조와 공격 간격.</summary>
    public class FlyingMonsterStateData : FlyingStateFactoryData
    {
        public Transform Owner { get; set; }
        public Vector2 Home { get; set; }
        public FlyingMonsterBehaviorDataSO Behavior { get; set; }
        public FlyingMonsterTargetHandler Target { get; set; }
        public float MoveSpeed { get; set; }
        public float NextAttackTime { get; set; }
        public FlyingMonsterAnimator FlyingAnimator => (FlyingMonsterAnimator)Animator;
        public bool InAttackRange => Target.HasValidTarget &&
            ((Vector2)Target.Root.position - (Vector2)Owner.position).sqrMagnitude <= Behavior.AttackDistance * Behavior.AttackDistance;

        // 목적지까지 마지막 이동 속도 제한.
        public void MoveTowards(Vector2 destination, float deltaTime)
        {
            Vector2 delta = destination - (Vector2)Owner.position;
            float distance = delta.magnitude;
            float maxDistance = MoveSpeed * deltaTime;
            MovementHandler.HandleMove(distance > 0f && maxDistance > 0f
                ? delta / distance * Mathf.Min(1f, distance / maxDistance) : Vector2.zero);
        }
    }

    public class FlyingPatrolWaitState : AgentStateBase
    {
        private readonly FlyingMonsterStateData _data;
        private float _endTime;
        public bool Finished => Time.time >= _endTime;
        public FlyingPatrolWaitState(FlyingMonsterStateData data) => _data = data;
        protected override void OnEnter()
        {
            _data.MovementHandler.Stop();
            _endTime = Time.time + UnityEngine.Random.Range(_data.Behavior.PatrolWaitMin, _data.Behavior.PatrolWaitMax);
        }
        protected override void OnExecute(float deltaTime) { }
    }

    public class FlyingPatrolState : AgentStateBase
    {
        private readonly FlyingMonsterStateData _data;
        private Vector2 _destination;
        public bool Arrived => Vector2.Distance(_data.Owner.position, _destination) <= _data.Behavior.ArrivalDistance;
        public FlyingPatrolState(FlyingMonsterStateData data) => _data = data;
        protected override void OnEnter() => _destination = _data.Home + UnityEngine.Random.insideUnitCircle * _data.Behavior.PatrolRadius;
        protected override void OnExecute(float deltaTime) => _data.MoveTowards(_destination, deltaTime);
        protected override void OnExit() => _data.MovementHandler.Stop();
    }

    public class FlyingChaseState : AgentStateBase
    {
        private readonly FlyingMonsterStateData _data;
        public FlyingChaseState(FlyingMonsterStateData data) => _data = data;
        protected override void OnEnter() { }
        protected override void OnExecute(float deltaTime)
        {
            if (!_data.Target.HasValidTarget || _data.InAttackRange)
                _data.MovementHandler.Stop();
            else
                _data.MoveTowards(_data.Target.Root.position, deltaTime);
        }
        protected override void OnExit() => _data.MovementHandler.Stop();
    }

    public class FlyingLostTargetWaitState : AgentStateBase
    {
        private readonly FlyingMonsterStateData _data;
        private float _endTime;
        public bool Finished => Time.time >= _endTime;
        public FlyingLostTargetWaitState(FlyingMonsterStateData data) => _data = data;
        protected override void OnEnter()
        {
            _data.MovementHandler.Stop();
            _endTime = Time.time + _data.Behavior.LostTargetWait;
        }
        protected override void OnExecute(float deltaTime) { }
    }

    public class FlyingReturnState : AgentStateBase
    {
        private readonly FlyingMonsterStateData _data;
        public bool Arrived => Vector2.Distance(_data.Owner.position, _data.Home) <= _data.Behavior.ArrivalDistance;
        public FlyingReturnState(FlyingMonsterStateData data) => _data = data;
        protected override void OnEnter() { }
        protected override void OnExecute(float deltaTime) => _data.MoveTowards(_data.Home, deltaTime);
        protected override void OnExit() => _data.MovementHandler.Stop();
    }

    /// <summary>타격 없이 기본 공격 모션 재생.</summary>
    public class FlyingMonsterAttackState : AttackState
    {
        private readonly FlyingMonsterStateData _data;
        public FlyingMonsterAttackState(FlyingMonsterStateData data) : base(data.FlyingAnimator) => _data = data;
        protected override void OnEnter()
        {
            _data.MovementHandler.Stop();
            if (_data.Target.HasValidTarget)
                _data.Motor.Face(_data.Target.Root.position.x - _data.Owner.position.x);
            Animator.SetAttackType(1);
            base.OnEnter();
        }
        public override void OnAttackFrame(int attackStep) { }
    }

    /// <summary>피격 종료 시 넉백 속도 정리.</summary>
    public class FlyingMonsterHitState : HitState
    {
        private readonly FlyingMovementHandler2D _movement;
        public FlyingMonsterHitState(IHitAnimation animator, FlyingMovementHandler2D movement) : base(animator) => _movement = movement;
        protected override void OnExit()
        {
            _movement.Stop();
            base.OnExit();
        }
    }

    /// <summary>조건식 기반 비행 Monster 전이.</summary>
    public class FlyingMonsterTransition : ITransitionRule
    {
        private readonly Func<bool> _condition;
        public Type NextStateType { get; }
        public FlyingMonsterTransition(Type nextStateType, Func<bool> condition)
        {
            NextStateType = nextStateType;
            _condition = condition;
        }
        public bool ShouldTransition(float deltaTime) => _condition();
    }

    /// <summary>모션 종료 후 대상 상태를 다시 판정.</summary>
    public class FlyingMonsterAnimationEndTransition : IEventTransitionRule
    {
        private readonly GetHitEndTransition _end;
        private readonly FlyingMonsterStateData _data;
        private readonly bool _attack;
        public Type NextStateType { get; private set; }

        public FlyingMonsterAnimationEndTransition(FlyingMonsterStateData data, bool attack)
        {
            _data = data;
            _attack = attack;
            _end = new GetHitEndTransition(data.AnimationEventSource, typeof(FlyingReturnState));
        }
        public void Subscribe() => _end.Subscribe();
        public void Unsubscribe() => _end.Unsubscribe();
        public bool ShouldTransition(float deltaTime)
        {
            if (!_end.ShouldTransition(deltaTime)) return false;
            if (_attack) _data.NextAttackTime = Time.time + _data.Behavior.AttackInterval;
            NextStateType = _data.Target.HasValidTarget ? typeof(FlyingChaseState)
                : _attack ? typeof(FlyingLostTargetWaitState) : typeof(FlyingReturnState);
            return true;
        }
    }
}
