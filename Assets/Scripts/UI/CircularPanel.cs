using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Vortices
{
    public class CircularPanel : MonoBehaviour
    {
        #region Variables and properties
        // Circular Panel UI Components
        [SerializeField] private List<GameObject> uiComponents;

        // Circular Panel Data Components
        [SerializeField] private FilePath optionFilePath;

        // Circular Panel Properties
        // (Input)
        public int actualComponentId { get; set; }
        public int mode { get; set; }
        public bool volumetric { get; set; }
        public Vector3Int dimension;

        // (Display)
        [SerializeField] List<GameObject> placementBasePrefabs;
        private GameObject placementBase;
        [SerializeField] private Fade mapFade;

        [SerializeField] private Transform spawnGroup;


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
            if(componentId != 9)
            {
                blockButton(componentId);
            }
            // FadeIn new component
            FadeUI newComponentFader = uiComponents[componentId].GetComponent<FadeUI>();
            yield return StartCoroutine(newComponentFader.FadeIn());
            actualComponentId = componentId;
        }
        public void AddBrowserToComponents()
        {
            uiComponents[9] = GameObject.Find("SimpleFileBrowserCanvas(Clone)");
        }

        // Handles block next button rules per component
        public void blockButton(int componentId)
        {
            bool hasToBlock = true;
            switch (componentId)
            {
                // Description has no block
                case 0:
                    hasToBlock = false;
                    break;
                // Mode has to be selected
                case 1:
                    Toggle planeToggle = uiComponents[componentId].transform.Find("Content/Horizontal Group/Plane Toggle").GetComponentInChildren<Toggle>();
                    Toggle radialToggle = uiComponents[componentId].transform.Find("Content/Horizontal Group/Radial Toggle").GetComponentInChildren<Toggle>();
                    if (!planeToggle.interactable || !radialToggle.interactable)
                    {
                        hasToBlock = false;
                    }
                    break;
                // Height, Width and layers have to be bigger than 0
                case 2: case 3: case 4: case 5: case 6: case 7:
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
                case 8:
                    FilePath filePath = uiComponents[componentId].GetComponentInChildren<FilePath>();
                    if(filePath.filePaths != null && filePath.filePaths.Count > 0)
                    {
                        hasToBlock = false;
                    }
                    break;
            }

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

        // Changes component based on option setting (width, height, layers) and mode
        public void ChangeComponentMode(int option)
        {
            // Plane mode
            if(mode == 0)
            {
                switch (option)
                {
                    case 0:
                        ChangeVisibleComponent(2);
                        break;
                    case 1:
                        ChangeVisibleComponent(3);
                        break;
                    case 2:
                        if (volumetric)
                        {
                            ChangeVisibleComponent(4);
                        }
                        else
                        {
                            ChangeVisibleComponent(8);
                        }
                        break;
                    default:
                        break;
                }
            }
            // Radial mode
            else if (mode == 1)
            {
                switch (option)
                {
                    case 0:
                        ChangeVisibleComponent(5);
                        break;
                    case 1:
                        ChangeVisibleComponent(6);
                        break;
                    case 2:
                        if(volumetric)
                        {
                            ChangeVisibleComponent(7);
                        }
                        else
                        {
                            ChangeVisibleComponent(8);
                        }
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
            Debug.Log(volumetric);
        }

        // Sets vector dimensions for multimedia display (Representing X as quantity or width, Y as height and Z as layers)
        public void SetDimension(int option)
        {
            // Plane mode
            if (mode == 0)
            {
                switch (option)
                {
                    case 0:
                        TMP_InputField planeXInput = uiComponents[2].GetComponentInChildren<TMP_InputField>();
                        dimension.x = int.Parse(planeXInput.text);
                        break;
                    case 1:
                        TMP_InputField planeYInput = uiComponents[3].GetComponentInChildren<TMP_InputField>();
                        dimension.y = int.Parse(planeYInput.text);
                        break;
                    case 2:
                        TMP_InputField planeZInput = uiComponents[4].GetComponentInChildren<TMP_InputField>();
                        dimension.z = int.Parse(planeZInput.text);
                        break;
                    default:
                        break;
                }
            }
            // Radial mode
            else if (mode == 1)
            {
                switch (option)
                {
                    case 0:
                        TMP_InputField radialXInput = uiComponents[5].GetComponentInChildren<TMP_InputField>();
                        dimension.x = int.Parse(radialXInput.text);
                        break;
                    case 1:
                        TMP_InputField radialYInput = uiComponents[6].GetComponentInChildren<TMP_InputField>();
                        dimension.y = int.Parse(radialYInput.text);
                        break;
                    case 2:
                        TMP_InputField radialZInput = uiComponents[7].GetComponentInChildren<TMP_InputField>();
                        dimension.z = int.Parse(radialZInput.text);
                        break;
                    default:
                        break;
                }
            }
        }
        #endregion

        #region Display Multimedia
        public void GenerateBase()
        {
            // Plane mode
            if (mode == 0)
            {
                Vector3 positionOffset = new Vector3(0, 0, 1f); ;
                placementBase = Instantiate(placementBasePrefabs[0], spawnGroup.transform.position + positionOffset, spawnGroup.transform.rotation, spawnGroup);

            }
            else if (mode == 1)
            {
                placementBase = Instantiate(placementBasePrefabs[1], spawnGroup.transform.position, placementBasePrefabs[1].transform.rotation, spawnGroup);
            }
            SpawnBase spawnBase = placementBase.GetComponent<SpawnBase>();
            spawnBase.filePaths = optionFilePath.filePaths;
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
            SpawnBase spawnBase = placementBase.GetComponent<SpawnBase>();
            StartCoroutine(spawnBase.StopSpawn());
        }
        #endregion
    }
}
