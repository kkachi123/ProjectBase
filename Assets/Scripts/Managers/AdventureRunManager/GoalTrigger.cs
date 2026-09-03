using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class GoalTrigger : MonoBehaviour, IInteractable
{
    public enum ReachMode
    {
        Touch,
        Interact,
    }

    [SerializeField] private ReachMode _reachMode = ReachMode.Touch;

    private bool _isReached;

    private void Reset()
    {
        Collider2D goalCollider = GetComponent<Collider2D>();
        goalCollider.isTrigger = true;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (_reachMode != ReachMode.Touch) return;

        PlayerController player = other.GetComponentInParent<PlayerController>();
        if (player == null) return;

        Reach();
    }

    public void Interact()
    {
        if (_reachMode != ReachMode.Interact) return;

        PlayerController player = Managers.Instance.Player.CurrentPlayer;
        if (player == null) return;

        Reach();
    }

    private void Reach()
    {
        if (_isReached) return;

        _isReached = true;
        if (Managers.Instance == null || Managers.Instance.AdventureRun == null) return;

        Managers.Instance.AdventureRun.EndRun();
    }
}
