using DG.Tweening;
using UniRx;
using UnityEngine;
using UnityEngine.UI;

public class HealthView : MonoBehaviour
{
    [SerializeField] private Image hpBarImage;

    private HealthViewModel _viewModel;
    private CompositeDisposable _bindings = new();
    private Tween _currentTween;

    public void Bind(HealthViewModel viewModel)
    {
        Unbind();

        _viewModel = viewModel;
        _viewModel.HpRatio.Subscribe(ActionHpChanged).AddTo(_bindings);
    }

    public void Unbind()
    {
        _bindings.Dispose();
        _bindings = new CompositeDisposable();
        _viewModel = null;

        _currentTween?.Kill();
        _currentTween = null;
    }

    private void OnDestroy()
    {
        Unbind();
    }

    private void ActionHpChanged(float newHpRatio)
    {
        _currentTween?.Kill();
        _currentTween = hpBarImage.DOFillAmount(newHpRatio, 0.5f).SetEase(Ease.OutCubic);
    }
}
