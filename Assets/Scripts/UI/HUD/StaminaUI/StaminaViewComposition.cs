using UnityEngine;

public class StaminaViewComposition : MonoBehaviour
{
    [SerializeField] private Stamina model;
    [SerializeField] private StaminaView view;

    private void Awake()
    {
        var viewModel = new StaminaViewModel(model);
        view.Bind(viewModel);
    }
}
