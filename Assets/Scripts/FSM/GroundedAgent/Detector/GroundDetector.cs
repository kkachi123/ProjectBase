using UnityEngine;

public class GroundDetector : MonoBehaviour
{
    [SerializeField] private LayerMask groundLayers;
    [SerializeField] Vector2 rayBoxSize = new Vector2(0.5f, 0.1f);
    [SerializeField] Vector3 playerFootPos = new Vector3(0f, 0.6f, 0f);

    public bool IsGrounded { get; private set; }
    // public bool StairsGrounded { get; private set; }
    public void UpdateGroundedStatus()
    {
        RaycastHit2D hit = Physics2D.BoxCast(transform.position - playerFootPos, rayBoxSize, 0f, Vector2.down, 0.1f, groundLayers);
        IsGrounded = hit.collider != null;
    }

    private void OnDrawGizmos()
    {
        // 1. Raycast 시작 위치 계산 (플레이어 발 위치에서 약간 위로)
        Vector3 startPos = transform.position - playerFootPos;

        // 2. Raycast 끝 위치 계산 (시작 위치에서 아래로 0.1f)
        Vector3 endPos = startPos + Vector3.down * 0.1f;

        // 3. Gizmos로 Raycast 시각화 (시작 위치에서 끝 위치까지 선 그리기)
        Gizmos.DrawWireCube(startPos, new Vector3(rayBoxSize.x, rayBoxSize.y, 0.1f));

        // 4. Raycast 결과 시각화
        Gizmos.DrawLine(startPos, endPos);
        Gizmos.color = Color.red;
        if (IsGrounded) Gizmos.color = Color.green; // 땅에 닿아있으면 초록색, 아니면 빨간색

        // 5. Raycast 끝 위치 시각화
        Gizmos.DrawWireCube(endPos, new Vector3(rayBoxSize.x, rayBoxSize.y, 0.1f));
    }
}
