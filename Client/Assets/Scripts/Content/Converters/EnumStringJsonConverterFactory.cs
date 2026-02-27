using System;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Assets.Scripts.Content
{
    /// <summary>
    /// 모든 enum 타입에 대해 EnumStringJsonConverter를 자동 생성하는 팩토리
    /// </summary>
    public sealed class EnumStringJsonConverterFactory : JsonConverterFactory
    {
        public override bool CanConvert(Type typeToConvert)
        {
            return typeToConvert.IsEnum;
        }

        public override JsonConverter CreateConverter(Type typeToConvert, JsonSerializerOptions options)
        {
            Type converterType = typeof(EnumStringJsonConverter<>).MakeGenericType(typeToConvert);
            return (JsonConverter)Activator.CreateInstance(converterType);
        }
    }

    /// <summary>
    /// enum <-> string 전용 컨버터
    /// - JSON 저장: 항상 문자열
    /// - JSON 읽기: 문자열 + 숫자 모두 허용
    /// </summary>
    public sealed class EnumStringJsonConverter<TEnum> : JsonConverter<TEnum>
        where TEnum : struct, Enum
    {
        public override TEnum Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            if (reader.TokenType == JsonTokenType.Number)
            {
                int value = reader.GetInt32();
                if (!Enum.IsDefined(typeof(TEnum), value))
                {
                    throw new JsonException($"Invalid enum numeric value {value} for {typeof(TEnum).Name}");
                }
                return (TEnum)Enum.ToObject(typeof(TEnum), value);
            }
            if (reader.TokenType == JsonTokenType.String)
            {
                string text = reader.GetString();
                if (string.IsNullOrEmpty(text))
                {
                    throw new JsonException($"Empty enum string for {typeof(TEnum).Name}");
                }
                if (!Enum.TryParse(text, ignoreCase: false, out TEnum result))
                {
                    throw new JsonException($"Invalid enum string '{text}' for {typeof(TEnum).Name}");
                }
                return result;
            }
            throw new JsonException($"Unexpected token {reader.TokenType} for enum {typeof(TEnum).Name}");
        }

        public override void Write(Utf8JsonWriter writer, TEnum value, JsonSerializerOptions options)
        {
            writer.WriteStringValue(value.ToString());
        }
    }
}