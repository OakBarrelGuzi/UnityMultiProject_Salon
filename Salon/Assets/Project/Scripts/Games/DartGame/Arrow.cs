using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Arrow : MonoBehaviour
{

    private Vector3 targetPosition;

    private Vector3 startPosition;

    private Vector3 heigtPosition;

    [SerializeField, Header("포물선높이"), Range(0f, 1f)]
    private float height = 0.5f;

    [Header("다트속도")]
    public float duration = 0.1f;

    private bool isStop = false;

    private void Start()
    {
        initialize();
    }
    public void initialize()
    {
        startPosition = this.transform.position;

        float distance = Vector3.Distance(startPosition, targetPosition);

        Vector3 direction = (targetPosition - startPosition).normalized;

        Vector3 halfPosition = startPosition + (direction * distance * 0.5f);

        halfPosition.y += height;

        heigtPosition = halfPosition;

        StartCoroutine(ParabolaMove());
    }

    public void SetTargetPosition(Vector3 target)
    {
        targetPosition = target;
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

        while (!isStop && time <= 1f)
        {

            Vector3 p1 = Vector3.Lerp(startPosition, heigtPosition, time);
            Vector3 p2 = Vector3.Lerp(heigtPosition, targetPosition, time);

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
