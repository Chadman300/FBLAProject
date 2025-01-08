using UnityEngine;

public class LimbConstraint : MonoBehaviour
{
    public Transform origin; // The origin point (e.g., the limb's base)
    public float maxDistance = 1.0f; // Maximum distance the Rigidbody can move
    private Rigidbody rb;

    void Start()
    {
        rb = GetComponent<Rigidbody>();
    }

    void FixedUpdate()
    {
        // Calculate the offset vector
        var dis = getDistance(origin, gameObject.transform);

        if (dis > maxDistance)
        {
            rb.linearVelocity = new Vector3(rb.linearVelocity.x, 0, rb.linearVelocity.z);
        }

        /*
        // Check if the offset exceeds the maximum allowed distance
        if (offset.x > maxDistance)
        {
            rb.linearVelocity = new Vector3(0, rb.linearVelocity.y, rb.linearVelocity.z);
        }

        

        if (offset.z > maxDistance)
        {
            rb.linearVelocity = new Vector3(rb.linearVelocity.x, rb.linearVelocity.y, 0);
        }
        */
    }

    private float getDistance(Transform t1, Transform t2)
    {
        return Mathf.Sqrt(
            ((t2.position.x - t1.position.x) * (t2.position.x - t1.position.x)) +
            ((t2.position.y - t1.position.y) * (t2.position.y - t1.position.y)) +
            ((t2.position.z - t1.position.z) * (t2.position.z - t1.position.z))
            );
    }
}
