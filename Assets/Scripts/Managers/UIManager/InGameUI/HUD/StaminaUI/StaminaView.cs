using DG.Tweening;
using UniRx;
using UnityEngine;
using UnityEngine.UI;

public class StaminaView : MonoBehaviour
{
    [SerializeField] private Image _staminaBarImage;

    private CompositeDisposable _bindings = new();
    private Tween _currentTween;

    public void Bind(StaminaViewModel viewModel)
    {
        Unbind();

        viewModel.StaminaRatio.Subscribe(OnStaminaChanged).AddTo(_bindings);
    }

    public void Unbind()
    {
        _bindings.Dispose();
        _bindings = new CompositeDisposable();

        _currentTween?.Kill();
        _currentTween = null;
    }

    private void OnDestroy()
    {
        Unbind();
    }

    private void OnStaminaChanged(float ratio)
    {
        _currentTween?.Kill();
        _currentTween = _staminaBarImage.DOFillAmount(ratio, 0.2f).SetEase(Ease.OutCubic);
    }
}
