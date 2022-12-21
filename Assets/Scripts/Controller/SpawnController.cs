using System.Collections;
using System.Collections.Generic;
using System.Linq;

using UnityEngine;


namespace Vortices
{
    public class SpawnController : MonoBehaviour
    {
        // Auxiliary references
        private SessionManager sessionManager;

        // Display (Prefabs for every base and a list for every environment)
        [SerializeField] List<GameObject> placementCircularBasePrefabs;
        [SerializeField] List<GameObject> placementMuseumBasePrefabs;
        public GameObject placementBase;

        // Other references
        private GameObject spawnGroup;
        public RighthandTools righthandTools;

        // Data
        public bool isElementHovered;
        public int elementsHovered;
        public bool movingOperationRunning;

        public void Initialize()
        {
            sessionManager = GameObject.Find("SessionManager").GetComponent<SessionManager>();
            spawnGroup = GameObject.Find("Information Object Group");
            righthandTools = GameObject.FindObjectOfType<RighthandTools>(true);
        }

        #region Base Spawn
        public void StartSession()
        {
            righthandTools.Initialize();
            // A fork for every environment possible
            if (sessionManager.environmentName == "Circular")
            {
                // A fork for every base compatible with environment
                if (sessionManager.displayMode == "Plane")
                {
                    Vector3 positionOffset = new Vector3(0, 0, 0.5f); ;
                    placementBase = Instantiate(placementCircularBasePrefabs[0], spawnGroup.transform.position + positionOffset, placementCircularBasePrefabs[0].transform.rotation, spawnGroup.transform);
                    CircularSpawnBase spawnBase = placementBase.GetComponent<CircularSpawnBase>();
                    spawnBase.displayMode = sessionManager.displayMode;
                    spawnBase.dimension = sessionManager.dimension;
                    spawnBase.volumetric = sessionManager.volumetric;
                    if (sessionManager.browsingMode == "Local")
                    {
                        spawnBase.browsingMode = sessionManager.browsingMode;
                        spawnBase.filePaths = sessionManager.filePaths;
                    }
                    else if (sessionManager.browsingMode == "Online")
                    {
                        spawnBase.browsingMode = sessionManager.browsingMode;
                        spawnBase.rootUrl = sessionManager.rootUrl;
                    }
                    spawnBase.StartGenerateSpawnGroup();
                }
                else if (sessionManager.displayMode == "Radial")
                {
                    placementBase = Instantiate(placementCircularBasePrefabs[1], spawnGroup.transform.position, placementCircularBasePrefabs[1].transform.rotation, spawnGroup.transform);
                    CircularSpawnBase spawnBase = placementBase.GetComponent<CircularSpawnBase>();
                    spawnBase.displayMode = sessionManager.displayMode;
                    spawnBase.dimension = sessionManager.dimension;
                    spawnBase.volumetric = sessionManager.volumetric;
                    if (sessionManager.browsingMode == "Local")
                    {
                        spawnBase.browsingMode = sessionManager.browsingMode;
                        spawnBase.filePaths = sessionManager.filePaths;
                    }
                    else if (sessionManager.browsingMode == "Online")
                    {
                        spawnBase.browsingMode = sessionManager.browsingMode;
                        spawnBase.rootUrl = sessionManager.rootUrl;
                    }
                    spawnBase.StartGenerateSpawnGroup();
                }
            }
            else if (sessionManager.environmentName == "Museum")
            {
                // A fork for every base compatible with environment
                if (sessionManager.displayMode == "Museum")
                {
                    // This base wont be instantiated as it has a premade spatial distribution (This can be changed to create more multimedia arrangements
                    MuseumSpawnBase spawnBase = GameObject.FindObjectOfType<MuseumBase>();
                    placementBase = spawnBase.gameObject;
                    if (sessionManager.browsingMode == "Local")
                    {
                        spawnBase.browsingMode = sessionManager.browsingMode;
                        spawnBase.filePaths = sessionManager.filePaths;
                    }
                    else if (sessionManager.browsingMode == "Online")
                    {
                        spawnBase.browsingMode = sessionManager.browsingMode;
                        spawnBase.rootUrl = sessionManager.rootUrl;
                    }
                    StartCoroutine(spawnBase.StartGenerateSpawnElements());
                }
            }
        }

        public IEnumerator StopSession()
        {
            if (!sessionManager.sessionLaunchRunning)
            {
                if (placementBase != null)
                {
                    // Fork for every environment possible 
                    if (sessionManager.displayMode == "Circular")
                    {
                        CircularSpawnBase circularSpawnBase = placementBase.GetComponent<CircularSpawnBase>();
                        yield return StartCoroutine(circularSpawnBase.DestroyBase());
                    }
                }

                StartCoroutine(sessionManager.StopSessionCoroutine());
            }
        }
        #endregion

        // This function makes sure there are no hovers on movement and that the selected one is kept selected
        public void ResetElements()
        {
            List<Element> elements = placementBase.GetComponentsInChildren<Element>().ToList();

            foreach (Element element in elements)
            {
                if (!(righthandTools.actualSelectedElement != null &&
                    righthandTools.actualSelectedElement.url == element.url))
                {
                    Renderer handInteractorRenderer = element.handInteractor.GetComponent<Renderer>();
                    Color rendererColor = handInteractorRenderer.material.color;

                    Color newColor = handInteractorRenderer.material.color;
                    handInteractorRenderer.material.color = new Color(rendererColor.r,
                        rendererColor.g,
                        rendererColor.b, 0f);
                }
            }
        }

        public bool IsElementHovered()
        {
            if (elementsHovered > 0)
            {
                return true;
            }
            
            return false;

        }
    }
}

