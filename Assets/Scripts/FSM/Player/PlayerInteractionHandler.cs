using UnityEngine;
using UniRx;

public class PlayerInteractionHandler : MonoBehaviour
{
    [SerializeField] private float _interactRadius = 2f;
    [SerializeField] private LayerMask _interactLayer;

    private PlayerInput _playerInput;

    private readonly Collider2D[] _hitBuffer = new Collider2D[8];
    private ContactFilter2D _contactFilter;
    private IInteractable _nearbyTarget;

    private void Awake()
    {
        _playerInput = GetComponent<PlayerInput>();

        _contactFilter = new ContactFilter2D();
        _contactFilter.SetLayerMask(_interactLayer);
        _contactFilter.useTriggers = true;

        _playerInput.InteractPressed
            .Where(v => v)
            .Subscribe(_ => TryInteract())
            .AddTo(this);
    }

    private void Update() => DetectNearbyTarget();

    private void DetectNearbyTarget()
    {
        int count = Physics2D.OverlapCircle(transform.position, _interactRadius, _contactFilter, _hitBuffer);
        _nearbyTarget = null;

        float minDist = float.MaxValue;
        for (int i = 0; i < count; i++)
        {
            if (!_hitBuffer[i].TryGetComponent(out IInteractable interactable)) continue;
            float dist = Vector2.SqrMagnitude(_hitBuffer[i].transform.position - transform.position);
            if (dist >= minDist) continue;
            minDist = dist;
            _nearbyTarget = interactable;
        }
    }

    private void TryInteract()
    {
        if (Managers.Instance.Game.State != GameManager.GameState.Playing) return;
        if (_nearbyTarget == null) return;
        _nearbyTarget.Interact();
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, _interactRadius);
    }
}
