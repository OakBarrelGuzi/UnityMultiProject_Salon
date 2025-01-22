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
        go_Button.onClick.AddListener(() =>
        {
            shellGameUI.shuffleManager.NextRound();

            if (All_betting_Toggle.isOn == true)
            {
               shellGameUI.shuffleManager.BettingGold = 
                Mathf.Min(shellGameUI.shuffleManager.maxBetting[shellGameUI.shuffleManager.shellDifficulty] 
                * shellGameUI.shuffleManager.round,
                shellGameUI.shuffleManager.myGold);
            }
            shellGameUI.PanelOpen(shellGameUI.gameInfo_Panel, this);
        });
    }

    private void Update()
    {
        if (All_betting_Toggle.isOn == true)
        {
            int maxBettingGold = Mathf.Min(shellGameUI.shuffleManager.myGold,
                shellGameUI.shuffleManager.maxBetting
                [shellGameUI.shuffleManager.shellDifficulty] *
                shellGameUI.shuffleManager.round);

            shellGameUI.shuffleManager.BettingGold = maxBettingGold;
            bettingGold_Text.text = maxBettingGold.ToString();
        }
        else
        {
            bettingGold_Text.text = shellGameUI.shuffleManager.BettingGold.ToString();
        }
    }

}
 