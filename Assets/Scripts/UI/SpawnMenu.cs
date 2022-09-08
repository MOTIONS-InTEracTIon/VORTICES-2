using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using UnityEngine.UI;
using SimpleFileBrowser;
using TMPro;
using System;

public class SpawnMenu : MonoBehaviour
{
    // Spawn Panel Properties
    [SerializeField] private Transform spawnPosition;
    [SerializeField] private Transform spawnGroup;
    [SerializeField] private GameObject[] spawnPrefabs;

    // Spawn Panel UI Components
    [SerializeField] private Toggle togglePanel;
    [SerializeField] private LoadingSmall loadingSmall;
    [SerializeField] private GameObject spawnPanel;

    // Spawn Panel Interactable UI Components
    [SerializeField] private TextToggle optionGravity; 
    [SerializeField] private TextDropdown optionObjectType;
    [SerializeField] private TextSlider optionObjectSize;
    [SerializeField] private TextInputField optionMaterialPath;
    [SerializeField] private Button spawnButton;

    // Auxiliary Task Class
    [SerializeField] private GameObject fileLoadManager; 
    private List<Texture> retrievedTexture;

    // Coroutine status
    private bool spawnObjectRunning;

    private void Start()
    {
        FileBrowser.SingleClickMode = true;
    }

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

    public void OpenFileBrowser()
    {
        //CHANGE: Put success data into a variable, not just set it to text
        FileBrowser.ShowLoadDialog((paths) => { optionMaterialPath.SetText(paths[0]); }, () => { Debug.Log("Canceled"); }, FileBrowser.PickMode.Files, false, Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), null, "Load", "Select");
        
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

        // Material retrieving is then launched, when it ends the configurations are applied including making the object visible
        // Material path has to point correctly, otherwise show console error (CHANGE: Show error message in the panel)
        string materialPath = optionMaterialPath.GetData();
        if (System.IO.File.Exists(materialPath)) // (CHANGE: && its extension is compatible with application)
        {
            retrievedTexture = null;
            StartCoroutine(WaitForTexture(materialPath));
            while(retrievedTexture == null)
            {
                yield return null;
            }
            // (CHANGE: Foreach texture, a prefab is instantiated, batch load)
            // Spawning starts from instantiating the prefab
            GameObject spawnPrefab = spawnPrefabs[optionObjectType.GetData()];
            GameObject spawnObject = Instantiate(spawnPrefab, spawnPosition.position, spawnPrefab.transform.rotation, spawnGroup);

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
            spawnObject.GetComponent<Renderer>().material.mainTexture = retrievedTexture[0];

            loadingSmall.DoneLoading();
        }
        else
        {
            Debug.Log("File does not exist");
            //Debug.Log("File extension not supported, please try: .x/.x/.x/.x")
        }

        spawnObjectRunning = false;
        
        yield return null;
    }

    private IEnumerator WaitForTexture(string materialPath)
    {
        loadingSmall.StartLoading();

        LoadLocalManager loadManager = Instantiate(fileLoadManager).GetComponent<LoadLocalManager>();
        StartCoroutine(loadManager.RetrieveTexture(materialPath));
        while (!loadManager.texturesRetrieved)
        {
            yield return null;
        }
        retrievedTexture = loadManager.retrievedTexture;
        Destroy(loadManager.gameObject);

        yield return null;
    }

}
