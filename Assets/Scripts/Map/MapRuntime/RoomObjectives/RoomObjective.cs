namespace ProjectRE
{
    using System;
    using UnityEngine;

    /// <summary>방의 개별 목표와 완료 알림.</summary>
    public abstract class RoomObjective : MonoBehaviour
    {
        [SerializeField] private string description;
        [SerializeField] private SpriteRenderer marker;
        [SerializeField] private Color completedColor = Color.green;

        public string Description => description;
        public bool IsComplete { get; private set; }
        public event Action<RoomObjective> Completed;

        protected void Complete()
        {
            if (IsComplete) return;
            IsComplete = true;
            if (marker != null) marker.color = completedColor;
            Completed?.Invoke(this);
        }
    }
}
