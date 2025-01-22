using System;
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

        public TextMeshProUGUI bettingReturnInfoText;


        public Slider bettingSlider;

        private void Start()
        {

            start_Button.onClick.AddListener(() =>
            {
                shellGameUI.PanelOpen(shellGameUI.gameInfo_Panel,this);//라운드가 적혀있음
                shellGameUI.shuffleManager.StartGame();      // 게임 시작
            });
        }

        private void Update()
        {
            bettingGoldText.text = bettingSlider.value.ToString();
           
        }

        private void OnEnable()
        {
            myGoldText.text = shellGameUI.shuffleManager.myGold.ToString();

            bettingReturnInfoText.text =
                $"{shellGameUI.shuffleManager.difficultyText[shellGameUI.shuffleManager.shellDifficulty]} 난이도를 선택하셨습니다. 성공 보상 배율은{shellGameUI.shuffleManager.bettingReturn[shellGameUI.shuffleManager.shellDifficulty]}배입니다.";

            bettingSlider.minValue = Mathf.Min(shellGameUI.shuffleManager.myGold, 1);

            int maxBettingGold = Mathf.Min(shellGameUI.shuffleManager.myGold,
                shellGameUI.shuffleManager.maxBetting
                [shellGameUI.shuffleManager.shellDifficulty] * 
                shellGameUI.shuffleManager.round);

            bettingSlider.maxValue = maxBettingGold;

            bettingSlider.value = MathF.Min(bettingSlider.minValue, 1);
        }

        private void OnDisable()
        {
            close_Button.onClick.RemoveAllListeners();
            close_Button.onClick.AddListener(() =>
            {
                shellGameUI.PanelOpen(shellGameUI.clear_Panel, this);
            });
            start_Button.onClick.RemoveAllListeners();
            start_Button.onClick.AddListener(() =>
            {
                shellGameUI.PanelOpen(shellGameUI.gameInfo_Panel, this);
                shellGameUI.shuffleManager.NextRound();
            });

            shellGameUI.shuffleManager.BettingGold = (int)bettingSlider.value;
        }
    }
}