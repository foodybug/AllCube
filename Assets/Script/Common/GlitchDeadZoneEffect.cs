using System.Collections;
using UnityEngine;

/// <summary>
/// Floor_DeadZone 오브젝트에 글리치(Glitch) 효과 텍스처와 실시간 노이즈/오프셋 애니메이션을 부여하는 컴포넌트
/// </summary>
public class GlitchDeadZoneEffect : MonoBehaviour
{
    private MeshRenderer m_renderer;
    private Material m_material;
    private Texture2D m_glitchTexture;
    private Color32[] m_pixels;

    private int m_width = 128;
    private int m_height = 128;

    private float m_timer = 0f;
    private float m_nextGlitchInterval = 0.04f;

    // AllCube 게임 아트 테마와 정화(Harmonize)된 글리치 컬러 팔레트 (레드/크림슨, 시안, 골드, 슬레이트 챠콜)
    private static readonly Color32 ColorDark = new Color32(18, 20, 30, 255);       // 슬레이트 챠콜 블랙
    private static readonly Color32 ColorRed = new Color32(230, 43, 43, 255);       // 장애물/위험 레드
    private static readonly Color32 ColorCrimson = new Color32(255, 77, 77, 255);   // 활성 크림슨
    private static readonly Color32 ColorCyan = new Color32(0, 229, 255, 255);      // 사이버 시안
    private static readonly Color32 ColorGold = new Color32(255, 183, 3, 255);      // 코인/에너지 골드
    private static readonly Color32 ColorWhite = new Color32(240, 240, 250, 255);

    private void Awake()
    {
        m_renderer = GetComponent<MeshRenderer>();
        if (m_renderer != null)
        {
            Shader shader = Shader.Find("Unlit/Texture");
            if (shader == null) shader = Shader.Find("Mobile/Unlit (Supports Lightmap)");
            if (shader == null) shader = Shader.Find("Sprites/Default");

            m_material = new Material(shader != null ? shader : m_renderer.material.shader);
            m_renderer.material = m_material;
        }

        CreateGlitchTexture();
    }

    private void CreateGlitchTexture()
    {
        m_glitchTexture = new Texture2D(m_width, m_height, TextureFormat.RGBA32, false);
        m_glitchTexture.filterMode = FilterMode.Point; // 디지탈 픽셀 글리치 느낌을 위해 Point 필터 사용
        m_glitchTexture.wrapMode = TextureWrapMode.Repeat;
        m_pixels = new Color32[m_width * m_height];

        GenerateBaseGlitchPattern();
        m_glitchTexture.SetPixels32(m_pixels);
        m_glitchTexture.Apply();

        if (m_material != null)
        {
            m_material.mainTexture = m_glitchTexture;
            m_material.mainTextureScale = new Vector2(15f, 3f); // 가로 세로 텍스처 타일링 비율
        }
    }

    private void GenerateBaseGlitchPattern()
    {
        for (int y = 0; y < m_height; y++)
        {
            Color32 lineBaseColor = ColorDark;
            bool isGlitchLine = (y % 4 == 0) || (Random.value < 0.18f);

            if (isGlitchLine)
            {
                float randVal = Random.value;
                if (randVal < 0.40f) lineBaseColor = ColorRed;
                else if (randVal < 0.65f) lineBaseColor = ColorCrimson;
                else if (randVal < 0.85f) lineBaseColor = ColorCyan;
                else lineBaseColor = ColorGold;
            }

            for (int x = 0; x < m_width; x++)
            {
                int idx = y * m_width + x;

                if (Random.value < 0.09f)
                {
                    m_pixels[idx] = (Random.value < 0.5f) ? ColorRed : ColorCyan;
                }
                else if (Random.value < 0.03f)
                {
                    m_pixels[idx] = ColorGold;
                }
                else
                {
                    m_pixels[idx] = lineBaseColor;
                }
            }
        }
    }

    private void Update()
    {
        if (m_material == null || m_glitchTexture == null) return;

        m_timer += Time.deltaTime;
        if (m_timer >= m_nextGlitchInterval)
        {
            m_timer = 0f;
            m_nextGlitchInterval = Random.Range(0.03f, 0.08f);

            TriggerGlitchFrame();
        }
    }

    private void TriggerGlitchFrame()
    {
        // 1. 텍스처 UV 수평/수직 무작위 점프 (Glitch Shift)
        float xOffset = (Random.value > 0.35f) ? Random.Range(-0.4f, 0.4f) : 0f;
        float yOffset = Random.Range(0f, 1f);
        m_material.mainTextureOffset = new Vector2(xOffset, yOffset);

        // 2. 무작위 가로 스캔라인 패치 변형 (Digital Corruption Noise)
        int startY = Random.Range(0, m_height - 12);
        int bandHeight = Random.Range(3, 14);
        Color32 glitchColor = (Random.value < 0.5f) ? ColorRed : ColorCrimson;
        if (Random.value < 0.25f) glitchColor = ColorCyan;

        for (int y = startY; y < Mathf.Min(m_height, startY + bandHeight); y++)
        {
            for (int x = 0; x < m_width; x++)
            {
                int idx = y * m_width + x;
                m_pixels[idx] = (Random.value < 0.65f) ? glitchColor : ColorDark;
            }
        }

        m_glitchTexture.SetPixels32(m_pixels);
        m_glitchTexture.Apply();

        // 3. 순간적인 색상 플래시 노이즈
        if (Random.value < 0.2f)
        {
            m_material.color = (Random.value < 0.5f) ? new Color(1f, 0.3f, 0.3f) : new Color(0.3f, 0.85f, 1f);
        }
        else
        {
            m_material.color = Color.white;
        }
    }
}
