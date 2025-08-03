using System;
using System.Collections;
using System.Net.Mime;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;
using Image = UnityEngine.UI.Image;

public class TigerController : MonoBehaviour
{
    //events
    public Action<int> OnHunt;
    
    [Header("Debug Settings")]
    public TextMeshProUGUI movementStateText;

    [Header("Stats Update Settings")]
    public float sprintSpeedIncreaseAmount = 2f;
    
    [Header("Health Settings")]
    public float maxHealth = 100f;
    public Image healthBar;
    public DamageEffect damageEffect;
    private float health;
    
    [Header("Movement Settings")]
    public float sneakSpeed = 2f;
    public float moveSpeed = 4.0f;
    public float sprintSpeed = 8f;
    public float movementSpeedLerp = 0.2f;
    public float movementSpeedAnimationLerp = 2.5f;
    public float rotationSpeed = 500.0f;
    public float jumpForce = 8.0f;

    [Header("Attack Settings")]
    public float attackDelay = 1f;
    public float attackRange = 2f;

    [Header("Ground Check")]
    public LayerMask groundLayer;
    public float groundCheckDistance = 0.2f;// How far down to check for ground

    [Header("Inputs")]
    public InputActionProperty moveActionProperty; 
    public InputActionProperty jumpActionProperty;
    public InputActionProperty runActionProperty;
    public InputActionProperty sneakActionProperty;
    public InputActionProperty attackActionProperty;
    private InputAction moveAction;

    [Header("Animations")]
    private Animator animator;
    private readonly int moveSpeedHash = Animator.StringToHash("MoveSpeed");

    [Header("Noise Settings")]
    public float noMovementNoiseMultiplier = -0.7f;
    public float walkNoiseMultiplier = 0.5f;
    public float runNoiseMultiplier = 1.5f;
    public CentralFillBar noiseBar;
    
    [Header("Ui Refs")]
    public TextMeshProUGUI maxSprintSpeedText;
    public TextMeshProUGUI requiredLevelText;

    private CountdownTimer attackTimer;
    
    //components
    private Rigidbody rb;
    private Collider playerCollider;
    
    private bool isDead;
    private bool isGrounded;
    private bool runKeyPressed;
    private bool sneakKeyPressed;
    private float movementMagnitude;
    private float currentMoveSpeed;
    private Vector2 moveInput => moveAction.ReadValue<Vector2>();

    public float Noise{get; private set;}

    void Awake()
    {
        animator = GetComponent<Animator>();
        rb = GetComponent<Rigidbody>();
        playerCollider = GetComponent<Collider>();
        
        // Freeze player physics rotations
        rb.freezeRotation = true;
        
        //input setup
        moveAction = moveActionProperty.action;

        attackTimer = new CountdownTimer(attackDelay);
        
        //UI
        maxSprintSpeedText.text = sprintSpeed.ToString();
        
        //Health
        health = maxHealth;
        healthBar.fillAmount = 1;
    }

    private void OnEnable()
    {
        moveAction.Enable();
        jumpActionProperty.action.Enable();
        runActionProperty.action.Enable();
        sneakActionProperty.action.Enable();
        attackActionProperty.action.Enable();
        sneakActionProperty.action.started += (_) => sneakKeyPressed = true;
        sneakActionProperty.action.canceled += (_) => sneakKeyPressed = false;
        runActionProperty.action.started += (_) => runKeyPressed = true;
        runActionProperty.action.canceled += (_) => runKeyPressed = false;
        jumpActionProperty.action.started += Jump;
        attackActionProperty.action.started += TryAttack;
        LevelManager.LevelChanged += UpdateStats;
    }

    private void UpdateStats()
    {
        sprintSpeed += sprintSpeedIncreaseAmount;
        maxSprintSpeedText.text = sprintSpeed.ToString();
    }

    private void OnDisable()
    {
        moveAction.Disable();
        jumpActionProperty.action.Disable();
        jumpActionProperty.action.started -= Jump;
    }

    void Update()
    {
        if(isDead) return;
        
        CheckGround();
    }

    void FixedUpdate()
    {
        if(isDead) return;
        
        // Calculate movement direction relative to the camera
        Vector3 cameraForward = Camera.main.transform.forward;
        cameraForward.y = 0;
        cameraForward.Normalize();

        Vector3 cameraRight = Camera.main.transform.right;
        cameraRight.y = 0;
        cameraRight.Normalize();

        Vector3 moveDirection = (cameraForward * moveInput.y + cameraRight * moveInput.x).normalized;

        if (moveDirection.magnitude > 0.1f){
            //movement
            currentMoveSpeed = Mathf.Lerp(currentMoveSpeed, 
                sneakKeyPressed ? sneakSpeed : (runKeyPressed ? sprintSpeed : moveSpeed), movementSpeedLerp * Time.deltaTime);
            rb.linearVelocity = new Vector3(moveDirection.x * currentMoveSpeed, rb.linearVelocity.y, moveDirection.z * currentMoveSpeed);

            //animation
            movementMagnitude = Mathf.Lerp(movementMagnitude, 
                sneakKeyPressed ? 0.5f : (runKeyPressed ? 2 : 1), movementSpeedAnimationLerp * Time.deltaTime);
            animator.SetFloat(moveSpeedHash, movementMagnitude);
            
            //rotation
            Quaternion targetRotation = Quaternion.LookRotation(moveDirection);
            transform.rotation = Quaternion.RotateTowards(transform.rotation, targetRotation, rotationSpeed * Time.fixedDeltaTime);
            
            //noise
            bool isRunning = !sneakKeyPressed && runKeyPressed;
            float noiseMultiplier = (sneakKeyPressed || !isRunning && Noise > 0.5f) ? noMovementNoiseMultiplier : (runKeyPressed ? runNoiseMultiplier : walkNoiseMultiplier); 
            ChangeNoiseAmount(Time.fixedDeltaTime * noiseMultiplier, isRunning || (!isRunning && Noise > 0.5f));
        }
        else
        {
            // If no input
            rb.linearVelocity = new Vector3(0, rb.linearVelocity.y, 0);
            
            //animation
            movementMagnitude = 0f;
            animator.SetFloat(moveSpeedHash, movementMagnitude);
            
            //Noise
            ChangeNoiseAmount(Time.fixedDeltaTime * noMovementNoiseMultiplier);
        }
        
        //update noise bar
        noiseBar.SetFillAmount(Noise);
        
        //Debug
        if(moveInput.magnitude <= .1f)
            movementStateText.text = "Idle";
        else if (sneakKeyPressed)
            movementStateText.text = "Sneaking";
        else if(runKeyPressed)
            movementStateText.text = "Running";
        else
            movementStateText.text = "Walking";
    }

    private void TryAttack(InputAction.CallbackContext _obj)
    {
        if(attackTimer.IsRunning) return;
        
        Attack();
    }

    private void Attack()
    {
        var colliders = Physics.OverlapSphere(transform.position, attackRange);

        foreach (var col in colliders){
            if (col.TryGetComponent(out IHuntableAnimal prey)){
                bool huntSuccess = prey.TryHunt();
                
                if (huntSuccess) OnHunt?.Invoke(prey.Points);
                else{
                    int requiredLevel = LevelManager.instance.RequiredLevel(prey.AnimalType);
                    StartCoroutine(EnableMessageForSec(requiredLevel));
                }

                break;
            }
        }
        
        IEnumerator EnableMessageForSec(int _level)
        {
            requiredLevelText.text = "Required Level :" + _level;
            requiredLevelText.gameObject.SetActive(true);

            yield return new WaitForSeconds(0.5f);
            requiredLevelText.gameObject.SetActive(false);
        }
    }

    private void Jump(InputAction.CallbackContext _obj)
    {
        if (isGrounded)
            rb.AddForce(Vector3.up * jumpForce, ForceMode.Impulse);
    }

    void CheckGround()
    {
        // Raycast origin at the bottom center of the player's collider
        // Adjusts for different collider shapes/sizes
        Vector3 rayOrigin = transform.position - transform.up * 0.05f;
        
        
        isGrounded = Physics.Raycast(rayOrigin, -transform.up, out RaycastHit hit, groundCheckDistance, groundLayer);
        
        Debug.DrawLine(rayOrigin, rayOrigin - transform.up * groundCheckDistance , isGrounded ? Color.green : Color.red);
    }

    void OnDrawGizmosSelected()
    {
        if (playerCollider == null) return;

        /*Vector3 rayOrigin = transform.position - transform.up * (playerCollider.bounds.extents.y - 0.05f);
        Gizmos.color = isGrounded ? Color.green : Color.red;
        Gizmos.DrawWireSphere(rayOrigin - transform.up * groundCheckDistance, playerCollider.bounds.extents.x * 0.4f);*/
        
        Vector3 rayOrigin = transform.position - transform.up * (playerCollider.bounds.extents.y - 0.05f);
        
        Gizmos.color = Color.white;
        Gizmos.DrawSphere(rayOrigin, .05f);
        
        bool Grounded = Physics.SphereCast(rayOrigin, playerCollider.bounds.extents.x * 0.4f, -transform.up, out RaycastHit hit, groundCheckDistance, groundLayer);
        
        Gizmos.color = Grounded ? Color.green : Color.red;
        Gizmos.DrawLine(rayOrigin, rayOrigin - transform.up * (groundCheckDistance));
        Gizmos.DrawWireSphere(rayOrigin - transform.up * (groundCheckDistance), playerCollider.bounds.extents.x * 0.4f);
    }

    private void ChangeNoiseAmount(float _value, bool _clampTo1 = false)
    {
        Noise += _value;
        
        if(_clampTo1)
            Noise = Mathf.Clamp01(Noise);
        else
            Noise = Mathf.Clamp(Noise, 0, 0.5f);
    }

    public void TakeDamage(float _damage)
    {
        
        health -= _damage;
        healthBar.fillAmount = health / maxHealth;
        
        damageEffect.ShowDamageEffect();
        if (health <= 0){
            isDead = true;
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
        }
    }
}