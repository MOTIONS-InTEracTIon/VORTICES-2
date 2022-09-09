using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using UnityEngine.UI;
using TMPro;

public class SpawnMenu : MonoBehaviour
{
    // Spawn Panel Properties
    [SerializeField] private Transform spawnCenter;
    private List<Vector3> spawnPositions;
    [SerializeField] private Transform spawnGroup;
    [SerializeField] private GameObject[] spawnPrefabs;

    // Spawn Panel UI Components
    [SerializeField] private Toggle togglePanel;
    [SerializeField] private LoadingSmall loadingSmall;
    [SerializeField] private GameObject spawnPanel;

    // Spawn Panel Interactable UI Components
    [SerializeField] private TextScrollView optionTexturePath;
    [SerializeField] private TextToggle optionGravity;
    [SerializeField] private TextDropdown optionObjectType;
    [SerializeField] private TextSlider optionObjectSize;
    [SerializeField] private Button spawnButton;

    // Auxiliary Task Class
    [SerializeField] private GameObject fileLoadManager;

    // Coroutine status
    private bool spawnObjectRunning;

    public void ToggleSpawnPanel()
    {
        if (togglePanel.isOn)
        {
            spawnPanel.SetActive(true);
        }
        else
        {
            spawnPanel.SetActive(false);
        }

    }

    public void StartSpawnOperation()
    {
        // Spawn operation has to end before initiating another one (CHANGE: Show error message in the panel)
        if (!spawnObjectRunning)
        {
            StartCoroutine("SpawnObject");
        }
        else
        {
            Debug.Log("There is an object spawning operation at the moment, please wait");
        }
    }

    public IEnumerator SpawnObject()
    {
        spawnObjectRunning = true;
        loadingSmall.StartLoading();

        // Texture file searching is made
        List<string> texturePaths = optionTexturePath.scrollViewPaths;

        // Try retrieving texture with scrollview paths
        List<Texture> retrievedTextures = null;
        LoadLocalManager loadManager = Instantiate(fileLoadManager).GetComponent<LoadLocalManager>();
        yield return StartCoroutine(loadManager.RetrieveTexture(texturePaths));
        if (loadManager.result == Result.Success)
        {
            Destroy(loadManager.gameObject);

            retrievedTextures = loadManager.retrievedTextures;
            // Generate list of positions CHANGE: For now, just add + 1 to x value, change this when implementing spatial placement
            yield return StartCoroutine(GenerateObjectPlacement(retrievedTextures.Count));
            // For each file create an object
            for (int i = 0; i < retrievedTextures.Count; i++)
            {
                // Spawning starts from instantiating the prefab
                GameObject spawnPrefab = spawnPrefabs[optionObjectType.GetData()];
                GameObject spawnObject = Instantiate(spawnPrefab, spawnPositions[i], spawnPrefab.transform.rotation, spawnGroup);

                // Get the data from menu components
                bool hasGravity = optionGravity.GetData();
                float sizeMultiplier = optionObjectSize.GetData();

                // Apply data
                // Apply gravity toggle
                if (!hasGravity)
                {
                    spawnObject.GetComponent<Rigidbody>().isKinematic = true;
                }
                // Apply size slider
                spawnObject.transform.localScale *= sizeMultiplier;
                // Apply material file
                spawnObject.GetComponent<Renderer>().material.mainTexture = retrievedTextures[i];
            }
        }
        else
        {
            Destroy(loadManager.gameObject);

            if (loadManager.result == Result.WebRequestError)
            {
                //CHANGE:TO SHOW IN UI
                Debug.Log("Could not retrieve texture (Webrequest error)");
            }
            else if (loadManager.result == Result.TypeError)
            {
                //CHANGE:TO SHOW IN UI
                // Supported extensions
                Debug.Log("One or more files have unsupported extension, please try: .jpg, .png");
            }
        }

        loadingSmall.DoneLoading();
        spawnObjectRunning = false;
    }

    public IEnumerator GenerateObjectPlacement(int numberOfPlacements) //CHANGE: Take an option and split this function for each placement mode
    {
        spawnPositions = new List<Vector3>();
        for (int i = 0; i < numberOfPlacements; i++)
        {
            Vector3 placementPosition = spawnCenter.position + Vector3.right * i;
            spawnPositions.Add(placementPosition);
            yield return null;
        }
    }

}
