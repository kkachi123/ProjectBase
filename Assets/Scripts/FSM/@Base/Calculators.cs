using UnityEngine;

public static class Calculators
{
    public static float CalcAttackRange(Vector2 offset, Vector2 size)
    {
        // 공격 범위는 공격의 중심점에서 가장 먼 지점까지의 거리를 계산하여 반환 
        return Mathf.Sqrt(offset.x * offset.x + offset.y * offset.y) + Mathf.Sqrt(size.x * size.x + size.y * size.y) / 2f;
    }
}
