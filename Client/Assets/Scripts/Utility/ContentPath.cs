using System;

namespace Assets.Scripts.Utility
{
    public static class ContentPath
    {
        public static string BuildMetaUri(string verifyRoot, string metaApiTemplate, string id, string schema, string platform)
        {
            if (string.IsNullOrEmpty(metaApiTemplate))
            {
                return string.Empty;
            }

            if (string.IsNullOrEmpty(id))
            {
                return string.Empty;
            }

            if (string.IsNullOrEmpty(schema))
            {
                return string.Empty;
            }

            string templ = metaApiTemplate.Trim();

            string rel = templ
                .Replace("{Id}", id.Trim())
                .Replace("{Schema}", schema.Trim())
                .Replace("{Platform}", platform.Trim());

            rel = NormalizeRelativePath(rel);

            if (string.IsNullOrEmpty(rel))
            {
                return string.Empty;
            }

            if (IsAbsoluteHttpUri(rel))
            {
                return rel;
            }

            string root = NormalizeServerRoot(verifyRoot);

            if (string.IsNullOrEmpty(root))
            {
                return string.Empty;
            }

            return root + "/" + rel;
        }


        public static string NormalizeServerRoot(string serverRoot)
        {
            if (string.IsNullOrEmpty(serverRoot))
            {
                return string.Empty;
            }

            string root = serverRoot.Trim();

            while (root.EndsWith("/"))
            {
                root = root.Substring(0, root.Length - 1);
            }

            return root;
        }

        public static string NormalizeRelativePath(string path)
        {
            if (string.IsNullOrEmpty(path))
            {
                return string.Empty;
            }

            string p = path.Trim();
            p = p.Replace('\\', '/');

            while (p.StartsWith("/"))
            {
                p = p.Substring(1);
            }

            while (p.Contains("//"))
            {
                p = p.Replace("//", "/");
            }

            return p;
        }

        private static bool IsAbsoluteHttpUri(string uri)
        {
            if (string.IsNullOrEmpty(uri))
            {
                return false;
            }

            if (uri.StartsWith("http://", StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }

            if (uri.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }

            return false;
        }
    }
}
