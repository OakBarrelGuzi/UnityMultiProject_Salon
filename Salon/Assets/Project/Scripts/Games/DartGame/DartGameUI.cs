using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Salon.UI;

namespace Salon.DartGame
{
    public class DartGameUI : Panel
    {
        public Slider turnTimeSlider;
        public TextMeshProUGUI turnTimeText;

        public Button settingButton;
        public Button shootButton;

        public Transform scoreTextField;
        public TextMeshProUGUI totalScoreText;
        public TextMeshProUGUI recentScoreText;

        public Transform chatTextField;
        public TMP_InputField chatInputField;
        public Button chatSendButton;

        public Slider breathTimeSlider;
        public TextMeshProUGUI scoreTextPrefab;

        public DartRoundPanel gameStartReady;

        public OptionPanel optionPanel;
        public ReCheckPanel reCheckPanel;
        public DartResultPanel dartResultPanel;

        public JoyStick joyStick;

        private void Start()
        {
            optionPanel.Initialize(this);
            settingButton.onClick.AddListener(()=> optionPanel.gameObject.SetActive(true));            
        }
    }
}