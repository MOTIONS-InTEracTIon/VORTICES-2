using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

using TMPro;
using System.Linq;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;


namespace Vortices
{
    public class CategoryController : MonoBehaviour
    {
        // Other references
        [SerializeField] private GameObject scrollviewContent;
        [SerializeField] private TextInputField categoryAddInputField;
        [SerializeField] private Button continueButton;
        [SerializeField] private GameObject horizontalLayoutPrefab;
        [SerializeField] private GameObject UICategoryPrefab;

        // Data
        public List<string> categories;
        public List<string> selectedCategories;
        private List<UICategory> UICategories;

        private GameObject lastHorizontalGroup;



        private void Start()
        {
            selectedCategories = new List<string>();
            UICategories = new List<UICategory>();
            categories = new List<string>();
            // When initialized will try to load categories, will create a new category list otherwise
            LoadCategories();
            // Categories will be added to UI Components
            UpdateCategories();

        }
        #region Data Operation;

        public void AddCategory()
        {
            string categoryName = categoryAddInputField.GetData();
            // Add to UI component
            AddCategoryToScrollView(categoryName);
            // Save all categories to file
            SaveCategories();
        }

        public void RemoveCategory(UICategory category)
        {
            // Remove from UI component
            RemoveCategoryFromScrollView(category);
            // Save all categories to file
            SaveCategories();
        }

        private void UpdateCategories()
        {
            // Identify Old horizontal rows
            foreach (Transform child in scrollviewContent.transform)
            {
                child.name = "Old Row";
            }

            // If UICategories is empty this means we create new objects to hold the categories
            if (UICategories.Count == 0)
            {
                for (int i = 0; i < categories.Count; i++)
                {
                    if (i % 3 == 0)
                    {
                        GameObject newHorizontalRow = Instantiate(horizontalLayoutPrefab, scrollviewContent.transform);
                        lastHorizontalGroup = newHorizontalRow;
                    }

                    CreateCategory(categories[i], lastHorizontalGroup, false);
                }
            }
            // If UICategories is not empty it means we can reuse the ui categories
            else
            {
                categories = categories.OrderBy(category => category).ToList();
                UICategories = UICategories.OrderBy(category => category.categoryName).ToList();
                for (int i = 0; i < UICategories.Count; i++)
                {
                    if (i % 3 == 0)
                    {
                        GameObject newHorizontalRow = Instantiate(horizontalLayoutPrefab, scrollviewContent.transform);
                        lastHorizontalGroup = newHorizontalRow;
                    }

                    UICategories[i].horizontalGroup = lastHorizontalGroup;
                    UICategories[i].transform.SetParent(lastHorizontalGroup.transform);
                }
            }
            // Delete old horizontal Rows

            foreach (Transform child in scrollviewContent.transform)
            {
                if (child.name == "Old Row")
                {
                    Destroy(child.gameObject);
                }
            }
        }

        private void AddCategoryToScrollView(string categoryName)
        {
            // If total number of categories is 0 when mod 3, this means a new row has to be added
            if (categories.Count % 3 == 0)
            {
                GameObject newHorizontalRow = Instantiate(horizontalLayoutPrefab, scrollviewContent.transform);
                lastHorizontalGroup = newHorizontalRow;
            }

            CreateCategory(categoryName, lastHorizontalGroup, true);
            // Updates rows
            UpdateCategories();
        }

        private void RemoveCategoryFromScrollView(UICategory category)
        {
            // Searches the UIComponents for category position
            GameObject horizontalGroup = category.horizontalGroup;
            string categoryName = category.categoryName;

            // Removes from list
            categories.Remove(categoryName);
            if (selectedCategories.Contains(categoryName))
            {
                selectedCategories.Remove(categoryName);
            }
            // Destroys said Component
            category.DestroyCategory();
            // Destroys UI Category
            UICategories.Remove(category);
            // Updates rows
            UpdateCategories();
        }

        private void CreateCategory(string categoryName, GameObject horizontalGroup, bool addToList)
        {
            
            if (categoryName != "")
            {
                //Filters if category should be created by the rules specified in this function
                string result = "";

                if (addToList && categoryName != "")
                {
                    result = FilterCategory(categoryName);
                }
                else
                {
                    result = "OK";
                }

                if (result == "OK")
                {
                    UICategory newCategory = Instantiate(UICategoryPrefab, horizontalGroup.transform).GetComponent<UICategory>();
                    // Initialize
                    newCategory.Init(categoryName, this, horizontalGroup);

                    // Add category to category list (If its loaded, you dont add it again)
                    if (addToList)
                    {
                        categories.Add(newCategory.categoryName);
                    }
                    // Add gameobject to list for easy access
                    UICategories.Add(newCategory);

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
                else if (result == "Same")
                {
                    categoryAddInputField.SetText("");
                    categoryAddInputField.placeholder.text = "Category already exists.";
                }
            }
        }

        private string FilterCategory(string categoryName)
        {
            // Check if category has been already added
            string newName = categoryName.ToLower();
            char[] a = newName.ToCharArray();
            a[0] = char.ToUpper(a[0]);
            newName = new string(a);

            if (categories.Contains(newName))
            {
                return "Same";
            }

            return "OK";
        }

        public void UnlockContinueButton()
        {
            if (selectedCategories.Count > 0)
            {
                continueButton.interactable = true;
            }
            else
            {
                continueButton.interactable = false;
            }
        }

        #endregion

        #region Persistence

        // Categories will be saved and loaded from a file in persistent data folder
        public void SaveCategories()
        {
            CategorySaveData newCategorySaveData = new CategorySaveData();
            newCategorySaveData.categories = categories;

            string json = JsonUtility.ToJson(newCategorySaveData);

            File.WriteAllText(Application.persistentDataPath + "/categories.json", json);
        }

        public void LoadCategories()
        {
            string path = Application.persistentDataPath + "/categories.json";
            if (File.Exists(path))
            {
                string json = File.ReadAllText(path);

                categories = JsonUtility.FromJson<CategorySaveData>(json).categories;
            }
            else
            {
                File.WriteAllText(Application.persistentDataPath + "/categories.json","");
            }
        }

        #endregion

    }

    #region Persistance classes

    [System.Serializable]
    public class CategorySaveData
    {
        public List<string> categories;
    }

    #endregion
}
