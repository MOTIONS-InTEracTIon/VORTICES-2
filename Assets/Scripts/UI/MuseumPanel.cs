using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using SimpleFileBrowser;

namespace Vortices
{
    public class MuseumPanel : SpawnPanel
    {
        #region Variables and properties

        // Museum Panel Properties
        public int browsingMode { get; set; }
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
            uiComponents[3] = GameObject.Find("SimpleFileBrowserCanvas(Clone)");
        }

        // Handles block next button rules per component
        public override void BlockButton(int componentId)
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
            }

            // Insert here panels that dont need block function
            if (componentId != 3 && componentId != 5)
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

        // Changes component and starts the base operation
        public void ChangeComponentBase()
        {
            ChangeVisibleComponent(5);
            GenerateBase();
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
        // Places all variables into a base that will display the multimedia objects
        public override void GenerateBase()
        {
            // Museum base wont instantiate as it has a premade spatial distribution (This can be changed to create more multimedia arrangements)
            placementBase = placementBasePrefabs[0];

            MuseumSpawnBase spawnBase = placementBase.GetComponent<MuseumSpawnBase>();
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

            StartCoroutine(spawnBase.StartGenerateSpawnElements());
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