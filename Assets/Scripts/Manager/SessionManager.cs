using System.Collections;
using System.Collections.Generic;
using UnityEngine;


namespace Vortices
{ 
    public class SessionManager : MonoBehaviour
    {

        // Settings
        public string sessionName { get; set; }
        public int userId { get; set; }

        // Controllers
        [SerializeField] public CategoryController categoryController;
        [SerializeField] public ElementCategoryController elementCategoryController;


        private void Awake()
        {
            // For now, Initialize with default sessionName and sessionId 0
            Initialize("default", 0);
        }

        public void Initialize(string sessionName, int userId)
        {
            this.sessionName = sessionName;
            this.userId = userId;
        }
    }
}

