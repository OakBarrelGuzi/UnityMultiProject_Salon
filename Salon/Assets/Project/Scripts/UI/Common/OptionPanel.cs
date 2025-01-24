using UnityEngine;
using UnityEngine.UI;
using Salon.Firebase;

public class OptionPanel : Panel
{
    [SerializeField] private Button closeButton;
    [SerializeField] private Button soundButton;
    [SerializeField] private Button bgmButton;
    [SerializeField] private Button saveButton;
    [SerializeField] private Button signOutButton;
    public override void Initialize()
    {
        closeButton.onClick.AddListener(() => UIManager.Instance.ClosePanel(PanelType.Option));
        if (FirebaseManager.Instance.CurrentUserUID != null)
        {
            signOutButton.gameObject.SetActive(true);
            signOutButton.onClick.AddListener(OnSignOut);
        }
        else
        {
            signOutButton.gameObject.SetActive(false);
        }
    }

    public override void Open()
    {
        base.Open();
        Initialize();
    }

    public override void Close()
    {
        signOutButton.onClick.RemoveAllListeners();
        closeButton.onClick.RemoveAllListeners();
        base.Close();
    }

    public void OnSignOut()
    {
        UIManager.Instance.CloseAllPanels();
        UIManager.Instance.OpenPanel(PanelType.MainDisplay);
        UIManager.Instance.OpenPanel(PanelType.SignIn);
        FirebaseManager.Instance.SignOut();
    }

}