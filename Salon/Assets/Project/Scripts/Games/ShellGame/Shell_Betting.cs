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
            close_Button.onClick.AddListener(() =>
            {
                print("뒤로가기 버튼이 눌렸습니당");
                shellGameUI.difficult_Panel.gameObject.SetActive(true);//다시 나이도 선택창으로 가기
                shellGameUI.betting_Panel.gameObject.SetActive(false);
            });

            start_Button.onClick.AddListener(() =>
            {
                shellGameUI.betting_Panel.gameObject.SetActive(false);  // 배팅 UI 숨기기
                shellGameUI.gameInfo_Panel.gameObject.SetActive(true);//라운드가 적혀있음
                shellGameUI.shuffleManager.StartGame();      // 게임 시작
            });
        }

    }
}