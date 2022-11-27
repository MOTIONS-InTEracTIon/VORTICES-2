using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using Vuplex.WebView;

using UnityEngine.InputSystem.UI;
using UnityEngine.UI;
using System.Linq;
using UnityEngine.XR.Interaction.Toolkit;

namespace Vortices
{
    public class Element : MonoBehaviour
    {
        // Other references
        [SerializeField] private GameObject UIElementCategoryPrefab;

        [SerializeField] private GameObject browserControls;
        [SerializeField] private GameObject webUrl;
        [SerializeField] private GameObject goBack;
        [SerializeField] private GameObject categorySwitch;
        [SerializeField] private GameObject categorySelector;
        [SerializeField] private GameObject categorySelectorContent;
        private IWebView canvasWebView;
        private CategoryController categoryController;
        private ElementCategoryController elementCategoryController;
        private HandKeyboard keyboardCanvas;

        // Data
        public ElementCategory elementCategory; // This element categories object
        public List<string> selectedCategories; // This element selected
        private List<UIElementCategory> UIElementCategories;

        // Settings
        private string url;
        private string browsingMode;

        // Coroutine
        private bool toggleComponentRunning;

        // Auxiliary references
        private SessionManager sessionManager;

        public async Task Initialize(string browsingMode, string url, CanvasWebViewPrefab canvas)
        {
            sessionManager = GameObject.Find("Session").GetComponent<SessionManager>();
            categoryController = sessionManager.categoryController;
            elementCategoryController = sessionManager.elementCategoryController;
            keyboardCanvas = GameObject.Find("Keyboard Canvas").GetComponent<HandKeyboard>();
            canvasWebView = canvas.WebView;


            browserControls.SetActive(true);
            this.browsingMode = browsingMode;
            this.url = url;

            selectedCategories = new List<string>();
            UIElementCategories = new List<UIElementCategory>();
            // Element will search for the categories to be used in CategoryController and it will add them to categorySelector UI object
            AddUICategories();
            UpdateUICategories();
            // Element will search for its categories in ElementCategoryController and will apply them to the categorySelector UI object
            GetSelectedCategories();
            // Activate UIElementCategory toggles
            for (int i = 0; i < UIElementCategories.Count; i++)
            {
                UIElementCategories[i].initialized = true;
            }

            // Enable Online controls
            if (browsingMode == "Online")
            {
                webUrl.SetActive(true);
                goBack.SetActive(true);

                StartCoroutine(keyboardCanvas.SetKeyboardOn());
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
            foreach (string category in categoryController.selectedCategories)
            {
                // Add to UI component
                AddCategoryToScrollView(category);
            }
        }

        private void AddCategoryToScrollView(string categoryName)
        {
            CreateCategory(categoryName);
            // Updates rows
            UpdateUICategories();
        }

        private void UpdateUICategories()
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
            if (url == "file://C:/Users/Moriyarnn/Desktop/test_images/g_imagen_027.jpg")
            {
                Debug.Log("This");
            }
            // Update UI Elements with said selected categories
            foreach (string category in selectedCategories)
            {
                UIElementCategory categoryToSelect = UIElementCategories.FirstOrDefault<UIElementCategory>(searchCategory => searchCategory.categoryName == category);

                if(categoryToSelect != null)
                {
                    categoryToSelect.SetToggle(true);
                }
            }
        }

        public void AddToSelectedCategories(string categoryName)
        {
            // Add to selected categories
            selectedCategories.Add(categoryName);
            selectedCategories.Sort();
            // Add to element categories
            elementCategory.elementCategories = selectedCategories;
            // Send element categories back to category controller
            elementCategoryController.UpdateElementCategoriesList(url, elementCategory);
        }

        public void RemoveFromSelectedCategories(string categoryName)
        {
            // Remove from selected categories
            selectedCategories.Remove(categoryName);
            selectedCategories.Sort();
            // Add to element categories
            elementCategory.elementCategories = selectedCategories;
            // Send element categories back to category controller
            elementCategoryController.UpdateElementCategoriesList(url, elementCategory);
        }

        public async void GoBack()
        {
            bool canGoBack = await canvasWebView.CanGoBack();
            if (canGoBack)
            {
                canvasWebView.GoBack();
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
                CanvasGroup categorySelectorCanvasGroup = categorySelector.GetComponent<CanvasGroup>();
                
                int fadeCoroutinesRunning = 0;
                Fade categorySelectorFader = categorySelector.GetComponent<Fade>();

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
    }
}

