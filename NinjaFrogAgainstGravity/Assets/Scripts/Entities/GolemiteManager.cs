using UnityEngine;

public class GolemiteManager : MonoBehaviour
{
    private BoxCollider2D collider;

    private bool isOnLedge = false;
    private bool isOnWall = false;
    private bool isFacingLeft;

    [SerializeField] private float speed;
    [SerializeField] private LayerMask groundLayer;

    [SerializeField] private Vector2 ledgeCheckOffset;
    [SerializeField] private float aggroRange;
    [SerializeField] private LayerMask detectionLayers;

    [SerializeField] private float turnTime;
    private float turnTimer = 10f;

    private bool isAlive;

    private void Start()
    {
        collider = GetComponent<BoxCollider2D>();
        isFacingLeft = false;
        isOnLedge = isOnWall = false;
        isAlive = true;
    }

    private void Update()
    {
        if (!isAlive)
            return;

        turnTimer += Time.deltaTime;

        CheckForLedge();
        CheckCollision();
        
        Move();
    }

    private void CheckForLedge()
    {
        Vector3 rOrigin = transform.position;
        if (isFacingLeft)
        {
            rOrigin.x -= ledgeCheckOffset.x;
        }
        else
        {
            rOrigin.x += ledgeCheckOffset.x;
        }
        rOrigin.y += ledgeCheckOffset.y;

        if (!Physics2D.Raycast(rOrigin, Vector2.down, 10.5f, groundLayer))
        {
            // No floor ahead
            isOnLedge = true;
        }
    }

    private void CheckCollision()
    {
        if (isFacingLeft)
        {
            isOnWall = Physics2D.BoxCast(collider.bounds.center, collider.bounds.size, 0.0f, Vector2.left, 0.5f, groundLayer);
        }
        else
        {
            isOnWall = Physics2D.BoxCast(collider.bounds.center, collider.bounds.size, 0.0f, Vector2.right, 0.5f, groundLayer);
        }
    }

    private void Turn()
    {
        if (turnTimer < turnTime)
            return;

        Vector3 scale = transform.localScale;
        scale.x *= -1;
        transform.localScale = scale;

        isFacingLeft = !isFacingLeft;
        turnTimer = 0f;
    }

    private void Move()
    {
        if (!isOnLedge && !isOnWall)
        {
            Vector3 diff = Vector2.right * transform.localScale.x / Mathf.Abs(transform.localScale.x) * speed * Time.deltaTime;
            transform.position += diff;
        }
        else
        {
            isOnLedge = false;
            isOnWall = false;
            Turn();
        }
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("PlayerAttack") && isAlive)
        {
            isAlive = false;
            GetComponent<Animator>().SetTrigger("Death");
        }
    }

    private void Death()
    {
        BossTag[] bosses = FindObjectsOfType<BossTag>();
        for (int i = 0; i < bosses.Length; i++)
        {
            if (bosses[i].bossNumber == 1)
            {
                bosses[i].GetComponent<GolemManager>().OnGolemiteDeath();
            }
        }

        Destroy(gameObject);
    }
}
