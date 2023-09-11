using UnityEngine;

public class BossTrigger : MonoBehaviour
{
    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("Player"))
        {
            FindObjectOfType<GameManager>().OnBossTrigger(GetComponent<BossTriggerTag>().bossToTrigger);
            Destroy(gameObject);
        }
    }
}
