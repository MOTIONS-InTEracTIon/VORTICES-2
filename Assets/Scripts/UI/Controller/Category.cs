using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using TMPro;

namespace Vortices
{
    public class Category : MonoBehaviour
    {
        // Other references
        [SerializeField] private TextMeshProUGUI nameText;
        [SerializeField] private CategoryRemoveButton removeButton;

        // Data variables
        public GameObject horizontalGroup; // Horizontal group gameobject where this category is present
        public string categoryName;


        #region Data Operation

        public void Init(string name, CategoryController categoryController, GameObject horizontalGroup)
        {
            string newName = name.ToLower();
            char[] a = newName.ToCharArray();
            a[0] = char.ToUpper(a[0]);
            newName = new string(a);

            nameText.text = newName;
            categoryName = newName;
            this.horizontalGroup = horizontalGroup;

            removeButton.category = this;
            removeButton.controller = categoryController;
        }

        public void DestroyCategory()
        {
            transform.SetParent(null);
            if (horizontalGroup.transform.childCount - 1 == 0)
            {
                Destroy(horizontalGroup.gameObject);
            }
            else
            {
                Destroy(gameObject);
            }
        }

        #endregion

    }
}

