using UnityEngine;

namespace Vortices
{
    public class RotateToObject : MonoBehaviour
    {
        [SerializeField] private Transform followObject;

        private bool follow;

        // Settings
        public Vector3 offset;
        public string followName = "";


        private void Start()
        {//TIENE QUE SEGUIR UN PUNTO NO MI CAMARA
            follow = true;

            if (followName != "")
            {
                followObject = GameObject.Find(followName).transform;
            }
            else if (followObject == null)
            {
                followObject = Camera.main.gameObject.transform;
            }

        }

        private void Update()
        {
            if(follow)
            {
                Quaternion lookRotation = Quaternion.LookRotation(followObject.position - transform.position);
                Quaternion lookDirection = lookRotation * Quaternion.Euler(offset.x, offset.y, offset.z);
                transform.rotation = lookDirection;
            }
        }
    }
}
