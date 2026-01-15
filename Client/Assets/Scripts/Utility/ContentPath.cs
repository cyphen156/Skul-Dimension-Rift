using System.IO;
using UnityEngine;

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

        public static string BuildLocalMetaPath(string schema, string id)
        {
            if (string.IsNullOrEmpty(schema) || string.IsNullOrEmpty(id))
            {
                return string.Empty;
            }

            return Path.Combine(
                Application.persistentDataPath,
                "Meta",
                schema,
                id + ".meta.json"
            );
        }

        public static string BuildLocalPayloadPath(string schema, string id)
        {
            if (string.IsNullOrEmpty(schema) || string.IsNullOrEmpty(id))
            {
                return string.Empty;
            }

            return Path.Combine(
                Application.persistentDataPath,
                "Data",
                schema,
                id + ".json"
            );
        }

        public static string BuildLocalBundlePath(string schema, string bundleId, string bundleSha256)
        {
            if (string.IsNullOrEmpty(schema) || string.IsNullOrEmpty(bundleId) || string.IsNullOrEmpty(bundleSha256))
            {
                return string.Empty;
            }

            return Path.Combine(
                Application.persistentDataPath,
                "Bundles",
                schema,
                bundleId,
                bundleSha256 + ".bundle"
            );
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

            if (uri.StartsWith("http://", System.StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }

            if (uri.StartsWith("https://", System.StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }

            return false;
        }

        /// <summary>
        /// 플랫폼 정보를 정규화하여 리턴합니다.
        /// 어드레서블 데이터를 다운로드 할때 사용할 예정입니다.
        /// </summary>
        /// <returns></returns>
        public static string GetPlatformFolder()
        {
            RuntimePlatform p = Application.platform;

            switch (p)
            {
                case RuntimePlatform.Android:
                    {
                        return "Android";
                    }
                case RuntimePlatform.IPhonePlayer:
                    {
                        return "Ios";
                    }
                case RuntimePlatform.WindowsPlayer:
                case RuntimePlatform.WindowsEditor:
                    {
                        return "Windows";
                    }
                case RuntimePlatform.OSXPlayer:
                case RuntimePlatform.OSXEditor:
                    {
                        return "Osx";
                    }
                case RuntimePlatform.WebGLPlayer:
                    {
                        return "Web";
                    }
                default:
                    {
                        return p.ToString();
                    }
            }
        }
    }
}
