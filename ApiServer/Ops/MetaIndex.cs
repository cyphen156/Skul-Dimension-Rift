namespace ApiServer.Ops
{
    public sealed class MetaIndex
    {
        private readonly Dictionary<string, string> map
            = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        private readonly string metaRoot;

        public MetaIndex(string metaRoot)
        {
            this.metaRoot = metaRoot;
        }

        public void Build()
        {
            map.Clear();

            if (Directory.Exists(metaRoot) == false)
            {
                return;
            }

            string[] files = Directory.GetFiles(
                metaRoot,
                "*.meta.json",
                SearchOption.AllDirectories
            );

            for (int i = 0; i < files.Length; i++)
            {
                string file = files[i];
                string name = Path.GetFileName(file);

                if (string.IsNullOrEmpty(name) == true)
                {
                    continue;
                }

                if (map.ContainsKey(name) == true)
                {
                    continue;
                }

                map.Add(name, file);
            }
        }

        public bool TryGetMetaPath(string key, out string metaAbsPath)
        {
            metaAbsPath = string.Empty;

            if (string.IsNullOrEmpty(key) == true)
            {
                return false;
            }

            string fileName = key + ".meta.json";
            return map.TryGetValue(fileName, out metaAbsPath);
        }
    }
}