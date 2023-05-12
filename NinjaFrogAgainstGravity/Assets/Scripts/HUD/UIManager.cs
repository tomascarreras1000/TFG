using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class UIManager : MonoBehaviour
{
    private TextMeshProUGUI pinappleText;
    private TextMeshProUGUI healthText;

    private void Awake()
    {
        TextMeshProUGUI[] texts = GetComponentsInChildren<TextMeshProUGUI>();
        for (int i = 0;i<texts.Length;i++)
        {
            if (texts[i].name.Contains("Pinapple"))
                pinappleText = texts[i];
            else if (texts[i].name.Contains("Health"))
                healthText = texts[i];
        }
    }
    private void OnEnable()
    {
        PlayerMovement.OnPickUp += UpdatePinapples;
        PlayerMovement.OnHealthChange += OnHealthChange;
    }
    private void OnDisable()
    {
        PlayerMovement.OnPickUp -= UpdatePinapples;
        PlayerMovement.OnHealthChange -= OnHealthChange;

    }
    private void UpdatePinapples(int count)
    {
        pinappleText.text = count.ToString();
    }

    private void OnHealthChange(int count)
    {
        healthText.text = count.ToString();
    }
}
