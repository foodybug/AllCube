using UnityEngine;
using System.Collections;

public class CubeBlink : MonoBehaviour
{
    [Header("Blink Fade Settings")]
    public float showDuration = 2.0f;
    public float hideDuration = 2.0f;
    public float fadeInDuration = 0.5f;
    public float fadeOutDuration = 0.5f;

    private Renderer m_renderer;
    private Collider m_collider;
    private bool m_isTriggered = false;
    private Color m_baseColor;

    void Awake()
    {
        m_renderer = GetComponent<Renderer>();
        m_collider = GetComponent<Collider>();
    }

    void Start()
    {
        if (m_renderer != null && m_collider != null)
        {
            m_collider.isTrigger = true;
            m_baseColor = m_renderer.material.color;

            // Standard Shader의 경우 투명(Fade) 렌더 모드로 런타임 설정
            SetupMaterialWithFadeMode(m_renderer.material);

            StartCoroutine(FadeSequence());
        }
    }

    private IEnumerator FadeSequence()
    {
        while (true)
        {
            // 1. Fade In (안전 -> 위험)
            m_collider.enabled = true; // 서서히 나타나기 시작할 때 충돌 켬
            m_isTriggered = false;
            float elapsed = 0f;
            while (elapsed < fadeInDuration)
            {
                elapsed += Time.deltaTime;
                float alpha = Mathf.Clamp01(elapsed / fadeInDuration);
                m_renderer.material.color = new Color(m_baseColor.r, m_baseColor.g, m_baseColor.b, alpha);
                yield return null;
            }
            m_renderer.material.color = new Color(m_baseColor.r, m_baseColor.g, m_baseColor.b, 1.0f);

            // 2. Show (완전 위험 상태 유지)
            yield return new WaitForSeconds(showDuration);

            // 3. Fade Out (위험 -> 안전)
            elapsed = 0f;
            while (elapsed < fadeOutDuration)
            {
                elapsed += Time.deltaTime;
                float alpha = Mathf.Clamp01(1.0f - (elapsed / fadeOutDuration));
                m_renderer.material.color = new Color(m_baseColor.r, m_baseColor.g, m_baseColor.b, alpha);

                // 투명도가 30% 이하로 내려가면 플레이어가 안전하게 건널 수 있도록 충돌 판정 조기 비활성화
                if (alpha < 0.3f)
                {
                    m_collider.enabled = false;
                }
                yield return null;
            }
            m_renderer.material.color = new Color(m_baseColor.r, m_baseColor.g, m_baseColor.b, 0.0f);
            m_collider.enabled = false;

            // 4. Hide (완전 안전 상태 유지)
            yield return new WaitForSeconds(hideDuration);
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        // 위험 상태일 때만 충돌 판정 (중복 감지 방지)
        if (!m_isTriggered && m_collider.enabled)
        {
            HandlePlayerCollision(other.gameObject);
        }
    }

    private void HandlePlayerCollision(GameObject otherGo)
    {
        Player player = otherGo.GetComponent<Player>();
        if (player != null || otherGo.name.Contains("Player") || otherGo.CompareTag("Player"))
        {
            if (player == null)
            {
                player = FindAnyObjectByType<Player>();
            }

            if (player != null)
            {
                m_isTriggered = true;
                Debug.Log($"[CubeBlink] Player hit Blink Obstacle! Resetting jump count from {player.JumpCount} to 0.");
                player.ResetJumpCount(0);

                if (AudioManager.Instance != null)
                {
                    AudioManager.Instance.Play("Sound/fail", 0.5f);
                }
            }
        }
    }

    private void SetupMaterialWithFadeMode(Material material)
    {
        // Standard Shader의 렌더 모드를 Fade(투명) 모드로 변경하는 연산
        material.SetFloat("_Mode", 2); // 2 is Fade mode
        material.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
        material.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
        material.SetInt("_ZWrite", 0);
        material.DisableKeyword("_ALPHATEST_ON");
        material.EnableKeyword("_ALPHABLEND_ON");
        material.DisableKeyword("_ALPHAPREMULTIPLY_ON");
        material.renderQueue = 3000;
    }
}
