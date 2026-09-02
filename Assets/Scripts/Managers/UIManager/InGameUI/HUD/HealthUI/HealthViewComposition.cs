using UnityEngine;

public class HealthViewComposition : MonoBehaviour
{
    [SerializeField] private Health model;
    [SerializeField] private HealthView view;

    private void Awake()
    {
        model = Managers.Instance.Player.CurrentPlayer.Health;
        var viewModel = new HealthViewModel(model); 
        view.Bind(viewModel);                      
    }
}
