using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using UnityEngine.UI;
using TMPro;

public class SpawnMenu : MonoBehaviour
{
    // Spawn Panel Properties
    [SerializeField] private Transform spawnCenter;
    private List<GameObject> spawnPositionObjects;
    [SerializeField] private Transform spawnGroup;
    [SerializeField] private GameObject[] spawnPrefabs;

    // Spawn Panel UI Components
    [SerializeField] private Toggle togglePanel;
    [SerializeField] private LoadingSmall loadingSmall;
    [SerializeField] private GameObject spawnPanel;

    // Spawn Panel Data Components
    [SerializeField] private FilePath optionFilePath;
    [SerializeField] private TextToggle optionGravity;
    [SerializeField] private TextDropdown optionObjectType;
    [SerializeField] private TextSlider optionObjectSize;
    [SerializeField] private TextInputField optionVisibleNumber;
    [SerializeField] private Button spawnButton;

    // Auxiliary Task Class
    [SerializeField] private GameObject renderManager;

    // Spawn Panel Properties
    public List<string> retrievedTextures { get; private set; }

    // Coroutine status
    private bool spawnObjectRunning;

    public void StartSpawnOperation()
    {
        // Spawn operation has to end before initiating another one (CHANGE: Show error message in the panel)
        if (!spawnObjectRunning)
        {
            StartCoroutine(SpawnInitialObject());
        }
        else
        {
            Debug.Log("There is an object spawning operation at the moment, please wait");
        }
    }

    // Spawns initial files in front, and enables controls to cycle through the rest of files
    private IEnumerator SpawnInitialObject()
    {
        spawnObjectRunning = true;
        loadingSmall.StartLoading();

        // Initial files path
        List<string> imagePaths = optionFilePath.filePaths;
        List<string> visiblePaths = new List<string>();
        // CHANGE: Make the search double the size, so the next batch is ready when you switch to the next
        int index = 0;
        while (index < optionVisibleNumber.GetDataInt())
        {
            visiblePaths.Add(CircularList.GetElement<string>(imagePaths, index));
            index++;
        }

        // Generate positions to make them appear

        yield return StartCoroutine(GenerateObjectPlacement(optionVisibleNumber.GetDataInt()));

        // Make them appear in the scene

        RenderManager render = Instantiate(renderManager).GetComponent<RenderManager>();
        yield return StartCoroutine(render.PlaceMultimedia(visiblePaths,
                                                           spawnPrefabs[optionObjectType.GetData()],
                                                           false, false, false,
                                                           spawnPositionObjects,
                                                           optionGravity.GetData(),
                                                           optionObjectSize.GetData()));



        loadingSmall.DoneLoading();
        spawnObjectRunning = false;
    }

    public IEnumerator GenerateObjectPlacement(int numberOfPlacements) //CHANGE: Take an option and split this function for each placement mode
    {
        spawnPositionObjects = new List<GameObject>();

        loadingSmall.UpdateText(0, numberOfPlacements, "Positioning");
        for (int i = 0; i < numberOfPlacements; i++)
        {
            GameObject positionObject = new GameObject(); 
            Vector3 placementPosition = spawnCenter.position + Vector3.right * i;

            positionObject.transform.position = placementPosition;
            spawnPositionObjects.Add(positionObject);
            loadingSmall.UpdateText(i + 1, numberOfPlacements, "Positioning");
            yield return null;
        }
    }

}
