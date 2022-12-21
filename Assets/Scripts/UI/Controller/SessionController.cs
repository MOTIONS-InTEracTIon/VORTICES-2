using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using System.IO;
using System.Linq;
using UnityEngine.UI;
using TMPro;

namespace Vortices
{ 
    public class SessionController : MonoBehaviour
    {
        // Other references
        [SerializeField] private MainMenuPanel mainMenuPanel;
        [SerializeField] private GameObject scrollviewContent;
        [SerializeField] private TextInputField sessionAddInputField;
        [SerializeField] private TextInputField userIdInputField;
        [SerializeField] private GameObject UISessionPrefab;
        [SerializeField] private Button continueButton;
        [SerializeField] private SessionManager sessionManager;

        // Data
        public List<string> sessions;
        private List<UISession> UISessions;
        public string selectedSession;
        public int selectedUserId;
        public string selectedEnvironment;


        private void OnEnable()
        {
            UnlockContinueButton();
        }

        public void Initialize()
        {
            selectedSession = "";
            sessions = new List<string>();
            UISessions = new List<UISession>();

            sessionManager = GameObject.Find("SessionManager").GetComponent<SessionManager>();

            selectedUserId = -1;
            selectedEnvironment = "";
            // When initialized will try to load sessions, will create a new session list otherwise
            LoadSessions();
            // Categories will be added to UI Components
            UpdateSessions();
        }

        #region Data Operation;

        // Session configuration
        public void AddSession()
        {
            string sessionName = sessionAddInputField.GetData();
            // Add to UI component
            AddSessionToScrollView(sessionName);
            // Save all sessions to file
            SaveSessions();
        }

        public void RemoveSession(UISession session)
        {
            // Remove from UI component
            RemoveSessionFromScrollView(session);
            // Save all sessions to file
            SaveSessions();
        }

        private void AddSessionToScrollView(string sessionName)
        {
            CreateSession(sessionName, true);
            // Updates rows
            UpdateSessions();
        }

        private void RemoveSessionFromScrollView(UISession session)
        {
            // Searches the UIComponents for session position
            string sessionName = session.sessionName;

            // Removes from list
            sessions.Remove(sessionName);
            if (selectedSession == sessionName)
            {
                selectedSession = "";
            }
            // Destroys said Component
            session.DestroySession();
            // Destroys UI Session
            UISessions.Remove(session);
            // Updates rows
            UpdateSessions();
        }

        private void UpdateSessions()
        {
            // Clear past UI Categories
            foreach (Transform child in scrollviewContent.transform)
            {
                Destroy(child.gameObject);
            }


            // If UISessions is empty this means we create new objects to hold the sessions
            if (UISessions.Count == 0)
            {
                for (int i = 0; i < sessions.Count; i++)
                { 
                    CreateSession(sessions[i], false);
                }
            }
            // If UISessions is not empty it means we can reuse the ui sessions
            else
            {
                sessions = sessions.OrderBy(session => session).ToList();
                UISessions = UISessions.OrderBy(session => session.sessionName).ToList();
                for (int i = 0; i < UISessions.Count; i++)
                {
                    UISessions[i].transform.SetParent(scrollviewContent.transform);
                }
            }
        }

        private void CreateSession(string sessionName, bool addToList)
        {

            if (sessionName != "")
            {
                //Filters if session should be created by the rules specified in this function
                string result = "";

                if (addToList && sessionName != "")
                {
                    result = FilterSession(sessionName);
                }
                else
                {
                    result = "OK";
                }

                if (result == "OK")
                {
                    UISession newSession = Instantiate(UISessionPrefab, scrollviewContent.transform).GetComponent<UISession>();
                    // Initialize
                    newSession.Init(sessionName, this);

                    // Add session to session list (If its loaded, you dont add it again)
                    if (addToList)
                    {
                        sessions.Add(newSession.sessionName);
                    }
                    // Add gameobject to list for easy access
                    UISessions.Add(newSession);

                    // Sometimes the UI elements deactivate, activate if so
                    LayoutElement layoutElement = newSession.GetComponent<LayoutElement>();
                    if (!layoutElement.isActiveAndEnabled)
                    {
                        layoutElement.gameObject.SetActive(true);
                    }
                }
                else if (result == "Same")
                {
                    sessionAddInputField.SetText("");
                    sessionAddInputField.placeholder.text = "Session already exists.";
                }
            }
        }

        private string FilterSession(string sessionName)
        {
            // Check if session has been already added
            string newName = sessionName.ToLower();
            char[] a = newName.ToCharArray();
            a[0] = char.ToUpper(a[0]);
            newName = new string(a);

            if (sessions.Contains(newName))
            {
                return "Same";
            }

            return "OK";
        }

        // Id Configuration
        public void SetUserId()
        {
            int userId = userIdInputField.GetDataInt();
            if (userId > -1)
            {
                this.selectedUserId = userId;
            }

        }

        // Environment Configuration

        public void SetEnvironment(Toggle environmentToggle)
        {
            string environmentName = environmentToggle.transform.Find("Environment Name").GetComponent<TextMeshProUGUI>().text;

            // Add other environments when created
            if (environmentName == "Circular")
            {
                selectedEnvironment = environmentName;
            }
            else if (environmentName == "Museum")
            {
                selectedEnvironment = environmentName;
            }
        }

        // All Configuration
        public void UnlockContinueButton()
        {
            // Only works with session but user Id and environment has to be selected THIS
            if (selectedSession != "" && selectedUserId > -1 && selectedEnvironment != "")
            {
                continueButton.interactable = true;
            }
            else
            {
                continueButton.interactable = false;
            }
        }

        public void GoToCategoryConfig()
        {
            sessionManager.sessionName = selectedSession;
            sessionManager.userId = selectedUserId;
            sessionManager.environmentName = selectedEnvironment;
            sessionManager.categoryController.Initialize();
            mainMenuPanel.ChangeVisibleComponent((int)MainMenuId.CategorySelection);
        }


        #endregion

        #region Persistence

        // Sessions will be saved and loaded from a file in persistent data folder
        public void SaveSessions()
        {
            SessionSaveData newSessionSaveData = new SessionSaveData();
            newSessionSaveData.sessions = sessions;

            string json = JsonUtility.ToJson(newSessionSaveData);

            File.WriteAllText(Application.persistentDataPath + "/Sessions.json", json);
        }

        public void LoadSessions()
        {
            string path = Application.persistentDataPath + "/Sessions.json";
            if (File.Exists(path))
            {
                string json = File.ReadAllText(path);

                sessions = JsonUtility.FromJson<SessionSaveData>(json).sessions;
            }
            else
            {
                // If there is no file we create one 
                List<string> sessions = new List<string>();

                this.sessions = sessions;
                SaveSessions();
            }
        }

        #endregion

    }

    #region Persistance classes

    [System.Serializable]
    public class SessionSaveData
    {
        public List<string> sessions;
    }

    #endregion
}

