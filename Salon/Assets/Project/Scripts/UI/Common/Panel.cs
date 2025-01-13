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
        Debug.Log($"{panelType} 닫기 시도");
        gameObject.SetActive(false);
        isOpen = false;
        Debug.Log($"{panelType} 닫기 완료");
    }
}