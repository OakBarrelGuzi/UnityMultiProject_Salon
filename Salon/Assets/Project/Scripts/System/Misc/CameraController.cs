using UnityEngine;

public class CameraController : MonoBehaviour
{
    [SerializeField] private Transform target;
    [SerializeField] private float smoothSpeed = 5f;

    private Vector3 initialOffset;
    private Quaternion initialRotation;

    private void Start()
    {
        if (target != null)
        {
            initialOffset = transform.position - target.position;
            initialRotation = transform.rotation;
        }
    }
    private void LateUpdate()
    {
        if (target == null) return;

        Vector3 desiredPosition = target.position + initialOffset;

        Vector3 smoothedPosition = Vector3.Lerp(transform.position, desiredPosition, smoothSpeed * Time.deltaTime);

        transform.position = smoothedPosition;
        transform.rotation = initialRotation;
    }

    public void SetTarget(Transform newTarget)
    {
        target = newTarget;
        if (target != null)
        {
            initialOffset = transform.position - target.position;
            initialRotation = transform.rotation;
        }
    }
}