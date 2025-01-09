using System.Collections;
using System.Runtime.Serialization.Formatters;
using Unity.VisualScripting;
using UnityEditor;
using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.InputSystem;
using UnityEngine.UIElements;

public class AdvancedRagdollController : MonoBehaviour
{    
    public AdvancedRagdollSettings settings;

    [Header("Movement")]
    [SerializeField] private float speed = 250; //forward back speed
    [SerializeField] private float rotateTourqe = 15; //left right speed
    [SerializeField] private bool mouseLook = true;
    [SerializeField] private float jumpForce = 3000;
    [Range(1, 20)][SerializeField] private float lungeForce = 2;
    public bool isGrounded = false;

    private float rotationY = 0;

    [Header("Mouse Look")]
    [SerializeField] private float lookTourqe = 200; //left right speed
    [SerializeField] private Camera playerCamera;
    [SerializeField] private LayerMask lookLayer;
    [SerializeField] private bool lockYAxis = true;

    [Header("Physics Parameters")]
    [SerializeField] private int limbCollisionLayer = 6;
    [SerializeField] private Rigidbody[] rigidbodies;
    [SerializeField] private ConfigurableJoint[] joints;
    [Tooltip("IMPORTANT: joints and animTrans must both have each respective joint in the same order and hips at the end of joints")]
    [SerializeField] private int solverIterations = 12;
    [Tooltip("Higher # the more accurate physics interactions are")]
    [SerializeField] private int velSolverIterations = 12;
    [Tooltip("Higher # the more accurate physics interactions are")]
    [SerializeField] private int maxAngularVelocity = 20;
    [Tooltip("Generally dipicts how fast your player can move")]

    private Quaternion[] jointsInitialStartRot;

    [Header("Balance")]
    public Rigidbody hipsRb;
    [SerializeField] private ConfigurableJoint hipJoint;
    [SerializeField] private float uprightTorque = 10000;
    [Tooltip("Defines how much torque percent is applied given the inclination angle percent [0, 1]")]
    [SerializeField] private AnimationCurve uprightTorqueFunction;
    [SerializeField] private float rotationTorque = 500;
    public Vector3 TargetDirection { get; set; }

    [Header("Animation")]
    [SerializeField] private Animator anim;
    [SerializeField] private Transform[] animTransforms;

    [Header("Ragdoll")]
    [SerializeField] private ConfigurableJoint[] legJoints;
    [SerializeField] private float driveStiffness = 90;
    [SerializeField] private float driveStiffnessLegs = 180;
    [Range(1, 15)][SerializeField] private float stiffnessDividend = 4f;
    [Range(1, 15)][SerializeField] private float massDividend = 4f;

    [Header("Attack Paramenters")]
    public LayerMask enemyLayer;
    public bool canLimbAttack = true;
    public float limbAttackDamage = 10f;
    public float limbDamageThreshold = 5f;
    [Tooltip("Minimum attack damage that will hurt enemy")]
    public float limbDamageAttackDelay = 0.1f;
    [Tooltip("Duration after limb attack were you cannot deal limb damage")]
    [Range(0, 10)] public float limbVelocityDividend = 1f;

    [Header("Grabbing")]
    [SerializeField] private bool canGrab = true;
    [SerializeField] private bool canRaiseHand = true;
    [SerializeField] private LayerMask grabbableObjects;
    [SerializeField] private Rigidbody rightHandRb;
    [SerializeField] private Rigidbody leftHandRb;
    [SerializeField] private float grabBreakForce = 1000f;
    [Tooltip("How much force is required to break off connected body of joint")]
    [SerializeField] private bool rightHandUp = false;
    [SerializeField] private bool leftHandUp = false;

    private GameObject grabbedObjRight;
    private GameObject grabbedObjLeft;

    [Header("Picking Up")]
    [SerializeField] private bool canPickUp = true;
    [SerializeField] private Transform rightHandTransform;
    [SerializeField] private Transform leftHandTransform;
    [SerializeField] private string pickUpTag;
    [SerializeField] private float pickRadius = 3f;
    [SerializeField] private Quaternion pickRotOffset;

    public bool leftHandHasItem = false;
    public bool rightHandHasItem = false;
    private bool leftHandHasGun = false;
    private bool rightHandHasGun = false;
    private GameObject leftHandItemObj = null;
    private GameObject rightHandItemObj = null;

    [Space]

    //input
    private Vector2 currentInput;
    private Vector2 currentInputRaw;

    private float jumpInput;
    private float jumpInputRaw;

    private Vector2 mouseP;

    void Start()
    {
        //camera
        //playerCamera = GetComponent<Camera>();

        //settings
        if(!TryGetComponent<AdvancedRagdollSettings>(out settings))
        {
            Debug.LogError($"{this} is missing AdvancedRagdollSettings reference");
        }

        //set physics params
        foreach (Rigidbody rb in rigidbodies)
        {
            rb.solverIterations = solverIterations;
            rb.solverVelocityIterations = velSolverIterations;
            rb.maxAngularVelocity = maxAngularVelocity;
        }

        //save start rots
        jointsInitialStartRot = new Quaternion[joints.Length];
        for (int i = 0; i < joints.Length; i++)
        {
            jointsInitialStartRot[i] = joints[i].transform.localRotation;
        }
    }

    private void Update()
    {
        HandleMovementInput();
        HandleLook();
        HandleAnimation();

        if (canGrab)
            TryGrab();

        if (canPickUp)
            TryPickUp();

        //set animimated rots to joints to animate
        for (int i = 0; i < joints.Length; i++)
        {
            ConfigurableJointExtensions.SetTargetRotationLocal(joints[i], animTransforms[i].localRotation, jointsInitialStartRot[i]);
        }
    }

    private void FixedUpdate()
    {
        if(mouseLook && isGrounded)
            RayRotate();

        Balance();

        ApplyFinalMovements();
    }

    private void HandleLook()
    {
        mouseP.x += Input.GetAxis("Mouse X") * lookTourqe;
        mouseP.y -= Input.GetAxis("Mouse Y") * lookTourqe;
    }

    private void ApplyFinalMovements()
    {
        if (isGrounded)
        {
            //Strafe
            var moveVel = hipsRb.transform.forward * currentInput.x * Time.deltaTime;
            var sideMoveVel = Vector3.zero;

            if(mouseLook)
            {
                sideMoveVel = hipsRb.transform.right * currentInput.y * Time.deltaTime;
            }

            //when add lunge force if jumping and moving forward
            if (jumpInputRaw > 0 && currentInputRaw.x > 0)
                moveVel *= lungeForce;

            hipsRb.linearVelocity = new Vector3(moveVel.x + sideMoveVel.x, hipsRb.linearVelocity.y, moveVel.z + sideMoveVel.z);

            //rotation
            if(!mouseLook)
            {
                rotationY -= (currentInput.y * rotateTourqe) * Time.deltaTime;
                //hipJoint.targetRotation = Quaternion.Euler(0, rotationY, 0); //keys
            }

            //Jumping
            hipsRb.AddForce(hipsRb.transform.up * jumpInput * Time.deltaTime, ForceMode.Impulse);

            //When grounded stiffen ragdoll joins
            RagDoll(false);
        }
        else
        {
            //When not grounded ragdoll
            RagDoll(true);
        }

        if (jumpInputRaw > 0)
            isGrounded = false;
    }

    private void HandleAnimation()
    {
        //run
        if (currentInputRaw.x > 0) //forwards
        {
            anim.SetBool("isRun", true);
        }

        else if (currentInputRaw.x < 0) //backward
        {

        }
        else
        {
            anim.SetBool("isRun", false);
        }

        //strafe
        /*
        if (currentInputRaw.y > 0) //right
        {
            anim.SetBool("isRight", true);
            anim.SetBool("isLeft", false);
        }
        else if (currentInputRaw.y < 0) //left
        {
            anim.SetBool("isLeft", true);
            anim.SetBool("isRight", false);
        }
        else
        {
            anim.SetBool("isLeft", false);
            anim.SetBool("isRight", false);
        }
        */

        //right arm
        if(rightHandUp)
        {
            //anim.SetTrigger("swing");
            if(rightHandHasGun)
            {
                anim.SetBool("isRightAim", true);
                anim.SetBool("isRightHandUp", false);
            }
            else
                anim.SetBool("isRightHandUp", true);
        }
        else
        {      
            anim.SetBool("isRightHandUp", false);
            anim.SetBool("isRightAim", false);
        }

        //left arm
        if (leftHandUp) 
        {
            if (leftHandHasGun)
            {
                anim.SetBool("isLeftAim", true);
                anim.SetBool("isLeftHandUp", false);
            }
            else
                anim.SetBool("isLeftHandUp", true);
        }
        else
        {
            anim.SetBool("isLeftHandUp", false);
            anim.SetBool("isLeftAim", false);
        }
    }

    private void HandleMovementInput()
    {
        //DEBUG : dellete latyer
        if(Input.GetKeyDown(KeyCode.F))
        {
            if(rightHandItemObj)
                Drop(true, rightHandTransform, rightHandRb);
            if(leftHandItemObj)
                Drop(false, leftHandTransform, leftHandRb);
        }

        //grabbing
        if (canRaiseHand)
        {
            if (Input.GetKey(settings.raiseRightHandKey) || (rightHandHasItem && rightHandHasGun))  //right arm
            { rightHandUp = true; }
            else
            { rightHandUp = false; }

            if (Input.GetKey(settings.raiseLeftHandKey) || (leftHandHasItem && leftHandHasGun))  //right arm
            { leftHandUp = true; }
            else
            { leftHandUp = false; }
        }
        else
        {
            rightHandUp = false;
            leftHandUp = false;
        }

        //move
        float horizonalMultiplyer = mouseLook ? speed / 2 : rotateTourqe;
        currentInput = new Vector2(speed * Input.GetAxis("Vertical"), horizonalMultiplyer * Input.GetAxis("Horizontal"));
        currentInputRaw = new Vector2(Input.GetAxisRaw("Vertical"), Input.GetAxisRaw("Horizontal"));

        if (isGrounded)
        {
            jumpInput = jumpForce * Input.GetAxis("Jump");
            jumpInputRaw = Input.GetAxis("Jump");
        }
    }

    private void TryPickUp()
    {
        //right hand
        if (rightHandUp && !rightHandHasItem)
        {
            Equip(true, rightHandTransform, rightHandRb);
        }

        //left hand
        if (leftHandUp && !leftHandHasItem)
        {
            Equip(false, leftHandTransform, leftHandRb);
        }
    }

    private void Equip(bool isRightHand, Transform handTransform, Rigidbody rb)
    {
        Collider[] colliders = null;
        MeeleWeapon meeleScript;
        GunController gunScript;
        GameObject currentObject;

        colliders = Physics.OverlapSphere(handTransform.position, pickRadius);

        foreach (Collider collider in colliders)
        {
            if (isRightHand)
            {
                rightHandItemObj = collider.gameObject;
                currentObject = rightHandItemObj;
            }
            else
            { 
                leftHandItemObj = collider.gameObject;
                currentObject = leftHandItemObj;
            }

            if (currentObject.CompareTag(pickUpTag))
            {
                meeleScript = currentObject.GetComponent<MeeleWeapon>();
                gunScript = currentObject.GetComponent<GunController>();

                //gun 
                if (gunScript != null)
                {
                    //make sure weapons not already equipt
                    if (gunScript.isEquipt)
                        return;

                    gunScript.isEquipt = true;
                    gunScript.playerRb = hipsRb;

                    //set has gun in hand
                    if (isRightHand)
                    {
                        rightHandHasGun = true;
                        gunScript.isRightHand = true;
                    }
                    else
                    {
                        leftHandHasGun = true;
                        gunScript.isRightHand = false;
                    }
                        
                }
                //meele weapon
                else if (meeleScript != null)
                {
                    //make sure weapons not already equipt
                    if (meeleScript.isEquipt)
                        return;

                    meeleScript.enabled = true;
                    meeleScript.isEquipt = true;
                    meeleScript.canAttack = true;

                    //reset has gun in hand
                    if (isRightHand)
                        rightHandHasGun = false;
                    else
                        leftHandHasGun = false;
                }

                if (isRightHand)
                    rightHandHasItem = true;
                else
                    leftHandHasItem = true;

                currentObject.transform.parent = rb.gameObject.transform;
                currentObject.transform.localPosition = Vector3.zero;
                currentObject.transform.localRotation = pickRotOffset;

                currentObject.layer = limbCollisionLayer;

                //pick item
                FixedJoint joint;
                if (!TryGetComponent<FixedJoint>(out joint))
                    joint = currentObject.AddComponent<FixedJoint>();

                joint.connectedBody = rb;
                joint.connectedMassScale = 0.5f;

                return;
            }
        }
    }

    private void Drop(bool isRightHand, Transform handTransform, Rigidbody rb)
    {
        var currentObject = isRightHand ? rightHandItemObj : leftHandItemObj;
        var currentJoint = currentObject.GetComponent<FixedJoint>();

        var meeleScript = currentObject.GetComponent<MeeleWeapon>();
        var gunScript = currentObject.GetComponent<GunController>();

        //gun 
        if (gunScript != null)
        {
            gunScript.isEquipt = false;
        }
        //meele weapon
        else if (meeleScript != null)
        {
            meeleScript.enabled = false;
            meeleScript.isEquipt = false;
            meeleScript.canAttack = false;
        }

        if (isRightHand)
        {
            rightHandHasItem = false;
            rightHandHasGun = false;
        }
        else
        {
            leftHandHasGun = false;
            leftHandHasItem = false;
        }
        
        currentObject.layer = 0; 
        currentObject.transform.parent = null;

        currentJoint.connectedBody = null;
    }

    private Quaternion AddQuaternions(Quaternion q1, Quaternion q2)
    {
        return new Quaternion(q1.x + q2.x, q1.y + q2.y, q1.z + q2.z, q1.w + q2.w);
    }

    private void TryGrab()
    {
        FixedJoint fixedJointR = null;
        FixedJoint fixedJointL = null;
        RaycastHit hit;
        float sphereSize = 10;

        //right hand
        if (rightHandUp)
        {
            //get obj
            Physics.SphereCast(rightHandTransform.transform.position, sphereSize, rightHandRb.transform.forward, out hit, grabbableObjects);
            if(hit.collider != null)
                grabbedObjRight = hit.collider.gameObject;

            //set joints and stuff
            if (grabbedObjRight != null)
            {
                Debug.Log("Grabbed");
                fixedJointR = grabbedObjRight.AddComponent<FixedJoint>();
                if (fixedJointR.connectedBody == null)
                {
                    fixedJointR.connectedBody = rightHandRb;
                    fixedJointR.breakForce = grabBreakForce;
                }
            }
        }
        else
        {
            if (fixedJointR != null)
                Destroy(grabbedObjRight.GetComponent<FixedJoint>());

            grabbedObjRight = null;
        }

        //left hand
        if (leftHandUp)
        {
            //get obj
            Physics.SphereCast(leftHandTransform.transform.position, sphereSize, leftHandRb.transform.forward, out hit, grabbableObjects);
            if (hit.collider != null)
                grabbedObjLeft = hit.collider.gameObject;

            //set joints and stuff
            if (grabbedObjLeft != null)
            {
                Debug.Log("Grabbed");
                fixedJointL = grabbedObjLeft.AddComponent<FixedJoint>();
                if (fixedJointL.connectedBody == null)
                {
                    fixedJointL.connectedBody = leftHandRb;
                    fixedJointL.breakForce = grabBreakForce;
                }
            }     
        }
        else
        {
            if (fixedJointL != null)
                Destroy(grabbedObjLeft.GetComponent<FixedJoint>());

            grabbedObjLeft = null;
        }
    }

    private void Balance()
    {
        //balance using upright tourqe
        var balancePercent = Vector3.Angle(hipsRb.transform.up, Vector3.up) / 180;
        balancePercent = uprightTorqueFunction.Evaluate(balancePercent);
        var rot = Quaternion.FromToRotation(hipsRb.transform.up, Vector3.up).normalized;
        //rot = new Quaternion(rot.x + targetLookRotation.x, rot.y + targetLookRotation.y, rot.z + targetLookRotation.z, rot.w + targetLookRotation.w);

        hipsRb.AddTorque(new Vector3(rot.x, rot.y, rot.z) * uprightTorque * balancePercent);

        var directionAnglePercent = Vector3.SignedAngle(hipsRb.transform.forward,
                            TargetDirection, Vector3.up) / 180;
        hipsRb.AddRelativeTorque(0, directionAnglePercent * rotationTorque, 0);
    }

    private void RayRotate()
    {
        Ray ray = playerCamera.ScreenPointToRay(Input.mousePosition);

        if (Physics.Raycast(ray, out RaycastHit hit, float.MaxValue, lookLayer))
        {
            Vector3 targetPoint = hit.point;
            Vector3 direction = targetPoint - transform.position;

            if (lockYAxis)
            {
                direction.y = 0f;
            }

            Quaternion targetRotaion = Quaternion.LookRotation(direction);
            targetRotaion.w = -targetRotaion.w;

            //hipJoint.targetRotation = targetRotaion;
            hipJoint.targetRotation = Quaternion.Slerp(hipJoint.targetRotation, targetRotaion, lookTourqe * Time.deltaTime);

            //Debug.DrawLine(transform.position, hit.point, Color.green);
        }
    }

    private Vector3 vectorM(Vector3 v1, Vector3 v2)
    {
        return new Vector3(v1.x * v2.x, v1.y * v2.y, v1.z * v2.z);
    }

    private void RagDoll(bool ragdoll)
    {
        ConfigurableJoint joint; JointDrive yzDrive; JointDrive xDrive;

        if (ragdoll)
        {
            //ragdoll by destiffining joints
            for (int i = 0; i < joints.Length; i++)
            {
                joint = joints[i];

                yzDrive = joint.angularYZDrive;
                xDrive = joint.angularXDrive;

                yzDrive.positionSpring = driveStiffness / stiffnessDividend;
                xDrive.positionSpring = driveStiffness / stiffnessDividend;

                joint.angularYZDrive = yzDrive;
                joint.angularXDrive = xDrive;

                joint.massScale = 1.6f / massDividend;
            }

            //disable anim
            anim.enabled = false;
        }
        else
        {
            //dont ragdoll
            for (int i = 0; i < joints.Length; i++)
            {
                joint = joints[i];

                yzDrive = joint.angularYZDrive;
                xDrive = joint.angularXDrive;

                yzDrive.positionSpring = driveStiffness;
                xDrive.positionSpring = driveStiffness;

                joint.angularYZDrive = yzDrive;
                joint.angularXDrive = xDrive;

                joint.massScale = 1.6f;
            }

            //legs
            for (int i = 0; i < legJoints.Length; i++)
            {
                yzDrive = legJoints[i].angularYZDrive;
                xDrive = legJoints[i].angularXDrive;

                yzDrive.positionSpring = driveStiffnessLegs;
                xDrive.positionSpring = driveStiffnessLegs;

                legJoints[i].angularYZDrive = yzDrive;
                legJoints[i].angularXDrive = xDrive;
            }

            //renable anim
            anim.enabled = true;
        }
    }

    public IEnumerator LimbDelay()
    {
        canLimbAttack = false;
        yield return new WaitForSeconds(limbDamageAttackDelay);
        canLimbAttack = true;
    }

    private void OnDrawGizmos()
    {
        Gizmos.DrawWireSphere(rightHandTransform.position, pickRadius);
        Gizmos.DrawWireSphere(leftHandTransform.position, pickRadius);
    }
}
