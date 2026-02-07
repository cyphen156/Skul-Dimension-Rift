using Assets.Scripts.Data;
using System;
using UnityEngine;

namespace Assets.Scripts.Content
{
    /// <summary>
    /// 콘텐츠 레코드를 각 타입에 맞게 직렬화/역직렬화 해주는 코덱
    /// 포맷 타입에 따라 바이너리 <-> 텍스트를 오가기 때문에 코덱으로 결정
    /// </summary>
    public static class ContentRecordCodec
    {
        private const uint ClassMask = 0xFF000000u;
        private const int ClassShift = 24;

        /// <summary>
        /// T타입을 ContentRecord형식으로 변환
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
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

            ContentHeader header = new ContentHeader(category, staticKey, id, version);

            string jsonBody;
            try
            {
                jsonBody = JsonUtility.ToJson(body);
            }
            catch (Exception e)
            {
                throw new InvalidOperationException("Failed to serialize body with JsonUtility.", e);
            }

            if (jsonBody == null)
            {
                jsonBody = string.Empty;
            }

            ContentRecord record = new ContentRecord();
            record.header = header;
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

            ContentHeader header = new ContentHeader(category, staticKey, id, version);

            ContentRecord record = new ContentRecord();
            record.header = header;
            record.body = rawJsonBody;

            return record;
        }

        /// <summary>
        /// ContentRecord -> body(object)
        /// - 타입은 record.header.staticKey에서 classKey를 뽑아 결정한다.
        /// </summary>
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

            switch (classKey)
            {
                case 0x00:
                    {
                        body = JsonUtility.FromJson<ContentManifest>(record.body);
                        break;
                    }
                case 0x01:
                    {
                        body = JsonUtility.FromJson<ContentMeta>(record.body);
                        break;
                    }
                case 0x02:
                    {
                        body = JsonUtility.FromJson<ContentCatalog>(record.body);
                        break;
                    }
                default:
                    {
                        body = null;
                        break;
                    }
            }

            return body != null;
        }
    }
}
