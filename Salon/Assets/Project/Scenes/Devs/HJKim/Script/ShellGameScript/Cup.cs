using Salon.ShellGame;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Cup : MonoBehaviour
{//컵의 상태
    public bool hasBall = false;
    public bool isMoving = false;
    public bool isSelectable = false;

    private ShellGameManager manager;
    //컵의 이동
    private float moveSpeed;
    private Vector3 targetPosition;
    private Vector3 startPosition;
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