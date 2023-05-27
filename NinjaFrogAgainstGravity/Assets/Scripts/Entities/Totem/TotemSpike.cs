using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TotemSpike : MonoBehaviour
{
    [SerializeField] private float speed;
    private BoxCollider2D collider;
    [SerializeField] private LayerMask groundLayer;
    private Vector2 direction = Vector2.right;

    private void Start()
    {
        collider = GetComponent<BoxCollider2D>();
        direction = transform.right * transform.localScale.x;
    }

    void Update()
    {
        if (Physics2D.BoxCast(collider.bounds.center, collider.bounds.size, 0.0f, direction, 0.1f, groundLayer)) // Check
            Destroy(gameObject);

        transform.position = new Vector3(transform.position.x - direction.x * speed * Time.deltaTime, transform.position.y - direction.y * speed * Time.deltaTime, transform.position.z);
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("Player") || collision.CompareTag("PlayerAttack"))
        {
            Destroy(gameObject);
        }
    }
}
