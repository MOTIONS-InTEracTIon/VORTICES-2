using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace Vortices
{
    public class SpawnGroup : MonoBehaviour
    {
        // Other references
        protected LayoutGroup3D layoutGroup;

        // SpawnBase Data Components
        [HideInInspector] public List<string> filePaths;

        // Utility
        protected int globalIndex;
        protected bool lastLoadForward;
        protected List<string> loadPaths;
        protected List<GameObject> unloadObjects;
        protected List<GameObject> loadObjects;
        public GameObject elementPrefab;
        protected Fade groupFader;
        public int fadeCoroutinesRunning;

        // Settings
        [HideInInspector] public Vector3Int dimension;
        public float softFadeUpperAlpha;

        // Auxiliary Task Class
        public GameObject renderManager; 

        #region Multimedia Spawn
        public virtual IEnumerator StartSpawnOperation(int offsetGlobalIndex, bool softFadeIn)
        {
            // Each spawn group has a different start operation, radial constructing aditional childs
            Debug.Log("Start Spawn Operation not being overridden");
            yield return null;
        }

        // Spawns files using overriden GenerateObjectPlacement
        protected IEnumerator ObjectSpawn(int unloadNumber, int loadNumber, bool forwards, bool softFade)
        {
            ObjectPreparing(unloadNumber, loadNumber, forwards);
            yield return StartCoroutine(ObjectLoad(loadNumber, forwards));
            yield return StartCoroutine(ObjectFadeIn(softFade));
        }
    
        // Destroys placement objects not needed and insert new ones at the same time
        protected void ObjectPreparing(int unloadNumber, int loadNumber, bool forwards)
        {
            // Generate list of child objects to destroy
            GenerateDestroyObjects(unloadNumber, forwards);
            foreach (GameObject go in unloadObjects)
            {
                Destroy(go.gameObject);
            }
            // Generate list of child objects to spawn into
            GenerateObjectPlacement(loadNumber, forwards);
        }

        protected IEnumerator ObjectLoad(int loadNumber, bool forwards)
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

        protected IEnumerator ObjectFadeIn(bool softFade)
        {
            fadeCoroutinesRunning = 0;

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
                    fadeCoroutinesRunning--;
                };
                fadeCoroutinesRunning++;
            }

            while (fadeCoroutinesRunning > 0)
            {
                yield return null;
            }

        }

        protected IEnumerator GenerateLoadPaths(int loadNumber, bool forwards)
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
                        actualPath = CircularList.GetElement<string>(filePaths, globalIndex);
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
                        actualPath = CircularList.GetElement<string>(filePaths, globalIndex);
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

        public virtual void GenerateObjectPlacement(int loadNumber, bool forwards)
        {
            // Generating placements varies in each spawn base, Generating Object Placement must be overridden
            Debug.Log("Not being overridden");
        }

        public IEnumerator SpawnForwards(int loadNumber, bool softFade)
        {
            // Startup
            loadPaths = new List<string>();
            unloadObjects = new List<GameObject>();
            // Execution
            yield return StartCoroutine(ObjectSpawn(loadNumber, loadNumber, true, softFade));

        }

        public IEnumerator SpawnBackwards(int loadNumber, bool softFade)
        {
            // Startup
            loadPaths = new List<string>();
            unloadObjects = new List<GameObject>();
            // Execution
            yield return StartCoroutine(ObjectSpawn(loadNumber, loadNumber, false, softFade));
        }

        #endregion

    }
}
