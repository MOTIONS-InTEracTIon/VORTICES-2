using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using UnityEngine.UI;
using TMPro;

public class LoadingSmall : MonoBehaviour
{
    // Loading prompt components
    [SerializeField] private Image background;
    [SerializeField] private TextMeshProUGUI loadingText;
    [SerializeField] private Color loadingColor;
    [SerializeField] private TextMeshProUGUI doneText;
    [SerializeField] private Color doneColor;

    // Coroutine status
    private bool switchStatusRunning;

    public void StartLoading()
    {
        if (switchStatusRunning)
        {
            StopCoroutine("SwitchStatus");
            background.color = loadingColor;
            loadingText.gameObject.SetActive(true);
            doneText.gameObject.SetActive(false);
        }
        else
        {
            background.gameObject.SetActive(true);
            background.color = loadingColor;
            loadingText.gameObject.SetActive(true);
            doneText.gameObject.SetActive(false);
        }
    }

    public void DoneLoading()
    {
        if (switchStatusRunning)
        {
            StopCoroutine("SwitchStatus");
        }
        StartCoroutine("SwitchStatus");
    }

    public IEnumerator SwitchStatus()
    {
        switchStatusRunning = true;
        background.color = doneColor;
        doneText.gameObject.SetActive(true);
        loadingText.gameObject.SetActive(false);
        yield return new WaitForSeconds(3);
        background.gameObject.SetActive(false);
        loadingText.gameObject.SetActive(false);
        doneText.gameObject.SetActive(false);
        switchStatusRunning = false;
    }
}
