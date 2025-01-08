using System;
using UnityEngine;
using UnityEngine.EventSystems;

public class RagdollController : MonoBehaviour
{
    [Header("Movement")]
    [SerializeField] private float speed = 100; //forward back speed
    [SerializeField] private float rotateTourqe = 80; //left right speed
    [SerializeField] private float lookTourqe = 200; //left right speed
    [SerializeField] private float jumpForce = 40;
    private float rotationY = 0;

    [Header("Rotation")]
    [SerializeField] private ConfigurableJoint hipJoint;
    [SerializeField] private ConfigurableJoint spineJoint;
    [SerializeField] private float spineOffset;

    [Header("Ragdoll")]
    [SerializeField] private ConfigurableJoint[] joints;
    [SerializeField] private float driveStiffness = 180;
    [SerializeField] private float driveStiffnessHips = 750;
    [Range(1, 15)][SerializeField] private float stiffnessDividend = 4f;
    [Range(1, 15)][SerializeField] private float massDividend = 4f;

    [Header("Attack Paramenters")]
    public LayerMask enemyLayer;
    public bool canLimbAttack = true;
    public float limbAttackDamage = 10f;

    [Header("Animation")]
    [SerializeField] private Animator anim;

    [Space]
    private Rigidbody hips;
    public bool isGrounded = false;

    //input
    private Vector2 currentInput;
    private Vector2 currentInputRaw;

    private float jumpInput;
    private float jumpInputRaw;

    private Vector2 mouseP;

    void Start()
    {
        hips = GetComponent<Rigidbody>(); 
        
        //lock and hide cursor
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        RagDoll(true);
    }

    private void Update()
    {
        HandleMovementInput();
        HandleLook();
    }

    private void FixedUpdate()
    {
        HandleAnimation();
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
            
            var moveVel =  hips.transform.forward * currentInput.x * Time.deltaTime;
            hips.linearVelocity = new Vector3(moveVel.x, hips.linearVelocity.y, moveVel.z);

            //hips.AddForce(hips.transform.forward * currentInput.x * Time.deltaTime); //forawrd back
            //hips.AddForce(hips.transform.right * currentInput.y * Time.deltaTime); //strafe

            //rotation
            //hipJoint.targetRotation = Quaternion.Euler(0, -mouseP.x * Time.deltaTime, 0); //mouse
            rotationY -= (currentInput.y * rotateTourqe) * Time.deltaTime;
            hipJoint.targetRotation = Quaternion.Euler(0, rotationY, 0); //keys

            //Jumping
            hips.AddForce(hips.transform.up * jumpInput * Time.deltaTime, ForceMode.Impulse);

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

        //attack
        if (Input.GetMouseButtonDown(0)) //swingdown
        {
            anim.SetTrigger("swing");
        }
    }

    private void HandleMovementInput()
    {
        currentInput = new Vector2(speed * Input.GetAxis("Vertical"), rotateTourqe * Input.GetAxis("Horizontal"));
        currentInputRaw = new Vector2(Input.GetAxisRaw("Vertical"), Input.GetAxisRaw("Horizontal"));

        if(isGrounded)
        {
            jumpInput = jumpForce * Input.GetAxis("Jump");
            jumpInputRaw = Input.GetAxis("Jump");
        }
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

            //hips
            joint = GetComponent<ConfigurableJoint>();

            yzDrive = joint.angularYZDrive;
            xDrive = joint.angularXDrive;

            yzDrive.positionSpring = driveStiffnessHips;
            xDrive.positionSpring = driveStiffnessHips;

            joint.angularYZDrive = yzDrive;
            joint.angularXDrive = xDrive;

            //renable anim
            anim.enabled = true;
        }
    }
}
