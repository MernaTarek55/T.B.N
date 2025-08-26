using System;
using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class Player : MonoBehaviour
{
    //this
    public InputActionAsset InputActions;
    public Camera mainCamera;
    public Image deadEyeCooldownImage;
    public Image InvisibilityCooldownImage;
    public StateMachine stateMachine { get; private set; }
    public Player_IdleState playerIdle { get; private set; }
    public Player_MoveState playerMove { get; private set; }
    public Player_JumpState playerJump { get; private set; }
    //public Player_DeadEyeStateTest1 playerDeadEye { get; private set; }
    public Death_State playerDeath { get; private set; }
    public Animator animator { get; private set; }
    public Rigidbody rb { get; private set; }
    public PlayerHealthComponent healthComponent { get; private set; }

    public float WalkSpeed = 5f;
    public float RotateSpeed = 10f;

    public Transform groundCheckPoint;
    public float groundCheckRadius = 0.2f;
    public LayerMask groundLayer;

    public bool IsGrounded => Physics.CheckSphere(groundCheckPoint.position, groundCheckRadius, groundLayer);
    public bool hasJumped = false;

    private InputAction moveAction;
    private InputAction jumpAction;
    private InputAction deadEyeAction;
    private Vector2 moveInput;
    public Vector2 MoveInput => moveInput;
    public bool JumpPressed { get; private set; }
    public bool DeadEyePressed { get; private set; }
    public bool IsShooting { get; private set; }

    public bool WasRunningBeforeJump { get; set; }
    public float WalkTimer { get; set; }

    public float deadEyeDuration = 10f;
    public float deadEyeCooldown = 30f;
    public float lastDeadEyeTime = -Mathf.Infinity;
    public bool CanUseDeadEye => Time.time >= lastDeadEyeTime + deadEyeCooldown;

    [SerializeField] GameObject pistol;
    [Header("Invisibility Settings")]
    //[SerializeField] GameObject invisibilityBtn;
    [Header("DeadEye Settings")]
    //[SerializeField] GameObject DeadEyeBtn;
    [Header("Movement Settings")]
    public float acceleration = 10f;
    public float deceleration = 15f;
    public float maxSpeed = 5f;
    public AnimationCurve movementCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);
    public float walkSpeed = 2f;
    public float runSpeed = 5f;
    [SerializeField] private WeaponSwitch weaponSwitch;

    public Transform playerHead;

    [HideInInspector]
    public Vector3 currentVelocity = Vector3.zero;
    [SerializeField] private SkinnedMeshRenderer renderer;
    [SerializeField] private Material dissolveMaterial;
    [SerializeField] private Material NormalMaterial;
    [SerializeField] private float dissolveSpeed = 0.5f;

    [Header("Smooth Rotation")]
    public AnimationCurve rotationCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);
    public float rotateDuration = 0.3f;

    [HideInInspector] public float rotateTimer = 0f;
    [HideInInspector] public Quaternion startRot;
    [HideInInspector] public Quaternion targetRot;
    [HideInInspector] public bool rotating = false;
    public ParticleSystem dust;
    private void Awake()
    {
        animator = GetComponent<Animator>();
        rb = GetComponent<Rigidbody>();
        healthComponent = GetComponent<PlayerHealthComponent>();
        moveAction = InputActions.FindActionMap("Player").FindAction("Move");
        jumpAction = InputActions.FindActionMap("Player").FindAction("Jump");

        stateMachine = new StateMachine();
        playerIdle = new Player_IdleState(stateMachine, "Idle", this);
        playerMove = new Player_MoveState(stateMachine, "Move", this);
        playerJump = new Player_JumpState(stateMachine, "Jump", this);
        playerDeath = new Death_State(stateMachine, "Death", this);
    }

    private void OnEnable()
    {
        InputActions.FindActionMap("Player").Enable();
    }

    private void OnDisable()
    {
        InputActions.FindActionMap("Player").Disable();
    }

    private void Start()
    {
        stateMachine.Initalize(playerIdle);
    }

    private void Update()
    {
        moveInput = moveAction.ReadValue<Vector2>();
        if (jumpAction.WasPressedThisFrame() && IsGrounded && !hasJumped)
        {
            JumpPressed = true;
        }
        else
        {
            JumpPressed = false;
        }
        animator.SetBool("IsGrounded", IsGrounded);
        stateMachine.UpdateActiveState();
    }
    public void SetVelocity(float xVelocity, float yVelocity)
    {
        rb.linearVelocity = new Vector2(xVelocity, yVelocity);
    }

    public void ActivateInvisibility()
    {
        GetComponent<InvisibilitySkill>().enabled = true;
        //invisibilityBtn.SetActive(true);
    }

    public void ActivateDeadEye()
    {
        //DeadEyeBtn.SetActive(true);
    }

    public void ActivatePistol()
    {
        pistol.SetActive(true);
    }

    public void SetShooting(bool isShooting)
    {
        IsShooting = isShooting;
    }
    public void FireWeaponEvent()
    {
        weaponSwitch?.FireBulletFromEvent();
    }

    public void ChangeMaterial(bool isRespawn)
    {
        if (renderer == null) return;
        if (isRespawn)
        {
        renderer.material = NormalMaterial;
        }
        else
        {
            NormalMaterial = renderer.material;
        renderer.material = dissolveMaterial;
        StartCoroutine(PlayerDissolve());

        }
    }



    public IEnumerator PlayerDissolve()
    {
        float dissolve = 0f;
        dissolveMaterial.SetFloat("_Dissolve", 0f); // Ensure reset before starting

        while (dissolve < 1f)
        {
            dissolve += Time.deltaTime * dissolveSpeed;
            dissolveMaterial.SetFloat("_Dissolve", Mathf.Clamp01(dissolve));
            yield return null;
        }

    }

    public void ResetHealth()
    {
        healthComponent.RenewHealth();
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.gameObject.layer == LayerMask.NameToLayer("FallOff"))
        {
            stateMachine.ChangeState(playerDeath);
        }
    }

}
