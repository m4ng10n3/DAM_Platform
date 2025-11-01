using UnityEngine;

public class CameraFollow : MonoBehaviour
{
    [Header("Settings")]
    public Vector3 followOffset;

    [Range(0, 1f)] public float smoothSpeed = 0.2f;

    [Header("Components")]
    public Transform playerTransform;

    float zPosition;
    Vector3 currentVelocity = Vector3.zero;
    private void Awake()
    {
        zPosition = transform.position.z;
    }

    public void LateUpdate()
    {
        Vector3 targetPosition = playerTransform.position + followOffset;
        targetPosition.z = zPosition;

        transform.position = Vector3.SmoothDamp(transform.position, targetPosition, ref currentVelocity, smoothSpeed);
    }
}
