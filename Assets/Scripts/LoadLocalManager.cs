using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using UnityEngine.UI;
using UnityEngine.Networking;
using System.IO;

public enum Result
{
	Success,
	OnGoing,
	WebRequestError,
	TypeError
}

public class LoadLocalManager : MonoBehaviour
{
	// This class manages loading from disk then converting into texture for shape, eventually will support multiple extensions and batch loading
	public List<Texture> retrievedTextures;

	public Result result;

    private void OnEnable()
    {
		result = Result.OnGoing;
    }

    public IEnumerator RetrieveTexture(List<string> fileUrls)
	{
		// Type check
		IsTypeCorrect(fileUrls);
		if(result == Result.OnGoing)
        {
			// Web retrieving

			bool doneRetrieving = false;
			int index = 0;
			while (!doneRetrieving)
			{
				// CHANGE: Handle different for more type support
				string url = fileUrls[index];
				string newurl = url.Replace(@"\", "/");
				UnityWebRequest www = UnityWebRequestTexture.GetTexture(@"file:///" + newurl);
				yield return www.SendWebRequest();

				if (www.result != UnityWebRequest.Result.Success)
				{
					result = Result.WebRequestError;
					doneRetrieving = true;
                }
                else
                {
					Texture texture = ((DownloadHandlerTexture)www.downloadHandler).texture;
					retrievedTextures.Add(texture);
				}

				if (index == fileUrls.Count - 1)
				{
					result = Result.Success;
					doneRetrieving = true;
				}
				else
				{
					index++;
				}
				yield return null;
			}
		}
	}

	private IEnumerator IsTypeCorrect(List<string> urlsToTest)
    {
		bool doneChecking = false;
		while(!doneChecking)
        {
			int index = 0;
			string url = urlsToTest[index];
			string urlextension = Path.GetExtension(url);
			// Supported extensions
			if (urlextension != ".jpg" || 
				urlextension != ".png")
            {
				result = Result.TypeError;
				doneChecking = true;
            }

			if(index == urlsToTest.Count - 1)
            {
				doneChecking = true;
            } 
			else
            {
				index++;
			}
			yield return null;
		}
    }
}