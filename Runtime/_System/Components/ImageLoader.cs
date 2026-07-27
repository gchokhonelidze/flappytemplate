using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;

[RequireComponent(typeof(RawImage))]
public class ImageLoader : MonoBehaviour
{
	RawImage targetImage;
	public string Url;

	private void Awake()
	{
		targetImage = GetComponent<RawImage>();
		StartCoroutine(Load());
	}

	IEnumerator Load()
	{
		using var req = UnityWebRequestTexture.GetTexture(Url);
		yield return req.SendWebRequest();

		if (req.result != UnityWebRequest.Result.Success)
		{
			Debug.LogError(req.error);
			yield break;
		}
		var tex = DownloadHandlerTexture.GetContent(req);
		targetImage.texture = tex;
	}
}
