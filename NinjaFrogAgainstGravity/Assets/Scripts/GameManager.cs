using System;
using System.Collections.Generic;
using Cinemachine;
using UnityEngine;

public class GameManager : MonoBehaviour
{
    [SerializeField] private Transform spawnPoint;
    [SerializeField] private GameObject playerPrefab;
    [HideInInspector] public GameObject player;

    [SerializeField] private GameObject pinapplePrefab;
    [HideInInspector] public List<GameObject> collectables;

    public List<GameObject> bossDoors;
    public List<CinemachineVirtualCamera> cameras;

    private int keyPickedUp = 0;
    private int bossToTrigger = 0;

    [SerializeField] private CinemachineVirtualCamera mainCamera;
    [SerializeField] private float transitionDuration;
    private float transitionTimer;

    public static event Action OnRestart;

    public void Start()
    {
        //Restart();
        for (int i = 0; i < cameras.Count; i++)
        {
            cameras[i].enabled = false;
        }
    }

    private void Update()
    {
        if (!mainCamera.enabled) 
        {
            transitionTimer += Time.deltaTime;

            if (keyPickedUp != 0)
            {
                if (transitionTimer >= transitionDuration)
                {
                    cameras[keyPickedUp - 1].enabled = false;
                    mainCamera.enabled = true;
                    keyPickedUp = 0;
                }
                else if (transitionTimer >= transitionDuration * 0.5f)
                {
                    bossDoors[keyPickedUp - 1].SetActive(false);
                }
            }
            else if (bossToTrigger != 0)
            {
                if (transitionTimer >= transitionDuration)
                {
                    cameras[bossToTrigger + 1].enabled = false;
                    mainCamera.enabled = true;
                    bossToTrigger = 0;
                }
            }
        }
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

    public void OnKeyPickup(int bossId)
    {
        if (bossId == 1 || bossId == 2)
        {
            keyPickedUp = bossId;
            cameras[bossId - 1].enabled = true;
            mainCamera.enabled = false;
            transitionTimer = 0f;
        }
        else
        {
            Debug.Log("Invalid Key Id!");
        }
    }

    public void OnBossTrigger(int bossId)
    {
        if (bossId == 1 || bossId == 2)
        {
            bossToTrigger = bossId;
            cameras[bossId + 1].enabled = true;
            mainCamera.enabled = false;
            transitionTimer = 0f;

            TriggerBoss(bossToTrigger);
        }
        else
        {
            Debug.Log("Invalid Key Id!");
        }
    }
    
    private void TriggerBoss(int bossId)
    {
        BossTag[] bosses = FindObjectsOfType<BossTag>();

        if (bossId == 1)
        {
            for (int i = 0; i < bosses.Length; i++)
            {
                if (bosses[i].TryGetComponent<GolemManager>(out GolemManager golem))
                {
                    golem.Spawn();
                }
            }
        }
        else if (bossId == 2)
        {
            for (int i = 0; i < bosses.Length; i++)
            {
                if (bosses[i].TryGetComponent<MinotaurManager>(out MinotaurManager minotaur))
                {
                    minotaur.Spawn();
                }
            }
        }
    }

    public void OnBossDeath(int bossId)
    {
        if (bossId == 1 || bossId == 2)
        {
            bossDoors[bossId + 1].SetActive(false);
        }
        else
        {
            Debug.Log("Invalid Key Id!");
        }
    }
}
