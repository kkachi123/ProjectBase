using DG.Tweening;
using UniRx;
using UnityEngine;
using UnityEngine.UI;

public class StaminaView : MonoBehaviour
{
    [SerializeField] private Image _staminaBarImage;

    private Tween _currentTween;

    public void Bind(StaminaViewModel viewModel)
    {
        viewModel.StaminaRatio.Subscribe(OnStaminaChanged).AddTo(this);
    }

    private void OnStaminaChanged(float ratio)
    {
        _currentTween?.Kill();
        _currentTween = _staminaBarImage.DOFillAmount(ratio, 0.2f).SetEase(Ease.OutCubic);
    }
}
