using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EventDebugger : MonoBehaviour
{
    private void OnEnable()
    {
        PlayerMovement.OnPlayerDamaged += OnPlayerDamaged;
        PlayerMovement.OnPickUp += OnPickUp;
        GameManager.OnRestart += OnRestart;
    }
    private void OnDisable()
    {
        PlayerMovement.OnPlayerDamaged -= OnPlayerDamaged;
        PlayerMovement.OnPickUp -= OnPickUp;
        GameManager.OnRestart -= OnRestart;
    }

    private void OnPlayerDamaged(int hp)
    {
        Debug.Log("Damage taken, current hp: " + hp);
    }
    private void OnPickUp(PlayerMovement.Collectables collectable, int i)
    {
        Debug.Log(collectable.ToString() + " picked up, now holding: " + i);
    }
    private void OnRestart()
    {
        Debug.Log("Restarting...");
    }
 

}
