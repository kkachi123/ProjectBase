using UnityEngine;

/// <summary>
/// 카메라 이동량에 비례해 배경을 따라가게 하는 패럴랙스 레이어.
/// factor 0 = 플레이 레이어와 동일 (따라가지 않음)
/// factor 1 = 하늘처럼 화면에 고정 (카메라를 100% 따라감)
/// 원경일수록 높은 값 (mount ≈ 0.85, back trees ≈ 0.55)
/// </summary>
public class ParallaxLayer : MonoBehaviour
{
    [SerializeField, Range(0f, 1f)] private float _factor = 0.5f;

    private Transform _cam;
    private Vector3 _startPos;
    private Vector3 _camStartPos;

    void Start()
    {
        if (Camera.main == null) { enabled = false; return; }
        _cam = Camera.main.transform;
        _startPos = transform.position;
        _camStartPos = _cam.position;
    }

    void LateUpdate()
    {
        Vector3 delta = _cam.position - _camStartPos;
        transform.position = _startPos + new Vector3(delta.x * _factor, delta.y * _factor, 0f);
    }
}
