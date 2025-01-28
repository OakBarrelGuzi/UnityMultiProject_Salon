using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class MemoryExit : MonoBehaviour
{
    public Button ExitYesButton;
    public Button ExitNoButton;

    void Start()
    {
        ExitYesButton.onClick.AddListener(OnClickExitYesButton);

        ExitNoButton.onClick.AddListener(() => gameObject.SetActive(false));
    }

    public void OnClickExitYesButton()
    {
        UIManager.Instance.CloseAllPanels();
        ScenesManager.Instance.ChanageScene("LobbyScene");
        UIManager.Instance.OpenPanel(PanelType.Lobby);
    }
}
