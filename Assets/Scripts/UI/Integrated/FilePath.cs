using System;
using System.Collections.Generic;
using System.IO;
using TMPro;
using UnityEngine;

namespace Vortices
{
    public class FilePath : MonoBehaviour
    {
        #region Variables and properties
        [SerializeField] private TextMeshProUGUI placeholderText;
        [SerializeField] private TextMeshProUGUI selectionText;

        [SerializeField] private List<TextMeshProUGUI> dataCounters;

        // Extension check
        private bool hasH264Codec = false;
        private List<string> supportedExtensions;
        private List<string> unsupportedExtensions;
        private List<string> filePathsRaw;

        // Data
        public List<string> filePaths { get; private set; }
        public int numberOfFolders { get; private set; }
        public int numberOfSupported { get; private set; }
        public int numberOfUnsupported { get; private set; }
        public int numberOfUnknown { get; private set; }
        #endregion

        private void Start()
        {
            supportedExtensions = new List<string>();
            unsupportedExtensions = new List<string>();
            AddSupportedExtensions();
            AddUnsupportedExtensions();
        }

        #region Path extraction
        // Filters paths obtained from SimpleFileBrowser turning folders into files
        public void GetFilePaths(string[] originPaths)
        {
            // Get files from each folder
            foreach (string path in originPaths)
            {
                if (Directory.Exists(path))
                {
                    numberOfFolders++;
                    filePathsRaw.AddRange(Directory.GetFiles(path, "*.*", SearchOption.AllDirectories));
                }
                else
                {
                    filePathsRaw.Add(path);
                }

            }
            // Filter by extension
            foreach (string path in filePathsRaw)
            {
                if (CheckExtensionSupport(path))
                {
                    filePaths.Add(path);
                }
            }
        }

        // Updates the UI texts after getting the paths
        public void SetUIText()
        {
            // Selection Text
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

            // Counters
            // Supported
            dataCounters[0].text = "" + numberOfSupported;
            if (numberOfSupported > 0)
            {
                dataCounters[0].color = Color.green;
            }
            // Unsupported
            dataCounters[1].text = "" + numberOfUnsupported;
            if (numberOfUnsupported > 0)
            {
                dataCounters[1].color = Color.red;
            }
            // Unknown
            dataCounters[2].text = "" + numberOfUnknown;
            if (numberOfUnsupported > 0)
            {
                dataCounters[2].color = Color.grey;
            }
        }

        // Clears the information everytime the list of folders or files is changed
        public void ClearPaths()
        {
            filePathsRaw = new List<string>();
            filePaths = new List<string>();
            numberOfFolders = 0;
            numberOfSupported = 0;
            numberOfUnsupported = 0;
            numberOfUnknown = 0;
        }

        #endregion

        #region Path extension check

        private bool CheckExtensionSupport(string path)
        {
            bool addToFilePaths = false;
            bool found = false;
            string pathExtension = Path.GetExtension(path);
            pathExtension = pathExtension.ToLower();
            //Check if its supported
            if (supportedExtensions.Contains(pathExtension))
            {
                numberOfSupported++;
                addToFilePaths = true;
                found = true;
            }
            //Check if its unsupported
            if (!found && unsupportedExtensions.Contains(pathExtension))
            {
                numberOfUnsupported++;
                found = true;
            }
            //Otherwise add to unknown
            if (!found)
            {
                numberOfUnknown++;
                addToFilePaths = true;
            }
            return addToFilePaths;
        }

        private void AddSupportedExtensions()
        {
            // Add here supported extensions
            supportedExtensions.Add(".mp3");
            supportedExtensions.Add(".ogv");
            supportedExtensions.Add(".ogg");
            supportedExtensions.Add(".oga");
            supportedExtensions.Add(".webm");
            supportedExtensions.Add(".wav");
            supportedExtensions.Add(".txt");
            supportedExtensions.Add(".pdf");
            supportedExtensions.Add(".bmp");
            supportedExtensions.Add(".gif");
            supportedExtensions.Add(".jpg");
            supportedExtensions.Add(".jpeg");
            supportedExtensions.Add(".png");
            supportedExtensions.Add(".webp");
            supportedExtensions.Add(".ico");
            supportedExtensions.Add(".webp");
            supportedExtensions.Add(".json");

            if (hasH264Codec)
            {
                supportedExtensions.Add(".3gp");
                supportedExtensions.Add(".mp4");
                supportedExtensions.Add(".m4a");
                supportedExtensions.Add(".m4v");
            }

        }

        private void AddUnsupportedExtensions()
        {
            // Add here tested unsupported extensions
            unsupportedExtensions.Add(".doc");
            unsupportedExtensions.Add(".docx");
            unsupportedExtensions.Add(".xls");
            unsupportedExtensions.Add(".xlsx");
            unsupportedExtensions.Add(".ppt");
            unsupportedExtensions.Add(".pptx");
            unsupportedExtensions.Add(".avi");
            unsupportedExtensions.Add(".mov");
            unsupportedExtensions.Add(".mkv");
            unsupportedExtensions.Add(".wmv");
            unsupportedExtensions.Add(".odt");
            unsupportedExtensions.Add(".ods");
            unsupportedExtensions.Add(".odp");
            unsupportedExtensions.Add(".rtf");
            unsupportedExtensions.Add(".tiff");
            unsupportedExtensions.Add(".xml");
            unsupportedExtensions.Add(".csv");
            if (!hasH264Codec)
            {
                unsupportedExtensions.Add(".3gp");
                unsupportedExtensions.Add(".mp4");
                unsupportedExtensions.Add(".m4a");
                unsupportedExtensions.Add(".m4v");
            }

        }
        #endregion
    }
}
