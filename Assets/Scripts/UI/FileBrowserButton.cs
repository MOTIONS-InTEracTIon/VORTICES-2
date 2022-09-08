using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using SimpleFileBrowser;
using System;

public class FileBrowserButton : MonoBehaviour
{
    [SerializeField] private TextScrollView texturePathScrollView;

    private void Start()
    {
        FileBrowser.SingleClickMode = true;
    }

    public void OpenFileBrowser()
    {
        //CHANGE: Put success data into a variable, not just set it to text
        FileBrowser.ShowLoadDialog((paths) => { texturePathScrollView.ClearPaths();
                                                texturePathScrollView.AddPaths(paths); },
                                        () => { Debug.Log("Canceled"); }, 
                                   FileBrowser.PickMode.Files, true, Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), null, "Load", "Select");

    }
}
