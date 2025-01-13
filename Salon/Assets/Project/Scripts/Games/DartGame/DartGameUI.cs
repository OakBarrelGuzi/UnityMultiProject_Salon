using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class DartGameUI : MonoBehaviour
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
}
