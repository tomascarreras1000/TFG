using UnityEngine;

public class MinotaurManager : MonoBehaviour
{
    private enum MinotaurState
    {
        INACTIVE = 0,

        CHARGE,
        SMASH,
        SPIN,
        DEAD
    }
    [SerializeField] private MinotaurState currentState;

    private Animator animator;
    private BoxCollider2D collider;
    private Rigidbody2D rigidbody;

    [SerializeField] private int maxHP;
    [SerializeField] private Transform player;
    [SerializeField] private LayerMask detectionLayers;
    [SerializeField] private LayerMask groundLayer;
    [SerializeField] private float stunDuration;
    [SerializeField] private float speed;
    [SerializeField] private float chargingSpeed;
    [SerializeField] private float spinSpeed;
    [SerializeField] private float tauntDuration;
    [SerializeField] private float idleTurnTime;
    [SerializeField] private float detectionRange;
    [SerializeField] private float pushbackForce;
    [SerializeField] private float smashRange;
    [SerializeField] private GameObject smashPrefab;
    [SerializeField] private float spinDuration;

    private int currentHP;
    private bool isFacingLeft;
    private bool isVulnerable;

    private float transitionTimer;
    private float stunTimer = 10f;
    private float tauntTimer;
    private float idleTurnTimer;
    private float spinTimer;

    private float currentStunDuration;

    private bool isCharging = false;
    private bool isTaunting = false;
    private Vector3 direction;
    private bool isPlayerInSight = false;
    private bool isPlayerInSmashRange = false;
    private bool isSmashing = false;
    private bool isSpinning = false;
    private bool hasToSpin = false;
    private int currentSmashCount = 0;
    private GameObject smashObject;

    [SerializeField] private bool d_cycleStateTrigger;

    private void Awake()
    {
        animator = GetComponent<Animator>();
        collider = GetComponent<BoxCollider2D>();
        rigidbody = GetComponent<Rigidbody2D>();
    }

    private void Start()
    {
        currentState = MinotaurState.INACTIVE;
        currentHP = maxHP;
        isVulnerable = false;
        isFacingLeft = transform.localScale.x < 0f;
        direction = Vector3.right * transform.localScale.x / Mathf.Abs(transform.localScale.x);
        idleTurnTimer = 0f;

        currentStunDuration = stunDuration;
    }

    private void Update()
    {
        UpdateTimers();
        if (currentState == MinotaurState.DEAD)
        {

        }
        else if (stunTimer < currentStunDuration)
        {
            stunTimer += Time.deltaTime;
            if (stunTimer >= currentStunDuration)
            {
                animator.SetTrigger("Unstun");
                isVulnerable = false;
            }
        }
        else if (currentState == MinotaurState.CHARGE)
        {
            if (isCharging)
            {
                bool hasToStun;
                if (CheckCollision(out hasToStun))
                {
                    isCharging = false;
                    isVulnerable = true;

                    if (hasToStun)
                    {
                        stunTimer = 0f;
                        animator.SetTrigger("Stun");
                    }

                    rigidbody.AddForce(-direction * pushbackForce, ForceMode2D.Impulse);
                }
                else
                    transform.position += direction * chargingSpeed * Time.deltaTime;
            }
            else
            {
                CheckForPlayer(); //Taunt cancels if player leaves sight

                if (isPlayerInSight)
                    Taunt();
                else
                {
                    idleTurnTimer += Time.deltaTime;
                    if (idleTurnTimer >= idleTurnTime)
                    {
                        idleTurnTimer = 0f;
                        Turn();
                    }
                }
            }
        }
        else if (currentState == MinotaurState.SMASH)
        {
            float deltaX = player.position.x - transform.position.x;
            if (deltaX / Mathf.Abs(deltaX) != direction.x)
                Turn();

            isPlayerInSmashRange = Mathf.Abs(deltaX) <= smashRange;
            animator.SetBool("isPlayerInSmashRange", isPlayerInSmashRange);

            if (!isSmashing)
                transform.position += direction * speed * Time.deltaTime;
        }
        else if (currentState == MinotaurState.SPIN)
        {
            float deltaX = player.position.x - transform.position.x;
            if (deltaX / Mathf.Abs(deltaX) != direction.x)
                Turn();

            if (isSpinning)
            {
                if (spinTimer >= spinDuration)
                {
                    animator.SetTrigger("SpinEnd");
                    isSpinning = false;
                }
                else if (Mathf.Abs(deltaX) > 1f)
                    transform.position += direction * spinSpeed * Time.deltaTime;

                if (smashObject)
                    smashObject.transform.position = transform.position;
            }
        }

        if (d_cycleStateTrigger)
            Debug_CycleState();
    }

    private void UpdateTimers()
    {
        spinTimer += Time.deltaTime;
    }

    public void Spawn()
    {
        Debug.Log("Spawning minotaur");

        currentState = MinotaurState.CHARGE;
        idleTurnTimer = 0f;
        isVulnerable = false;
    }

    private bool CheckCollision(out bool hasToStun)
    {
        RaycastHit2D hit = Physics2D.BoxCast(collider.bounds.center, collider.bounds.size, 0f, direction, 0.1f, detectionLayers);
        hasToStun = true;
        if (hit && hit.collider.CompareTag("Player"))
        {
            player.GetComponent<PlayerMovement>().HandleHit(hit.point);
            hasToStun = false;
        }
        return hit;
    }

    private void CheckForPlayer()
    {
        // Look for player
        RaycastHit2D hit = Physics2D.BoxCast(transform.position, collider.bounds.size, 0f, direction, detectionRange, detectionLayers);

        if (hit && hit.collider.CompareTag("Player"))
            isPlayerInSight = true;
        else
        {
            isPlayerInSight = false;
            isTaunting = false;
        }

        animator.SetBool("isPlayerInSight", isPlayerInSight);
    }

    private void Turn()
    {
        Vector3 scale = transform.localScale;
        scale.x *= -1;
        transform.localScale = scale;

        direction = Vector2.right * scale.x / Mathf.Abs(scale.x);

        isFacingLeft = !isFacingLeft;
    }

    private void Taunt()
    {
        if (!isTaunting)
        {
            isTaunting = true;
            animator.SetTrigger("Taunt");
            tauntTimer = 0f;
            return;
        }

        tauntTimer += Time.deltaTime;
        idleTurnTimer = 0f;
        if (tauntTimer >= tauntDuration)
        {
            isTaunting = false;
            BeginCharge();
        }
    }

    private void BeginCharge()
    {
        isCharging = true;
        animator.SetTrigger("Charge");
    }

    private void AC_StunToSmash()
    {
        currentState = MinotaurState.SMASH;
    }
    
    private void AC_BeginSmash()
    {
        isSmashing = true;
    }

    private void AC_GenerateSmash()
    {
        smashObject = Instantiate(smashPrefab, transform.position, transform.rotation);        
    }

    private void AC_EndSmash()
    {
        isSmashing = false;
        if (smashObject)
            Destroy(smashObject);
        currentSmashCount++;
        if (currentSmashCount == 3)
        {
            if (hasToSpin)
            {
                currentState = MinotaurState.SPIN;
                animator.SetTrigger("Spin");
            }
            else
            {
                currentState = MinotaurState.CHARGE;
                animator.SetTrigger("Taunt");
            }
        }
    }

    private void AC_BeginSpin()
    {
        isSpinning = true;
        spinTimer = 0f;
        isVulnerable = false;
        smashObject = Instantiate(smashPrefab, transform.position, transform.rotation);
    }

    private void AC_EndSpin()
    {
        isSpinning = false;
        currentState = MinotaurState.CHARGE;
        if (smashObject)
            Destroy(smashObject);
    }

    private void Hit()
    {
        currentHP--;
        if (currentHP <= 0)
        {
            Death();
            return;
        }

        isVulnerable = false;

        animator.SetTrigger("Smash");
        stunTimer = currentStunDuration;
        currentSmashCount = 0;

        if (currentHP == 2)
        {
            speed *= 1.25f;
            chargingSpeed *= 1.25f;
            currentStunDuration *= 0.5f;
            hasToSpin = true;
        }
    }

    private void Death()
    {
        animator.SetTrigger("Death");
        currentState = MinotaurState.DEAD;
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("PlayerAttack") && isVulnerable)
        {
            Hit();
        }
    }

    private void Debug_CycleState()
    {
        if (currentState == MinotaurState.INACTIVE)
        {
            currentState = MinotaurState.CHARGE;
            isTaunting = false;
            isCharging = false;
        }
        else if (currentState == MinotaurState.CHARGE)
            currentState = MinotaurState.SMASH;
        else if (currentState == MinotaurState.SMASH)
        {
            currentState = MinotaurState.SPIN;
            animator.SetTrigger("Spin");
            spinTimer = 0f;
        }
        else if (currentState == MinotaurState.SPIN)
            currentState = MinotaurState.DEAD;

        d_cycleStateTrigger = !d_cycleStateTrigger;
    }
}
