using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using SimpleFileBrowser;
using System;
using System.IO;
using TMPro;

public class FilePath : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI placeholderText;
    [SerializeField] private TextMeshProUGUI selectionText;



    public List<string> filePaths { get; private set; }
    public int numberOfFolders { get; private set; }

    // Uses SimpleFileBrowser to obtain a list of paths and apply them to the property filePaths so other components can use them
    public void OpenFileBrowser()
    {
        FileBrowser.ShowLoadDialog((paths) =>
        {
            ClearPaths();
            GetFilePaths(paths);
            SetSelectionText();
            GameObject.Find("Circular Panel").GetComponent<CircularPanel>().ChangeVisibleComponent(8);
        },
        () => {/* Handle closing*/
            GameObject.Find("Circular Panel").GetComponent<CircularPanel>().ChangeVisibleComponent(8);
        },
                                  FileBrowser.PickMode.FilesAndFolders, true, Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), null, "Load", "Select");

    }

    // Filters paths obtained from SimpleFileBrowser turning folders into files
    private void GetFilePaths(string[] originPaths)
    {
        foreach (string path in originPaths)
        {
            if (Directory.Exists(path))
            {
                numberOfFolders++;
                filePaths.AddRange(Directory.GetFiles(path, "*.*", SearchOption.AllDirectories));
            }
            else
            {
                filePaths.Add(path);
            }

        }
    }

    // Updates the selection text after getting the paths
    public void SetSelectionText()
    {
        placeholderText.gameObject.SetActive(false);
        selectionText.gameObject.SetActive(true);

        string selection;
        if (filePaths.Count > 1 || filePaths.Count == 0)
        {
            selection = filePaths.Count + " files selected from ";
        }
        else
        {
            selection = filePaths.Count + " file selected from ";
        }

        if (numberOfFolders > 1)
        {
            selection += numberOfFolders + " folders.";
        }
        else
        {
            selection += "1 folder.";
        }

        selectionText.text = selection;
    }

    // Clears the information everytime the list of folders or files is changed
    public void ClearPaths()
    {
        filePaths = new List<string>();
        numberOfFolders = 0;
    }

}
