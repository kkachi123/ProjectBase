using System.Collections;
using UnityEngine;

public class UIOverlayController : MonoBehaviour
{
    [SerializeField] private CanvasGroup _screenFade;
    [SerializeField] private CanvasGroup _damageFlash;
    [SerializeField] private GameObject _pauseDim;
    [SerializeField] private GameObject _loading;

    public void FadeIn(float duration) => StartCoroutine(FadeRoutine(_screenFade, 0f, 1f, duration));
    public void FadeOut(float duration) => StartCoroutine(FadeRoutine(_screenFade, 1f, 0f, duration));
    public void PlayDamageFlash() => StartCoroutine(FlashRoutine());
    public void ShowPauseDim(bool show) => _pauseDim.SetActive(show);
    public void ShowLoading(bool show) => _loading.SetActive(show);

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
