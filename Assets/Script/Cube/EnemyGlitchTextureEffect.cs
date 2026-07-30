using System.Collections;
using UnityEngine;

/// <summary>
/// 적/장애물 블록 전용 1프레임 글리치 텍스처 효과 컴포넌트 (중앙 집중형 디커플링 구조)
/// 초당 기본 5% 확률로 바닥의 글리치 텍스처로 1프레임만 바뀝니다.
/// Combo가 높아질수록 초당 확률이 상향되어 최대 20%까지 증가합니다. (0-GC MaterialPropertyBlock 구현)
/// </summary>
public class EnemyGlitchTextureEffect : MonoBehaviour
{
    private Renderer m_renderer;
    private MaterialPropertyBlock m_mpb;
    private Texture m_originalTexture;
    private static readonly int s_mainTexPropId = Shader.PropertyToID("_MainTex");

    private bool m_isGlitching = false;

    public static EnemyGlitchTextureEffect AttachTo(GameObject go)
    {
        if (go == null) return null;
        EnemyGlitchTextureEffect effect = go.GetComponent<EnemyGlitchTextureEffect>();
        if (effect == null)
        {
            effect = go.AddComponent<EnemyGlitchTextureEffect>();
        }
        return effect;
    }

    void Awake()
    {
        m_renderer = GetComponent<Renderer>();
        if (m_renderer == null) m_renderer = GetComponentInChildren<Renderer>();

        m_mpb = new MaterialPropertyBlock();
    }

    void Start()
    {
        if (m_renderer != null)
        {
            if (m_renderer.sharedMaterial != null)
            {
                m_originalTexture = m_renderer.sharedMaterial.mainTexture;
            }
            if (m_renderer.material != null && m_renderer.material.mainTexture != null)
            {
                m_originalTexture = m_renderer.material.mainTexture;
            }
        }
    }

    void Update()
    {
        if (m_renderer == null || m_isGlitching) return;

        // Combo 상승에 따른 초당 글리치 발동 확률 계산 (기본 5% -> 최대 20%)
        int combo = (JumpIntervalTracker.Instance != null) ? JumpIntervalTracker.Instance.ComboCount : 0;
        float chancePerSecond = Mathf.Clamp(0.05f + (combo * 0.015f), 0.05f, 0.20f);

        // 프레임당 난수 확률 검사
        float frameChance = chancePerSecond * Time.deltaTime;
        if (Random.value < frameChance)
        {
            StartCoroutine(CoTrigger1FrameGlitch());
        }
    }

    private IEnumerator CoTrigger1FrameGlitch()
    {
        m_isGlitching = true;

        Texture glitchTex = GlitchDeadZoneEffect.SharedGlitchTexture;
        if (glitchTex != null && m_renderer != null)
        {
            // 1프레임 바닥 글리치 텍스처로 변경 (0-GC MaterialPropertyBlock)
            m_renderer.GetPropertyBlock(m_mpb);
            m_mpb.SetTexture(s_mainTexPropId, glitchTex);
            m_renderer.SetPropertyBlock(m_mpb);

            // 정확히 1프레임 대기
            yield return null;

            // 원본 텍스처로 즉시 복원
            m_renderer.GetPropertyBlock(m_mpb);
            if (m_originalTexture != null)
            {
                m_mpb.SetTexture(s_mainTexPropId, m_originalTexture);
            }
            else
            {
                m_mpb.Clear();
            }
            m_renderer.SetPropertyBlock(m_mpb);
        }

        m_isGlitching = false;
    }
}
