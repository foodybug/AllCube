#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using System.IO;

public class TextureAlphaConverterWindow : EditorWindow
{
    private Texture2D m_targetTexture;
    private Color m_targetColor = Color.white;
    private float m_tolerance = 0.05f;
    private bool m_setAsSprite = true;
    private bool m_setAlphaTransparency = true;

    [MenuItem("Tools/AllCube/Texture Alpha Converter")]
    public static void ShowWindow()
    {
        var window = GetWindow<TextureAlphaConverterWindow>("Texture Alpha Converter");
        window.minSize = new Vector2(400, 480);
        window.Show();
    }

    private void OnEnable()
    {
        if (m_targetTexture == null && Selection.activeObject is Texture2D)
        {
            m_targetTexture = Selection.activeObject as Texture2D;
        }
    }

    private void OnGUI()
    {
        GUILayout.Space(10);
        EditorGUILayout.LabelField("Texture Chroma-Key Alpha Converter", EditorStyles.boldLabel);
        EditorGUILayout.LabelField("특정 색상을 알파 0(투명)으로 도려내고 PNG로 저장합니다.", EditorStyles.miniLabel);
        GUILayout.Space(10);

        m_targetTexture = (Texture2D)EditorGUILayout.ObjectField("Target Texture", m_targetTexture, typeof(Texture2D), false);
        m_targetColor = EditorGUILayout.ColorField("Target Background Color", m_targetColor);
        m_tolerance = EditorGUILayout.Slider("Color Distance Tolerance", m_tolerance, 0.0f, 1.0f);
        EditorGUILayout.LabelField("임계값이 높을수록 지정한 색상과 유사한 노이즈 픽셀도 함께 투명해집니다.", EditorStyles.wordWrappedMiniLabel);

        if (m_targetTexture != null)
        {
            GUILayout.Space(15);
            EditorGUILayout.LabelField("Interactive Eyedropper Preview", EditorStyles.boldLabel);
            EditorGUILayout.LabelField("아래 이미지의 픽셀 위를 마우스 클릭하면 해당 색상이 스포이드 추출됩니다.", EditorStyles.miniLabel);
            GUILayout.Space(5);

            float aspect = (float)m_targetTexture.width / m_targetTexture.height;
            float previewW = 200f;
            float previewH = 200f;
            if (aspect > 1f)
            {
                previewH = 200f / aspect;
            }
            else
            {
                previewW = 200f * aspect;
            }

            Rect previewRect = GUILayoutUtility.GetRect(previewW, previewH, GUILayout.ExpandWidth(false));
            GUI.DrawTexture(previewRect, m_targetTexture, ScaleMode.ScaleToFit);

            Event e = Event.current;
            if (e.type == EventType.MouseDown && previewRect.Contains(e.mousePosition))
            {
                float localX = e.mousePosition.x - previewRect.x;
                float localY = e.mousePosition.y - previewRect.y;

                float normX = localX / previewRect.width;
                float normY = 1.0f - (localY / previewRect.height);

                int px = Mathf.Clamp((int)(normX * m_targetTexture.width), 0, m_targetTexture.width - 1);
                int py = Mathf.Clamp((int)(normY * m_targetTexture.height), 0, m_targetTexture.height - 1);

                string assetPath = AssetDatabase.GetAssetPath(m_targetTexture);
                Color picked = GetTexturePixelColor(assetPath, px, py);
                
                m_targetColor = picked;
                Repaint();

                Debug.Log(string.Format("[TextureAlphaConverter] Eyedropper picked Color {0} at pixel: ({1}, {2})", picked, px, py));
                e.Use();
            }
        }

        GUILayout.Space(15);
        EditorGUILayout.LabelField("Import Settings Options", EditorStyles.boldLabel);
        m_setAsSprite = EditorGUILayout.Toggle("Set Texture Type as Sprite", m_setAsSprite);
        m_setAlphaTransparency = EditorGUILayout.Toggle("Enforce AlphaIsTransparency", m_setAlphaTransparency);

        GUILayout.FlexibleSpace();

        EditorGUI.BeginDisabledGroup(m_targetTexture == null);
        if (GUILayout.Button("Apply Transparency and Save PNG", GUILayout.Height(36)))
        {
            ProcessTextureTransparency();
        }
        EditorGUI.EndDisabledGroup();
        GUILayout.Space(10);
    }

    private Color GetTexturePixelColor(string assetPath, int x, int y)
    {
        if (string.IsNullOrEmpty(assetPath) || !File.Exists(assetPath)) return Color.white;

        TextureImporter importer = AssetImporter.GetAtPath(assetPath) as TextureImporter;
        if (importer == null) return Color.white;

        bool oldIsReadable = importer.isReadable;
        TextureImporterCompression oldCompression = importer.textureCompression;

        importer.isReadable = true;
        importer.textureCompression = TextureImporterCompression.Uncompressed;
        importer.SaveAndReimport();

        Texture2D tempTex = AssetDatabase.LoadAssetAtPath<Texture2D>(assetPath);
        Color pickedColor = Color.white;
        if (tempTex != null)
        {
            pickedColor = tempTex.GetPixel(x, y);
        }

        TextureImporter postImporter = AssetImporter.GetAtPath(assetPath) as TextureImporter;
        if (postImporter != null)
        {
            postImporter.isReadable = oldIsReadable;
            postImporter.textureCompression = oldCompression;
            postImporter.SaveAndReimport();
        }

        AssetDatabase.Refresh();
        return pickedColor;
    }

    private void ProcessTextureTransparency()
    {
        if (m_targetTexture == null) return;

        string assetPath = AssetDatabase.GetAssetPath(m_targetTexture);
        if (string.IsNullOrEmpty(assetPath) || !File.Exists(assetPath))
        {
            EditorUtility.DisplayDialog("Error", "Target texture path is invalid or file does not exist.", "OK");
            return;
        }

        // 1. TextureImporter 권한 임시 수정 (읽기 허용 및 압축 해제)
        TextureImporter importer = AssetImporter.GetAtPath(assetPath) as TextureImporter;
        if (importer == null)
        {
            EditorUtility.DisplayDialog("Error", "Could not obtain TextureImporter for " + m_targetTexture.name, "OK");
            return;
        }

        bool oldIsReadable = importer.isReadable;
        TextureImporterCompression oldCompression = importer.textureCompression;

        importer.isReadable = true;
        importer.textureCompression = TextureImporterCompression.Uncompressed;
        importer.SaveAndReimport();

        // 2. 텍스처 데이터 로드
        Texture2D tex = AssetDatabase.LoadAssetAtPath<Texture2D>(assetPath);
        if (tex == null)
        {
            EditorUtility.DisplayDialog("Error", "Failed to load raw Texture2D data.", "OK");
            return;
        }

        // [핵심 보완: 원본의 락과 압축 상태를 완전히 회피하는 독립 RGBA32 복제 텍스처 인스턴스 생성]
        Texture2D writableTex = new Texture2D(tex.width, tex.height, TextureFormat.RGBA32, false);
        writableTex.SetPixels(tex.GetPixels());
        writableTex.Apply();

        Color[] pixels = writableTex.GetPixels();
        int pixelCount = pixels.Length;
        int clearedCount = 0;

        Color targetColor = m_targetColor;
        float tolerance = m_tolerance;

        // 3. 픽셀 순회하며 지정 색상 투명화 (유클리디안 거리 판정)
        for (int i = 0; i < pixelCount; i++)
        {
            Color c = pixels[i];
            float colorDistance = Mathf.Sqrt(
                Mathf.Pow(c.r - targetColor.r, 2) +
                Mathf.Pow(c.g - targetColor.g, 2) +
                Mathf.Pow(c.b - targetColor.b, 2)
            );

            if (colorDistance <= tolerance)
            {
                c.a = 0f;
                pixels[i] = c;
                clearedCount++;
            }
        }

        writableTex.SetPixels(pixels);
        writableTex.Apply();

        // 4. 독립 복제본의 투명 픽셀 데이터를 디스크 물리 PNG로 인코딩하여 영구 저장
        byte[] pngBytes = writableTex.EncodeToPNG();
        DestroyImmediate(writableTex); // 메모리 해제

        if (pngBytes != null)
        {
            string fullPath = Path.Combine(Directory.GetCurrentDirectory(), assetPath);
            File.WriteAllBytes(fullPath, pngBytes);
            Debug.Log(string.Format("[TextureAlphaConverter] Wrote bytes with transparent alpha for: {0}", assetPath));
        }
        else
        {
            EditorUtility.DisplayDialog("Error", "Failed to EncodeToPNG for " + m_targetTexture.name, "OK");
            return;
        }

        // 5. [강제 에셋 리포트 트리거] 디스크의 파일 데이터가 변경되었음을 에셋 DB에 강제 통보
        AssetDatabase.ImportAsset(assetPath, ImportAssetOptions.ForceUpdate);

        // 6. TextureImporter 설정을 최종 복구하면서 스프라이트 알파 투명 채널 체크 강제화
        TextureImporter postImporter = AssetImporter.GetAtPath(assetPath) as TextureImporter;
        if (postImporter != null)
        {
            postImporter.isReadable = oldIsReadable;
            postImporter.textureCompression = oldCompression;
            
            if (m_setAsSprite)
            {
                postImporter.textureType = TextureImporterType.Sprite;
            }
            if (m_setAlphaTransparency)
            {
                postImporter.alphaSource = TextureImporterAlphaSource.FromInput;
                postImporter.alphaIsTransparency = true;
            }
            postImporter.SaveAndReimport();
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        // 윈도우 UI 텍스처 변수 리로딩
        m_targetTexture = AssetDatabase.LoadAssetAtPath<Texture2D>(assetPath);

        EditorUtility.DisplayDialog("Success", 
            string.Format("Successfully cleared {0} background pixels and converted texture alpha channel!", clearedCount), 
            "OK");
    }
}
#endif
