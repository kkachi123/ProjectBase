namespace ProjectRE
{
    using UnityEngine;

    /// <summary>방 내부 체류를 Player Collider로 감지한다.</summary>
    [RequireComponent(typeof(Collider2D))]
    public sealed class RoomPresenceSensor : MonoBehaviour
    {
        [SerializeField] private RoomObjectiveController controller;

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (other.GetComponentInParent<PlayerController>() != null) controller.SetPlayerInside(true);
        }

        private void OnTriggerExit2D(Collider2D other)
        {
            if (other.GetComponentInParent<PlayerController>() != null) controller.SetPlayerInside(false);
        }
    }
}
