using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class CardResultUi : MonoBehaviour
{

    public TextMeshProUGUI localPlayerScore;
    public TextMeshProUGUI remotePlayerScore;
    public TextMeshProUGUI localPlayerName;
    public TextMeshProUGUI remotePlayerName;
    public TextMeshProUGUI myGoldText;
    public TextMeshProUGUI getGoldText;
    public Button ExitButton;

    private void Start()
    {
        ExitButton.onClick.AddListener(() =>
        {
            UIManager.Instance.CloseAllPanels();
            ScenesManager.Instance.ChanageScene("LobbyScene");
            UIManager.Instance.OpenPanel(PanelType.Lobby);
        });
    }

}
