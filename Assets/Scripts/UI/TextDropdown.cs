using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using TMPro;

public class TextDropdown : MonoBehaviour
{
    [SerializeField] private TMP_Dropdown dropdown;

    public int GetData()
    {
        return dropdown.value;
    }
}
