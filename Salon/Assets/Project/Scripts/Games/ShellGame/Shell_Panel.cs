using Salon.ShellGame;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Shell_Panel : MonoBehaviour
{
    protected ShellGameUI shellGameUI;

    public virtual void Initialize(ShellGameUI shellGameUI)
    {
        this.shellGameUI = shellGameUI;
    }
}
