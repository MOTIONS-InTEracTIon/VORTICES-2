using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using UnityEngine;

using UnityEngine.UI;

namespace Vortices
{
    enum MainMenuId
    {
        // Change this when order is changed or when new submenus are added
        Main = 0,
        Session = 1,
        CategorySelection = 2,
        Options = 3,
        About = 4
    }

    public class MainMenuPanel : MonoBehaviour
    {
        // Panel UI Components
        [SerializeField] public List<GameObject> optionScreenUiComponents;
        [SerializeField] public List<Toggle> panelToggles;

        // Panel Properties
        public int actualComponentId { get; set; }

        // Other
        Color normalColor = new Color(0.2956568f, 0.3553756f, 0.4150943f, 1.0f);
        Color disabledColor = Color.black;

        // Settings
        public int currentEnvironmentId;

        // Coroutine
        private bool isChangePanelRunning;

        // Auxiliary References
        private SceneTransitionManager transitionManager;
        private SessionManager sessionManager;


        #region User Input

        private void OnEnable()
        {
            sessionManager = GameObject.Find("SessionManager").GetComponent<SessionManager>();
            transitionManager = GameObject.Find("TransitionManager").GetComponent<SceneTransitionManager>();
        }

        // UI Components will change according to user configurations one by one
        public void ChangeVisibleComponent(int componentId)
        {
            StartCoroutine(ChangeComponent(componentId));
        }
        private IEnumerator ChangeComponent(int componentId)
        {
            // FadeOut actual component
            FadeUI actualComponentFader = optionScreenUiComponents[actualComponentId].GetComponent<FadeUI>();
            yield return StartCoroutine(actualComponentFader.FadeOut());
            // Disable actual component
            optionScreenUiComponents[actualComponentId].SetActive(false);
            // Enable new component
            optionScreenUiComponents[componentId].SetActive(true);
            actualComponentId = componentId;
            // FadeIn new component
            FadeUI newComponentFader = optionScreenUiComponents[componentId].GetComponent<FadeUI>();
            yield return StartCoroutine(newComponentFader.FadeIn());
        }

        // Changes to panel based on environment selection
        public void ChangePanelEnvironment()
        {
            ChangeVisibleComponent(5 + currentEnvironmentId);
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

            if(optionScreenUiComponents.Count > 5)
            {
                bool environmentStatus = optionScreenUiComponents[5 + currentEnvironmentId].activeInHierarchy;

                // If a configuration panel was running, it has to be resetted
                if (environmentStatus)
                {
                    SpawnPanel environmentPanel = optionScreenUiComponents[5 + currentEnvironmentId].GetComponent<SpawnPanel>();
                    environmentPanel.RestartPanel();
                }
            }

            // Change component using toggle parent name
            if (toggle.transform.parent.name == "Start Toggle")
            {
                yield return StartCoroutine(ChangeComponent((int) MainMenuId.Session));
            }
            else if (toggle.transform.parent.name == "Options Toggle")
            {
                yield return StartCoroutine(ChangeComponent((int)MainMenuId.Options));
            }
            else if (toggle.transform.parent.name == "About Toggle")
            {
                yield return StartCoroutine(ChangeComponent((int)MainMenuId.About));
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

        #endregion

        #region Data Operations

        public void CloseAplication() 
        { 
            StartCoroutine(CloseApplicationCoroutine());
        }
        public IEnumerator CloseApplicationCoroutine()
        {
            yield return StartCoroutine(transitionManager.FadeScreenOut());
            #if UNITY_EDITOR
                UnityEditor.EditorApplication.isPlaying = false;
            #endif
                Application.Quit();
        }


        #endregion
    }
}

