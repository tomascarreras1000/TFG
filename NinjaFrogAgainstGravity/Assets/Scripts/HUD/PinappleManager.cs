using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class PinappleManager : MonoBehaviour
{
    private TextMeshProUGUI textMesh;

    private void Awake()
    {
        textMesh = GetComponent<TextMeshProUGUI>();
    }
    private void OnEnable()
    {
        PlayerMovement.OnPickUp += UpdateHealth;
    }
    private void OnDisable()
    {
        PlayerMovement.OnPickUp -= UpdateHealth;
    }
    private void UpdateHealth(PlayerMovement.Collectables collectable, int count)
    {
        if (collectable == PlayerMovement.Collectables.PINAPPLE)
            textMesh.text = count.ToString();
    }
}
