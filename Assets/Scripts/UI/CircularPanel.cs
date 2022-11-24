using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using SimpleFileBrowser;

enum CircularId
{
    // Change this when order is changed or when new panels are added
    Introduction = 0,
    BrowsingMode = 1,
    BrowsingLocal = 2,
    FileBrowser = 3,
    BrowsingOnline = 4,
    CategorySelection = 5,
    DisplayMode = 6,
    DistributionPlane1 = 7,
    DistributionPlane2 = 8,
    DistributionPlane3 = 9,
    DistributionRadial1 = 10,
    DistributionRadial2 = 11,
    DistributionRadial3 = 12,
    Postload = 13

}

namespace Vortices
{
    public class CircularPanel : SpawnPanel
    {
        #region Variables and properties

        // Circular Panel Properties
        public int displayMode { get; set; }
        public int browsingMode { get; set; }
        public bool volumetric { get; set; }
        public Vector3Int dimension;
        public string rootUrl { get; set; }

        // Display
        [SerializeField] List<GameObject> placementBasePrefabs;
        private GameObject placementBase;

        private void Start()
        {
            // Default configs to properties
            rootUrl = "https://www.google.com";
        }

        #endregion

        #region User Input

        public void AddBrowserToComponents()
        {
            uiComponents[(int)CircularId.FileBrowser] = GameObject.Find("SimpleFileBrowserCanvas(Clone)");
        }

        // Handles block next button rules per component
        public override void BlockButton(int componentId)
        {
            bool hasToBlock = true;
            switch (componentId)
            {
                // Description has no block
                case (int)CircularId.Introduction:
                    hasToBlock = false;
                    break;
                // Browsing mode has to be selected
                case (int)CircularId.BrowsingMode:
                    Toggle localToggle = uiComponents[componentId].transform.Find("Content/Horizontal Group/Local Toggle").GetComponentInChildren<Toggle>();
                    Toggle onlineToggle = uiComponents[componentId].transform.Find("Content/Horizontal Group/Online Toggle").GetComponentInChildren<Toggle>();
                    if (!localToggle.interactable || !onlineToggle.interactable)
                    {
                        hasToBlock = false;
                    }
                    break;
                // Local mode has to have a correct set to load
                case (int)CircularId.BrowsingLocal:
                    if (optionFilePath.filePaths != null && optionFilePath.filePaths.Count > 0)
                    {
                        hasToBlock = false;
                    }
                    break;
                // Online mode has a default url so it starts enabled, disabled if no url
                case (int)CircularId.BrowsingOnline:
                    if (optionRootUrl.text.text != "" || optionRootUrl.placeholder.text != "")
                    {
                        hasToBlock = false;
                    }
                    break;
                // Display mode has to be selected
                case (int)CircularId.DisplayMode:
                    Toggle planeToggle = uiComponents[componentId].transform.Find("Content/Horizontal Group/Plane Toggle").GetComponentInChildren<Toggle>();
                    Toggle radialToggle = uiComponents[componentId].transform.Find("Content/Horizontal Group/Radial Toggle").GetComponentInChildren<Toggle>();
                    if (!planeToggle.interactable || !radialToggle.interactable)
                    {
                        hasToBlock = false;
                    }
                    break;
                // Category Controller unlocks button by its own
                // Height, Width and layers have to be bigger than 0
                case (int)CircularId.DistributionPlane1: 
                case (int)CircularId.DistributionPlane2: 
                case (int)CircularId.DistributionPlane3: 
                case (int)CircularId.DistributionRadial1: 
                case (int)CircularId.DistributionRadial2: 
                case (int)CircularId.DistributionRadial3:
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
            if (componentId != (int)CircularId.FileBrowser && 
                componentId != (int)CircularId.Postload)
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
                ChangeVisibleComponent((int)CircularId.BrowsingLocal);
            }
            else if (browsingMode == 1)
            {
                ChangeVisibleComponent((int)CircularId.BrowsingOnline);
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
                        ChangeVisibleComponent((int)CircularId.DistributionPlane1);
                        break;
                    // Plane 2
                    case 1:
                        ChangeVisibleComponent((int)CircularId.DistributionPlane2);
                        break;
                    // Plane 3
                    case 2:
                        if (volumetric)
                        {
                            ChangeVisibleComponent((int)CircularId.DistributionPlane3);
                        }
                        else
                        {
                            ChangeVisibleComponent((int)CircularId.Postload);
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
                        ChangeVisibleComponent((int)CircularId.DistributionRadial1);
                        break;
                    // Radial 2
                    case 1:
                        ChangeVisibleComponent((int)CircularId.DistributionRadial2);
                        break;
                    // Radial 3
                    case 2:
                        if(volumetric)
                        {
                            ChangeVisibleComponent((int)CircularId.DistributionRadial3);
                        }
                        else
                        {
                            ChangeVisibleComponent((int)CircularId.Postload);
                            GenerateBase();
                        }
                        break;
                    case 3:
                        ChangeVisibleComponent((int)CircularId.Postload);
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
                        TMP_InputField planeXInput = uiComponents[(int)CircularId.DistributionPlane1].GetComponentInChildren<TMP_InputField>();
                        dimension.x = int.Parse(planeXInput.text);
                        break;
                    case 1:
                        TMP_InputField planeYInput = uiComponents[(int)CircularId.DistributionPlane2].GetComponentInChildren<TMP_InputField>();
                        dimension.y = int.Parse(planeYInput.text);
                        break;
                    case 2:
                        TMP_InputField planeZInput = uiComponents[(int)CircularId.DistributionPlane3].GetComponentInChildren<TMP_InputField>();
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
                        TMP_InputField radialXInput = uiComponents[(int)CircularId.DistributionRadial1].GetComponentInChildren<TMP_InputField>();
                        dimension.x = int.Parse(radialXInput.text);
                        break;
                    case 1:
                        TMP_InputField radialYInput = uiComponents[(int)CircularId.DistributionRadial2].GetComponentInChildren<TMP_InputField>();
                        dimension.y = int.Parse(radialYInput.text);
                        break;
                    case 2:
                        TMP_InputField radialZInput = uiComponents[(int)CircularId.DistributionRadial3].GetComponentInChildren<TMP_InputField>();
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
                    ChangeVisibleComponent((int)CircularId.BrowsingLocal);
                },
                () => {/* Handle closing*/
                    ChangeVisibleComponent((int)CircularId.BrowsingLocal);
                },
                FileBrowser.PickMode.FilesAndFolders, true, Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), null, "Load", "Select");

        }

        #endregion

        #region Display Multimedia
        // Places all variables into a base that will display the multimedia objects
        public override void GenerateBase()
        {
            // Plane mode
            if (displayMode == 0)
            {
                Vector3 positionOffset = new Vector3(0, 0, 1f); ;
                placementBase = Instantiate(placementBasePrefabs[0], spawnGroup.transform.position + positionOffset, placementBasePrefabs[0].transform.rotation, spawnGroup);

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

        public override void DestroyBase()
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
