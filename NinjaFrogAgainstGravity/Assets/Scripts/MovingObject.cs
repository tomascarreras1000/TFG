using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MovingObject : MonoBehaviour
{
    [SerializeField] private GameObject[] patrolPoints;
    private GameObject currentPatrolPoint;
    private int patrolIndex;
    private bool isBouncing;

    [SerializeField] private float speed;
    [SerializeField] private bool hasToBounce;

    private Animator animator;
    private bool hasAnimator;
    private bool isFacingLeft = true;

    private bool isResting = false;
    private float restTimer = 0.0f;
    [SerializeField] private float restTime;

    private void Start()
    {
        animator = GetComponent<Animator>();
        if (animator != null)
            hasAnimator = true;
        
        else
            hasAnimator = false;

        patrolIndex = 0;
        currentPatrolPoint = patrolPoints[patrolIndex];
    }

    private void Update()
    {
        if (isResting)
        {
            restTimer -= Time.deltaTime;
            if (restTimer <= 0.0f)
                isResting = !isResting;
            return;
        }

        bool isGoingDown = false;
        if (transform.position.y > currentPatrolPoint.transform.position.y) 
        {
            isGoingDown = true;
        }
        
        transform.position += Vector3.Normalize(currentPatrolPoint.transform.position - transform.position) * speed * Time.deltaTime;

        if (isGoingDown)
        {
            if (transform.position.y < currentPatrolPoint.transform.position.y)
            {
                transform.position = new Vector3(transform.position.x, currentPatrolPoint.transform.position.y, transform.position.z);
            }
        }
        else if (transform.position.y > currentPatrolPoint.transform.position.y)
        {
            transform.position = new Vector3(transform.position.x, currentPatrolPoint.transform.position.y, transform.position.z);
        }


        if (Vector2.Distance(transform.position, currentPatrolPoint.transform.position) < 0.05f)
        {
            UpdateCurrentPatrolPoint();
            Rest();
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

        if (currentPatrolPoint.transform.position.x > transform.position.x)
        {
            if (hasAnimator)
                animator.SetBool("isGoingLeft", false);
        }

        else
        {
            if (hasAnimator)
                animator.SetBool("isGoingLeft", true);
        }
    }

    private void Rest()
    {
        isResting = true;
        restTimer = restTime;
    }
}
