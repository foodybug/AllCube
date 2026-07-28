using UnityEngine;

public class Coin : MonoBehaviour
{
    private Renderer m_renderer;
    private Transform m_playerTransform;

    // 보석 등급 변수 추가 (1~5)
    public int grade = 1;

    void Awake()
    {
        m_renderer = GetComponentInChildren<Renderer>();
    }

    void Start()
    {
        ApplyRandomTexture();
        ApplyGradeScale();
    }

    void Update()
    {
        if (m_playerTransform == null && Player.Instance != null)
        {
            m_playerTransform = Player.Instance.transform;
        }

        if (m_playerTransform != null)
        {
            transform.rotation = m_playerTransform.rotation;
        }
    }

    public void ApplyGradeScale()
    {
        // grade 1 일 때 기본 크기 0.5f 에서 시작하여 등급 당 0.2f 씩 증가 (최대 1.3f)
        float targetScale = 0.5f + (grade - 1) * 0.2f;
        transform.localScale = new Vector3(targetScale, targetScale, targetScale);
    }

    public void ApplyRandomTexture()
    {
        if (m_renderer == null)
        {
            m_renderer = GetComponentInChildren<Renderer>();
        }

        if (m_renderer != null && MapManager.Instance != null)
        {
            Texture[] texCube = MapManager.Instance.texCube;
            if (texCube != null && texCube.Length > 0)
            {
                int maxIdx = Mathf.Min(3, texCube.Length - 1);
                if (maxIdx >= 0)
                {
                    int randIdx = Random.Range(0, maxIdx + 1);
                    Texture targetTex = texCube[randIdx];

                    if (targetTex != null)
                    {
                        m_renderer.material.mainTexture = targetTex;
                    }
                }
            }
        }
    }
}
