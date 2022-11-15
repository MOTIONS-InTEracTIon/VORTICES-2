using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Vortices
{
    public class PlaneGroup : SpawnGroup
    {
        // Other references
        [SerializeField] GameObject planeRowPrefab;

        #region Multimedia Spawn

        public void Init(List<string> filePaths, Vector3Int dimension, string browsingMode, string displayMode, string rootUrl, float softFadeUpperAlpha)
        {
            this.filePaths = filePaths;
            this.dimension = dimension;
            this.browsingMode = browsingMode;
            this.displayMode = displayMode;
            this.rootUrl = rootUrl;
            this.softFadeUpperAlpha = softFadeUpperAlpha;
        }

        protected override GameObject BuildRow(bool onTop)
        {
            GameObject gameObject = Instantiate(planeRowPrefab, transform.position, planeRowPrefab.transform.rotation, transform);

            if (!onTop)
            {
                gameObject.transform.SetAsFirstSibling();
                rowList.Insert(0, gameObject);
            }
            else
            {
                rowList.Add(gameObject);
            }

            return gameObject;
        }

        #endregion
    }
}
