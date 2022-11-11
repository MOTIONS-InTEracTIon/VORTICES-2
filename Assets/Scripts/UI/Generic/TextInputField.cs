using TMPro;
using UnityEngine;

namespace Vortices
{
    public class TextInputField : MonoBehaviour
    {
        [SerializeField] public TMP_InputField inputfield;

        [SerializeField] public TextMeshProUGUI placeholder;
        [SerializeField] public TextMeshProUGUI text;

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

        public void ClearPlaceholderText()
        {
            placeholder.text = "";
        }
    }
}
