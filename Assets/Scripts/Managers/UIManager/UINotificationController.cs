using System.Collections.Generic;
using UnityEngine;
using TMPro;
using Cysharp.Threading.Tasks;

public class UINotificationController : MonoBehaviour
{
    [SerializeField] private GameObject _panel;
    [SerializeField] private TextMeshProUGUI _messageText;
    [SerializeField] private float _displayDuration = 2f;

    private readonly Queue<string> _queue = new();
    private bool _isPlaying;

    public void Show(string message)
    {
        _queue.Enqueue(message);
        if (!_isPlaying)
            PlayQueue().Forget();
    }

    private async UniTaskVoid PlayQueue()
    {
        _isPlaying = true;
        while (_queue.Count > 0)
        {
            _messageText.text = _queue.Dequeue();
            _panel.SetActive(true);
            await UniTask.WaitForSeconds(_displayDuration);
            _panel.SetActive(false);
        }
        _isPlaying = false;
    }
}
