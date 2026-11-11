namespace ProjectRE
{
    using UnityEngine;

    /// <summary>Player의 지점 도달 또는 상호작용으로 완료한다.</summary>
    [RequireComponent(typeof(Collider2D))]
    public sealed class RoomTriggerObjective : RoomObjective, IInteractable
    {
        public enum TriggerMode { Reach, Interact }
        [SerializeField] private TriggerMode mode;

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (mode == TriggerMode.Reach && other.GetComponentInParent<PlayerController>() != null)
                Complete();
        }

        public void Interact()
        {
            if (mode == TriggerMode.Interact) Complete();
        }
    }
}
