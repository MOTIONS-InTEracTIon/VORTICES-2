using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using SimpleFileBrowser;
using System;
using System.IO;

public class FileBrowserButton : MonoBehaviour
{
    [SerializeField] private TextScrollView texturePathScrollView;

    private List<string> filePaths;

    private void Start()
    {
        FileBrowser.SingleClickMode = true;
    }

    public void OpenFileBrowser()
    {
        FileBrowser.ShowLoadDialog((paths) => 
        {
            texturePathScrollView.ClearPaths();
            filePaths = new List<string>();
            GetFilePaths(paths);
            texturePathScrollView.AddPaths(filePaths);
        },
        () => { Debug.Log("Canceled"); },FileBrowser.PickMode.FilesAndFolders, true, Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), null, "Load", "Select");

    }

    // Recursion to get files outside or inside folders
    private void GetFilePaths(string[] originPaths)
    {
        foreach (string path in originPaths)
        {
            if (Directory.Exists(path))
            {
                filePaths.AddRange(Directory.GetFiles(path, "*.*", SearchOption.AllDirectories));
            }
            else
            {
                filePaths.Add(path);
            }

        }
    }
}
