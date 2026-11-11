namespace ProjectRE
{
    using System;
    using UniRx;
    using UnityEngine;

    /// <summary>지정된 몬스터의 사망 상태를 구독한다.</summary>
    public sealed class RoomMonsterObjective : RoomObjective
    {
        [SerializeField] private Health target;
        private IDisposable subscription;
        private bool started;

        public void SetTarget(Health value) => target = value;

        private void Start()
        {
            started = true;
            Bind();
        }

        private void OnEnable()
        {
            if (started) Bind();
        }

        private void OnDisable()
        {
            subscription?.Dispose();
            subscription = null;
        }

        private void Bind()
        {
            if (subscription != null || target == null || IsComplete) return;
            subscription = target.IsDead.Subscribe(dead => { if (dead) Complete(); });
        }
    }
}
