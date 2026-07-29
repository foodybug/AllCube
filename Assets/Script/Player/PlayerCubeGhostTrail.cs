using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 플레이어 큐브(Cube) 형태와 회전값을 본뜬 3D 아후터이미지(Ghost Afterimage) 트레일 컴포넌트
/// [순수 Z축 위치 배치]: 셰이더 조작 대신 transform.position.z = player.z + 0.55f 오프셋을 통해 플레이어 후면에 정확히 배치합니다.
/// </summary>
public class PlayerCubeGhostTrail : MonoBehaviour
{
    private Player m_player;
    private Renderer m_playerRenderer;
    private Rigidbody m_playerRb;

    private float m_spawnTimer = 0f;
    private Material m_ghostMaterialBase;

    // 오브젝트 풀링 (Object Pooling)
    private Queue<GameObject> m_ghostPool = new Queue<GameObject>();
    private const int PREWARM_POOL_SIZE = 35;

    private void Awake()
    {
        EnsureReferences();
        InitGhostMaterial();
        PrewarmPool();
    }

    private void EnsureReferences()
    {
        if (m_player == null) m_player = GetComponent<Player>();
        if (m_playerRenderer == null) m_playerRenderer = GetComponent<Renderer>();
        if (m_playerRenderer == null) m_playerRenderer = GetComponentInChildren<Renderer>();
        if (m_playerRb == null) m_playerRb = GetComponent<Rigidbody>();
    }

    private void InitGhostMaterial()
    {
        Shader shader = Shader.Find("Sprites/Default");
        if (shader == null) shader = Shader.Find("UI/Default");
        if (shader == null) shader = Shader.Find("Unlit/Transparent");
        if (shader == null) shader = Shader.Find("Unlit/Color");
        if (shader == null) shader = Shader.Find("Legacy Shaders/Transparent/Diffuse");

        m_ghostMaterialBase = new Material(shader != null ? shader : Shader.Find("Sprites/Default"));
    }

    private void PrewarmPool()
    {
        for (int i = 0; i < PREWARM_POOL_SIZE; i++)
        {
            GameObject ghost = PrimitiveUtil.CreatePrimitive(PrimitiveType.Cube);
            ghost.name = "Player_Ghost_Cube_Pooled";

            Collider col = ghost.GetComponent<Collider>();
            if (col != null) Destroy(col);
            Rigidbody rb = ghost.GetComponent<Rigidbody>();
            if (rb != null) Destroy(rb);

            Renderer ghostRend = ghost.GetComponent<Renderer>();
            if (ghostRend != null && m_ghostMaterialBase != null)
            {
                ghostRend.material = new Material(m_ghostMaterialBase);
            }

            ghost.SetActive(false);
            m_ghostPool.Enqueue(ghost);
        }
    }

    private GameObject GetGhostFromPool()
    {
        if (m_ghostPool.Count > 0)
        {
            GameObject ghost = m_ghostPool.Dequeue();
            ghost.SetActive(true);
            return ghost;
        }

        GameObject newGhost = PrimitiveUtil.CreatePrimitive(PrimitiveType.Cube);
        newGhost.name = "Player_Ghost_Cube_Pooled";
        Collider col = newGhost.GetComponent<Collider>();
        if (col != null) Destroy(col);
        Rigidbody rb = newGhost.GetComponent<Rigidbody>();
        if (rb != null) Destroy(rb);

        Renderer rend = newGhost.GetComponent<Renderer>();
        if (rend != null && m_ghostMaterialBase != null)
        {
            rend.material = new Material(m_ghostMaterialBase);
        }
        return newGhost;
    }

    private void ReturnGhostToPool(GameObject ghost)
    {
        if (ghost == null) return;
        ghost.SetActive(false);
        m_ghostPool.Enqueue(ghost);
    }

    private void Update()
    {
        // 트레일(Ghost Trail) 이펙트 완전 비활성화
        return;
    }

    private Color GetPlayerBaseColor()
    {
        if (m_playerRenderer != null && m_playerRenderer.material != null)
        {
            if (m_playerRenderer.material.HasProperty("_Color"))
            {
                Color c = m_playerRenderer.material.color;
                if (c.r > 0.05f || c.g > 0.05f || c.b > 0.05f)
                {
                    return c;
                }
            }
        }
        // 기본 플레이어 시안 네온 색상
        return new Color(0f, 0.95f, 1.0f, 0.95f);
    }

    private void SpawnGhostCube(int combo)
    {
        GameObject ghost = GetGhostFromPool();

        // 순수 Transform.position.Z 위치 조절: 플레이어 본체(Z=0f, 후면 Z=+0.5f) 바로 뒤쪽인 Z = +0.55f 위치에 정확히 스폰
        Vector3 pos = transform.position;
        pos.z = transform.position.z + 0.55f;
        ghost.transform.position = pos;
        ghost.transform.rotation = transform.rotation;

        Vector3 scale = transform.localScale;
        if (scale == Vector3.zero) scale = Vector3.one;
        ghost.transform.localScale = scale;

        float lifetime = Mathf.Clamp(0.25f + (combo * 0.07f), 0.25f, 1.0f);
        Color playerBaseColor = GetPlayerBaseColor();
        playerBaseColor.a = 0.95f;

        Renderer ghostRend = ghost.GetComponent<Renderer>();
        if (ghostRend != null && ghostRend.material != null)
        {
            if (m_playerRenderer != null && m_playerRenderer.material != null && m_playerRenderer.material.mainTexture != null)
            {
                ghostRend.material.mainTexture = m_playerRenderer.material.mainTexture;
            }

            SetupMaterialColorAndAlpha(ghostRend.material, playerBaseColor);
        }

        StartCoroutine(AnimateGhostCube_CR(ghost, lifetime, playerBaseColor));
    }

    private void SetupMaterialColorAndAlpha(Material mat, Color color)
    {
        if (mat == null) return;
        mat.color = color;
        if (mat.HasProperty("_Color")) mat.SetColor("_Color", color);
        if (mat.HasProperty("_TintColor")) mat.SetColor("_TintColor", color);

        if (mat.HasProperty("_Mode"))
        {
            mat.SetFloat("_Mode", 2); // Fade Mode
            mat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
            mat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            mat.SetInt("_ZWrite", 0);
            mat.DisableKeyword("_ALPHATEST_ON");
            mat.EnableKeyword("_ALPHABLEND_ON");
            mat.DisableKeyword("_ALPHAPREMULTIPLY_ON");
        }
    }

    private IEnumerator AnimateGhostCube_CR(GameObject ghost, float duration, Color startPlayerColor)
    {
        if (ghost == null) yield break;

        Renderer rend = ghost.GetComponent<Renderer>();
        Vector3 startScale = ghost.transform.localScale;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            if (ghost == null || !ghost.activeInHierarchy) yield break;

            elapsed += Time.deltaTime;
            float t = elapsed / duration;

            if (rend != null && rend.material != null)
            {
                // 플레이어 고유 색상에서 시작하여 끝(Tail)으로 갈수록 Pure White(하얀색)로 전이되며 알파 투명화
                Color c = Color.Lerp(startPlayerColor, Color.white, t);
                c.a = Mathf.Lerp(startPlayerColor.a, 0f, t);
                SetupMaterialColorAndAlpha(rend.material, c);
            }

            ghost.transform.localScale = Vector3.Lerp(startScale, startScale * 0.2f, t);
            yield return null;
        }

        ReturnGhostToPool(ghost);
    }

    private void OnDestroy()
    {
        while (m_ghostPool != null && m_ghostPool.Count > 0)
        {
            GameObject g = m_ghostPool.Dequeue();
            if (g != null) Destroy(g);
        }
    }
}
