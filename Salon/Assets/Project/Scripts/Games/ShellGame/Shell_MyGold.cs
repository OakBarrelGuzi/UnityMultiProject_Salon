using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class Shell_MyGold : Shell_Panel
{
    public TextMeshProUGUI myGold_Text;
    //TODO: �÷��̾� ��� ���� 

    private void Update()
    {
        myGold_Text.text = shellGameUI.shuffleManager.myGold.ToString();
    }
}
