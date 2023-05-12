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
    private GravityDirection gravityDirectionBuffer = GravityDirection.DOWN;

    private List<GravityDirection> gravityDirectionQueue = new List<GravityDirection>();

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

    // Combat
    [Header("Combat Logic")]
    [SerializeField] private GameObject slashPrefab;
    private GameObject slash = null;
    [SerializeField] private float attackRange;
    [SerializeField] private float attackSpeed;

    // Timers
    private float lastPressedJumpTimer; 
    private float lastOnGroundTimer;    
    private float lastOnWallRightTimer; 
    private float lastOnWallLeftTimer;  
    private float lastOnWallTimer;
    private float lastOnGravityFieldTimer;
    private float iFramesTimer;
    private float attackTimer;






    // Gravity Logic
    [Header("Gravity Logic")]
    [SerializeField] private float gravityFieldTime;

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
        GameObject sceneManagerGO = GameObject.Find("SceneManager");
        if (sceneManager)
            sceneManager = sceneManagerGO.GetComponent<GameManager>();
    }

    private void Start()
    {
        gravityDirection = GravityDirection.DOWN;
        if (gravityDirection != GravityDirection.DOWN)
            gravityDirectionQueue.Add(gravityDirection);
        
        isGrounded = IsGrounded(Vector2.down);
        gravityMag = Physics.gravity.y;

        currentHP = maxHP;
        OnHealthChange(currentHP);
        pinappleCount = 0;
        OnPickUp(pinappleCount);
    }

    private void Update()
    {
        UpdateTimers();
        CheckInputs();
        CheckCollisions();
        CheckJumpState();
        // CheckSlide();
        UpdateGravity();

        UpdateAnimation();
    }

    private void FixedUpdate()
    {
        Run();

        // Handle Slide
        //if (isSliding)
        //    Slide();
    }

    private void UpdateTimers()
    {
        lastOnGroundTimer -= Time.deltaTime;
        lastOnWallTimer -= Time.deltaTime;
        lastOnWallRightTimer -= Time.deltaTime;
        lastOnWallLeftTimer -= Time.deltaTime;
        lastPressedJumpTimer -= Time.deltaTime;
        lastOnGravityFieldTimer -= Time.deltaTime;
        iFramesTimer -= Time.deltaTime;
        attackTimer -= Time.deltaTime;
    }

    private void CheckInputs()
    {
        //if (gravityDirectionQueue.Count == 0)
        //{
        //    gravityDirection = GravityDirection.DOWN;
        //    Rotate();
        //}
        //else if (gravityDirection != gravityDirectionQueue[0])
        //{
        //    gravityDirection = gravityDirectionQueue[0];
        //    Rotate();
        //}



        deltaX = 0.0f;
        if (Input.GetKey(KeyCode.A) && !Input.GetKey(KeyCode.D))        // Left
        {
            deltaX = -1.0f;
        }
        else if (Input.GetKey(KeyCode.D) && !Input.GetKey(KeyCode.A))   // Right
        {
            deltaX = 1.0f;
        }

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
            CheckDirectionToFace(deltaX > 0.0f);

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
                if (isFacingRight)
                    slash = Instantiate(slashPrefab, new Vector3(transform.position.x + 1.5f, transform.position.y, transform.position.z), transform.rotation);
                else
                {
                    slash = Instantiate(slashPrefab, new Vector3(transform.position.x - 1.5f, transform.position.y, transform.position.z), transform.rotation);
                    slash.transform.localScale = new Vector3(slash.transform.localScale.x * -1.0f, slash.transform.localScale.y, slash.transform.localScale.z);
                }
            }
        }

        if (Input.GetKeyDown(KeyCode.Escape))
        {
            Application.Quit();
        }
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


    private void UpdateAnimation()
    {
        
        if (MathF.Abs(deltaX) > 0.0f)
        {
            movementState = MovementState.RUNNING;
        }
        else
        {
            movementState = MovementState.IDLE;
        }

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

    private bool IsGrounded(Vector2 side)
    {
        return Physics2D.BoxCast(collider.bounds.center, collider.bounds.size, 0.0f, side, 0.1f, groundLayer);
    }

    void OnRestart()
    {
        //transform.position = spawnPoint.position;
        //rigidbody.velocity = new Vector2(0.0f,0.0f);

        //isDoubleJumpUp = false;

        //maxHP = 3;
        //currentHP = maxHP;
        
        //OnPlayerDamaged(currentHP);
        //pinappleCount = 0;
        //OnPickUp(Collectables.PINAPPLE, pinappleCount);
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
            HandleHit();
        }

        //else if (collision.gameObject.CompareTag("GravityLeft"))
        //{
        //    if (!gravityDirectionQueue.Contains(GravityDirection.LEFT))
        //        gravityDirectionQueue.Add(GravityDirection.LEFT);
        //}
        //else if (collision.gameObject.CompareTag("GravityRight"))
        //{
        //    if (!gravityDirectionQueue.Contains(GravityDirection.RIGHT))
        //        gravityDirectionQueue.Add(GravityDirection.RIGHT);
        //}
        //else if (collision.gameObject.CompareTag("GravityUp"))
        //{
        //    if (!gravityDirectionQueue.Contains(GravityDirection.UP))
        //        gravityDirectionQueue.Add(GravityDirection.UP);
        //}
    }

    private void OnTriggerStay2D(Collider2D collision)
    {
        if (collision.gameObject.CompareTag("Trap") || collision.gameObject.CompareTag("Enemy"))
        {
            HandleHit();
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
     
        //else if (collision.gameObject.CompareTag("GravityLeft"))
        //{
        //    if (gravityDirectionQueue[0] == GravityDirection.LEFT)
        //    {
        //        gravityDirectionQueue.Remove(GravityDirection.LEFT);
        //        Rotate();
        //    }
        //    else
        //        gravityDirectionQueue.Remove(GravityDirection.LEFT);
        //}
        //else if (collision.gameObject.CompareTag("GravityRight"))
        //{
        //    if (gravityDirectionQueue[0] == GravityDirection.RIGHT)
        //    {
        //        gravityDirectionQueue.Remove(GravityDirection.RIGHT);
        //        Rotate();
        //    }
        //    else
        //        gravityDirectionQueue.Remove(GravityDirection.RIGHT);
        //}
        //else if (collision.gameObject.CompareTag("GravityUp"))
        //{
        //    if (gravityDirectionQueue[0] == GravityDirection.UP)
        //    {
        //        gravityDirectionQueue.Remove(GravityDirection.UP);
        //        Rotate();
        //    }
        //    else
        //        gravityDirectionQueue.Remove(GravityDirection.UP);
        //}
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.gameObject.CompareTag("Trap") || collision.gameObject.CompareTag("Enemy"))
        {
            HandleHit();
        }
    }

    private void OnCollisionStay2D(Collision2D collision)
    {
        if (collision.gameObject.CompareTag("Trap") || collision.gameObject.CompareTag("Enemy"))
        {
            HandleHit();
        }
    }

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
                    //Increase are acceleration and maxSpeed when at the apex of their jump, makes the jump feel a bit more bouncy, responsive and natural
                    if ((isJumping || isFalling) && Mathf.Abs(rigidbody.velocity.y) < jumpHangTimeThreshold)
                    {
                        accelRate *= jumpHangAccelerationMult;
                        targetSpeed *= jumpHangMaxSpeedMult;
                    }

                    //We won't slow the player down if they are moving in their desired direction but at a greater speed than their maxSpeed
                    //if (doConserveMomentum && Mathf.Abs(RB.velocity.x) > Mathf.Abs(targetSpeed) && Mathf.Sign(RB.velocity.x) == Mathf.Sign(targetSpeed) && Mathf.Abs(targetSpeed) > 0.01f && LastOnGroundTime < 0)
                    //{
                    //    //Prevent any deceleration from happening, or in other words conserve are current momentum
                    //    //You could experiment with allowing for the player to slightly increae their speed whilst in this "state"
                    //    accelRate = 0;
                    //}


                    float speedDif = targetSpeed - rigidbody.velocity.x;
                    //Calculate force along x-axis to apply to thr player

                    float movement = speedDif * accelRate;

                    //Convert this to a vector and apply to rigidbody
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

                    float speedDif = targetSpeed - rigidbody.velocity.x * (-1.0f);

                    float movement = speedDif * accelRate;

                    if (Math.Abs(speedDif) > 1.0f)
                        rigidbody.AddForce(movement * Vector2.left, ForceMode2D.Force);

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

    private void OnEnable()
    {
        GameManager.OnRestart += OnRestart;
    }

    private void OnDisable()
    {
        GameManager.OnRestart -= OnRestart;
    }

    private void HandleHit()
    {
        if (iFramesTimer > 0.0f) // Virtually no hit
            return;

        currentHP--;
        OnHealthChange(currentHP);

        if (currentHP <= 0)
        {
            Die();
        }
        else
        {
            animator.SetTrigger("Hit");
            canMove = false;
            iFramesTimer = iFrames;
            rigidbody.AddForce(new Vector2(transform.localScale.x * -15.0f, 0.0f), ForceMode2D.Impulse);
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
}
