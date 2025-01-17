using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;


namespace Salon.DartGame
{
    public class ReCheckPanel : MonoBehaviour
    {
        public Button rechckYesButton;
        public Button rechckNoButton;

        private void Awake()
        {
            rechckYesButton.onClick.AddListener(() => { 
                ScenesManager.Instance.ChanageScene("LobbyScene");
                UIManager.Instance.ClosePanel(PanelType.Dart);
                gameObject.SetActive(false);
            });
            rechckNoButton.onClick.AddListener(() => gameObject.SetActive(false));
        }


    }


    
}
