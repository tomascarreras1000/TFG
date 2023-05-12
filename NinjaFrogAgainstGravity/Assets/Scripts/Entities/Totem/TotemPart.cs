using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TotemPart : MonoBehaviour
{
    [SerializeField] private int maxHp;
    private int currentHp;
    [SerializeField] private float iFrames;
    private float iFramesTimer = 0.0f;
    [SerializeField] public bool canGetHurt;
    [SerializeField] private GameObject spikePrefab;
    [SerializeField] private float attackRate;
    private float attackTimer;
    private Animator animator;

    private void Start()
    {
        currentHp = maxHp;
        animator = GetComponent<Animator>();
    }

    private void Update()
    {
        iFramesTimer -= Time.deltaTime;
        attackTimer -= Time.deltaTime;

        if (attackTimer <= 0.0f)
        {
            animator.SetTrigger("Attack");
            attackTimer = attackRate;
        }
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (!canGetHurt)
            return;

        if (collision.CompareTag("PlayerAttack"))
        {
            if (iFramesTimer <= 0.0f)
                Hit();
        }
    }

    private void Hit()
    {
        currentHp--;
        if (currentHp <= 0)
        {
            Die();
            return;
        }
        iFramesTimer = iFrames;
        GetComponent<Animator>().SetTrigger("Hit");
    }

    private void Die()
    {
        transform.GetComponentInParent<TotemController>().PartDeath(transform);
        GetComponent<BoxCollider2D>().enabled = false;
        GetComponent<Animator>().SetTrigger("Death");
    }

    private void EndDeath()
    {
        Destroy(gameObject);
    }

    private void Shoot()
    {
        GameObject spike = Instantiate(spikePrefab, new Vector3(transform.position.x - 1.25f * transform.localScale.x, transform.position.y - 0.6f, transform.position.z), transform.rotation);
        spike.transform.localScale = transform.localScale;
    }
}
