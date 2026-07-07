using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class LoadingUI : MonoBehaviour
{
    [SerializeField] private Slider _progressBar;
    [SerializeField] private TextMeshProUGUI _progressText;

    public void Show()
    {
        gameObject.SetActive(true);
        SetProgress(0f);
    }

    public void Hide() => gameObject.SetActive(false);

    public void SetProgress(float value)
    {
        value = Mathf.Clamp01(value);
        if (_progressBar != null) _progressBar.value = value;
        if (_progressText != null) _progressText.text = $"{Mathf.RoundToInt(value * 100)}%";
    }
}
