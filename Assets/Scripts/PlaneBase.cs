using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Vortices
{
    public class PlaneBase : SpawnBase
    {
        // Other references
        [SerializeField] private GameObject linearRailPrefab;
        private GameObject linearRail;
        public GameObject planeGroupPrefab;

        // Movement variables
        private bool lerpToPositionRunning;

        // Bounds
        private BoxCollider boxCollider;
        private float boxBoundsize = 0.0001f;
        protected LayoutGroup3D layoutGroup;
        public Vector3 centerPosition;
        public Vector4 bounds; //PRIVATE
        protected float boundZOffset = 0.001f;

        #region Group Spawn
        public override void StartGenerateSpawnGroup()
        {
            globalIndex = -1;
            lastLoadForward = true;
            // Linear Rail Generation
            linearRail = Instantiate(linearRailPrefab, transform.position, transform.rotation, transform);
            LayoutGroup3D railLayout = linearRail.GetComponent<LayoutGroup3D>();
            railLayout.PrimaryAlignment = Alignment.Min;
            railLayout.LayoutAxis = LayoutAxis3D.Z;
            // Plane Group Generation
            groupList = new List<GameObject>();
            for(int i = 0; i < dimension.z; i++)
            {
                GameObject gameObject = Instantiate(planeGroupPrefab, transform.position, transform.rotation, linearRail.transform);
                groupList.Add(gameObject);
                PlaneGroup spawnGroup = gameObject.GetComponent<PlaneGroup>();
                spawnGroup.filePaths = filePaths;
                spawnGroup.dimension = dimension;
                spawnGroup.softFadeUpperAlpha = softFadeUpperAlpha;
                bool softFadeIn = true;
                if (i == 0)
                {
                    // Front Group Operations
                    frontGroup = gameObject;
                    softFadeIn = false;
                }
                else
                {
                    MoveGlobalIndex(true);
                }
                StartCoroutine(spawnGroup.StartSpawnOperation(globalIndex, softFadeIn));
            }

            SetMovementBoundBox();
        }

        private void SetMovementBoundBox()
        {
            // Uses first plane layout to set bound box
            layoutGroup = frontGroup.GetComponent<LayoutGroup3D>();
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

        #endregion

        #region Movement
        protected override void PerformAction()
        {
            if (drag)
            {
                Vector3 center = frontGroup.transform.position;
                // This means the base has been pulled and will spawn inwards
                if (volumetric && dragDir == "Pull")
                {
                    if (afterSpawnTime >= spawnCooldownZ && !movingOperationRunning)
                    {
                        afterSpawnTime = 0;
                        coroutineQueue.Enqueue(GroupSpawnPull());
                    }
                }
                // This means the base has been pushed and will spawn outwards
                else if (volumetric && dragDir == "Push")
                {
                    if (afterSpawnTime >= spawnCooldownZ && drag && !movingOperationRunning)
                    {
                        afterSpawnTime = 0;
                        coroutineQueue.Enqueue(GroupSpawnPush());
                    }
                }
                // This means the base has touched the left bound and will spawn
                else if (dragDir == "Left")
                {
                    if (afterSpawnTime >= spawnCooldownX && !movingOperationRunning)
                    {
                        afterSpawnTime = 0;
                        coroutineQueue.Enqueue(GroupSpawnForwards());
                    }
                }
                // This means the base has touched the right bound and will spawn
                else if (dragDir == "Right")
                {
                    if (afterSpawnTime >= spawnCooldownX && !movingOperationRunning)
                    {
                        afterSpawnTime = 0;
                        coroutineQueue.Enqueue(GroupSpawnBackwards());
                    }
                }
                else if (dragDir == "Up" &&
                         ((center.y + boundZOffset) < bounds.x && (center.y - boundZOffset) < bounds.x))
                {
                    if (afterSpawnTime >= spawnCooldownX && !lerpToPositionRunning)
                    {
                        afterSpawnTime = 0;
                        StartCoroutine(LerpToPosition(dragDir));
                    }
                }
                else if (dragDir == "Down" &&
                         ((center.y + boundZOffset) > bounds.z && (center.y - boundZOffset) > bounds.z))
                {
                    if (afterSpawnTime >= spawnCooldownX && !lerpToPositionRunning)
                    {
                        afterSpawnTime = 0;
                        StartCoroutine(LerpToPosition(dragDir));
                    }
                }
            }
        }

        // Not Spawning movement
        public IEnumerator LerpToPosition(string moveDir)
        {
            if(frontGroup != null)
            {
                lerpToPositionRunning = true;
                Vector3 position = Vector3.zero;
                if (moveDir == "Up")
                {
                    position = new Vector3(frontGroup.transform.position.x, frontGroup.transform.position.y + layoutGroup.ElementDimensions.y + layoutGroup.Spacing, frontGroup.transform.position.z);
                }
                else if (moveDir == "Down")
                {
                    position = new Vector3(frontGroup.transform.position.x, frontGroup.transform.position.y - layoutGroup.ElementDimensions.y - layoutGroup.Spacing, frontGroup.transform.position.z);
                }

                float timeElapsed = 0;
                while (timeElapsed < timeLerp)
                {
                    timeElapsed += Time.deltaTime;
                    frontGroup.transform.position = Vector3.Lerp(frontGroup.transform.position, position, timeElapsed / timeLerp);
                    centerPosition.y = frontGroup.transform.position.y;
                    boxCollider.center = frontGroup.transform.localPosition;
                    yield return null;
                }
                lerpToPositionRunning = false;
            }
        }
        
        #endregion

        #region Multimedia Spawn
        protected override IEnumerator GroupSpawnForwards()
        {
            movingOperationRunning = true;
            int spawnCoroutinesRunning = 0;

            for (int i = 0; i < dimension.z; i++)
            {
                PlaneGroup planeGroup = groupList[i].GetComponent<PlaneGroup>();
                bool softFadeIn = true;
                if (i == 0)
                {
                    softFadeIn = false;
                }
                TaskCoroutine spawnCoroutine = new TaskCoroutine(planeGroup.SpawnForwards(dimension.y, softFadeIn));
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

        protected override IEnumerator GroupSpawnBackwards()
        {
            movingOperationRunning = true;
            int spawnCoroutinesRunning = 0;

            for (int i = 0; i < dimension.z; i++)
            {
                PlaneGroup planeGroup = groupList[i].GetComponent<PlaneGroup>();
                bool softFadeIn = true;
                if (i == 0)
                {
                    softFadeIn = false;
                }
                TaskCoroutine spawnCoroutine = new TaskCoroutine(planeGroup.SpawnBackwards(dimension.y, softFadeIn));
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

        protected override IEnumerator GroupSpawnPull()
        {
            movingOperationRunning = true;
            // Destroy group in front
            GameObject planeInFront = groupList[0];
            groupList.Remove(planeInFront);
            Destroy(planeInFront.transform.gameObject);
            planeInFront.transform.parent = null;
            // Front Group Operations
            frontGroup = groupList[0];
            SetMovementBoundBox();
            // Front group has to be fade alpha 1
            Fade frontGroupFader = frontGroup.gameObject.GetComponent<Fade>();
            frontGroupFader.lowerAlpha = softFadeUpperAlpha;
            frontGroupFader.upperAlpha = 1;
            int fadeCoroutinesRunning = 0;
            TaskCoroutine fadeCoroutine = new TaskCoroutine(frontGroupFader.FadeInCoroutine());
            fadeCoroutine.Finished += delegate (bool manual)
            {
                fadeCoroutinesRunning--;
            };
            fadeCoroutinesRunning++;
            // Change global Index
            MoveGlobalIndex(true);
            // Spawn group in back
            GameObject gameObject = Instantiate(planeGroupPrefab, transform.position, transform.rotation, linearRail.transform);
            groupList.Add(gameObject);
            PlaneGroup spawnGroup = gameObject.GetComponent<PlaneGroup>();
            spawnGroup.filePaths = filePaths;
            spawnGroup.dimension = dimension;
            yield return StartCoroutine(spawnGroup.StartSpawnOperation(globalIndex, true));
        
            yield return new WaitForSeconds(spawnCooldownZ);

            while (fadeCoroutinesRunning > 0)
            {
                yield return null;
            }

            movingOperationRunning = false;
        }

        protected override IEnumerator GroupSpawnPush()
        {
            movingOperationRunning = true;
            // Front group has to be fade alpha 0
            Fade frontGroupFader = groupList[0].gameObject.GetComponent<Fade>();
            frontGroupFader.lowerAlpha = softFadeUpperAlpha;
            frontGroupFader.upperAlpha = 1;
            int fadeCoroutinesRunning = 0;
            TaskCoroutine fadeCoroutine = new TaskCoroutine(frontGroupFader.FadeOutCoroutine());
            fadeCoroutine.Finished += delegate (bool manual)
            {
                fadeCoroutinesRunning--;
            };
            fadeCoroutinesRunning++;
            // Change global Index
            MoveGlobalIndex(false);
            // Spawn group in front
            GameObject gameObject = Instantiate(planeGroupPrefab, transform.position - new Vector3(0, 0, 1f), transform.rotation, linearRail.transform);
            gameObject.transform.SetSiblingIndex(0);
            groupList.Insert(0, gameObject);
            PlaneGroup spawnGroup = gameObject.GetComponent<PlaneGroup>();
            spawnGroup.filePaths = filePaths;
            spawnGroup.dimension = dimension;
            yield return StartCoroutine(spawnGroup.StartSpawnOperation(globalIndex, false));

            // Front Group Operations
            frontGroup = gameObject;
            SetMovementBoundBox();

            // Destroy group in back
            GameObject planeInBack = groupList[groupList.Count - 1];
            groupList.Remove(planeInBack);
            Destroy(planeInBack.transform.gameObject);
            planeInBack.transform.parent = null;

            yield return new WaitForSeconds(spawnCooldownZ);

            while (fadeCoroutinesRunning > 0)
            {
                yield return null;
            }

            movingOperationRunning = false;
        }

        #endregion

    }
}
