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

namespace Vortices
{
    public class Element : MonoBehaviour
    {
        // Other references
        [SerializeField] private GameObject UIElementCategoryPrefab;
        [SerializeField] private GameObject dummyPrefab;

        [SerializeField] private GameObject browserControls;
        [SerializeField] private GameObject upperControls;
        [SerializeField] private GameObject webUrl;
        [SerializeField] private GameObject goBack;
        [SerializeField] private GameObject goForward;
        [SerializeField] private GameObject unselect;
        [SerializeField] private GameObject categorySwitch;
        [SerializeField] private GameObject categorySelectorContent;
        [SerializeField] private GameObject handInteractor;
        [SerializeField] private GameObject categorySelectorUI;
        private IWebView canvasWebView;
        private CategoryController categoryController;
        private ElementCategoryController elementCategoryController;
        private HandKeyboard keyboardCanvas;

        // Data
        public ElementCategory elementCategory; // This element categories object
        public List<string> selectedCategories; // This element selected
        public List<UIElementCategory> UIElementCategories;
        // Selection Data
        private GameObject selectionObject; // Object located in XROrigin
        private GameObject placedTransform;

        // Settings
        public string url;
        private string browsingMode;
        private string displayMode;
        private float selectionTime = 3.0f;

        // Coroutine
        private bool toggleComponentRunning;
        private bool selectionCoroutineRunning;

        // Auxiliary references
        private SessionManager sessionManager;

        public void Initialize(string browsingMode, string displayMode, string url, CanvasWebViewPrefab canvas)
        {
            sessionManager = GameObject.Find("SessionManager").GetComponent<SessionManager>();
            categoryController = sessionManager.categoryController;
            elementCategoryController = sessionManager.elementCategoryController;
            keyboardCanvas = GameObject.Find("Keyboard Canvas").GetComponent<HandKeyboard>();
            canvasWebView = canvas.WebView;
            selectionObject = GameObject.Find("Selector").gameObject;


            browserControls.SetActive(true);
            this.browsingMode = browsingMode;
            this.url = url;
            this.displayMode = displayMode;

            selectedCategories = new List<string>();
            UIElementCategories = new List<UIElementCategory>();
            // Element will search for the categories to be used in CategoryController and it will add them to categorySelector UI object
            AddUICategories();
            SortUICategories();
            // Element will search for its categories in ElementCategoryController and will apply them to the categorySelector UI object
            GetSelectedCategories();

            // Enable Online controls
            if (browsingMode == "Online")
            {
                // Subscribe to keyboard event for it to show
                TMP_InputField webUrlInputfield = webUrl.transform.Find("InputField").GetComponent<TMP_InputField>();
                webUrlInputfield.onSelect.AddListener(delegate { keyboardCanvas.SetInputField(webUrlInputfield); });

                StartCoroutine(keyboardCanvas.SetKeyboardOn());

                // Add event so it updates categories when navigating
                canvasWebView.UrlChanged += (sender, eventArgs) =>
                {
                    this.url = canvasWebView.Url;
                    GetSelectedCategories();
                };
            }
            // Enable Local controls
            else if (browsingMode == "Local")
            {
                
            }
            // Enable both controls
            categorySwitch.SetActive(true);

            canvasWebView = GetComponentInChildren<CanvasWebViewPrefab>().WebView;

        }

        #region Data Operations
        public void AddUICategories()
        {
            // Ask CategoryController for every category to add
            List<string> categories = categoryController.GetCategories();
            foreach (string category in categories)
            {
                // Add to UI component
                AddCategoryToScrollView(category);
            }
        }

        private void AddCategoryToScrollView(string categoryName)
        {
            CreateCategory(categoryName);
            // Updates rows
            SortUICategories();
        }

        private void SortUICategories()
        {
            UIElementCategories = UIElementCategories.OrderBy(category => category.categoryName).ToList();
            for (int i = 0; i < UIElementCategories.Count; i++)
            {
                UIElementCategories[i].transform.SetSiblingIndex(i);
            }
        }

        private void CreateCategory(string categoryName)
        {
            UIElementCategory newCategory = Instantiate(UIElementCategoryPrefab, categorySelectorContent.transform).GetComponent<UIElementCategory>();
            // Initialize
            newCategory.Init(categoryName, this);

            // Add gameobject to list for easy access
            UIElementCategories.Add(newCategory);

            // Sometimes the UI elements deactivate, activate if so
            HorizontalLayoutGroup horizontalLayoutGroup = newCategory.GetComponent<HorizontalLayoutGroup>();
            LayoutElement layoutElement = newCategory.GetComponent<LayoutElement>();
            if (!horizontalLayoutGroup.isActiveAndEnabled)
            {
                horizontalLayoutGroup.gameObject.SetActive(true);
            }
            if (!layoutElement.isActiveAndEnabled)
            {
                layoutElement.gameObject.SetActive(true);
            }
        }

        // Extracts categories from elementCategoryController
        private void GetSelectedCategories()
        {
            // Get selectedCategories if there is any, if there isnt, create a blank entry of Element Category
            elementCategory = elementCategoryController.GetSelectedCategories(url);
            selectedCategories = elementCategory.elementCategories;
            // Update UI Elements with said selected categories
            // Set all categories to false
            foreach (UIElementCategory category in UIElementCategories)
            {
                category.changeSelection = false;
                category.SetToggle(false);
                category.changeSelection = true;
            }
            // Set found ones to true
            foreach (string category in selectedCategories)
            {
                UIElementCategory categoryToSelect = UIElementCategories.FirstOrDefault<UIElementCategory>(searchCategory => searchCategory.categoryName == category);

                if(categoryToSelect != null)
                {
                    categoryToSelect.changeSelection = false;
                    categoryToSelect.SetToggle(true);
                    categoryToSelect.changeSelection = true;
                }
            }
        }

        public void AddToSelectedCategories(string categoryName)
        {
            // Log category addition
            sessionManager.loggingController.LogCategory(url, true, categoryName);

            // Add to selected categories
            selectedCategories.Add(categoryName);
            selectedCategories.Sort();
            // Add to element categories
            elementCategory.elementCategories = selectedCategories;
            // Send element categories back to category controller
            elementCategoryController.UpdateElementCategoriesList(url, elementCategory);
            // Update all UIObjects of this session to reflect this change
            elementCategoryController.UpdateUICategories();
        }

        public void RemoveFromSelectedCategories(string categoryName)
        {
            // Log category addition
            sessionManager.loggingController.LogCategory(url, false, categoryName);

            // Remove from selected categories
            selectedCategories.Remove(categoryName);
            selectedCategories.Sort();
            // Add to element categories
            elementCategory.elementCategories = selectedCategories;
            // Send element categories back to category controller
            elementCategoryController.UpdateElementCategoriesList(url, elementCategory);
            // Update all UIObjects of this session to reflect this change
            elementCategoryController.UpdateUICategories();
        }

        public async void GoBack()
        {
            bool canGoBack = await canvasWebView.CanGoBack();
            if (canGoBack)
            {
                canvasWebView.GoBack();
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
                GetSelectedCategories();
            }
        }

        #endregion

        #region Component Operations

        public void ToggleCategorySelector()
        {
            StartCoroutine(ToggleCategorySelectorCoroutine());
        }

        private IEnumerator ToggleCategorySelectorCoroutine()
        {
            if (!toggleComponentRunning)
            {
                toggleComponentRunning = true;
                CanvasGroup categorySelectorCanvasGroup = categorySelectorUI.GetComponent<CanvasGroup>();
                
                int fadeCoroutinesRunning = 0;
                Fade categorySelectorFader = categorySelectorUI.GetComponent<Fade>();

                if (categorySelectorCanvasGroup.alpha == 0)
                {
                    categorySelectorCanvasGroup.gameObject.SetActive(true);
                    TaskCoroutine fadeCoroutine = new TaskCoroutine(categorySelectorFader.FadeInCoroutine());
                    fadeCoroutine.Finished += delegate (bool manual)
                    {
                        fadeCoroutinesRunning--;
                    };
                    fadeCoroutinesRunning++;

                    while (fadeCoroutinesRunning > 0)
                    {
                        yield return null;
                    }
                }
                else if (categorySelectorCanvasGroup.alpha == 1)
                {
                    TaskCoroutine fadeCoroutine = new TaskCoroutine(categorySelectorFader.FadeOutCoroutine());
                    fadeCoroutine.Finished += delegate (bool manual)
                    {
                        fadeCoroutinesRunning--;
                    };
                    fadeCoroutinesRunning++;

                    while (fadeCoroutinesRunning > 0)
                    {
                        yield return null;
                    }

                    categorySelectorCanvasGroup.gameObject.SetActive(false);
                }

                toggleComponentRunning = false;
            }
        }

        #endregion

        #region Movement

        public void SetAsSelectedElement()
        {
            // Only allow selection when there is no object selected
            if (selectionObject.transform.childCount == 0 && !selectionCoroutineRunning)
            {
                // Log selection
                sessionManager.loggingController.LogSelection(url, true);

                unselect.gameObject.SetActive(true);
                if(browsingMode == "Online")
                {
                    upperControls.gameObject.SetActive(true);
                }

                // Cant move elements while selecting
                if (browsingMode == "Local" || browsingMode == "Online") 
                {
                    List<GameObject> colliderBoxes = GameObject.FindGameObjectsWithTag("External").ToList();
                    foreach (GameObject colliderBox in colliderBoxes)
                    {
                        BoxCollider boxCollider = colliderBox.GetComponent<BoxCollider>();
                        boxCollider.enabled = false;
                    }
                }

                handInteractor.gameObject.SetActive(false);
                StartCoroutine(SetAsSelectedElementCoroutine());
            }
        }

        public void RemoveFromSelectedElement()
        {
            // Only allows deselection if parent is the selector
            if (transform.parent.name == "Selector" && !selectionCoroutineRunning)
            {
                // Log selection
                sessionManager.loggingController.LogSelection(url, false);

                unselect.gameObject.SetActive(false);
                if (browsingMode == "Online")
                {
                    upperControls.gameObject.SetActive(false);
                }

                // Cant move elements while selecting
                if (browsingMode == "Local" || browsingMode == "Online")
                {
                    List<GameObject> colliderBoxes = GameObject.FindGameObjectsWithTag("External").ToList();
                    foreach (GameObject colliderBox in colliderBoxes)
                    {
                        BoxCollider boxCollider = colliderBox.GetComponent<BoxCollider>();
                        boxCollider.enabled = true;
                    }
                }

                handInteractor.gameObject.SetActive(true);
                StartCoroutine(RemoveFromSelectedElementCoroutine());
            }
        }

        private IEnumerator SetAsSelectedElementCoroutine()
        {
            selectionCoroutineRunning = true;
            if(displayMode == "Radial")
            {
                GetComponent<RotateToObject>().enabled = false;
            }
            // Swap element with a dummy so the layouts wont move
            placedTransform = Instantiate(dummyPrefab, transform.position, transform.rotation, transform.parent.transform);
            placedTransform.transform.position = transform.position;
            placedTransform.transform.rotation = transform.rotation;
            placedTransform.transform.localScale = transform.localScale;
            transform.SetParent(selectionObject.transform);
            // Launch Position and Rotation Lerp Task at the same time so it goes from bases to selection object
            int movementCoroutinesRunning = 0;
            TaskCoroutine positionLerpCoroutine = new TaskCoroutine(LerpToSelectedPosition(selectionObject.transform.position));
            positionLerpCoroutine.Finished += delegate (bool manual)
            {
                movementCoroutinesRunning--;
            };
            movementCoroutinesRunning++;
            TaskCoroutine rotationLerpCoroutine = new TaskCoroutine(LerpToSelectedRotation(selectionObject.transform.rotation));
            rotationLerpCoroutine.Finished += delegate (bool manual)
            {
                movementCoroutinesRunning--;
            };
            movementCoroutinesRunning++;
            TaskCoroutine scaleLerpCoroutine = new TaskCoroutine(LerpToSelectedScale(placedTransform.transform.localScale));
            scaleLerpCoroutine.Finished += delegate (bool manual)
            {
                movementCoroutinesRunning--;
            };
            movementCoroutinesRunning++;

            while (movementCoroutinesRunning > 0)
            {
                yield return null;
            }

            selectionCoroutineRunning = false;
        }
 
        private IEnumerator RemoveFromSelectedElementCoroutine()
        {
            selectionCoroutineRunning = true;
            // Set as child of last parent
            transform.SetParent(placedTransform.transform.parent.transform);
            // Launch Position and Rotation Lerp Task at the same time so it goes from selection object to base
            int movementCoroutinesRunning = 0;
            TaskCoroutine positionLerpCoroutine = new TaskCoroutine(LerpToSelectedPosition(placedTransform.transform.position));
            positionLerpCoroutine.Finished += delegate (bool manual)
            {
                movementCoroutinesRunning--;
            };
            movementCoroutinesRunning++;
            TaskCoroutine rotationLerpCoroutine = new TaskCoroutine(LerpToSelectedRotation(placedTransform.transform.rotation));
            rotationLerpCoroutine.Finished += delegate (bool manual)
            {
                movementCoroutinesRunning--;
            };
            movementCoroutinesRunning++;
            TaskCoroutine scaleLerpCoroutine = new TaskCoroutine(LerpToSelectedScale(placedTransform.transform.localScale));
            scaleLerpCoroutine.Finished += delegate (bool manual)
            {
                movementCoroutinesRunning--;
            };
            movementCoroutinesRunning++;

            if (displayMode == "Radial")
            {
                GetComponent<RotateToObject>().enabled = true;
            }

            while (movementCoroutinesRunning > 0)
            {
                yield return null;
            }

            // Swap with dummy and destroy it
            Destroy(placedTransform.gameObject);

            selectionCoroutineRunning = false;
        }

        private IEnumerator LerpToSelectedPosition(Vector3 finalPosition)
        {
            float timeElapsed = 0;
            while (timeElapsed < selectionTime)
            {
                timeElapsed += Time.deltaTime;
                transform.position = Vector3.Lerp(transform.position, finalPosition, timeElapsed / selectionTime);
               
                yield return null;
            }
        }

        private IEnumerator LerpToSelectedRotation(Quaternion finalRotation)
        {
            float timeElapsed = 0;
            while (timeElapsed < selectionTime)
            {
                timeElapsed += Time.deltaTime;
                transform.rotation = Quaternion.Lerp(transform.rotation, finalRotation, timeElapsed / selectionTime);
                
                yield return null;
            }
        }

        private IEnumerator LerpToSelectedScale(Vector3 finalScale)
        {
            float timeElapsed = 0;
            while (timeElapsed < selectionTime)
            {
                timeElapsed += Time.deltaTime;
                transform.localScale = Vector3.Lerp(transform.localScale, finalScale , timeElapsed / selectionTime);

                yield return null;
            }
        }

        #endregion
    }
}

