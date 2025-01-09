using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Arrow : MonoBehaviour
{
    private void OnTriggerEnter(Collider other)
    {
        if(other.TryGetComponent<Dart>(out Dart dart))
        {
            Vector3 TriggerPoint = other.ClosestPoint(transform.position);

            dart.SetShootPoint(TriggerPoint);
        }
    }

    //private void OnCollisionEnter(Collision collision)
    //{
    //    if (collision.gameObject.TryGetComponent<Dart>(out Dart dart))
    //    {
    //        Vector3 collisionPoint = collision.contacts[0].point;
    //
    //        dart.SetShootPoint(collisionPoint);
    //    }
    //}
}
