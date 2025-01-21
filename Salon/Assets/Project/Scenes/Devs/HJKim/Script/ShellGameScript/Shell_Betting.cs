using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Salon.ShellGame
{
    public class Shell_Betting : Shell_Panel
    {
        public Button close_Button;

        public Button start_Button;
        //TODO:배팅 골드 구현해야함
        public TextMeshProUGUI myGoldText;

        public TextMeshProUGUI bettingGoldText;

        public Slider bettingSlider;

        private void Start()
        {

            start_Button.onClick.AddListener(() =>
            {
                shellGameUI.betting_Panel.gameObject.SetActive(false);  // 배팅 UI 숨기기
                shellGameUI.gameInfo_Panel.gameObject.SetActive(true);//라운드가 적혀있음
                shellGameUI.shuffleManager.SetCup();
                shellGameUI.shuffleManager.StartGame();      // 게임 시작
                start_Button.onClick?.RemoveListener(shellGameUI.shuffleManager.SetCup);
            });
        }

    }
}