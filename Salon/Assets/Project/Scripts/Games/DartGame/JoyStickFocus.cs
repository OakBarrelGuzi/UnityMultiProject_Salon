using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class JoyStickFocus : MonoBehaviour
{
    public FOCUSTYPE focusType;
}

public enum FOCUSTYPE
{
    TOP,
    BOTTOM,
    LEFT,
    RIGHT
}