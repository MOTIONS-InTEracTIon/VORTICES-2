using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using UnityEngine.UI;
using TMPro;

public class TextScrollView : MonoBehaviour
{
    [SerializeField] private GameObject entryPrefab;
    [SerializeField] private Transform scrollViewContent;
    [SerializeField] private Scrollbar contentScrollBar;

    public List<string> scrollViewPaths { get; private set;} 

    public void AddPaths(List<string> stringList)
    {
        scrollViewPaths = stringList;

        foreach (string path in scrollViewPaths)
        {
            GameObject newEntry = Instantiate(entryPrefab, scrollViewContent);
            newEntry.name = "Path Entry";
            newEntry.GetComponent<TextMeshProUGUI>().text = path;
        }

        contentScrollBar.numberOfSteps = scrollViewPaths.Count;

    }

    public void ClearPaths()
    {
        foreach (Transform child in scrollViewContent.transform)
        {
            Destroy(child.gameObject);
        }
    }

}
