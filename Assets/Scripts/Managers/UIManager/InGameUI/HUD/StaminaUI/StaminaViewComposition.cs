using UnityEngine;

public class StaminaViewComposition : MonoBehaviour
{
    [SerializeField] private Stamina model;
    [SerializeField] private StaminaView view;

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

        model = player.Stamina;
        view.Bind(new StaminaViewModel(model));
    }
}
