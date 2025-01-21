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

        public Slider bettingSlider;

        private void Start()
        {
            close_Button.onClick.AddListener(() =>
            {
                print("�ڷΰ��� ��ư�� ���Ƚ��ϴ�");
                shellGameUI.difficult_Panel.gameObject.SetActive(true);//�ٽ� ���̵� ����â���� ����
                shellGameUI.betting_Panel.gameObject.SetActive(false);
            });

            start_Button.onClick.AddListener(() =>
            {
                shellGameUI.betting_Panel.gameObject.SetActive(false);  // ���� UI �����
                shellGameUI.gameInfo_Panel.gameObject.SetActive(true);//���尡 ��������
                shellGameUI.shuffleManager.StartGame();      // ���� ����
            });
        }

    }
}