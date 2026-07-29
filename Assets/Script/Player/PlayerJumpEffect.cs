using UnityEngine;

/// <summary>
/// 플레이어가 점프할 때 해당 도약 위치에 즉각 생성되는 점프 파티클 이펙트
/// Prefab/FX/FX_PlayerJump 프리팹 연동 및 파티클 개수/속도/개별 Color Over Time(시간에 따른 파티클별 고유 색상 변화) 시스템 (0-GC 극대화 최적화)
/// </summary>
public class PlayerJumpEffect : MonoBehaviour
{
    private static GameObject s_jumpEffectPrefab = null;
    private static Material s_sharedJumpMat = null;
    private static Gradient s_cachedGradA = null;
    private static Gradient s_cachedGradB = null;
    private static MaterialPropertyBlock s_sharedMPB = null;

    public static GameObject GetJumpPrefab()
    {
        if (s_jumpEffectPrefab == null)
        {
            s_jumpEffectPrefab = Resources.Load<GameObject>("Prefab/FX/FX_PlayerJump");
            if (s_jumpEffectPrefab == null) s_jumpEffectPrefab = Resources.Load<GameObject>("Prefabs/FX/FX_PlayerJump");
            if (s_jumpEffectPrefab == null) s_jumpEffectPrefab = Resources.Load<GameObject>("FX/FX_PlayerJump");
            if (s_jumpEffectPrefab == null) s_jumpEffectPrefab = Resources.Load<GameObject>("FX_PlayerJump");
        }
        return s_jumpEffectPrefab;
    }

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

    public static MaterialPropertyBlock GetSharedPropertyBlock()
    {
        if (s_sharedMPB == null) s_sharedMPB = new MaterialPropertyBlock();
        return s_sharedMPB;
    }

    private static void EnsureStaticGradients()
    {
        if (s_cachedGradA == null)
        {
            s_cachedGradA = new Gradient();
            s_cachedGradA.SetKeys(
                new GradientColorKey[] { new GradientColorKey(new Color(0.2f, 0.85f, 1.0f), 0f), new GradientColorKey(new Color(0.9f, 0.2f, 0.9f), 1f) },
                new GradientAlphaKey[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(0f, 1f) }
            );

            s_cachedGradB = new Gradient();
            s_cachedGradB.SetKeys(
                new GradientColorKey[] { new GradientColorKey(new Color(1.0f, 0.85f, 0.2f), 0f), new GradientColorKey(new Color(1.0f, 0.3f, 0.1f), 1f) },
                new GradientAlphaKey[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(0f, 1f) }
            );
        }
    }

    public static void Spawn(Vector3 jumpPos)
    {
        int combo = (JumpIntervalTracker.Instance != null) ? JumpIntervalTracker.Instance.ComboCount : 0;
        GameObject prefab = GetJumpPrefab();

        if (prefab != null)
        {
            GameObject fxInstance = Instantiate(prefab, jumpPos, Quaternion.identity);

            // 콜라이더 무력화 (플레이어 도약 물리 충돌 차단 - enabled=false 후 비동기 Destroy)
            Collider[] cols = fxInstance.GetComponentsInChildren<Collider>();
            foreach (var c in cols)
            {
                if (c != null)
                {
                    c.enabled = false;
                    Destroy(c);
                }
            }

            // Combo 비례 파티클 수(+3/combo), 속도 가속 및 각 파티클별 고유 Color Over Time(MinMaxGradient) 적용
            ParticleSystem[] psList = fxInstance.GetComponentsInChildren<ParticleSystem>();
            if (psList != null && psList.Length > 0)
            {
                EnsureStaticGradients();
                float speedMultiplier = 1.0f + (combo * 0.20f); // 콤보당 속도 20% 상승
                int extraParticles = combo * 3;                // 콤보당 파티클 3개 추가

                foreach (var ps in psList)
                {
                    if (ps != null)
                    {
                        var main = ps.main;
                        main.simulationSpeed *= speedMultiplier;

                        // 개별 파티클마다 독립적 Color Over Time (정적 캐싱된 듀얼 그라데이션) 설정
                        var colorOverLifetime = ps.colorOverLifetime;
                        colorOverLifetime.enabled = true;
                        colorOverLifetime.color = new ParticleSystem.MinMaxGradient(s_cachedGradA, s_cachedGradB);

                        // 파티클 방출 (Emit)
                        int baseEmit = 8 + extraParticles;
                        ps.Emit(baseEmit);
                    }
                }
            }

            Destroy(fxInstance, 1.5f);
        }
        else
        {
            // 프리팹이 아직 미등록된 상황에서의 예체 0-GC 도약 파티클 시스템
            GameObject effectGo = new GameObject("PlayerJumpEffect");
            effectGo.transform.position = jumpPos;
            PlayerJumpEffect comp = effectGo.AddComponent<PlayerJumpEffect>();
            comp.Init(combo);
        }
    }

    private void Init(int combo)
    {
        Material sharedMat = GetSharedJumpMaterial();
        MaterialPropertyBlock mpb = GetSharedPropertyBlock();

        // 1. 도약 지점 충격파 링 생성
        GameObject ringGo = PrimitiveUtil.CreatePrimitive(PrimitiveType.Quad);
        ringGo.name = "JumpRing";
        ringGo.transform.SetParent(transform, false);
        ringGo.transform.localPosition = Vector3.zero;
        ringGo.transform.localScale = new Vector3(0.4f, 0.4f, 1f);

        Collider col = ringGo.GetComponent<Collider>();
        if (col != null)
        {
            col.enabled = false;
            Destroy(col);
        }

        Renderer rend = ringGo.GetComponent<Renderer>();
        if (rend != null)
        {
            rend.sharedMaterial = sharedMat;
            mpb.Clear();
            mpb.SetColor("_Color", new Color(0.3f, 0.85f, 1.0f, 0.8f));
            rend.SetPropertyBlock(mpb);
        }

        // 2. Combo마다 particle 개수 +3개씩 증가 & 각 particle마다 고유한 Color Over Time 변환 적용
        int baseParticleCount = 8;
        int totalParticleCount = baseParticleCount + (combo * 3);
        float speedScale = 1.0f + (combo * 0.25f); // 콤보 당 파티클 속도 25% 가속

        for (int i = 0; i < totalParticleCount; i++)
        {
            GameObject pCube = PrimitiveUtil.CreatePrimitive(PrimitiveType.Cube);
            pCube.name = "JumpParticle_" + i;
            pCube.transform.SetParent(transform, false);
            pCube.transform.localPosition = Vector3.zero;
            pCube.transform.localScale = Vector3.one * Random.Range(0.18f, 0.32f);

            Collider pCol = pCube.GetComponent<Collider>();
            if (pCol != null)
            {
                pCol.enabled = false;
                Destroy(pCol);
            }

            Renderer pRend = pCube.GetComponent<Renderer>();
            if (pRend != null)
            {
                pRend.sharedMaterial = sharedMat;
            }

            float angle = (i * (360f / totalParticleCount) + Random.Range(-15f, 15f)) * Mathf.Deg2Rad;
            Vector3 vel = new Vector3(Mathf.Cos(angle) * Random.Range(3f, 6f), Mathf.Sin(angle) * Random.Range(2f, 5f) + 1.5f, Random.Range(-1f, 1f)) * speedScale;

            JumpParticleMover mover = pCube.AddComponent<JumpParticleMover>();
            mover.velocity = vel;
            // 각 파티클별로 완전히 독립적이고 다채로운 시작 Hue와 종료 Hue 지정 (Color Over Time)
            mover.startHue = Random.value;
            mover.endHue = (mover.startHue + Random.Range(0.2f, 0.45f)) % 1.0f;
        }

        Destroy(gameObject, 0.55f);
    }
}

public class JumpParticleMover : MonoBehaviour
{
    public Vector3 velocity;
    public float startHue = 0.5f;
    public float endHue = 0.8f;

    private Renderer m_renderer;
    private static MaterialPropertyBlock s_mpb = null;
    private float m_lifeTimer = 0f;
    private const float MAX_LIFE = 0.45f;
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

        transform.localScale = Vector3.Lerp(transform.localScale, Vector3.zero, Time.deltaTime * 4.5f);

        if (m_renderer != null)
        {
            float progress = Mathf.Clamp01(m_lifeTimer / MAX_LIFE);
            float alpha = 1.0f - progress;
            float currentHue = Mathf.Lerp(startHue, endHue, progress);

            Color particleColor = Color.HSVToRGB(currentHue, 0.85f, 1.0f);
            particleColor.a = alpha;

            m_renderer.GetPropertyBlock(s_mpb);
            s_mpb.SetColor(s_colorPropId, particleColor);
            m_renderer.SetPropertyBlock(s_mpb);
        }
    }
}
