using System.Collections;
using System.Collections.Generic;
 using UnityEngine;
using UnityEngine.UI;

public class Shell_Exit : Shell_Panel
{
    public Button exit_Button;
    public Button no_Button;

    private void Start()
    {
        exit_Button.onClick.AddListener(() =>
        {
            ScenesManager.Instance.ChanageScene("LobbyScene");
            //TODO:초기화 후에 패널끄기
            shellGameUI.gameObject.SetActive(false);
        });

        no_Button.onClick.AddListener(() =>
        {
            gameObject.SetActive(false);
        });
    }
}
