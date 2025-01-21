using Salon.ShellGame;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class Shell_GameOver : Shell_Panel
{
    public Button lobby_Button;
    //TODO:배팅골드 할당
    public TextMeshProUGUI bettingGold_Text;


    private void Start()
    {
        lobby_Button.onClick.AddListener(() =>
        {
            ScenesManager.Instance.ChanageScene("LobbyScene");
            //TODO:초기화 진행후 패널끄기
            shellGameUI.gameObject.SetActive(false);
        });
    }
}
