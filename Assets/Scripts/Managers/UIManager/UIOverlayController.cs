namespace ProjectRE
{
using System.Collections;
using UnityEngine;

public class UIOverlayController : MonoBehaviour
{
    [SerializeField] private CanvasGroup _screenFade;
    [SerializeField] private CanvasGroup _damageFlash;
    [SerializeField] private GameObject _pauseDim;
    [SerializeField] private LoadingUI _loading;
    private Coroutine _screenFadeRoutine;

    public LoadingUI Loading => _loading;

    public void FadeIn(float duration) => StartScreenFade(1f, duration);
    public void FadeOut(float duration) => StartScreenFade(0f, duration);
    public void PlayDamageFlash() => StartCoroutine(FlashRoutine());
    public void ShowPauseDim(bool show) => _pauseDim.SetActive(show);
    public void ShowLoading(bool show) { if (show) _loading.Show(); else _loading.Hide(); }

    // 씬 전환 시 오버레이 잔존 상태를 방지하기 위한 일괄 리셋
    public void ResetAll()
    {
        ShowPauseDim(false);
        ShowLoading(false);
        if (_screenFadeRoutine != null)
            StopCoroutine(_screenFadeRoutine);
        _screenFadeRoutine = null;
        if (_screenFade != null)
        {
            _screenFade.alpha = 0f;
            _screenFade.blocksRaycasts = false;
        }
        if (_damageFlash != null) _damageFlash.alpha = 0f;
    }

    // 기존 화면 페이드만 중단하고 현재 Alpha에서 전환.
    private void StartScreenFade(float targetAlpha, float duration)
    {
        if (_screenFadeRoutine != null)
            StopCoroutine(_screenFadeRoutine);
        _screenFade.blocksRaycasts = true;
        _screenFadeRoutine = StartCoroutine(ScreenFadeRoutine(targetAlpha, duration));
    }

    // 시간 정지와 무관한 화면 전환·완료 후 입력 차단 해제.
    private IEnumerator ScreenFadeRoutine(float targetAlpha, float duration)
    {
        float from = _screenFade.alpha;
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            _screenFade.alpha = Mathf.Lerp(from, targetAlpha, elapsed / duration);
            yield return null;
        }
        _screenFade.alpha = targetAlpha;
        _screenFade.blocksRaycasts = targetAlpha > 0f;
        _screenFadeRoutine = null;
    }

    private IEnumerator FadeRoutine(CanvasGroup group, float from, float to, float duration)
    {
        float elapsed = 0f;
        group.alpha = from;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            group.alpha = Mathf.Lerp(from, to, elapsed / duration);
            yield return null;
        }
        group.alpha = to;
    }

    private IEnumerator FlashRoutine()
    {
        yield return FadeRoutine(_damageFlash, 0.6f, 0f, 0.3f);
    }
}
}
