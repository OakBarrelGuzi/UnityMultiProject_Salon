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
        //TODO:���� ��� �����ؾ���
        public TextMeshProUGUI myGoldText;

        public TextMeshProUGUI bettingGoldText;

        public TextMeshProUGUI bettingReturnInfoText;


        public Slider bettingSlider;

        private void Start()
        {

            start_Button.onClick.AddListener(() =>
            {
                shellGameUI.PanelOpen(shellGameUI.gameInfo_Panel,this);//���尡 ��������
                shellGameUI.shuffleManager.StartGame();      // ���� ����
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
                $"{shellGameUI.shuffleManager.difficultyText[shellGameUI.shuffleManager.shellDifficulty]} ���̵��� �����ϼ̽��ϴ�. ���� ���� ������{shellGameUI.shuffleManager.bettingReturn[shellGameUI.shuffleManager.shellDifficulty]}���Դϴ�.";

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