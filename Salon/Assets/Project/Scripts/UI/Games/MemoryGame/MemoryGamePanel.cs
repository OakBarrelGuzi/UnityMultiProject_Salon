using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MemoryGamePanelUi : Panel
{
    public CardPanel cardPanel;
    public CardResultUi cardResultUi;

    private void Start()
    {
        cardPanel.gameObject.SetActive(true);
    }
}
