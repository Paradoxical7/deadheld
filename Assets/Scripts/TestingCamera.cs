
using UnityEngine;

public class TestingCamera : MonoBehaviour
{
    [Header("Target")]
    public Transform target;

    [Header("Camera Position")]
    public float distance = 4f;
    public float height = 2f;
    public float lookHeight = 1f;

    [Header("Smooth Movement")]
    public float positionSpeed = 8f;
    public float rotationSpeed = 10f;

    [Header("Camera Collision")]
    public LayerMask collisionLayers;
    public float collisionRadius = 0.25f;
    public float collisionPadding = 0.15f;
    public float minimumDistance = 0.3f;

    void LateUpdate()
    {
        if (target == null) return;

        Vector3 pivot = target.position + Vector3.up * lookHeight;

        Vector3 desiredPosition =
            target.position
            - target.forward * distance
            + Vector3.up * height;

        Vector3 offset = desiredPosition - pivot;
        float desiredDistance = offset.magnitude;
        Vector3 direction = offset.normalized;

        float safeDistance = desiredDistance;

        if (Physics.SphereCast(
            pivot,
            collisionRadius,
            direction,
            out RaycastHit hit,
            desiredDistance,
            collisionLayers,
            QueryTriggerInteraction.Ignore))
        {
            safeDistance = Mathf.Max(
                0f,
                hit.distance - collisionPadding
            );
        }

        Vector3 safePosition = pivot + direction * safeDistance;

        float speed = safeDistance < Vector3.Distance(
            pivot, transform.position)
            ? 100f : positionSpeed;

        transform.position = Vector3.Lerp(
            transform.position,
            safePosition,
            1f - Mathf.Exp(-speed * Time.deltaTime)
        );

        Vector3 lookDirection = pivot - transform.position;

        if (lookDirection.sqrMagnitude > 0.001f)
        {
            Quaternion desiredRotation =
                Quaternion.LookRotation(lookDirection);

            transform.rotation = Quaternion.Slerp(
                transform.rotation,
                desiredRotation,
                1f - Mathf.Exp(-rotationSpeed * Time.deltaTime)
            );
        }
    }
}


