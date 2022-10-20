using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using UnityEngine.XR.Interaction.Toolkit;

public class PlaneBase : MonoBehaviour
{
    List<XRRayInteractor> rayInteractors;

    private XRRayInteractor currentlySelecting;
    private bool drag;
    Vector3 offset = Vector3.zero;



    private void Start()
    {
        rayInteractors = new List<XRRayInteractor>();
        rayInteractors.Add(GameObject.Find("Ray Interactor Left").GetComponent<XRRayInteractor>());
        rayInteractors.Add(GameObject.Find("Ray Interactor Right").GetComponent<XRRayInteractor>());
    }


    private void Update()
    {
        if(drag)
        {
            Vector3 position;
            Vector3 normal;
            int positionInLine;
            bool isValidTarget;
            if (currentlySelecting.TryGetHitInfo(out position, out normal, out positionInLine, out isValidTarget))
            {
                position.z = transform.position.z;
                transform.position = position + offset;
            }
        }
    }

    public void MoveToCursor()
    {
        if (rayInteractors[0].isSelectActive)
        {
            currentlySelecting = rayInteractors[0];
        } 
        else if (rayInteractors[1].isSelectActive)
        {
            currentlySelecting = rayInteractors[1];
        }

        Vector3 position;
        Vector3 normal;
        int positionInLine;
        bool isValidTarget;
        if (currentlySelecting.TryGetHitInfo(out position, out normal, out positionInLine, out isValidTarget))
        {
            position.z = transform.position.z;
            offset = transform.position - position;
            drag = true;
        }
    }

    public void StopMoveToCursor()
    {
        currentlySelecting = null;
        drag = false;
        offset = Vector3.zero;
    } 

}
