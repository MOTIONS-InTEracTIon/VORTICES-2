using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using System.Linq;

namespace Vortices
{ 
    public class SessionManager : MonoBehaviour
    {
        // Other references
        private GameObject spawnGroup;
        private GameObject mapObjects;

        // Static instance
        public static SessionManager instance;

        // Settings
        public string sessionName;
        public int userId;
        public string environmentName;
        // Environment Settings
        public string displayMode;
        public string browsingMode;
        public bool volumetric;
        public Vector3Int dimension;
        public string rootUrl;
        public List<string> filePaths;
        // Session Manager settings
        public float initializeTime = 3.0f;

        // Display (Prefabs for every base and a list for every environment)
        [SerializeField] List<GameObject> placementCircularBasePrefabs;
        [SerializeField] List<GameObject> placementMuseumBasePrefabs;
        private GameObject placementBase;

        // Controllers
        [SerializeField] private SceneTransitionManager actualTransitionManager;
        [SerializeField] public CategoryController categoryController;
        [SerializeField] public ElementCategoryController elementCategoryController;
        private CategorySelector categorySelector;
        [SerializeField] public LoggingController loggingController;

        // Coroutine
        private bool sessionLaunchRunning;


        private void Start()
        {
            if (instance != null)
            {
                Destroy(gameObject);
                return;
            }

            instance = this;
            DontDestroyOnLoad(gameObject);
        }

        #region Data Operations
        public void LaunchSession()
        {
            if (!sessionLaunchRunning)
            {
                StartCoroutine(LaunchSessionCoroutine(sessionName, userId, environmentName));
            }
        }

        public IEnumerator LaunchSessionCoroutine(string sessionName, int userId, string environmentName)
        {
            sessionLaunchRunning = true;
            // Switch to environment scene
            actualTransitionManager = GameObject.Find("TransitionManager").GetComponent<SceneTransitionManager>();
            if (environmentName == "Circular")
            {
                actualTransitionManager.sceneTarget = 1;
            }
            else if (environmentName == "Museum")
            {
                actualTransitionManager.sceneTarget = 2;
            }

            yield return StartCoroutine(actualTransitionManager.GoToSceneRoutine());

            yield return new WaitForSeconds(initializeTime);

            // When done, configure controllers of the scene
            actualTransitionManager = GameObject.FindGameObjectsWithTag("Manager").FirstOrDefault(manager => manager.name == "TransitionManager").GetComponent<SceneTransitionManager>();
            categoryController = GameObject.FindObjectOfType<CategoryController>(true);
            elementCategoryController = GameObject.FindObjectOfType<ElementCategoryController>(true);
            loggingController = GameObject.FindObjectOfType<LoggingController>(true);

            elementCategoryController.Initialize();
            loggingController.Initialize();

            // Environment dependant references
            spawnGroup = GameObject.Find("Information Object Group");
            mapObjects = GameObject.Find("Map Objects");
            sessionLaunchRunning = false;
        }

        public IEnumerator StopSessionCoroutine()
        {
            sessionLaunchRunning = true;
            if (placementBase != null)
            {
                // Fork for every environment possible 
                if (displayMode == "Circular")
                {
                    SpawnBase spawnBase = placementBase.GetComponent<SpawnBase>();
                    yield return StartCoroutine(spawnBase.DestroyBase());
                }
            }

            yield return StartCoroutine(actualTransitionManager.GoToSceneRoutine());

            yield return new WaitForSeconds(initializeTime);

            categorySelector = GameObject.FindObjectOfType<CategorySelector>(true);

            sessionLaunchRunning = false;
        }

        #endregion

        #region Base Spawn
        public void StartSession()
        {
            // A fork for every environment possible
            if(environmentName == "Circular")
            {
                // A fork for every base compatible with environment
                if(displayMode == "Plane")
                {
                    Vector3 positionOffset = new Vector3(0, 0, 0.5f); ;
                    placementBase = Instantiate(placementCircularBasePrefabs[0], spawnGroup.transform.position + positionOffset, placementCircularBasePrefabs[0].transform.rotation, spawnGroup.transform);
                    SpawnBase spawnBase = placementBase.GetComponent<SpawnBase>();
                    spawnBase.displayMode = displayMode;
                    spawnBase.dimension = dimension;
                    spawnBase.volumetric = volumetric;
                    if (browsingMode == "Local")
                    {
                        spawnBase.browsingMode = browsingMode;
                        spawnBase.filePaths = filePaths;
                    }
                    else if (browsingMode == "Online")
                    {
                        spawnBase.browsingMode = browsingMode;
                        spawnBase.rootUrl = rootUrl;
                    }
                    spawnBase.StartGenerateSpawnGroup();
                }
                else if (displayMode == "Radial")
                {
                    placementBase = Instantiate(placementCircularBasePrefabs[1], spawnGroup.transform.position, placementCircularBasePrefabs[1].transform.rotation, spawnGroup.transform);
                    SpawnBase spawnBase = placementBase.GetComponent<SpawnBase>();
                    spawnBase.displayMode = displayMode;
                    spawnBase.dimension = dimension;
                    spawnBase.volumetric = volumetric;
                    if (browsingMode == "Local")
                    {
                        spawnBase.browsingMode = browsingMode;
                        spawnBase.filePaths = filePaths;
                    }
                    else if (browsingMode == "Online")
                    {
                        spawnBase.browsingMode = browsingMode;
                        spawnBase.rootUrl = rootUrl;
                    }
                    spawnBase.StartGenerateSpawnGroup();
                }
            }
            else if(environmentName == "Museum")
            {
                // A fork for every base compatible with environment
                if (displayMode == "Museum")
                {
                    // This base wont be instantiated as it has a premade spatial distribution (This can be changed to create more multimedia arrangements
                    MuseumSpawnBase spawnBase = GameObject.FindObjectOfType<MuseumBase>();
                    placementBase = spawnBase.gameObject;
                    if (browsingMode == "Local")
                    {
                        spawnBase.browsingMode = browsingMode;
                        spawnBase.filePaths = filePaths;
                    }
                    else if (browsingMode == "Online")
                    {
                        spawnBase.browsingMode = browsingMode;
                        spawnBase.rootUrl = rootUrl;
                    }
                    StartCoroutine(spawnBase.StartGenerateSpawnElements());
                }
            }
        }

        public void StopSession()
        {
            if (!sessionLaunchRunning)
            {
                StartCoroutine(StopSessionCoroutine());
            }
        }
        #endregion

    }
}

