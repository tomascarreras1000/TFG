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
                for (int i = 0; i < bosses.Length; i++)
                {
                    if (bosses[i].TryGetComponent<GolemManager>(out GolemManager golem))
                    {
                        golem.Spawn();
                        Destroy(gameObject);
                    }
                }
            }
            else if (GetComponent<BossTriggerTag>().bossToTrigger == 2)
            {
                for (int i = 0; i < bosses.Length; i++)
                {
                    if (bosses[i].TryGetComponent<MinotaurManager>(out MinotaurManager minotaur))
                    {
                        minotaur.Spawn();
                        Destroy(gameObject);
                    }
                }
            }
        }
    }
}
