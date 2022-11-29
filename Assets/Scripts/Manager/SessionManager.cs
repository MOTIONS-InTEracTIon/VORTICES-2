using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using System.Linq;

namespace Vortices
{ 
    public class SessionManager : MonoBehaviour
    {
        // Static instance
        public static SessionManager instance;

        // Settings
        public string sessionName { get; set; }
        public int userId { get; set; }

        // Controllers
        [SerializeField] private SceneTransitionManager actualTransitionManager;
        [SerializeField] public CategoryController categoryController;
        [SerializeField] public ElementCategoryController elementCategoryController;
        [SerializeField] public CategorySelector categorySelector;

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

        public void LaunchSession(string sessionName, int userId, string environmentName)
        {
            if (!sessionLaunchRunning)
            {
                StartCoroutine(LaunchSessionCoroutine(sessionName, userId, environmentName));
            }
        }

        public IEnumerator LaunchSessionCoroutine(string sessionName, int userId, string environmentName)
        {
            sessionLaunchRunning = true;
            // Configure settings
            this.sessionName = sessionName;
            this.userId = userId;

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

            yield return new WaitForSeconds(5.0f);

            // When done, configure controllers of the scene
            actualTransitionManager = GameObject.FindGameObjectsWithTag("Manager").FirstOrDefault(manager => manager.name == "TransitionManager").GetComponent<SceneTransitionManager>();
            categoryController = GameObject.FindObjectOfType<CategoryController>(true);
            categorySelector = GameObject.FindObjectOfType<CategorySelector>(true);
            elementCategoryController = GameObject.FindObjectOfType<ElementCategoryController>(true);

            elementCategoryController.Initialize();
            categoryController.Initialize();
            sessionLaunchRunning = false;
        }

    }
}

