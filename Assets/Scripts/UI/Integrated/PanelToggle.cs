using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using UnityEngine.UI;
using TMPro;

public class PanelToggle : MonoBehaviour
{
    [SerializeField] private GameObject spawnPanel;
    [SerializeField] private Toggle togglePanel;

    public void ToggleSpawnPanel()
    {
        if (!togglePanel.isOn)
        {
            spawnPanel.SetActive(true);
        }
        else
        {
            spawnPanel.SetActive(false);
        }

    }
}
