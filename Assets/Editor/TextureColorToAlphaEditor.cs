#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using System.IO;

public class TextureColorToAlphaEditor : EditorWindow
{
    [MenuItem("Tools/AllCube/Clear Logo White Background")]
    public static void ClearLogoWhiteBackground()
    {
        string logoPath = "Assets/Resources/UI/logo.png";
        ClearTextureColor(logoPath);
    }

    [MenuItem("Assets/AllCube/Clear White Background of Selected Texture")]
    public static void ClearSelectedTextureWhite()
    {
        Texture2D selected = Selection.activeObject as Texture2D;
        if (selected == null)
        {
            Debug.LogError("[TextureColorToAlpha] Selected object is not a valid Texture2D! Please select a texture in Project View.");
            EditorUtility.DisplayDialog("Error", "Please select a Texture2D asset in Project Window first.", "OK");
            return;
        }

        string path = AssetDatabase.GetAssetPath(selected);
        ClearTextureColor(path);
    }

    public static void ClearTextureColor(string assetPath)
    {
        if (string.IsNullOrEmpty(assetPath) || !File.Exists(assetPath))
        {
            Debug.LogError("[TextureColorToAlpha] Target texture path is invalid or missing at: " + assetPath);
            return;
        }

        Debug.Log("[TextureColorToAlpha] Starting chroma-key transparency process on: " + assetPath);

        // 1. TextureImporter 셋업 임시 백업 및 읽기 가능 모드로 강제 설정
        TextureImporter importer = AssetImporter.GetAtPath(assetPath) as TextureImporter;
        if (importer == null)
        {
            Debug.LogError("[TextureColorToAlpha] Could not obtain TextureImporter for: " + assetPath);
            return;
        }

        bool oldIsReadable = importer.isReadable;
        TextureImporterCompression oldCompression = importer.textureCompression;

        importer.isReadable = true;
        importer.textureCompression = TextureImporterCompression.Uncompressed;
        importer.SaveAndReimport();

        // 2. Texture2D 오브젝트 로드
        Texture2D tex = AssetDatabase.LoadAssetAtPath<Texture2D>(assetPath);
        if (tex == null)
        {
            Debug.LogError("[TextureColorToAlpha] Failed to load Texture2D at: " + assetPath);
            return;
        }

        // [핵심 보완: 원본의 락과 압축 상태를 완전히 회피하는 독립 RGBA32 복제 텍스처 인스턴스 생성]
        Texture2D writableTex = new Texture2D(tex.width, tex.height, TextureFormat.RGBA32, false);
        writableTex.SetPixels(tex.GetPixels());
        writableTex.Apply();

        Color[] pixels = writableTex.GetPixels();
        int pixelCount = pixels.Length;
        int clearedCount = 0;

        // 3. 픽셀 전수 조사 - RGB가 모두 0.95 초과인 흰색/회백색 계열 색상을 완벽 투명(Alpha = 0f)으로 치환
        for (int i = 0; i < pixelCount; i++)
        {
            Color c = pixels[i];
            
            // RGB 값이 모두 242/255 (약 0.95f) 이상인 밝은 흰색 계열을 흰색 배경 노이즈로 인지
            if (c.r > 0.95f && c.g > 0.95f && c.b > 0.95f)
            {
                c.a = 0f; // 알파 채널 완전 투명화
                pixels[i] = c;
                clearedCount++;
            }
        }

        writableTex.SetPixels(pixels);
        writableTex.Apply();

        // 4. 변환 사항 텍스처 데이터 png 포맷 인코딩 디스크 영구 저장 및 메모리 해제
        byte[] pngBytes = writableTex.EncodeToPNG();
        DestroyImmediate(writableTex);

        if (pngBytes != null)
        {
            string fullPath = Path.Combine(Directory.GetCurrentDirectory(), assetPath);
            File.WriteAllBytes(fullPath, pngBytes);
            Debug.Log(string.Format("[TextureColorToAlpha] Successfully cleared {0} white pixels and overwrote png file at: {1}", clearedCount, assetPath));
        }
        else
        {
            Debug.LogError("[TextureColorToAlpha] Failed to EncodeToPNG for " + assetPath);
        }

        // 5. [강제 에셋 리포트 트리거] 디스크의 파일 데이터가 변경되었음을 에셋 DB에 강제 통보
        AssetDatabase.ImportAsset(assetPath, ImportAssetOptions.ForceUpdate);

        // 6. TextureImporter 설정을 최종 복구하면서 스프라이트 알파 투명 채널 체크 강제화
        TextureImporter postImporter = AssetImporter.GetAtPath(assetPath) as TextureImporter;
        if (postImporter != null)
        {
            postImporter.isReadable = oldIsReadable;
            postImporter.textureCompression = oldCompression;
            postImporter.textureType = TextureImporterType.Sprite; // Sprite/UI 용도로 규격화
            postImporter.alphaSource = TextureImporterAlphaSource.FromInput;
            postImporter.alphaIsTransparency = true; // 투명 알파 채널 활성화 강제화
            postImporter.SaveAndReimport();
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        
        Debug.Log("[TextureColorToAlpha] Texture transparent re-import completed successfully.");
        EditorUtility.DisplayDialog("Success", string.Format("Successfully cleared {0} background pixels and updated texture transparent channel!", clearedCount), "OK");
    }
}
#endif
