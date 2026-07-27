// #nullable enable
// using System;
// using Newtonsoft.Json;

// namespace FlappyTemplate
// {
// 	public class FloatToStringConverter : JsonConverter<string>
// 	{
// 		public override string ReadJson(JsonReader reader, Type objectType, string existingValue, bool hasExistingValue, JsonSerializer serializer)
// 		{
// 			if (reader.TokenType == JsonToken.Float || reader.TokenType == JsonToken.Integer)
// 				return System.Convert.ToDouble(reader.Value).ToString();
// 			if (reader.TokenType == JsonToken.String)
// 				return (string)reader.Value!;
// 			throw new JsonSerializationException("Invalid token type");
// 		}

// 		public override void WriteJson(JsonWriter writer, string value, JsonSerializer serializer)
// 		{
// 			writer.WriteValue(value);
// 		}
// 	}
// }
