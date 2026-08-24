using UnityEngine;
using System.Collections;

public class CubeLaser : MonoBehaviour
{
    private LineRenderer m_lineRenderer;
    private GameObject m_lineObj;
    private Vector3 m_targetDirection;
    private float m_speed = 50f;
    private bool m_isMoving = false;
    private float m_lifetime = 6f; // 안전 해제 수명

    void Awake()
    {
        Collider col = GetComponent<Collider>();
        if (col != null) col.isTrigger = true; // 몸체 충돌은 통과하는 Trigger 처리
        Rigidbody rb = GetComponent<Rigidbody>();
        if (rb != null) Destroy(rb);
    }

    private Color GetTextureColor(Texture tex)
    {
        if (tex == null) return new Color(0.95f, 0.2f, 0.4f);

        string name = tex.name.ToLower();
        if (name.Contains("enemy_1")) return new Color(0.70f, 0.72f, 0.71f);
        if (name.Contains("enemy_2")) return new Color(0.75f, 0.72f, 0.70f);
        if (name.Contains("enemy_3")) return new Color(0.73f, 0.73f, 0.71f);
        if (name.Contains("enemy_4")) return new Color(0.70f, 0.72f, 0.73f);
        if (name.Contains("break1")) return new Color(0.84f, 0.84f, 0.84f);
        if (name.Contains("break2")) return new Color(0.77f, 0.77f, 0.77f);
        if (name.Contains("1")) return new Color(0.89f, 0.89f, 0.89f);
        if (name.Contains("2")) return new Color(0.40f, 0.69f, 0.61f);
        if (name.Contains("3")) return new Color(0.98f, 0.63f, 0.44f);
        if (name.Contains("4")) return new Color(0.95f, 0.88f, 0.60f);
        if (name.Contains("5")) return new Color(0.45f, 0.65f, 0.80f);
        if (name.Contains("6")) return new Color(0.89f, 0.45f, 0.49f);

        return new Color(0.95f, 0.2f, 0.4f);
    }

    private Color GetLaserColor(Renderer rend)
    {
        if (rend == null || rend.sharedMaterial == null) return new Color(1.0f, 0.2f, 0.4f, 1.0f);

        Material mat = rend.sharedMaterial;
        Color tintColor = Color.white;
        if (mat.HasProperty("_Color"))
        {
            tintColor = mat.color;
        }

        Color texColor = Color.white;
        if (mat.mainTexture != null)
        {
            texColor = GetTextureColor(mat.mainTexture);
        }

        return new Color(texColor.r * tintColor.r, texColor.g * tintColor.g, texColor.b * tintColor.b, texColor.a * tintColor.a);
    }

    void Start()
    {
        // 1. 적대 장애물 전용 텍스처 머티리얼 적용 (5번 - Enemy 텍스처 지정)
        Renderer rend = GetComponent<Renderer>();
        if (rend != null && MapManager.Instance != null)
        {
            rend.sharedMaterial = MapManager.Instance.GetSharedMaterial(5);
        }

        // 레이저 블럭 시각적 식별 강화를 위해 1.2배 크기 적용
        transform.localScale = Vector3.one * 1.2f;

        // 2. 360도 무작위 출현 각도(spawnAngle) 및 화면 외곽 스폰 위치 계산 (카메라 시야 경계 14~16m)
        float spawnAngle = Random.Range(0f, 360f);
        float spawnDistance = Random.Range(14.0f, 16.0f);

        Player player = FindFirstObjectByType<Player>();
        Vector3 playerPos = Vector3.zero;
        if (player != null)
        {
            playerPos = player.transform.position;
        }
        else if (CameraManager.Instance != null && CameraManager.Instance.Target != null)
        {
            playerPos = CameraManager.Instance.Target.position;
        }

        Vector3 randomRadialDir = new Vector3(Mathf.Cos(spawnAngle * Mathf.Deg2Rad), Mathf.Sin(spawnAngle * Mathf.Deg2Rad), 0f);
        Vector3 spawnPos = playerPos + randomRadialDir * spawnDistance;
        spawnPos.z = 0f;

        transform.position = spawnPos;

        // 플레이어 본체 조준 오프셋 각도 (-3° ~ +3°로 축소하여 화면 및 플레이어 궤적에 확실히 노출)
        float aimOffsetAngle = Random.Range(-3f, 3f);
        Vector3 baseTargetDir = (playerPos - spawnPos).normalized;
        if (baseTargetDir == Vector3.zero) baseTargetDir = Vector3.left;

        m_targetDirection = (Quaternion.Euler(0, 0, aimOffsetAngle) * baseTargetDir).normalized;

        // 다른 모든 큐브 장애물들과 동일하게 Z축 회전을 0으로 정직교 정렬 (월드 이동 궤적은 m_targetDirection 벡터로 수행)
        transform.rotation = Quaternion.Euler(90f, 0f, 0f);

        // 3. 큐브 메쉬 렌더러와 간섭하지 않도록 별도 자식 GameObject에 LineRenderer 생성
        m_lineObj = new GameObject("LaserLineEffect");
        m_lineObj.transform.SetParent(transform, false);
        m_lineObj.transform.localPosition = Vector3.zero;
        m_lineObj.transform.localRotation = Quaternion.identity;

        m_lineRenderer = m_lineObj.AddComponent<LineRenderer>();
        
        Color laserColor = GetLaserColor(rend);

        m_lineRenderer.startColor = laserColor;
        m_lineRenderer.endColor = laserColor;
        m_lineRenderer.startWidth = 1.0f; // 플레이어 크기만한 최초 두께
        m_lineRenderer.endWidth = 1.0f;

        // LineRenderer의 셰이더를 조명 영향을 받지 않는 Unlit 스프라이트 셰이더로 지정
        Shader bgShader = Shader.Find("Sprites/Default");
        if (bgShader == null) bgShader = Shader.Find("UI/Default");
        if (bgShader != null)
        {
            m_lineRenderer.material = new Material(bgShader);
            m_lineRenderer.material.color = laserColor;
        }

        // 4. 화면 양방향 끝까지 잘림 없이 길게 뻗어나가도록 양방향 300미터 길이 적용
        m_lineRenderer.positionCount = 2;
        m_lineRenderer.SetPosition(0, spawnPos - m_targetDirection * 300f);
        m_lineRenderer.SetPosition(1, spawnPos + m_targetDirection * 300f);

        // 5. 레이저 페이드아웃 -> 돌진 코루틴 실행
        StartCoroutine(LaserRoutine());

        // 생성 후 6초가 지나면 씬을 탈출한 것이므로 자동 소거
        Destroy(gameObject, m_lifetime);
    }

    private IEnumerator LaserRoutine()
    {
        float duration = 0.7f;
        float elapsed = 0.0f;

        // 0.7초 동안 레이저 빔 두께 선형 축소 (조준 경고 연출)
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;
            float currentWidth = Mathf.Lerp(1.0f, 0.0f, t);
            if (m_lineRenderer != null)
            {
                m_lineRenderer.startWidth = currentWidth;
                m_lineRenderer.endWidth = currentWidth;
            }
            yield return null;
        }

        // 경고 이펙트 종료 시 자식 이펙트 오브젝트 제거 (메인 큐브 메쉬 영향 제로)
        if (m_lineObj != null)
        {
            Destroy(m_lineObj);
            m_lineObj = null;
            m_lineRenderer = null;
        }

        // 경고선 축소 완료 후 지체 없이 바로 돌진 (0.05초)
        yield return new WaitForSeconds(0.05f);

        // 이동 시작
        m_isMoving = true;
    }

    void Update()
    {
        if (m_isMoving)
        {
            // 정해진 타겟팅 방향으로 빠른 속도로 질주
            transform.Translate(m_targetDirection * m_speed * Time.deltaTime, Space.World);
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
                player = FindFirstObjectByType<Player>();
            }

            if (player != null)
            {
                Debug.Log("[CubeLaser] Player hit by laser cube! Killing player.");
                MainManager.lastDeathCause = "Laser";
                player.KillPlayer();
            }
        }
    }
}
