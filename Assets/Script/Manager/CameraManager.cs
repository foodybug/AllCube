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

    private int m_lastScreenWidth = 0;
    private int m_lastScreenHeight = 0;

    void Start()
    {
        EnforceAspectRatio();
    }

    void Update()
    {
        // 모바일 기기별 해상도 변경 감지 시 종횡비 자동 보정
        if (Screen.width != m_lastScreenWidth || Screen.height != m_lastScreenHeight)
        {
            m_lastScreenWidth = Screen.width;
            m_lastScreenHeight = Screen.height;
            EnforceAspectRatio();
        }
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

    private static float s_shakeTimer = 0f;
    private static float s_shakeIntensity = 0f;

    public static void ShakeMainCamera(float duration, float intensity)
    {
        s_shakeTimer = Mathf.Max(s_shakeTimer, duration);
        s_shakeIntensity = Mathf.Max(s_shakeIntensity, intensity);
    }

    void LateUpdate()
    {
        if (mainCamera == null) return;

        if (s_shakeTimer > 0f)
        {
            s_shakeTimer -= Time.deltaTime;
            Vector2 randomShake = Random.insideUnitCircle * s_shakeIntensity;
            mainCamera.transform.position += new Vector3(randomShake.x, randomShake.y, 0f);
        }
    }

    public void Init()
    {
        if (mainCamera == null)
            return;

        mainCamera.transform.position = new Vector3(0.0f, 0.0f, -10.0f);
        mainCamera.orthographicSize = m_fOrthographicSize_Min;
        isFollowing = true;

        EnforceAspectRatio();
    }

    public void EnforceAspectRatio()
    {
        ApplyAspect(mainCamera);
    }

    private static Camera s_backgroundClearCamera = null;

    private static Camera s_cachedMainCam = null;

    public static Camera GetMainCamera()
    {
        EnsureBackgroundClearCamera();

        if (s_cachedMainCam != null && s_cachedMainCam.gameObject.activeInHierarchy && s_cachedMainCam.name != "BackgroundClearCamera")
        {
            return s_cachedMainCam;
        }

        Camera cam = Camera.main;
        if (cam != null && cam.name != "BackgroundClearCamera")
        {
            s_cachedMainCam = cam;
            return s_cachedMainCam;
        }

        Camera[] allCams = Object.FindObjectsByType<Camera>(FindObjectsSortMode.None);
        if (allCams != null)
        {
            foreach (var c in allCams)
            {
                if (c != null && c.name != "BackgroundClearCamera")
                {
                    s_cachedMainCam = c;
                    return s_cachedMainCam;
                }
            }
        }
        return null;
    }

    public static void EnsureBackgroundClearCamera()
    {
        if (s_backgroundClearCamera == null)
        {
            GameObject bgCamGo = GameObject.Find("BackgroundClearCamera");
            if (bgCamGo == null)
            {
                bgCamGo = new GameObject("BackgroundClearCamera");
                Object.DontDestroyOnLoad(bgCamGo);
                s_backgroundClearCamera = bgCamGo.AddComponent<Camera>();
            }
            else
            {
                s_backgroundClearCamera = bgCamGo.GetComponent<Camera>();
            }

            bgCamGo.tag = "Untagged";
            s_backgroundClearCamera.clearFlags = CameraClearFlags.SolidColor;
            s_backgroundClearCamera.backgroundColor = Color.black;
            s_backgroundClearCamera.cullingMask = 0; // 3D 레벨 객체 그리지 않고 순수 레터박스 포함 전화면 지우기만 수행
            s_backgroundClearCamera.depth = -100; // 최하단에서 전 화면(Letterbox 포함) 매 프레임 클리어
            s_backgroundClearCamera.rect = new Rect(0, 0, 1, 1);
            s_backgroundClearCamera.allowHDR = false;
            s_backgroundClearCamera.allowMSAA = false;
        }
        else
        {
            s_backgroundClearCamera.cullingMask = 0; // 혹시 타 스크립트에서 cullingMask가 변경되었더라도 0으로 강제 복구
        }
    }

    public static void ApplyAspect(Camera cam)
    {
        if (cam == null) return;
        EnsureBackgroundClearCamera();

        float targetAspect = 9.0f / 16.0f; // 720x1280 고정 세로(Portrait) 9:16 종횡비
        float currentAspect = (float)Screen.width / Screen.height;
        float scaleHeight = currentAspect / targetAspect;

        if (scaleHeight < 1.0f)
        {
            Rect rect = cam.rect;
            rect.width = 1.0f;
            rect.height = scaleHeight;
            rect.x = 0;
            rect.y = (1.0f - scaleHeight) / 2.0f;
            cam.rect = rect;
        }
        else
        {
            float scaleWidth = 1.0f / scaleHeight;
            Rect rect = cam.rect;
            rect.width = scaleWidth;
            rect.height = 1.0f;
            rect.x = (1.0f - scaleWidth) / 2.0f;
            rect.y = 0;
            cam.rect = rect;
        }
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
