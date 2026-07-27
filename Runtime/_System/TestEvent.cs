using System.Collections.Generic;
using Newtonsoft.Json.Linq;
using UnityEngine;
using UnityEngine.Scripting;

namespace FlappyTemplate
{
	[Preserve]
	public class RRR
	{
		public string Hello { get; set; } = string.Empty;
	}

	public class TestEvent : MonoBehaviour
	{
		// void Start()
		// {
		// 	if (StateManager.Inst != null)
		// 	{
		// 		StateManager.Inst.Events.OnSettings.AddListener(OnClickResponse);
		// 	}
		// }

		// void OnDestroy()
		// {
		// 	if (StateManager.Inst != null)
		// 	{
		// 		StateManager.Inst.Events.OnSettings.RemoveListener(OnClickResponse);
		// 	}
		// }

		public void OnClick()
		{
			Debug.Log("Clicked!");
			Emitter.Inst.OnSettingSet(
				new SettingDto
				{
					Name = "test",
					Value = new RRR { Hello = "world" }
				}
			);
		}

		public void OnClickResponse(Dictionary<string, JToken> data)
		{
			Debug.Log("OnClickResponseOnClickResponseOnClickResponseOnClickResponseOnClickResponseOnClickResponseOnClickResponseOnClickResponse");
			foreach (var kv in data)
			{
				Debug.Log($"Key: {kv.Key}, Value: {kv.Value.Value<string>()}");
			}
		}
	}
}
