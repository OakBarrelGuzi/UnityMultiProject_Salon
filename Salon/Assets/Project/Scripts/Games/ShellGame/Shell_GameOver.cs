using Salon.ShellGame;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class Shell_GameOver : Shell_Panel
{
    public Button lobby_Button;
    //TODO:���ð�� �Ҵ�
    public TextMeshProUGUI bettingGold_Text;


    private void Start()
    {
        lobby_Button.onClick.AddListener(() =>
        {
            ScenesManager.Instance.ChanageScene("LobbyScene");
            //TODO:�ʱ�ȭ ������ �гβ���
            shellGameUI.gameObject.SetActive(false);
        });
    }
}
