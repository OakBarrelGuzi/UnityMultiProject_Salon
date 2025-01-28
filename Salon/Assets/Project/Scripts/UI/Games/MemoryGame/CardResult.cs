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

    private void Awake()
    {
        InitializeUI();
    }

    private void OnEnable()
    {
        ExitButton.onClick.RemoveAllListeners();
        ExitButton.onClick.AddListener(OnExitButtonClick);
    }

    private void OnDisable()
    {
        ExitButton.onClick.RemoveAllListeners();
        ClearUI();
    }

    private void InitializeUI()
    {
        if (localPlayerScore) localPlayerScore.text = "0";
        if (remotePlayerScore) remotePlayerScore.text = "0";
        if (localPlayerName) localPlayerName.text = "";
        if (remotePlayerName) remotePlayerName.text = "";
        if (myGoldText) myGoldText.text = "0";
        if (getGoldText) getGoldText.text = "0";
    }

    private void ClearUI()
    {
        if (localPlayerScore) localPlayerScore.text = "";
        if (remotePlayerScore) remotePlayerScore.text = "";
        if (localPlayerName) localPlayerName.text = "";
        if (remotePlayerName) remotePlayerName.text = "";
        if (myGoldText) myGoldText.text = "";
        if (getGoldText) getGoldText.text = "";
    }

    private void OnExitButtonClick()
    {
        UIManager.Instance.CloseAllPanels();
        ScenesManager.Instance.ChanageScene("LobbyScene");
        UIManager.Instance.OpenPanel(PanelType.Lobby);
    }
}
