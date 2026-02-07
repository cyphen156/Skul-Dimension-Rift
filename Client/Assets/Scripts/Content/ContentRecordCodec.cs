using Assets.Scripts.Data;
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

            string jsonBody;
            try
            {
                jsonBody = JsonSerializer.Serialize(body, Options);
            }
            catch (Exception e)
            {
                throw new InvalidOperationException("Failed to serialize body with System.Text.Json.", e);
            }

            if (jsonBody == null)
            {
                jsonBody = string.Empty;
            }

            record.body = jsonBody;
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
            record.body = rawJsonBody;
            return record;
        }

        public static bool TryDecode(ContentRecord record, out object body)
        {
            body = null;

            if (record == null)
            {
                return false;
            }

            if (string.IsNullOrEmpty(record.body))
            {
                return false;
            }

            uint staticKey;
            if (!DomainKeyParser.TryParseStaticKey(record.header.staticKey, out staticKey))
            {
                return false;
            }

            byte classKey = (byte)((staticKey & ClassMask) >> ClassShift);

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
    }
}
