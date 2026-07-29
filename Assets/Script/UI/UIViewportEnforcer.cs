using UnityEngine;

/// <summary>
/// UI Container의 RectTransform anchorMin / anchorMax를 메인 카메라의 9:16 뷰포트(cam.rect)와 100% 직동 매핑하여
/// 상/하단 레터박스 및 좌/우 필러박스 검은 여백으로 UI가 이탈하는 현상을 완벽히 방지하는 스크립트
/// </summary>
public class UIViewportEnforcer : MonoBehaviour
{
    private RectTransform m_rectTransform;
    private Rect m_lastCamRect = new Rect(-1, -1, -1, -1);

    void Awake()
    {
        m_rectTransform = GetComponent<RectTransform>();
    }

    void Start()
    {
        UpdateViewportBounds();
    }

    void Update()
    {
        UpdateViewportBounds();
    }

    public void UpdateViewportBounds()
    {
        if (m_rectTransform == null) m_rectTransform = GetComponent<RectTransform>();
        if (m_rectTransform == null) return;

        Camera cam = Camera.main;
        if (cam == null) cam = FindFirstObjectByType<Camera>();
        if (cam == null) return;

        Rect camRect = cam.rect;
        if (camRect != m_lastCamRect)
        {
            m_lastCamRect = camRect;
            m_rectTransform.anchorMin = new Vector2(camRect.x, camRect.y);
            m_rectTransform.anchorMax = new Vector2(camRect.x + camRect.width, camRect.y + camRect.height);
            m_rectTransform.offsetMin = Vector2.zero;
            m_rectTransform.offsetMax = Vector2.zero;
        }
    }
}
