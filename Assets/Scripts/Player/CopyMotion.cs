using UnityEngine;

public class CopyMotion : MonoBehaviour
{
    [SerializeField] private Transform targetLimb; // limb it needs to follow
    [SerializeField] private ConfigurableJoint joint;
    [SerializeField] private bool inverse;

    Quaternion startRot;

    void Start()
    {
        joint = GetComponent<ConfigurableJoint>();
        startRot = transform.localRotation;
    }

    void Update()
    {
        if (!inverse) joint.targetRotation = targetLimb.localRotation * startRot;
        else joint.targetRotation = Quaternion.Inverse(targetLimb.localRotation) * startRot;
    }
}
