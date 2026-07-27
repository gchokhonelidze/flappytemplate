#nullable enable
using System.Collections.Generic;
using Newtonsoft.Json.Linq;

namespace FlappyTemplate
{
	public static class DictionaryExtensions
	{
		public static Dictionary<string, JToken> MergePartial(this Dictionary<string, JToken> d1, Dictionary<string, JToken> partial)
		{
			foreach (var el in partial)
				if (d1.ContainsKey(el.Key))
					d1[el.Key] = el.Value;
			return d1;
		}

		public static Dictionary<string, JToken> MergeFull(this Dictionary<string, JToken>? d1, Dictionary<string, JToken>? d2)
		{
			d1 ??= new Dictionary<string, JToken>();
			d2 ??= new Dictionary<string, JToken>();
			foreach (var el in d2)
				d1[el.Key] = el.Value;
			return d1;
		}

		public static GenericDictionary<string, string?> ToGeneric(this Dictionary<string, JToken> dict)
		{
			var result = new GenericDictionary<string, string?>();
			foreach (var kv in dict)
				result[kv.Key] = kv.Value.GetString_Nullable();
			return result;
		}
	}
}
