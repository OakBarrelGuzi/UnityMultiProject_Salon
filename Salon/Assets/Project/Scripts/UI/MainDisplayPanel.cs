using UnityEngine;
using UnityEngine.UI;

public class MainDisplayPanel : Panel
{
    [SerializeField] private Button startButton;
    [SerializeField] private Button optionButton;
    [SerializeField] private Button customizeButton;

    public override void Initialize()
    {
        this.panelType = PanelType.MainDisplay;

        if (startButton == null || optionButton == null || customizeButton == null)
        {
            GetReferences();
        }
        else
        {
            if (startButton != null)
                startButton.onClick.AddListener(OnStartButtonClick);
            if (optionButton != null)
                optionButton.onClick.AddListener(OnOptionButtonClick);
            if (customizeButton != null)
                customizeButton.onClick.AddListener(OnCustomizeButtonClick);
        }
    }

    private void GetReferences()
    {
        Button[] buttons = GetComponentsInChildren<Button>();
        foreach (Button button in buttons)
        {
            if (button.name == "Main_Start_Button" && startButton == null)
                startButton = button;
            else if (button.name == "Main_Option_Button" && optionButton == null)
                optionButton = button;
            else if (button.name == "Main_Customize_Button" && customizeButton == null)
                customizeButton = button;
        }
    }

    public void OnStartButtonClick()
    {
        if (startButton == null)
        {
            LogManager.Instance.ShowLog("StartButton 이 할당되지 않았습니다.");
            return;
        }
        UIManager.Instance.OpenPanel(PanelType.Lobby);
    }

    public void OnOptionButtonClick()
    {
        if (optionButton == null)
        {
            LogManager.Instance.ShowLog("OptionButton 이 할당되지 않았습니다.");
            return;
        }
        UIManager.Instance.OpenPanel(PanelType.Option);
    }
    public void OnCustomizeButtonClick()
    {
        if (customizeButton == null)
        {
            LogManager.Instance.ShowLog("CustomizeButton 이 할당되지 않았습니다.");
            return;
        }
        UIManager.Instance.OpenPanel(PanelType.Customize);
    }
}