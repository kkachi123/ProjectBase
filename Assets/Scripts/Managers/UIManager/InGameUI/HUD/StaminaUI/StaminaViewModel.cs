using UniRx;

public class StaminaViewModel
{
    public IReadOnlyReactiveProperty<float> StaminaRatio { get; private set; }

    public StaminaViewModel(Stamina model)
    {
        StaminaRatio = model.CurrentStamina
            .Select(stamina => stamina / model.MaxStamina)
            .DistinctUntilChanged()
            .ToReactiveProperty();
    }
}
