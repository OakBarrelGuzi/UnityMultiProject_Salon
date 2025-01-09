using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TestScript : MonoBehaviour
{
    public Transform center;

    public Transform Point;

    private void Update()    
    {
        float pos = Vector3.Distance(center.position,Point.position);
        Vector3 pos2 = center.position - Point.position;

        //print(pos);
        //print(pos2.magnitude);

        float angle = Mathf.Atan2(Point.position.y - center.position.y, Point.position.x - center.position.x) * Mathf.Rad2Deg;
        
        float sectionOffset = 9f;
        angle += sectionOffset;
        if (angle < 0)
        {
            angle += 360f;
        }

        //print(angle);
    }
}
