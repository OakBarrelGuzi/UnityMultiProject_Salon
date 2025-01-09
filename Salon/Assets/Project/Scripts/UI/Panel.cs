using UnityEngine;

public abstract class Panel : MonoBehaviour
{
    public PanelType panelType;
    public bool isOpen;

    public abstract void Initialize();

    public virtual void Open()
    {
        gameObject.SetActive(true);
        isOpen = true;
    }

    public virtual void Close()
    {
        gameObject.SetActive(false);
        isOpen = false;
    }
}