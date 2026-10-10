namespace ProjectRE
{
    using System;
    using Unity.Behavior;

    /// <summary>변수 이름과 타입별 실행 Blackboard 참조 관리.</summary>
    public class MonsterBehaviorBlackboard
    {
        private const string ContextName = "Context";
        private const string IsDeadName = "IsDead";
        private const string LostTargetName = "LostTarget";
        private const string HasValidTargetName = "HasValidTarget";
        private const string LostTargetWaitName = "LostTargetWait";
        private const string PatrolWaitMinName = "PatrolWaitMin";
        private const string PatrolWaitMaxName = "PatrolWaitMax";

        private BlackboardVariable<MonsterBehaviorContext> _context;
        private BlackboardVariable<bool> _isDead;
        private BlackboardVariable<bool> _lostTarget;
        private BlackboardVariable<bool> _hasValidTarget;
        private BlackboardVariable<float> _lostTargetWait;
        private BlackboardVariable<float> _patrolWaitMin;
        private BlackboardVariable<float> _patrolWaitMax;

        public MonsterBehaviorContext Context { get => _context.Value; set => _context.Value = value; }
        public bool IsDead { get => _isDead.Value; set => _isDead.Value = value; }
        public bool LostTarget { get => _lostTarget.Value; set => _lostTarget.Value = value; }
        public bool HasValidTarget { get => _hasValidTarget.Value; set => _hasValidTarget.Value = value; }
        public float LostTargetWait { get => _lostTargetWait.Value; set => _lostTargetWait.Value = value; }
        public float PatrolWaitMin { get => _patrolWaitMin.Value; set => _patrolWaitMin.Value = value; }
        public float PatrolWaitMax { get => _patrolWaitMax.Value; set => _patrolWaitMax.Value = value; }

        /// <summary>초기화된 실행 Graph의 변수 참조를 한 번 확보.</summary>
        public void Bind(BehaviorGraphAgent agent)
        {
            _context = GetVariable<MonsterBehaviorContext>(agent, ContextName);
            _isDead = GetVariable<bool>(agent, IsDeadName);
            _lostTarget = GetVariable<bool>(agent, LostTargetName);
            _hasValidTarget = GetVariable<bool>(agent, HasValidTargetName);
            _lostTargetWait = GetVariable<float>(agent, LostTargetWaitName);
            _patrolWaitMin = GetVariable<float>(agent, PatrolWaitMinName);
            _patrolWaitMax = GetVariable<float>(agent, PatrolWaitMaxName);
        }

        /// <summary>Bind 시점의 변수 누락·타입 불일치 확인.</summary>
        private static BlackboardVariable<T> GetVariable<T>(BehaviorGraphAgent agent, string variableName)
        {
            if (!agent.GetVariable<T>(variableName, out var variable) || variable == null)
                throw new InvalidOperationException($"{agent.name}: Blackboard '{variableName}' ({typeof(T).Name}) missing or type mismatch.");

            return variable;
        }
    }
}
