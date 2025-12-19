using System.IO;

namespace Assets.Scripts.Utility
{
    public static class ContentPath
    {
        public static string NormalizeServerRoot(string serverRoot)
        {
            if (string.IsNullOrEmpty(serverRoot) == true)
            {
                return string.Empty;
            }

            string root = serverRoot.Trim();

            while (root.EndsWith("/") == true)
            {
                root = root.Substring(0, root.Length - 1);
            }

            return root;
        }

        public static string NormalizeRelativePath(string path)
        {
            if (string.IsNullOrEmpty(path) == true)
            {
                return string.Empty;
            }

            string p = path.Trim();
            p = p.Replace('\\', '/');

            while (p.StartsWith("/") == true)
            {
                p = p.Substring(1);
            }

            while (p.Contains("//") == true)
            {
                p = p.Replace("//", "/");
            }

            return p;
        }

        public static string BuildMetaUri(string serverRoot, string metaApiTemplate, string key)
        {
            string root = NormalizeServerRoot(serverRoot);

            if (string.IsNullOrEmpty(root) == true)
            {
                return string.Empty;
            }

            string templ = NormalizeRelativePath(metaApiTemplate);

            if (string.IsNullOrEmpty(templ) == true)
            {
                return string.Empty;
            }

            string k = key.Trim();

            if (string.IsNullOrEmpty(k) == true)
            {
                return string.Empty;
            }

            string rel = templ.Replace("{Key}", k);
            return root + "/" + rel;
        }

        public static string BuildLocalMetaPath(string persistentRoot, string key)
        {
            if (string.IsNullOrEmpty(persistentRoot) == true)
            {
                return string.Empty;
            }

            string k = key.Trim();

            if (string.IsNullOrEmpty(k) == true)
            {
                return string.Empty;
            }

            return Path.Combine(persistentRoot, "Meta", k + ".meta.json");
        }
    }
}