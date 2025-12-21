using System.Collections.Generic;

namespace ApiServer.Ops
{
    public sealed class MetaIndex
    {
        private readonly string metaRootAbs;
        private readonly Dictionary<string, string> map = new Dictionary<string, string>();

        public MetaIndex(string metaRootAbs)
        {
            this.metaRootAbs = metaRootAbs;
        }

        public void Build()
        {
            map.Clear();

            if (string.IsNullOrEmpty(metaRootAbs) == true)
            {
                return;
            }

            if (Directory.Exists(metaRootAbs) == false)
            {
                return;
            }

            string[] files = Directory.GetFiles(metaRootAbs, "*.meta.json", SearchOption.AllDirectories);

            for (int i = 0; i < files.Length; i++)
            {
                string abs = files[i];

                if (string.IsNullOrEmpty(abs) == true)
                {
                    continue;
                }

                string rel = Path.GetRelativePath(metaRootAbs, abs);
                rel = rel.Replace('\\', '/');

                if (string.IsNullOrEmpty(rel) == true)
                {
                    continue;
                }

                string[] tokens = rel.Split('/');

                if (tokens.Length != 2)
                {
                    continue;
                }

                string schema = tokens[0].Trim();
                string file = tokens[1].Trim();

                if (string.IsNullOrEmpty(schema) == true)
                {
                    continue;
                }

                if (string.IsNullOrEmpty(file) == true)
                {
                    continue;
                }

                if (file.EndsWith(".meta.json", System.StringComparison.OrdinalIgnoreCase) == false)
                {
                    continue;
                }

                string id = file.Substring(0, file.Length - ".meta.json".Length).Trim();

                if (string.IsNullOrEmpty(id) == true)
                {
                    continue;
                }

                string key = MakeKey(schema, id);
                map[key] = abs;
            }
        }

        public bool TryGetMetaPath(string schema, string id, out string metaAbsPath)
        {
            metaAbsPath = string.Empty;

            if (string.IsNullOrWhiteSpace(schema) == true)
            {
                return false;
            }

            if (string.IsNullOrWhiteSpace(id) == true)
            {
                return false;
            }

            string s = schema.Trim();
            string i = id.Trim();

            if (IsSafeToken(s) == false)
            {
                return false;
            }

            if (IsSafeToken(i) == false)
            {
                return false;
            }

            string key = MakeKey(s, i);

            if (map.TryGetValue(key, out string abs) == false)
            {
                return false;
            }

            if (File.Exists(abs) == false)
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
            if (string.IsNullOrWhiteSpace(value) == true)
            {
                return false;
            }

            if (value.Contains("/") == true)
            {
                return false;
            }

            if (value.Contains("\\") == true)
            {
                return false;
            }

            if (value.Contains("..") == true)
            {
                return false;
            }

            if (value.Contains(":") == true)
            {
                return false;
            }

            return true;
        }
    }
}