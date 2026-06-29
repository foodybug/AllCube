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
    /// Updates the position based on the camera displacement. Called centrally by MapManager.
    /// </summary>
    public void UpdateParallax()
    {
        if (m_cameraTransform == null) return;

        Vector3 cameraDiff = m_cameraTransform.position - m_cameraStartPos;

        // Apply parallax offset relative to initial coordinate
        float targetX = m_originalX + cameraDiff.x * (1f - m_parallaxFactorX);
        float targetY = m_originalY + cameraDiff.y * (1f - m_parallaxFactorY);

        Vector3 pos = transform.position;
        pos.x = targetX;
        pos.y = targetY;
        transform.position = pos;

        // Apply looping behavior on X axis if scrollWidth is valid
        if (m_scrollWidth > 0f)
        {
            float cameraX = m_cameraTransform.position.x;
            float cycle = Mathf.Round(cameraX / m_scrollWidth);
            float targetLoopedX = targetX + cycle * m_scrollWidth;

            pos.x = targetLoopedX;
            transform.position = pos;
        }

        // Apply looping behavior on Y axis if scrollHeight is valid
        if (m_scrollHeight > 0f)
        {
            float cameraY = m_cameraTransform.position.y;
            float cycleY = Mathf.Round(cameraY / m_scrollHeight);
            float targetLoopedY = targetY + cycleY * m_scrollHeight;

            pos.y = targetLoopedY;
            transform.position = pos;
        }
    }
}
