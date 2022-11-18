using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using SimpleFileBrowser;

namespace Vortices
{
    public class CircularPanel : MonoBehaviour
    {
        #region Variables and properties
        // Circular Panel UI Components
        [SerializeField] private List<GameObject> uiComponents;

        // Circular Panel Data Components
        [SerializeField] private FilePath optionFilePath;
        [SerializeField] private TextInputField optionRootUrl;

        // Circular Panel Properties
        // (Input)
        public int actualComponentId { get; set; }
        public int displayMode { get; set; }
        public int browsingMode { get; set; }
        public bool volumetric { get; set; }
        public Vector3Int dimension;
        public string rootUrl { get; set; }

        // (Display)
        [SerializeField] List<GameObject> placementBasePrefabs;
        private GameObject placementBase;
        [SerializeField] private Fade mapFade;

        [SerializeField] private Transform spawnGroup;

        private void Start()
        {
            rootUrl = "https://www.google.com";
        }

        #endregion

        #region User Input
        // This set of functions are used so the system can take user input to display multimedia
        // Changes single component
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
            // Block button if necessary
            if (componentId != 3)
            {
                BlockButton(componentId);
            }
            // FadeIn new component
            FadeUI newComponentFader = uiComponents[componentId].GetComponent<FadeUI>();
            yield return StartCoroutine(newComponentFader.FadeIn());
            actualComponentId = componentId;
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
        public void AddBrowserToComponents()
        {
            uiComponents[3] = GameObject.Find("SimpleFileBrowserCanvas(Clone)");
        }

        // Handles block next button rules per component
        public void BlockButton(int componentId)
        {
            bool hasToBlock = true;
            switch (componentId)
            {
                // Description has no block
                case 0:
                    hasToBlock = false;
                    break;
                // Browsing mode has to be selected
                case 1:
                    Toggle localToggle = uiComponents[componentId].transform.Find("Content/Horizontal Group/Local Toggle").GetComponentInChildren<Toggle>();
                    Toggle onlineToggle = uiComponents[componentId].transform.Find("Content/Horizontal Group/Online Toggle").GetComponentInChildren<Toggle>();
                    if (!localToggle.interactable || !onlineToggle.interactable)
                    {
                        hasToBlock = false;
                    }
                    break;
                // Local mode has to have a correct set to load
                case 2:
                    if (optionFilePath.filePaths != null && optionFilePath.filePaths.Count > 0)
                    {
                        hasToBlock = false;
                    }
                    break;
                // Online mode has a default url so it starts enabled, disabled if no url
                case 4:
                    if (optionRootUrl.text.text != "" || optionRootUrl.placeholder.text != "")
                    {
                        hasToBlock = false;
                    }
                    break;
                // Display mode has to be selected
                case 5:
                    Toggle planeToggle = uiComponents[componentId].transform.Find("Content/Horizontal Group/Plane Toggle").GetComponentInChildren<Toggle>();
                    Toggle radialToggle = uiComponents[componentId].transform.Find("Content/Horizontal Group/Radial Toggle").GetComponentInChildren<Toggle>();
                    if (!planeToggle.interactable || !radialToggle.interactable)
                    {
                        hasToBlock = false;
                    }
                    break;
                // Height, Width and layers have to be bigger than 0
                case 6: case 7: case 8: case 9: case 10: case 11:
                    string input = uiComponents[componentId].GetComponentInChildren<TMP_InputField>().text;
                    int value = 0;
                    try
                    {
                        value = int.Parse(input);
                    }
                    catch {}
                    if(value > 0)
                    {
                        hasToBlock = false;
                    }
                    break;
            }

            // Insert here panels that dont need block function
            if (componentId != 12)
            {
                Button nextButton = uiComponents[componentId].transform.Find("Footer").transform.GetComponentInChildren<Button>();
                if (hasToBlock)
                {
                    nextButton.interactable = false;
                }
                else
                {
                    nextButton.interactable = true;
                }
            }
        }

        // Changes component based on settings fork browsingMode
        public void ChangeComponentBrowserMode()
        {
            // Browsing mode fork
            if (browsingMode == 0)
            {
                ChangeVisibleComponent(2);
            }
            else if (browsingMode == 1)
            {
                ChangeVisibleComponent(4);
            }
        }
        // Changes component based on settings fork displayMode
        public void ChangeComponentDisplayMode(int option)
        {
            // Plane mode
            if(displayMode == 0)
            {
                switch (option)
                {
                    // Plane 1
                    case 0:
                        ChangeVisibleComponent(6);
                        break;
                    // Plane 2
                    case 1:
                        ChangeVisibleComponent(7);
                        break;
                    // Plane 3
                    case 2:
                        if (volumetric)
                        {
                            ChangeVisibleComponent(8);
                        }
                        else
                        {
                            ChangeVisibleComponent(12);
                            GenerateBase();
                        }
                        break;
                    case 3:
                        ChangeVisibleComponent(12);
                        GenerateBase();
                        break;
                    default:
                        break;
                }
            }
            // Radial mode
            else if (displayMode == 1)
            {
                switch (option)
                {
                    // Radial 1
                    case 0:
                        ChangeVisibleComponent(9);
                        break;
                    // Radial 2
                    case 1:
                        ChangeVisibleComponent(10);
                        break;
                    // Radial 3
                    case 2:
                        if(volumetric)
                        {
                            ChangeVisibleComponent(11);
                        }
                        else
                        {
                            ChangeVisibleComponent(12);
                            GenerateBase();
                        }
                        break;
                    case 3:
                        ChangeVisibleComponent(12);
                        GenerateBase();
                        break;
                    default:
                        break;
                }
            }
        }

        // Toggles volumetric bool
        public void VolumetricToggle()
        {
            if(volumetric)
            {
                volumetric = false;
            }
            else
            {
                volumetric = true;
            }
        }

        // Sets vector dimensions for multimedia display (Representing X as quantity or width, Y as height and Z as layers)
        public void SetDimension(int option)
        {
            // Plane mode
            if (displayMode == 0)
            {
                switch (option)
                {
                    case 0:
                        TMP_InputField planeXInput = uiComponents[6].GetComponentInChildren<TMP_InputField>();
                        dimension.x = int.Parse(planeXInput.text);
                        break;
                    case 1:
                        TMP_InputField planeYInput = uiComponents[7].GetComponentInChildren<TMP_InputField>();
                        dimension.y = int.Parse(planeYInput.text);
                        break;
                    case 2:
                        TMP_InputField planeZInput = uiComponents[8].GetComponentInChildren<TMP_InputField>();
                        dimension.z = int.Parse(planeZInput.text);
                        break;
                    default:
                        break;
                }
            }
            // Radial mode
            else if (displayMode == 1)
            {
                switch (option)
                {
                    case 0:
                        TMP_InputField radialXInput = uiComponents[9].GetComponentInChildren<TMP_InputField>();
                        dimension.x = int.Parse(radialXInput.text);
                        break;
                    case 1:
                        TMP_InputField radialYInput = uiComponents[10].GetComponentInChildren<TMP_InputField>();
                        dimension.y = int.Parse(radialYInput.text);
                        break;
                    case 2:
                        TMP_InputField radialZInput = uiComponents[11].GetComponentInChildren<TMP_InputField>();
                        dimension.z = int.Parse(radialZInput.text);
                        break;
                    default:
                        break;
                }
            }
        }

        // Sets starting Url for online mode
        public void SetRootUrl()
        {
            if (optionRootUrl.text.text != "")
            {
                rootUrl = optionRootUrl.text.text;
            }
        }

        // Uses SimpleFileBrowser to obtain a list of paths and apply them to the property filePaths so other components can use them
        public void OpenFileBrowser()
        {
            FileBrowser.ShowLoadDialog((paths) =>
                {
                    optionFilePath.ClearPaths();
                    optionFilePath.GetFilePaths(paths);
                    optionFilePath.SetUIText();
                    ChangeVisibleComponent(2);
                },
                () => {/* Handle closing*/
                    ChangeVisibleComponent(2);
                },
                FileBrowser.PickMode.FilesAndFolders, true, Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), null, "Load", "Select");

        }

        #endregion

        #region Display Multimedia
        public void GenerateBase()
        {
            // Plane mode
            if (displayMode == 0)
            {
                Vector3 positionOffset = new Vector3(0, 0, 1f); ;
                placementBase = Instantiate(placementBasePrefabs[0], spawnGroup.transform.position + positionOffset, spawnGroup.transform.rotation, spawnGroup);

            }
            else if (displayMode == 1)
            {
                placementBase = Instantiate(placementBasePrefabs[1], spawnGroup.transform.position, placementBasePrefabs[1].transform.rotation, spawnGroup);
            }
            SpawnBase spawnBase = placementBase.GetComponent<SpawnBase>();
            if (browsingMode == 0)
            {
                spawnBase.browsingMode = "Local";
                spawnBase.filePaths = optionFilePath.filePaths;
            }
            else
            {
                spawnBase.browsingMode = "Online";
                spawnBase.rootUrl = rootUrl;
            }
            if (displayMode == 0)
            {
                spawnBase.displayMode = "Plane";
            }
            else
            {
                spawnBase.displayMode = "Radial";
            }
            spawnBase.volumetric = volumetric;
            if (volumetric == false || dimension.z == 0)
            {
                dimension.z = 1;
            }
            spawnBase.dimension = dimension;
            spawnBase.StartGenerateSpawnGroup();
            mapFade.lowerAlpha = 0.1f;
            mapFade.FadeOut();
        }

        public void DestroyBase()
        {
            if (placementBase != null)
            {
                SpawnBase spawnBase = placementBase.GetComponent<SpawnBase>();
                StartCoroutine(spawnBase.DestroyBase());
            }

        }
        #endregion
    }
}
