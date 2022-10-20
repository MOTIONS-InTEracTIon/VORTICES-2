using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using UnityEngine.UI;
using TMPro;
using System.IO;

public class CircularPanel : MonoBehaviour
{
    #region Variables and properties
    // Circular Panel UI Components
    [SerializeField] private List<GameObject> uiComponents;

    // Circular Panel Data Components
    [SerializeField] private FilePath optionFilePath;

    // Circular Panel Properties
    // (Input)
    public int actualComponentId { get; set; }
    public int mode { get; set; }
    public bool volumetric { get; set; }
    public Vector3Int dimension;

    // (Display)
    [SerializeField] List<GameObject> placementBasePrefabs;
    private GameObject placementBase;
    [SerializeField] GameObject elementPrefab;

    [SerializeField] private Transform spawnCenter;
    private List<GameObject> spawnPositionObjects;
    [SerializeField] private Transform spawnGroup;
    public int globalIndex;
    public bool lastLoadForward;
    private List<string> selectionPaths;

    // Auxiliary Task Class
    [SerializeField] private GameObject renderManager;

    #endregion


    #region User Input
    // This set of functions are used so the system can take user input to display multimedia
    // Changes single component
    public void ChangeVisibleComponent(int componentId)
    {
        StartCoroutine(ChangeComponent(componentId));
    }
    private IEnumerator ChangeComponent(int componentId)
    {
        // FadeOut actual component
        FadeUI actualComponentFader = uiComponents[actualComponentId].GetComponent<FadeUI>();
        yield return StartCoroutine(actualComponentFader.FadeOut());
        // Disable actual component
            uiComponents[actualComponentId].SetActive(false);
        // Enable new component
        uiComponents[componentId].SetActive(true);
        // Block button if necessary
        if(componentId != 9)
        {
            blockButton(componentId);
        }
        // FadeIn new component
        FadeUI newComponentFader = uiComponents[componentId].GetComponent<FadeUI>();
        yield return StartCoroutine(newComponentFader.FadeIn());
        actualComponentId = componentId;
    }
    public void AddBrowserToComponents()
    {
        uiComponents[9] = GameObject.Find("SimpleFileBrowserCanvas(Clone)");
    }

    // Handles block next button rules per component
    public void blockButton(int componentId)
    {
        bool hasToBlock = true;
        switch (componentId)
        {
            // Description has no block
            case 0:
                hasToBlock = false;
                break;
            // Mode has to be selected
            case 1:
                Toggle planeToggle = uiComponents[componentId].transform.Find("Content/Horizontal Group/Plane Toggle").GetComponentInChildren<Toggle>();
                Toggle radialToggle = uiComponents[componentId].transform.Find("Content/Horizontal Group/Radial Toggle").GetComponentInChildren<Toggle>();
                if (!planeToggle.interactable || !radialToggle.interactable)
                {
                    hasToBlock = false;
                }
                break;
            // Height, Width and layers have to be bigger than 0
            case 2: case 3: case 4: case 5: case 6: case 7:
                string input = uiComponents[componentId].GetComponentInChildren<TMP_InputField>().text;
                int value = 0;
                try
                {
                    value = int.Parse(input);
                }
                catch {}
                if(value > 0)
                {
                    hasToBlock = false;
                }
                break;
            case 8:
                FilePath filePath = uiComponents[componentId].GetComponentInChildren<FilePath>();
                if(filePath.filePaths != null && filePath.filePaths.Count > 0)
                {
                    hasToBlock = false;
                }
                break;
        }

        Button nextButton = uiComponents[componentId].transform.Find("Footer").transform.GetComponentInChildren<Button>();
        if (hasToBlock)
        {
            nextButton.interactable = false;
        }
        else
        {
            nextButton.interactable = true;
        }
    }

    // Changes component based on option setting (width, height, layers) and mode
    public void ChangeComponentMode(int option)
    {
        // Plane mode
        if(mode == 0)
        {
            switch (option)
            {
                case 0:
                    ChangeVisibleComponent(2);
                    break;
                case 1:
                    ChangeVisibleComponent(3);
                    break;
                case 2:
                    if (volumetric)
                    {
                        ChangeVisibleComponent(4);
                    }
                    else
                    {
                        ChangeVisibleComponent(8);
                    }
                    break;
                default:
                    break;
            }
        }
        // Radial mode
        else if (mode == 1)
        {
            switch (option)
            {
                case 0:
                    ChangeVisibleComponent(5);
                    break;
                case 1:
                    ChangeVisibleComponent(6);
                    break;
                case 2:
                    if(volumetric)
                    {
                        ChangeVisibleComponent(7);
                    }
                    else
                    {
                        ChangeVisibleComponent(8);
                    }
                    break;
                default:
                    break;
            }
        }
    }

    // Toggles volumetric bool
    public void VolumetricToggle()
    {
        if(volumetric)
        {
            volumetric = false;
        }
        else
        {
            volumetric = true;
        }
        Debug.Log(volumetric);
    }

    // Sets vector dimensions for multimedia display (Representing X as quantity or width, Y as height and Z as layers)
    public void SetDimension(int option)
    {
        // Plane mode
        if (mode == 0)
        {
            switch (option)
            {
                case 0:
                    TMP_InputField planeXInput = uiComponents[2].GetComponentInChildren<TMP_InputField>();
                    dimension.x = int.Parse(planeXInput.text);
                    break;
                case 1:
                    TMP_InputField planeYInput = uiComponents[3].GetComponentInChildren<TMP_InputField>();
                    dimension.y = int.Parse(planeYInput.text);
                    break;
                case 2:
                    TMP_InputField planeZInput = uiComponents[4].GetComponentInChildren<TMP_InputField>();
                    dimension.z = int.Parse(planeZInput.text);
                    break;
                default:
                    break;
            }
        }
        // Radial mode
        else if (mode == 1)
        {
            switch (option)
            {
                case 0:
                    TMP_InputField radialXInput = uiComponents[5].GetComponentInChildren<TMP_InputField>();
                    dimension.x = int.Parse(radialXInput.text);
                    break;
                case 1:
                    TMP_InputField radialYInput = uiComponents[6].GetComponentInChildren<TMP_InputField>();
                    dimension.y = int.Parse(radialYInput.text);
                    break;
                case 2:
                    TMP_InputField radialZInput = uiComponents[7].GetComponentInChildren<TMP_InputField>();
                    dimension.z = int.Parse(radialZInput.text);
                    break;
                default:
                    break;
            }
        }
    }
    #endregion

    #region Display Multimedia
    public void StartSpawnOperation()
    {
        // Startup
        globalIndex = -1;
        lastLoadForward = true;
        selectionPaths = new List<string>();
        // Execution
        // First time has to fill every slot so it uses width * height * layer
        int startingLoad = dimension.x * dimension.y;
        if (dimension.z != 0)
        {
            startingLoad *= dimension.z;
        }
        StartCoroutine(ObjectSpawn(startingLoad, true));
    }

    public void SpawnForwards(int loadNumber)
    {
        // Spawn operation has to end before initiating another one (CHANGE: Show error message in the panel)
        // Startup
        selectionPaths = new List<string>();
        // Execution
        StartCoroutine(ObjectSpawn(loadNumber, true));

    }

    public void SpawnBackwards(int loadNumber)
    {
        // Spawn operation has to end before initiating another one (CHANGE: Show error message in the panel)
        // Startup
        selectionPaths = new List<string>();
        // Execution
        StartCoroutine(ObjectSpawn(loadNumber, false));
    }

    // Spawns initial files in front, and enables controls to cycle through the rest of files
    private IEnumerator ObjectSpawn(int loadNumber, bool forwards)
    {
        // Get rid of old objects
        // CHANGE: Could reuse this instead of destroying all
        foreach (Transform child in spawnGroup)
        {
            Destroy(child.gameObject);
        }

        // Generate selection path to get via render
        yield return StartCoroutine(GenerateSelectionPaths(loadNumber, forwards));

        // Generate positions to make them appear
        yield return StartCoroutine(GenerateObjectPlacement(loadNumber));

        // Make them appear in the scene
        RenderManager render = Instantiate(renderManager).GetComponent<RenderManager>();
        yield return StartCoroutine(render.PlaceMultimedia(selectionPaths,
                                                           elementPrefab,
                                                           false, false, false,
                                                           spawnPositionObjects));
        Destroy(render.gameObject);

    }

    private IEnumerator GenerateSelectionPaths(int loadNumber, bool forwards)
    {
        // CHANGE: Make the search double the size, so the next batch is ready when you switch to the next
        int index = 0;

        if (forwards)
        {
            if (!lastLoadForward)
            {
                globalIndex += loadNumber - 1;
            }
            lastLoadForward = true;
        }
        else
        {
            if (lastLoadForward)
            {
                globalIndex -= loadNumber - 1;
            }
            lastLoadForward = false;
        }

        while (index < loadNumber)
        {
            if (forwards)
            {
                globalIndex++;
                bool search = true;
                int attempts = 200;
                int attempt = 0;
                string actualPath;
                string pathExtension;
                while(search && attempt < attempts)
                {
                    actualPath = CircularList.GetElement<string>(optionFilePath.filePaths, globalIndex);
                    pathExtension = Path.GetExtension(actualPath);
                    if (pathExtension == ".png" ||
                        pathExtension == ".jpg" ||
                        pathExtension == ".jpge")
                    {
                        selectionPaths.Add(CircularList.GetElement<string>(optionFilePath.filePaths, globalIndex));
                        search = false;
                    }
                    else
                    {
                        globalIndex++;
                        attempt++;
                    }
                }
            }
            else
            {
                globalIndex--;
                bool search = true;
                int attempts = 200;
                int attempt = 0;
                string actualPath;
                string pathExtension;
                while (search && attempt < attempts)
                {
                    actualPath = CircularList.GetElement<string>(optionFilePath.filePaths, globalIndex);
                    pathExtension = Path.GetExtension(actualPath);
                    if (pathExtension == ".png" ||
                        pathExtension == ".jpg" ||
                        pathExtension == ".jpge")
                    {
                        selectionPaths.Add(CircularList.GetElement<string>(optionFilePath.filePaths, globalIndex));
                        search = false;
                    }
                    else
                    {
                        globalIndex--;
                        attempt++;
                    }
                }
            }
            index++;
            yield return null;
        }
    }

    public IEnumerator GenerateObjectPlacement(int numberOfPlacements) //CHANGE: Take an option and split this function for each placement mode
    {
        spawnPositionObjects = new List<GameObject>();

        // Plane mode
        if(mode == 0)
        {
            if (volumetric)
            {
                
            }
            else
            {
                yield return StartCoroutine(GetPlacementPlane(numberOfPlacements, false));
            }
        }
        else if (mode == 0)
        {
            if(volumetric)
            {

            }
            else
            {

            }
        }

        /*for (int i = 0; i < numberOfPlacements; i++)
        {
            GameObject positionObject = new GameObject();
            positionObject.transform.parent = spawnGroup;
            Vector3 placementPosition = spawnCenter.position + Vector3.left * (numberOfPlacements / 2) + Vector3.right * i;

            positionObject.transform.position = placementPosition;
            spawnPositionObjects.Add(positionObject);
            yield return null;
        }*/
    }

    private IEnumerator GetPlacementPlane(int numberOfPlacements, bool volumetric)
    {
        // Initial setup
        if (volumetric)
        {

        }
        else
        {
            placementBase = Instantiate(placementBasePrefabs[0], spawnGroup.transform, false);

            LayoutGroup3D layoutGroup = placementBase.GetComponent<LayoutGroup3D>();
            layoutGroup.GridConstraintCount = dimension.y;

            for(int i = 0; i < numberOfPlacements; i++)
            {
                GameObject positionObject = new GameObject();
                spawnPositionObjects.Add(positionObject);
                positionObject.transform.parent = placementBase.transform;

                if(i > 0)
                {
                    Vector3 distance = spawnPositionObjects[i - 1].transform.localPosition - spawnPositionObjects[i].transform.localPosition;
                    Debug.Log(distance);
                    Debug.Log(distance.magnitude);
                }


                yield return null;
            }

            // Generate Collider Box
            BoxCollider boxCollider = placementBase.GetComponent<BoxCollider>();
            boxCollider.center = Vector3.zero;
            boxCollider.size = new Vector3((layoutGroup.ElementDimensions.x + layoutGroup.Spacing) * dimension.x, (layoutGroup.ElementDimensions.y + layoutGroup.Spacing) * dimension.y, 0.001f);

        }
        // Fill
    }

    #endregion

    #region Debug
    public void DebugStart()
    {
        dimension = new Vector3Int(4, 4, 4);
        mode = 0;
        StartSpawnOperation();
        ChangeVisibleComponent(3);
    }
    #endregion
}
