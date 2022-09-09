using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using UnityEngine.UI;
using UnityEngine.Networking;
using System.IO;
using System.Linq;

public enum Result
{
	Success,
	OnGoing,
	WebRequestError,
	TypeError,
	WebRequestTypeError
}

public class LoadLocalManager : MonoBehaviour
{
	// This class manages loading from disk then converting into texture for shape, eventually will support multiple extensions and batch loading
	public List<Texture> retrievedTextures;

	private List<string> fileUrls;

	public Result result;

    private void OnEnable()
    {
		result = Result.OnGoing;
    }

    public IEnumerator RetrieveTexture(List<string> urlsToRetrieve)
	{
		fileUrls = urlsToRetrieve;
		// Type check
		yield return StartCoroutine(IsTypeCorrect());

		// Web retrieving
		// CHANGE: Handle different for more type support
		foreach (string url in fileUrls)
        {
			string newurl = url.Replace(@"\", "/");
			UnityWebRequest www = UnityWebRequestTexture.GetTexture(@"file:///" + newurl);
			yield return www.SendWebRequest();

			if (www.result != UnityWebRequest.Result.Success)
			{
				if (result != Result.WebRequestTypeError && result != Result.WebRequestError)
				{
					if (result == Result.TypeError)
					{
						result = Result.WebRequestTypeError;
					}
					result = Result.WebRequestError;
				}
			}
			else
			{
				Texture texture = ((DownloadHandlerTexture)www.downloadHandler).texture;
				retrievedTextures.Add(texture);
			}
			yield return null;
		}

		if(result == Result.OnGoing)
        {
			result = Result.Success;
        }
	}

	private IEnumerator IsTypeCorrect()
    {
		foreach (string url in fileUrls.ToList())
        {
			string urlextension = Path.GetExtension(url);
			// Supported extensions
			if (urlextension != ".jpg" &&
				urlextension != ".png")
			{
				if(result != Result.TypeError)
                {
					result = Result.TypeError;
				}

				fileUrls.Remove(url);
			}

			yield return null;
		}
    }
}