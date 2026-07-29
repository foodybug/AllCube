using UnityEngine;

/// <summary>
/// 플레이어가 점프할 때 해당 도약 위치에 즉각 생성되는 다이내믹 펄스/파티클 점프 이펙트 (0-GC / MaterialPropertyBlock 최적화 버전)
/// </summary>
public class PlayerJumpEffect : MonoBehaviour
{
    private static Material s_sharedJumpMat = null;

    public static Material GetSharedJumpMaterial()
    {
        if (s_sharedJumpMat == null)
        {
            Shader defaultShader = Shader.Find("Sprites/Default");
            if (defaultShader == null) defaultShader = Shader.Find("UI/Default");
            s_sharedJumpMat = new Material(defaultShader);
            s_sharedJumpMat.color = Color.white;
            s_sharedJumpMat.enableInstancing = true;
        }
        return s_sharedJumpMat;
    }

    public static void Spawn(Vector3 jumpPos)
    {
        GameObject effectGo = new GameObject("PlayerJumpEffect");
        effectGo.transform.position = jumpPos;
        PlayerJumpEffect comp = effectGo.AddComponent<PlayerJumpEffect>();
        comp.Init();
    }

    private void Init()
    {
        Material sharedMat = GetSharedJumpMaterial();

        // 1. 도약 지점 충격파 링 생성
        GameObject ringGo = PrimitiveUtil.CreatePrimitive(PrimitiveType.Quad);
        ringGo.name = "JumpRing";
        ringGo.transform.SetParent(transform, false);
        ringGo.transform.localPosition = Vector3.zero;
        ringGo.transform.localScale = new Vector3(0.4f, 0.4f, 1f);

        Collider col = ringGo.GetComponent<Collider>();
        if (col != null) Destroy(col);

        Renderer rend = ringGo.GetComponent<Renderer>();
        if (rend != null)
        {
            rend.sharedMaterial = sharedMat;
            MaterialPropertyBlock mpb = new MaterialPropertyBlock();
            mpb.SetColor("_Color", new Color(0.3f, 0.85f, 1.0f, 0.8f));
            rend.SetPropertyBlock(mpb);
        }

        // 2. 도약 사방 분산 파티클 큐브 8개 흩날림
        int particleCount = 8;
        for (int i = 0; i < particleCount; i++)
        {
            GameObject pCube = PrimitiveUtil.CreatePrimitive(PrimitiveType.Cube);
            pCube.name = "JumpParticle_" + i;
            pCube.transform.SetParent(transform, false);
            pCube.transform.localPosition = Vector3.zero;
            pCube.transform.localScale = Vector3.one * Random.Range(0.18f, 0.32f);

            Collider pCol = pCube.GetComponent<Collider>();
            if (pCol != null) Destroy(pCol);

            Renderer pRend = pCube.GetComponent<Renderer>();
            if (pRend != null)
            {
                pRend.sharedMaterial = sharedMat;
            }

            float angle = (i * (360f / particleCount) + Random.Range(-15f, 15f)) * Mathf.Deg2Rad;
            Vector3 vel = new Vector3(Mathf.Cos(angle) * Random.Range(3f, 6f), Mathf.Sin(angle) * Random.Range(2f, 5f) + 1.5f, Random.Range(-1f, 1f));

            JumpParticleMover mover = pCube.AddComponent<JumpParticleMover>();
            mover.velocity = vel;
        }

        Destroy(gameObject, 0.45f);
    }
}

public class JumpParticleMover : MonoBehaviour
{
    public Vector3 velocity;
    private Renderer m_renderer;
    private static MaterialPropertyBlock s_mpb = null;
    private float m_lifeTimer = 0f;
    private const float MAX_LIFE = 0.4f;
    private static readonly int s_colorPropId = Shader.PropertyToID("_Color");

    void Awake()
    {
        m_renderer = GetComponent<Renderer>();
        if (s_mpb == null) s_mpb = new MaterialPropertyBlock();
    }

    void Update()
    {
        m_lifeTimer += Time.deltaTime;
        transform.position += velocity * Time.deltaTime;
        velocity.y -= 9.81f * Time.deltaTime; // 가벼운 중력 적용

        transform.localScale = Vector3.Lerp(transform.localScale, Vector3.zero, Time.deltaTime * 5f);

        if (m_renderer != null)
        {
            float alpha = Mathf.Clamp01(1f - (m_lifeTimer / MAX_LIFE));
            Color c = new Color(0.4f, 0.9f, 1.0f, alpha);

            m_renderer.GetPropertyBlock(s_mpb);
            s_mpb.SetColor(s_colorPropId, c);
            m_renderer.SetPropertyBlock(s_mpb);
        }
    }
}
