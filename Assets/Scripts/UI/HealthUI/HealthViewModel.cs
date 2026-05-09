using UniRx;
using UnityEngine;

public class HealthViewModel
{
    public IReadOnlyReactiveProperty<float> HpRatio { get; private set; }

    public HealthViewModel(Health model)
    {
        HpRatio = model.CurrentHealth
            .Select(hp => hp / model.MaxHealth) 
            .DistinctUntilChanged()             // 기능 : 이전 값과 다른 경우에만 업데이트하도록 함
            .ToReactiveProperty();                
    }
}
