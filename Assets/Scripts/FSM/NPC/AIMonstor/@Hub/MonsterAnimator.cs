namespace ProjectRE
{
    using System;
    using System.Collections.Generic;
    using UnityEngine;

    public enum MonsterAnimationBoolType { Grounded, Attack }
    public enum MonsterAnimationFloatType { MoveSpeed }
    public enum MonsterAnimationIntType { AttackType }

    /// <summary>Agent 공통 Hit·Death와 Monster Grounded·Attack 표현 연결.</summary>
    public class MonsterAnimator : AgentAnimator, IGroundedAnimation, ICombatAnimation
    {
        private readonly Dictionary<MonsterAnimationBoolType, int> _boolParameters = new();
        private readonly Dictionary<MonsterAnimationFloatType, int> _floatParameters = new();
        private readonly Dictionary<MonsterAnimationIntType, int> _intParameters = new();

        public override void Initialize()
        {
            if (_animationData is not MonsterAnimationDataSO data)
                throw new InvalidOperationException($"{name}: MonsterAnimationDataSO is required.");
            if (_anim == null || _anim.runtimeAnimatorController == null)
                throw new InvalidOperationException($"{name}: Visual Animator and Controller are required.");

            ValidateParameter(data.IsHitBool, AnimatorControllerParameterType.Bool);
            ValidateParameter(data.IsDeathBool, AnimatorControllerParameterType.Bool);
            ValidateParameter(data.IsGroundedBool, AnimatorControllerParameterType.Bool);
            ValidateParameter(data.IsAttackBool, AnimatorControllerParameterType.Bool);
            ValidateParameter(data.MoveSpeedFloat, AnimatorControllerParameterType.Float);
            ValidateParameter(data.AttackTypeInt, AnimatorControllerParameterType.Int);

            base.Initialize();
            _boolParameters.Clear();
            _floatParameters.Clear();
            _intParameters.Clear();
            Register(_boolParameters, MonsterAnimationBoolType.Grounded, data.IsGroundedBool);
            Register(_boolParameters, MonsterAnimationBoolType.Attack, data.IsAttackBool);
            Register(_floatParameters, MonsterAnimationFloatType.MoveSpeed, data.MoveSpeedFloat);
            Register(_intParameters, MonsterAnimationIntType.AttackType, data.AttackTypeInt);
        }

        private void ValidateParameter(string parameterName, AnimatorControllerParameterType type)
        {
            foreach (AnimatorControllerParameter parameter in _anim.parameters)
            {
                if (parameter.name == parameterName && parameter.type == type)
                    return;
            }

            throw new InvalidOperationException($"{name}: Animator parameter '{parameterName}' ({type}) is missing.");
        }

        public void SetGrounded(bool value) => SetBool(_boolParameters, MonsterAnimationBoolType.Grounded, value);
        public void SetMoveSpeed(float value) => SetFloat(_floatParameters, MonsterAnimationFloatType.MoveSpeed, value);
        public void SetAttack(bool value) => SetBool(_boolParameters, MonsterAnimationBoolType.Attack, value);
        public void SetAttackType(int value) => SetInteger(_intParameters, MonsterAnimationIntType.AttackType, value);
    }
}
