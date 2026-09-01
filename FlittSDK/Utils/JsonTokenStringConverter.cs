using System;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace FlittSDK.Utils
{
    /// <summary>
    /// Accepts either a JSON string or a nested JSON value while preserving
    /// the SDK's legacy public string property type.
    /// </summary>
    public class JsonTokenStringConverter : JsonConverter
    {
        public override bool CanConvert(Type objectType)
        {
            return objectType == typeof(string);
        }

        public override object ReadJson(
            JsonReader reader,
            Type objectType,
            object existingValue,
            JsonSerializer serializer
        )
        {
            if (reader.TokenType == JsonToken.Null)
            {
                return null;
            }

            var token = JToken.Load(reader);
            return token.Type == JTokenType.String
                ? token.Value<string>()
                : token.ToString(Formatting.None);
        }

        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            writer.WriteValue(value as string);
        }
    }
}
