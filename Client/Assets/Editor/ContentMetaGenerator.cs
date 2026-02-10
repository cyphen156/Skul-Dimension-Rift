using Assets.Scripts.Content;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using UnityEditor;
using UnityEngine;

public static class ContentMetaGenerator
{
    private const string MetaFolderName = "Meta";
    private const string MetaExtension = ".meta.json";

    [MenuItem("Tools/Content/Generate Meta (RemoteDatasRoot)")]
    public static void GenerateAll()
    {
        string remoteDatasRoot = EditorUtility.OpenFolderPanel(
            "Select RemoteDatas Root Folder",
            Application.dataPath,
            string.Empty
        );

        if (string.IsNullOrEmpty(remoteDatasRoot) == true)
        {
            return;
        }

        string rootFull = Path.GetFullPath(remoteDatasRoot);
        string metaRoot = Path.Combine(rootFull, MetaFolderName);

        if (Directory.Exists(metaRoot) == false)
        {
            Directory.CreateDirectory(metaRoot);
        }

        string[] files = Directory.GetFiles(
            rootFull,
            "*",
            SearchOption.AllDirectories
        );

        int generated = 0;

        for (int i = 0; i < files.Length; i++)
        {
            string absPath = files[i];

            if (ShouldSkip(absPath) == true)
            {
                continue;
            }

            byte[] data = File.ReadAllBytes(absPath);

            if (data == null || data.Length == 0)
            {
                continue;
            }

            string relPath = Path.GetRelativePath(rootFull, absPath);
            relPath = relPath.Replace('\\', '/');

            string key = ToKeyFromRelativePath(relPath);

            if (string.IsNullOrEmpty(key) == true)
            {
                continue;
            }

            string metaOutputPath = Path.Combine(metaRoot, key + MetaExtension);

            string hash = ComputeSha256(data);

            ContentMeta meta = new ContentMeta();
            meta.version = 1;
            meta.sha256 = hash;
            string metaJson = JsonUtility.ToJson(meta, true);

            File.WriteAllText(metaOutputPath, metaJson, Encoding.UTF8);
            generated++;
        }

        AssetDatabase.Refresh();
        Debug.Log("Meta generated (count): " + generated);
    }

    private static bool ShouldSkip(string absolutePath)
    {
        if (File.Exists(absolutePath) == false)
        {
            return true;
        }

        string normalized = absolutePath.Replace('\\', '/');

        if (normalized.Contains("/" + MetaFolderName + "/") == true)
        {
            return true;
        }

        if (normalized.EndsWith(".meta") == true)
        {
            return true;
        }

        if (normalized.EndsWith(MetaExtension) == true)
        {
            return true;
        }

        return false;
    }

    private static string ToKeyFromRelativePath(string relativePath)
    {
        if (string.IsNullOrEmpty(relativePath) == true)
        {
            return string.Empty;
        }

        string p = relativePath.Trim();
        p = p.Replace('\\', '/');

        while (p.StartsWith("/") == true)
        {
            p = p.Substring(1);
        }

        while (p.Contains("//") == true)
        {
            p = p.Replace("//", "/");
        }

        if (p.Contains("..") == true)
        {
            return string.Empty;
        }

        string withoutExt = Path.ChangeExtension(p, null);
        withoutExt = withoutExt.Replace('\\', '/');

        string key = withoutExt.Replace('/', '_');

        if (string.IsNullOrEmpty(key) == true)
        {
            return string.Empty;
        }

        return key;
    }

    private static string ComputeSha256(byte[] data)
    {
        using (SHA256 sha256 = SHA256.Create())
        {
            byte[] hashBytes = sha256.ComputeHash(data);
            StringBuilder sb = new StringBuilder();

            for (int i = 0; i < hashBytes.Length; i++)
            {
                sb.Append(hashBytes[i].ToString("x2"));
            }

            return sb.ToString();
        }
    }
}
