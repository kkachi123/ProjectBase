using UnityEngine;
using UniRx;

public class Stamina : MonoBehaviour
{
    private FloatReactiveProperty _currentStamina = new FloatReactiveProperty();
    public IReadOnlyReactiveProperty<float> CurrentStamina => _currentStamina;
    public float MaxStamina { get; private set; }

    private float _recoveryRate;

    public void Initialize(float maxStamina, float recoveryRate)
    {
        _currentStamina.Value = maxStamina;
        MaxStamina = maxStamina;
        _recoveryRate = recoveryRate;
    }

    void Update()
    {
        if (_currentStamina.Value < MaxStamina)
            _currentStamina.Value = Mathf.Min(_currentStamina.Value + _recoveryRate * Time.deltaTime, MaxStamina);
    }

    public bool Use(float amount)
    {
        if (_currentStamina.Value < amount) return false;
        _currentStamina.Value -= amount;
        return true;
    }
}
