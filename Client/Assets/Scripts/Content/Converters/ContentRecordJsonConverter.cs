using System;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Assets.Scripts.Content
{
    public sealed class ContentRecordJsonConverter : JsonConverter<ContentRecord>
    {
        public override ContentRecord Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            using (JsonDocument doc = JsonDocument.ParseValue(ref reader))
            {
                JsonElement root = doc.RootElement;

                if (!root.TryGetProperty("header", out JsonElement headerElement))
                {
                    throw new JsonException("ContentRecord.header is missing.");
                }

                if (!root.TryGetProperty("body", out JsonElement bodyElement))
                {
                    throw new JsonException("ContentRecord.body is missing.");
                }

                ContentRecord record = new ContentRecord();
                record.header = JsonSerializer.Deserialize<ContentHeader>(headerElement.GetRawText(), options);
                record.body = bodyElement.GetRawText();

                if (record.body == null)
                {
                    record.body = string.Empty;
                }

                return record;
            }
        }

        public override void Write(Utf8JsonWriter writer, ContentRecord value, JsonSerializerOptions options)
        {
            writer.WriteStartObject();

            writer.WritePropertyName("header");
            JsonSerializer.Serialize(writer, value.header, options);

            writer.WritePropertyName("body");

            if (string.IsNullOrEmpty(value.body))
            {
                writer.WriteNullValue();
            }
            else
            {
                using (JsonDocument bodyDoc = JsonDocument.Parse(value.body))
                {
                    bodyDoc.RootElement.WriteTo(writer);
                }
            }

            writer.WriteEndObject();
        }
    }
}