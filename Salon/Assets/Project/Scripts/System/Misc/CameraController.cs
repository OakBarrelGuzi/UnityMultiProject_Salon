using UnityEngine;

public class CameraController : MonoBehaviour
{
    [SerializeField] private Transform target;
    [SerializeField] private float smoothSpeed = 5f;

    private Vector3 initialOffset;
    private Quaternion initialRotation;

    private void Start()
    {
        initialOffset = new Vector3(356f, 358f, 356f);
        initialRotation = Quaternion.Euler(35f, -135f, 0f);

        if (target != null)
        {
            transform.position = target.position + initialOffset;
            transform.rotation = initialRotation;
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
            initialOffset = new Vector3(356f, 358f, 356f);
            initialRotation = Quaternion.Euler(35f, -135f, 0f);

            transform.position = target.position + initialOffset;
            transform.rotation = initialRotation;
        }
    }
}