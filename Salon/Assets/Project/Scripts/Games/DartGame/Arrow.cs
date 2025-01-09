using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public class Arrow : MonoBehaviour
{
    [SerializeField]
    public Transform targetPosition;//{ get; set; }

    private Vector3 startPosition;

    private Vector3 heigtPosition;

    [SerializeField,Header("곡선 정도"),Range(0f,1f)]
    private float height = 0.5f;

    [SerializeField, Header("가는데 걸리는 시간")]
    private float duration=0.1f;

    private bool isStop = false;

    private void Start()
    {
        startPosition = this.transform.position;

        float distance = Vector3.Distance(startPosition, targetPosition.position);

        Vector3 direction = (targetPosition.position - startPosition).normalized;

        Vector3 halfPosition = startPosition + (direction * distance * 0.5f);

        halfPosition.y += height;

        heigtPosition = halfPosition;

        StartCoroutine(ParabolaMove());
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.TryGetComponent<Dart>(out Dart dart))
        {
            isStop = true;

            Vector3 TriggerPoint = other.ClosestPoint(transform.position);

            dart.SetShootPoint(TriggerPoint);
            print(TriggerPoint);
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

    public IEnumerator ParabolaMove()
    {

        float time = 0f;

        while (!isStop)
        {

            Vector3 p1 = Vector3.Lerp(startPosition,heigtPosition,time);
            Vector3 p2 = Vector3.Lerp(heigtPosition,targetPosition.position,time);

            Vector3 previousPos = transform.position;
            transform.position = Vector3.Lerp(p1, p2, time);

            Vector3 moveDirection = (transform.position - previousPos).normalized;
            if (moveDirection != Vector3.zero)
            {
                transform.rotation = Quaternion.LookRotation(moveDirection);
            }

            time += Time.deltaTime / duration;

            yield return null;
        }
    }
}
