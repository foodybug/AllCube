using UnityEngine;

public class InfiniteScrollObject : MonoBehaviour
{
    private float m_originalX;
    private float m_scrollWidth;
    private Transform m_playerTransform;

    public void Init(float originalX, float scrollWidth, Transform playerTransform)
    {
        m_originalX = originalX;
        m_scrollWidth = scrollWidth;
        m_playerTransform = playerTransform;

        // 스폰 즉시 현재 플레이어 X 좌표 사이클에 맞추어 위치를 텔레포트 동기화 (1프레임 딜레이 방지)
        if (m_playerTransform != null && m_scrollWidth > 0f)
        {
            float playerX = m_playerTransform.position.x;
            float cycle = Mathf.Round(playerX / m_scrollWidth);
            float targetX = m_originalX + cycle * m_scrollWidth;

            Vector3 pos = transform.position;
            pos.x = targetX;
            transform.position = pos;

            Rigidbody rb = GetComponent<Rigidbody>();
            if (rb != null)
            {
                rb.position = pos;
            }
        }
    }

    private void LateUpdate()
    {
        if (m_playerTransform == null)
        {
            if (CameraManager.Instance != null && CameraManager.Instance.Target != null)
            {
                m_playerTransform = CameraManager.Instance.Target;
            }
            else
            {
                return;
            }
        }

        if (m_scrollWidth <= 0f) return;

        float playerX = m_playerTransform.position.x;
        // 개별 거리가 아닌 플레이어의 절대 전역 X 위치를 스크롤 너비 기준으로 사이클 연산
        float cycle = Mathf.Round(playerX / m_scrollWidth);
        float targetX = m_originalX + cycle * m_scrollWidth;

        Vector3 pos = transform.position;
        if (Mathf.Abs(pos.x - targetX) > 0.01f)
        {
            pos.x = targetX;
            transform.position = pos;

            Rigidbody rb = GetComponent<Rigidbody>();
            if (rb != null)
            {
                rb.position = pos;
            }
        }
    }
}
