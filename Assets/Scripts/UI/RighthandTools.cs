using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

using System.Linq;

namespace Vortices
{
    enum Tools
    {
        // Change this when order is changed or when new submenus are added
        Base = 0,   
        CategorizeEmpty = 1,
        Categorize = 2,
        Sort = 3
    }

    public class RighthandTools : MonoBehaviour
    {
        // Other references
        [SerializeField] private GameObject UIElementCategoryPrefab;
        [SerializeField] private GameObject categorySelectorContent;


        // Panel UI Components
        [SerializeField] public List<GameObject> toolsUiComponents;
        [SerializeField] public List<Toggle> panelToggles;

        public List<UIElementCategory> UIElementCategories;


        // Panel Properties
        public int actualComponentId { get; set; }

        // Selection
        public Element actualSelectedElement;
        // Selection Data
        public ElementCategory elementCategory; // This element categories object
        public List<string> elementSelectedCategories;
        public bool hadElement;


        // Other
        Color normalColor = new Color(0.2956568f, 0.3553756f, 0.4150943f, 1.0f);
        Color disabledColor = Color.black;

        // Coroutine
        private bool isChangePanelRunning;
        private bool isUpdateRunning;

        // Auxiliary References
        private SessionManager sessionManager;
        private CategoryController categoryController;
        private ElementCategoryController elementCategoryController;

        private void Update()
        {
            if (hadElement && actualSelectedElement == null && actualComponentId == (int)Tools.Categorize)
            {
                StartCoroutine(ChangePanelSelectedCoroutine());
            }
        }

        // Called by Session Manager
        public void Initialize()
        {
            // Canvas has to have main camera
            Canvas canvas = GetComponent<Canvas>();
            canvas.worldCamera = Camera.main;

            sessionManager = GameObject.Find("SessionManager").GetComponent<SessionManager>();
            categoryController = sessionManager.categoryController;
            elementCategoryController = sessionManager.elementCategoryController;

            // Make visible
            Fade toolsFader = GetComponent<Fade>();
            StartCoroutine(toolsFader.FadeInCoroutine());
        }

        #region User Input

        // UI Components will change according to user configurations one by one
        public void ChangeVisibleComponent(int componentId)
        {
            StartCoroutine(ChangeComponent(componentId));
        }

        private IEnumerator ChangeComponent(int componentId)
        {
            // FadeOut actual component
            FadeUI actualComponentFader = toolsUiComponents[actualComponentId].GetComponent<FadeUI>();
            yield return StartCoroutine(actualComponentFader.FadeOut());
            // Disable actual component
            toolsUiComponents[actualComponentId].SetActive(false);
            // Enable new component
            toolsUiComponents[componentId].SetActive(true);
            actualComponentId = componentId;
            // FadeIn new component
            FadeUI newComponentFader = toolsUiComponents[componentId].GetComponent<FadeUI>();
            yield return StartCoroutine(newComponentFader.FadeIn());
        }

        public void ChangePanelToggle(Toggle toggle)
        {
            if (!isChangePanelRunning)
            {
                StartCoroutine(ChangePanelToggleCoroutine(toggle));
            }
        }

        public IEnumerator ChangePanelToggleCoroutine(Toggle toggle)
        {
            isChangePanelRunning = true;

            // Turn all toggles uninteractable with color normal except the one thats pressed which will have color disabled
            foreach (Toggle panelToggle in panelToggles)
            {
                if (!(panelToggle == toggle))
                {
                    // They have to have color disabled normal
                    ColorBlock disabledNormal = toggle.colors;
                    disabledNormal.disabledColor = normalColor;
                    panelToggle.colors = disabledNormal;

                }

                panelToggle.interactable = false;
            }

            // Change component using toggle parent name
            if (toggle.transform.parent.name == "Categorize Toggle" && actualSelectedElement != null)
            {
                yield return StartCoroutine(ChangeComponent((int)Tools.Categorize));
            }
            else if (toggle.transform.parent.name == "Categorize Toggle" && actualSelectedElement == null)
            {
                yield return StartCoroutine(ChangeComponent((int)Tools.CategorizeEmpty));
            }
            else if (toggle.transform.parent.name == "Sort Toggle")
            {
                yield return StartCoroutine(ChangeComponent((int)Tools.Sort));
            }

            // Turn all toggles interactable with color normal except the one that was pressed
            foreach (Toggle panelToggle in panelToggles)
            {
                if (!(panelToggle == toggle))
                {
                    // They have to have color disabled normal
                    ColorBlock disabledBlack = panelToggle.colors;
                    disabledBlack.disabledColor = disabledColor;
                    panelToggle.colors = disabledBlack;
                    panelToggle.interactable = true;
                }
            }

            isChangePanelRunning = false;
        }

        public IEnumerator ChangePanelSelectedCoroutine()
        {
            bool categorizeStatus = toolsUiComponents[(int)Tools.Categorize].activeInHierarchy;
            bool categorizeEmptyStatus = toolsUiComponents[(int)Tools.CategorizeEmpty].activeInHierarchy;
            if (categorizeStatus || categorizeEmptyStatus)
            {
                isChangePanelRunning = true;

                // Turn all toggles uninteractable with color normal
                foreach (Toggle panelToggle in panelToggles)
                {

                    // They have to have color disabled normal
                    ColorBlock disabledNormal = panelToggle.colors;
                    disabledNormal.disabledColor = normalColor;
                    panelToggle.colors = disabledNormal;

                    panelToggle.interactable = false;
                }

                // Change component using toggle parent name
                if (actualSelectedElement == null)
                {
                    yield return StartCoroutine(ChangeComponent((int)Tools.CategorizeEmpty));
                }
                else 
                {
                    yield return StartCoroutine(ChangeComponent((int)Tools.Categorize));
                }

                // Turn all toggles interactable with color normal except the one that was pressed
                foreach (Toggle panelToggle in panelToggles)
                {
                    // They have to have color disabled normal
                    ColorBlock disabledBlack = panelToggle.colors;
                    disabledBlack.disabledColor = disabledColor;
                    panelToggle.colors = disabledBlack;
                    panelToggle.interactable = true;
                }

                isChangePanelRunning = false;
            }
        }

        #endregion

        #region Data Operations

        public void UpdateCategorizeSubMenu(Element selectedElement)
        {
            if (!hadElement)
            {
                hadElement = true;
            }

            if (actualSelectedElement != null && selectedElement.url != actualSelectedElement.url)
            {
                actualSelectedElement.selected = false;

                Renderer selectionBoxRenderer = actualSelectedElement.handInteractor.GetComponent<Renderer>();
                selectionBoxRenderer.material.color = Color.yellow;
                Color selectionRendererColor = selectionBoxRenderer.material.color;
                // If out of hover it becomes invisible
                selectionBoxRenderer.material.color = new Color(selectionRendererColor.r,
                                                                selectionRendererColor.g,
                                                                selectionRendererColor.b, 0f);

                if (sessionManager.environmentName == "Museum")
                {
                    Renderer frameRenderer = actualSelectedElement.transform.parent.GetComponent<Renderer>();
                    Color frameRendererColor = frameRenderer.material.color;
                    frameRenderer.material.color = new Color(selectionRendererColor.r,
                                                            selectionRendererColor.g,
                                                            selectionRendererColor.b, 1f);
                }
            }

            elementSelectedCategories = new List<string>();
            UIElementCategories = new List<UIElementCategory>();
            // Search for the categories to be used in CategoryController and it will add them to categorySelector UI object
            AddUICategories();
            SortUICategories();
            // Element will search for its categories in ElementCategoryController and will apply them to the categorySelector UI object
            GetSelectedCategories(selectedElement);
            actualSelectedElement = selectedElement;
            // Update Categorized
            UpdateCategorized();
            if (actualComponentId != (int)Tools.Categorize)
            {
                StartCoroutine(ChangePanelSelectedCoroutine());
            }
        }

        public void AddUICategories()
        {
            // Clear past UI Categories
            foreach (Transform child in categorySelectorContent.transform)
            {
                Destroy(child.gameObject);
            }

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
        public void GetSelectedCategories(Element selectedElement)
        {
            // Get selectedCategories if there is any, if there isnt, create a blank entry of Element Category
            elementCategory = elementCategoryController.GetSelectedCategories(selectedElement.url);
            elementSelectedCategories = elementCategory.elementCategories;
            // Update UI Elements with said selected categories
            // Set all categories to false
            foreach (UIElementCategory category in UIElementCategories)
            {
                category.changeSelection = false;
                category.SetToggle(false);
                category.changeSelection = true;
            }
            // Set found ones to true
            foreach (string category in elementSelectedCategories)
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
            if (actualSelectedElement != null)
            {
                // Log category addition
                sessionManager.loggingController.LogCategory(actualSelectedElement.url, true, categoryName);

                // Add to selected categories
                elementSelectedCategories.Add(categoryName);
                elementSelectedCategories.Sort();
                // Update Categorized
                UpdateCategorized();
                // Add to element categories
                elementCategory.elementCategories = elementSelectedCategories;
                // Send element categories back to category controller
                elementCategoryController.UpdateElementCategoriesList(actualSelectedElement.url, elementCategory);
            }
        }

        public void RemoveFromSelectedCategories(string categoryName)
        {
            if (actualSelectedElement != null)
            {
                // Log category addition
                sessionManager.loggingController.LogCategory(actualSelectedElement.url, false, categoryName);

                // Remove from selected categories
                elementSelectedCategories.Remove(categoryName);
                elementSelectedCategories.Sort();
                // Update Categorized
                UpdateCategorized();
                // Add to elemen
                // Add to element categories
                elementCategory.elementCategories = elementSelectedCategories;
                // Send element categories back to category controller
                elementCategoryController.UpdateElementCategoriesList(actualSelectedElement.url, elementCategory);
            }
        }

        public void UpdateCategorized()
        {
            if (elementSelectedCategories.Count > 0)
            {
                actualSelectedElement.SetCategorized(true);
            }
            else
            {
                actualSelectedElement.SetCategorized(false);
            }
        }
        #endregion
    }
}

