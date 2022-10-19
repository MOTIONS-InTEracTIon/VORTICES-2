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
    [SerializeField] private LoadingSmall loadingSmall;
    [SerializeField] private GameObject spawnPanel;
    [SerializeField] private GameObject controls;

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

    public int globalIndex;
    public bool lastLoadForward;
    private List<string> selectionPaths;

    // Coroutine status
    private bool spawnObjectRunning;

    public void StartSpawnOperation()
    {
        // Spawn operation has to end before initiating another one (CHANGE: Show error message in the panel)
        if (!spawnObjectRunning)
        {
            // Startup
            globalIndex = -1;
            lastLoadForward = true;
            selectionPaths = new List<string>();
            // Execution
            StartCoroutine(ObjectSpawn(true));
            // Finishing
            controls.GetComponent<FadeUI>().FadeIn();
        }
        else
        {
            Debug.Log("There is an object spawning operation at the moment, please wait");
        }
    }

    public void SpawnForwards()
    {
        // Spawn operation has to end before initiating another one (CHANGE: Show error message in the panel)
        if (!spawnObjectRunning)
        {
            // Startup
            selectionPaths = new List<string>();
            // Execution
            StartCoroutine(ObjectSpawn(true));
        }
        else
        {
            Debug.Log("There is an object spawning operation at the moment, please wait");
        }
    }

    public void SpawnBackwards()
    {
        // Spawn operation has to end before initiating another one (CHANGE: Show error message in the panel)
        if (!spawnObjectRunning)
        {
            // Startup
            selectionPaths = new List<string>();
            // Execution
            StartCoroutine(ObjectSpawn(false));
        }
        else
        {
            Debug.Log("There is an object spawning operation at the moment, please wait");
        }
    }

    // Spawns initial files in front, and enables controls to cycle through the rest of files
    private IEnumerator ObjectSpawn(bool forwards)
    {
        spawnObjectRunning = true;
        loadingSmall.StartLoading();

        // Get rid of old objects
        // CHANGE: Could reuse this instead of destroying all
        foreach (Transform child in spawnGroup)
        {
            Destroy(child.gameObject);
        }

        // Generate selection path to get via render
        yield return StartCoroutine(GenerateSelectionPaths(forwards));

        // Generate positions to make them appear
        yield return StartCoroutine(GenerateObjectPlacement(optionVisibleNumber.GetDataInt()));

        // Make them appear in the scene
        RenderManager render = Instantiate(renderManager).GetComponent<RenderManager>();
        yield return StartCoroutine(render.PlaceMultimedia(selectionPaths,
                                                           spawnPrefabs[optionObjectType.GetData()],
                                                           false, false, false,
                                                           spawnPositionObjects,
                                                           optionGravity.GetData(),
                                                           optionObjectSize.GetData()));
        Destroy(render.gameObject);

        loadingSmall.DoneLoading();
        spawnObjectRunning = false;
    }

    private IEnumerator GenerateSelectionPaths(bool forwards)
    {
        // CHANGE: Make the search double the size, so the next batch is ready when you switch to the next
        int index = 0;

        if (forwards)
        {
            if (!lastLoadForward)
            {
                globalIndex += optionVisibleNumber.GetDataInt() - 1;
            }
            lastLoadForward = true;
        }
        else
        {
            if (lastLoadForward)
            {
                globalIndex -= optionVisibleNumber.GetDataInt() - 1;
            }
            lastLoadForward = false;
        }

        while (index < optionVisibleNumber.GetDataInt())
        {
            if(forwards)
            {
                globalIndex++;
                selectionPaths.Add(CircularList.GetElement<string>(optionFilePath.filePaths, globalIndex));
            }
            else
            {
                globalIndex--;
                selectionPaths.Add(CircularList.GetElement<string>(optionFilePath.filePaths, globalIndex));
            }
            index++;
            yield return null;  
        }
    }

    public IEnumerator GenerateObjectPlacement(int numberOfPlacements) //CHANGE: Take an option and split this function for each placement mode
    {
        spawnPositionObjects = new List<GameObject>();

        loadingSmall.UpdateText(0, numberOfPlacements, "Positioning");
        for (int i = 0; i < numberOfPlacements; i++)
        {
            GameObject positionObject = new GameObject();
            positionObject.transform.parent = spawnGroup;
            Vector3 placementPosition = spawnCenter.position + Vector3.left * (numberOfPlacements / 2) + Vector3.right * i;

            positionObject.transform.position = placementPosition;
            spawnPositionObjects.Add(positionObject);
            loadingSmall.UpdateText(i + 1, numberOfPlacements, "Positioning");
            yield return null;
        }
    }

    //private IEnumerator Get

}
