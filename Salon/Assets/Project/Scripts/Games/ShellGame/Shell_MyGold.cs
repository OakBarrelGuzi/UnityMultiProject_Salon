using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class Shell_MyGold : Shell_Panel
{
    public TextMeshProUGUI myGold_Text;
    //TODO: 플레이어 골드 연동 

    private void Update()
    {
        myGold_Text.text = shellGameUI.shuffleManager.myGold.ToString();
    }
}
