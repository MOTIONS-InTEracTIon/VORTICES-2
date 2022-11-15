using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using UnityEngine;

namespace Vortices
{
    public class SpawnGroup : MonoBehaviour
    {
        // Other references
        protected LayoutGroup3D layoutGroup;
        protected List<GameObject> rowList;

        // Data variables
        [HideInInspector] public List<string> filePaths;
        [HideInInspector] public string rootUrl { get; set; }

        // Utility
        public int globalIndex;
        public int movIndex;
        public string globalJumpDirection;
        public int lastJumpGlobalIndex;
        protected bool lastLoadForward;
        protected List<string> loadPaths;
        protected List<GameObject> unloadObjects;
        protected List<GameObject> loadObjects;
        protected Fade groupFader;
        private int spawnedHandlingCoroutinesRunning;

        // Settings
        [HideInInspector] public Vector3Int dimension { get; set; }
        public string browsingMode { get; set; }
        public string displayMode { get; set; }
        public float softFadeUpperAlpha { get; set; }
  

        // Auxiliary Task Class
        public GameObject renderManager;

        #region Multimedia Spawn
        public IEnumerator StartSpawnOperation(int offsetGlobalIndex, bool softFadeIn)
        {
            // Startup
            globalIndex = offsetGlobalIndex;
            lastLoadForward = true;
            globalJumpDirection = "Both";
            rowList = new List<GameObject>();
            loadPaths = new List<string>();
            unloadObjects = new List<GameObject>();
            loadObjects = new List<GameObject>();
            groupFader = GetComponent<Fade>();

            int startingLoad = dimension.x * dimension.y;

            // Execution
            yield return StartCoroutine(ObjectSpawn(0, startingLoad, true, false, softFadeIn));
        }

        // Spawns files using overriden GenerateExitObjects and GenerateEnterObjects
        protected IEnumerator ObjectSpawn(int unloadNumber, int loadNumber, bool forwards, bool horizontally, bool softFade)
        {
            ObjectPreparing(unloadNumber, loadNumber, forwards, horizontally);
            yield return StartCoroutine(DestroyObjectHandling());
            yield return StartCoroutine(ObjectLoad(loadNumber, forwards, horizontally));
            yield return StartCoroutine(SpawnedObjectHandling(softFade));
        }
    
        // Destroys placement objects not needed and insert new ones at the same time
        protected void ObjectPreparing(int unloadNumber, int loadNumber, bool forwards, bool horizontally)
        {
            // Generate list of child objects to leave the scene
            GenerateExitObjects(unloadNumber, forwards, horizontally);

            // Generate list of child objects to spawn into the scene
            GenerateEnterObjects(loadNumber, forwards, horizontally);
        }

        protected IEnumerator ObjectLoad(int loadNumber, bool forwards, bool horizontally)
        {
            // Generate selection path to get via render
            yield return StartCoroutine(GenerateLoadPaths(loadNumber, forwards, horizontally));

            // Make them appear in the scene
            RenderManager render = Instantiate(renderManager).GetComponent<RenderManager>();
            yield return StartCoroutine(render.PlaceMultimedia(loadPaths, loadObjects, browsingMode, displayMode));

            Destroy(render.gameObject);
        }

        protected IEnumerator DestroyObjectHandling()
        {
            foreach (GameObject go in unloadObjects)
            {
                Destroy(go.gameObject);
            }

            yield return null;
        }

        protected IEnumerator SpawnedObjectHandling(bool softFade)
        {
            spawnedHandlingCoroutinesRunning = 0;

            foreach (GameObject go in loadObjects)
            {
                Fade objectFader = go.GetComponent<Fade>();
                if (softFade)
                {
                    objectFader.upperAlpha = softFadeUpperAlpha;
                }
                TaskCoroutine fadeCoroutine = new TaskCoroutine(objectFader.FadeInCoroutine());
                fadeCoroutine.Finished += delegate(bool manual)
                {
                    spawnedHandlingCoroutinesRunning--;
                };
                spawnedHandlingCoroutinesRunning++;
            }

            while (spawnedHandlingCoroutinesRunning > 0)
            {
                yield return null;
            }

        }

        protected IEnumerator GenerateLoadPaths(int loadNumber, bool forwards, bool horizontally)
        {
            int index = 0;

            if (forwards)
            {
                if (!lastLoadForward)
                {
                    if (horizontally)
                    {
                        if (globalJumpDirection == "None 1")
                        {
                            globalIndex = -1 + lastJumpGlobalIndex + (loadNumber * dimension.y - 1);
                        }
                        else if (globalJumpDirection == "None 2")
                        {
                            globalIndex = -2 + lastJumpGlobalIndex + (loadNumber * dimension.y - 1);
                        }
                        else
                        {
                            globalIndex += loadNumber * dimension.y - 1;
                        }
                    }
                    else
                    {
                        globalIndex += loadNumber * dimension.y - 1;
                    }
                }
                lastLoadForward = true;
            }
            else
            {
                if (lastLoadForward)
                {
                    if (horizontally)
                    {
                        if (globalJumpDirection == "None 1")
                        {
                            globalIndex = 1 + lastJumpGlobalIndex - (loadNumber * dimension.y - 1);
                        }
                        else if (globalJumpDirection == "None 2") 
                        {
                            globalIndex = 2 + lastJumpGlobalIndex - (loadNumber * dimension.y - 1);
                        }
                        else
                        {
                            globalIndex -= loadNumber * dimension.y - 1;
                        }
                    }
                    else
                    {
                        globalIndex -= loadNumber * dimension.y - 1;
                    }
                }
                lastLoadForward = false;
            }
            if (!forwards && !horizontally)
            {
                globalIndex -= loadNumber + 1;
            }

            while (index < loadNumber)
            {
                if (horizontally)
                {


                    for (int i = 0; i < dimension.y; i++)
                    {
                        string actualPath = "";
                        // Uses rootUrl for online mode and searches filePaths in circular manner for local mode
                        if (browsingMode == "Online")
                        {
                            actualPath = rootUrl;
                        }
                        else if (browsingMode == "Local")
                        {
                            if (forwards)
                            {
                                if (globalJumpDirection == "Both")
                                {
                                    actualPath = CircularList.GetElement<string>(filePaths, globalIndex + 1 + (dimension.x * i));
                                }
                                else
                                {
                                    actualPath = CircularList.GetElement<string>(filePaths, globalIndex + 1 - (dimension.x * (dimension.y - 1 - i)));

                                }
                            }
                            else
                            {
                                if (globalJumpDirection == "Both")
                                {
                                    actualPath = CircularList.GetElement<string>(filePaths, globalIndex - 1 - (dimension.x * (dimension.y - 1 - i)));
                                }
                                else
                                {
                                    actualPath = CircularList.GetElement<string>(filePaths, globalIndex - 1 + (dimension.x * i));
                                }
                            }
                        }
                        loadPaths.Add(actualPath);
                        index++;
                    }

                    if (forwards)
                    {
                        if (globalJumpDirection == "Both")
                        {
                            lastJumpGlobalIndex = globalIndex;
                            globalIndex += 1 + (dimension.x * (dimension.y - 1));
                        }
                        else
                        {
                            globalIndex++;
                        }

                        movIndex++;
                    }
                    else
                    {
                        if (globalJumpDirection == "Both")
                        {
                            lastJumpGlobalIndex = globalIndex;
                            globalIndex -= 1 + dimension.x * (dimension.y - 1);
                        }
                        else
                        {
                            globalIndex--;
                        }

                        movIndex--;
                    }
                    

                    if (movIndex < 0)
                    {
                        float position = ListUtils.nfmod(movIndex, dimension.x);
                        if (position == 2)
                        {
                            globalJumpDirection = "None 1";
                        }
                        else if (position == 1)
                        {
                            globalJumpDirection = "None 2";
                        }
                        else
                        {
                            globalJumpDirection = "Both";
                        }
                    }
                    else
                    {
                        float position = movIndex % dimension.x;
                        if (position == 1)
                        {
                            globalJumpDirection = "None 1";
                        }
                        else if (position == 2)
                        {
                            globalJumpDirection = "None 2";
                        }
                        else
                        {
                            globalJumpDirection = "Both";
                        }
                    }

                    yield return null;
                }
                else
                {
                    globalIndex++;
                    string actualPath = "";
                    // Uses rootUrl for online mode and searches filePaths in circular manner for local mode
                    if (browsingMode == "Online")
                    {
                        actualPath = rootUrl;
                    }
                    else if (browsingMode == "Local")
                    {
                        actualPath = CircularList.GetElement<string>(filePaths, globalIndex);
                    }
                    // Look if that path is in memory X
                    loadPaths.Add(actualPath);

                    index++;
                    yield return null;
                }
            }

            if (!forwards && !horizontally)
            {
                globalIndex -= loadNumber - 1;
            }
        }

        public void GenerateExitObjects(int unloadNumber, bool forwards, bool horizontally)
        {
            unloadObjects = new List<GameObject>();
            // Horizontally cant happen at startup so unloadNumber is always = dimension.x for it
            if (horizontally)
            {
                for (int i = 0; i < dimension.y; i++)
                {
                    if (forwards)
                    {
                        // Get first element of each row
                        unloadObjects.Add(rowList[i].transform.GetChild(0).gameObject);
                    }
                    else
                    {
                        // Get last element of each row
                        unloadObjects.Add(rowList[i].transform.GetChild(rowList[i].transform.childCount - 1).gameObject);
                    }
                }
            }
            else
            {
                for (int i = 0; i < unloadNumber / dimension.x; i++)
                {
                    if (forwards)
                    {
                        unloadObjects.Add(rowList[0].gameObject);
                        rowList.RemoveAt(0);
                    }
                    else
                    {
                        unloadObjects.Add(rowList[rowList.Count - 1].gameObject);
                        rowList.RemoveAt(rowList.Count - 1);
                    }
                }
            }
        }

        public void GenerateEnterObjects(int loadNumber, bool forwards, bool horizontally)
        {
            loadObjects = new List<GameObject>();

            if (horizontally)
            {
                for (int i = 0; i < dimension.y; i++)
                {
                    GameObject rowObject = rowList[i];

                    GameObject positionObject = new GameObject();
                    positionObject.AddComponent<Fade>();

                    positionObject.transform.parent = rowObject.transform;
                    if(!forwards)
                    {
                        positionObject.transform.SetAsFirstSibling();
                    }


                    loadObjects.Add(positionObject);
                }
            }
            else
            {
                for (int i = 0; i < loadNumber / dimension.x; i++)
                {
                    GameObject rowObject = BuildRow(forwards);
                    for (int j = 0; j < dimension.x; j++)
                    {
                        GameObject positionObject = new GameObject();
                        positionObject.AddComponent<Fade>();
                        positionObject.transform.parent = rowObject.transform;

                        loadObjects.Add(positionObject);
                    }
                }
            }
            
        }

        protected virtual GameObject BuildRow(bool onTop)
        {
            // Both spawnGroups build its row differently
            Debug.Log("BuildRow was not overriden");
            return null;
        }

        public IEnumerator SpawnForwards(int loadNumber, bool softFade, bool horizontally)
        {
            // Startup
            loadPaths = new List<string>();
            unloadObjects = new List<GameObject>();
            // Execution
            yield return StartCoroutine(ObjectSpawn(loadNumber, loadNumber, true, horizontally,  softFade));

        }

        public IEnumerator SpawnBackwards(int loadNumber, bool softFade, bool horizontally)
        {
            // Startup
            loadPaths = new List<string>();
            unloadObjects = new List<GameObject>();
            // Execution
            yield return StartCoroutine(ObjectSpawn(loadNumber, loadNumber, false, horizontally, softFade));
        }

        #endregion

    }
}
