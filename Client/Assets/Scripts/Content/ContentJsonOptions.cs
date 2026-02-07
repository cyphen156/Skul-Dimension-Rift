using System.Text.Json;

namespace Assets.Scripts.Content
{
    public static class ContentJsonOptions
    {
        public static readonly JsonSerializerOptions Options = Create();

        private static JsonSerializerOptions Create()
        {
            JsonSerializerOptions options = new JsonSerializerOptions();
            options.IncludeFields = true;
            options.PropertyNameCaseInsensitive = false;

            options.Converters.Add(new ContentRecordJsonConverter());
            options.Converters.Add(new JsonHexUInt32Converter());
            options.Converters.Add(new EnumStringJsonConverter<Catagory>());

            return options;
        }
    }
}
