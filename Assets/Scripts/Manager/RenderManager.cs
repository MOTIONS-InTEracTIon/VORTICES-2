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
    // OBJECT PROPERTIES
    // hasGravity = Enables rigidbody component in prefab making it fall
    // sizeMultiplier = Makes prefab smaller or larger


    public IEnumerator PlaceMultimedia(List<string> texturePaths, GameObject prefabObject, bool asThumbnail, bool invisible, bool asFirstSibling, List<GameObject> placementObjects, bool hasGravity, float sizeMultiplier)
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
                    yield return StartCoroutine(PlaceFitObject(textures[i], prefabObject, placementObjects[0], invisible, asFirstSibling, hasGravity, sizeMultiplier));
                }
                else
                {
                    yield return StartCoroutine(PlaceFitObject(textures[i], prefabObject, placementObjects[i], invisible, asFirstSibling, hasGravity, sizeMultiplier));
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

    private IEnumerator PlaceFitObject(Texture2D texture, GameObject prefabObject, GameObject fitObject, bool invisible, bool asFirstSibling, bool hasGravity, float sizeMultiplier)
    {
        // For each file create an object
        for (int i = 0; i < 1; i++)
        {
            // Spawning starts from instantiating the prefab
            GameObject spawnObject = Instantiate(prefabObject, fitObject.transform.position, prefabObject.transform.rotation, fitObject.transform);
            if (invisible)
            {
                spawnObject.SetActive(false);
            }
            if (asFirstSibling)
            {
                spawnObject.transform.SetAsFirstSibling();
            }
            // Apply properties
            // Apply gravity toggle
            if (!hasGravity)
            {
                spawnObject.GetComponent<Rigidbody>().isKinematic = true;
            }
            // Apply size slider
            spawnObject.transform.localScale *= sizeMultiplier;
            // Apply material file
            spawnObject.GetComponent<Renderer>().material.mainTexture = texture;
            yield return null;
        }
    }

    /*private void SizeToParent(SpriteRenderer image, GameObject fitObject)
    {
        RectTransform imageRect = image.GetComponent<RectTransform>();
        RectTransform objectRect = fitObject.GetComponent<RectTransform>();
        float childAspectRatio = image.bounds.size.x / image.bounds.size.y;
        float objectAspectRatio = objectRect.rect.width / objectRect.rect.height;
        Bounds imageBounds = image.sprite.bounds;

        if (childAspectRatio > objectAspectRatio)
        {
            float factor = (objectRect.rect.height / imageBounds.size.y);
            image.transform.localScale = new Vector3(factor, factor, factor);
            factor = (objectRect.rect.width / imageBounds.size.x);
            image.transform.localScale = new Vector3(factor, factor, factor);
        }
        else
        {
            float factor = (objectRect.rect.width / imageBounds.size.x);
            image.transform.localScale = new Vector3(factor, factor, factor);
            factor = (objectRect.rect.height / imageBounds.size.y);
            image.transform.localScale = new Vector3(factor, factor, factor);
        }
    }*/

}
