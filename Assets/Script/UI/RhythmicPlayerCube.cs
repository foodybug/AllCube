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

        // 실제 인게임 플레이어 전용 On/Off 텍스처 리소스 로드
        m_texOn = Resources.Load("Player/texPlayerOn") as Texture;
        m_texOff = Resources.Load("Player/texPlayerOff") as Texture;

        // Rigidbody를 동적으로 부착하고 Constraints를 인게임과 완전 동일하게 구성
        m_rb = gameObject.GetComponent<Rigidbody>();
        if (m_rb == null)
        {
            m_rb = gameObject.AddComponent<Rigidbody>();
        }
        m_rb.useGravity = false; // 첫 도약 전까지 중력 OFF
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
        // 도약 개시 전 Y축 속도를 가상 카메라 스크롤 속도(3.6f)로 동기화하여 관성 부여
        if (m_rb != null)
        {
            m_rb.linearVelocity = new Vector3(0f, 3.6f, 0f);
            m_rb.angularVelocity = Vector3.zero;
            m_rb.useGravity = false; // 리스폰 복귀 후 첫 도약 전까지 중력 OFF
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

        // 3. 카메라 Frustum(절두체) 뷰포트 외곽 이탈 감지 및 무한 순환 리스폰
        if (m_camera != null)
        {
            Vector3 pos = transform.position;
            Vector3 viewportPos = m_camera.WorldToViewportPoint(pos);

            // 화면 위(1.15f 이상)로 솟구쳐 나가거나, 하단(-0.25f 이하), 또는 좌우 외곽(-0.15f / 1.15f)으로 나간 경우
            if (viewportPos.y > 1.15f || viewportPos.y < -0.25f || viewportPos.x < -0.15f || viewportPos.x > 1.15f || viewportPos.z < 0f)
            {
                // [중앙 로고 가림 방지 - 뷰포트 좌우 날개 가장자리 영역 지정]
                bool spawnOnLeft = (Random.value > 0.5f);
                float targetViewportX = spawnOnLeft 
                    ? Random.Range(0.01f, 0.26f) 
                    : Random.Range(0.74f, 0.99f);

                // 화면 아래(-0.25f)에서 다시 위로 솟구치도록 하단 리스폰 위치 지정
                float targetViewportY = -0.25f;
                float targetZ = Random.Range(6.0f, 8.5f);

                Vector3 spawnWorldPos = m_camera.ViewportToWorldPoint(new Vector3(targetViewportX, targetViewportY, targetZ));
                ResetParams(spawnWorldPos.x, spawnWorldPos.y, false);
            }
        }
        else if (m_cameraTransform != null)
        {
            float camY = m_cameraTransform.position.y;
            if (transform.position.y > camY + 45f || transform.position.y < camY - 45f)
            {
                float newY = camY - Random.Range(15f, 25f);
                float newX = Random.Range(-20f, 20f);
                ResetParams(newX, newY, false);
            }
        }
    }

    private void ExecuteJump()
    {
        if (m_rb == null) return;
        m_rb.useGravity = true; // 도약 시작 시 중력 활성화

        m_rb.linearVelocity = new Vector3(0f, 3.6f, 0f);
        m_rb.angularVelocity = Vector3.zero;

        float angleOffset = Random.Range(13.0f, 25.0f) * m_jumpDir;
        Vector3 jumpDir = Quaternion.Euler(0f, 0f, angleOffset) * Vector3.up;

        float jumpForceMagnitude = Random.Range(3.6f, 4.8f);
        float torqueVal = Random.Range(1.8f, 2.8f);

        m_rb.AddForce(jumpDir * jumpForceMagnitude, ForceMode.Impulse);

        float torqueDirection = jumpDir.x > 0f ? -1f : 1f;
        m_rb.AddTorque(new Vector3(0f, 0f, torqueDirection * torqueVal), ForceMode.Impulse);

        m_forceWaitTimer = 0.05f;
        m_jumpDir *= -1f;
    }
}
