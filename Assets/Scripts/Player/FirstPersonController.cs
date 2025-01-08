using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using Controls;
using System;
using UnityEngine.UI;
using UnityEditor.Build;
using MoreMountains.Feedbacks;
using DG.Tweening;

public class FirstPersonController : MonoBehaviour
{
    public bool CanMove { get; private set; } = true;
    //doing this instead of putting two ifs VVVV
    [HideInInspector] public bool isSprinting => canSprint && Input.GetKey(controls.SprintKey);
    private bool shouldJump => Input.GetKeyDown(controls.JumpKey) && characterController.isGrounded;
    private bool shouldCrouch => (crouchKeyUp ? Input.GetKeyUp(controls.CrouchKey) : Input.GetKeyDown(controls.CrouchKey)) && !duringCrouchAnimation && characterController.isGrounded;
    private bool canLeanLeft => Input.GetKey(controls.LeanLeftKey);
    private bool canLeanRight => Input.GetKey(controls.LeanRightKey);

    [Header("Functional Options")]
    [SerializeField] private bool canLook = false;
    [SerializeField] private bool canSprint = true;
    [SerializeField] private bool canJump = true;
    [SerializeField] private bool canCrouch = true;
    [SerializeField] private bool willSlideOnSlopes = true;
    [Space(5)]
    [SerializeField] private bool canUseHeadBob = true;
    [SerializeField] private bool canZoom = true;
    [SerializeField] private bool canInteract = true;
    [SerializeField] private bool useFootSteps = true;
    [Space(5)]
    [SerializeField] private bool canLean = true;
    [SerializeField] private bool walkingCameraTilt = true;
    [SerializeField] private bool cameraTilt = true;

    [Header("Effects")]
    [SerializeField] private MMFeedbacks jumpFeedBack;
    [SerializeField] private MMFeedbacks landFeedBack;
    [Space(5)]
    public MMFeedbacks wallrunFeedBack;
    [SerializeField] private MMFeedbacks slideFeedBack;
    [Space(5)]
    [SerializeField] private MMFeedbacks crouchFeedBack;
    [SerializeField] private MMFeedbacks crouchToStandFeedBack;
    [Space(5)]
    [SerializeField] private MMFeedbacks regenerateHealthFeedBack;
    [SerializeField] private MMFeedbacks damageFeedBack;
    [SerializeField] private MMFeedbacks deathFeedBack;

    [Header("Movement Parameters")]
    [SerializeField] private float mass = 10f;
    [SerializeField] private float walkSpeed = 3.0f;
    [SerializeField] private float sprintSpeed = 6.0f;
    [SerializeField] private float crouchSpeed = 1.5f;
    [SerializeField] private float slopeSpeed = 8f;
    [SerializeField] private LayerMask whatIsGround;
    public bool useGravity = true;

    [Header("Controls")]
    [SerializeField] private PlayerControls controls;

    [Header("Look Parameters")]
    [SerializeField, Range(1, 10)] private float lookSpeedX = 2;
    [SerializeField, Range(1, 10)] private float lookSpeedY = 2;
    [SerializeField, Range(-90, 180)] private float upperLookLimit = 80f;
    [SerializeField, Range(-90, 180)] private float lowerLookLimit = 80f;

    [Header("Health Parameters")]
    [SerializeField] private float maxHealth = 100f;
    [SerializeField] private float timeBeforeRegenStarts = 3f;
    [SerializeField] private float healthValueIncrement = 3;
    [Tooltip("Increments currentHealth by healthValueIncrement every time of this value")]
    [SerializeField] private float healthTimeIncrement = 0.1f;
    [SerializeField] private Slider HealthSlider;
    private float currentHealth;
    private Coroutine regeneratingHealth;
    public static Action<float> OnTakeDamage;
    public static Action<float> OnDamage;
    public static Action<float> OnHeal;

    [Header("Jumping Parameters")]
    [SerializeField] private float jumpForce = 8f;
    [SerializeField] private float gravity = 30.0f;

    [Header("Crouch Parameters")]
    [SerializeField] private float crouchHeight = 0.5f;
    [SerializeField] private float standHeight = 2f;
    [SerializeField] private float timeToCrouch = 0.25f;
    [SerializeField] private Vector3 crouchingCenter = new Vector3(0, 0.5f, 0);
    [SerializeField] private Vector3 standingCenter = new Vector3(0, 0, 0);
    private bool crouchKeyUp = false;
    private bool isCrouching;
    private bool duringCrouchAnimation;

    [Header("Leaning Parameters")]
    [SerializeField] private float leanAmount = 15f;
    [SerializeField] private float leanSpeed = 0.25f;
    [SerializeField] private bool toggleLean = false;
    [SerializeField] private Transform cameraRotLeaning;
    private bool isLeaning;

    [Header("Headbob Parameters")]
    [SerializeField] private float walkBobSpeed = 14f;
    [SerializeField] private float walkBobAmount = 0.05f;
    [SerializeField] private float sprintBobSpeed = 18f;
    [SerializeField] private float sprintBobAmount = 0.1f;
    [SerializeField] private float crouchBobSpeed = 8f;
    [SerializeField] private float crouchBobAmount = 0.025f;
    private float defaultYPos = 0;
    private float timer;

    [Header("Zoom Parameters")]
    [SerializeField] private float timeToZoom = 0.3f;
    [SerializeField] private float zoomFov = 30f;
    public float defaultFov;
    private Coroutine zoomRoutine;

    [Header("Footstep Parameters")]
    [Range(0, 5)] [SerializeField] private float footStepVolume = 1f;
    [Range(0, 15)] [SerializeField] private float baseStepSpeed = 0.5f;
    [Range(0, 15)] [SerializeField] private float crouchStepMultiplyer = 1.5f;
    [Range(0, 15)] [SerializeField] private float sprintStepMultiplyer = 0.6f;
    [SerializeField] private AudioSource footStepsAudioSource = default;
    [SerializeField] private AudioClip[] defaultFootStepClips = default;
    [SerializeField] private AudioClip[] woodFootStepClips = default;
    [SerializeField] private AudioClip[] grassFootStepClips = default;
    [SerializeField] private AudioClip[] metalFootStepClips = default;
    private float footStepTimer = 0;
    private float GetCurrentOffSet => isCrouching ? baseStepSpeed * crouchStepMultiplyer : isSprinting ? baseStepSpeed * sprintStepMultiplyer : baseStepSpeed;

    // sliding parameters
    private Vector3 hitPointNormal;

    private bool isSlopeSliding
    {
        get
        {
            if (characterController.isGrounded && Physics.Raycast(transform.position, Vector3.down, out RaycastHit slopeHit, 2f))
            {
                hitPointNormal = slopeHit.normal;
                return Vector3.Angle(hitPointNormal, Vector3.up) > characterController.slopeLimit;
            }
            else
            {
                return false;
            }
        }
    }

    [Header("Interaction")]
    [SerializeField] private Collider interactionCollider;
    [SerializeField] private Vector3 interactionRayPoint = default;
    [SerializeField] private float interactionDistance = default;
    [SerializeField] private LayerMask interactionLayer;
    private Interactable currentInteractable;

    [Header("Camera Tilt")]
    [SerializeField] private Transform cameraRot;
    [SerializeField] private float walkingCameraTiltAmount = 2f;
    [SerializeField] private float walkingCameraTiltTransitionSpeed = 0.15f;

    [HideInInspector] public Camera playerCamera;
    [HideInInspector] public CharacterController characterController;

    [HideInInspector] public Vector3 moveDirection;
    [HideInInspector] public Vector2 currentInput;
    [HideInInspector] public Vector2 currentInputRaw;
    [HideInInspector] public Vector2 lookInput;
    private float currentSpeed;
    private Quaternion initialRotation;

    private float rotationX = 0;

    private void OnEnable()
    {
        OnTakeDamage += ApplyDamage;
        HealthSlider.maxValue = maxHealth;
        HealthSlider.value = currentHealth;
    }

    private void OnDisable()
    {
        OnTakeDamage -= ApplyDamage;
    }

    void Awake()
    {
        playerCamera = GetComponentInChildren<Camera>();
        characterController = GetComponent<CharacterController>();

        initialRotation = playerCamera.transform.rotation;

        footStepsAudioSource.volume = footStepVolume;
        defaultYPos = playerCamera.transform.localPosition.y;
        defaultFov = playerCamera.fieldOfView;
        
        currentHealth = maxHealth;

        ApplyDamage(0); // to reset health bar

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }


    private void FixedUpdate()
    {
        
    }

    void Update()
    {
        if (CanMove)
        {
            HandleMovementInput(); 
            if(canLook)
                HandleMouseLook();

            if (canJump)
                HandleJump();

            if (canCrouch)
                HandleCrouch();

            if (canUseHeadBob)
                HandleHeadBob();

            if (canLean)
                HandleLeaning();

            if (canZoom)
                HandleZoom();

            if (useFootSteps)
                HandleFootSteps();

            if (walkingCameraTilt)
                HandleWalkingCameraTilt();

            if (canInteract)
            {
                HandleInteractionCheck();
                HandleInteractionInput();
            }

            ApplyFinalMovements();
        }
    }

    private void HandleMovementInput()
    {
        currentSpeed = isCrouching ? crouchSpeed : isSprinting ? sprintSpeed : walkSpeed;
        currentInput = new Vector2(currentSpeed * Input.GetAxis("Vertical"), currentSpeed * Input.GetAxis("Horizontal"));
        currentInputRaw = new Vector2(Input.GetAxisRaw("Vertical"), Input.GetAxisRaw("Horizontal"));

        float moveDirectionY = moveDirection.y; 
        moveDirection = (transform.TransformDirection(Vector3.forward) * currentInput.x) 
                      + (transform.TransformDirection(Vector3.right) * currentInput.y);
        moveDirection.y = moveDirectionY; 
    }

    private void HandleMouseLook()
    {
        lookInput = new Vector2(Input.GetAxis("Mouse Y") * lookSpeedY, Input.GetAxis("Mouse X") * lookSpeedX);
        rotationX -= Input.GetAxis("Mouse Y") * lookSpeedY;
        rotationX = Mathf.Clamp(rotationX, -upperLookLimit, lowerLookLimit);

        playerCamera.transform.localRotation = Quaternion.Euler(rotationX, 0, 0);

        transform.rotation *= Quaternion.Euler(0, Input.GetAxis("Mouse X") * lookSpeedX, 0);
    }

    public void DoFov(float endValue, float transitionTime = 0.25f, bool usePlayerCamera = true, bool useFpsCamera = true)
    {
        if (cameraTilt)
        {
            if (usePlayerCamera) playerCamera.DOFieldOfView(endValue, transitionTime);
        }
    }

    public void DoTiltZ(float zTilt, Transform cameraRotPoint, float transitionTime = 0.25f)
    {
        if (cameraTilt)
            cameraRotPoint.DOLocalRotate(new Vector3(0, 0, zTilt), transitionTime);
    }

    public void DoTiltX(float xTilt, Transform cameraRotPoint, float transitionTime = 0.25f)
    {
        if (cameraTilt)
            cameraRotPoint.DOLocalRotate(new Vector3(xTilt, 0, 0), transitionTime);
    }

    private void HandleJump()
    {
        if(shouldJump)
        {
            moveDirection.y = jumpForce;
            jumpFeedBack?.PlayFeedbacks(); //?. means not null
        }
    }

    private void HandleCrouch()
    {
        if (shouldCrouch)
        {
            crouchKeyUp = !crouchKeyUp;
            StartCoroutine(CrouchStand());
        }
    }

    private void HandleHeadBob()
    {
        if (!characterController.isGrounded) return;
        
        if(Mathf.Abs(moveDirection.x) > 0.1f || Mathf.Abs(moveDirection.z) > 0.1f)
        {
            timer += Time.deltaTime * (isCrouching ? crouchBobSpeed : isSprinting ? sprintBobSpeed : walkBobSpeed);
            playerCamera.transform.localPosition = new Vector3(
                playerCamera.transform.localPosition.x,
                defaultYPos + Mathf.Sin(timer) * (isCrouching ? crouchBobAmount : isSprinting ? sprintBobAmount : walkBobAmount),
                playerCamera.transform.localPosition.z
                );
        }
    }

    private void HandleLeaning()
    {
        if (canLeanLeft)
        {
            isLeaning = true;
            DoTiltZ(leanAmount, cameraRotLeaning, leanSpeed);
        }
        else if (canLeanRight)
        {
            isLeaning = true;
            DoTiltZ(-leanAmount, cameraRotLeaning, leanSpeed);
        }
        else
        {
            if(!cameraTilt)
            {
                DoTiltZ(0, cameraRotLeaning, leanSpeed);
            }
            DoTiltZ(0, cameraRotLeaning, leanSpeed);

            isLeaning = false;
        }
    }

    private void HandleZoom()
    {
        if (Input.GetKeyDown(controls.ZoomKey))
        {
            if(zoomRoutine != null)
            {
                StopCoroutine(zoomRoutine);
                zoomRoutine = null;
            }

            zoomRoutine = StartCoroutine(ToggleZoom(true, zoomFov, timeToZoom, zoomRoutine, false));
        }

        if (Input.GetKeyUp(controls.ZoomKey))
        {
            if (zoomRoutine != null)
            {
                StopCoroutine(zoomRoutine);
                zoomRoutine = null;
            }

            zoomRoutine = StartCoroutine(ToggleZoom(false, zoomFov, timeToZoom, zoomRoutine, false));
        }
    }

    private void HandleInteractionCheck()
    {
        // player
        if (Physics.Raycast(playerCamera.ViewportPointToRay(interactionRayPoint), out RaycastHit hit, interactionDistance))
        {
            if (hit.collider.gameObject.layer == 6 && (currentInteractable == null || hit.collider.gameObject.GetInstanceID() != currentInteractable.gameObject.GetInstanceID()))
            {
                hit.collider.TryGetComponent(out currentInteractable);

                if (currentInteractable)
                {
                    currentInteractable.OnFocus();
                }
            }
        }
        else if (currentInteractable)
        {
            currentInteractable.OnLoseFocus();
            currentInteractable = null;
        }
    }

    private void HandleInteractionInput()
    {
        if (Input.GetKeyDown(controls.InteractKey) && currentInteractable != null && Physics.Raycast(playerCamera.ViewportPointToRay(interactionRayPoint), out RaycastHit hit, interactionDistance, interactionLayer))
        {
            currentInteractable.OnInteract(hit, this);
        }
    }

    private void HandleWalkingCameraTilt()
    {
        if (!isLeaning)
        {
            if (currentInputRaw.y < 0)
                DoTiltZ(walkingCameraTiltAmount, cameraRot, walkingCameraTiltTransitionSpeed);
            else if (currentInputRaw.y > 0)
                DoTiltZ(-walkingCameraTiltAmount, cameraRot, walkingCameraTiltTransitionSpeed);
            else
            {
                DoTiltZ(0, cameraRot, walkingCameraTiltTransitionSpeed);
            }

            if (currentInputRaw.x < 0)
                DoTiltX(walkingCameraTiltAmount, cameraRot, walkingCameraTiltTransitionSpeed);
            else if (currentInputRaw.x > 0)
                DoTiltX(-walkingCameraTiltAmount, cameraRot, walkingCameraTiltTransitionSpeed);
            else
            {
                DoTiltX(0, cameraRot, walkingCameraTiltTransitionSpeed);
            }
        }
    }

    private void ApplyFinalMovements()
    {
        playerCamera.transform.position = new Vector3(transform.localPosition.x, playerCamera.transform.position.y, transform.localPosition.z);

        if (!characterController.isGrounded && useGravity)
        {
            moveDirection.y -= (gravity / 1f) * Time.deltaTime;
        }
        else if (!useGravity) { moveDirection = new Vector3(moveDirection.x, 0f, moveDirection.z); Debug.Log(moveDirection);  };

        if (willSlideOnSlopes && isSlopeSliding)
        {
            moveDirection += new Vector3(hitPointNormal.x, -hitPointNormal.y, hitPointNormal.z) * slopeSpeed;
        }

        bool wasJustInAir = false;
        if(!characterController.isGrounded)
            wasJustInAir = true;

        characterController.Move(moveDirection * Time.deltaTime);

        if(wasJustInAir == true && characterController.isGrounded)
            landFeedBack?.PlayFeedbacks();
    }
 
    private void HandleFootSteps()
    {
        if (!characterController.isGrounded || currentInput == Vector2.zero) return;

        footStepTimer -= Time.deltaTime;

        if (footStepTimer <= 0) 
        { 
            if(Physics.Raycast(playerCamera.transform.position, Vector3.down, out RaycastHit hit, 3))
            {
                switch(hit.collider.tag) // add more footstep types here
                {
                    case "footsteps/Default":
                        if (defaultFootStepClips.Length > 0) footStepsAudioSource.PlayOneShot(defaultFootStepClips[UnityEngine.Random.Range(0, defaultFootStepClips.Length - 1)]);
                        break;

                    case "footsteps/Grass":
                        if (grassFootStepClips.Length > 0) footStepsAudioSource.PlayOneShot(grassFootStepClips[UnityEngine.Random.Range(0, grassFootStepClips.Length - 1)]);
                        break;

                    case "footsteps/Metal":
                        if (metalFootStepClips.Length > 0) footStepsAudioSource.PlayOneShot(metalFootStepClips[UnityEngine.Random.Range(0, metalFootStepClips.Length - 1)]);
                        break;

                    case "footsteps/Wood":
                        if (woodFootStepClips.Length > 0) footStepsAudioSource.PlayOneShot(woodFootStepClips[UnityEngine.Random.Range(0, woodFootStepClips.Length - 1)]);
                        break;

                    default:
                        if (defaultFootStepClips.Length > 0) footStepsAudioSource.PlayOneShot(defaultFootStepClips[UnityEngine.Random.Range(0, defaultFootStepClips.Length - 1)]);
                        break;   
                }
            }

            footStepTimer = GetCurrentOffSet;
        }
    }

    private void ApplyDamage(float damage)
    {
        currentHealth -= damage;
        OnDamage?.Invoke(currentHealth);

        //effects
        damageFeedBack?.PlayFeedbacks();

        if (currentHealth <= 0) 
            KillPlayer();
        else if (regeneratingHealth != null) 
            StopCoroutine(regeneratingHealth);

        regeneratingHealth = StartCoroutine(RegenerateHealth());
    }

    private void KillPlayer()
    {
        currentHealth = 0;

        if(regeneratingHealth != null)
            StopCoroutine(regeneratingHealth);

        //effects
        deathFeedBack?.PlayFeedbacks();
        Debug.Log("dead", gameObject);
    }
    private IEnumerator CrouchStand()
    {
        if (isCrouching && Physics.Raycast(playerCamera.transform.position, Vector3.up, 1f, whatIsGround)) // Physics.SphereCast(playerCamera.transform.position, 1f, Vector3.up, out var hit, 1f, whatIsGround)
        {
            yield break;
        }

        duringCrouchAnimation = true;
    
        float timeElapsed = 0;
        float targetHeight = isCrouching ? standHeight : crouchHeight;
        float currentHeight = characterController.height;
        Vector3 targetCenter = isCrouching ? standingCenter : crouchingCenter;
        Vector3 currentCenter = characterController.center;

        while (timeElapsed < timeToCrouch)
        {
            characterController.height = Mathf.Lerp(currentHeight, targetHeight, timeElapsed / timeToCrouch);
            characterController.center = Vector3.Lerp(currentCenter, targetCenter, timeElapsed / timeToCrouch);
            timeElapsed += Time.deltaTime;

            yield return null;
        }

        characterController.height = targetHeight;
        characterController.center = targetCenter;

        isCrouching = !isCrouching;
        
        //effects
        if(isCrouching)
            crouchFeedBack?.PlayFeedbacks();
        else
            crouchToStandFeedBack?.PlayFeedbacks();

        duringCrouchAnimation = false;
    }

    private IEnumerator ToggleZoom(bool isEnter, float _TargetFov, float _TimeToZoom, Coroutine routine, bool zoomFpsCam = false)
    {
        float targetFov = isEnter ? _TargetFov : defaultFov;
        float startFov = playerCamera.fieldOfView;
        float timeElapsed = 0;

        while(timeElapsed < _TimeToZoom)
        {
            playerCamera.fieldOfView = Mathf.Lerp(startFov, targetFov, timeElapsed / _TimeToZoom);
            if (zoomFpsCam) playerCamera.fieldOfView = Mathf.Lerp(startFov, targetFov, timeElapsed / _TimeToZoom);
            timeElapsed += Time.deltaTime;

            yield return null;
        }

        playerCamera.fieldOfView = targetFov;
        if (zoomFpsCam) playerCamera.fieldOfView = targetFov;

        routine = null; 
    }

    private IEnumerator RegenerateHealth()
    {
        yield return new WaitForSeconds(timeBeforeRegenStarts);
        WaitForSeconds timeToWait = new WaitForSeconds(healthTimeIncrement);

        while(currentHealth < maxHealth)
        {
            currentHealth += healthValueIncrement;

            if (currentHealth > maxHealth)
                currentHealth = maxHealth;

            OnHeal?.Invoke(currentHealth);

            //effects
            regenerateHealthFeedBack?.PlayFeedbacks();

            yield return timeToWait;   
        }

        regeneratingHealth = null;
    }
}
