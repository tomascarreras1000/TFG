using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TotemSpike : MonoBehaviour
{
    [SerializeField] private float speed;
    private BoxCollider2D collider;
    [SerializeField] private LayerMask groundLayer;

    private void Start()
    {
        collider = GetComponent<BoxCollider2D>();
    }

    void Update()
    {
        if (Physics2D.BoxCast(collider.bounds.center, collider.bounds.size, 0.0f, new Vector2(transform.localScale.x, 0), 0.1f, groundLayer))
            Destroy(gameObject);

        transform.position = new Vector3(transform.position.x - transform.localScale.x * speed * Time.deltaTime, transform.position.y, transform.position.z);
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("Player") || collision.CompareTag("PlayerAttack"))
        {
            Destroy(gameObject);
        }
    }
}
