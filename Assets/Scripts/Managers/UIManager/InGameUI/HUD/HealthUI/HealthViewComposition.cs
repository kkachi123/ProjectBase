using UnityEngine;

public class HealthViewComposition : MonoBehaviour
{
    [SerializeField] private Health model;
    [SerializeField] private HealthView view;

    private void OnEnable()
    {
        if (Managers.Instance == null) return;

        Managers.Instance.Player.OnPlayerChanged += BindPlayer;
        BindPlayer(Managers.Instance.Player.CurrentPlayer);
    }

    private void OnDisable()
    {
        if (Managers.Instance == null) return;

        Managers.Instance.Player.OnPlayerChanged -= BindPlayer;
        view?.Unbind();
    }

    private void BindPlayer(PlayerController player)
    {
        if (player == null)
        {
            model = null;
            view?.Unbind();
            return;
        }

        model = player.Health;
        view.Bind(new HealthViewModel(model));
    }
}
