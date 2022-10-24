using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using UnityEngine.XR.Interaction.Toolkit;
using System;

public class PlaneBase : SpawnBase
{
    #region Variables and properties
    // Movement variables
    List<XRRayInteractor> rayInteractors;
    Rigidbody rb;

    private XRRayInteractor currentlySelecting;
    private Vector3 initialDragPos;
    private bool hasAxis;
    public string dragDir;
    private bool lerpToPositionRunning;
    private bool drag;
    private bool spring;
    Vector3 offset = Vector3.zero;

    // Bounds
    private LayoutGroup3D layoutGroup;
    private BoxCollider boxCollider;
    public Vector3 centerPosition;
    public Vector4 bounds; //PRIVATE
    private float boxBoundsize = 0.0001f;
    private float boundOffset = 0.001f;

    // Settings
    public float afterSpawnTime = 0;
    public float spawnCooldown = 1.0f;
    public float moveSpeed = 1.0f;

    #endregion

    private void Start()
    {
        rayInteractors = new List<XRRayInteractor>();
        rayInteractors.Add(GameObject.Find("Ray Interactor Left").GetComponent<XRRayInteractor>());
        rayInteractors.Add(GameObject.Find("Ray Interactor Right").GetComponent<XRRayInteractor>());
        rb = GetComponent<Rigidbody>();

        // Starting layout settings
        layoutGroup = GetComponent<LayoutGroup3D>();
        if(volumetric)
        {

        }
        else
        {
            layoutGroup.Style = LayoutStyle.Grid;
            layoutGroup.GridConstraintCount = dimension.y;
        }

        // Generates Collider Box for moving
        boxCollider = GetComponent<BoxCollider>();
        boxCollider.center = Vector3.zero;
        boxCollider.size = new Vector3((layoutGroup.ElementDimensions.x + layoutGroup.Spacing) * dimension.x, (layoutGroup.ElementDimensions.y + layoutGroup.Spacing) * dimension.y, 0.001f);
        // Generates bounds using dimension given (Box from the left side to its down side)
        centerPosition = transform.position;
        bounds.w = -boxBoundsize;
        bounds.x = centerPosition.y + (layoutGroup.ElementDimensions.y + layoutGroup.Spacing) * ((dimension.y - 1) / 2);
        bounds.y = boxBoundsize;
        bounds.z = centerPosition.y - (layoutGroup.ElementDimensions.y + layoutGroup.Spacing) * ((dimension.y - 1) / 2);
        //Start Coroutine Coordinator
        coroutineQueue = new Queue<IEnumerator>();
        StartCoroutine(CoroutineCoordinator());
    }

    #region Movement

    private void Update()
    {
        // If spawn is done, this makes sure the cooldown applies
        if (afterSpawnTime < spawnCooldown)
        {
            afterSpawnTime += Time.deltaTime;
        }

        // This makes sure elements only move in bounds even if user is not dragging
        if ((CheckBounds()) || drag)
        {
            if (drag)
            {
                Vector3 position; Vector3 normal; int positionInLine; bool isValidTarget;
                if (currentlySelecting.TryGetHitInfo(out position, out normal, out positionInLine, out isValidTarget))
                {
                    // This makes sure the dragging of elements is in one axis only
                    if (!hasAxis)
                    {
                        Vector3 distance = initialDragPos - position;
                        if (Mathf.Abs(distance.x) > Mathf.Abs(distance.y))
                        {
                            rb.constraints = RigidbodyConstraints.FreezePositionY;
                            hasAxis = true;
                            if(distance.x > 0)
                            {
                                dragDir = "Left";
                            }
                            else
                            {
                                dragDir = "Right";
                            }
                        }
                        else if (Mathf.Abs(distance.x) < Mathf.Abs(distance.y))
                        {
                            rb.constraints = RigidbodyConstraints.FreezePositionX;
                            hasAxis = true;
                            if (distance.y > 0)
                            {
                                dragDir = "Down";
                            }
                            else
                            {
                                dragDir = "Up";
                            }
                        }
                    }
                }
                // This makes sure the elements only move in bounds
                if (dragDir == "Left" || dragDir == "Right")
                {
                    position.z = transform.position.z;
                    position.y = transform.position.y;
                    rb.MovePosition(position + offset);
                }

            }
        }
    }
    
    // Checks if the multimedia bas has touched any bounds so it can stop it or spawn a new set of multimedia, returns true if movement is permitted
    private bool CheckBounds(Vector3 nextPosition)
    {
        bool canMove = false;
        Vector3 center = transform.position;
        // Sides have to be evaluated first
        if((rb.constraints & RigidbodyConstraints.FreezePositionY) != RigidbodyConstraints.None)
        {
            // This means the base has touched the left bound and will spawn
            if (center.x <= bounds.w)
            {
                rb.velocity = Vector3.zero;
                Vector3 newPosition = new Vector3(bounds.w, transform.position.y, transform.position.z);
                transform.position = newPosition;
            }
            // This means the base has touched the right bound and will spawn
            else if (center.x >= bounds.y)
            {
                rb.velocity = Vector3.zero;
                Vector3 newPosition = new Vector3(bounds.y, transform.position.y, transform.position.z);
                transform.position = newPosition;
            }
            else
            {
                canMove = true;
            }
        }
        else if ((rb.constraints & RigidbodyConstraints.FreezePositionX) != RigidbodyConstraints.None)
        {
            // This means the base has touched the upper bound and will stop
            if (center.y >= bounds.x)
            {
                rb.velocity = Vector3.zero;
                Vector3 newPosition = new Vector3(transform.position.x, bounds.x, transform.position.z);
                transform.position = newPosition;

            }
            // This means the base has touched the lower bound and will stop
            else if (center.y <= bounds.z)
            {
                rb.velocity = Vector3.zero;
                Vector3 newPosition = new Vector3(transform.position.x, bounds.z, transform.position.z);
                transform.position = newPosition;
            }
            else
            {
                canMove = true;
            }
        }

        return canMove;
    }
    // Checks even when user is not dragging and eliminates chance of re-dragging (As there is no drag)
    private bool CheckBounds()
    {
        bool canMove = false;
        Vector3 center = transform.position;
        // This means the base has touched the left bound and will spawn
        if (center.x <= bounds.w)
        {
            rb.velocity = Vector3.zero;
            transform.position = centerPosition;
            if (afterSpawnTime >= spawnCooldown)
            {
                afterSpawnTime = 0;
                coroutineQueue.Enqueue(SpawnForwards(dimension.y));
            }
        }
        // This means the base has touched the right bound and will spawn
        else if (center.x >= bounds.y)
        {
            rb.velocity = Vector3.zero;
            transform.position = centerPosition;
            if (afterSpawnTime >= spawnCooldown && drag)
            {
                afterSpawnTime = 0;
                coroutineQueue.Enqueue(SpawnBackwards(dimension.y));
            }
        }
        else if (drag && dragDir == "Up" && ((center.y + boundOffset) < bounds.x && (center.y - boundOffset) < bounds.x))
        {
            if (afterSpawnTime >= spawnCooldown && !lerpToPositionRunning)
            {
                afterSpawnTime = 0;
                coroutineQueue.Enqueue(LerpToPosition(dragDir));
            }
        }
        else if (drag && dragDir == "Down" && ((center.y + boundOffset) > bounds.z && (center.y - boundOffset) > bounds.z))
            {
            if (afterSpawnTime >= spawnCooldown && !lerpToPositionRunning)
            {
                afterSpawnTime = 0;
                coroutineQueue.Enqueue(LerpToPosition(dragDir));
            }
        }
        else
        {
            canMove = true;
        }

        return canMove;
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
            initialDragPos = position;
            position.z = transform.position.z;
            offset = transform.position - position;
            drag = true;
        }
    }

    public IEnumerator LerpToPosition(string moveDir)
    {
        lerpToPositionRunning = true;
        Vector3 position = Vector3.zero;
        if(moveDir == "Up")
        {
            position = new Vector3(transform.position.x, transform.position.y + layoutGroup.ElementDimensions.y + layoutGroup.Spacing, transform.position.z);
        }
        else if (moveDir == "Down")
        {
            position = new Vector3(transform.position.x, transform.position.y - layoutGroup.ElementDimensions.y - layoutGroup.Spacing, transform.position.z);
        }

        float timeElapsed = 0;
        while (timeElapsed < spawnCooldown)
        {
            timeElapsed += Time.deltaTime;
            transform.position = Vector3.Lerp(transform.position, position, timeElapsed / spawnCooldown);
            yield return null;
        }
        centerPosition.y = transform.position.y;
        lerpToPositionRunning = false;
    }

    public void StopMoveToCursor()
    {
        currentlySelecting = null;
        drag = false;
        offset = Vector3.zero;
        hasAxis = false;
        dragDir = "";
        rb.constraints = RigidbodyConstraints.None | RigidbodyConstraints.FreezeRotationZ | RigidbodyConstraints.FreezeRotationY | RigidbodyConstraints.FreezeRotationX;
    }
    #endregion

    #region Multimedia Spawn
    public override void GenerateObjectPlacement(int loadNumber, bool forwards)
    {
        loadObjects = new List<GameObject>();

        for (int i = 0; i < loadNumber; i++)
        {
            GameObject positionObject = new GameObject();
            loadObjects.Add(positionObject);
            if (forwards)
            {
                positionObject.transform.parent = transform;
                positionObject.transform.SetAsFirstSibling();
            }
            else
            {
                positionObject.transform.parent = transform;
            }
        }
    }

    public override void GenerateDestroyObjects(int unloadNumber, bool forwards)
    {
        for(int i = 0; i < unloadNumber; i++)
        {
            if (forwards)
            {
                unloadObjects.Add(transform.GetChild(i).gameObject);
            }
            else
            {
                unloadObjects.Add(transform.GetChild(transform.childCount - i - 1).gameObject);
            }
        }

    }

    #endregion

}
