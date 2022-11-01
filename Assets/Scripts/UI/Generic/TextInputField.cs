using TMPro;
using UnityEngine;

namespace Vortices
{
    public class TextInputField : MonoBehaviour
    {
        [SerializeField] private TMP_InputField inputfield;

        public string GetData()
        {
            return inputfield.text;
        }
        public int GetDataInt()
        {
            try
            {
                return int.Parse(inputfield.text);
            }
            catch
            {
                return 0;
            }
        
        }

        public void SetText(string text)
        {
            inputfield.text = text;
        }
    }
}
