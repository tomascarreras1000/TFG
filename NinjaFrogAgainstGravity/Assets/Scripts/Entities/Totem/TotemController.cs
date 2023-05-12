using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TotemController : MonoBehaviour
{
    private Transform[] totemParts;

    private void Awake()
    {
        totemParts = GetComponentsInChildren<Transform>(); // 0 will be transform
    }

    public void PartDeath(Transform part)
    {
        if (part == totemParts[1])
            totemParts[2].GetComponent<TotemPart>().canGetHurt = true;
        else if (part == totemParts[2])
            totemParts[3].GetComponent<TotemPart>().canGetHurt = true;
    }
}
