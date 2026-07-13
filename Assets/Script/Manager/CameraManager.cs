using System.Collections;
using UnityEngine;

public class CameraManager : MonoBehaviour
{
    static CameraManager m_instance;
    public static CameraManager Instance { get { return m_instance; } }

    public Camera mainCamera;
    public Camera uiCamera;

    private Transform m_target;
    private Rigidbody m_targetRigidbody;
    public Transform Target { get { return m_target; } }
    public bool isFollowing = true;

    [Header("Camera Follow & Zoom Settings")]
    [SerializeField] private float m_fFollowSpeed = 1.0f;
    [SerializeField] private float m_fMinZoomSpeed = 15.0f;
    [SerializeField] private float m_fMaxZoomSpeed = 40.0f;
    [SerializeField] private float m_fOrthographicSize_Min = 12;
    [SerializeField] private float m_fOrthographicSize_Max = 20;
    [SerializeField] private float m_fZoomFactorMin = 0.1f;
    [SerializeField] private float m_fZoomFactorMax = 0.1f;

    [Header("Combo Zoom Settings")]
    [SerializeField] private float m_comboZoomMultiplier = 0.4f;
    [SerializeField] private float m_maxComboZoomOffset = 24.0f;

    [Header("Camera Axis Constraints")]
    [SerializeField] private bool m_enableXFollow = true;

    void Awake()
    {
        m_instance = this;
        m_enableXFollow = true;
    }

    void Start()
    {
    }

    void Update()
    {
    }

    void FixedUpdate()
    {
        if (mainCamera == null || m_target == null || m_targetRigidbody == null || !isFollowing)
            return;

        // update camera pos
        Vector3 vStart = mainCamera.transform.position;
        Vector3 targetPos = m_target.position;
        if (!m_enableXFollow)
        {
            targetPos.x = 0f;
        }
        targetPos.z = vStart.z;
        Vector3 vEnd = Vector3.MoveTowards(vStart, targetPos, m_fFollowSpeed);
        mainCamera.transform.position = vEnd;

        // update orthographic size
        float fSpeed = m_targetRigidbody.linearVelocity.magnitude;
        float scl = Mathf.Clamp01((fSpeed - m_fMinZoomSpeed) / (m_fMaxZoomSpeed - m_fMinZoomSpeed));

        // 콤보에 비례하여 카메라 시야(Orthographic Size) 확대 보너스 적용
        int combo = JumpIntervalTracker.Instance != null ? JumpIntervalTracker.Instance.ComboCount : 0;
        float comboZoomOffset = Mathf.Min(combo * m_comboZoomMultiplier, m_maxComboZoomOffset);
        float targetZoomFactor = Mathf.Lerp(m_fOrthographicSize_Min, m_fOrthographicSize_Max, scl) + comboZoomOffset;

        float fDelta = (targetZoomFactor - mainCamera.orthographicSize) > 0.0f ? m_fZoomFactorMax : m_fZoomFactorMin;
        mainCamera.orthographicSize = Mathf.MoveTowards(mainCamera.orthographicSize, targetZoomFactor, fDelta);// * Time.deltaTime);
    }

    public void Init()
    {
        if (mainCamera == null)
            return;

        mainCamera.transform.position = new Vector3(0.0f, 0.0f, -10.0f);
        mainCamera.orthographicSize = m_fOrthographicSize_Min;
        isFollowing = true;
    }

    public void SetTarget(GameObject go)
    {
        if (null != go)
        {
            m_target = go.transform;
            m_targetRigidbody = go.GetComponent<Rigidbody>();
        }
        else
        {
            m_target = null;
            m_targetRigidbody = null;
        }
    }
}
