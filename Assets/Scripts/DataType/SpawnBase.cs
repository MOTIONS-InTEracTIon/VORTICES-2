using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using System.IO;

public class SpawnBase : MonoBehaviour
{
    // SpawnBase Data Components
    [HideInInspector] public FilePath optionFilePath;

    protected int globalIndex;
    protected bool lastLoadForward;
    protected List<string> loadPaths;
    protected List<GameObject> unloadObjects;
    protected List<GameObject> loadObjects;
    public GameObject elementPrefab;
    [HideInInspector] public Transform spawnCenter;


    public bool volumetric { get; set; }
    [HideInInspector] public Vector3Int dimension;

    // Auxiliary Task Class
    [HideInInspector] public GameObject renderManager;

    // Coroutine
    protected Queue<IEnumerator> coroutineQueue;


    #region Multimedia Spawn
    public void StartSpawnOperation()
    {
        // Startup
        globalIndex = -1;
        lastLoadForward = true;
        loadPaths = new List<string>();
        unloadObjects = new List<GameObject>();
        // First time has to fill every slot so it uses width * height * layer
        int startingLoad = dimension.x * dimension.y;
        if (dimension.z != 0)
        {
            startingLoad *= dimension.z;
        }
        // Execution
        StartCoroutine(ObjectSpawn(0, startingLoad, true));
    }

    // Spawns files using overriden GenerateObjectPlacement
    private IEnumerator ObjectSpawn(int unloadNumber, int loadNumber, bool forwards)
    {
        ObjectPreparing(unloadNumber, loadNumber, forwards);
        yield return StartCoroutine(ObjectLoad(loadNumber, forwards));
    }

    private IEnumerator ObjectLoad(int loadNumber, bool forwards)
    {
        // Generate selection path to get via render
        yield return StartCoroutine(GenerateLoadPaths(loadNumber, forwards));

        // Make them appear in the scene
        RenderManager render = Instantiate(renderManager).GetComponent<RenderManager>();
        yield return StartCoroutine(render.PlaceMultimedia(loadPaths,
                                                           elementPrefab,
                                                           false, false,
                                                           loadObjects));
        Destroy(render.gameObject);
        // Eliminate 
    }

    // Destroys objects not needed and insert new ones at the same time
    private void ObjectPreparing(int unloadNumber, int loadNumber, bool forwards)
    {
        // Generate list of child objects to destroy
        GenerateDestroyObjects(unloadNumber, forwards);
        foreach (GameObject go in unloadObjects)
        {
            Destroy(go.gameObject);
        }
        // Generate list of child objects to spawn into
        GenerateObjectPlacement(loadNumber, !forwards);
    }

    private IEnumerator GenerateLoadPaths(int loadNumber, bool forwards)
    {
        // CHANGE: Make the search double the size, so the next batch is ready when you switch to the next
        int index = 0;

        if (forwards)
        {
            if (!lastLoadForward)
            {
                globalIndex += loadNumber * dimension.x - 1;
            }
            lastLoadForward = true;
        }
        else
        {
            if (lastLoadForward)
            {
                globalIndex -= loadNumber * dimension.x - 1;
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
                while (search && attempt < attempts)
                {
                    actualPath = CircularList.GetElement<string>(optionFilePath.filePaths, globalIndex);
                    pathExtension = Path.GetExtension(actualPath);
                    if (pathExtension == ".png" ||
                        pathExtension == ".PNG" ||
                        pathExtension == ".JPG" ||
                        pathExtension == ".jpg" ||
                        pathExtension == ".jpeg"||
                        pathExtension == ".JPEG")
                    {
                        loadPaths.Add(actualPath);
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
                        pathExtension == ".PNG" ||
                        pathExtension == ".JPG" ||
                        pathExtension == ".jpg" ||
                        pathExtension == ".jpeg"||
                        pathExtension == ".JPEG")
                    {
                        loadPaths.Add(actualPath);
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

    public virtual void GenerateDestroyObjects(int unloadNumber, bool forwards)
    {
        // Destroying varies in each spawn base, Generating Destroy Objects must be overridden
        Debug.Log("Not being overridden");
    }

    public virtual void GenerateObjectPlacement(int loadNumber, bool asFirstSibling)
    {
        // Generating placements varies in each spawn base, Generating Object Placement must be overridden
        Debug.Log("Not being overridden");
    }

    public void StopSpawn()
    {
        Destroy(gameObject);
    }

    protected IEnumerator SpawnForwards(int loadNumber)
    {
        // Startup
        loadPaths = new List<string>();
        unloadObjects = new List<GameObject>();
        // Execution
        yield return StartCoroutine(ObjectSpawn(loadNumber, loadNumber, true));

    }

    protected IEnumerator SpawnBackwards(int loadNumber)
    {
        // Startup
        loadPaths = new List<string>();
        unloadObjects = new List<GameObject>();
        // Execution
        yield return StartCoroutine(ObjectSpawn(loadNumber, loadNumber, false));
    }

    protected IEnumerator CoroutineCoordinator()
    {
        while (true)
        {
            while (coroutineQueue.Count > 0)
                yield return StartCoroutine(coroutineQueue.Dequeue());
            yield return null;

        }
    }
    #endregion

}
