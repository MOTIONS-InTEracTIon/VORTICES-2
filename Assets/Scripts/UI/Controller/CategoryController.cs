using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using System.IO;
using System.Linq;

namespace Vortices
{
    public class CategoryController : MonoBehaviour
    {
        // This class will load the categories of every session to be chosen
        // Data
        private List<SessionCategory> allSessionCategory; // All sessions
        public List<string> categoriesList; // For this session
        public List<string> selectedCategoriesList;
        public CategorySelector categorySelector;

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
            allSessionCategory = new List<SessionCategory>(); // All sessions
            categoriesList = new List<string>();
            selectedCategoriesList = new List<string>();

            this.sessionName = sessionManager.sessionName;
            this.userId = sessionManager.userId;

            LoadAllSessionCategories();
            categorySelector.Initialize();
        }

        #region Data Operations

        public List<string> GetCategories()
        {
            List<string> categories = categoriesList;
            if (categories != null)
            {
                return categories;
            }
            else
            {
                categories = new List<string>();
                UpdateSessionCategoriesList(categories);
                return categories;
            }
        }

        public List<string> GetSelectedCategories()
        {
            List<string> selectedcategories = selectedCategoriesList;
            if (selectedcategories != null)
            {
                return selectedcategories;
            }
            else
            {
                selectedcategories = new List<string>();
                UpdateSessionSelectedCategoriesList(selectedcategories);
                return selectedcategories;
            }
        }

        public void UpdateCategoriesList(List<string> updatedCategories)
        {
            categoriesList = updatedCategories;

            UpdateSessionCategoriesList(updatedCategories);
        }

        public void UpdateSelectedCategoriesList(List<string> updatedSelectedCategories)
        {
            selectedCategoriesList = updatedSelectedCategories;

            UpdateSessionSelectedCategoriesList(updatedSelectedCategories);
        }

        private void UpdateSessionCategoriesList(List<string> updatedCategoriesList)
        {
            SessionCategory oldSessionCategory = allSessionCategory.FirstOrDefault<SessionCategory>(session => session.sessionName == this.sessionName && session.userId == this.userId);
            if (oldSessionCategory != null)
            {
                int oldSessionCategoryIndex = allSessionCategory.IndexOf(oldSessionCategory);
                allSessionCategory[oldSessionCategoryIndex].categoriesList = updatedCategoriesList;
            }
            allSessionCategory = allSessionCategory.OrderBy(session => session.sessionName).ThenBy(session => session.userId).ToList();

            SaveAllSessionCategories();
        }

        private void UpdateSessionSelectedCategoriesList(List<string> updatedSelectedCategoriesList)
        {
            SessionCategory oldSessionCategory = allSessionCategory.FirstOrDefault<SessionCategory>(session => session.sessionName == this.sessionName && session.userId == this.userId);
            if (oldSessionCategory != null)
            {
                int oldSessionCategoryIndex = allSessionCategory.IndexOf(oldSessionCategory);
                allSessionCategory[oldSessionCategoryIndex].selectedCategoriesList = updatedSelectedCategoriesList;
            }
            allSessionCategory = allSessionCategory.OrderBy(session => session.sessionName).ThenBy(session => session.userId).ToList();

            SaveAllSessionCategories();
        }

        #endregion

        #region Persistence

        // All sessions category data will be saved and loaded from a file in persistent data folder
        public void SaveAllSessionCategories()
        {
            CategorySaveData newCategorySaveData = new CategorySaveData();
            newCategorySaveData.allSessionCategory = allSessionCategory;

            string json = JsonUtility.ToJson(newCategorySaveData);

            File.WriteAllText(Application.persistentDataPath + "/Session categories.json", json);
            SaveSessionCategoriesToRootFolder();
        }

        public void LoadAllSessionCategories()
        {
            string path = Application.persistentDataPath + "/Session categories.json";
            if (File.Exists(path))
            {
                string json = File.ReadAllText(path);

                // Loads all session data to eventually edit
                allSessionCategory = JsonUtility.FromJson<CategorySaveData>(json).allSessionCategory;

                // Loads data relevant to session to use in the program
                SessionCategory thisSessionCategory = allSessionCategory.FirstOrDefault<SessionCategory>(session => session.sessionName == this.sessionName);
                if (thisSessionCategory != null)
                {
                    categoriesList = thisSessionCategory.categoriesList;
                    selectedCategoriesList = thisSessionCategory.selectedCategoriesList;
                    SaveAllSessionCategories();
                }
                // No data found, create one
                else
                {
                    SessionCategory newSessionCategory = new SessionCategory();
                    newSessionCategory.sessionName = this.sessionName;
                    newSessionCategory.userId = this.userId;
                    newSessionCategory.categoriesList = new List<string>();
                    newSessionCategory.selectedCategoriesList = new List<string>();

                    allSessionCategory.Add(newSessionCategory);
                    SaveAllSessionCategories();
                }

            }
            else
            {
                // If there is no file we create one and apply an empty all Session categories for it to be filled
                SessionCategory newSessionCategory = new SessionCategory();
                newSessionCategory.sessionName = this.sessionName;
                newSessionCategory.userId = this.userId;
                newSessionCategory.categoriesList = new List<string>();
                newSessionCategory.selectedCategoriesList = new List<string>();

                allSessionCategory.Add(newSessionCategory);
                SaveAllSessionCategories();
            }
        }

        public void SaveSessionCategoriesToRootFolder()
        {
            string filename = Path.Combine(Application.dataPath + "/Results");
            // File path depends on session name and user Id
            filename = Path.Combine(filename, sessionName);

            if (!Directory.Exists(filename))
            {
                Directory.CreateDirectory(filename);
            }

            filename = Path.Combine(filename, "Session Categories.csv");

            TextWriter tw = new StreamWriter(filename, false);
            tw.WriteLine("Categories");
            tw.Close();

            tw = new StreamWriter(filename, true);

            for (int i = 0; i < categoriesList.Count; i++)
            {
                if(!(i == categoriesList.Count - 1))
                {
                    tw.Write(categoriesList[i] + ";");
                }
                else
                {
                    tw.Write(categoriesList[i]);
                }
            }
            tw.WriteLine();
            tw.WriteLine("Selected Categories");
            for (int i = 0; i < selectedCategoriesList.Count; i++)
            {
                if(!(i == selectedCategoriesList.Count - 1))
                {
                    tw.Write(selectedCategoriesList[i] + ";");
                }
                else
                {
                    tw.Write(selectedCategoriesList[i]);
                }

            }
            tw.WriteLine();
            tw.Close();
        }

        #endregion


        #region Persistance classes

        // Deals with all the  categories from all sessions and user Ids, it has to be filtered into the correct session and user Id for use
        [System.Serializable]
        public class CategorySaveData
        {
            public List<SessionCategory> allSessionCategory;
        }

        [System.Serializable]
        public class SessionCategory
        {
            public string sessionName;
            public int userId;
            public List<string> categoriesList;  // Saves a list of categories to be selected
            public List<string> selectedCategoriesList;
        }

        #endregion
    }
}






