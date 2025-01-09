using UnityEngine;

public class TestRotate : MonoBehaviour
{
    [Header("Functional Parameters")]
    [SerializeField] private Camera camera;
    [SerializeField] private float rotSpeed = 10f;
    [SerializeField] private bool lockYAxis = true;
    [SerializeField] private LayerMask castLayer = 1;
    [SerializeField] private ConfigurableJoint hipsJoint;

    private void Update()
    {
        RayRotate();
    }

    private void RayRotate()
    {
        Ray ray = camera.ScreenPointToRay(Input.mousePosition);

        if(Physics.Raycast(ray, out RaycastHit hit, float.MaxValue, castLayer))
        {
            Vector3 targetPoint = hit.point;
            Vector3 direction = targetPoint - transform.position;

            if(lockYAxis)
            {
                direction.y = 0f;
            }

            Quaternion targetRotaion = Quaternion.LookRotation(direction);

            hipsJoint.targetRotation = Quaternion.Slerp(hipsJoint.targetRotation, targetRotaion, rotSpeed * Time.deltaTime);
            Debug.Log(hipsJoint.targetRotation);

            //Debug.DrawLine(transform.position, hit.point, Color.green);
        }
    }

    private Quaternion QuaternionMultiplication(Quaternion q1, Quaternion q2)
    {
        return new Quaternion(q1.x * q2.x, q1.y * q2.y, q1.z * q2.z, q1.w * q2.w);
    }

    private Quaternion QSub(Quaternion q1, Quaternion q2)
    {
        return new Quaternion(q1.x - q2.x, q1.y - q2.y, q1.z - q2.z, q1.w - q2.w);
    }
} 
