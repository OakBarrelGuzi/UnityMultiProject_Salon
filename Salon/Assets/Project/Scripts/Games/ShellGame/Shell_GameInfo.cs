using Salon.ShellGame;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class Shell_GameInfo : Shell_Panel
{//TODO:버튼이랑 텍스트 할당하기
    public TextMeshProUGUI round_Text;
    public TextMeshProUGUI timer_Text;
    public TextMeshProUGUI bettingGold_Text;
    public Button setting_Button;

    private void Start()
    {
        setting_Button.onClick.AddListener(() =>
        
            shellGameUI.option_Panel.gameObject.SetActive(true)

        );

      
    }
    private void Update()
    {
        bettingGold_Text.text = shellGameUI.shuffleManager.BettingGold.ToString();
    }
}

