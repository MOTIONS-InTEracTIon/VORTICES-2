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
        private bool rotateFrontSpawnGroupRunning;
        private float pullPushCount;
        private bool lastPullPushForward;

        // Settings
        public float startingRadius = 2.0f;
        public float radiusStep = 1.0f;
        public float startingAngle = 15.0f;
        public float angleStep = 15.0f;
        public float rotationAngleStep = 15.0f;

        #region Group Spawn
        public override void StartGenerateSpawnGroup()
        {
            globalIndex = -1;
            rotationAngleStep = 360 / dimension.x;
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
                spawnGroup.Init(filePaths, dimension, browsingMode, displayMode, rootUrl, linearRail,
                    startingRadius + radiusStep * i, startingAngle + angleStep * i, softFadeUpperAlpha, rotationAngleStep);
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

        #region Input
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
                        StartCoroutine(GroupSpawnLeft());
                    }
                }
                // This means the base has touched the right bound and will spawn
                else if (dragDir == "Right")
                {
                    if (afterSpawnTime >= spawnCooldownX && !rotateFrontSpawnGroupRunning)
                    {
                        afterSpawnTime = 0;
                        StartCoroutine(GroupSpawnRight());
                    }
                }
                else if (dragDir == "Up")
                {
                    if (afterSpawnTime >= spawnCooldownX && !movingOperationRunning)
                    {
                        afterSpawnTime = 0;
                        coroutineQueue.Enqueue(GroupSpawnUp());

                    }
                }
                else if (dragDir == "Down")
                {
                    if (afterSpawnTime >= spawnCooldownX && !movingOperationRunning)
                    {
                        afterSpawnTime = 0;
                        coroutineQueue.Enqueue(GroupSpawnDown());
                    }
                }
            }
        }

        protected void MoveGroupCount(bool forwards)
        {
            if (forwards)
            {
                if (!lastPullPushForward)
                {
                    pullPushCount += dimension.z;
                }
                else
                {
                    pullPushCount += 1;
                }

                lastPullPushForward = true;
            }
            else
            {
                if (lastPullPushForward)
                {
                    pullPushCount -= dimension.z;
                }
                else
                {
                    pullPushCount -= 1;
                }

                lastPullPushForward = false;
            }
        }

        #endregion

        #region Spawn Movement

        protected override IEnumerator GroupSpawnDown()
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
                TaskCoroutine spawnCoroutine = new TaskCoroutine(radialGroup.SpawnForwards(dimension.x, softFadeIn));
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

        protected override IEnumerator GroupSpawnUp()
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
                TaskCoroutine spawnCoroutine = new TaskCoroutine(radialGroup.SpawnBackwards(dimension.x, softFadeIn));
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
            TaskCoroutine fadeCoroutine = new TaskCoroutine(frontGroupFader.FadeInCoroutine());
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
                TaskCoroutine radiusLerpCoroutine = new TaskCoroutine(radialGroupComponent.RadiusLerp("Pull", radiusStep, timeLerp));
                radiusLerpCoroutine.Finished += delegate (bool manual)
                {
                    radiusLerpCoroutinesRunning--;
                };
                radiusLerpCoroutinesRunning++;
            }
            // Change global Index
            MoveGlobalIndex(true);
            // Change Group Offset Index
            MoveGroupCount(true);
            // Spawn group in back
            GameObject gameObject = Instantiate(radialGroupPrefab, transform.position, transform.rotation, transform);
            groupList.Add(gameObject);
            GameObject linearRail = Instantiate(linearRailPrefab, transform.position, transform.rotation, gameObject.transform);
            LayoutGroup3D railLayout = linearRail.GetComponent<LayoutGroup3D>();
            railLayout.LayoutAxis = LayoutAxis3D.Y;
            railLayout.PrimaryAlignment = Alignment.Center;
            RadialGroup spawnGroup = gameObject.GetComponent<RadialGroup>();
            spawnGroup.Init(filePaths, dimension, browsingMode, displayMode, rootUrl, linearRail,
                startingRadius + radiusStep * (groupList.Count - 1), angleStep * pullPushCount, softFadeUpperAlpha, rotationAngleStep);
            yield return StartCoroutine(spawnGroup.StartSpawnOperation(globalIndex, true));

            while (fadeCoroutinesRunning > 0 && radiusLerpCoroutinesRunning > 0)
            {
                yield return null;
            }

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
            // Every group has to lerp radius outwards
            int radiusLerpCoroutinesRunning = 0;
            foreach (GameObject radialGroup in groupList)
            {
                RadialGroup radialGroupComponent = radialGroup.GetComponent<RadialGroup>();
                TaskCoroutine radiusLerpCoroutine = new TaskCoroutine(radialGroupComponent.RadiusLerp("Push", radiusStep, timeLerp));
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
            TaskCoroutine fadeCoroutine = new TaskCoroutine(frontGroupFader.FadeOutCoroutine());
            fadeCoroutine.Finished += delegate (bool manual)
            {
                fadeCoroutinesRunning--;
            };
            fadeCoroutinesRunning++;
            // Change global Index
            MoveGlobalIndex(false);
            // Change Group Offset Index
            MoveGroupCount(false);
            // Spawn group in front
            GameObject gameObject = Instantiate(radialGroupPrefab, transform.position, transform.rotation, transform);
            gameObject.transform.SetSiblingIndex(0);
            frontGroup = gameObject;
            groupList.Insert(0, gameObject);
            GameObject linearRail = Instantiate(linearRailPrefab, transform.position, transform.rotation, gameObject.transform);
            LayoutGroup3D railLayout = linearRail.GetComponent<LayoutGroup3D>();
            railLayout.LayoutAxis = LayoutAxis3D.Y;
            railLayout.PrimaryAlignment = Alignment.Center;
            RadialGroup spawnGroup = gameObject.GetComponent<RadialGroup>();
            spawnGroup.Init(filePaths, dimension, browsingMode, displayMode, rootUrl, linearRail,
                startingRadius, angleStep * pullPushCount, softFadeUpperAlpha, rotationAngleStep);
            yield return StartCoroutine(spawnGroup.StartSpawnOperation(globalIndex, false));

            while (fadeCoroutinesRunning > 0 && radiusLerpCoroutinesRunning > 0)
            {
                yield return null;
            }

            movingOperationRunning = false;
        }

        protected override IEnumerator GroupSpawnRight()
        {
            movingOperationRunning = true;
            RadialGroup radialGroup = frontGroup.GetComponent<RadialGroup>();
            yield return StartCoroutine(radialGroup.RotateSpawnGroup("Right"));
            movingOperationRunning = false;
        }

        protected override IEnumerator GroupSpawnLeft()
        {
            movingOperationRunning = true;
            RadialGroup radialGroup = frontGroup.GetComponent<RadialGroup>();
            yield return StartCoroutine(radialGroup.RotateSpawnGroup("Left"));
            movingOperationRunning = false;
        }
        #endregion

        #region Movement

       
        #endregion
    }
}
