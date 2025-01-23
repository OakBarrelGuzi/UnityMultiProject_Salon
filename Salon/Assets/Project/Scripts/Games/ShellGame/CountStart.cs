using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CountStart : Shell_Panel
{
    public List<GameObject> counts = new List<GameObject>();

    private void OnDisable()
    {
        foreach(GameObject count in counts)
        {
            count.gameObject.SetActive(false);
        }
    }
}
