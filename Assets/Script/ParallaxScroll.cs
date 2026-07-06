using UnityEngine;

public class ParallaxScroll : MonoBehaviour
{
    private Transform m_cameraTransform;
    private float m_parallaxFactorX; // 0 = follow camera 100% (stays on screen), 1 = stays fixed in world coordinate
    private float m_parallaxFactorY;
    private float m_scrollWidth;
    private float m_scrollHeight;
    private float m_originalX;
    private float m_originalY;

    private Vector3 m_cameraStartPos;

    /// <summary>
    /// Initializes the parallax scroll object parameters.
    /// </summary>
    /// <param name="cameraTransform">The transform of the tracking camera.</param>
    /// <param name="factorX">Parallax factor for X-axis (0.0f to 1.0f).</param>
    /// <param name="factorY">Parallax factor for Y-axis (0.0f to 1.0f).</param>
    /// <param name="scrollWidth">The width cycle to loop the position on the X axis.</param>
    /// <param name="scrollHeight">The height cycle to loop the position on the Y axis.</param>
    public void Init(Transform cameraTransform, float factorX, float factorY, float scrollWidth, float scrollHeight = 0f)
    {
        m_cameraTransform = cameraTransform;
        m_parallaxFactorX = factorX;
        m_parallaxFactorY = factorY;
        m_scrollWidth = scrollWidth;
        m_scrollHeight = scrollHeight;

        m_originalX = transform.position.x;
        m_originalY = transform.position.y;

        if (m_cameraTransform != null)
        {
            m_cameraStartPos = m_cameraTransform.position;
        }
    }

    /// <summary>
    /// Updates the position based on the camera displacement. Called centrally by MapManager or ResultMain.
    /// </summary>
    public void UpdateParallax()
    {
        if (m_cameraTransform == null) return;

        Vector3 cameraDiff = m_cameraTransform.position - m_cameraStartPos;

        // 실제 렌더링 중인 메인 카메라가 존재하고, 추적 대상 가상 앵커와 다를 경우 (렌더링 카메라는 고정된 연출 상황)
        Camera mainCam = Camera.main;
        bool isStaticCamera = (mainCam != null && mainCam.transform != m_cameraTransform);

        // 렌더링 카메라가 고정되어 있다면 가상 카메라의 상승/이동 방향의 역방향으로 흐르도록 부호 반전
        float multiplierX = isStaticCamera ? -(1f - m_parallaxFactorX) : (1f - m_parallaxFactorX);
        float multiplierY = isStaticCamera ? -(1f - m_parallaxFactorY) : (1f - m_parallaxFactorY);

        float targetX = m_originalX + cameraDiff.x * multiplierX;
        float targetY = m_originalY + cameraDiff.y * multiplierY;

        // 랩핑의 절대 기준점은 실제 렌더링 뷰포트 카메라인 메인 카메라의 좌표로 통일
        float cameraX = isStaticCamera ? mainCam.transform.position.x : m_cameraTransform.position.x;
        float cameraY = isStaticCamera ? mainCam.transform.position.y : m_cameraTransform.position.y;

        Vector3 pos = transform.position;
        pos.z = transform.position.z; // Z축 고정 보장

        // [X축 상대적 뷰포트 루프 공식 적용]
        if (m_scrollWidth > 0f)
        {
            float localX = targetX - cameraX;
            float halfW = m_scrollWidth * 0.5f;
            // 카메라 중심 기준 [-halfW, halfW] 범위로 랩핑(Wrap)하여 절대 화면 밖 탈출을 방지
            float loopedLocalX = Mathf.Repeat(localX + halfW, m_scrollWidth) - halfW;
            pos.x = cameraX + loopedLocalX;
        }
        else
        {
            pos.x = targetX;
        }

        // [Y축 카메라 Frustum(절두체) 랩핑 순환 적용]
        if (mainCam != null)
        {
            pos.y = targetY;
            transform.position = pos;

            // 큐브 크기(Scale)의 절반 반경 연산
            float halfScale = transform.localScale.y * 0.5f;

            // 큐브의 맨 위쪽 끝단 월드 Y 좌표
            float topWorldY = pos.y + halfScale;

            // 맨 위쪽 끝단의 뷰포트 좌표 (가장 윗면마저 화면 아래 밖으로 탈출했는지 판단용)
            Vector3 topViewport = mainCam.WorldToViewportPoint(new Vector3(pos.x, topWorldY, pos.z));

            // 큐브의 맨 위쪽 끝부분마저 화면 하단 안전선(-0.25f) 밑으로 완전히 빠져나갔을 때 리스폰
            if (topViewport.y < -0.25f)
            {
                // 화면 상단 안전지대(1.25f)의 월드 Y 좌표 역산
                Vector3 targetViewport = new Vector3(topViewport.x, 1.25f, topViewport.z);
                Vector3 targetWorld = mainCam.ViewportToWorldPoint(targetViewport);

                // 큐브의 맨 아랫부분(꼬리)이 targetWorld.y에 위치하도록 새로운 중심 Y 계산
                float newCenterY = targetWorld.y + halfScale;

                // 원래 중심 Y(pos.y)에서 새로운 중심 Y(newCenterY)까지의 높이차 산출
                float worldHeightDiff = newCenterY - pos.y;

                // 기준 Y 좌표와 실제 오브젝트 월드 Y 좌표를 높이차만큼 끌어올려 자연 순환 보장
                m_originalY += worldHeightDiff;
                pos.y += worldHeightDiff;
                transform.position = pos;
            }
        }
        else
        {
            pos.y = targetY;
            transform.position = pos;
        }
    }
}
