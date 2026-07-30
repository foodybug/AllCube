using UnityEngine;

/// <summary>
/// 플레이어가 점프할 때 해당 도약 위치에 즉각 생성되는 다이내믹 점프 파티클 이펙트 (0-GC / MaterialPropertyBlock 최적화 버전)
/// Combo당 파티클 개수 +3개씩 증가, 속도(velocity) 아래쪽 가속 및 개별 Color Over Time(시간에 따른 입자별 고유 색상 변환) 시스템
/// </summary>
public class PlayerJumpEffect : MonoBehaviour
{
    private static Material s_sharedJumpMat = null;
    private static MaterialPropertyBlock s_sharedMPB = null;

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

    public static void Spawn(Vector3 jumpPos)
    {
        int combo = (JumpIntervalTracker.Instance != null) ? JumpIntervalTracker.Instance.ComboCount : 0;

        // 순수 0-GC 도약 파티클 오브젝트 생성 및 초기화
        GameObject effectGo = new GameObject("PlayerJumpEffect");
        effectGo.transform.position = jumpPos;
        PlayerJumpEffect comp = effectGo.AddComponent<PlayerJumpEffect>();
        comp.Init(combo);
    }

    private void Init(int combo)
    {
        Material sharedMat = GetSharedJumpMaterial();

        // Combo마다 particle 개수 +3개씩 증가 & 각 particle마다 아래쪽 속도 가속 및 고유 Color Over Time 변환 적용
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

            // 콜라이더 즉각 비활성화 후 비동기 파괴 (물리 충돌 100% 차단 및 0-GC)
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

            // 하단 180도 부채꼴 각도(200도 ~ 340도)로 아래쪽을 향해 힘차게 분사
            float angle = Random.Range(200f, 340f) * Mathf.Deg2Rad;
            float speedY = -Mathf.Abs(Mathf.Sin(angle)) * Random.Range(3.5f, 7.5f) - 2.0f;
            Vector3 vel = new Vector3(Mathf.Cos(angle) * Random.Range(2.5f, 5.5f), speedY, Random.Range(-1f, 1f)) * speedScale;

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
