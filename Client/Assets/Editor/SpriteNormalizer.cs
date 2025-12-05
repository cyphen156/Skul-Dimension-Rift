using System.IO;
using UnityEditor;
using UnityEngine;

public class SpriteNormalizer : EditorWindow
{
    private string rootFolder = "Assets/AssetNormalizeDirectory";
    private int tileSize = 32;

    private enum AnchorPosition
    {
        BottomLeft,
        BottomCenter,
        BottomRight,
        Center,
        TopLeft,
        TopCenter,
        TopRight
    }

    private AnchorPosition anchor = AnchorPosition.BottomLeft;

    [MenuItem("Tools/Sprites/Normalize Canvas To Grid")]
    public static void OpenWindow()
    {
        SpriteNormalizer window = GetWindow<SpriteNormalizer>();
        window.titleContent = new GUIContent("Sprite Canvas Normalizer");
        window.Show();
    }

    private void OnGUI()
    {
        EditorGUILayout.LabelField("Sprite Canvas Normalizer", EditorStyles.boldLabel);
        EditorGUILayout.Space();

        EditorGUILayout.LabelField("Root Folder (search scope)");
        rootFolder = EditorGUILayout.TextField(rootFolder);

        EditorGUILayout.LabelField("Grid Size (pixels per tile)");
        tileSize = EditorGUILayout.IntField(tileSize);

        EditorGUILayout.Space();

        EditorGUILayout.LabelField("Pivot / Anchor (원본이 새 캔버스 안에서 붙을 위치)");
        anchor = (AnchorPosition)EditorGUILayout.EnumPopup(anchor);

        EditorGUILayout.Space();

        if (GUILayout.Button("Scan And Normalize Textures"))
        {
            NormalizeAllTexturesInFolder();
        }
    }

    private void NormalizeAllTexturesInFolder()
    {
        if (!AssetDatabase.IsValidFolder(rootFolder))
        {
            Debug.LogError("[SpriteCanvasNormalizer] 유효하지 않은 폴더 경로입니다: " + rootFolder);
            return;
        }

        string[] guids = AssetDatabase.FindAssets("t:Texture2D", new[] { rootFolder });

        int processedCount = 0;
        int skippedCount = 0;

        for (int i = 0; i < guids.Length; i++)
        {
            string path = AssetDatabase.GUIDToAssetPath(guids[i]);

            Texture2D texture = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
            if (texture == null)
            {
                skippedCount++;
                continue;
            }

            int originalWidth = texture.width;
            int originalHeight = texture.height;

            int newWidth = NormalizeSize(originalWidth, tileSize);
            int newHeight = NormalizeSize(originalHeight, tileSize);

            if (newWidth == originalWidth && newHeight == originalHeight)
            {
                skippedCount++;
                continue;
            }

            bool success = NormalizeTextureCanvas(path, newWidth, newHeight, anchor);
            if (success)
            {
                processedCount++;
            }
            else
            {
                skippedCount++;
            }
        }

        Debug.Log(
            string.Format(
                "[SpriteCanvasNormalizer] 처리 완료 - 수정: {0}개, 건너뜀: {1}개",
                processedCount,
                skippedCount
            )
        );
    }

    private static int NormalizeSize(int size, int unit)
    {
        if (size <= unit)
        {
            return unit;
        }

        int remainder = size % unit;

        if (remainder == 0)
        {
            return size;
        }

        return size + (unit - remainder);
    }

    private static bool NormalizeTextureCanvas(
        string assetPath,
        int newWidth,
        int newHeight,
        AnchorPosition anchor
    )
    {
        TextureImporter importer = AssetImporter.GetAtPath(assetPath) as TextureImporter;
        if (importer == null)
        {
            Debug.LogError("[SpriteCanvasNormalizer] TextureImporter 를 찾을 수 없습니다: " + assetPath);
            return false;
        }

        // 기존 설정 보관
        bool originalIsReadable = importer.isReadable;
        TextureImporterCompression originalCompression = importer.textureCompression;
        TextureImporterNPOTScale originalNpotScale = importer.npotScale;

        // 읽기 가능 + 압축 해제 + NPOT 그대로 사용
        importer.isReadable = true;
        importer.textureCompression = TextureImporterCompression.Uncompressed;
        importer.npotScale = TextureImporterNPOTScale.None;

        AssetDatabase.ImportAsset(assetPath, ImportAssetOptions.ForceUpdate);

        Texture2D sourceTexture = AssetDatabase.LoadAssetAtPath<Texture2D>(assetPath);
        if (sourceTexture == null)
        {
            Debug.LogError("[SpriteCanvasNormalizer] Texture2D 로드 실패: " + assetPath);
            return false;
        }

        int srcWidth = sourceTexture.width;
        int srcHeight = sourceTexture.height;

        if (srcWidth > newWidth || srcHeight > newHeight)
        {
            Debug.LogError(
                string.Format(
                    "[SpriteCanvasNormalizer] 새 캔버스 크기가 원본보다 작습니다. assetPath={0}, src=({1},{2}), new=({3},{4})",
                    assetPath,
                    srcWidth,
                    srcHeight,
                    newWidth,
                    newHeight
                )
            );
            return false;
        }

        // 새 텍스처 생성 (투명 배경)
        Texture2D newTexture = new Texture2D(newWidth, newHeight, TextureFormat.RGBA32, false);
        Color32[] clearPixels = new Color32[newWidth * newHeight];

        Color32 clearColor = new Color32(0, 0, 0, 0);
        for (int i = 0; i < clearPixels.Length; i++)
        {
            clearPixels[i] = clearColor;
        }

        newTexture.SetPixels32(clearPixels);

        // 원본 픽셀
        Color32[] sourcePixels = sourceTexture.GetPixels32();

        // 앵커에 따른 오프셋 계산
        int offsetX = 0;
        int offsetY = 0;
        GetAnchorOffset(anchor, srcWidth, srcHeight, newWidth, newHeight, out offsetX, out offsetY);

        // 원본 픽셀 복사
        for (int y = 0; y < srcHeight; y++)
        {
            for (int x = 0; x < srcWidth; x++)
            {
                int srcIndex = x + y * srcWidth;
                int dstX = x + offsetX;
                int dstY = y + offsetY;

                if (srcIndex < 0 || srcIndex >= sourcePixels.Length)
                {
                    continue;
                }

                if (dstX < 0 || dstX >= newWidth || dstY < 0 || dstY >= newHeight)
                {
                    continue;
                }

                int dstIndex = dstX + dstY * newWidth;
                clearPixels[dstIndex] = sourcePixels[srcIndex];
            }
        }

        newTexture.SetPixels32(clearPixels);
        newTexture.Apply();

        // PNG로 다시 저장
        byte[] pngData = newTexture.EncodeToPNG();
        if (pngData == null || pngData.Length == 0)
        {
            Debug.LogError("[SpriteCanvasNormalizer] PNG 인코딩 실패: " + assetPath);
            return false;
        }

        string fullPath = Path.GetFullPath(assetPath);
        try
        {
            File.WriteAllBytes(fullPath, pngData);
        }
        catch (System.Exception e)
        {
            Debug.LogError("[SpriteCanvasNormalizer] 파일 쓰기 실패: " + fullPath + "\n" + e);
            return false;
        }

        // 임시 텍스처 메모리 해제
        Object.DestroyImmediate(newTexture);

        // 임포터 설정 복원
        importer.isReadable = originalIsReadable;
        importer.textureCompression = originalCompression;
        importer.npotScale = originalNpotScale;

        AssetDatabase.ImportAsset(assetPath, ImportAssetOptions.ForceUpdate);

        Debug.Log(
            string.Format(
                "[SpriteCanvasNormalizer] 캔버스 확장 완료: {0} ({1}x{2} → {3}x{4})",
                assetPath,
                srcWidth,
                srcHeight,
                newWidth,
                newHeight
            )
        );

        return true;
    }

    private static void GetAnchorOffset(
        AnchorPosition anchor,
        int srcWidth,
        int srcHeight,
        int newWidth,
        int newHeight,
        out int offsetX,
        out int offsetY
    )
    {
        int deltaX = newWidth - srcWidth;
        int deltaY = newHeight - srcHeight;

        switch (anchor)
        {
            case AnchorPosition.BottomLeft:
                {
                    offsetX = 0;
                    offsetY = 0;
                    break;
                }
            case AnchorPosition.BottomCenter:
                {
                    offsetX = deltaX / 2;
                    offsetY = 0;
                    break;
                }
            case AnchorPosition.BottomRight:
                {
                    offsetX = deltaX;
                    offsetY = 0;
                    break;
                }
            case AnchorPosition.Center:
                {
                    offsetX = deltaX / 2;
                    offsetY = deltaY / 2;
                    break;
                }
            case AnchorPosition.TopLeft:
                {
                    offsetX = 0;
                    offsetY = deltaY;
                    break;
                }
            case AnchorPosition.TopCenter:
                {
                    offsetX = deltaX / 2;
                    offsetY = deltaY;
                    break;
                }
            case AnchorPosition.TopRight:
                {
                    offsetX = deltaX;
                    offsetY = deltaY;
                    break;
                }
            default:
                {
                    offsetX = 0;
                    offsetY = 0;
                    break;
                }
        }
    }
}
