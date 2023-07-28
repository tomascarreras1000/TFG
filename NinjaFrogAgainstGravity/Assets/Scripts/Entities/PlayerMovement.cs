using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class PlayerMovement : MonoBehaviour // Maybe change name to PlayerScript or smth
{
    public delegate void Ability();
    public static event Ability AbilityCast;

    private Rigidbody2D rigidbody;
    private Collider2D collider;
    private Animator animator;
    private SpriteRenderer sprite;
    private AudioSource audioSource;

    [SerializeField] private LayerMask groundLayer;
    [SerializeField] private PhysicsMaterial2D defaultMaterial;
    [SerializeField] private PhysicsMaterial2D slipMaterial;

    private Transform spawnPoint;

    private GameManager sceneManager;

    private enum GravityDirection
    {
        DOWN    = 0,
        RIGHT   = 1,
        UP      = 2,
        LEFT    = 3
    }

    private GravityDirection gravityDirection = GravityDirection.DOWN;

    private enum MovementState
    {
        IDLE = 0,
        WALKING = 1, // New animation slowing running down
        RUNNING = 2,
        JUMPING = 3,
        FALLING = 4,
        DOUBLE_JUMPING = 5,

        HURT = 6,
        DEATH = 7
    }

    private MovementState movementState = MovementState.IDLE;
    private bool isFacingRight = true;
    private bool isDoubleJumpUp;
    private bool isGrounded;

    // Basic Logic 
    [Header("Basic Logic")]
    [SerializeField] private int maxHP;
    private int currentHP;
    private int pinappleCount;

    private float deltaX = 0.0f;
    private bool canMove = true;
    //private int deltaY = 0;
    [Header("Movement Logic")]
    [SerializeField] private float maxFallSpeed;
    [SerializeField] private float runMaxSpeed;
    [SerializeField] private float runAccelAmount;
    [SerializeField] private float runDecelAmount;
    [SerializeField] private float accelInAir;
    [SerializeField] private float decelInAirMult;


    // Jump Logic
    [Header("Jump Logic")]
    [SerializeField] private float jumpInputBufferTime;
    [SerializeField] private float coyoteTime;
    private bool isJumping = false;
    private bool isJumpCut = false;
    private bool isFalling = false;
    private bool isSliding = false;
    [SerializeField] private float jumpForce;
    [SerializeField] private float jumpHangAccelerationMult;
    [SerializeField] private float jumpHangMaxSpeedMult;



    // Xtra logic
    [Header("Xtra Logic")]
    [SerializeField] private float gravityScale;
    [SerializeField] private float jumpCutGravityMult;
    [SerializeField] private float jumpHangTimeThreshold;
    [SerializeField] private float jumpHangGravityMult;
    [SerializeField] private float fallGravityMult;
    [SerializeField] private float iFrames; // In seconds
    [SerializeField] private float pushbackForce;

    // Combat
    [Header("Combat Logic")]
    [SerializeField] private GameObject slashPrefab;
    private GameObject slash = null;
    [SerializeField] private float attackRange;
    [SerializeField] private float attackSpeed;
    private bool isGodModeOn = false;

    // Timers
    private float lastPressedJumpTimer; 
    private float lastOnGroundTimer;    
    private float lastOnWallRightTimer; 
    private float lastOnWallLeftTimer;  
    private float lastOnWallTimer;
    private float iFramesTimer;
    private float attackTimer;

    // Sounds
    [Header("Sounds")]
    [SerializeField] private AudioClip audioClipRunning;


    // Physics shit
    private float speed = 3.0f;
    private float jumpSpeed = 30.0f;

    private Vector2 gravityMultiplier = new Vector2(0.0f, 1.0f);
    private float gravityMag;
    private bool gravityAltered = false;


    public enum Collectables
    {
        NO_TYPE = 0,

        PINAPPLE = 1
    }
    public static event Action<int> OnHealthChange;
    public static event Action<int> OnPickUp;

    private float maxHeight = 0.0f;

    private void Awake()
    {
        spawnPoint = GameObject.Find("SpawnPoint").transform;
        
        rigidbody = GetComponent<Rigidbody2D>();
        collider = GetComponent<Collider2D>();
        animator = GetComponent<Animator>();
        sprite = GetComponent<SpriteRenderer>();
        audioSource = GetComponent<AudioSource>();

        GameObject sceneManagerGO = GameObject.Find("SceneManager");
        if (sceneManager)
            sceneManager = sceneManagerGO.GetComponent<GameManager>();
    }

    private void Start()
    {
        gravityDirection = GravityDirection.DOWN;
        
        isGrounded = IsGrounded(Vector2.down);
        gravityMag = Physics.gravity.y;

        currentHP = maxHP;
        OnHealthChange(currentHP);
        pinappleCount = 0;
        OnPickUp(pinappleCount);
        isGodModeOn = false;
    }

    private void Update()
    {
        UpdateTimers();
        HandleInput();
        CheckCollisions();
        CheckJumpState();
        UpdateGravity();
        UpdateAudioSource();
        UpdateAnimation();
    }

    private void FixedUpdate()
    {
        Run();
    }

    private void UpdateTimers()
    {
        lastOnGroundTimer -= Time.deltaTime;
        lastOnWallTimer -= Time.deltaTime;
        lastOnWallRightTimer -= Time.deltaTime;
        lastOnWallLeftTimer -= Time.deltaTime;
        lastPressedJumpTimer -= Time.deltaTime;
        iFramesTimer -= Time.deltaTime;
        attackTimer -= Time.deltaTime;
    }

    

    private void CheckCollisions()
    {
        if (!isJumping)
        {
            switch (gravityDirection)
            {
                case GravityDirection.DOWN:
                    {
                        // Ground Check
                        if (IsGrounded(Vector2.down))
                            lastOnGroundTimer = coyoteTime;

                        // Right Wall Check
                        if (IsGrounded(Vector2.right))
                            lastOnWallRightTimer = coyoteTime;

                        // Left Wall Check
                        if (IsGrounded(Vector2.left))
                            lastOnWallLeftTimer = coyoteTime;

                        // Two checks needed for both left and right walls since whenever the play turns the wall checkPoints swap sides
                        lastOnWallTimer = Mathf.Max(lastOnWallLeftTimer, lastOnWallRightTimer);
                        break;
                    }
                case GravityDirection.RIGHT:
                    {
                        if (IsGrounded(Vector2.right))
                            lastOnGroundTimer = coyoteTime;

                        if (IsGrounded(Vector2.up))
                            lastOnWallRightTimer = coyoteTime;

                        if (IsGrounded(Vector2.down))
                            lastOnWallLeftTimer = coyoteTime;

                        lastOnWallTimer = Mathf.Max(lastOnWallLeftTimer, lastOnWallRightTimer);
                        break;
                    }
                case GravityDirection.UP:
                    {
                        if (IsGrounded(Vector2.up))
                            lastOnGroundTimer = coyoteTime;
                        
                        if (IsGrounded(Vector2.left))
                            lastOnWallRightTimer = coyoteTime;

                        if (IsGrounded(Vector2.right))
                            lastOnWallLeftTimer = coyoteTime;

                        lastOnWallTimer = Mathf.Max(lastOnWallLeftTimer, lastOnWallRightTimer);
                        break;
                    }
                case GravityDirection.LEFT:
                    {
                        if (IsGrounded(Vector2.left))
                            lastOnGroundTimer = coyoteTime;

                        if (IsGrounded(Vector2.down))
                            lastOnWallRightTimer = coyoteTime;

                        if (IsGrounded(Vector2.up))
                            lastOnWallLeftTimer = coyoteTime;

                        lastOnWallTimer = Mathf.Max(lastOnWallLeftTimer, lastOnWallRightTimer);
                        break;
                    }
            }
        }
    }

    private void CheckJumpState()
    {
        if (isJumping)
        {
            switch (gravityDirection)
            {
                case GravityDirection.DOWN:
                    {
                        if (rigidbody.velocity.y < 0.01f)
                        {
                            isJumping = false;
                            isFalling = true;
                        }
                        break;
                    }
                case GravityDirection.RIGHT:
                    {
                        if (rigidbody.velocity.x > -0.01f)
                        {
                            isJumping = false;
                            isFalling = true;
                        }
                        break;
                    }
                case GravityDirection.UP:
                    {
                        if (rigidbody.velocity.y > -0.01f)
                        {
                            isJumping = false;
                            isFalling = true;
                        }
                        break;
                    }
                case GravityDirection.LEFT:
                    {
                        if (rigidbody.velocity.x < 0.01f)
                        {
                            isJumping = false;
                            isFalling = true;
                        }
                        break;
                    }
            }
        }

        if (lastOnGroundTimer > 0.0f && !isJumping)
        {
            isJumpCut = false;
            isFalling = false;

            //Jump
            if (lastPressedJumpTimer > 0.0f)
            {
                isJumping = true;
                Jump();
            }
        }
    }

    private void UpdateGravity()
    {
        // Reset to default material
        if (rigidbody.sharedMaterial != defaultMaterial)
            rigidbody.sharedMaterial = defaultMaterial;

        switch (gravityDirection)
        {
            case GravityDirection.DOWN:
                {
                    // Higher gravity if jump button released
                    if (isJumpCut)
                        rigidbody.gravityScale = gravityScale * jumpCutGravityMult;

                    // Lower gravity when on the pinacle of the jump
                    else if ((isJumping || isFalling) && Mathf.Abs(rigidbody.velocity.y) < jumpHangTimeThreshold)
                    {
                        rigidbody.gravityScale = gravityScale * jumpHangGravityMult;
                        rigidbody.sharedMaterial = slipMaterial;
                    }

                    // Higher gravity when falling
                    else if (rigidbody.velocity.y < -0.01f)
                    {
                        rigidbody.gravityScale = gravityScale * fallGravityMult;
                        rigidbody.velocity = new Vector2(rigidbody.velocity.x, Mathf.Max(rigidbody.velocity.y, -maxFallSpeed));
                    }

                    // Default gravity if standing on a platform or moving upwards
                    else
                        rigidbody.gravityScale = gravityScale;

                    break;
                }
            case GravityDirection.RIGHT:
                {
                    if (isJumpCut)
                        rigidbody.gravityScale = gravityScale * jumpCutGravityMult;

                    else if ((isJumping || isFalling) && Mathf.Abs(rigidbody.velocity.x) < jumpHangTimeThreshold)
                    {
                        rigidbody.gravityScale = gravityScale * jumpHangGravityMult;
                        rigidbody.sharedMaterial = slipMaterial;
                    }

                    else if (rigidbody.velocity.x > 0.01f)
                    {
                        rigidbody.gravityScale = gravityScale * fallGravityMult;
                        rigidbody.velocity = new Vector2(Mathf.Max(rigidbody.velocity.x, -maxFallSpeed), rigidbody.velocity.y);
                    }
                    else
                        rigidbody.gravityScale = gravityScale;

                    break;
                }
            case GravityDirection.UP:
                {
                    if (isJumpCut)
                        rigidbody.gravityScale = gravityScale * jumpCutGravityMult;

                    else if ((isJumping || isFalling) && Mathf.Abs(rigidbody.velocity.y) < jumpHangTimeThreshold)
                    {
                        rigidbody.gravityScale = gravityScale * jumpHangGravityMult;
                        rigidbody.sharedMaterial = slipMaterial;
                    }

                    else if (rigidbody.velocity.y > 0.01f)
                    {
                        rigidbody.gravityScale = gravityScale * fallGravityMult;
                        rigidbody.velocity = new Vector2(rigidbody.velocity.x, Mathf.Max(rigidbody.velocity.y, -maxFallSpeed));
                    }

                    else
                        rigidbody.gravityScale = gravityScale;

                    break;
                }
            case GravityDirection.LEFT:
                {
                    if (isJumpCut)
                        rigidbody.gravityScale = gravityScale * jumpCutGravityMult;

                    else if ((isJumping || isFalling) && Mathf.Abs(rigidbody.velocity.x) < jumpHangTimeThreshold)
                    {
                        rigidbody.gravityScale = gravityScale * jumpHangGravityMult;
                        rigidbody.sharedMaterial = slipMaterial;
                    }

                    else if (rigidbody.velocity.x < -0.01f)
                    {
                        rigidbody.gravityScale = gravityScale * fallGravityMult;
                        rigidbody.velocity = new Vector2(Mathf.Max(rigidbody.velocity.x, -maxFallSpeed), rigidbody.velocity.y);
                    }
                    else
                        rigidbody.gravityScale = gravityScale;

                    break;
                }
        }
    }


    private void HandleInput()
    {
        HandleHorizontalInput();
        HandleJumpInput();
        HandleAttackInput();
        HandleOtherInputs();
    }

    private void HandleHorizontalInput()
    {
        deltaX = 0.0f;

        if (Input.GetKey(KeyCode.A) && !Input.GetKey(KeyCode.D))    // Left
        {
            deltaX = -1.0f;
        }
        else if (Input.GetKey(KeyCode.D) && !Input.GetKey(KeyCode.A))    // Right
        {
            deltaX = 1.0f;
        }

        if (Math.Abs(deltaX) > 0.0f)
        {
            if (gravityDirection != GravityDirection.UP)
            {
                CheckDirectionToFace(deltaX > 0.0f);
            }
            else
            {
                CheckDirectionToFace(deltaX * (-1.0f) > 0.0f);
            }
        }
    }

    private void HandleJumpInput()
    {
        if (Input.GetKeyDown(KeyCode.Space))
        {
            lastPressedJumpTimer = jumpInputBufferTime;
        }

        if (Input.GetKeyUp(KeyCode.Space))
        {
            if (isJumping)
            {
                switch (gravityDirection)
                {
                    case GravityDirection.DOWN:
                        isJumpCut = rigidbody.velocity.y > 0.01f;
                        break;
                    case GravityDirection.RIGHT:
                        isJumpCut = rigidbody.velocity.x < -0.01f;
                        break;
                    case GravityDirection.UP:
                        isJumpCut = rigidbody.velocity.y < -0.01f;
                        break;
                    case GravityDirection.LEFT:
                        isJumpCut = rigidbody.velocity.x > 0.01f;
                        break;
                }
            }
        }
    }

    private void HandleAttackInput()
    {
        if (Input.GetKeyDown(KeyCode.N))
        {
            if (slash == null)
            {
                switch (gravityDirection)
                {
                    case GravityDirection.DOWN:
                        slash = Instantiate(slashPrefab, transform.position + new Vector3(attackRange * transform.localScale.x, 0, 0), transform.rotation);
                        break;
                    case GravityDirection.RIGHT:
                        slash = Instantiate(slashPrefab, transform.position + new Vector3(0, attackRange * transform.localScale.x, 0), transform.rotation);
                        break;
                    case GravityDirection.UP:
                        slash = Instantiate(slashPrefab, transform.position - new Vector3(attackRange * transform.localScale.x, 0, 0), transform.rotation);
                        break;
                    case GravityDirection.LEFT:
                        slash = Instantiate(slashPrefab, transform.position - new Vector3(0, attackRange * transform.localScale.x, 0), transform.rotation);
                        break;
                }

                slash.transform.localScale = transform.localScale;
            }
        }
    }

    private void HandleOtherInputs()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            Application.Quit();
        }
        if (Input.GetKeyDown(KeyCode.F1))
        {
            SetGodMode(!isGodModeOn);
        }
    }


    private void UpdateAudioSource()
    {
        if (Mathf.Abs(deltaX) > 0f && lastOnGroundTimer > 0.0f && !isJumping)
        {
            if (audioSource.clip != audioClipRunning)
            {
                audioSource.clip = audioClipRunning;
                audioSource.Play();
            }
            else if (!audioSource.isPlaying)
            {
                audioSource.Play();
                Debug.Log("Here");
            }
        }
        else
        {
            audioSource.Pause();
        }
    }

    private void UpdateAnimation()
    {
        // Set movement state based on deltaX
        movementState = (MathF.Abs(deltaX) > 0.0f) ? MovementState.RUNNING : MovementState.IDLE;

        // Check if object is jumping or falling (common logic for all gravity directions)
        if (isFalling)
            movementState = MovementState.FALLING;

        // Apply specific checks based on gravity direction
        switch (gravityDirection)
        {
            case GravityDirection.DOWN:
                if (rigidbody.velocity.y > 0.01f)
                    movementState = MovementState.JUMPING;
                break;
            case GravityDirection.RIGHT:
                if (rigidbody.velocity.x < -0.01f)
                    movementState = MovementState.JUMPING;
                break;
            case GravityDirection.UP:
                if (rigidbody.velocity.y < -0.01f)
                    movementState = MovementState.JUMPING;
                break;
            case GravityDirection.LEFT:
                if (rigidbody.velocity.x > 0.01f)
                    movementState = MovementState.JUMPING;
                break;
        }

        animator.SetInteger("movementState", (int)movementState);
    }


    private bool IsGrounded(Vector2 side)
    {
        return Physics2D.BoxCast(collider.bounds.center, collider.bounds.size, 0.0f, side, 0.1f, groundLayer);
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.gameObject.CompareTag("Pinapple"))
        {
            pinappleCount++;
            OnPickUp(pinappleCount);

            if (currentHP < maxHP)
            {
                currentHP++;
                OnHealthChange(currentHP);
            }
            Destroy(collision.gameObject);
        }
        else if (collision.gameObject.CompareTag("Trap") || collision.gameObject.CompareTag("Enemy"))
        {
            HandleHit(collision.transform.position);
        }
        else if (collision.gameObject.CompareTag("Limit"))
        {
            HandleHit(collision.transform.position, true);
        }
    }

    private void OnTriggerStay2D(Collider2D collision)
    {
        if (collision.gameObject.CompareTag("Trap") || collision.gameObject.CompareTag("Enemy"))
        {
            HandleHit(collision.transform.position);
        }

        if (gravityDirection == GravityDirection.DOWN)
        {
            if (collision.gameObject.CompareTag("GravityLeft"))
            {
                gravityDirection = GravityDirection.LEFT;
                Rotate();
            }
            else if (collision.gameObject.CompareTag("GravityRight"))
            {
                gravityDirection = GravityDirection.RIGHT;
                Rotate();
            }
            else if (collision.gameObject.CompareTag("GravityUp"))
            {
                gravityDirection = GravityDirection.UP;
                Rotate();
            }
        }
    }

    private void OnTriggerExit2D(Collider2D collision)
    {
        if (collision.gameObject.CompareTag("Platform"))
        {
            transform.SetParent(null);
        }

        if (collision.gameObject.CompareTag("GravityLeft") || collision.gameObject.CompareTag("GravityRight") || collision.gameObject.CompareTag("GravityUp"))
        {
            gravityDirection = GravityDirection.DOWN;
            Rotate();
            Debug.Log("Out");
        }      
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.gameObject.CompareTag("Trap"))
        {
            HandleHit(collision.transform.position);
        }
    }

    private void OnCollisionStay2D(Collision2D collision)
    {
        if (collision.gameObject.CompareTag("Trap") || collision.gameObject.CompareTag("Enemy"))
        {
            HandleHit(collision.transform.position);
        }
    }

    private void Jump()
    {
        lastPressedJumpTimer = 0.0f;
        lastOnGroundTimer = 0.0f;

        switch (gravityDirection)
        {
            case GravityDirection.DOWN:
                {
                    float force = jumpForce - Mathf.Abs(rigidbody.velocity.y);
                    rigidbody.AddForce(Vector2.up * force, ForceMode2D.Impulse);
                    break;
                }
            case GravityDirection.RIGHT:
                {
                    float force = jumpForce + Mathf.Abs(rigidbody.velocity.x);
                    rigidbody.AddForce(Vector2.left * force, ForceMode2D.Impulse);
                    break;
                }
            case GravityDirection.UP:
                {
                    float force = jumpForce - Mathf.Abs(rigidbody.velocity.y);
                    rigidbody.AddForce(Vector2.down * force, ForceMode2D.Impulse);
                    break;
                }
            case GravityDirection.LEFT:
                {
                    float force = jumpForce - Mathf.Abs(rigidbody.velocity.x);
                    rigidbody.AddForce(Vector2.right * force, ForceMode2D.Impulse);
                    break;
                }
        }
    }


    private void Run()
    {
        if (!canMove)
            return;

        //Calculate the direction we want to move in and our desired velocity
        float targetSpeed = deltaX * runMaxSpeed;

        float accelRate;
        if (lastOnGroundTimer > 0.0f)
            accelRate = (Mathf.Abs(targetSpeed) > 0.01f) ? runAccelAmount : runDecelAmount;
        else
            accelRate = (Mathf.Abs(targetSpeed) > 0.01f) ? runAccelAmount * accelInAir : runDecelAmount * decelInAirMult;

        switch (gravityDirection)
        {
            case GravityDirection.DOWN:
                {
                    if ((isJumping || isFalling) && Mathf.Abs(rigidbody.velocity.y) < jumpHangTimeThreshold)
                    {
                        accelRate *= jumpHangAccelerationMult;
                        targetSpeed *= jumpHangMaxSpeedMult;
                    }

                    float speedDif = targetSpeed - rigidbody.velocity.x;
                    float movement = speedDif * accelRate;

                    if (Math.Abs(speedDif) > 1.0f)
                        rigidbody.AddForce(movement * Vector2.right, ForceMode2D.Force);

                    break;
                }
            case GravityDirection.RIGHT:
                {
                    if ((isJumping || isFalling) && Mathf.Abs(rigidbody.velocity.x) < jumpHangTimeThreshold)
                    {
                        accelRate *= jumpHangAccelerationMult;
                        targetSpeed *= jumpHangMaxSpeedMult;
                    }

                    float speedDif = targetSpeed - rigidbody.velocity.y;
                    float movement = speedDif * accelRate;

                    if (Math.Abs(speedDif) > 1.0f)
                        rigidbody.AddForce(movement * Vector2.up, ForceMode2D.Force);

                    break;
                }
            case GravityDirection.UP:
                {
                    if ((isJumping || isFalling) && Mathf.Abs(rigidbody.velocity.y) < jumpHangTimeThreshold)
                    {
                        accelRate *= jumpHangAccelerationMult;
                        targetSpeed *= jumpHangMaxSpeedMult;
                    }

                    float speedDif = targetSpeed - rigidbody.velocity.x;
                    float movement = speedDif * accelRate;

                    if (Math.Abs(speedDif) > 1.0f)
                        rigidbody.AddForce(movement * Vector2.right, ForceMode2D.Force); 

                    break;
                }
            case GravityDirection.LEFT:
                {
                    if ((isJumping || isFalling) && Mathf.Abs(rigidbody.velocity.x) < jumpHangTimeThreshold)
                    {
                        accelRate *= jumpHangAccelerationMult;
                        targetSpeed *= jumpHangMaxSpeedMult;
                    }

                    float speedDif = targetSpeed - rigidbody.velocity.y * (-1.0f);
                    float movement = speedDif * accelRate;

                    if (Math.Abs(speedDif) > 1.0f)
                        rigidbody.AddForce(movement * Vector2.down, ForceMode2D.Force);

                    break;
                }
        }
    }

    private void CheckDirectionToFace(bool isFacingRight)
    {
        if (this.isFacingRight != isFacingRight)
            Turn();
    }

    private void Turn()
    {
        Vector3 scale = transform.localScale;
        scale.x *= -1;
        transform.localScale = scale;

        isFacingRight = !isFacingRight;
    }

    private void Rotate()
    {
        switch (gravityDirection)
        {
            case GravityDirection.DOWN:
                {
                    Vector3 rotation = transform.localRotation.eulerAngles;
                    rotation.z = 0;
                    transform.rotation = Quaternion.Euler(rotation);

                    Physics2D.gravity = new Vector2(0.0f, -9.81f);

                    break;
                }
            case GravityDirection.RIGHT:
                {
                    Vector3 rotation = transform.localRotation.eulerAngles;
                    rotation.z = 90;
                    transform.rotation = Quaternion.Euler(rotation);

                    Physics2D.gravity = new Vector2(9.81f, 0.0f);

                    break;
                }
            case GravityDirection.UP:
                {
                    Vector3 rotation = transform.localRotation.eulerAngles;
                    rotation.z = 180;
                    transform.rotation = Quaternion.Euler(rotation);

                    Physics2D.gravity = new Vector2(0.0f, 9.81f);

                    break;
                }
            case GravityDirection.LEFT:
                {
                    Vector3 rotation = transform.localRotation.eulerAngles;
                    rotation.z = 270;
                    transform.rotation = Quaternion.Euler(rotation);

                    Physics2D.gravity = new Vector2(-9.81f, 0.0f);

                    break;
                }
        }
    }

    private void HandleHit(Vector3 origin, bool isLethalDmg = false)
    {
        if (iFramesTimer > 0.0f || isGodModeOn) // Virtually no hit
            return;
        
        currentHP--;
        if (currentHP <= 0 || isLethalDmg)
        {
            currentHP = 0;
            OnHealthChange(currentHP);
            Die();
        }
        else
        {
            OnHealthChange(currentHP);
            animator.SetTrigger("Hit");
            canMove = false;
            iFramesTimer = iFrames;

            Vector3 pushbackDirection = Vector3.Normalize(transform.position - origin);
            rigidbody.AddForce(pushbackDirection * pushbackForce, ForceMode2D.Impulse);
        }
    }

    private void Die()
    {
        animator.SetTrigger("Death");
        collider.enabled = false;
        rigidbody.bodyType = RigidbodyType2D.Static;
    }

    private void EndHitAnimation()
    {
        canMove = true;
    }

    private void EndDeathAnimation()
    {
        SceneManager.LoadScene("Map02");
    }

    private void SetGodMode(bool setTo)
    {
        if (isGodModeOn != setTo)
            isGodModeOn = setTo;
    }
}



/*
 
private void UpdateGravity()
    {
        // Reset to default material
        if (rigidbody.sharedMaterial != defaultMaterial)
            rigidbody.sharedMaterial = defaultMaterial;

        switch (gravityDirection)
        {
            case GravityDirection.DOWN:
                {
                    // Higher gravity if jump button released
                    if (isJumpCut)
                        rigidbody.gravityScale = gravityScale * jumpCutGravityMult;

                    // Lower gravity when on the pinacle of the jump
                    else if ((isJumping || isFalling) && Mathf.Abs(rigidbody.velocity.y) < jumpHangTimeThreshold)
                    {
                        rigidbody.gravityScale = gravityScale * jumpHangGravityMult;
                        rigidbody.sharedMaterial = slipMaterial;
                    }

                    // Higher gravity when falling
                    else if (rigidbody.velocity.y < -0.01f)
                    {
                        rigidbody.gravityScale = gravityScale * fallGravityMult;
                        rigidbody.velocity = new Vector2(rigidbody.velocity.x, Mathf.Max(rigidbody.velocity.y, -maxFallSpeed));
                    }

                    // Default gravity if standing on a platform or moving upwards
                    else
                        rigidbody.gravityScale = gravityScale;

                    break;
                }
            case GravityDirection.RIGHT:
                {
                    if (isJumpCut)
                        rigidbody.gravityScale = gravityScale * jumpCutGravityMult;

                    else if ((isJumping || isFalling) && Mathf.Abs(rigidbody.velocity.x) < jumpHangTimeThreshold)
                    {
                        rigidbody.gravityScale = gravityScale * jumpHangGravityMult;
                        rigidbody.sharedMaterial = slipMaterial;
                    }

                    else if (rigidbody.velocity.x > 0.01f)
                    {
                        rigidbody.gravityScale = gravityScale * fallGravityMult;
                        rigidbody.velocity = new Vector2(Mathf.Max(rigidbody.velocity.x, -maxFallSpeed), rigidbody.velocity.y);
                    }
                    else
                        rigidbody.gravityScale = gravityScale;

                    break;
                }
            case GravityDirection.UP:
                {
                    if (isJumpCut)
                        rigidbody.gravityScale = gravityScale * jumpCutGravityMult;

                    else if ((isJumping || isFalling) && Mathf.Abs(rigidbody.velocity.y) < jumpHangTimeThreshold)
                    {
                        rigidbody.gravityScale = gravityScale * jumpHangGravityMult;
                        rigidbody.sharedMaterial = slipMaterial;
                    }

                    else if (rigidbody.velocity.y > 0.01f)
                    {
                        rigidbody.gravityScale = gravityScale * fallGravityMult;
                        rigidbody.velocity = new Vector2(rigidbody.velocity.x, Mathf.Max(rigidbody.velocity.y, -maxFallSpeed));
                    }

                    else
                        rigidbody.gravityScale = gravityScale;

                    break;
                }
            case GravityDirection.LEFT:
                {
                    if (isJumpCut)
                        rigidbody.gravityScale = gravityScale * jumpCutGravityMult;

                    else if ((isJumping || isFalling) && Mathf.Abs(rigidbody.velocity.x) < jumpHangTimeThreshold)
                    {
                        rigidbody.gravityScale = gravityScale * jumpHangGravityMult;
                        rigidbody.sharedMaterial = slipMaterial;
                    }

                    else if (rigidbody.velocity.x < -0.01f)
                    {
                        rigidbody.gravityScale = gravityScale * fallGravityMult;
                        rigidbody.velocity = new Vector2(Mathf.Max(rigidbody.velocity.x, -maxFallSpeed), rigidbody.velocity.y);
                    }
                    else
                        rigidbody.gravityScale = gravityScale;

                    break;
                }
        }
    }

*/

/*
 
private void UpdateAnimation()
    {
        // Set movement state based on deltaX
        movementState = (MathF.Abs(deltaX) > 0.0f) ? MovementState.RUNNING : MovementState.IDLE;


        switch (gravityDirection)
        {
            case GravityDirection.DOWN:
                {
                    if (rigidbody.velocity.y > 0.01f)
                    {
                        movementState = MovementState.JUMPING;
                    }
                    else if (isFalling)
                    {
                        movementState = MovementState.FALLING;
                    }
                    break;
                }
            case GravityDirection.RIGHT:
                {
                    if (rigidbody.velocity.x < -0.01f)
                    {
                        movementState = MovementState.JUMPING;
                    }
                    else if (isFalling)
                    {
                        movementState = MovementState.FALLING;
                    }
                    break;
                }
            case GravityDirection.UP:
                {
                    if (rigidbody.velocity.y < -0.01f)
                    {
                        movementState = MovementState.JUMPING;
                    }
                    else if (isFalling)
                    {
                        movementState = MovementState.FALLING;
                    }
                    break;
                }
            case GravityDirection.LEFT:
                {
                    if (rigidbody.velocity.x > 0.01f)
                    {
                        movementState = MovementState.JUMPING;
                    }
                    else if (isFalling)
                    {
                        movementState = MovementState.FALLING;
                    }
                    break;
                }
        }
        
        animator.SetInteger("movementState", (int)movementState);
    }

*/


/*
 

private void CheckInputs()
    {
        deltaX = 0.0f;
        if (Input.GetKey(KeyCode.A) && !Input.GetKey(KeyCode.D))        // Left
        {
            deltaX = -1.0f;
        }
        else if (Input.GetKey(KeyCode.D) && !Input.GetKey(KeyCode.A))   // Right
        {
            deltaX = 1.0f;
        }

        // This might be used for climbing ladders
        // int deltaY = 0;
        //if (Input.GetKey(KeyCode.S) && !Input.GetKey(KeyCode.W))        // Down
        //{
        //    deltaY = -1;
        //}
        //else if (Input.GetKey(KeyCode.W) && !Input.GetKey(KeyCode.S))   // Up
        //{
        //    deltaY = 1;
        //}

        if (Math.Abs(deltaX) > 0.0f)
        {
            if (gravityDirection != GravityDirection.UP)
                CheckDirectionToFace(deltaX > 0.0f);
            else
                CheckDirectionToFace(deltaX * (-1.0f) > 0.0f);
        }

        if (Input.GetKeyDown(KeyCode.Space))
        {
            lastPressedJumpTimer = jumpInputBufferTime;
        }

        if (Input.GetKeyUp(KeyCode.Space))
        {
            switch (gravityDirection)
            {
                case GravityDirection.DOWN:
                    {
                        if (isJumping && rigidbody.velocity.y > 0.01f)
                        {
                            isJumpCut = true;
                        }

                        break;
                    }
                case GravityDirection.RIGHT:
                    {
                        if (isJumping && rigidbody.velocity.x < -0.01f)
                        {
                            isJumpCut = true;
                        }

                        break;
                    }
                case GravityDirection.UP:
                    {
                        if (isJumping && rigidbody.velocity.y < -0.01f)
                        {
                            isJumpCut = true;
                        }

                        break;
                    }
                case GravityDirection.LEFT:
                    {
                        if (isJumping && rigidbody.velocity.x > 0.01f)
                        {
                            isJumpCut = true;
                        }

                        break;
                    }
            }
        }

        if (Input.GetKeyDown(KeyCode.N))
        {
            if (slash == null)
            {

                switch (gravityDirection)
                {
                    case GravityDirection.DOWN:
                        {
                            slash = Instantiate(slashPrefab, new Vector3(transform.position.x + attackRange * transform.localScale.x, transform.position.y, transform.position.z), transform.rotation);

                            break;
                        }
                    case GravityDirection.RIGHT:
                        {
                            slash = Instantiate(slashPrefab, new Vector3(transform.position.x, transform.position.y + attackRange * transform.localScale.x, transform.position.z), transform.rotation);

                            break;
                        }
                    case GravityDirection.UP:
                        {
                            slash = Instantiate(slashPrefab, new Vector3(transform.position.x - attackRange * transform.localScale.x, transform.position.y, transform.position.z), transform.rotation);

                            break;
                        }
                    case GravityDirection.LEFT:
                        {
                            slash = Instantiate(slashPrefab, new Vector3(transform.position.x, transform.position.y - attackRange * transform.localScale.x, transform.position.z), transform.rotation);

                            break;
                        }
                }

                slash.transform.localScale = transform.localScale;

            }
        }

        if (Input.GetKeyDown(KeyCode.Escape))
        {
            Application.Quit();
        }
    }


*/


/*
 

    private void Jump()
    {
        switch (gravityDirection)
        {
            case GravityDirection.DOWN:
                {
                    // Reset timers
                    lastPressedJumpTimer = 0.0f;
                    lastOnGroundTimer = 0.0f;

                    // Calculate resulting force (it has to be stronger if the player's falling to counter the fall's strength) 
                    float force = jumpForce;
                    if (rigidbody.velocity.y < -0.01f)
                        force -= rigidbody.velocity.y;

                    rigidbody.AddForce(Vector2.up * force, ForceMode2D.Impulse);

                    break;
                }
            case GravityDirection.RIGHT:
                {
                    lastPressedJumpTimer = 0.0f;
                    lastOnGroundTimer = 0.0f;

                    float force = jumpForce;
                    if (rigidbody.velocity.x > 0.01f)
                        force += rigidbody.velocity.x;

                    rigidbody.AddForce(Vector2.left * force, ForceMode2D.Impulse);

                    break;
                }
            case GravityDirection.UP:
                {
                    lastPressedJumpTimer = 0.0f;
                    lastOnGroundTimer = 0.0f;

                    float force = jumpForce;
                    if (rigidbody.velocity.y > 0.01f)
                        force -= rigidbody.velocity.y;

                    rigidbody.AddForce(Vector2.down * force, ForceMode2D.Impulse);

                    break;
                }
            case GravityDirection.LEFT:
                {
                    lastPressedJumpTimer = 0.0f;
                    lastOnGroundTimer = 0.0f;

                    float force = jumpForce;
                    if (rigidbody.velocity.x < -0.01f)
                        force -= rigidbody.velocity.x;

                    rigidbody.AddForce(Vector2.right * force, ForceMode2D.Impulse);

                    break;
                }
        }
    }


*/


