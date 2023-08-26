using System.Collections.Generic;
using UnityEngine;

public class GolemManager : MonoBehaviour
{
    private enum GolemState
    {
        INACTIVE = 0,

        SPAWN,
        SUMMONING,
        VULNERABLE,
        LASER,
        DEAD
    }

    private GolemState currentState;

    private Animator animator;

    [SerializeField] private int currentHP;
    [SerializeField] private int maxHP;
    private int currentFase;
    private bool isVulnerable;
    [SerializeField] private float iFrames;
    private float iFramesTimer;
    private float transitionTimer;
    private float laserTimer;
    [SerializeField] private float laserDuration;

    [SerializeField] private Transform player;

    [SerializeField] private GameObject golemitePrefab;
    [SerializeField] private GameObject laserPrefab;

    [SerializeField] private float laserMaxRange;
    private Vector3 laserTarget;
    [SerializeField] private float rotationSpeed;  // Speed of rotation in degrees per second
    private float currentAngle = 0.0f;
    private bool isLaserActive = false;
    private GameObject laser;
    [SerializeField] private Vector3 laserSpawnOffset;
    [SerializeField] private LayerMask laserLayerMask;

    private List<Transform> golemiteSpawns = new List<Transform>();
    public int golemitesAlive;

    private float deathHeight = -9.67f;
    private float deathRepositionSpeed = 3f;

    private bool isTransitioning;
    private float transitionSpawn = 2f;
    private float transitionLaser = 1f;

    public bool debugDeathTrigger = false;

    private void Awake()
    {
        animator = GetComponent<Animator>();
    }

    private void Start()
    {
        currentState = GolemState.INACTIVE;
        isVulnerable = false;
        isTransitioning = false;

        currentFase = 0;
        currentHP = maxHP;

        GameObject gameObject = FindObjectOfType<GolemSpawnTag>().gameObject;
        for (int i = 0; i < gameObject.transform.childCount; i++)
        {
            golemiteSpawns.Add(gameObject.transform.GetChild(i));
        }
    }

    private void Update()
    {
        if (currentState == GolemState.SPAWN)
        {
            transitionTimer += Time.deltaTime;
            if (isTransitioning && transitionTimer > transitionSpawn)
            {
                isTransitioning = false;
                animator.SetTrigger("Summon");
            }
        }
        else if (currentState == GolemState.VULNERABLE)
        {
            iFramesTimer += Time.deltaTime;
            if (iFramesTimer > iFrames)
            {
                isVulnerable = true;
            }
        }
        else if (currentState == GolemState.LASER)
        {
            transitionTimer += Time.deltaTime;

            if (isTransitioning && transitionTimer > transitionLaser)
            {
                isTransitioning = false;
                animator.SetTrigger("Laser");
            }

            if (isLaserActive)
            {
                laserTimer += Time.deltaTime;
                if (laserTimer >= laserDuration)
                {
                    isLaserActive = false;
                    animator.SetTrigger("Summon");
                    Destroy(laser);
                    return;
                }
                currentAngle += rotationSpeed * Time.deltaTime;
                laserTarget = transform.position + Quaternion.Euler(0, 0, currentAngle) * Vector3.right * laserMaxRange;

                LineRenderer lineRenderer = laser.GetComponentInChildren<LineRenderer>();
                Vector3[] line = new Vector3[2];
                line[0] = transform.position + laserSpawnOffset;
                line[1] = laserTarget;
                lineRenderer.SetPositions(line);
                Vector2 vec = line[1] - line[0];
                RaycastHit2D hit = Physics2D.Raycast(line[0], vec, laserMaxRange, laserLayerMask);
                if (hit)
                {
                    player.GetComponent<PlayerMovement>().HandleHit(Vector3.zero, true);
                }
            }
        }
        else if (currentState == GolemState.DEAD) 
        {
            if (Mathf.Abs(transform.position.y - deathHeight) > .1f)
            {
                transform.position += new Vector3(0f, deathRepositionSpeed * Time.deltaTime, 0f);
            }
            else
                transform.position = new Vector3(transform.position.x, deathHeight, transform.position.z);
        }
        if (debugDeathTrigger)
        {
            debugDeathTrigger = false;
            Death();
        }
    }

    private void StartTransition()
    {
        transitionTimer = 0f;
        isTransitioning = true;
    }

    public void Spawn()
    {
        currentState = GolemState.SPAWN;
        isVulnerable = false;

        animator.SetTrigger("Spawn");
    }

    public void Summon()
    {
        currentFase++;
        currentState = GolemState.SUMMONING;
        isVulnerable = false;

        if (currentFase == 1)
        {
            Instantiate(golemitePrefab, golemiteSpawns[0].position, Quaternion.identity);
            Instantiate(golemitePrefab, golemiteSpawns[1].position, Quaternion.identity);
            golemitesAlive = 2;
        }
        else if (currentFase == 2)
        {
            Instantiate(golemitePrefab, golemiteSpawns[0].position, Quaternion.identity);
            Instantiate(golemitePrefab, golemiteSpawns[1].position, Quaternion.identity);
            Instantiate(golemitePrefab, golemiteSpawns[2].position, Quaternion.identity);
            Instantiate(golemitePrefab, golemiteSpawns[3].position, Quaternion.identity);
            golemitesAlive = 4;
        }
        else if (currentFase == 3)
        {
            Vector3 offset = new Vector3(3.5f, 0f, 0f);
            Instantiate(golemitePrefab, golemiteSpawns[0].position + offset, Quaternion.identity);
            Instantiate(golemitePrefab, golemiteSpawns[0].position - offset, Quaternion.identity);
            Instantiate(golemitePrefab, golemiteSpawns[1].position + offset, Quaternion.identity);
            Instantiate(golemitePrefab, golemiteSpawns[1].position - offset, Quaternion.identity);
            Instantiate(golemitePrefab, golemiteSpawns[2].position, Quaternion.identity);
            Instantiate(golemitePrefab, golemiteSpawns[3].position, Quaternion.identity);
            golemitesAlive = 6;
        }
        else
            Debug.Log("Error spawning");
    }

    private void Laser()
    {
        laser = Instantiate(laserPrefab, transform.position, transform.rotation);
    }

    public void ActivateLaser() // Called from laser prefab
    {
        laserTarget = transform.position;
        laserTarget.x += laserMaxRange;

        isLaserActive = true;
        laserTimer = 0f;
    }

    public void OnGolemiteDeath()
    {
        golemitesAlive--;
        if (golemitesAlive > 0)
            return;

        animator.SetTrigger("Vulnerable");
        currentState = GolemState.VULNERABLE;
        isVulnerable = true;
    }

    private void Hit()
    {
        currentHP--;
        if (currentHP <= 0)
        {
            if (currentFase < 3)
            {
                currentHP = maxHP;
                currentState = GolemState.LASER;
                animator.SetTrigger("Invulnerable");
                StartTransition();
            }
            else
            {
                Death();
            }
        }
        else
        {
            isVulnerable = false;
            iFramesTimer = 0f;
            animator.SetTrigger("Hurt");
        }
    }

    private void Death()
    {
        animator.SetTrigger("Death");
        currentState = GolemState.DEAD;
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("PlayerAttack") && isVulnerable)
        {
            Hit();
        }
    }
}
