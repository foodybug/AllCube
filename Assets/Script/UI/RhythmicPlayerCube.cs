using UnityEngine;

public class RhythmicPlayerCube : MonoBehaviour
{
    private Transform m_cameraTransform;
    private Camera m_camera;
    private Rigidbody m_rb;
    private Renderer m_renderer;
    private Texture m_texOn;
    private Texture m_texOff;

    private float m_amountX;
    private float m_amountY;
    private float m_torque;

    private float m_jumpInterval;
    private float m_jumpTimer;
    private float m_jumpDir = 1f;

    // 텍스처 상태 제어 변수 (도약 직후 눈을 깜빡이게 유도)
    private float m_forceWaitTimer = 0f;

    public void Init(Transform cameraTransform, float startX, float startY)
    {
        m_cameraTransform = cameraTransform;
        m_camera = Camera.main;
        if (m_camera == null)
        {
            m_camera = FindFirstObjectByType<Camera>();
        }

        m_renderer = GetComponent<Renderer>();

        // 큐브 간 물리 충돌로 인한 덜덜 떨림 현상 완전 방지 (Collider 제거)
        Collider col = GetComponent<Collider>();
        if (col != null)
        {
            Destroy(col);
        }

        // 실제 인게임 플레이어 전용 On/Off 텍스처 리소스 로드
        m_texOn = Resources.Load("Player/texPlayerOn") as Texture;
        m_texOff = Resources.Load("Player/texPlayerOff") as Texture;

        // Rigidbody를 동적으로 부착하고 Constraints를 인게임과 완전 동일하게 구성
        m_rb = gameObject.GetComponent<Rigidbody>();
        if (m_rb == null)
        {
            m_rb = gameObject.AddComponent<Rigidbody>();
        }
        m_rb.useGravity = true;
        m_rb.isKinematic = false;
        m_rb.mass = 1.0f;
        m_rb.constraints = RigidbodyConstraints.FreezePositionZ | RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationY;
        m_rb.collisionDetectionMode = CollisionDetectionMode.Continuous;
        m_rb.interpolation = RigidbodyInterpolation.Interpolate; // 물리 렌더링 저더(떨림) 현상 완전 제거
        m_rb.linearDamping = 0.05f; // 자연스러운 가로 감속 유도
        m_rb.angularDamping = 0.05f; // 부드러운 회전 감속 유도

        m_jumpDir = Random.value > 0.5f ? 1f : -1f;

        ResetParams(startX, startY, true); // 최초 스폰 플래그 true
    }

    private void ResetParams(float startX, float startY, bool isInitialSpawn)
    {
        if (m_rb != null)
        {
            m_rb.isKinematic = false; // velocity 설정 전 kinematic 해제 (경고 방지)
            m_rb.linearVelocity = Vector3.zero;
            m_rb.angularVelocity = Vector3.zero;
            m_rb.isKinematic = true;  // 재배치 대기 중 PhysX 충돌 떨림 방지
            m_rb.useGravity = true;
        }

        transform.position = new Vector3(startX, startY, transform.position.z);
        transform.rotation = Quaternion.identity;

        // 임펄스(Impulse) 물리 스케일에 맞춘 보정 난수
        m_amountY = Random.Range(3.6f, 4.8f); 
        m_amountX = Random.Range(1.5f, 2.5f);
        m_torque = Random.Range(1.8f, 2.8f);

        // 점프 시간 간격 (1.0초 ~ 1.4초마다 점프 입력 강제 가동)
        m_jumpInterval = Random.Range(1.0f, 1.4f);
        
        if (isInitialSpawn)
        {
            m_jumpTimer = Random.Range(0f, m_jumpInterval * 0.8f); // 최초에는 부드러운 시간차 대기
        }
        else
        {
            m_jumpTimer = m_jumpInterval; // 화면 밖 리스폰 복귀 시에는 즉각적 점프 격발!
        }
    }

    void Update()
    {
        if (m_camera == null)
        {
            m_camera = Camera.main;
            if (m_camera == null) m_camera = FindFirstObjectByType<Camera>();
        }

        // 1. 실제 플레이어와 동일한 On/Off 텍스처 스와핑 제어
        m_forceWaitTimer -= Time.deltaTime;
        if (m_renderer != null)
        {
            Texture targetTex = (m_forceWaitTimer > 0f) ? m_texOff : m_texOn;
            if (m_renderer.sharedMaterial.mainTexture != targetTex && targetTex != null)
            {
                m_renderer.sharedMaterial.mainTexture = targetTex;
            }
        }

        // 2. 타이머 기반 리드미컬 물리 점프 격발
        m_jumpTimer += Time.deltaTime;
        if (m_jumpTimer >= m_jumpInterval)
        {
            m_jumpTimer = 0f;
            ExecuteJump();
        }

        // 3. 메인 카메라의 3D 월드 절두체(Frustum) 및 높이 기준 외곽 이탈 감지 및 무한 순환 리스폰
        Camera mainCam = CameraManager.GetMainCamera();
        if (mainCam != null)
        {
            float halfHeight = mainCam.orthographicSize;
            float camCenterY = mainCam.transform.position.y;
            float camCenterX = mainCam.transform.position.x;

            // 카메라 상단/하단 절두체 월드 경계
            float camTopWorldY = camCenterY + halfHeight;
            float camBottomWorldY = camCenterY - halfHeight;

            // 절두체 위/아래 공간 한계 (상단 +15m, 하단 -10m)
            float upperLimitY = camTopWorldY + 15.0f;
            float lowerLimitY = camBottomWorldY - 10.0f;

            Vector3 pos = transform.position;

            // 화면 절두체 상단 한계 위로 솟구쳤거나, 하단 한계 아래로 완전 떨어진 경우 리스폰
            if (pos.y > upperLimitY || pos.y < lowerLimitY || Mathf.Abs(pos.x - camCenterX) > 25.0f)
            {
                // [중앙 로고 영역 가림 방지 - 좌우 날개 가로 위치 선택]
                bool spawnOnLeft = (Random.value > 0.5f);
                float spawnX = spawnOnLeft
                    ? camCenterX - Random.Range(3.8f, 6.5f)
                    : camCenterX + Random.Range(3.8f, 6.5f);

                // 카메라 절두체 하단 경계(camBottomWorldY) 바로 아래(-3.5m ~ -5.5m)에서 자연스럽게 도약 리스폰!
                float spawnY = camBottomWorldY - Random.Range(3.5f, 5.5f);
                ResetParams(spawnX, spawnY, false);
            }
        }
    }

    private void ExecuteJump()
    {
        if (m_rb == null) return;
        m_rb.isKinematic = false; // 도약 격발 시 Kinematic 해제하여 물리 운동 가동!
        m_rb.useGravity = true;

        m_rb.linearVelocity = Vector3.zero;
        m_rb.angularVelocity = Vector3.zero;

        float angleOffset = Random.Range(10.0f, 22.0f) * m_jumpDir;
        Vector3 jumpDir = Quaternion.Euler(0f, 0f, angleOffset) * Vector3.up;

        // 카메라 절두체 내부로 크게 솟구쳐 오르는 강력한 점프 임펄스 힘 (14.0f ~ 18.0f)
        float jumpForceMagnitude = Random.Range(14.0f, 18.0f);
        float torqueVal = Random.Range(12.0f, 20.0f);

        m_rb.AddForce(jumpDir * jumpForceMagnitude, ForceMode.Impulse);

        float torqueDirection = jumpDir.x > 0f ? -1f : 1f;
        m_rb.AddTorque(new Vector3(0f, 0f, torqueDirection * torqueVal), ForceMode.Impulse);

        m_forceWaitTimer = 0.05f;
        m_jumpDir *= -1f;
    }
}
