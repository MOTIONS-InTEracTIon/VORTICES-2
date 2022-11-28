using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Vortices
{
    public abstract class SpawnPanel : MonoBehaviour
    {
        #region Variables and properties
        // Panel UI Components
        [SerializeField] protected List<GameObject> uiComponents;
        [SerializeField] protected FilePathController optionFilePath;
        [SerializeField] protected TextInputField optionRootUrl;

        // Display
        [SerializeField] protected Fade mapFade;
        [SerializeField] protected Transform spawnGroup;

        // Panel Properties
        public int actualComponentId { get; set; }

        #endregion

        #region User Input
        // This set of functions are used so the system can take user input while changing panel components
        
        // UI Components will change according to user configurations one by one
        public void ChangeVisibleComponent(int componentId)
        {
            StartCoroutine(ChangeComponent(componentId));
        }
        private IEnumerator ChangeComponent(int componentId)
        {
            // FadeOut actual component
            FadeUI actualComponentFader = uiComponents[actualComponentId].GetComponent<FadeUI>();
            yield return StartCoroutine(actualComponentFader.FadeOut());
            // Disable actual component
            uiComponents[actualComponentId].SetActive(false);
            // Enable new component
            uiComponents[componentId].SetActive(true);
            actualComponentId = componentId;
            // Block button if necessary
            BlockButton(componentId);
            // FadeIn new component
            FadeUI newComponentFader = uiComponents[componentId].GetComponent<FadeUI>();
            yield return StartCoroutine(newComponentFader.FadeIn());

        }
        public void ChangeVisibleComponentFade(int componentId)
        {
            StartCoroutine(ChangeComponentFade(componentId));
        }
        private IEnumerator ChangeComponentFade(int componentId)
        {
            // FadeOut actual component
            FadeUI actualComponentFader = uiComponents[actualComponentId].GetComponent<FadeUI>();
            yield return StartCoroutine(actualComponentFader.FadeOut());
            // FadeIn new component
            FadeUI newComponentFader = uiComponents[componentId].GetComponent<FadeUI>();
            yield return StartCoroutine(newComponentFader.FadeIn());
            actualComponentId = componentId;
        }

        // Different SpawnPanels block buttons of their components differently
        public abstract void BlockButton(int componentId);

        #endregion

        #region Display Multimedia
        public abstract void GenerateBase();

        public abstract void DestroyBase();
        #endregion
    }
}

