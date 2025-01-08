using UnityEngine;
using UnityEngine.UI;

public class MainDisplayPanel : Panel
{
    private Button startButton;
    private Button optionButton;
    private Button customizeButton;

    public override void Initialize()
    {
        Button[] buttons = GetComponentsInChildren<Button>();
        foreach (Button button in buttons)
        {
            if (button.name == "Main_Start_Button")
                startButton = button;
            else if (button.name == "Main_Option_Button")
                optionButton = button;
            else if (button.name == "Main_Customize_Button")
                customizeButton = button;
        }
    }

    public void OnStartButtonClick()
    {
        UIManager.Instance.OpenPanel(PanelType.Lobby);
    }

    public void OnOptionButtonClick()
    {
        UIManager.Instance.OpenPanel(PanelType.Option);
    }
    public void OnCustomizeButtonClick()
    {
        UIManager.Instance.OpenPanel(PanelType.Customize);
    }
}