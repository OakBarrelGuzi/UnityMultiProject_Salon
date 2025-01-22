using Salon.ShellGame;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;


public class Shell_Clear : Shell_Panel
{
    public Button lobby_Button;
    //TODO: 배팅골드 할당
    public Button editBetting_Button;
    public Button go_Button;
    //TODO:배팅 텍스트 할당 두개
    public TextMeshProUGUI bettingGold_Text;
    public Toggle All_betting_Toggle;
    public TextMeshProUGUI clearRound_Text;


    private void Start()
    {
        lobby_Button.onClick.AddListener(() =>
        {
            ScenesManager.Instance.ChanageScene("LobbyScene");
            //TODO:초기화 진행
            shellGameUI.gameObject.SetActive(false);
        });
        editBetting_Button.onClick.AddListener(() =>
        {
            //TODO:배팅 UI텍스트 넣기
            shellGameUI.betting_Panel.gameObject.SetActive(true);
            gameObject.SetActive(false);
        });

    }
    private void OnEnable()
    {
        shellGameUI.betting_Panel.close_Button.onClick.RemoveAllListeners();
        shellGameUI.betting_Panel.close_Button.onClick.AddListener(() =>
        {
            gameObject.SetActive(true);
            shellGameUI.betting_Panel.gameObject.SetActive(false);

        });
    }
}
 