using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class HealthManager : MonoBehaviour
{
    private TextMeshProUGUI textMesh;

    private void Awake()
    {
        textMesh = GetComponent<TextMeshProUGUI>();
    }
    private void OnEnable()
    {
        PlayerMovement.OnHealthChange += UpdateHealth;
    }
    private void OnDisable()
    {
        PlayerMovement.OnHealthChange -= UpdateHealth;
    }
    private void UpdateHealth(int health)
    {
        textMesh.text = health.ToString();
    }
}
