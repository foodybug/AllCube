using UnityEngine;

/// <summary>
/// 무작위 방향으로 아주 빠른 속도로 움직이며 플레이어에게 위협을 가하는 광란의 블럭 장애물 (CubeCrazy)
/// </summary>
public class CubeCrazy : MonoBehaviour
{
    [SerializeField] private float minSpeed = 10.0f;
    [SerializeField] private float maxSpeed = 18.0f;
    
    private float m_currentSpeed;
    private Vector3 m_moveDirection;
    private float m_directionChangeTimer;

    private float m_boundMinX = -16.0f;
    private float m_boundMaxX = 16.0f;

    void Awake()
    {
        Collider col = GetComponent<Collider>();
        if (col != null) col.isTrigger = true;
        Rigidbody rb = GetComponent<Rigidbody>();
        if (rb != null) Destroy(rb);
    }

    void Start()
    {
        Renderer rend = GetComponent<Renderer>();
        if (rend != null && MapManager.Instance != null)
        {
            // 7번 시안/광란 적대 장애물 머티리얼 적용
            rend.sharedMaterial = MapManager.Instance.GetSharedMaterial(7);
        }

        // 글리치 질감 이펙트 부착
        EnemyGlitchTextureEffect.AttachTo(gameObject);

        m_currentSpeed = Random.Range(minSpeed, maxSpeed);
        PickNewRandomDirection();
    }

    private void PickNewRandomDirection()
    {
        // 360도 무작위 2D 방향 벡터 산출
        float angle = Random.Range(0f, 360f);
        m_moveDirection = new Vector3(Mathf.Cos(angle * Mathf.Deg2Rad), Mathf.Sin(angle * Mathf.Deg2Rad), 0f).normalized;
        m_directionChangeTimer = Random.Range(0.35f, 0.75f);
    }

    void Update()
    {
        if (MainManager.Instance != null && MainManager.Instance.eCurState != eGameState.eGameState_Play)
            return;

        // 무작위 시간 주기마다 갑작스럽게 이동 방향 꺾임
        m_directionChangeTimer -= Time.deltaTime;
        if (m_directionChangeTimer <= 0f)
        {
            PickNewRandomDirection();
        }

        // 무작위 방향으로 빠른 이동
        transform.Translate(m_moveDirection * m_currentSpeed * Time.deltaTime, Space.World);

        // 플레이어 중심 가로 경계 이탈 방지 반사 처리
        Transform playerT = CameraManager.Instance != null && CameraManager.Instance.Target != null ? CameraManager.Instance.Target : null;
        float centerX = playerT != null ? playerT.position.x : 0f;
        float currentMinX = centerX + m_boundMinX;
        float currentMaxX = centerX + m_boundMaxX;

        Vector3 pos = transform.position;
        if (pos.x < currentMinX)
        {
            pos.x = currentMinX;
            m_moveDirection.x = Mathf.Abs(m_moveDirection.x);
            transform.position = pos;
        }
        else if (pos.x > currentMaxX)
        {
            pos.x = currentMaxX;
            m_moveDirection.x = -Mathf.Abs(m_moveDirection.x);
            transform.position = pos;
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        HandlePlayerCollision(other.gameObject);
    }

    private void OnCollisionEnter(Collision collision)
    {
        HandlePlayerCollision(collision.gameObject);
    }

    private void HandlePlayerCollision(GameObject otherGo)
    {
        Player player = otherGo.GetComponent<Player>();
        if (player != null || otherGo.name.Contains("Player") || otherGo.CompareTag("Player"))
        {
            if (player == null)
            {
                player = Player.Instance;
            }

            if (player != null)
            {
                Debug.Log("[CubeCrazy] Player hit Crazy Block! Killing player.");
                MainManager.lastDeathCause = "CrazyObstacle";
                player.KillPlayer();
            }
        }
    }
}
