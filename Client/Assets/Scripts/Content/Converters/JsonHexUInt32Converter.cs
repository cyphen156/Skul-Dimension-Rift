using System;
using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Assets.Scripts.Content
{
    public sealed class JsonHexUInt32Converter : JsonConverter<uint>
    {
        public override uint Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            if (reader.TokenType == JsonTokenType.Number)
            {
                return reader.GetUInt32();
            }

            if (reader.TokenType != JsonTokenType.String)
            {
                throw new JsonException("Expected uint as number or hex string.");
            }

            string text = reader.GetString();
            if (string.IsNullOrEmpty(text))
            {
                throw new JsonException("Empty string is not valid for uint.");
            }

            string hex = text;
            if (hex.StartsWith("0x", StringComparison.OrdinalIgnoreCase))
            {
                hex = hex.Substring(2);
            }

            if (hex.Length != 8)
            {
                throw new JsonException("Hex uint must be 8 characters (optionally prefixed with 0x).");
            }

            uint value;
            if (!uint.TryParse(hex, NumberStyles.AllowHexSpecifier, CultureInfo.InvariantCulture, out value))
            {
                throw new JsonException("Invalid hex uint string.");
            }

            return value;
        }

        public override void Write(Utf8JsonWriter writer, uint value, JsonSerializerOptions options)
        {
            writer.WriteStringValue("0x" + value.ToString("X8", CultureInfo.InvariantCulture));
        }
    }
}
