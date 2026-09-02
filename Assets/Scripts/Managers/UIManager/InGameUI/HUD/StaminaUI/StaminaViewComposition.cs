using UnityEngine;

public class StaminaViewComposition : MonoBehaviour
{
    [SerializeField] private Stamina model;
    [SerializeField] private StaminaView view;

    private void Awake()
    {
        model = Managers.Instance.Player.CurrentPlayer.Stamina;
        var viewModel = new StaminaViewModel(model);
        view.Bind(viewModel);
    }
}
