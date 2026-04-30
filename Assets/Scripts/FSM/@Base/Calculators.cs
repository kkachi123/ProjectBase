using UnityEngine;

public static class Calculators
{
    public static float CalcAttackRange(Vector2 offset, Vector2 size)
    {
        // 공격 범위는 공격의 중심점에서 가장 먼 지점까지의 거리를 계산하여 반환 
        return Mathf.Sqrt(offset.x * offset.x + offset.y * offset.y) + Mathf.Sqrt(size.x * size.x + size.y * size.y) / 2f;
    }

    public static float CalcAttackRange(Vector2 offset, Vector2 size, Vector2 direction)
    {
        // 특정 방향(direction)에 대한 공격 범위를 계산. 
        // 오프셋의 해당 방향 투영값과 크기의 해당 방향 투영 절반값을 더함.
        return Vector2.Dot(offset, direction) + (size.x * Mathf.Abs(direction.x) + size.y * Mathf.Abs(direction.y)) / 2f;
    }
}
