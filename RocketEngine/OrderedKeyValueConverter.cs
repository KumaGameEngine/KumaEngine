using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace KumaEngine
{
    using System;
    using Newtonsoft.Json;
    using Newtonsoft.Json.Linq;

    public class OrderedKeyValueConverter : JsonConverter
    {
        public override bool CanConvert(Type objectType) =>
            objectType == typeof(List<(string Key, string Value)>);

        public override object ReadJson(
            JsonReader reader,
            Type objectType,
            object existingValue,
            JsonSerializer serializer
        )
        {
            var result = new List<(string Key, string Value)>();

            if (reader.TokenType == JsonToken.Null)
                return result;

            var obj = JObject.Load(reader);

            foreach (var prop in obj.Properties())
                result.Add((prop.Name, prop.Value.ToString()));

            return result;
        }

        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            writer.WriteStartObject();

            foreach (var (key, val) in (List<(string Key, string Value)>)value)
            {
                writer.WritePropertyName(key);
                writer.WriteValue(val);
            }

            writer.WriteEndObject();
        }
    }
}
