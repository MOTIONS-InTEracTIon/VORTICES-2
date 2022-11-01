using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;

namespace Vortices
{
    public class PlaneBase : MonoBehaviour
    {
        // Other references
        private List<GameObject> planeList;
        [SerializeField] private GameObject linearRail;
        public GameObject planeGroupPrefab;
        List<XRRayInteractor> rayInteractors;
        Rigidbody rb;

        // SpawnBase Data Components
        [HideInInspector] public List<string> filePaths;

        // Movement variables
        private XRRayInteractor currentlySelecting;
        private Vector3 initialDragPos;
        private Vector3 initialDragDist;
        private bool hasAxis;
        public string dragDir;
        private bool lerpToPositionRunning;
        private bool drag;
        Vector3 offset = Vector3.zero;
        private int globalIndex;
        private bool lastLoadForward;

        // Bounds
        private LayoutGroup3D layoutGroup;
        private BoxCollider boxCollider;
        public Vector3 centerPosition;
        public Vector4 bounds; //PRIVATE
        private float boxBoundsize = 0.0001f;
        private float boundZOffset = 0.001f;
        public float movementOffset = 0.1f;

        // Settings
        public bool volumetric { get; set; }
        [HideInInspector] public Vector3Int dimension;
        public float afterSpawnTime;
        public float spawnCooldownX = 1.0f;
        public float spawnCooldownZ = 3f;
        public float timeLerp = 1f;
        public GameObject frontPlane;

        // Coroutine
        protected Queue<IEnumerator> coroutineQueue;
        private int spawnCoroutinesRunning;
        protected bool coordinatorWorking;
        private bool movingOperationRunning;

        private void Start()
        {
            rayInteractors = new List<XRRayInteractor>();
            rayInteractors.Add(GameObject.Find("Ray Interactor Left").GetComponent<XRRayInteractor>());
            rayInteractors.Add(GameObject.Find("Ray Interactor Right").GetComponent<XRRayInteractor>());
            rb = GetComponent<Rigidbody>();

            //Start Coroutine Coordinator
            coroutineQueue = new Queue<IEnumerator>();
            StartCoroutine(CoroutineCoordinator());
        }

        #region Group Spawn
        public void StartGenerateSpawnGroup()
        {
            globalIndex = -1;
            lastLoadForward = true;
            // Rail Generation
            linearRail = Instantiate(linearRail, transform.position, transform.rotation, transform);
            // Plane Generation
            planeList = new List<GameObject>();
            for(int i = 0; i < dimension.z; i++)
            {
                GameObject gameObject = Instantiate(planeGroupPrefab, transform.position, transform.rotation, linearRail.transform);
                planeList.Add(gameObject);
                if (i == 0)
                {
                    frontPlane = gameObject;
                }
                else
                {
                    MoveGlobalIndex(true);
                }
                PlaneGroup spawnGroup = gameObject.GetComponent<PlaneGroup>();
                spawnGroup.filePaths = filePaths;
                spawnGroup.dimension = dimension;
                StartCoroutine(spawnGroup.StartSpawnOperation(globalIndex));
            }

            SetMovementBoundBox();
        }

        private void SetMovementBoundBox()
        {
            // Uses first plane layout to set bound box
            layoutGroup = frontPlane.GetComponent<LayoutGroup3D>();
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
        }

        public IEnumerator StopSpawn()
        {
            Fade planeFader = GetComponent<Fade>();
            yield return StartCoroutine(planeFader.FadeOutCoroutine());
            Destroy(gameObject);
        }

        #endregion

        #region Movement

        private void Update()
        {
            // If spawn is done, this makes sure the cooldown applies
            if (afterSpawnTime < spawnCooldownZ)
            {
                afterSpawnTime += Time.deltaTime;
            }

            // This makes sure elements only move in bounds even if user is not dragging
            if ((frontPlane != null && CheckBounds()) || drag)
            {
                if (drag)
                {
                    Vector3 position; Vector3 normal; int positionInLine; bool isValidTarget;
                    if (currentlySelecting.TryGetHitInfo(out position, out normal, out positionInLine, out isValidTarget))
                    {
                        // This makes sure the dragging of elements is in one axis only
                        if (!hasAxis)
                        {
                            Vector3 distanceXY = position - initialDragPos;
                            Vector3 distanceZ = position - currentlySelecting.rayOriginTransform.position;

                            if (distanceZ.magnitude > (initialDragDist.magnitude + movementOffset))
                            {
                                hasAxis = true;
                                dragDir = "Pull";
                            }
                            else if (distanceZ.magnitude < (initialDragDist.magnitude - movementOffset))
                            {
                                hasAxis = true;
                                dragDir = "Push";
                            }
                            else if (Mathf.Abs(distanceXY.x) > Mathf.Abs(distanceXY.y))
                            {
                                if (distanceXY.x < -movementOffset)
                                {
                                    dragDir = "Left";
                                    hasAxis = true;
                                }
                                else if (distanceXY.x > movementOffset)
                                {
                                    dragDir = "Right";
                                    hasAxis = true;
                                }
                            }
                            else if (Mathf.Abs(distanceXY.x) < Mathf.Abs(distanceXY.y))
                            {
                                if (distanceXY.y < -movementOffset)
                                {
                                    dragDir = "Down";
                                    hasAxis = true;
                                }
                                else if (distanceXY.y > movementOffset)
                                {
                                    dragDir = "Up";
                                    hasAxis = true;
                                }
                            }
                        }
                    }
                }
            }
        }

        // Checks even when user is not dragging and eliminates chance of re-dragging (As there is no drag)
        private bool CheckBounds()
        {
            bool canMove = false;
            Vector3 center = frontPlane.transform.position;
            // This means the base has been pulled and will spawn inwards
            if(volumetric && drag && dragDir == "Pull")
            {
                if (afterSpawnTime >= spawnCooldownZ && drag && !movingOperationRunning)
                {
                    afterSpawnTime = 0;
                    coroutineQueue.Enqueue(GroupSpawnPull());
                }
            }
            // This means the base has been pushed and will spawn outwards
            else if (volumetric && drag && dragDir == "Push")
            {
                if (afterSpawnTime >= spawnCooldownZ && drag && !movingOperationRunning)
                {
                    afterSpawnTime = 0;
                    coroutineQueue.Enqueue(GroupSpawnPush());
                }
            }
            // This means the base has touched the left bound and will spawn
            else if (drag && dragDir == "Left")
            {
                if (afterSpawnTime >= spawnCooldownX && drag && !movingOperationRunning)
                {
                    afterSpawnTime = 0;
                    coroutineQueue.Enqueue(GroupSpawnForwards());
                }
            }
            // This means the base has touched the right bound and will spawn
            else if (drag && dragDir == "Right")
            {
                if (afterSpawnTime >= spawnCooldownX && drag && !movingOperationRunning)
                {
                    afterSpawnTime = 0;
                    coroutineQueue.Enqueue(GroupSpawnBackwards());
                }
            }
            else if (drag && dragDir == "Up" && ((center.y + boundZOffset) < bounds.x && (center.y - boundZOffset) < bounds.x))
            {
                if (afterSpawnTime >= spawnCooldownX && !lerpToPositionRunning)
                {
                    afterSpawnTime = 0;
                    StartCoroutine(LerpToPosition(dragDir));
                }
            }
            else if (drag && dragDir == "Down" && ((center.y + boundZOffset) > bounds.z && (center.y - boundZOffset) > bounds.z))
            {
                if (afterSpawnTime >= spawnCooldownX && !lerpToPositionRunning)
                {
                    afterSpawnTime = 0;
                    StartCoroutine(LerpToPosition(dragDir));
                }
            }
            else
            {
                canMove = true;
            }

            return canMove;
        }

        // Enables movement on a currently selected plane
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
                initialDragDist = currentlySelecting.rayOriginTransform.position - position;
                position.z = transform.position.z;
                offset = transform.position - position;
                drag = true;
            }
        }

        // Executes a lerp order for every multimedia element in a panel
        public IEnumerator LerpToPosition(string moveDir)
        {
            if (frontPlane != null)
            {
                lerpToPositionRunning = true;
                Vector3 position = Vector3.zero;
                if (moveDir == "Up")
                {
                    position = new Vector3(frontPlane.transform.position.x, frontPlane.transform.position.y + layoutGroup.ElementDimensions.y + layoutGroup.Spacing, frontPlane.transform.position.z);
                }
                else if (moveDir == "Down")
                {
                    position = new Vector3(frontPlane.transform.position.x, frontPlane.transform.position.y - layoutGroup.ElementDimensions.y - layoutGroup.Spacing, frontPlane.transform.position.z);
                }

                float timeElapsed = 0;
                while (timeElapsed < spawnCooldownX)
                {
                    timeElapsed += Time.deltaTime;
                    frontPlane.transform.position = Vector3.Lerp(frontPlane.transform.position, position, timeElapsed / timeLerp);
                    centerPosition.y = frontPlane.transform.position.y;
                    boxCollider.center = frontPlane.transform.localPosition;
                    yield return null;
                }
                lerpToPositionRunning = false;
            }
        }

        // Resets variables for the next drag operation
        public void StopMoveToCursor()
        {
            currentlySelecting = null;
            drag = false;
            offset = Vector3.zero;
            hasAxis = false;
            dragDir = "";
            //rb.constraints = RigidbodyConstraints.None | RigidbodyConstraints.FreezeRotationZ | RigidbodyConstraints.FreezeRotationY | RigidbodyConstraints.FreezeRotationX;
        }

        private IEnumerator CoroutineCoordinator()
        {
            while (true)
            {
                while (coroutineQueue.Count > 0)
                {
                    coordinatorWorking = true;
                    Debug.Log(coordinatorWorking);
                    yield return StartCoroutine(coroutineQueue.Dequeue());
                    coordinatorWorking = false;
                    Debug.Log(coordinatorWorking);
                }
                yield return null;

            }
        }
        #endregion

        #region Multimedia Spawn

        private void MoveGlobalIndex(bool forwards)
        {
            if (forwards)
            {
                if (!lastLoadForward)
                {
                    globalIndex += dimension.x * dimension.y * dimension.z;
                }
                else
                {
                    globalIndex += dimension.x * dimension.y;
                }
                lastLoadForward = true;
            }
            else
            {
                if (lastLoadForward)
                {
                    globalIndex -= dimension.x * dimension.y * dimension.z;
                }
                else
                {
                    globalIndex -= dimension.x * dimension.y;
                }
                lastLoadForward = false;
            }
        }

        private IEnumerator GroupSpawnForwards()
        {
            movingOperationRunning = true;
            int spawnCoroutinesRunning = 0;

            for (int i = 0; i < dimension.z; i++)
            {
                PlaneGroup planeGroup = planeList[i].GetComponent<PlaneGroup>();
                Task spawnCoroutine = new Task(planeGroup.SpawnForwards(dimension.y));
                spawnCoroutine.Finished += delegate (bool manual) { spawnCoroutinesRunning--; };
                spawnCoroutinesRunning++;
            }
            globalIndex += dimension.y;

            while (spawnCoroutinesRunning > 0)
            {
                yield return null;
            }

            movingOperationRunning = false;
        }

        private IEnumerator GroupSpawnBackwards()
        {
            movingOperationRunning = true;
            int spawnCoroutinesRunning = 0;

            for (int i = 0; i < dimension.z; i++)
            {
                PlaneGroup planeGroup = planeList[i].GetComponent<PlaneGroup>();
                Task spawnCoroutine = new Task(planeGroup.SpawnBackwards(dimension.y));
                spawnCoroutine.Finished += delegate(bool manual) { spawnCoroutinesRunning--; };
                spawnCoroutinesRunning++;
            }

            globalIndex -= dimension.y;
            
            while (spawnCoroutinesRunning > 0)
            {
                yield return null;
            }

            movingOperationRunning = false;
        }

        private IEnumerator GroupSpawnPull()
        {
            movingOperationRunning = true;
            // Destroy group in front
            GameObject planeInFront = planeList[0];
            planeList.Remove(planeInFront);
            Destroy(planeInFront.transform.gameObject);
            planeInFront.transform.parent = null;
            // Change global Index
            MoveGlobalIndex(true);
            // Spawn group in back
            GameObject gameObject = Instantiate(planeGroupPrefab, transform.position, transform.rotation, linearRail.transform);
            planeList.Add(gameObject);
            PlaneGroup spawnGroup = gameObject.GetComponent<PlaneGroup>();
            spawnGroup.filePaths = filePaths;
            spawnGroup.dimension = dimension;
            yield return StartCoroutine(spawnGroup.StartSpawnOperation(globalIndex));
        
            frontPlane = planeList[0];
            SetMovementBoundBox();

            yield return new WaitForSeconds(spawnCooldownZ);

            movingOperationRunning = false;
        }

        private IEnumerator GroupSpawnPush()
        {
            movingOperationRunning = true;
            // Change global Index
            MoveGlobalIndex(false);
            // Spawn group in front
            GameObject gameObject = Instantiate(planeGroupPrefab, transform.position - new Vector3(0, 0, 1f), transform.rotation, linearRail.transform);
            gameObject.transform.SetSiblingIndex(0);
            planeList.Insert(0, gameObject);
            PlaneGroup spawnGroup = gameObject.GetComponent<PlaneGroup>();
            spawnGroup.filePaths = filePaths;
            spawnGroup.dimension = dimension;
            yield return StartCoroutine(spawnGroup.StartSpawnOperation(globalIndex));

            frontPlane = gameObject;
            SetMovementBoundBox();

            // Destroy group in back
            GameObject planeInBack = planeList[planeList.Count - 1];
            planeList.Remove(planeInBack);
            Destroy(planeInBack.transform.gameObject);
            planeInBack.transform.parent = null;

            yield return new WaitForSeconds(spawnCooldownZ);
            
            movingOperationRunning = false;
        }

        #endregion

    }
}
