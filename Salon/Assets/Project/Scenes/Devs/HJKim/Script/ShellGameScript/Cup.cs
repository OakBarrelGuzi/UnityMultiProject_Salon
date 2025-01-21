using Salon.ShellGame;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Cup : MonoBehaviour
{//컵의 상태
    public bool hasBall = false;

    private ShellGameManager manager;

    public void OnMouseDown()
    {
        if (manager != null)
        {
            manager.OnCupSelected(this);
        }
    }

    public void Initialize(ShellGameManager shuffleManager)
    {
        manager = shuffleManager;
    }
    
}