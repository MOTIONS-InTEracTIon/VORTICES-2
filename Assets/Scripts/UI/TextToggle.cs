using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using UnityEngine.UI;

public class TextToggle : MonoBehaviour
{
    [SerializeField] private Toggle toggle;
    
    public bool GetData()
    {
        return toggle.isOn;
    }
}
