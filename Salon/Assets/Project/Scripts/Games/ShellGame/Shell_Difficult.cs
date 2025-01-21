using Salon.ShellGame;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using static ShellGameDiffi.Difficult;

public class Shell_Difficult : Shell_Panel
{
    public Button[] difficulty_Button;

    public Button close_Button;

    private void Start()
    {
        difficulty_Button[0].onClick.AddListener(() =>
        {
            shellGameUI.shuffleManager.SetDifficulty(SHELLDIFFICULTY.Easy);
            print("나이도 버튼은 잘 눌립니다");

            shellGameUI.ShowBettingUI();
        });

        difficulty_Button[1].onClick.AddListener(() =>
        {
            shellGameUI.shuffleManager.SetDifficulty(SHELLDIFFICULTY.Normal);
            shellGameUI.ShowBettingUI();
        });

        difficulty_Button[2].onClick.AddListener(() =>
        {
            shellGameUI.shuffleManager.SetDifficulty(SHELLDIFFICULTY.Hard);
            shellGameUI.ShowBettingUI();
        });

        close_Button.onClick.AddListener(() =>
        {
            ScenesManager.Instance.ChanageScene("LobbyScene");
            //TODO: 초기화 진행후 패널끄기
            shellGameUI.gameObject.SetActive(false);
        });
    }
}
