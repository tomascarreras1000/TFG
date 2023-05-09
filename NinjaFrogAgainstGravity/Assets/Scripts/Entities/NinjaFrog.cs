using System.Collections;
using System.Collections.Generic;
using System.Windows;
using UnityEngine;

public class NinjaFrog : MonoBehaviour
{
    private PlayerMovement playerMovement;

    private void OnEnable()
    {
        PlayerMovement.AbilityCast += ActivateFrogAbility;
    }

    private void Start()
    {
        playerMovement = GetComponent<PlayerMovement>();
    }

    public void ActivateFrogAbility()
    {
        Debug.Log("Ajaj");
    }

    private void OnDisable()
    {
        PlayerMovement.AbilityCast -= ActivateFrogAbility;
    }
}
