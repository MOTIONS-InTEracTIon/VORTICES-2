using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using UnityEngine.UI;
using UnityEngine.Networking;

public class LoadLocalManager : MonoBehaviour
{
	// This class manages loading from disk then converting into texture for shape, eventually will support multiple extensions and batch loading
	public List<Texture> retrievedTexture;

	public bool texturesRetrieved;

    private void OnEnable()
    {
		texturesRetrieved = false;
    }

    public IEnumerator RetrieveTexture(string urlFile)
	{
		texturesRetrieved = false;
		string newurl = urlFile.Replace(@"\", "/");
		UnityWebRequest www = UnityWebRequestTexture.GetTexture(@"file:///" + newurl);
		yield return www.SendWebRequest();

		if (www.result != UnityWebRequest.Result.Success)
		{
			Debug.Log(www.error);
		}
		else
		{
			Texture texture = ((DownloadHandlerTexture)www.downloadHandler).texture;
			retrievedTexture.Add(texture);
			texturesRetrieved = true;
		}
	}
}