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
    [SerializeField] private GameObject exclamation;


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
        CheckTimers();
        CheckLedge();
        CheckCollision();
        CheckForPlayer();
        UpdateAnimation();

        if (isAttacking)
            return;

        if (target)
        {
            if (exclamation)
            {
                if (!exclamation.activeInHierarchy)
                {
                    exclamation.SetActive(true);
                }
            }
            if (Mathf.Abs(target.position.x - transform.position.x) <= attackRange)
            {
                // Attack
                 Attack();
            }
            else
            {
                if (isFacingLeft)
                    transform.position += Vector3.left * speed /* Maybe a different speed (?) */ * Time.deltaTime;
                else
                    transform.position += Vector3.right * speed /* Maybe a different speed (?) */ * Time.deltaTime;
            }

            return;
        }
        else if (exclamation)
        {
            if (exclamation.activeInHierarchy)
            {
                exclamation.SetActive(false);
            }
        }


        switch (currentBehaviour)
        {
            case EnemyBehaviours.IDLE:
                {
                    return;
                }
            case EnemyBehaviours.PATROL:
                {
                    if (!isOnLedge) // TODO: get a way to unstuck (maybe a timer to repath (?) )
                        transform.position += Vector3.Normalize(currentPatrolPoint.transform.position - transform.position) * speed * Time.deltaTime;

                    if (Vector2.Distance(transform.position, currentPatrolPoint.transform.position) < 0.05f)
                    {
                        UpdateCurrentPatrolPoint();
                        CheckDirectionToFace((currentPatrolPoint.transform.position.x - transform.position.x) < -0.0f);
                    }
                    break;
                }
            case EnemyBehaviours.WANDER:
                {
                    if (!isOnLedge && !isOnWall)
                    {
                        if (isFacingLeft)
                            transform.position += Vector3.left * speed * Time.deltaTime;
                        else
                            transform.position += Vector3.right * speed * Time.deltaTime;
                    }
                    else
                    {
                        Turn();
                        isOnLedge = false;
                        isOnWall = false;
                    }
                    
                    break;
                }
        }
        
    }


    private void CheckDirectionToFace(bool isFacingLeft)
    {
        if (this.isFacingLeft != isFacingLeft)
            Turn();
    }

    private void CheckTimers()
    {
        turnTimer -= Time.deltaTime;
        attackTimer -= Time.deltaTime;
        if (attackTimer <= 0.0f)
            SetIsAttacking(false);
    }
    private void CheckLedge()
    {
        Vector3 rOrigin;
        if (isFacingLeft)
            rOrigin = transform.position + ledgeCheckOffset.x * Vector3.left;
        else
            rOrigin = transform.position + ledgeCheckOffset.x * Vector3.right;

        if (!Physics2D.Raycast(rOrigin, Vector2.down, ledgeCheckOffset.y, groundLayer))
        {
            // No floor ahead
            isOnLedge = true;
        }
    }

    private void CheckCollision()
    {
        if (isFacingLeft)
            isOnWall = IsCollidingWithSide(Vector2.left);
        else
            isOnWall = IsCollidingWithSide(Vector2.right);
    }

    private void CheckForPlayer()
    {
        // Look for target
        RaycastHit2D hit;
        if (isFacingLeft)
            hit = Physics2D.Raycast(transform.position, Vector2.left, aggroRange, detectionLayers);
        else
            hit = Physics2D.Raycast(transform.position, Vector2.right, aggroRange, detectionLayers);

        if (hit && hit.collider.CompareTag("Player"))
            target = hit.transform;
        
        else
            target = null;
    }

    private void UpdateAnimation()
    {
        animator.SetInteger("currentBehaviour", (int)currentBehaviour);
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

        //if (currentPatrolPoint.transform.position.x > transform.position.x)
        //{
        //    if (hasAnimator)
        //        animator.SetBool("isGoingLeft", false);
        //}
        //
        //else
        //{
        //    if (hasAnimator)
        //        animator.SetBool("isGoingLeft", true);
        //}
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
    }
    private void EndAttack()
    {
        attackTimer = attackSpeed;
        colliders[1].enabled = false;
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
        return Physics2D.BoxCast(collider.bounds.center, collider.bounds.size, 0.0f, side, 0.1f, groundLayer);
    }

    private void SetIsAttacking(bool setTo)
    {
        isAttacking = setTo;
        animator.SetBool("isAttacking", isAttacking);
    }
}
