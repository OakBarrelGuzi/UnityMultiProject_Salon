using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class JoyStickFocus : MonoBehaviour
{
    public FocusType focusType;
}

public enum FocusType
{
    TOP,
    BOTTOM,
    LEFT,
    RIGHT
}