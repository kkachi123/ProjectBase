using UnityEngine;

public class CameraFollow2D : MonoBehaviour
{
    [SerializeField] private Vector3 _offset = new Vector3(0f, 0f, -10f);
    [SerializeField] private float _followSpeed = 8f;

    private Transform _target;

    public Vector3 Offset => _offset;
    public Transform Target => _target;

    private void LateUpdate()
    {
        if (_target == null) return;

        Vector3 desiredPosition = _target.position + _offset;
        float t = 1f - Mathf.Exp(-_followSpeed * Time.deltaTime);
        transform.position = Vector3.Lerp(transform.position, desiredPosition, t);
    }

    public void SetTarget(Transform target, bool snap = true)
    {
        _target = target;
        if (snap && _target != null)
            transform.position = _target.position + _offset;
    }
}
