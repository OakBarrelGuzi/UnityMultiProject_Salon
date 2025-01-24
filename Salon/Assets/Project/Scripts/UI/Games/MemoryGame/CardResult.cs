using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;

public class CardResultUi : MonoBehaviour, IPointerClickHandler
{

    public TextMeshProUGUI localPlayerScore;
    public TextMeshProUGUI remotePlayerScore;
    public TextMeshProUGUI localPlayerName;
    public TextMeshProUGUI remotePlayerName;

    public void OnPointerClick(PointerEventData eventData)
    {
        UIManager.Instance.CloseAllPanels();
        ScenesManager.Instance.ChanageScene("LobbyScene");
    }
}
