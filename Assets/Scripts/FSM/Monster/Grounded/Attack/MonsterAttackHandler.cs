namespace ProjectRE
{
    using System;
    using System.Collections.Generic;
    using UnityEngine;

    /// <summary>장착 SO 복제·동일 확률 선택·정상 종료 간격 및 실행 수명 관리.</summary>
    [Serializable]
    public class MonsterAttackHandler
    {
        [Serializable]
        public struct AttackDefinition
        {
            public int AnimatorAttackType;
            public MonsterAttackSO Attack;
        }

        [SerializeField] private AttackDefinition[] _attackDefinitions = Array.Empty<AttackDefinition>();
        /// <summary>Combat Editor의 장착 공격 원본 SO 조회.</summary>
        public IReadOnlyList<AttackDefinition> AttackDefinitions => _attackDefinitions;

        private readonly List<MonsterAttackSO> _strategies = new();
        private readonly List<MonsterAttackSO> _eligible = new();
        private MonsterTargetHandler _target;
        private MonsterAttackSO _pendingAttack;
        private MonsterAttackSO _activeAttack;
        private float _nextAttackTime;
        public MonsterAttackStatus Status => _activeAttack.Status;

        /// <summary>장착 SO를 개체별로 복제·주입. 재활성화 때 재생성하지 않음.</summary>
        public void Initialize(GroundedMotor2D motor, GroundedMovementHandler2D movement,
            MonsterTargetHandler target, MonsterAnimator animator, AgentMeleeHitHandler2D hitHandler, float arrivalDistance)
        {
            _target = target;
            _strategies.Clear();
            foreach (AttackDefinition definition in _attackDefinitions)
            {
                MonsterAttackSO strategy = UnityEngine.Object.Instantiate(definition.Attack);
                strategy.name = definition.Attack.name + " (Runtime)";
                strategy.hideFlags = HideFlags.DontSave;
                strategy.Initialize(motor, movement, target, animator, hitHandler, arrivalDistance, definition.AnimatorAttackType);
                _strategies.Add(strategy);
            }
        }

        /// <summary>Chase에서 사용할 장착 공격 중 하나 이상의 거리 충족 여부.</summary>
        public bool IsInAttackRange(float deltaX)
        {
            foreach (MonsterAttackSO strategy in _strategies)
                if (Mathf.Abs(deltaX) <= strategy.AttackDistance)
                    return true;
            return false;
        }

        /// <summary>유효 대상·공격 간격·거리 확인 후 다음 진입 전략만 선택.</summary>
        public bool TryPrepareAttack()
        {
            if (!_target.HasValidTarget || Time.time < _nextAttackTime)
                return false;

            _eligible.Clear();
            foreach (MonsterAttackSO strategy in _strategies)
                if (Mathf.Abs(_target.DeltaX) <= strategy.AttackDistance)
                    _eligible.Add(strategy);
            if (_eligible.Count == 0)
                return false;

            _pendingAttack = _eligible[UnityEngine.Random.Range(0, _eligible.Count)];
            return true;
        }

        /// <summary>이전 공격 Exit 후 예약된 전략을 현재 실행으로 승격.</summary>
        public void BeginAttack()
        {
            _activeAttack = _pendingAttack;
            _pendingAttack = null;
            _activeAttack.Begin();
        }

        public void Execute(float deltaTime) => _activeAttack.Execute(deltaTime);
        public void OnAttackFrame(int intParameter) => _activeAttack.OnAttackFrame(intParameter);

        /// <summary>실제 정상 종료 한 번에만 다음 공격 가능 시각 기록.</summary>
        public void OnAnimationEnd()
        {
            if (_activeAttack.Status != MonsterAttackStatus.Attacking)
                return;
            _activeAttack.OnAnimationEnd();
            if (_activeAttack.Status == MonsterAttackStatus.Completed)
                _nextAttackTime = Time.time + _activeAttack.AttackInterval;
        }

        /// <summary>현재 실행만 정리. 재진입용 다음 선택은 유지.</summary>
        public void EndAttack()
        {
            _activeAttack.End();
            _activeAttack = null;
        }

        /// <summary>현재 State Exit 이후 실행 복제본·보관 참조 제거.</summary>
        public void Dispose()
        {
            foreach (MonsterAttackSO strategy in _strategies)
                UnityEngine.Object.Destroy(strategy);
            _strategies.Clear();
            _eligible.Clear();
            _activeAttack = null;
            _pendingAttack = null;
            _target = null;
        }
    }
}
