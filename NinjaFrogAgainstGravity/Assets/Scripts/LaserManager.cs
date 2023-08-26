using UnityEngine;

public class LaserManager : MonoBehaviour
{
    private void ActivateLaser()
    {
        BossTag[] bosses = FindObjectsOfType<BossTag>();
        for (int i = 0; i < bosses.Length; i++)
        {
            if (bosses[i].bossNumber == 1)
            {
                bosses[i].GetComponent<GolemManager>().ActivateLaser();
            }
        }
    }
}
