using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using System.IO;
using System.Linq;
using Unity.VisualScripting;

namespace Vortices
{ 
    // This class will load the categories of every element in a SpawnBase
    public class ElementCategoryController : MonoBehaviour
    {
        // Data
        private List<SessionElementCategory> allSessionElementCategory; // All sessions
        public List<ElementCategory> elementCategoriesList; // For this session
        public List<Element> elementGameObjects;

        // Settings
        private string sessionName { get; set; }
        private int userId { get; set; }

        // Auxiliary references
        private SessionManager sessionManager;

        private void Start()
        {
            sessionManager = GameObject.Find("SessionManager").GetComponent<SessionManager>();
        }

        public void Initialize()
        {
            allSessionElementCategory = new List<SessionElementCategory>(); // All sessions
            elementCategoriesList = new List<ElementCategory>();

            this.sessionName = sessionManager.sessionName;
            this.userId = sessionManager.userId;

            LoadAllSessionElementCategories();
        }

        #region Data Operations

        public ElementCategory GetSelectedCategories(string url)
        {
            ElementCategory elementCategory = elementCategoriesList.FirstOrDefault<ElementCategory>(elementCategories => elementCategories.elementUrl == url);
            if(elementCategory != null)
            {
                return elementCategory;
            }
            else
            {
                elementCategory = new ElementCategory();
                elementCategory.elementCategories = new List<string>();
                elementCategory.elementUrl = url;
                UpdateElementCategoriesList(url, elementCategory);
                return elementCategory;
            }
        }

        public void UpdateUICategories()
        {
            // Get all elements in scene
            elementGameObjects = transform.GetComponentsInChildren<Element>().ToList();
            foreach(Element element in elementGameObjects)
            {
                ElementCategory elementCategory = GetSelectedCategories(element.url);
                List<string> selectedCategories = elementCategory.elementCategories;
                // Get their selected categories
                // Set all categories to false
                foreach (UIElementCategory category in element.UIElementCategories)
                {
                    category.changeSelection = false;
                    category.SetToggle(false);
                    category.changeSelection = true;
                }
                // Set found ones to true
                foreach (string category in selectedCategories)
                {
                    UIElementCategory categoryToSelect = element.UIElementCategories.FirstOrDefault<UIElementCategory>(searchCategory => searchCategory.categoryName == category);

                    if (categoryToSelect != null)
                    {
                        categoryToSelect.changeSelection = false;
                        categoryToSelect.SetToggle(true);
                        categoryToSelect.changeSelection = true;
                    }
                }
            } 
        }

        public void UpdateElementCategoriesList(string url, ElementCategory updatedElementCategory)
        {
            Debug.Log("Im updating");
            ElementCategory oldElementCategory = elementCategoriesList.FirstOrDefault<ElementCategory>(elementCategories => elementCategories.elementUrl == url);
            if(oldElementCategory != null)
            {
                int oldElementCategoryIndex = elementCategoriesList.IndexOf(oldElementCategory);
                elementCategoriesList[oldElementCategoryIndex] = updatedElementCategory; // New elementCategoriesList
            }
            else
            {
                elementCategoriesList.Add(updatedElementCategory);
            }

            UpdateSessionCategoryList(elementCategoriesList);
        }

        private void UpdateSessionCategoryList(List<ElementCategory> updatedElementCategoryList)
        {
            SessionElementCategory oldSessionElementCategory = allSessionElementCategory.FirstOrDefault<SessionElementCategory>(session => session.sessionName == this.sessionName && session.userId == this.userId);
            if(oldSessionElementCategory != null)
            {
                int oldSessionElementCategoryIndex = allSessionElementCategory.IndexOf(oldSessionElementCategory);
                allSessionElementCategory[oldSessionElementCategoryIndex].elementCategoriesList = updatedElementCategoryList;
            }
            allSessionElementCategory = allSessionElementCategory.OrderBy(session => session.sessionName).ThenBy(session => session.userId).ToList();

            SaveAllSessionElementCategories();
        }

        #endregion

        #region Persistence

        // All sessions data will be saved and loaded from a file in persistent data folder, also will have a function to save as a csv in the program's folder but as separate files in different folders
        // with structure /Session/userId/categories.csv
        public void SaveAllSessionElementCategories()
        {
            ElementCategorySaveData newElementCategorySaveData = new ElementCategorySaveData();
            newElementCategorySaveData.allSessionElementCategory = allSessionElementCategory;

            string json = JsonUtility.ToJson(newElementCategorySaveData);

            File.WriteAllText(Application.persistentDataPath + "/Session element categories.json", json);
            SaveSessionElementCategoriesToRootFolder();
        }

        public void LoadAllSessionElementCategories()
        {
            string path = Application.persistentDataPath + "/Session element categories.json";
            if (File.Exists(path))
            {
                string json = File.ReadAllText(path);

                // Loads all session data to eventually edit
                allSessionElementCategory = JsonUtility.FromJson<ElementCategorySaveData>(json).allSessionElementCategory;

                // Loads data relevant to session to use in the program
                SessionElementCategory thisSessionElementCategory = allSessionElementCategory.FirstOrDefault<SessionElementCategory>(session => session.sessionName == this.sessionName && session.userId == this.userId);
                if(thisSessionElementCategory != null)
                {
                    elementCategoriesList = thisSessionElementCategory.elementCategoriesList;
                    SaveAllSessionElementCategories();
                }
                // No data found, create one
                else
                {
                    SessionElementCategory newSessionElementCategory = new SessionElementCategory();
                    newSessionElementCategory.sessionName = this.sessionName;
                    newSessionElementCategory.userId = this.userId;
                    newSessionElementCategory.elementCategoriesList = new List<ElementCategory>();

                    allSessionElementCategory.Add(newSessionElementCategory);
                    SaveAllSessionElementCategories();
                }
                
            }
            else
            {
                // If there is no file we create one and apply an empty all Session Element Categories for it to be filled
                SessionElementCategory newSessionElementCategory = new SessionElementCategory();
                newSessionElementCategory.sessionName = this.sessionName;
                newSessionElementCategory.userId = this.userId;
                newSessionElementCategory.elementCategoriesList = new List<ElementCategory>();

                allSessionElementCategory.Add(newSessionElementCategory);
                SaveAllSessionElementCategories();
            }
        }

        public void SaveSessionElementCategoriesToRootFolder()
        {
            string filename = Path.Combine(Application.dataPath + "/Results");
            // File path depends on session name and user Id
            filename = Path.Combine(filename, sessionName);
            filename = Path.Combine(filename, userId.ToString());

            if (!Directory.Exists(filename))
            {
                Directory.CreateDirectory(filename);
            }

            filename = Path.Combine(filename, "Session Element Categories.csv");

            TextWriter tw = new StreamWriter(filename, false);
            tw.WriteLine("Url;Categories");
            tw.Close();

            tw= new StreamWriter(filename, true);

            for(int i = 0; i < elementCategoriesList.Count; i++)
            {
                tw.Write(elementCategoriesList[i].elementUrl + ";");
                for(int j = 0; j < elementCategoriesList[i].elementCategories.Count; j++)
                {
                    if(!(j == elementCategoriesList[i].elementCategories.Count - 1))
                    {
                        tw.Write(elementCategoriesList[i].elementCategories[j] + ";");
                    }
                    else
                    {
                        tw.Write(elementCategoriesList[i].elementCategories[j]);
                    }
                }
                tw.WriteLine();
            }
            tw.Close();
        }

        #endregion
    }

    #region Persistance classes

    // Deals with all the element categories from all sessions and user Ids, it has to be filtered into the correct session and user Id for use
    [System.Serializable]
    public class ElementCategorySaveData
    {
        public List<SessionElementCategory> allSessionElementCategory;
    }

    [System.Serializable]
    public class SessionElementCategory
    {
        public string sessionName;
        public int userId;
        public List<ElementCategory> elementCategoriesList;  // Saves a list of every element categories
    }

    [System.Serializable]
    public class ElementCategory
    {
        public string elementUrl; // This connects a file or a online url to its categories (If the name of file or page changes, its categories are lost with it)
        public List<string> elementCategories;
    }

    #endregion
}

