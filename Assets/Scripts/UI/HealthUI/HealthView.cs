using DG.Tweening;
using UniRx;
using UnityEngine;
using UnityEngine.UI;

public class HealthView : MonoBehaviour
{
    [SerializeField] private Image hpBarImage;
    private HealthViewModel _viewModel;


    private Tween _currentTween; // 현재 진행 중인 트윈 저장용
    public void Bind(HealthViewModel viewModel)
    {
        _viewModel = viewModel;
        // HpRatio가 변경될 때마다 ActionHpChanged 메서드 호출
        _viewModel.HpRatio.Subscribe(ActionHpChanged).AddTo(this);
    }

    private void ActionHpChanged(float newHpRatio)
    {
        // 기존 트윈이 있다면 중지
        _currentTween?.Kill();
        // 새로운 트윈 시작 (0.5초 동안 부드럽게 변화)
        _currentTween = hpBarImage.DOFillAmount(newHpRatio, 0.5f).SetEase(Ease.OutCubic);
    }
}
