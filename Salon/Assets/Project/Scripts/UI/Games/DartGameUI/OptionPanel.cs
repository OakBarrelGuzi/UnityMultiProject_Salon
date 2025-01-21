using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Salon.DartGame
{
    public class OptionPanel : MonoBehaviour
    {
        private DartGameUI gameUI;

        public Button GiveupButton;
        public Button ContinueButton;

        public void Initialize(DartGameUI ui)
        {
            gameUI = ui;

            GiveupButton.onClick.AddListener(() =>
            {
                gameObject.SetActive(false);
                gameUI.reCheckPanel.gameObject.SetActive(true);
            });

            ContinueButton.onClick.AddListener(() => gameObject.SetActive(false));
        }
    }
}