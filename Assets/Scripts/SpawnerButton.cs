using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SpawnerButton : MonoBehaviour
{
    [SerializeField] GameObject spawnPrefab; 

    [SerializeField] MenuCanvas menuCanvas;
    [SerializeField] Transform spawnPosition;
    [SerializeField] Transform parentGroup;
    public void SpawnObject()
    {
        menuCanvas.ToggleSpawnPanel();
        Instantiate(spawnPrefab, spawnPosition.position, spawnPosition.rotation, parentGroup);
    }
}
