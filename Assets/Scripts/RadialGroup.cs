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
        public float groupRadius = 2.0f;
        public float groupAngleOffset = 15.0f;
        public float rotationAngleStep = 1.0f;
        public float rotationTime = 0.5f;

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
                TaskCoroutine rotateCoroutine = new TaskCoroutine(RotateRing(ring, moveDir, rotationTime, rotationAngleStep));
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
            float startingAngle = layout.StartAngleOffset;
            float finalAngle = 0;
            if (moveDir == "Right")
            {
                finalAngle = layout.StartAngleOffset - rotationAngleStep;
            }
            else if (moveDir == "Left")
            {
                finalAngle = layout.StartAngleOffset + rotationAngleStep;
            }

            while (timeElapsed < rotationTime)
            {
                timeElapsed += Time.deltaTime;
                layout.StartAngleOffset = Mathf.Lerp(startingAngle, finalAngle, timeElapsed / rotationTime);
                yield return null;
            }
            groupAngleOffset = layout.StartAngleOffset;
        }

        public IEnumerator RadiusLerp(string dragDir, float radiusStep, float timeLerp)
        {
            int radiusLerpCoroutinesRunning = 0;
            foreach (GameObject radialRing in radialRingList)
            {
                TaskCoroutine radiusLerpCoroutine = new TaskCoroutine(RadiusLerpRing(radialRing, dragDir, radiusStep, timeLerp));
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
            groupRadius = finalRadius;
        }

        #endregion

        #region Multimedia Spawn
        public void Init(List<string> filePaths, Vector3Int dimension, string browsingMode, string displayMode, string rootUrl, GameObject linearRail, float groupRadius, float groupAngleOffset,  float softFadeUpperAlpha, float rotationAngleStep)
        {
            this.filePaths = filePaths;
            this.dimension = dimension;
            this.browsingMode = browsingMode;
            this.displayMode = displayMode;
            this.rootUrl = rootUrl;
            this.radialRingLinearRail = linearRail;
            this.groupRadius = groupRadius;
            this.groupAngleOffset = groupAngleOffset;
            this.softFadeUpperAlpha = softFadeUpperAlpha;
            this.rotationAngleStep = rotationAngleStep;
        }

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

        public override void GenerateEnterObjects(int loadNumber, bool forwards)
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

        public override void GenerateExitObjects(int unloadNumber, bool forwards)
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

