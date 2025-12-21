namespace Assets.Scripts.Utility
{
    public static class ContentPath
    {
        public static string BuildMetaUri(string verifyRoot, string metaApiTemplate, string id, string schema)
        {
            if (string.IsNullOrEmpty(metaApiTemplate) == true)
            {
                return string.Empty;
            }

            if (string.IsNullOrEmpty(id) == true)
            {
                return string.Empty;
            }

            if (string.IsNullOrEmpty(schema) == true)
            {
                return string.Empty;
            }

            string templ = metaApiTemplate.Trim();
            string rel = templ.Replace("{Id}", id.Trim()).Replace("{Schema}", schema.Trim());
            rel = NormalizeRelativePath(rel);

            if (string.IsNullOrEmpty(rel) == true)
            {
                return string.Empty;
            }

            if (IsAbsoluteHttpUri(rel) == true)
            {
                return rel;
            }

            string root = NormalizeServerRoot(verifyRoot);

            if (string.IsNullOrEmpty(root) == true)
            {
                return string.Empty;
            }

            return root + "/" + rel;
        }

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

        private static bool IsAbsoluteHttpUri(string uri)
        {
            if (string.IsNullOrEmpty(uri) == true)
            {
                return false;
            }

            if (uri.StartsWith("http://", System.StringComparison.OrdinalIgnoreCase) == true)
            {
                return true;
            }

            if (uri.StartsWith("https://", System.StringComparison.OrdinalIgnoreCase) == true)
            {
                return true;
            }

            return false;
        }
    }
}
