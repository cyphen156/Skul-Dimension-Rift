using System.IO;

namespace Assets.Scripts.Utility
{
    public enum ContentFileKind
    {
        Meta,
        Data
    }

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

            if (string.IsNullOrEmpty(key) == true)
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

        public static string BuildDataUri(string serverRoot, string basePath, string relativePath)
        {
            string root = NormalizeServerRoot(serverRoot);

            if (string.IsNullOrEmpty(root) == true)
            {
                return string.Empty;
            }

            string baseRel = NormalizeRelativePath(basePath);
            string rel = NormalizeRelativePath(relativePath);

            if (string.IsNullOrEmpty(rel) == true)
            {
                return string.Empty;
            }

            if (string.IsNullOrEmpty(baseRel) == true)
            {
                return root + "/" + rel;
            }

            return root + "/" + baseRel + "/" + rel;
        }

        public static string BuildRemoteUriByKey(string serverRoot, string basePath, ContentFileKind kind, string key)
        {
            if (string.IsNullOrEmpty(key) == true)
            {
                return string.Empty;
            }

            string k = key.Trim();

            if (string.IsNullOrEmpty(k) == true)
            {
                return string.Empty;
            }

            string rel = BuildRemoteRelativePath(kind, k);

            if (string.IsNullOrEmpty(rel) == true)
            {
                return string.Empty;
            }

            return BuildDataUri(serverRoot, basePath, rel);
        }

        public static string BuildLocalPathByKey(string persistentRoot, ContentFileKind kind, string key)
        {
            if (kind == ContentFileKind.Meta)
            {
                return BuildLocalMetaPath(persistentRoot, key);
            }

            return BuildLocalDataPath(persistentRoot, key);
        }

        public static string BuildLocalMetaPath(string persistentRoot, string key)
        {
            if (string.IsNullOrEmpty(persistentRoot) == true)
            {
                return string.Empty;
            }

            if (string.IsNullOrEmpty(key) == true)
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

        public static string BuildLocalDataPath(string persistentRoot, string key)
        {
            if (string.IsNullOrEmpty(persistentRoot) == true)
            {
                return string.Empty;
            }

            if (string.IsNullOrEmpty(key) == true)
            {
                return string.Empty;
            }

            string k = key.Trim();

            if (string.IsNullOrEmpty(k) == true)
            {
                return string.Empty;
            }

            return Path.Combine(persistentRoot, "Data", k, k + ".json");
        }

        private static string BuildRemoteRelativePath(ContentFileKind kind, string key)
        {
            if (kind == ContentFileKind.Meta)
            {
                return "Meta/" + key + ".meta.json";
            }

            return "Data/" + key + "/" + key + ".json";
        }
    }
}