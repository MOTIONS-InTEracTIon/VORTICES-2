using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using UnityEngine.UI;

public class RenderManager : MonoBehaviour
{
    // Auxiliary Components

    [SerializeField] GameObject loadManager;

    public Result result;

    // Places a multimedia object int the world using:
    // texturePaths = Indicates the path in storage where the textures will be taken
    // prefabObject = Indicates the form that the texture will take when placed
    // asThumbnail = Lowers the quality of the texture (Used as thumbnails)
    // invisible = Places the prefab as invisible
    // asFirstSibling = Places the prefab as first sibling of placementObject
    // placementObject = List of locations to be filled by multimedia objects


    public IEnumerator PlaceMultimedia(List<string> texturePaths, GameObject prefabObject, bool asThumbnail, bool invisible, List<GameObject> placementObjects)
    {
        result = Result.OnGoing;
        // X images will be placed inside X gameobjects
        LoadLocalManager loadLocalManager = Instantiate(loadManager).GetComponent<LoadLocalManager>();
        yield return StartCoroutine(loadLocalManager.GetMultipleImage(texturePaths, asThumbnail));
        if (loadLocalManager.result != Result.TotalError)
        {
            List<Texture2D> textures = loadLocalManager.textureBuffer;
            // Places images in object
            for (int i = 0; i < textures.Count; i++)
            {
                if (placementObjects.Count == 1)
                {
                    PlaceFitObject(textures[i], prefabObject, placementObjects[0], invisible);
                }
                else
                {
                    PlaceFitObject(textures[i], prefabObject, placementObjects[i], invisible);
                }
            }

            if (result == Result.OnGoing)
            {
                result = Result.Success;
            }
        }
        else
        {
            // Show that couldnt get any image from the paths provided
            result = Result.TotalError;
        }
        Destroy(loadLocalManager.gameObject);

    }

    private void PlaceFitObject(Texture2D texture, GameObject prefabObject, GameObject fitObject, bool invisible)
    {
        // Spawning starts from instantiating the prefab (Destroying if fitobject has already a child)
        if(fitObject.transform.childCount > 0)
        {
            Destroy(fitObject.transform.GetChild(0).gameObject);
        }
        GameObject spawnObject = Instantiate(prefabObject, fitObject.transform.position, prefabObject.transform.rotation, fitObject.transform);

        if (invisible)
        {
            spawnObject.SetActive(false);
        }
        // Apply properties
        // Fit into object while conserving aspect ratio
        SizeToParent(spawnObject, texture);
        // Apply material file
        spawnObject.GetComponent<Renderer>().material.mainTexture = texture;
    }

    private void SizeToParent(GameObject spawnObject, Texture2D texture)
    {
        RectTransform objectRect = spawnObject.GetComponent<RectTransform>();

        float boundX = objectRect.localScale.x;

        float finalScaleY = objectRect.localScale.y;
        float finalScaleX = texture.width * finalScaleY / texture.height;

        objectRect.localScale = new Vector3 (finalScaleX, 1 , finalScaleY);

        if(objectRect.localScale.x > boundX)
        {
            finalScaleX = boundX;
            finalScaleY = texture.height * finalScaleX / texture.width;

            objectRect.localScale = new Vector3(finalScaleX, 1, finalScaleY);
        }
    }

}
