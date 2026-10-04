namespace ProjectRE
{
    using System;
    using UnityEngine;

    /// <summary>외부 이동 명령 보관 및 단발 공격 요청 전달.</summary>
    public class MonsterInput : MonoBehaviour, IAgentMovementInput, IAgentCombatInput
    {
        private Vector2 _movement;
        public event Action OnAttackRequested;

        public Vector2 GetMovementInput() => _movement;

        /// <summary>x축 이동 명령 설정. 수직 입력 제외.</summary>
        public void SetMovement(Vector2 movement)
        {
            _movement = new Vector2(Mathf.Clamp(movement.x, -1f, 1f), 0f);
        }

        /// <summary>공격 요청 1회 발행. 공격 타입·입력 예약 제외.</summary>
        public void RequestAttack() => OnAttackRequested?.Invoke();
    }
}
