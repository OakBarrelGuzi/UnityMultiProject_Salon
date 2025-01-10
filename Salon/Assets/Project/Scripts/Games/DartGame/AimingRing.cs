using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AimingRing : MonoBehaviour
{
    public Vector3 startScale;

    public Transform innerLine;

    public Transform outerLine;

    private void Awake()
    {
        startScale = transform.localScale;
    }

}
