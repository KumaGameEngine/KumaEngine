using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.ComponentModel;

namespace KumaEngine
{
    public class OrderedKeyValueConverter<TKey, TValue> : JsonConverter
    {
        public override bool CanConvert(Type objectType) =>
            objectType == typeof(List<(TKey Key, TValue Value)>);

        public override object ReadJson(
            JsonReader reader,
            Type objectType,
            object existingValue,
            JsonSerializer serializer)
        {
            var result = new List<(TKey Key, TValue Value)>();
            if (reader.TokenType == JsonToken.Null)
                return result;

            var obj = JObject.Load(reader);
            foreach (var prop in obj.Properties())
            {
                TKey key = ConvertKey(prop.Name);
                TValue value = prop.Value.ToObject<TValue>(serializer);
                result.Add((key, value));
            }
            return result;
        }

        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            writer.WriteStartObject();
            foreach (var (key, val) in (List<(TKey Key, TValue Value)>)value)
            {
                writer.WritePropertyName(Convert.ToString(key));
                serializer.Serialize(writer, val);
            }
            writer.WriteEndObject();
        }

        private static TKey ConvertKey(string propertyName)
        {
            if (typeof(TKey) == typeof(string))
                return (TKey)(object)propertyName;

            var converter = TypeDescriptor.GetConverter(typeof(TKey));
            if (converter.CanConvertFrom(typeof(string)))
                return (TKey)converter.ConvertFromInvariantString(propertyName)!;

            return (TKey)Convert.ChangeType(propertyName, typeof(TKey));
        }
    }
}
