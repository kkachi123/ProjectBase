using UniRx;
using UnityEngine;

public class HealthViewModel
{
    public IReadOnlyReactiveProperty<float> HpRatio { get; private set; }

    public HealthViewModel(Health model)
    {
        HpRatio = model.CurrentHealth
            .Select(hp => hp / model.MaxHealth) // 데이터를 비율(0~1)로 가공
            .DistinctUntilChanged()               // 값이 실제로 변했을 때만 실행 (최적화)
            .ToReactiveProperty();                
    }
}
