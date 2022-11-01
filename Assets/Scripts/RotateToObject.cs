using UnityEngine;

namespace Vortices
{
    public class RotateToObject : MonoBehaviour
    {
        [SerializeField] private Transform followObject;

        public Vector3 offset;

        private float rotationSpeed = 3.0f;
        private bool follow;

        private void Start()
        {//TIENE QUE SEGUIR UN PUNTO NO MI CAMARA
            follow = true;
            if(followObject == null)
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

                transform.rotation = Quaternion.Slerp(transform.rotation, lookDirection, Time.deltaTime * rotationSpeed);
            }
        }
    }
}
