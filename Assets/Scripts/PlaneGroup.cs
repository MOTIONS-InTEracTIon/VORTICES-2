using System.Collections.Generic;
using UnityEngine;

namespace Vortices
{
    public class PlaneGroup : SpawnGroup
    {
        #region Variables and properties

        private LayoutGroup3D layoutGroup;

        #endregion

        private void Start()
        {
            // Starting layout settings
            layoutGroup = GetComponent<LayoutGroup3D>();
            layoutGroup.Style = LayoutStyle.Grid;
            layoutGroup.GridConstraintCount = dimension.y;

        }

        #region Multimedia Spawn
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
            for(int i = 0; i < unloadNumber; i++)
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
