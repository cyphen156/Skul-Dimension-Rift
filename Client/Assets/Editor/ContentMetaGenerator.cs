using Assets.Scripts.Content;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using UnityEditor;
using UnityEngine;

public static class ContentMetaGenerator
{
    [MenuItem("Tools/Content/Generate Meta")]
    public static void GenerateMeta()
    {
        string sourcePath = EditorUtility.OpenFilePanel(
            "Select Content JSON",
            Application.dataPath,
            "json"
        );

        if (string.IsNullOrEmpty(sourcePath) == true)
        {
            return;
        }

        string json = File.ReadAllText(sourcePath, Encoding.UTF8);

        if (string.IsNullOrEmpty(json) == true)
        {
            Debug.LogError("Error : Source JSON is empty");
            return;
        }

        string metaRelativePath = TryGetMetaRelativePath(json);

        if (string.IsNullOrEmpty(metaRelativePath) == true)
        {
            Debug.LogError("Error : Meta path not found in JSON (verify.manifestMetaPath)");
            return;
        }

        string rootFolder = EditorUtility.OpenFolderPanel(
            "Select Output Root Folder (e.g. RemoteDatas)",
            Application.dataPath,
            string.Empty
        );

        if (string.IsNullOrEmpty(rootFolder) == true)
        {
            return;
        }

        byte[] data = File.ReadAllBytes(sourcePath);
        string hash = ComputeSha256(data);

        ContentMeta meta = new ContentMeta();
        meta.version = 1;
        meta.sha256 = hash;

        string metaJson = JsonUtility.ToJson(meta, true);

        string metaOutputPath = Path.Combine(rootFolder, metaRelativePath);

        string directory = Path.GetDirectoryName(metaOutputPath);

        if (string.IsNullOrEmpty(directory) == false &&
            Directory.Exists(directory) == false)
        {
            Directory.CreateDirectory(directory);
        }

        File.WriteAllText(metaOutputPath, metaJson, Encoding.UTF8);

        AssetDatabase.Refresh();
        Debug.Log("Meta generated : " + metaOutputPath);
    }

    private static string TryGetMetaRelativePath(string json)
    {
        ContentManifest manifest = JsonUtility.FromJson<ContentManifest>(json);

        if (manifest == null ||
            manifest.verify == null ||
            string.IsNullOrEmpty(manifest.verify.manifestMetaPath) == true)
        {
            return string.Empty;
        }

        return manifest.verify.manifestMetaPath.Replace('\\', '/');
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
