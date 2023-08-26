using UnityEngine;

public class BossTrigger : MonoBehaviour
{
    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("Player"))
        {
            BossTag[] bosses = FindObjectsOfType<BossTag>();

            if (GetComponent<BossTriggerTag>().bossToTrigger == 1)
            {
                bosses[0].GetComponent<GolemManager>().Spawn();
            }
        }
    }
}
