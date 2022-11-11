using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using UnityEngine;
using Vuplex.WebView;

namespace Vortices
{
    public class RenderManager : MonoBehaviour
    {
        #region Variables and properties
        // Other references
        [SerializeField] private GameObject webViewPrefab;
        [SerializeField] GameObject webViewCanvasPrefab;
        [SerializeField] GameObject webViewCanvasFollowerPrefab;

        // Auxiliary Components
        [SerializeField] GameObject loadManager;

        // Coroutine
        private int spawnCoroutinesRunning;


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
            LoadLocalManager loadLocalManager = Instantiate(loadManager).GetComponent<LoadLocalManager>();
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
        public IEnumerator PlaceMultimedia(List<string> loadPaths, List<GameObject> placementObjects, string browsingMode)
        {

            result = Result.OnGoing;
            spawnCoroutinesRunning = 0;

            // Spawning
            for (int i = 0; i < placementObjects.Count; i++)
            {
                TaskCoroutine spawnCoroutine = new TaskCoroutine(GenerateCanvasWebView(loadPaths[i], placementObjects[i], browsingMode));
                spawnCoroutine.Finished += delegate (bool manual) { spawnCoroutinesRunning--; };
                spawnCoroutinesRunning++;

                yield return null;
            }

            while (spawnCoroutinesRunning > 0)
            {
                yield return null;
            }

            result = Result.Success;
        }

        private IEnumerator GenerateCanvasWebView(string loadPath, GameObject placementObject, string browsingMode)
        {
            GameObject canvasPrefab = new GameObject();
            canvasPrefab.hideFlags = HideFlags.HideInHierarchy;
            if (browsingMode == "Online")
            {
                canvasPrefab = webViewCanvasFollowerPrefab;
            }
            else if (browsingMode == "Local")
            {
                canvasPrefab = webViewCanvasPrefab;
            }

            GameObject canvasHolder = Instantiate(canvasPrefab, placementObject.transform.position, placementObject.transform.rotation, placementObject.transform);
            Canvas canvasHolderCanvas = canvasHolder.GetComponent<Canvas>();
            canvasHolderCanvas.worldCamera = Camera.main;

            GameObject canvas = Instantiate(webViewPrefab);
            canvas.transform.SetParent(canvasHolder.transform);
            CanvasWebViewPrefab canvasWebView = canvas.GetComponent<CanvasWebViewPrefab>();
            RectTransform rectTransform = canvasWebView.transform as RectTransform;
            rectTransform.anchoredPosition3D = Vector3.zero;
            rectTransform.offsetMin = Vector2.zero;
            rectTransform.offsetMax = Vector2.zero;
            canvas.transform.localScale = Vector3.one;
            yield return StartCoroutine(canvasWebView.WaitUntilInitialized().AsIEnumerator());

            int failedTimes = 0;
            int finishedTimes = 0;
            canvasWebView.WebView.LoadProgressChanged += (sender, eventArgs) => {
                if (eventArgs.Type == ProgressChangeType.Finished)
                {
                    finishedTimes++;
                }

                if (eventArgs.Type == ProgressChangeType.Failed)
                {
                    failedTimes++;
                }
            };

            string url = "";
            if (browsingMode == "Local")
            {
                url = loadPath.Replace(@"\", "/");
                url = url.Replace(" ", "%20");
                url = @"file://" + url;

                string extension = Path.GetExtension(loadPath);
                // FIX THIS, SO IT DOESNT USE EXTENSIONS WITH HELP OF THE DEV
                if (extension == ".mp3" ||
                    extension == ".ogg" ||
                    extension == ".wav" ||
                    extension == ".webm" ||
                    extension == ".oga" ||
                    extension == ".ogv")
                {
                    while (failedTimes != 2)
                    {
                        yield return null;
                    }
                }
                else
                {
                    while (finishedTimes != 1)
                    {
                        yield return null;
                    }
                }

                yield return StartCoroutine(PauseWebView(canvasWebView).AsIEnumerator());
            }
            else if (browsingMode == "Online")
            {
                url = loadPath;
            }

            canvasWebView.WebView.LoadUrl(url);
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
