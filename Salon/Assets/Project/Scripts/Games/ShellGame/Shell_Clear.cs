using Salon.ShellGame;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;


public class Shell_Clear : Shell_Panel
{
    public Button lobby_Button;
    //TODO: ���ð�� �Ҵ�
    public Button editBetting_Button;
    public Button go_Button;
    //TODO:���� �ؽ�Ʈ �Ҵ� �ΰ�
    public TextMeshProUGUI bettingGold_Text;
    public Toggle All_betting_Toggle;


    private void Start()
    {
        lobby_Button.onClick.AddListener(() =>
        {
            ScenesManager.Instance.ChanageScene("LobbyScene");
            //TODO:�ʱ�ȭ ����
            shellGameUI.gameObject.SetActive(false);
        });
    }

}
