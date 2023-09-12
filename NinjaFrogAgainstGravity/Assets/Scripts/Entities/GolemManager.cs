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

    private int currentHP;
    [SerializeField] private int maxHP;
    private bool isVulnerable;
    [SerializeField] private float iFrames;
    [SerializeField] private float vulnerableDuration;
    private float iFramesTimer;
    private float transitionTimer;
    private float laserTimer;
    private float vulnerableTimer;
    [SerializeField] private float laserDuration;

    [SerializeField] private Transform player;

    [SerializeField] private GameObject golemitePrefab;
    [SerializeField] private GameObject dustPrefab;
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
    private float transitionSpawn = 3.5f;
    private float transitionLaser = 1f;
    private bool isSummonNotice;

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
        isSummonNotice = true;

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
                isSummonNotice = true;
            }
        }
        else if (currentState == GolemState.VULNERABLE)
        {
            iFramesTimer += Time.deltaTime;
            vulnerableTimer += Time.deltaTime;
            if (iFramesTimer >= iFrames)
            {
                isVulnerable = true;
            }
            if (vulnerableTimer >= vulnerableDuration)
            {
                currentState = GolemState.LASER;
                animator.SetTrigger("Invulnerable");
                StartTransition();
                isVulnerable = false;
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
                    isSummonNotice = true;
                    Destroy(laser);
                    return;
                }
                currentAngle += rotationSpeed * (maxHP / currentHP) * 0.75f * Time.deltaTime;
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
            {
                transform.position = new Vector3(transform.position.x, deathHeight, transform.position.z);
                FindObjectOfType<GameManager>().OnBossDeath(1);
                GetComponent<GolemManager>().enabled = false;
            }
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
        Debug.Log("Spawning golem");
        currentState = GolemState.SPAWN;
        isVulnerable = false;

        animator.SetTrigger("Spawn");
    }

    public void Summon() 
    {
        currentState = GolemState.SUMMONING;
        isVulnerable = false;

        if (currentHP > 6)
        {
            Instantiate(isSummonNotice ? dustPrefab : golemitePrefab, golemiteSpawns[0].position, Quaternion.identity);
            Instantiate(isSummonNotice ? dustPrefab : golemitePrefab, golemiteSpawns[1].position, Quaternion.identity);
            golemitesAlive = 2;
        }
        else if (currentHP > 3)
        {
            Instantiate(isSummonNotice ? dustPrefab : golemitePrefab, golemiteSpawns[0].position, Quaternion.identity);
            Instantiate(isSummonNotice ? dustPrefab : golemitePrefab, golemiteSpawns[1].position, Quaternion.identity);
            Instantiate(isSummonNotice ? dustPrefab : golemitePrefab, golemiteSpawns[2].position, Quaternion.identity);
            Instantiate(isSummonNotice ? dustPrefab : golemitePrefab, golemiteSpawns[3].position, Quaternion.identity);
            golemitesAlive = 4;
        }
        else
        {
            Vector3 offset = new Vector3(3.5f, 0f, 0f);
            Instantiate(isSummonNotice ? dustPrefab : golemitePrefab, golemiteSpawns[0].position + offset, Quaternion.identity);
            Instantiate(isSummonNotice ? dustPrefab : golemitePrefab, golemiteSpawns[0].position - offset, Quaternion.identity);
            Instantiate(isSummonNotice ? dustPrefab : golemitePrefab, golemiteSpawns[1].position + offset, Quaternion.identity);
            Instantiate(isSummonNotice ? dustPrefab : golemitePrefab, golemiteSpawns[1].position - offset, Quaternion.identity);
            Instantiate(isSummonNotice ? dustPrefab : golemitePrefab, golemiteSpawns[2].position, Quaternion.identity);
            Instantiate(isSummonNotice ? dustPrefab : golemitePrefab, golemiteSpawns[3].position, Quaternion.identity);
            golemitesAlive = 6;
        }

        if (isSummonNotice)
            isSummonNotice = !isSummonNotice;
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
        vulnerableTimer = 0f;
    }

    private void Hit()
    {
        currentHP--;
        if (currentHP <= 0)
        {
            Death();
            return;
        }

        if (currentHP == 6 || currentHP == 3)
        {
            currentState = GolemState.LASER;
            animator.SetTrigger("Invulnerable");
            StartTransition();
        }
        else
            animator.SetTrigger("Hurt");

        isVulnerable = false;
        iFramesTimer = 0f;
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
