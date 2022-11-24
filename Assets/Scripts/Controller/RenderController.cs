using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using UnityEngine;
using Vuplex.WebView;

namespace Vortices
{
    public class RenderController : MonoBehaviour
    {
        #region Variables and properties
        // Other references
        [SerializeField] private GameObject webViewPrefab;
        [SerializeField] GameObject elementPrefab;

        // Auxiliary Components
        [SerializeField] GameObject loadManager;


        // Coroutine
        private int spawnCoroutinesRunning;

        // Debug
        public int finishes;

        public Result result;
        #endregion

        #region WebRequest Loading
        // Places a multimedia object in the world using the loadManager which picks up files via Unity's WebRequest (Old Version of multimedia loading)
        // loadPaths = Indicates the path in storage where the textures will be taken
        // prefabObject = Indicates the form that the texture will take when placed
        // asThumbnail = Lowers the quality of the texture (Used as thumbnails)
        // invisible = Places the prefab as invisible
        // placementObject = List of locations to be filled by multimedia objects

        public IEnumerator PlaceMultimedia(List<string> loadPaths, GameObject prefabObject, bool asThumbnail, bool invisible, List<GameObject> placementObjects)
        {
            result = Result.OnGoing;
            // X images will be placed inside X gameobjects
            LoadLocalController loadLocalManager = Instantiate(loadManager).GetComponent<LoadLocalController>();
            yield return StartCoroutine(loadLocalManager.GetMultipleImage(loadPaths, asThumbnail));
            if (loadLocalManager.result != Result.TotalError)
            {
                List<Texture2D> textures = loadLocalManager.textureBuffer;
                // Places images in object
                for (int i = 0; i < textures.Count; i++)
                {
                    if (placementObjects.Count == 1)
                    {
                        PlaceFitObject(textures[i], prefabObject, placementObjects[0], invisible);
                    }
                    else
                    {
                        PlaceFitObject(textures[i], prefabObject, placementObjects[i], invisible);
                    }
                }

                if (result == Result.OnGoing)
                {
                    result = Result.Success;
                }
            }
            else
            {
                // Show that couldnt get any image from the paths provided
                result = Result.TotalError;
            }

            Destroy(loadLocalManager.gameObject);

        }

        private void PlaceFitObject(Texture2D texture, GameObject prefabObject, GameObject fitObject, bool invisible)
        {
            // Spawning starts from instantiating the prefab (Destroying if fitobject has already a child)
            if(fitObject.transform.childCount > 0)
            {
                Destroy(fitObject.transform.GetChild(0).gameObject);
            }
            GameObject spawnObject = Instantiate(prefabObject, fitObject.transform.position, prefabObject.transform.rotation, fitObject.transform);

            if (invisible)
            {
                spawnObject.SetActive(false);
            }
            // Apply properties
            // Fit into object while conserving aspect ratio
            SizeToParent(spawnObject, texture);
            // Apply material file
            spawnObject.GetComponent<Renderer>().material.mainTexture = texture;
        }

        private void SizeToParent(GameObject spawnObject, Texture2D texture)
        {
            RectTransform objectRect = spawnObject.GetComponent<RectTransform>();

            float boundX = objectRect.localScale.x;

            float finalScaleY = objectRect.localScale.y;
            float finalScaleX = texture.width * finalScaleY / texture.height;

            objectRect.localScale = new Vector3 (finalScaleX, 1 , finalScaleY);

            if(objectRect.localScale.x > boundX)
            {
                finalScaleX = boundX;
                finalScaleY = texture.height * finalScaleX / texture.width;

                objectRect.localScale = new Vector3(finalScaleX, 1, finalScaleY);
            }
        }


        #endregion

        #region WebView Loading
        // Places a multimedia object in the world using the 3D Web View Asset (New Version of multimedia loading)
        // loadPaths = Indicates file paths for local loading only
        // placementObject = List of locations to be filled by multimedia objects
        public IEnumerator PlaceMultimedia(List<string> loadPaths, List<GameObject> placementObjects, string browsingMode, string displayMode)
        {

            result = Result.OnGoing;
            spawnCoroutinesRunning = 0;
            // Spawning
            for (int i = 0; i < placementObjects.Count; i++)
            {
                Canvas canvasHolder = new Canvas();
                // Plane
                if (displayMode == "Plane")
                {
                    canvasHolder = GenerateCanvas(placementObjects[i]);
                }
                // Radial
                else if (displayMode == "Radial")
                {
                    canvasHolder = GenerateCanvas(placementObjects[i]);
                    RotateToObject canvasHolderComponent = canvasHolder.gameObject.AddComponent<RotateToObject>();
                    canvasHolderComponent.offset = new Vector3(180f, 0, 180f);
                    canvasHolderComponent.followName = "Information Object Group";
                    canvasHolderComponent.StartRotating();
                }
                TaskCoroutine spawnCoroutine = new TaskCoroutine(GenerateCanvasWebView(canvasHolder, loadPaths[i], placementObjects[i], browsingMode));
                spawnCoroutine.Finished += delegate (bool manual) { spawnCoroutinesRunning--; };
                spawnCoroutinesRunning++;
            }

            while (spawnCoroutinesRunning > 0)
            {
                yield return null;
            }

            result = Result.Success;
        }

        private Canvas GenerateCanvas(GameObject placementObject)
        {
            GameObject canvasPrefab = Instantiate(elementPrefab, placementObject.transform.position, elementPrefab.transform.rotation, placementObject.transform);
            canvasPrefab.transform.localRotation = placementObject.transform.localRotation;
            Canvas canvasHolder = canvasPrefab.GetComponent<Canvas>();
            canvasHolder.worldCamera = Camera.main;
            return canvasHolder;
        }

        private IEnumerator GenerateCanvasWebView(Canvas canvasHolder, string loadPath, GameObject placementObject, string browsingMode)
        {
            GameObject canvas = Instantiate(webViewPrefab, canvasHolder.transform.position, canvasHolder.transform.rotation, canvasHolder.transform);
            CanvasWebViewPrefab canvasWebView = canvas.GetComponent<CanvasWebViewPrefab>();
            RectTransform rectTransform = canvasWebView.transform as RectTransform;
            rectTransform.anchoredPosition3D = Vector3.zero;
            rectTransform.offsetMin = Vector2.zero;
            rectTransform.offsetMax = Vector2.zero;
            canvas.transform.localScale = Vector3.one;
            canvasWebView.Resolution = 640;
            yield return StartCoroutine(canvasWebView.WaitUntilInitialized().AsIEnumerator());

            bool finished = false;
            canvasWebView.WebView.LoadProgressChanged += (sender, eventArgs) =>
            {
                //Debug.Log($"Load progress changed: {eventArgs.Type}, {eventArgs.Progress}");
                if (eventArgs.Type == ProgressChangeType.Finished)
                {
                    finished = true;
                    finishes++;
                   // Debug.Log("Finished " + finishes + " times");
                }
                if (eventArgs.Type == ProgressChangeType.Failed)
                {
                    Debug.Log("Load failed");
                    finished = false;
                }
            };

            string url = "";
            if (browsingMode == "Local")
            {
                url = loadPath.Replace(@"\", "/");
                url = url.Replace(" ", "%20");
                url = @"file://" + url;
            }
            else if (browsingMode == "Online")
            {
                url = loadPath;
            }

            canvasWebView.WebView.LoadUrl(url);
           
            while (!finished)
            {
                yield return null;
            }

            // After load configuration
            GameObject browserControls = canvasHolder.transform.Find("Browser Controls").gameObject;
            browserControls.SetActive(true);

            if (browsingMode == "Local")
            {
                yield return StartCoroutine(PauseWebView(canvasWebView).AsIEnumerator());
            }
            else if (browsingMode == "Online")
            {
                // If online it has to instantiate extra controls to go back and write url
                GameObject webUrl = browserControls.transform.Find("Web URL").gameObject;
                webUrl.SetActive(true);
                GameObject goBack = browserControls.transform.Find("Go Back").gameObject;
                GoBackBrowserButton goBackComponent = goBack.GetComponent<GoBackBrowserButton>();
                goBackComponent.canvasWebView = canvasWebView.WebView;
                goBack.SetActive(true);

                //canvasWebView.WebView.SetRenderingEnabled(false);
                var keyboard = CanvasKeyboard.Instantiate();
                keyboard.transform.SetParent(canvasHolder.transform, false);
                keyboard.transform.localEulerAngles = Vector3.zero;
                keyboard.transform.localScale = new Vector3(0.001f, 0.001f, 0.001f);
                var rectTransformKeyboard = keyboard.transform as RectTransform;
                rectTransformKeyboard.anchoredPosition3D = new Vector3(0, -0.15f ,0);
            }
        }

        public async Task PauseWebView(CanvasWebViewPrefab canvas)
        {
            await canvas.WebView.ExecuteJavaScript(
                "document.querySelectorAll('video, audio').forEach(mediaElement => mediaElement.pause())"
            );

        }

        #endregion
    }
}
