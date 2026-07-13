using UnityEngine;
using System.Collections;
#if UNITY_EDITOR
using UnityEditor;
#endif

public class Util : MonoBehaviour
{
	static public void MyDestroy(Object o)
	{
#if UNITY_EDITOR
		//DestroyImmediate( o);
		Destroy( o);
#else
		Destroy( o);
#endif
	}

	static public void Quit()
	{
#if UNITY_EDITOR
		EditorApplication.isPlaying = false;
		EditorApplication.isPaused = false;
#else
	#if UNITY_ANDROID
		/*
		// dispose
		System.Diagnostics.ProcessThreadCollection ptc = System.Diagnostics.Process.GetCurrentProcess().Threads;
		foreach( System.Diagnostics.ProcessThread pt in ptc)
		{
			pt.Dispose();
		}
		System.Diagnostics.Process.GetCurrentProcess().Kill();
		*/
	#endif

		Application.Quit();
#endif
	}
}

public class Coin : MonoBehaviour
{
    private Renderer m_renderer;
    private Transform m_playerTransform;

    // 보석 등급 변수 추가 (1~5)
    public int grade = 1;

    void Awake()
    {
        m_renderer = GetComponentInChildren<Renderer>();
    }

    void Start()
    {
        ApplyRandomTexture();
        ApplyGradeScale();
    }

    void Update()
    {
        if (m_playerTransform == null)
        {
            Player player = FindAnyObjectByType<Player>();
            if (player != null)
            {
                m_playerTransform = player.transform;
            }
        }

        if (m_playerTransform != null)
        {
            transform.rotation = m_playerTransform.rotation;
        }
    }

    public void ApplyGradeScale()
    {
        // grade 1 일 때 기본 크기 0.5f 에서 시작하여 등급 당 0.2f 씩 증가 (최대 1.3f)
        float targetScale = 0.5f + (grade - 1) * 0.2f;
        transform.localScale = new Vector3(targetScale, targetScale, targetScale);
    }

    public void ApplyRandomTexture()
    {
        if (m_renderer == null)
        {
            m_renderer = GetComponentInChildren<Renderer>();
        }

        if (m_renderer != null && MapManager.Instance != null)
        {
            Texture[] texCube = MapManager.Instance.texCube;
            if (texCube != null && texCube.Length > 0)
            {
                int maxIdx = Mathf.Min(3, texCube.Length - 1);
                if (maxIdx >= 0)
                {
                    int randIdx = Random.Range(0, maxIdx + 1);
                    Texture targetTex = texCube[randIdx];

                    if (targetTex != null)
                    {
                        m_renderer.material.mainTexture = targetTex;
                    }
                }
            }
        }
    }
}

public class CubeLaser : MonoBehaviour
{
    private LineRenderer m_lineRenderer;
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

    void Start()
    {
        // 1. 적대 장애물 재질 입히기 (8번 머티리얼)
        Renderer rend = GetComponent<Renderer>();
        if (rend != null && MapManager.Instance != null)
        {
            rend.sharedMaterial = MapManager.Instance.GetSharedMaterial(8);
        }

        // 2. LineRenderer 컴포넌트 추가
        m_lineRenderer = gameObject.AddComponent<LineRenderer>();
        m_lineRenderer.startColor = Color.red;
        m_lineRenderer.endColor = Color.red;
        m_lineRenderer.startWidth = 1.0f; // 플레이어 크기만한 최초 두께
        m_lineRenderer.endWidth = 1.0f;

        // LineRenderer의 셰이더를 조명 영향을 받지 않는 Unlit 스프라이트 셰이더로 지정
        Shader bgShader = Shader.Find("Sprites/Default");
        if (bgShader == null) bgShader = Shader.Find("UI/Default");
        if (bgShader != null)
        {
            m_lineRenderer.material = new Material(bgShader);
            m_lineRenderer.material.color = Color.red;
        }

        // 3. 타겟팅할 플레이어 최초 위치 획득
        Player player = FindAnyObjectByType<Player>();
        Vector3 playerPos = Vector3.zero;
        if (player != null)
        {
            playerPos = player.transform.position;
        }
        else
        {
            playerPos = transform.position + Vector3.left * 15f;
        }

        Vector3 spawnPos = transform.position;
        m_targetDirection = (playerPos - spawnPos).normalized;

        // 4. 레이저 경고 궤적 끝단 설정 (화면 밖 100미터 길이)
        m_lineRenderer.positionCount = 2;
        m_lineRenderer.SetPosition(0, spawnPos);
        m_lineRenderer.SetPosition(1, spawnPos + m_targetDirection * 100f);

        // 5. 레이저 페이드아웃 -> 대기 -> 돌진 코루틴 실행
        StartCoroutine(LaserRoutine());

        // 생성 후 6초가 지나면 씬을 탈출한 것이므로 자동 소거
        Destroy(gameObject, m_lifetime);
    }

    private IEnumerator LaserRoutine()
    {
        float duration = 1.0f;
        float elapsed = 0.0f;

        // 1초 동안 레이저 빔 두께 선형 축소
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

        // 얇아지다 완전히 사라지면 컴포넌트 정리
        if (m_lineRenderer != null)
        {
            m_lineRenderer.enabled = false;
            Destroy(m_lineRenderer);
        }

        // 사라진 후 1초 대기
        yield return new WaitForSeconds(1.0f);

        // 이동 시작
        m_isMoving = true;
    }

    void Update()
    {
        if (m_isMoving)
        {
            // 정해진 타겟팅 방향으로 아주 빠른 속도로 질주
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
                player = FindAnyObjectByType<Player>();
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
