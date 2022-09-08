using System.Reflection.Emit;
using UnityEngine;
using UnityEngine.UI;

using TMPro;

[RequireComponent(typeof(TextMeshProUGUI))]
public class ShowSliderValue : MonoBehaviour
{
	public void UpdateLabel (float value)
	{
		TextMeshProUGUI lbl = GetComponent<TextMeshProUGUI>();
		if (lbl != null)
        {
			if(value < 0.5)
            {
				lbl.text = "x" + System.Math.Round(0.5 + value, 1).ToString();
			} 
			else
            {
				lbl.text = "x" + System.Math.Round(value * 2, 1).ToString();
			}
		}

	}
}
