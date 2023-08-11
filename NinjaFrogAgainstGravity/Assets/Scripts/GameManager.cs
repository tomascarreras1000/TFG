using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GameManager : MonoBehaviour
{
    [SerializeField] private Transform spawnPoint;
    [SerializeField] private GameObject playerPrefab;
    [HideInInspector] public GameObject player;

    [SerializeField] private GameObject pinapplePrefab;
    [HideInInspector] public List<GameObject> collectables;

    public List<GameObject> bossDoors;

    public static event Action OnRestart;

    public void Start()
    {
        //Restart();
    }
    public void Restart()
    {
        CleanseScene();

        if (!player)
            player = Instantiate(playerPrefab, spawnPoint.position, Quaternion.identity);

        //GameObject collectablesParent = GameObject.Find("Collectables");
        //Transform[] allCollectables = collectablesParent.GetComponentsInChildren<Transform>();
        //for (int i = 0; i < allCollectables.Length; i++)
        //{
        //    if (allCollectables[i] != collectablesParent.transform)
        //        collectables.Add(Instantiate(pinapplePrefab, allCollectables[i].position, Quaternion.identity));
        //}

        OnRestart();
    }

    private void CleanseScene()
    {
        //if (player != null)
        //    Destroy(player);
        for (int i = 0; i < collectables.Count; i++)
        {
            Destroy(collectables[i]);
        }
    }

    public void OnKeyPickup(int keyId)
    {
        if (keyId == 1)
        {
            bossDoors[keyId - 1].SetActive(false);
        }
        else if (keyId == 2)
        {
            bossDoors[keyId - 1].SetActive(false);
        }
        else
        {
            Debug.Log("Invalid Key Id!");
        }
    }
}
