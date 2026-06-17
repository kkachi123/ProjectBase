using UnityEngine;

public abstract class UIScreen : MonoBehaviour
{
    public virtual void OnEnter()
    {
        gameObject.SetActive(true);
    }

    public virtual void OnExit()
    {
        gameObject.SetActive(false);
    }

    public virtual void OnPause()
    {
        gameObject.SetActive(false);
    }

    public virtual void OnResume()
    {
        gameObject.SetActive(true);
    }
}
