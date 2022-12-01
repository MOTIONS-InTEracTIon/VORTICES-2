using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

using UnityEngine.XR.Interaction.Toolkit;
using Vuplex.WebView;

namespace Vortices
{
    public abstract class SpawnBase : MonoBehaviour
    {
        // Other references
        protected List<XRRayInteractor> rayInteractors;
        protected List<GameObject> groupList;
        protected GameObject normalCollider;
        protected List<GameObject> followerCollider;
        public GameObject followerColliderPrefab;

        // SpawnBase Data Components
        [HideInInspector] public List<string> filePaths;
        [HideInInspector] public string rootUrl { get; set; }

        // Movement variables
        protected XRRayInteractor currentlySelecting;
        protected int globalIndex;
        protected bool lastLoadForward;
        protected Vector3 initialDragPos;
        protected Vector3 initialDragDist;
        protected bool hasAxis;
        public string dragDir;
        protected bool drag;
        protected Vector3 offset = Vector3.zero;

        // Settings
        [HideInInspector] public Vector3Int dimension { get; set; }
        public bool volumetric { get; set; }
        public string browsingMode { get; set; }
        public string displayMode { get; set; }
        public GameObject frontGroup;
        public float afterSpawnTime;
        public float spawnCooldownX = 1.0f;
        public float spawnCooldownZ = 1.0f;
        public float timeLerp = 1f;
        public float softFadeUpperAlpha = 0.6f;
        protected float movementOffset = 0.1f;

        // Bounds
        protected LayoutGroup3D layoutGroup;
        public Vector3 centerPosition;
        public Vector4 bounds; //PRIVATE
        protected float boundOffset = 0.001f;

        // Coroutine
        protected Queue<IEnumerator> coroutineQueue;
        protected bool coordinatorWorking;
        protected bool movingOperationRunning;

        // Auxiliary References
        protected SessionManager sessionManager;

        private void Awake()
        {
            rayInteractors = new List<XRRayInteractor>();
            rayInteractors.Add(GameObject.Find("Ray Interactor Left").GetComponent<XRRayInteractor>());
            rayInteractors.Add(GameObject.Find("Ray Interactor Right").GetComponent<XRRayInteractor>());

            sessionManager = GameObject.FindObjectOfType<SessionManager>();

            //Start Coroutine Coordinator
            coroutineQueue = new Queue<IEnumerator>();
            StartCoroutine(CoroutineCoordinator());
        }

        #region Group Spawn

        // Every base creates its first set of spawn groups differently
        public abstract void StartGenerateSpawnGroup();

        // Every base creates its control movement box collider differently
        protected abstract void SetMovementBoundBox();

        protected void MoveGlobalIndex(bool forwards)
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

        public IEnumerator DestroyBase()
        {
            int fadeCoroutinesRunning = 0;
            // Every group has to be alpha 0
            foreach (GameObject group in groupList)
            {
                Fade groupFader = group.GetComponent<Fade>();
                groupFader.lowerAlpha = 0;
                groupFader.upperAlpha = 1;
                TaskCoroutine fadeCoroutine = new TaskCoroutine(groupFader.FadeOutCoroutine());
                fadeCoroutine.Finished += delegate (bool manual)
                {
                    fadeCoroutinesRunning--;
                };
                fadeCoroutinesRunning++;
            }

            List<GameObject> controllers = GameObject.FindGameObjectsWithTag("Controller").ToList();
            if (controllers.Count > 0)
            {
                foreach (GameObject manager in controllers)
                {
                    Destroy(manager.gameObject);
                }
            }

            List<GameObject> externals = GameObject.FindGameObjectsWithTag("External").ToList();
            if (externals.Count > 0)
            {
                foreach (GameObject external in externals)
                {
                    Destroy(external.gameObject);
                }
            }

            while (fadeCoroutinesRunning > 0)
            {
                yield return null;
            }

            if (browsingMode == "Online")
            {
                yield return StartCoroutine(StandaloneWebView.TerminateBrowserProcess().AsIEnumerator());
                //Web.ClearAllData();
            }

            Destroy(gameObject);
        }

        #endregion

        #region Input

        private void Update()
        {
            // If spawn is done, this makes sure the cooldown applies
            if (afterSpawnTime < spawnCooldownZ)
            {
                afterSpawnTime += Time.deltaTime;
            }

            if (frontGroup != null)
            {
                if (drag)
                {
                    GetMotionInput();
                }
                // Execute action assigned to drag direction
                PerformAction();
            }
        }

        private void GetMotionInput()
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

        // Different bases do different things according to the actual dragDir
        protected abstract void PerformAction();

        // Initializes drag operation when grabbing base collider
        public void MoveToCursor(SelectEnterEventArgs args)
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

        // Disables drag operation and resets variables for the next drag operation
        public void StopMoveToCursor(SelectExitEventArgs args)
        {
            currentlySelecting = null;
            drag = false;
            offset = Vector3.zero;
            hasAxis = false;
            dragDir = "";
        }

        private IEnumerator CoroutineCoordinator()
        {
            while (true)
            {
                while (coroutineQueue.Count > 0)
                {
                    coordinatorWorking = true;
                    yield return StartCoroutine(coroutineQueue.Dequeue());
                    coordinatorWorking = false;
                }

                yield return null;

            }
        }

        #endregion

        #region Spawn Movement
        // Moves then spawns more multimedia
        protected virtual IEnumerator GroupSpawnRight()
        {
            // Different bases make multimedia go right differently
            yield return null;
        }

        protected virtual IEnumerator GroupSpawnLeft()
        {
            // Different bases make multimedia go left differently
            yield return null;
        }

        protected virtual IEnumerator GroupSpawnPull()
        {
            // Different bases make multimedia go into the foreground differently
            yield return null;
        }

        protected virtual IEnumerator GroupSpawnPush()
        {
            // Different bases make multimedia go onto the background differently
            yield return null;
        }

        protected virtual IEnumerator GroupSpawnUp()
        {
            // Different bases make multimedia go up differently
            yield return null;
        }

        protected virtual IEnumerator GroupSpawnDown()
        {
            // Different bases make multimedia go down differently
            yield return null;
        }



        #endregion

        #region Movement
        // Only moves
        protected virtual IEnumerator GroupRight()
        {
            // Different bases make multimedia go right differently
            yield return null;
        }

        protected virtual IEnumerator GroupLeft()
        {
            // Different bases make multimedia go left differently
            yield return null;
        }

        protected virtual IEnumerator GroupPull()
        {
            // Different bases make multimedia go into the foreground differently
            yield return null;
        }

        protected virtual IEnumerator GroupPush()
        {
            // Different bases make multimedia go onto the background differently
            yield return null;
        }

        protected virtual IEnumerator GroupUp()
        {
            // Different bases make multimedia go up differently
            yield return null;
        }

        protected virtual IEnumerator GroupDown()
        {
            // Different bases make multimedia go down differently
            yield return null;
        }


        protected void LogMovement(string movementDir)
        {
            string controller = "";
            if (currentlySelecting.gameObject == rayInteractors[0].gameObject)
            {
                controller = "Left Hand Controller";
            }
            else if (currentlySelecting.gameObject == rayInteractors[1].gameObject)
            {
                controller = "Right Hand Controller";
            }
            sessionManager.loggingController.LogMovement(movementDir, controller);
        }

        #endregion
    }
}
