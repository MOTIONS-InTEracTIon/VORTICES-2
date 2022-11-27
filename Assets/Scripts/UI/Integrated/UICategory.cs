using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using TMPro;
using System.Linq;
using UnityEngine.UI;

namespace Vortices
{
    public class UICategory : MonoBehaviour
    {
        // Other references
        [SerializeField] private TextMeshProUGUI nameText;
        [SerializeField] private CategoryController categoryController;

        [SerializeField] private CategoryRemoveButton removeButton;
        [SerializeField] private Toggle selectToggle;

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

            this.categoryController = categoryController;
            selectToggle.onValueChanged.AddListener(delegate {
                    categoryController.UnlockContinueButton();
                });
            removeButton.GetComponent<Button>().onClick.AddListener(delegate {
                    categoryController.UnlockContinueButton();
                });

            removeButton.category = this;
            removeButton.controller = this.categoryController;
        }

        public void SelectedToggle()
        {
            if (categoryController.selectedCategories.Contains(categoryName))
            {
                categoryController.selectedCategories.Remove(categoryName);
            }
            else
            {
                categoryController.selectedCategories.Add(categoryName);
                categoryController.selectedCategories = categoryController.selectedCategories.OrderBy(cat => cat).ToList();
            }
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

