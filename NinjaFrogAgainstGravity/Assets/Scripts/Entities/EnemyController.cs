using System;
using System.Collections.Generic;
using UnityEngine;

public class EnemyController : MonoBehaviour
{
    // General
    private Animator animator;
    private BoxCollider2D collider;
    private BoxCollider2D[] colliders;

    // Movement
    private bool isFacingLeft;
    private float turnTimer = 0.0f;

    // Behaviour
    private bool isAlive = true;

    // Patrol
    private GameObject currentPatrolPoint;
    private int patrolIndex;
    private bool isBouncing;

    // Wander
    private bool isOnLedge = false;
    private bool isOnWall = false;

    // Player interaction
    private Transform target = null;
    private float attackTimer = 0.0f;
    private bool isAttacking = false;
    private GameObject slashEffect = null;
    private GameObject exclamation = null;

    private enum EnemyBehaviours
    {
        NO_BEHAVIOUR = 0,

        IDLE = 1,
        PATROL = 2,
        WANDER = 3
    }

    [Header("Movement Logic")]
    [SerializeField] private float speed;
    [SerializeField] private bool isFacingLeftStart = true;
    [SerializeField] private LayerMask groundLayer;
    [SerializeField] private float turnTime;

    [Header("Behaviours Logic")]
    [SerializeField] private EnemyBehaviours currentBehaviour;

    [Header("Patrolling Logic")]
    [SerializeField] private GameObject[] patrolPoints;
    [SerializeField] private bool hasToBounce;

    [Header("Wander Logic")]
    [SerializeField] private Vector2 ledgeCheckOffset;

    [Header("Player interaction")]
    [SerializeField] private float aggroRange;
    [SerializeField] private float attackRange;
    [SerializeField] private float attackSpeed;
    [SerializeField] private LayerMask detectionLayers;
    [SerializeField] private GameObject exclamationPrefab;
    [SerializeField] private GameObject slashPrefab;


    private void Start()
    {
        collider = GetComponent<BoxCollider2D>();
        colliders = GetComponents<BoxCollider2D>();
        animator = GetComponent<Animator>();

        animator.SetInteger("currentBehaviour", (int)currentBehaviour);

        if (patrolPoints.Length > 0)
        {
            patrolIndex = 0;
            currentPatrolPoint = patrolPoints[patrolIndex];
        }

        isFacingLeft = isFacingLeftStart;
    }

    private void Update()
    {
        if (!isAlive)
            return;

        UpdateLocalTimers();
        CheckForLedge();
        CheckCollision();
        CheckForPlayer();
        UpdateCurrentAnimation();

        if (isAttacking)
            return;

        if (target)
            HandleAttack();
        else
            HandleCurrentBehaviour();
    }

    private void CheckDirectionToFace(bool isFacingLeft)
    {
        if (this.isFacingLeft != isFacingLeft)
            Turn();
    }

    private void UpdateLocalTimers()
    {
        turnTimer -= Time.deltaTime;
        attackTimer -= Time.deltaTime;
        if (attackTimer <= 0.0f)
            SetIsAttacking(false);
    }
    private void CheckForLedge()
    {
        Vector3 rOrigin = transform.position + ledgeCheckOffset.x * transform.right * transform.localScale.x * (-1.0f);

        if (!Physics2D.Raycast(rOrigin, transform.up * (-1.0f), ledgeCheckOffset.y, groundLayer))
        {
            // No floor ahead
            isOnLedge = true;
        }
    }

    private void CheckCollision()
    {
        isOnWall = IsCollidingWithSide(transform.right * transform.localScale.x * (-1.0f));
    }

    private void CheckForPlayer()
    {
        // Look for target
        RaycastHit2D hit = Physics2D.Raycast(transform.position, transform.right * transform.localScale.x * (-1.0f), aggroRange, detectionLayers);

        if (hit && hit.collider.CompareTag("Player"))
            target = hit.transform;
        else
            target = null;
    }

    private void UpdateCurrentAnimation()
    {
        animator.SetInteger("currentBehaviour", (int)currentBehaviour);
    }

    private void HandleAttack()
    {
        CreateExclamation();

        if (Vector3.Distance(transform.position, target.position) <= attackRange)
            Attack();
        else
            transform.position += transform.right * transform.localScale.x * (-1.0f) * speed * Time.deltaTime;
    }

    private void CreateExclamation()
    {
        if (exclamation)
            return;

        Vector3 offset = new Vector3(-1.0f, 1.0f, 0.0f);
        exclamation = Instantiate(exclamationPrefab, transform.position, transform.rotation, transform);
        exclamation.transform.localScale = transform.localScale;
        exclamation.transform.Translate(offset);
    }

    private void HandleCurrentBehaviour()
    {
        if (exclamation)
            Destroy(exclamation);

        switch (currentBehaviour)
        {
            case EnemyBehaviours.IDLE:
                return;
            case EnemyBehaviours.PATROL:
                Patrol();
                break;
            case EnemyBehaviours.WANDER:
                Wander();
                break;
        }
    }

    private void Patrol()
    {
        if (!isOnLedge) // TODO: get a way to unstuck (maybe a timer to repath (?) )
            transform.position += Vector3.Normalize(currentPatrolPoint.transform.position - transform.position) * speed * Time.deltaTime;

        if (Vector2.Distance(transform.position, currentPatrolPoint.transform.position) < 0.05f)
        {
            UpdateCurrentPatrolPoint();
            CheckDirectionToFace((currentPatrolPoint.transform.position.x - transform.position.x) < -0.0f);
        }
    }

    private void Wander()
    {
        if (!isOnLedge && !isOnWall)
        {
            transform.position += transform.right * transform.localScale.x * (-1.0f) * speed * Time.deltaTime;
        }
        else
        {
            Turn();
            isOnLedge = false;
            isOnWall = false;
        }
    }
    private void UpdateCurrentPatrolPoint()
    {
        if (isBouncing)
        {
            patrolIndex--;

            if (patrolIndex < 0)
            {
                isBouncing = false;
                patrolIndex = 1;
            }

            currentPatrolPoint = patrolPoints[patrolIndex];
        }
        else
        {
            patrolIndex++;

            if (patrolIndex >= patrolPoints.Length)
            {
                if (hasToBounce)
                {
                    patrolIndex--;
                    isBouncing = true;
                }
                else
                    patrolIndex = 0;
            }
            currentPatrolPoint = patrolPoints[patrolIndex];
        }
    }

    private void Attack()
    {
        attackTimer = 1000.0f; // This is for the timer check not to set isAttacking to false before it should :/
        animator.SetTrigger("Attack");
        SetIsAttacking(true);
    }
   

    private void AddAttackColliders()
    {
        colliders[1].enabled = true;
        if (slashEffect == null)
        {
            if (isFacingLeft)
                slashEffect = Instantiate(slashPrefab, transform.position, transform.rotation, transform.parent);
            else
            {
                slashEffect = Instantiate(slashPrefab, transform.position, transform.rotation, transform.parent);
                slashEffect.transform.localScale = transform.localScale;
            }
        }
    }
    private void EndAttack()
    {
        attackTimer = attackSpeed;
        colliders[1].enabled = false;
        if (slashEffect != null)
            Destroy(slashEffect);
    }

    private void Turn()
    {
        if (turnTimer > 0.0f)
            return;

        Vector3 scale = transform.localScale;
        scale.x *= -1;
        transform.localScale = scale;

        isFacingLeft = !isFacingLeft;
        turnTimer = turnTime;
    }

    private bool IsCollidingWithSide(Vector2 side)
    {
        return Physics2D.BoxCast(collider.bounds.center, collider.bounds.size, 0.0f, side, 0.5f, groundLayer);
    }

    private void SetIsAttacking(bool setTo)
    {
        isAttacking = setTo;
        animator.SetBool("isAttacking", isAttacking);
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("PlayerAttack") && isAlive)
        {
            Die();
        }
    }

    private void Die()
    {
        isAlive = false;
        animator.SetTrigger("Death");
        collider.enabled = false;
        EndAttack(); // This is to prevent a bug where if the crab dies while on his attack animation this would never be called and the trigger would stay active
    }
}
