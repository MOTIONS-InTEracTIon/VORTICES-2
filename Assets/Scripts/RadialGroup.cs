using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Vortices
{
    public class RadialGroup : SpawnGroup
    {
        // Other references
        public List<GameObject> radialRingList;
        [SerializeField] GameObject radialRingPrefab;
        public GameObject radialRingLinearRail;

        // Utility
        private int rotationCoroutinesRunning;

        // Settings
        public float groupRadius;
        public float groupAngleOffset;
        public float rotationAngleStep = 1.0f;
        public float rotationTime = 1.0f;

        private void Start()
        {
            // Starting layout settings
            layoutGroup = GetComponent<LayoutGroup3D>();
            // IN THIS STEP YOU CONFIGURE SO THE RADIUS IS DIFFERENT IN EVERY GROUP (Layer radius)
        }


        #region Movement

        public IEnumerator RotateSpawnGroup(string moveDir)
        {
            rotationCoroutinesRunning = 0;

            foreach (GameObject radialRing in radialRingList)
            {
                GameObject ring = radialRing.transform.GetChild(0).gameObject;
                Task rotateCoroutine = new Task(RotateRing(ring, moveDir, rotationTime, rotationAngleStep));
                rotateCoroutine.Finished += delegate (bool manual)
                {
                    rotationCoroutinesRunning--;
                };
                rotationCoroutinesRunning++;
            }

            while (rotationCoroutinesRunning > 0)
            {
                yield return null;
            }
        }

        public IEnumerator RotateRing(GameObject radialRing, string moveDir, float rotationTime, float rotationAngleStep)
        {
            LayoutGroup3D layout = radialRing.GetComponent<LayoutGroup3D>();

            float timeElapsed = 0;
            while (timeElapsed < rotationTime)
            {
                timeElapsed += Time.deltaTime;
                float finalAngle = 0;
                if (moveDir == "Right")
                {
                    finalAngle = layout.StartAngleOffset - rotationAngleStep;
                }
                else if (moveDir == "Left")
                {
                    finalAngle = layout.StartAngleOffset + rotationAngleStep;
                }
                layout.StartAngleOffset = Mathf.Lerp(layout.StartAngleOffset, finalAngle, timeElapsed / rotationTime);
                yield return null;
            }
            groupAngleOffset = layout.StartAngleOffset;
        }

        public IEnumerator RadiusLerp(string dragDir, float radiusStep, float timeLerp)
        {
            int radiusLerpCoroutinesRunning = 0;
            foreach (GameObject radialRing in radialRingList)
            {
                Task radiusLerpCoroutine = new Task(RadiusLerpRing(radialRing, dragDir, radiusStep, timeLerp));
                radiusLerpCoroutine.Finished += delegate (bool manual)
                {
                    radiusLerpCoroutinesRunning--;
                };
                radiusLerpCoroutinesRunning++;
            }

            while (radiusLerpCoroutinesRunning > 0)
            {
                yield return null;
            }
        }

        private IEnumerator RadiusLerpRing(GameObject radialRing, string dragDir, float radiusStep, float timeLerp)
        {
            LayoutGroup3D ringLayout = radialRing.transform.GetChild(0).GetComponent<LayoutGroup3D>();
            float timeElapsed = 0;
            float finalRadius = 0;
            if (dragDir == "Push")
            {
                finalRadius = ringLayout.Radius + radiusStep;
            }
            else if (dragDir == "Pull")
            {
                finalRadius = ringLayout.Radius - radiusStep;
            }
            while (timeElapsed < timeLerp)
            {
                timeElapsed += Time.deltaTime;
                ringLayout.Radius = Mathf.Lerp(ringLayout.Radius, finalRadius, timeElapsed / timeLerp);
                yield return null;
            }
        }

        #endregion

        #region Multimedia Spawn
        public override IEnumerator StartSpawnOperation(int offsetGlobalIndex, bool softFadeIn)
        {
            // Startup
            globalIndex = offsetGlobalIndex;
            lastLoadForward = true;
            radialRingList = new List<GameObject>();
            loadPaths = new List<string>();
            unloadObjects = new List<GameObject>();
            loadObjects = new List<GameObject>();
            groupFader = GetComponent<Fade>();

            int startingLoad = dimension.x * dimension.y;

            // Execution
            yield return StartCoroutine(ObjectSpawn(0, startingLoad, true,softFadeIn));

        }

        private GameObject BuildRadialRing(bool onTop)
        {
            GameObject gameObject = Instantiate(radialRingPrefab, transform.position, radialRingPrefab.transform.rotation, radialRingLinearRail.transform);
            // Radial Ring Setting
            LayoutGroup3D ringLayout = gameObject.transform.GetChild(0).GetComponent<LayoutGroup3D>();
            ringLayout.Radius = groupRadius;
            ringLayout.StartAngleOffset = groupAngleOffset;

            if (!onTop)
            {
                gameObject.transform.SetAsFirstSibling();
                radialRingList.Insert(0 ,gameObject);
            }
            else
            {
                radialRingList.Add(gameObject);
            }

            return gameObject;
        }

        public override void GenerateObjectPlacement(int loadNumber, bool forwards)
        {
            loadObjects = new List<GameObject>();

            for (int i = 0; i < loadNumber / dimension.x; i++)
            {
                GameObject radialRing = BuildRadialRing(forwards);
                for (int j = 0; j < dimension.x; j++)
                {
                    GameObject positionObject = new GameObject();
                    positionObject.AddComponent<Fade>();
                    loadObjects.Add(positionObject);

                    positionObject.transform.parent = radialRing.transform.GetChild(0).transform;
                }
            }
        }

        public override void GenerateDestroyObjects(int unloadNumber, bool forwards)
        {
            unloadObjects = new List<GameObject>();

            for (int i = 0; i < unloadNumber / dimension.x; i++)
            {
                if (forwards)
                {
                    unloadObjects.Add(radialRingList[0].gameObject);
                    radialRingList.RemoveAt(0);
                }
                else
                {
                    unloadObjects.Add(radialRingList[radialRingList.Count - 1].gameObject);
                    radialRingList.RemoveAt(radialRingList.Count - 1);
                }
            }
        }
        #endregion
    }
}

