using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using UnityEngine.UI;

public class TextSlider : MonoBehaviour
{
    [SerializeField] private Slider slider;

    public float GetData()
    {
        if (slider.value < 0.5)
        {
            return (float)System.Math.Round(0.5 + slider.value, 1);
        }
        else
        {
            return (float)System.Math.Round(slider.value * 2, 1);
        }

    }
}
