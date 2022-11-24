using System.Collections;
using System.Collections.Generic;
using UnityEngine;


namespace Vortices
{
    public class CategoryRemoveButton : MonoBehaviour
    {
        public CategoryController controller;
        public Category category;

        public void RemoveCategory()
        {
            controller.RemoveCategory(category);
        }
    }
}


