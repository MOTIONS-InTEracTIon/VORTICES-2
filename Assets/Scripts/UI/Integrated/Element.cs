using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using Vuplex.WebView;

using UnityEngine.InputSystem.UI;
using UnityEngine.UI;
using System.Linq;
using UnityEngine.XR.Interaction.Toolkit;
using TMPro;
using UnityEngine.UIElements;
using static UnityEditor.Rendering.FilterWindow;
using System;

namespace Vortices
{
    public class Element : MonoBehaviour
    {
        // Other references
        [SerializeField] private GameObject dummyPrefab;

        [SerializeField] private GameObject browserControls;
        [SerializeField] private GameObject upperControls;
        [SerializeField] private GameObject webUrl;
        [SerializeField] private GameObject goBack;
        [SerializeField] private GameObject goForward;
        [SerializeField] public GameObject handInteractor;
        [SerializeField] private GameObject categorySelectorUI;
        [SerializeField] private GameObject categorizedYes;
        [SerializeField] private GameObject categorizedNo;
        private IWebView canvasWebView;

        private HandKeyboard keyboardCanvas;
        private CircularSpawnBase circularSpawnBase;
        private SpawnController spawnController;

        private RighthandTools righthandTools;

        // Settings
        public string url;
        private string browsingMode;
        private string displayMode;
        private float selectionTime = 3.0f;
        private bool initialized;
        public bool selected;

        // Coroutine
        private bool toggleComponentRunning;
        public bool selectionCoroutineRunning;

        // Auxiliary references
        private SessionManager sessionManager;

        public void Initialize(string browsingMode, string displayMode, string url, CanvasWebViewPrefab canvas)
        {
            sessionManager = GameObject.FindObjectOfType<SessionManager>();
            righthandTools = GameObject.FindObjectOfType<RighthandTools>();
            keyboardCanvas = GameObject.Find("Keyboard Canvas").GetComponent<HandKeyboard>();
            spawnController = GameObject.FindObjectOfType<SpawnController>();
            canvasWebView = canvas.WebView;
            if(sessionManager.environmentName == "Circular")
            {
                circularSpawnBase = GameObject.Find("Information Object Group").GetComponentInChildren<CircularSpawnBase>();

            }



            browserControls.SetActive(true);
            this.browsingMode = browsingMode;
            this.url = url;
            this.displayMode = displayMode;

            // Set categorized to true or false
            if (sessionManager.elementCategoryController.GetSelectedCategories(url).elementCategories.Count > 0)
            {
                SetCategorized(true);
            }
            else
            {
                SetCategorized(false);
            }

            // Enable Online controls
            if (browsingMode == "Online")
            {
                // Enable browser controls
                upperControls.SetActive(true);
                // Subscribe to keyboard event for it to show
                TMP_InputField webUrlInputfield = webUrl.transform.Find("InputField").GetComponent<TMP_InputField>();
                webUrlInputfield.onSelect.AddListener(delegate { keyboardCanvas.SetInputField(webUrlInputfield); });

                StartCoroutine(keyboardCanvas.SetKeyboardOn());

                // Add event so it updates categories when navigating
                canvasWebView.UrlChanged += (sender, eventArgs) =>
                {
                    this.url = canvasWebView.Url;
                    sessionManager.loggingController.LogUrlChanged(url);
                    righthandTools.UpdateCategorizeSubMenu(this);
                };
            }
            // Enable Local controls
            else if (browsingMode == "Local")
            {
                
            }

            canvasWebView = GetComponentInChildren<CanvasWebViewPrefab>().WebView;

            Renderer selectionBoxRenderer = handInteractor.GetComponent<Renderer>();
            Color selectionRendererColor = selectionBoxRenderer.material.color;

            selectionBoxRenderer.material.color = new Color(selectionRendererColor.r,
                selectionRendererColor.g,
                selectionRendererColor.b, 0f);

            initialized = true;
        }

        #region Data Operations

        public async void GoBack()
        {
            bool canGoBack = await canvasWebView.CanGoBack();
            if (canGoBack)
            {
                canvasWebView.GoBack();
                url = canvasWebView.Url;
            }
        }

        public void GoForward()
        {
            // Get Url from gameobject
            string finalurl = webUrl.GetComponent<TextInputField>().GetData();
            if (finalurl != canvasWebView.Url)
            {

                canvasWebView.LoadUrl(finalurl);
                url = finalurl;
            }
        }

        public void SetCategorized(bool activate)
        {
            if (activate)
            {
                categorizedNo.SetActive(false);
                categorizedYes.SetActive(true);
            }
            else
            {
                categorizedNo.SetActive(true);
                categorizedYes.SetActive(false);
            }
        }

        #endregion

        #region Selection

        public void HoverElement(bool activate)
        {
            if (sessionManager != null && !spawnController.movingOperationRunning)
            {
                if (activate)
                {
                    if (!selected)
                    {
                        Renderer selectionBoxRenderer = handInteractor.GetComponent<Renderer>();
                        selectionBoxRenderer.material.color = Color.yellow;

                        Color selectionRendererColor = selectionBoxRenderer.material.color;

                        // If out of hover it becomes invisible
                        selectionBoxRenderer.material.color = new Color(selectionRendererColor.r,
                            selectionRendererColor.g,
                            selectionRendererColor.b, 1f);

                        if (sessionManager.environmentName == "Museum")
                        {
                            Renderer frameRenderer = transform.parent.GetComponent<Renderer>();
                            Color frameRendererColor = frameRenderer.material.color;

                            frameRenderer.material.color = new Color(frameRendererColor.r,
                                frameRendererColor.g,
                                frameRendererColor.b, 0f);

                        }
                    }
                    spawnController.elementsHovered++;
                }
                else 
                {
                    if(!selected)
                    {
                        Renderer selectionBoxRenderer = handInteractor.GetComponent<Renderer>();
                        selectionBoxRenderer.material.color = Color.yellow;
                        Color selectionRendererColor = selectionBoxRenderer.material.color;

                        // If out of hover it becomes invisible
                        selectionBoxRenderer.material.color = new Color(selectionRendererColor.r,
                            selectionRendererColor.g,
                            selectionRendererColor.b, 0f);


                        if (sessionManager.environmentName == "Museum")
                        {
                            Renderer frameRenderer = transform.parent.GetComponent<Renderer>();
                            Color frameRendererColor = frameRenderer.material.color;

                            frameRenderer.material.color = new Color(frameRendererColor.r,
                                frameRendererColor.g,
                                frameRendererColor.b, 1f);
                        }
                    }
                    
                    spawnController.elementsHovered--;
                }

            }
        }

        public void SelectElement()
        {
            if (!selectionCoroutineRunning && !selected && spawnController != null && !spawnController.movingOperationRunning)
            {
                selectionCoroutineRunning = true;
                selected = true;
                StartCoroutine(SelectElementCoroutine());
            }
        }

        public IEnumerator SelectElementCoroutine()
        {
            Renderer selectionBoxRenderer = handInteractor.GetComponent<Renderer>();
            selectionBoxRenderer.material.color = Color.green;
            sessionManager.loggingController.LogSelection(url, true);

            righthandTools.UpdateCategorizeSubMenu(this);

            yield return new WaitForSeconds(3.0f);
            selectionCoroutineRunning = false;


        }


        #endregion
    }
}

