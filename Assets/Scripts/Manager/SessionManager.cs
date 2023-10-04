using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using System.Linq;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System.IO;

namespace Vortices
{
    public class SessionManager : MonoBehaviour
    {
        // Prefabs
        [SerializeField] private GameObject tutorialCircularPanelPrefab;
        [SerializeField] private GameObject tutorialMuseumPanelPrefab;

        // Static instance
        public static SessionManager instance;

        // Data
        public int demoId;

        // Settings
        public string sessionName;
        public int userId;
        public string environmentName;
        // Environment Settings
        public string displayMode;
        public string browsingMode;
        public bool volumetric;
        public Vector3Int dimension;
        public List<string> elementPaths;
        // Session Manager settings
        public float initializeTime = 2.0f;

        // Controllers
        [SerializeField] private SceneTransitionManager actualTransitionManager;
        [SerializeField] public CategoryController categoryController;
        [SerializeField] public ElementCategoryController elementCategoryController;
        [SerializeField] public SpawnController spawnController;
        [SerializeField] public AddonsController addonsController;
        private CategorySelector categorySelector;
        [SerializeField] public LoggingController loggingController;
        public InputController inputController;
        public RighthandTools righthandTools;

        // Coroutine
        public bool sessionLaunchRunning;

        public GameObject currentlySelected;
        public GameObject lastSelected;
        private void OnEnable()
        {
            SceneManager.sceneLoaded += OnSceneLoaded;
        }

        private void OnDisable()
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
        }

        private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            lastSelected = null; // Reset lastSelected when a new scene is loaded

            if (GameObject.Find("Scenario 1 Button") != null)
            {
                GameObject.Find("Scenario 1 Button").GetComponent<Button>().onClick.AddListener(delegate { StartScenario(1); });
                GameObject.Find("Scenario 2 Button").GetComponent<Button>().onClick.AddListener(delegate { StartScenario(2); });
                GameObject.Find("Scenario 3 Button").GetComponent<Button>().onClick.AddListener(delegate { StartScenario(3); });
            }
        }

        private void Start()
        {
            if (instance != null)
            {
                Destroy(gameObject);
                return;
            }

            categoryController = GameObject.FindObjectOfType<CategoryController>(true);
            elementCategoryController = GameObject.FindObjectOfType<ElementCategoryController>(true);
            inputController = GameObject.FindObjectOfType<InputController>(true);

            inputController.Initialize();
            addonsController.Initialize();

            instance = this;
            DontDestroyOnLoad(gameObject);
        }


        #region UI Handling

        private void Update()
        {
            if (EventSystem.current.currentSelectedGameObject != null)
            {
                currentlySelected = EventSystem.current.currentSelectedGameObject;
            }
            else
            {
                currentlySelected = null;
            }
            if (EventSystem.current.currentSelectedGameObject == null || !EventSystem.current.currentSelectedGameObject.activeInHierarchy)
            {
                FindAndSetNextSelectable();
            }
            else
            {
                lastSelected = EventSystem.current.currentSelectedGameObject;
            }
        }

        private void FindAndSetNextSelectable()
        {
            if (GameObject.Find("Canvas") == null)
            {
                return;
            }

            GameObject canvasObject = GameObject.Find("Canvas"); // Find the GameObject named "Canvas"

            if (canvasObject == null)
            {
                Debug.LogError("No GameObject named 'Canvas' found in the scene.");
                return;
            }

            Canvas canvasComponent = canvasObject.GetComponent<Canvas>(); // Get the Canvas component from the GameObject

            if (canvasComponent == null)
            {
                Debug.LogError("No Canvas component found on the 'Canvas' GameObject.");
                return;
            }

            Selectable[] selectables = canvasComponent.GetComponentsInChildren<Selectable>();

            if (selectables.Length == 0)
            {
                return; // No selectables in the canvas
            }

            int startIndex = 0;
            if (lastSelected != null)
            {
                int lastIndex = System.Array.FindIndex(selectables, selectable => selectable.gameObject == lastSelected);
                if (lastIndex != -1)
                {
                    startIndex = (lastIndex + 1) % selectables.Length;
                }
            }

            for (int i = 0; i < selectables.Length; i++)
            {
                int index = (startIndex + i) % selectables.Length;
                if (selectables[index].gameObject.activeInHierarchy && selectables[index].interactable)
                {
                    EventSystem.current.SetSelectedGameObject(selectables[index].gameObject);
                    lastSelected = selectables[index].gameObject;
                    break;
                }
            }
        }

        #endregion

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

            GameObject keyboard = GameObject.Find("Keyboard Canvas");
            if (keyboard != null)
            {
                keyboard.GetComponent<HandKeyboard>().RemoveInputField();
            }


            // Switch to environment scene
            actualTransitionManager = GameObject.Find("TransitionManager").GetComponent<SceneTransitionManager>();
            actualTransitionManager.returnToMain = false;
            yield return StartCoroutine(actualTransitionManager.GoToSceneRoutine());

            yield return new WaitForSeconds(initializeTime);

            // When done, configure controllers of the scene
            actualTransitionManager = GameObject.FindObjectOfType<SceneTransitionManager>(true);
            categoryController = GameObject.FindObjectOfType<CategoryController>(true);
            elementCategoryController = GameObject.FindObjectOfType<ElementCategoryController>(true);
            loggingController = GameObject.FindObjectOfType<LoggingController>(true);
            spawnController = GameObject.FindObjectOfType<SpawnController>(true);
            righthandTools = GameObject.FindObjectOfType<RighthandTools>(true);

            elementCategoryController.Initialize();
            loggingController.Initialize();
            spawnController.Initialize();

            inputController.RestartInputs();

            GameObject panel = tutorialCircularPanelPrefab.gameObject;
            if(displayMode == "Museum")
            {
                panel = Instantiate(tutorialMuseumPanelPrefab);
            }
            else
            {
                panel = Instantiate(tutorialCircularPanelPrefab);
            }

            panel.GetComponent<TutorialPanel>().Initialize(Camera.main.gameObject);


            sessionLaunchRunning = false;
        }

        public IEnumerator StopSessionCoroutine()
        {
            sessionLaunchRunning = true;
            actualTransitionManager = GameObject.Find("TransitionManager").GetComponent<SceneTransitionManager>();
            Fade toolsFader = righthandTools.GetComponent<Fade>();
            yield return StartCoroutine(toolsFader.FadeOutCoroutine());

            actualTransitionManager.returnToMain = true;
            yield return StartCoroutine(actualTransitionManager.GoToSceneRoutine());

            sessionLaunchRunning = false;
        }
        #endregion

        #region Demo Data Overrides
        public void StartScenario(int scenarioNumber)
        {

            switch (scenarioNumber)
            {
                case 1:
                    InjectScenarioData(1, "Circular");
                    break;
                case 2:
                    InjectScenarioData(2, "Circular");
                    break;
                case 3:
                    InjectScenarioData(3, "Museum");
                    break;
            }
            userId++;
        }

        private void InjectScenarioData(int variant, string environmentName)
        {
            // Inject needed data
            // Session Data
            AddonsController.instance.LoadAddonObjects();
            // Select Environment
            if (environmentName == "Circular")
            {
                AddonsController.instance.SetEnvironment(0);
                this.environmentName = environmentName;
            }
            else if (environmentName == "Museum")
            {
                AddonsController.instance.SetEnvironment(1);
                this.environmentName = environmentName;
            }

            sessionName = "Demo Array";

            // Add next id
            string dataPath = Application.dataPath;
            string results = "Results";
            string resultsPath = Path.Combine(dataPath, results);
            resultsPath = Path.Combine(resultsPath, sessionName);
            int largestNumericValue = 0;
            if(Directory.Exists(resultsPath))
            {
                string[] subdirectories = Directory.GetDirectories(resultsPath);
                foreach (string subdirectory in subdirectories)
                {
                    string folder = Path.GetFileName(subdirectory);

                    if (int.TryParse(folder, out int numericValue))
                    {
                        if (numericValue > largestNumericValue)
                        {
                            largestNumericValue = numericValue;
                        }
                    }
                }
                userId = largestNumericValue++;
            }
            else
            {
                userId = 0;
            }

            // Category Data (The persistence will auto create the demo categories)
            categoryController.Initialize();

            // Environment Data
            switch (variant)
            {
                case 1:
                    displayMode = "Plane";
                    volumetric = true;
                    dimension = new Vector3Int(4, 3, 5);
                    break;
                case 2:
                    displayMode = "Radial";
                    volumetric = true;
                    dimension = new Vector3Int(8, 3, 5);
                    break;
                case 3:
                    displayMode = "Museum";
                    break;
            }
            browsingMode = "Local";

            // Element Path Data
            string folderName = "images";
            string streamingAssetsPath = Application.streamingAssetsPath;

            string folderPath = Path.Combine(streamingAssetsPath, folderName);
            if (Directory.Exists(folderPath))
            {
                elementPaths = Directory.GetFiles(folderPath).ToList();
            }

            // Start Session
            LaunchSession();
        }


        #endregion
    }
}

