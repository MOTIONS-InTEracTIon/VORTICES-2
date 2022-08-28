using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using UnityEngine.UI;
using TMPro;

public class MenuCanvas : MonoBehaviour
{
    [SerializeField] private GameObject spawnPanel;

    [SerializeField] private TextMeshProUGUI openPanelText;
    [SerializeField] private TextMeshProUGUI closePanelText;

    public bool isClosed;

    public void ToggleSpawnPanel()
    {
        if (isClosed)
        {
            spawnPanel.SetActive(true);
            openPanelText.gameObject.SetActive(false);
            closePanelText.gameObject.SetActive(true);
            isClosed = false;
        }
        else
        {
            spawnPanel.SetActive(false);
            openPanelText.gameObject.SetActive(true);
            closePanelText.gameObject.SetActive(false);
            isClosed = true;
        }

    }
}
