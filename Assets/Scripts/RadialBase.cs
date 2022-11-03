using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Vortices
{
    public class RadialBase : SpawnBase
    {
        // Other references
        [SerializeField] private GameObject linearRailPrefab;
        public GameObject radialGroupPrefab;

        // Movement Variables
        private bool radiusLerpRunning;
        private bool rotateFrontSpawnGroupRunning;
        private float pullPushCount;

        // Settings
        public float startingRadius = 2.0f;
        public float radiusStep = 1.0f;
        public float angleStep = 15.0f;

        #region Group Spawn
        public override void StartGenerateSpawnGroup()
        {
            globalIndex = -1;
            lastLoadForward = true;
            groupList = new List<GameObject>();
            for (int i = 0; i < dimension.z; i++) 
            {
                // Radial Group Generation
                GameObject gameObject = Instantiate(radialGroupPrefab, transform.position, transform.rotation, transform);
                groupList.Add(gameObject);
                // Linear Rail Generation
                GameObject linearRail = Instantiate(linearRailPrefab, transform.position, transform.rotation, gameObject.transform);
                LayoutGroup3D railLayout = linearRail.GetComponent<LayoutGroup3D>();
                railLayout.LayoutAxis = LayoutAxis3D.Y;
                railLayout.PrimaryAlignment = Alignment.Center;
                // Radial Group Setting
                RadialGroup spawnGroup = gameObject.GetComponent<RadialGroup>();
                spawnGroup.filePaths = filePaths;
                spawnGroup.dimension = dimension;
                spawnGroup.radialRingLinearRail = linearRail;
                spawnGroup.groupRadius = startingRadius + radiusStep * i;
                spawnGroup.groupAngleOffset += angleStep * i;
                spawnGroup.softFadeUpperAlpha = softFadeUpperAlpha;
                bool softFadeIn = true;
                if (i == 0)
                {
                    frontGroup = gameObject;
                    softFadeIn = false;
                }
                else
                {
                    MoveGlobalIndex(true);
                }
                StartCoroutine(spawnGroup.StartSpawnOperation(globalIndex,softFadeIn));
            }
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
                    if (afterSpawnTime >= spawnCooldownX && !rotateFrontSpawnGroupRunning)
                    {
                        afterSpawnTime = 0;
                        StartCoroutine(RotateFrontSpawnGroup(dragDir));
                    }
                }
                // This means the base has touched the right bound and will spawn
                else if (dragDir == "Right")
                {
                    if (afterSpawnTime >= spawnCooldownX && !rotateFrontSpawnGroupRunning)
                    {
                        afterSpawnTime = 0;
                        StartCoroutine(RotateFrontSpawnGroup(dragDir));
                    }
                }
                else if (dragDir == "Up")
                {
                    if (afterSpawnTime >= spawnCooldownX && !movingOperationRunning)
                    {
                        afterSpawnTime = 0;
                        coroutineQueue.Enqueue(GroupSpawnBackwards());

                    }
                }
                else if (dragDir == "Down")
                {
                    if (afterSpawnTime >= spawnCooldownX && !movingOperationRunning)
                    {
                        afterSpawnTime = 0;
                        coroutineQueue.Enqueue(GroupSpawnForwards());
                    }
                }
            }
        }

        // Not Spawning movement
        private IEnumerator RotateFrontSpawnGroup(string dragDir)
        {
            rotateFrontSpawnGroupRunning = true;
            RadialGroup radialGroup = frontGroup.GetComponent<RadialGroup>();
            yield return StartCoroutine(radialGroup.RotateSpawnGroup(dragDir));
            rotateFrontSpawnGroupRunning = false;
        }

        #endregion

        #region Multimedia Spawn
        protected override IEnumerator GroupSpawnForwards()
        {
            movingOperationRunning = true;
            int spawnCoroutinesRunning = 0;

            for (int i = 0; i < dimension.z; i++)
            {
                RadialGroup radialGroup = groupList[i].GetComponent<RadialGroup>();
                bool softFadeIn = true;
                if (i == 0)
                {
                    softFadeIn = false;
                }
                Task spawnCoroutine = new Task(radialGroup.SpawnForwards(dimension.x, softFadeIn));
                spawnCoroutine.Finished += delegate (bool manual) { spawnCoroutinesRunning--; };
                spawnCoroutinesRunning++;
            }
            globalIndex += dimension.x;

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
                RadialGroup radialGroup = groupList[i].GetComponent<RadialGroup>();
                bool softFadeIn = true;
                if (i == 0)
                {
                    softFadeIn = false;
                }
                Task spawnCoroutine = new Task(radialGroup.SpawnBackwards(dimension.x, softFadeIn));
                spawnCoroutine.Finished += delegate (bool manual) { spawnCoroutinesRunning--; };
                spawnCoroutinesRunning++;
            }

            globalIndex -= dimension.x;

            while (spawnCoroutinesRunning > 0)
            {
                yield return null;
            }

            movingOperationRunning = false;
        }

        // Could be cleaned...
        protected override IEnumerator GroupSpawnPull()
        {
            movingOperationRunning = true;
            // Destroy group in front
            GameObject radialGroupInFront = groupList[0];
            groupList.Remove(radialGroupInFront);
            Destroy(radialGroupInFront.transform.gameObject);
            radialGroupInFront.transform.parent = null;
            // Front Group Operations
            frontGroup = groupList[0];
            // Front group has to be fade alpha 1
            Fade frontGroupFader = frontGroup.gameObject.GetComponent<Fade>();
            frontGroupFader.lowerAlpha = softFadeUpperAlpha;
            frontGroupFader.upperAlpha = 1;
            int fadeCoroutinesRunning = 0;
            Task fadeCoroutine = new Task(frontGroupFader.FadeInCoroutine());
            fadeCoroutine.Finished += delegate (bool manual)
            {
                fadeCoroutinesRunning--;
            };
            fadeCoroutinesRunning++;
            // Every group has to lerp radius inwards
            int radiusLerpCoroutinesRunning = 0;
            foreach (GameObject radialGroup in groupList)
            {
                RadialGroup radialGroupComponent = radialGroup.GetComponent<RadialGroup>();
                Task radiusLerpCoroutine = new Task(radialGroupComponent.RadiusLerp("Pull", radiusStep, timeLerp));
                radiusLerpCoroutine.Finished += delegate (bool manual)
                {
                    radiusLerpCoroutinesRunning--;
                };
                radiusLerpCoroutinesRunning++;
            }
            // Change global Index
            MoveGlobalIndex(true);
            // Spawn group in back
            GameObject gameObject = Instantiate(radialGroupPrefab, transform.position, transform.rotation, transform);
            groupList.Add(gameObject);
            GameObject linearRail = Instantiate(linearRailPrefab, transform.position, transform.rotation, gameObject.transform);
            LayoutGroup3D railLayout = linearRail.GetComponent<LayoutGroup3D>();
            railLayout.LayoutAxis = LayoutAxis3D.Y;
            railLayout.PrimaryAlignment = Alignment.Center;
            RadialGroup spawnGroup = gameObject.GetComponent<RadialGroup>();
            spawnGroup.filePaths = filePaths;
            spawnGroup.dimension = dimension;
            spawnGroup.radialRingLinearRail = linearRail;
            spawnGroup.groupRadius = startingRadius + radiusStep * (groupList.Count - 1);
            spawnGroup.groupAngleOffset += angleStep * pullPushCount;
            spawnGroup.softFadeUpperAlpha = softFadeUpperAlpha;
            yield return StartCoroutine(spawnGroup.StartSpawnOperation(globalIndex, true));

            while (fadeCoroutinesRunning > 0 && radiusLerpCoroutinesRunning > 0)
            {
                yield return null;
            }

            pullPushCount++;
            movingOperationRunning = false;
        }

        protected override IEnumerator GroupSpawnPush()
        {
            movingOperationRunning = true;
            // Destroy group in back
            GameObject radialGroupInBack = groupList[groupList.Count - 1];
            groupList.Remove(radialGroupInBack);
            Destroy(radialGroupInBack.transform.gameObject);
            radialGroupInBack.transform.parent = null;
            // Every group except front has to lerp radius outwards
            int radiusLerpCoroutinesRunning = 0;
            foreach (GameObject radialGroup in groupList)
            {
                RadialGroup radialGroupComponent = radialGroup.GetComponent<RadialGroup>();
                Task radiusLerpCoroutine = new Task(radialGroupComponent.RadiusLerp("Push", radiusStep, timeLerp));
                radiusLerpCoroutine.Finished += delegate (bool manual)
                {
                    radiusLerpCoroutinesRunning--;
                };
                radiusLerpCoroutinesRunning++;
            }
            // Front group has to be fade alpha 0
            Fade frontGroupFader = groupList[0].gameObject.GetComponent<Fade>();
            frontGroupFader.lowerAlpha = softFadeUpperAlpha;
            frontGroupFader.upperAlpha = 1;
            int fadeCoroutinesRunning = 0;
            Task fadeCoroutine = new Task(frontGroupFader.FadeOutCoroutine());
            fadeCoroutine.Finished += delegate (bool manual)
            {
                fadeCoroutinesRunning--;
            };
            fadeCoroutinesRunning++;
            // Change global Index
            MoveGlobalIndex(false);
            // Spawn group in front
            GameObject gameObject = Instantiate(radialGroupPrefab, transform.position, transform.rotation, transform);
            gameObject.transform.SetSiblingIndex(0);
            groupList.Insert(0, gameObject);
            GameObject linearRail = Instantiate(linearRailPrefab, transform.position, transform.rotation, gameObject.transform);
            LayoutGroup3D railLayout = linearRail.GetComponent<LayoutGroup3D>();
            railLayout.LayoutAxis = LayoutAxis3D.Y;
            railLayout.PrimaryAlignment = Alignment.Center;
            RadialGroup spawnGroup = gameObject.GetComponent<RadialGroup>();
            spawnGroup.filePaths = filePaths;
            spawnGroup.dimension = dimension;
            spawnGroup.radialRingLinearRail = linearRail;
            spawnGroup.groupRadius = startingRadius;
            spawnGroup.groupAngleOffset -= angleStep * pullPushCount;
            spawnGroup.softFadeUpperAlpha = softFadeUpperAlpha;

            yield return StartCoroutine(spawnGroup.StartSpawnOperation(globalIndex, false));

            // Front Group Operations
            frontGroup = gameObject;

            while (fadeCoroutinesRunning > 0 && radiusLerpCoroutinesRunning > 0)
            {
                yield return null;
            }

            pullPushCount++;
            yield return new WaitForSeconds(spawnCooldownZ);

            movingOperationRunning = false;
        }
        #endregion
    }
}
