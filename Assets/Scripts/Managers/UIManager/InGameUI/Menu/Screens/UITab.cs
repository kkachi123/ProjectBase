using UnityEngine;

public abstract class UITab : MonoBehaviour
{
    public virtual void OnShow() => gameObject.SetActive(true);
    public virtual void OnHide() => gameObject.SetActive(false);
}
