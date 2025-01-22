using Salon.ShellGame;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;


public class Shell_Difficult : Shell_Panel
{
    public Button[] difficulty_Button;

    public Button close_Button;

    private void Start()
    {
        difficulty_Button[0].onClick.AddListener((UnityEngine.Events.UnityAction)(() =>
        {
            if (shellGameUI.shuffleManager.CheckGold())
            {
                shellGameUI.shuffleManager.SetDifficulty(SHELLDIFFICULTY.Easy);
                print("���̵� ��ư�� �� �����ϴ�");
                shellGameUI.shuffleManager.SetCup();
                shellGameUI.PanelOpen(shellGameUI.betting_Panel, this);
            }
        }));

        difficulty_Button[1].onClick.AddListener((UnityEngine.Events.UnityAction)(() =>
        {
            if (shellGameUI.shuffleManager.CheckGold())
            {
                shellGameUI.shuffleManager.CheckGold();
                shellGameUI.shuffleManager.SetDifficulty(SHELLDIFFICULTY.Normal);
                shellGameUI.shuffleManager.SetCup();
                shellGameUI.PanelOpen(shellGameUI.betting_Panel, this);
            }
        }));

        difficulty_Button[2].onClick.AddListener((UnityEngine.Events.UnityAction)(() =>
        {
            if (shellGameUI.shuffleManager.CheckGold())
            {
                shellGameUI.shuffleManager.SetDifficulty(SHELLDIFFICULTY.Hard);
                shellGameUI.shuffleManager.SetCup();
                shellGameUI.PanelOpen(shellGameUI.betting_Panel, this);
            }
        }));

        close_Button.onClick.AddListener(() =>
        {
            ScenesManager.Instance.ChanageScene("LobbyScene");
            //TODO: �ʱ�ȭ ������ �гβ���
            shellGameUI.gameObject.SetActive(false);
        });
    }
    private void OnEnable()
    {
        shellGameUI.betting_Panel.close_Button.onClick.RemoveAllListeners();
        shellGameUI.betting_Panel.close_Button.onClick.AddListener(() =>
        {
            shellGameUI.PanelOpen(this, shellGameUI.betting_Panel);
        });
    }



}
