using UnityEngine;

public class CameraFollow : MonoBehaviour
{
    [Header("Functional Parameters")]
    [SerializeField] private Transform target;
    [Range(0, 1)] [SerializeField] private float smoothSpeed = 0.125f;
    [Range(0, 10)] [SerializeField] private float buffer = 1f;
    [SerializeField] private Vector3 offset;
    [SerializeField] private Quaternion rotOffset;
    [SerializeField] private bool lookAt = false;

    private void FixedUpdate()
    {
        transform.rotation = rotOffset;
        Vector3 desiredPos = target.position + offset;

        //only move if distance is greater than buffer
        if(getDistance(desiredPos, transform.position) > buffer)
        {
            Vector3 smoothedPos = Vector3.Lerp(transform.position, desiredPos, smoothSpeed * Time.deltaTime * 10f);
            transform.position = smoothedPos;
        }

        if(lookAt)
            transform.LookAt(target);
    }

    private float getDistance(Vector3 p1, Vector3 p2)
    {
        return Mathf.Sqrt(
            ((p2.x - p1.x) * (p2.x - p1.x)) +
            ((p2.y - p1.y) * (p2.y - p1.y)) +
            ((p2.z - p1.z) * (p2.z - p1.z))
            );
    }
}
