namespace ProjectRE
{
    using System;
    using Unity.Behavior;
    using UnityEngine;

    /// <summary>변수 이름과 타입별 실행 Blackboard 참조 관리.</summary>
    public class MonsterBehaviorBlackboard
    {
        private const string ContextName = "Context";
        private const string InputName = "Input";
        private const string TargetName = "Target";
        private const string HomeXName = "HomeX";
        private const string IsDeadName = "IsDead";
        private const string NeedsReturnName = "NeedsReturn";
        private const string NeedsLostTargetWaitName = "NeedsLostTargetWait";
        private const string HasValidTargetName = "HasValidTarget";
        private const string AttackRequestIntervalName = "AttackRequestInterval";
        private const string RearApproachTimeoutName = "RearApproachTimeout";
        private const string LostTargetWaitName = "LostTargetWait";
        private const string PatrolRadiusName = "PatrolRadius";
        private const string PatrolWaitMinName = "PatrolWaitMin";
        private const string PatrolWaitMaxName = "PatrolWaitMax";

        private BlackboardVariable<MonsterBehaviorContext> _context;
        private BlackboardVariable<MonsterInput> _input;
        private BlackboardVariable<GameObject> _target;
        private BlackboardVariable<float> _homeX;
        private BlackboardVariable<bool> _isDead;
        private BlackboardVariable<bool> _needsReturn;
        private BlackboardVariable<bool> _needsLostTargetWait;
        private BlackboardVariable<bool> _hasValidTarget;
        private BlackboardVariable<float> _attackRequestInterval;
        private BlackboardVariable<float> _rearApproachTimeout;
        private BlackboardVariable<float> _lostTargetWait;
        private BlackboardVariable<float> _patrolRadius;
        private BlackboardVariable<float> _patrolWaitMin;
        private BlackboardVariable<float> _patrolWaitMax;

        public MonsterBehaviorContext Context { get => _context.Value; set => _context.Value = value; }
        public MonsterInput Input { get => _input.Value; set => _input.Value = value; }
        public GameObject Target { get => _target.Value; set => _target.Value = value; }
        public float HomeX { get => _homeX.Value; set => _homeX.Value = value; }
        public bool IsDead { get => _isDead.Value; set => _isDead.Value = value; }
        public bool NeedsReturn { get => _needsReturn.Value; set => _needsReturn.Value = value; }
        public bool NeedsLostTargetWait { get => _needsLostTargetWait.Value; set => _needsLostTargetWait.Value = value; }
        public bool HasValidTarget { get => _hasValidTarget.Value; set => _hasValidTarget.Value = value; }
        public float AttackRequestInterval { get => _attackRequestInterval.Value; set => _attackRequestInterval.Value = value; }
        public float RearApproachTimeout { get => _rearApproachTimeout.Value; set => _rearApproachTimeout.Value = value; }
        public float LostTargetWait { get => _lostTargetWait.Value; set => _lostTargetWait.Value = value; }
        public float PatrolRadius { get => _patrolRadius.Value; set => _patrolRadius.Value = value; }
        public float PatrolWaitMin { get => _patrolWaitMin.Value; set => _patrolWaitMin.Value = value; }
        public float PatrolWaitMax { get => _patrolWaitMax.Value; set => _patrolWaitMax.Value = value; }

        /// <summary>초기화된 실행 Graph의 변수 참조를 한 번 확보.</summary>
        public void Bind(BehaviorGraphAgent agent)
        {
            _context = GetVariable<MonsterBehaviorContext>(agent, ContextName);
            _input = GetVariable<MonsterInput>(agent, InputName);
            _target = GetVariable<GameObject>(agent, TargetName);
            _homeX = GetVariable<float>(agent, HomeXName);
            _isDead = GetVariable<bool>(agent, IsDeadName);
            _needsReturn = GetVariable<bool>(agent, NeedsReturnName);
            _needsLostTargetWait = GetVariable<bool>(agent, NeedsLostTargetWaitName);
            _hasValidTarget = GetVariable<bool>(agent, HasValidTargetName);
            _attackRequestInterval = GetVariable<float>(agent, AttackRequestIntervalName);
            _rearApproachTimeout = GetVariable<float>(agent, RearApproachTimeoutName);
            _lostTargetWait = GetVariable<float>(agent, LostTargetWaitName);
            _patrolRadius = GetVariable<float>(agent, PatrolRadiusName);
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
