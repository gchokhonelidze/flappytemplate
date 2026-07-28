#nullable enable
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UnityEngine;

namespace FlappyTemplate
{
    public static class Utils
    {
        public static void ApplyPatch<T>(T dto, string json, bool ignoreNulls = false)
        {
            var patch = JsonConvert.DeserializeObject<Dictionary<string, JToken>>(json);
            if (patch is null)
                return;
            var props = typeof(T).GetProperties();
            foreach (var (key, jToken) in patch)
            {
                var prop = props.FirstOrDefault(p => p.Name == key);
                if (prop is null)
                    continue;
                if (ignoreNulls && jToken.Type == JTokenType.Null)
                    continue;
                var result = jToken.ToObject(prop.PropertyType);
                prop.SetValue(dto, result);
            }
        }

        public static T? Deserialize<T>(string json)
        {
            return JsonConvert.DeserializeObject<T>(
                json,
                new JsonSerializerSettings { MissingMemberHandling = MissingMemberHandling.Ignore }
            );
        }

        public static T? TryDeserialize<T>(string json)
        {
            try
            {
                return Deserialize<T>(json);
            }
            catch (JsonException ex)
            {
                Debug.LogError($"Failed to deserialize JSON: {ex.Message}");
                return default;
            }
        }

        public static string Serialize(object obj)
        {
            return JsonConvert.SerializeObject(obj);
        }
    }
}
