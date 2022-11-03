using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Vortices
{
    public class PlaneGroup : SpawnGroup
    {
        private void Start()
        {
            // Starting layout settings
            layoutGroup = GetComponent<LayoutGroup3D>();
            layoutGroup.GridConstraintCount = dimension.y;
        }

        #region Multimedia Spawn

        public override IEnumerator StartSpawnOperation(int offsetGlobalIndex, bool softFadeIn)
        {
            // Startup
            globalIndex = offsetGlobalIndex;
            lastLoadForward = true;
            loadPaths = new List<string>();
            loadObjects = new List<GameObject>();
            unloadObjects = new List<GameObject>();
            groupFader = GetComponent<Fade>();

            // First time has to fill every slot so it uses width * height
            int startingLoad = dimension.x * dimension.y;

            // Execution
            yield return StartCoroutine(ObjectSpawn(0, startingLoad, true, softFadeIn));

        }

        public override void GenerateObjectPlacement(int loadNumber, bool forwards)
        {
            loadObjects = new List<GameObject>();

            for (int i = 0; i < loadNumber; i++)
            {
                GameObject positionObject = new GameObject();
                positionObject.AddComponent<Fade>();
                loadObjects.Add(positionObject);
                if (!forwards)
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
            unloadObjects = new List<GameObject>();

            for (int i = 0; i < unloadNumber; i++)
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
}
