using UnityEngine;

public class InfiniteScrollObject : MonoBehaviour
{
    private float m_originalX;
    private float m_scrollWidth;
    private Transform m_playerTransform;
    private Rigidbody m_rb;

    public void Init(float originalX, float scrollWidth, Transform playerTransform)
    {
        m_originalX = originalX;
        m_scrollWidth = scrollWidth;
        m_playerTransform = playerTransform;
        m_rb = GetComponent<Rigidbody>();

        // 스폰 즉시 현재 플레이어 X 좌표 사이클에 맞추어 위치를 텔레포트 동기화 (1프레임 딜레이 방지)
        if (m_playerTransform != null && m_scrollWidth > 0f)
        {
            float playerX = m_playerTransform.position.x;
            float cycle = Mathf.Round(playerX / m_scrollWidth);
            float targetX = m_originalX + cycle * m_scrollWidth;

            Vector3 pos = transform.position;
            pos.x = targetX;
            transform.position = pos;

            if (m_rb != null)
            {
                m_rb.position = pos;
            }
        }
    }

    // MapManager에서 일괄적으로 호출되어 Update 오버헤드를 방지하는 매니저 업데이트 메소드
    public void UpdateScroll(float playerX)
    {
        if (m_scrollWidth <= 0f) return;

        // 플레이어의 절대 전역 X 위치를 스크롤 너비 기준으로 사이클 연산
        float cycle = Mathf.Round(playerX / m_scrollWidth);
        float targetX = m_originalX + cycle * m_scrollWidth;

        Vector3 pos = transform.position;
        if (Mathf.Abs(pos.x - targetX) > 0.01f)
        {
            pos.x = targetX;
            transform.position = pos;

            if (m_rb != null)
            {
                m_rb.position = pos;
            }
        }
    }
}
