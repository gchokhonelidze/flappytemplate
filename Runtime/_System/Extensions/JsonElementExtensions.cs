#nullable enable

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace FlappyTemplate
{
	public static class JTokenExtensions
	{
		public static bool GetBoolean(this JToken jsonToken)
		{
			return jsonToken.Value<bool>();
		}

		public static int GetInt32(this JToken jsonToken)
		{
			return jsonToken.Value<int>();
		}

		public static long GetInt64(this JToken jsonToken)
		{
			return jsonToken.Value<long>();
		}

		public static double GetDouble(this JToken jsonToken)
		{
			return jsonToken.Value<double>();
		}

		public static int? GetInt32_Nullable(this JToken jsonToken)
		{
			return jsonToken.Type switch
			{
				JTokenType.Null => null,
				JTokenType.Undefined => null,
				JTokenType.Integer => jsonToken.GetInt32(),
				JTokenType.Float => jsonToken.Value<int>(),
				JTokenType.String when int.TryParse(jsonToken.Value<string>(), out var result) => result,
				_ => null,
			};
		}

		public static string? GetString(this JToken jsonToken)
		{
			return jsonToken.Type switch
			{
				JTokenType.Null => null,
				JTokenType.Undefined => null,
				JTokenType.String => jsonToken.Value<string>(),
				JTokenType.Integer => jsonToken.Value<long>().ToString(),
				JTokenType.Float => jsonToken.Value<double>().ToString(),
				_ => jsonToken.ToString(Formatting.None),
			};
		}

		public static string? GetString_Nullable(this JToken jsonToken)
		{
			return jsonToken.Type switch
			{
				JTokenType.Null => null,
				JTokenType.Undefined => null,
				JTokenType.String => jsonToken.Value<string>(),
				JTokenType.Integer => jsonToken.Value<long>().ToString(),
				JTokenType.Float => jsonToken.Value<double>().ToString(),
				_ => jsonToken.ToString(Formatting.None),
			};
		}
	}
}
