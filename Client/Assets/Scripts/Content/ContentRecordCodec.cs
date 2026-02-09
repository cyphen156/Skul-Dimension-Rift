using System;
using System.Text.Json;

namespace Assets.Scripts.Content
{
    public static class ContentRecordCodec
    {
        private const uint ClassMask = 0xFF000000u;
        private const int ClassShift = 24;

        public static readonly JsonSerializerOptions Options = ContentJsonOptions.Options;

        public static ContentRecord Encode<T>(Catagory category, uint staticKey, string id, int version, T body)
        {
            if (string.IsNullOrEmpty(id))
            {
                throw new ArgumentException("id is null or empty.", nameof(id));
            }

            if (version <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(version));
            }

            if (body == null)
            {
                throw new ArgumentNullException(nameof(body));
            }

            ContentRecord record = new ContentRecord();
            record.header = new ContentHeader(category, staticKey, id, version);

            try
            {
                record.body = JsonSerializer.SerializeToElement(body, Options);
            }
            catch (Exception e)
            {
                throw new InvalidOperationException("Failed to serialize body.", e);
            }
            return record;
        }

        public static ContentRecord Encode(Catagory category, uint staticKey, string id, int version, string rawJsonBody)
        {
            if (string.IsNullOrEmpty(id))
            {
                throw new ArgumentException("id is null or empty.", nameof(id));
            }

            if (version <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(version));
            }

            if (rawJsonBody == null)
            {
                throw new ArgumentNullException(nameof(rawJsonBody));
            }

            ContentRecord record = new ContentRecord();
            record.header = new ContentHeader(category, staticKey, id, version);
            using (JsonDocument doc = JsonDocument.Parse(rawJsonBody))
            {
                record.body = doc.RootElement.Clone();
            }
            return record;
        }

        public static bool TryDecode(ContentRecord record, out object body)
        {
            body = null;

            if (record == null)
            {
                return false;
            }

            if (record.body.ValueKind == JsonValueKind.Undefined ||
                record.body.ValueKind == JsonValueKind.Null)
            {
                return false;
            }

            byte classKey = (byte)((record.header.staticKey & ClassMask) >> ClassShift);

            try
            {
                switch (classKey)
                {
                    case 0x00:
                        {
                            body = JsonSerializer.Deserialize<ContentManifest>(record.body, Options);
                            break;
                        }
                    case 0x01:
                        {
                            body = JsonSerializer.Deserialize<ContentMeta>(record.body, Options);
                            break;
                        }
                    case 0x02:
                        {
                            body = JsonSerializer.Deserialize<ContentCatalog>(record.body, Options);
                            break;
                        }
                    default:
                        {
                            return false;
                        }
                }
            }
            catch
            {
                body = null;
                return false;
            }

            return body != null;
        }

        public static bool TryDecode<T>(ContentRecord record, out T body)
        {
            body = default;

            object decoded;
            if (!TryDecode(record, out decoded))
            {
                return false;
            }

            if (decoded == null)
            {
                return false;
            }

            if (decoded.GetType() != typeof(T))
            {
                return false;
            }

            body = (T)decoded;
            return true;
        }
    }
}
