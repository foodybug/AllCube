using UnityEngine;

/// <summary>
/// 느린 속도(chaseSpeed)로 플레이어(Player)의 위치를 2D 공간상에서 은밀히 추적해오는 장애물 블럭
/// </summary>
public class CubeHomingObstacle : MonoBehaviour
{
    [Header("Homing Chase Settings")]
    [SerializeField]
    private float m_chaseSpeed = 0.5f; // 초반 최소 느린 추적 속도 (0.5f)

    public void SetChaseSpeed(float speed)
    {
        if (speed > 0f)
        {
            m_chaseSpeed = speed;
        }
    }

    [SerializeField]
    private float m_maxDetectDistance = 35.0f; // 추적 감지 최대 거리

    private Transform m_playerTarget;
    private Renderer m_renderer;
    private Color m_baseColor = new Color(0.9f, 0.15f, 0.15f, 1.0f); // 경고용 진한 레드
    private bool m_isTriggered = false;

    private void Awake()
    {
        m_renderer = GetComponent<Renderer>();

        Rigidbody rb = GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.isKinematic = true; // 물리 밀림 방지
        }

        Collider col = GetComponent<Collider>();
        if (col != null)
        {
            col.isTrigger = true; // 통과형 위험 장애물 충돌 처리
        }
    }

    private void Start()
    {
        FindPlayerTarget();
        InitVisuals();
    }

    private void InitVisuals()
    {
        if (m_renderer != null && MapManager.Instance != null)
        {
            Material sharedMat = MapManager.Instance.GetSharedMaterial(8);
            if (sharedMat != null)
            {
                m_renderer.sharedMaterial = sharedMat;
            }

            Texture[] texCube = MapManager.Instance.texCube;
            if (texCube != null && texCube.Length > 0)
            {
                int randIdx = Random.Range(4, Mathf.Min(8, texCube.Length));
                if (randIdx < texCube.Length && texCube[randIdx] != null)
                {
                    Material homingMat = new Material(sharedMat != null ? sharedMat : m_renderer.material);
                    homingMat.mainTexture = texCube[randIdx];
                    m_renderer.material = homingMat;
                }
            }
        }
    }

    private MaterialPropertyBlock m_propBlock;

    private void FindPlayerTarget()
    {
        if (m_playerTarget == null)
        {
            if (Player.Instance != null)
            {
                m_playerTarget = Player.Instance.transform;
            }
            else if (CameraManager.Instance != null && CameraManager.Instance.Target != null)
            {
                m_playerTarget = CameraManager.Instance.Target;
            }
        }
    }

    private void Update()
    {
        FindPlayerTarget();

        if (m_playerTarget == null) return;

        // 플레이어와의 2D 거리 계산
        Vector3 currentPos = transform.position;
        Vector3 targetPos = m_playerTarget.position;
        targetPos.z = currentPos.z; // Z축 고정

        float distance = Vector3.Distance(currentPos, targetPos);

        // 플레이어가 하단으로 멀리 지나친 경우(30m 이하)만 자동 소거 (상단 사전 생성된 블록 파괴 방지)
        if (currentPos.y < targetPos.y - 30.0f)
        {
            Util.MyDestroy(gameObject);
            return;
        }

        // 감지 범위 내에 플레이어가 있을 경우 X축 및 Y축 독립 추적 (점프 시 X추적 마비 현상 완전 방지)
        if (distance <= m_maxDetectDistance && distance > 0.05f)
        {
            float newX = Mathf.MoveTowards(currentPos.x, targetPos.x, m_chaseSpeed * Time.deltaTime);
            float newY = Mathf.MoveTowards(currentPos.y, targetPos.y, (m_chaseSpeed * 0.75f) * Time.deltaTime);
            transform.position = new Vector3(newX, newY, currentPos.z);

            // MaterialPropertyBlock을 사용한 GPU Instancing 유지 펄스 연출
            if (m_renderer != null)
            {
                if (m_propBlock == null) m_propBlock = new MaterialPropertyBlock();
                float pulse = 0.7f + 0.3f * Mathf.PingPong(Time.time * 3.0f, 1.0f);
                Color pulseColor = new Color(m_baseColor.r * pulse, m_baseColor.g * pulse, m_baseColor.b * pulse, 1.0f);
                m_renderer.GetPropertyBlock(m_propBlock);
                m_propBlock.SetColor("_Color", pulseColor);
                m_renderer.SetPropertyBlock(m_propBlock);
            }
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (m_isTriggered) return;

        Player player = other.GetComponent<Player>();
        if (player == null)
        {
            player = other.GetComponentInParent<Player>();
        }

        if (player != null || other.CompareTag("Player") || other.name.Contains("Player"))
        {
            if (player == null)
            {
                player = FindFirstObjectByType<Player>();
            }

            if (player != null)
            {
                m_isTriggered = true;
                Debug.Log($"[CubeHomingObstacle] Hit player! Killing player...");
                player.ResetJumpCount(0);
                player.KillPlayer();
            }
        }
    }
}
