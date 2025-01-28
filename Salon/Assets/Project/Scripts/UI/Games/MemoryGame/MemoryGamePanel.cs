using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MemoryGamePanelUi : Panel
{
    public CardPanel cardPanel;
    public CardResultUi cardResultUi;

    public override void Open()
    {
        base.Open();
        cardPanel.gameObject.SetActive(true);
        cardResultUi.gameObject.SetActive(false);
    }
    public override void Close()
    {
        base.Close();
        cardPanel.gameObject.SetActive(false);
        cardResultUi.gameObject.SetActive(false);
    }
}
