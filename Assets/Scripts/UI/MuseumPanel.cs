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
    BrowsingMode = 0,
    BrowsingLocal = 1,
    FileBrowser = 2,
    BrowsingOnline = 3,
    Postload = 4

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

            sessionManager = GameObject.Find("SessionManager").GetComponent<SessionManager>();
        }

        #endregion

        #region User Input

        public void AddBrowserToComponents()
        {
            uiComponents[(int)MuseumId.FileBrowser] = GameObject.Find("SimpleFileBrowserCanvas(Clone)");
            FileBrowser fileBrowser = uiComponents[(int)MuseumId.FileBrowser].GetComponent<FileBrowser>();
            fileBrowser.SetAsPersistent(false);
        }

        // Handles block next button rules per component
        public override void BlockButton(int componentId)
        {
            bool hasToBlock = true;
            switch (componentId)
            {
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
        }
        public void RemoveBrowserFromComponents()
        {
            FileBrowser fileBrowser = uiComponents[(int)CircularId.FileBrowser].GetComponent<FileBrowser>();
            fileBrowser.SetAsPersistent(true);
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

        public void SendDataToSessionManager()
        {
            // Sends Museum Data setting variables
            // Browsing Mode
            if (browsingMode == 0)
            {
                sessionManager.browsingMode = "Local";
                sessionManager.filePaths = optionFilePath.filePaths;
            }
            else
            {
                sessionManager.browsingMode = "Online";
                sessionManager.rootUrl = rootUrl;
            }
            sessionManager.displayMode = "Museum";

            sessionManager.LaunchSession();
        }

        #endregion

    }
}
