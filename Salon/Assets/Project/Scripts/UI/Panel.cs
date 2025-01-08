using UnityEngine;

public class Panel : MonoBehaviour
{
    public PanelType panelType;
    public bool isOpen;

    protected virtual void OnEnable()
    {
        Initialize();
    }

    public virtual void Initialize() { }
    public virtual void Open()
    {
        isOpen = true;
        gameObject.SetActive(true);
    }
    public virtual void Close()
    {
        isOpen = false;
        gameObject.SetActive(false);
    }
}