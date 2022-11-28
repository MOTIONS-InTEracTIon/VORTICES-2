using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using SimpleFileBrowser;

enum MuseumId
{
    // Change this when order is changed or when new panels are added
    Introduction = 0,
    BrowsingMode = 1,
    BrowsingLocal = 2,
    FileBrowser = 3,
    BrowsingOnline = 4,
    CategorySelection = 5,
    Postload = 6

}

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
            uiComponents[(int)MuseumId.FileBrowser] = GameObject.Find("SimpleFileBrowserCanvas(Clone)");
        }

        // Handles block next button rules per component
        public override void BlockButton(int componentId)
        {
            bool hasToBlock = true;
            switch (componentId)
            {
                // Description has no block
                case (int)MuseumId.Introduction:
                    hasToBlock = false;
                    break;
                // Browsing mode has to be selected
                case (int)MuseumId.BrowsingMode:
                    Toggle localToggle = uiComponents[componentId].transform.Find("Content/Horizontal Group/Local Toggle").GetComponentInChildren<Toggle>();
                    Toggle onlineToggle = uiComponents[componentId].transform.Find("Content/Horizontal Group/Online Toggle").GetComponentInChildren<Toggle>();
                    if (!localToggle.interactable || !onlineToggle.interactable)
                    {
                        hasToBlock = false;
                    }
                    break;
                // Local mode has to have a correct set to load
                case (int)MuseumId.BrowsingLocal:
                    if (optionFilePath.filePaths != null && optionFilePath.filePaths.Count > 0)
                    {
                        hasToBlock = false;
                    }
                    break;
                // Online mode has a default url so it starts enabled, disabled if no url
                case (int)MuseumId.BrowsingOnline:
                    if (optionRootUrl.text.text != "" || optionRootUrl.placeholder.text != "")
                    {
                        hasToBlock = false;
                    }
                    break;
                // Category Controller unlocks button by its own
            }

            // Insert here panels that dont need block function
            if (componentId != (int)MuseumId.FileBrowser && 
                componentId != (int)MuseumId.Postload)
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
                ChangeVisibleComponent((int)MuseumId.BrowsingLocal);
            }
            else if (browsingMode == 1)
            {
                ChangeVisibleComponent((int)MuseumId.BrowsingOnline);
            }
        }

        // Changes component and starts the base operation
        public void ChangeComponentBase()
        {
            ChangeVisibleComponent((int)MuseumId.Postload);
            GenerateBase();
        }

        // Sets starting Url for online mode
        public void SetRootUrl()
        {
            if (optionRootUrl.GetData() != "")
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
                    ChangeVisibleComponent((int)MuseumId.BrowsingLocal);
                },
                () => {/* Handle closing*/
                    ChangeVisibleComponent((int)MuseumId.BrowsingLocal);
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
                MuseumSpawnBase spawnBase = placementBase.GetComponent<MuseumSpawnBase>();
                StartCoroutine(spawnBase.DestroyBase());
            }

        }
        #endregion
    }
}
