using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;

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
        public float startingRadius = 1.0f;
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

            SetMovementBoundBox();
        }

        protected override void SetMovementBoundBox()
        {
            // Uses first plane layout to set bound box
            layoutGroup = frontGroup.transform.GetChild(0).GetComponent<LayoutGroup3D>();

            followerCollider = new List<GameObject>();
            // We create one collider for each hand
            GameObject sourceLeft = rayInteractors[0].transform.parent.gameObject;
            followerCollider.Add(Instantiate(followerColliderPrefab, sourceLeft.transform.forward * (layoutGroup.Radius / 4) + sourceLeft.transform.position, sourceLeft.transform.rotation, sourceLeft.transform));
            GameObject sourceRight = rayInteractors[1].transform.parent.gameObject;
            followerCollider.Add(Instantiate(followerColliderPrefab, sourceRight.transform.forward * (layoutGroup.Radius / 4) + sourceRight.transform.position, sourceRight.transform.rotation, sourceRight.transform));
            foreach (GameObject collider in followerCollider)
            {
                XRGrabInteractable grabInteractable = collider.GetComponent<XRGrabInteractable>();
                grabInteractable.selectEntered.AddListener(MoveToCursor);
                grabInteractable.selectExited.AddListener(ResetFollowerCollider);
                grabInteractable.selectExited.AddListener(StopMoveToCursor);
                collider.GetComponent<RotateToObject>().enabled = true;

                BoxCollider boxCollider = collider.GetComponent<BoxCollider>();
                boxCollider.center = Vector3.zero;
                boxCollider.size = new Vector3((layoutGroup.ElementDimensions.x + layoutGroup.Spacing) * dimension.x, (layoutGroup.ElementDimensions.y + layoutGroup.Spacing) * dimension.y, 0.001f);

                RotateToObject rotateToObject = collider.GetComponent<RotateToObject>();
                rotateToObject.offset = new Vector3(180f, 0, 180f);
                rotateToObject.followName = collider.transform.parent.name;
                rotateToObject.StartRotating(true);
            }

            followerCollider[0].gameObject.layer = LayerMask.NameToLayer("Interactable Left");
            followerCollider[1].gameObject.layer = LayerMask.NameToLayer("Interactable Right");

            // Generates bounds using dimension given (Box from the left side to its down side)
            centerPosition = transform.position;
            bounds.w = -centerPosition.x - (layoutGroup.ElementDimensions.x + layoutGroup.Spacing) * ((dimension.x - 1) / 2);
            bounds.x = centerPosition.y + (layoutGroup.ElementDimensions.y + layoutGroup.Spacing) * ((dimension.y - 1) / 2);
            bounds.y = centerPosition.x + (layoutGroup.ElementDimensions.x + layoutGroup.Spacing) * ((dimension.x - 1) / 2);
            bounds.z = centerPosition.y - (layoutGroup.ElementDimensions.y + layoutGroup.Spacing) * ((dimension.y - 1) / 2);
        }

        private void ResetFollowerCollider(SelectExitEventArgs args)
        {
            GameObject sourceLeft = rayInteractors[0].transform.parent.gameObject;
            GameObject sourceRight = rayInteractors[1].transform.parent.gameObject;
            if (currentlySelecting.gameObject == rayInteractors[0].gameObject)
            {
                followerCollider[0].transform.position = sourceLeft.transform.forward * (layoutGroup.Radius / 2) + sourceLeft.transform.position;
            }
            else if (currentlySelecting.gameObject == rayInteractors[1].gameObject)
            {
                followerCollider[1].transform.position = sourceRight.transform.forward * (layoutGroup.Radius / 2) + sourceRight.transform.position;
            }

        }

        private void OnDrawGizmos()
        {
            GameObject sourceLeft = rayInteractors[1].transform.parent.gameObject;
            Vector3 position = sourceLeft.transform.forward * (layoutGroup.Radius / 2) + sourceLeft.transform.position;
            Gizmos.color = new Color(0f, 1f, 1f, 0.75f);
            Gizmos.DrawSphere(position, 0.15f);
        }

        #endregion

        #region Input

        // Changed so it only spawns when pulling or pushing
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
                        if (browsingMode == "Local")
                        {
                            coroutineQueue.Enqueue(GroupSpawnPull());
                        }
                        else if (browsingMode == "Online")
                        {
                            coroutineQueue.Enqueue(GroupPull());
                        }
                    }
                }
                // This means the base has been pushed and will spawn outwards
                else if (volumetric && dragDir == "Push")
                {
                    if (afterSpawnTime >= spawnCooldownZ && drag && !movingOperationRunning)
                    {
                        afterSpawnTime = 0;
                        if (browsingMode == "Local")
                        {
                            coroutineQueue.Enqueue(GroupSpawnPush());
                        }
                        else if (browsingMode == "Online")
                        {
                            coroutineQueue.Enqueue(GroupPush());
                        }
                    }
                }
                // This means the base has touched the left bound and will spawn
                else if (dragDir == "Left")
                {
                    if (afterSpawnTime >= spawnCooldownX && !rotateFrontSpawnGroupRunning)
                    {
                        afterSpawnTime = 0;
                        if (browsingMode == "Local")
                        {
                            coroutineQueue.Enqueue(GroupSpawnLeft());
                        }
                        else if (browsingMode == "Online")
                        {
                            coroutineQueue.Enqueue(GroupLeft());
                        }
                    }
                }
                // This means the base has touched the right bound and will spawn
                else if (dragDir == "Right")
                {
                    if (afterSpawnTime >= spawnCooldownX && !rotateFrontSpawnGroupRunning)
                    {
                        afterSpawnTime = 0;
                        if (browsingMode == "Local")
                        {
                            coroutineQueue.Enqueue(GroupSpawnRight());
                        }
                        else if (browsingMode == "Online")
                        {
                            coroutineQueue.Enqueue(GroupRight());
                        }
                    }
                }
                else if (dragDir == "Up")
                {
                    if (afterSpawnTime >= spawnCooldownX && !movingOperationRunning)
                    {
                        afterSpawnTime = 0;
                        if (browsingMode == "Local")
                        {
                            coroutineQueue.Enqueue(GroupUp());
                        }
                        else if (browsingMode == "Online")
                        {
                            coroutineQueue.Enqueue(GroupUp());
                        }
                    }
                }
                else if (dragDir == "Down")
                {
                    if (afterSpawnTime >= spawnCooldownX && !movingOperationRunning)
                    {
                        afterSpawnTime = 0;
                        if (browsingMode == "Local")
                        {
                            coroutineQueue.Enqueue(GroupDown());
                        }
                        else if (browsingMode == "Online")
                        {
                            coroutineQueue.Enqueue(GroupDown());
                        }

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

            // Log Action
            LogMovement("Down");

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

            // Log Action
            LogMovement("Up");

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

            // Log Action
            LogMovement("Pull");

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

            // Log Action
            LogMovement("Push");

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

            // Log Action
            LogMovement("Right");

            RadialGroup radialGroup = frontGroup.GetComponent<RadialGroup>();
            yield return StartCoroutine(radialGroup.RotateSpawnGroup("Right"));
            movingOperationRunning = false;
        }

        protected override IEnumerator GroupSpawnLeft()
        {
            movingOperationRunning = true;

            // Log Action
            LogMovement("Left");

            RadialGroup radialGroup = frontGroup.GetComponent<RadialGroup>();
            yield return StartCoroutine(radialGroup.RotateSpawnGroup("Left"));
            movingOperationRunning = false;
        }

        #endregion

        #region Movement

        protected override IEnumerator GroupRight()
        {
            movingOperationRunning = true;

            // Log Action
            LogMovement("Right");

            RadialGroup radialGroup = frontGroup.GetComponent<RadialGroup>();
            yield return StartCoroutine(radialGroup.RotateSpawnGroup("Right"));
            movingOperationRunning = false;
        }

        protected override IEnumerator GroupLeft()
        {
            movingOperationRunning = true;

            // Log Action
            LogMovement("Left");

            RadialGroup radialGroup = frontGroup.GetComponent<RadialGroup>();
            yield return StartCoroutine(radialGroup.RotateSpawnGroup("Left"));
            movingOperationRunning = false;
        }

        protected override IEnumerator GroupPull()
        {
            movingOperationRunning = true;

            // Log Action
            LogMovement("Pull");

            // Bring group in front to back
            GameObject radialGroupInFront = groupList[0];
            ListUtils.Move(groupList, 0, groupList.Count - 1);
            radialGroupInFront.transform.SetAsLastSibling();
            // Front Group Operations
            frontGroup = groupList[0];
            // Front group has to be fade alpha 1 and back group has to be fade alpha softfadeUpperAlpha
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
            Fade backGroupFader = radialGroupInFront.gameObject.GetComponent<Fade>();
            backGroupFader.lowerAlpha = softFadeUpperAlpha;
            backGroupFader.upperAlpha = 1;
            fadeCoroutine = new TaskCoroutine(backGroupFader.FadeOutCoroutine());
            fadeCoroutine.Finished += delegate (bool manual)
            {
                fadeCoroutinesRunning--;
            };
            fadeCoroutinesRunning++;

            int radiusLerpCoroutinesRunning = 0;
            // Group in the front has to be brought to back by adding to its radius
            RadialGroup inFrontRadialGroup = radialGroupInFront.GetComponent<RadialGroup>();
            TaskCoroutine frontRadiusLerpCoroutine = new TaskCoroutine(inFrontRadialGroup.RadiusLerp("Push", radiusStep * dimension.z, 0.5f));
            frontRadiusLerpCoroutine.Finished += delegate (bool manual)
            {
                radiusLerpCoroutinesRunning--;
            };
            radiusLerpCoroutinesRunning++;

            while (fadeCoroutinesRunning > 0 && radiusLerpCoroutinesRunning > 0)
            {
                yield return null;
            }

            // Every group has to lerp radius inwards
            for (int i = 0; i < groupList.Count; i++)
            {
                RadialGroup radialGroupComponent = groupList[i].GetComponent<RadialGroup>();

                TaskCoroutine radiusLerpCoroutine = new TaskCoroutine(radialGroupComponent.RadiusLerp("Pull", radiusStep, timeLerp));
                radiusLerpCoroutine.Finished += delegate (bool manual)
                {
                    radiusLerpCoroutinesRunning--;
                };
                radiusLerpCoroutinesRunning++;

            }

            while (radiusLerpCoroutinesRunning > 0 )
            {
                yield return null;
            }

            movingOperationRunning = false;
        }

        protected override IEnumerator GroupPush()
        {
            movingOperationRunning = true;

            // Log Action
            LogMovement("Push");

            // Bring group in back to front
            GameObject radialGroupInFront = groupList[groupList.Count - 1];
            ListUtils.Move(groupList, groupList.Count - 1, 0);
            radialGroupInFront.transform.SetAsFirstSibling();
            // Front Group Operations
            frontGroup = groupList[0];
            // Front group has to be fade alpha 1 and back group has to be fade alpha softfadeUpperAlpha
            GameObject lastFrontGroup = groupList[1];
            Fade backGroupFader = lastFrontGroup.gameObject.GetComponent<Fade>();
            backGroupFader.lowerAlpha = softFadeUpperAlpha;
            backGroupFader.upperAlpha = 1;
            int fadeCoroutinesRunning = 0;
            TaskCoroutine fadeCoroutine = new TaskCoroutine(backGroupFader.FadeOutCoroutine());
            fadeCoroutine.Finished += delegate (bool manual)
            {
                fadeCoroutinesRunning--;
            };
            fadeCoroutinesRunning++;

            // Every group has to lerp radius outwards
            int radiusLerpCoroutinesRunning = 0;
            for (int i = 0; i < groupList.Count; i++)
            {
                RadialGroup radialGroupComponent = groupList[i].GetComponent<RadialGroup>();

                TaskCoroutine radiusLerpCoroutine = new TaskCoroutine(radialGroupComponent.RadiusLerp("Push", radiusStep, timeLerp));
                radiusLerpCoroutine.Finished += delegate (bool manual)
                {
                    radiusLerpCoroutinesRunning--;
                };
                radiusLerpCoroutinesRunning++;

            }

            while (fadeCoroutinesRunning > 0 && radiusLerpCoroutinesRunning > 0)
            {
                yield return null;
            }

            // Group in the back has to be brought to front by reducing its radius
            RadialGroup inFrontRadialGroup = frontGroup.GetComponent<RadialGroup>();
            TaskCoroutine frontRadiusLerpCoroutine = new TaskCoroutine(inFrontRadialGroup.RadiusLerp("Pull", radiusStep * dimension.z, 0.5f));
            frontRadiusLerpCoroutine.Finished += delegate (bool manual)
            {
                radiusLerpCoroutinesRunning--;
            };
            radiusLerpCoroutinesRunning++;

            Fade frontGroupFader = radialGroupInFront.gameObject.GetComponent<Fade>();
            frontGroupFader.lowerAlpha = softFadeUpperAlpha;
            frontGroupFader.upperAlpha = 1;
            fadeCoroutine = new TaskCoroutine(frontGroupFader.FadeInCoroutine());
            fadeCoroutine.Finished += delegate (bool manual)
            {
                fadeCoroutinesRunning--;
            };
            fadeCoroutinesRunning++;

            while (fadeCoroutinesRunning > 0 && radiusLerpCoroutinesRunning > 0)
            {
                yield return null;
            }

            movingOperationRunning = false;
        }

        protected override IEnumerator GroupUp()
        {
            movingOperationRunning = true;

            // Log Action
            LogMovement("Up");

            int movingCoroutinesRunning = 0;

            for (int i = 0; i < dimension.z; i++)
            {
                RadialGroup radialGroup = groupList[i].GetComponent<RadialGroup>();
                TaskCoroutine spawnCoroutine = new TaskCoroutine(radialGroup.SwapRowsVertically("Up"));
                spawnCoroutine.Finished += delegate (bool manual) { movingCoroutinesRunning--; };
                movingCoroutinesRunning++;
            }

            while (movingCoroutinesRunning > 0)
            {
                yield return null;
            }

            movingOperationRunning = false;
        }

        protected override IEnumerator GroupDown()
        {
            movingOperationRunning = true;

            // Log Action
            LogMovement("Down");

            int movingCoroutinesRunning = 0;

            for (int i = 0; i < dimension.z; i++)
            {
                RadialGroup radialGroup = groupList[i].GetComponent<RadialGroup>();
                TaskCoroutine spawnCoroutine = new TaskCoroutine(radialGroup.SwapRowsVertically("Down"));
                spawnCoroutine.Finished += delegate (bool manual) { movingCoroutinesRunning--; };
                movingCoroutinesRunning++;
            }

            while (movingCoroutinesRunning > 0)
            {
                yield return null;
            }

            movingOperationRunning = false;
        }
        #endregion
    }
}
