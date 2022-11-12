using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using UnityEngine.XR.Interaction.Toolkit;
using Vuplex.WebView;

namespace Vortices
{
    public class SpawnBase : MonoBehaviour
    {
        // Other references
        protected List<XRRayInteractor> rayInteractors;
        protected List<GameObject> groupList;

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

        // Coroutine
        protected Queue<IEnumerator> coroutineQueue;
        protected bool coordinatorWorking;
        protected bool movingOperationRunning;

        private void Start()
        {
            rayInteractors = new List<XRRayInteractor>();
            rayInteractors.Add(GameObject.Find("Ray Interactor Left").GetComponent<XRRayInteractor>());
            rayInteractors.Add(GameObject.Find("Ray Interactor Right").GetComponent<XRRayInteractor>());

            //Start Coroutine Coordinator
            coroutineQueue = new Queue<IEnumerator>();
            StartCoroutine(CoroutineCoordinator());
        }

        #region Group Spawn

        // Every base creates its first set of spawn groups differently
        public virtual void StartGenerateSpawnGroup()
        {
            Debug.Log("Start Generate Spawn Group was not overriden");
        }

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

            while (fadeCoroutinesRunning > 0)
            {
                yield return null;
            }

            if (browsingMode == "Online")
            {
                yield return StartCoroutine(StandaloneWebView.TerminateBrowserProcess().AsIEnumerator());
                Web.ClearAllData();
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
        protected virtual void PerformAction()
        {
            // Different bases do different things according to the actual dragDir
            Debug.Log("Perform Action was not overriden");
        }

        // Initializes drag operation when grabbing base collider
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

        // Disables drag operation and resets variables for the next drag operation
        public void StopMoveToCursor()
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
            Debug.Log("Group Spawn Right was not overriden");
            yield return null;
        }

        protected virtual IEnumerator GroupSpawnLeft()
        {
            // Different bases make multimedia go left differently
            Debug.Log("Group Spawn Left was not overriden");
            yield return null;
        }

        protected virtual IEnumerator GroupSpawnPull()
        {
            // Different bases make multimedia go into the foreground differently
            Debug.Log("Group Spawn Pull was not overriden");
            yield return null;
        }

        protected virtual IEnumerator GroupSpawnPush()
        {
            // Different bases make multimedia go onto the background differently
            Debug.Log("Group Spawn Push was not overriden");
            yield return null;
        }

        protected virtual IEnumerator GroupSpawnUp()
        {
            // Different bases make multimedia go up differently
            Debug.Log("Group Spawn Up was not overriden");
            yield return null;
        }

        protected virtual IEnumerator GroupSpawnDown()
        {
            // Different bases make multimedia go down differently
            Debug.Log("Group Spawn Down was not overriden");
            yield return null;
        }



        #endregion

        #region Movement
        // Only moves
        protected virtual IEnumerator GroupRight()
        {
            // Different bases make multimedia go right differently
            Debug.Log("Group Right was not overriden");
            yield return null;
        }

        protected virtual IEnumerator GroupLeft()
        {
            // Different bases make multimedia go left differently
            Debug.Log("Group Left was not overriden");
            yield return null;
        }

        protected virtual IEnumerator GroupPull()
        {
            // Different bases make multimedia go into the foreground differently
            Debug.Log("Group Pull was not overriden");
            yield return null;
        }

        protected virtual IEnumerator GroupPush()
        {
            // Different bases make multimedia go onto the background differently
            Debug.Log("Group Push was not overriden");
            yield return null;
        }

        protected virtual IEnumerator GroupUp()
        {
            // Different bases make multimedia go up differently
            Debug.Log("Group Up was not overriden");
            yield return null;
        }

        protected virtual IEnumerator GroupDown()
        {
            // Different bases make multimedia go down differently
            Debug.Log("Group Down was not overriden");
            yield return null;
        }
        #endregion
    }
}
