using System.Collections.Generic;

namespace ApiServer.Ops
{
    public sealed class MetaIndex
    {
        private readonly string metaRootAbs;
        private Dictionary<string, string> map = new Dictionary<string, string>();

        public MetaIndex(string metaRootAbs)
        {
            this.metaRootAbs = metaRootAbs;
        }

        public void Build()
        {
            if (string.IsNullOrEmpty(metaRootAbs))
            {
                return;
            }

            if (!Directory.Exists(metaRootAbs))
            {
                return;
            }

            Dictionary<string, string> newMap = new Dictionary<string, string>();

            string[] schemaDirs = Directory.GetDirectories(metaRootAbs);

            for (int si = 0; si < schemaDirs.Length; si++)
            {
                string schemaPath = schemaDirs[si];

                if (string.IsNullOrEmpty(schemaPath))
                {
                    continue;
                }

                string schema = Path.GetFileName(schemaPath);

                if (string.IsNullOrWhiteSpace(schema))
                {
                    continue;
                }

                schema = schema.Trim();

                if (!IsSafeToken(schema))
                {
                    continue;
                }

                string[] files = Directory.GetFiles(schemaPath, "*.meta.json", SearchOption.TopDirectoryOnly);

                for (int fi = 0; fi < files.Length; fi++)
                {
                    string abs = files[fi];

                    if (string.IsNullOrEmpty(abs))
                    {
                        continue;
                    }

                    string fileName = Path.GetFileName(abs);

                    if (string.IsNullOrWhiteSpace(fileName))
                    {
                        continue;
                    }

                    if (!fileName.EndsWith(".meta.json", System.StringComparison.OrdinalIgnoreCase))
                    {
                        continue;
                    }

                    string id = fileName.Substring(0, fileName.Length - ".meta.json".Length).Trim();

                    if (string.IsNullOrWhiteSpace(id))
                    {
                        continue;
                    }

                    if (!IsSafeToken(id))
                    {
                        continue;
                    }

                    string key = MakeKey(schema, id);
                    newMap[key] = abs;
                }
            }

            map = newMap;
        }

        public bool TryGetMetaPath(string schema, string id, out string metaAbsPath)
        {
            metaAbsPath = string.Empty;

            if (string.IsNullOrWhiteSpace(schema))
            {
                return false;
            }

            if (string.IsNullOrWhiteSpace(id))
            {
                return false;
            }

            string s = schema.Trim();
            string i = id.Trim();

            if (!IsSafeToken(s))
            {
                return false;
            }

            if (!IsSafeToken(i))
            {
                return false;
            }

            string key = MakeKey(s, i);

            if (!map.TryGetValue(key, out string abs))
            {
                return false;
            }

            if (!File.Exists(abs))
            {
                return false;
            }

            metaAbsPath = abs;
            return true;
        }

        private static string MakeKey(string schema, string id)
        {
            return schema + "/" + id;
        }

        private static bool IsSafeToken(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return false;
            }

            if (value.Contains("/"))
            {
                return false;
            }

            if (value.Contains("\\"))
            {
                return false;
            }

            if (value.Contains(".."))
            {
                return false;
            }

            if (value.Contains(":"))
            {
                return false;
            }

            return true;
        }
    }
}