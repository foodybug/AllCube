using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

public class MapManager : MonoBehaviour
{
    public enum eMapProp
    {
        eMapProp_None = 0,
        eMapProp_Coin,
        eMapProp_Normal,
        eMapProp_Break,
        eMapProp_MoveX,
        eMapProp_MoveY,
        eMapProp_JumpZero,
        eMapProp_Blink
    }

    static MapManager m_instance;
    public static MapManager Instance { get { return m_instance; } }

    public GameObject goCubeSrc;
    public GameObject goCoinSrc;
    public GameObject goCoinEffSrc;
    public GameObject goCubeEffSrc;

    public Texture[] texCube = new Texture[6];

    private List<GameObject> m_listCube = new List<GameObject>();
    private List<GameObject> m_listCoin = new List<GameObject>();
    private List<InfiniteScrollObject> m_scrollObjects = new List<InfiniteScrollObject>();
    private Coroutine m_cleanupCoroutine = null;
    private Material[] m_sharedMaterials;
    private GameObject m_goBackgroundContainer = null;
    private List<ParallaxScroll> m_parallaxObjects = new List<ParallaxScroll>();
    private Material m_farSkyMaterial = null;
    private Material m_midCubeMaterial = null;
    private float m_fCubeSize = 0.0f;
    private float m_fLerp = 0.1f;

    private int m_nTotalCoinsCollected = 0;
    public int TotalCoinsCollected { get { return m_nTotalCoinsCollected; } }

    private int m_highestGeneratedY = 0;
    private GameObject m_goFloor;

    [System.Serializable]
    public struct DifficultyTier
    {
        public int minHeight;
        public int minSpawnX;
        public int maxSpawnX;
        public int staticObstacleInterval;
        public int flyingObstacleInterval;
        public int blinkObstacleInterval; // Blink 장애물 생성 주기 (0 이면 미생성)
        public int minBlinkHeight;        // Blink 장애물이 등장하기 시작하는 최소 높이
        public int coinInterval;
        public int coinSequence;
        public float minFlyingSpeed;
        public float maxFlyingSpeed;
        public int initialJumps;
        public List<GameObject> segmentPrefabs; // 타일맵 기반 조립식 맵 세그먼트 프리팹 리스트
    }

    [Header("Stage Generation Settings")]
    [SerializeField] private int m_generationAheadRange = 20;
    [SerializeField] private int m_cleanupBehindRange = 18;

    public int MinSpawnX { get { return GetTierForHeight(0).minSpawnX; } }
    public int MaxSpawnX { get { return GetTierForHeight(0).maxSpawnX; } }

    [Header("Level Design Settings")]
    [SerializeField]
    private List<DifficultyTier> m_difficultyTier = new List<DifficultyTier>()
    {
        new DifficultyTier { minHeight = 0, minSpawnX = -30, maxSpawnX = 30, staticObstacleInterval = 10, flyingObstacleInterval = 15, blinkObstacleInterval = 0, minBlinkHeight = 0, coinInterval = 3, coinSequence = 1, minFlyingSpeed = 4f, maxFlyingSpeed = 6f, initialJumps = 10, segmentPrefabs = null },
        new DifficultyTier { minHeight = 20, minSpawnX = -25, maxSpawnX = 25, staticObstacleInterval = 8, flyingObstacleInterval = 12, blinkObstacleInterval = 0, minBlinkHeight = 0, coinInterval = 4, coinSequence = 1, minFlyingSpeed = 6f, maxFlyingSpeed = 8f, initialJumps = 8, segmentPrefabs = null },
        new DifficultyTier { minHeight = 40, minSpawnX = -20, maxSpawnX = 20, staticObstacleInterval = 6, flyingObstacleInterval = 9, blinkObstacleInterval = 8, minBlinkHeight = 40, coinInterval = 5, coinSequence = 1, minFlyingSpeed = 8f, maxFlyingSpeed = 11f, initialJumps = 7, segmentPrefabs = null },
        new DifficultyTier { minHeight = 60, minSpawnX = -15, maxSpawnX = 15, staticObstacleInterval = 5, flyingObstacleInterval = 7, blinkObstacleInterval = 6, minBlinkHeight = 60, coinInterval = 6, coinSequence = 1, minFlyingSpeed = 10f, maxFlyingSpeed = 14f, initialJumps = 5, segmentPrefabs = null },
        new DifficultyTier { minHeight = 80, minSpawnX = -10, maxSpawnX = 10, staticObstacleInterval = 4, flyingObstacleInterval = 5, blinkObstacleInterval = 5, minBlinkHeight = 80, coinInterval = 7, coinSequence = 1, minFlyingSpeed = 12f, maxFlyingSpeed = 18f, initialJumps = 4, segmentPrefabs = null }
    };

    // 현재 레벨에 적용되는 런타임 세팅 값들
    private int m_staticObstacleInterval = 5;
    private int m_flyingObstacleInterval = 8;
    private int m_coinInterval = 3;
    private int m_coinSequence = 1;
    private float m_minFlyingSpeed = 6.0f;
    private float m_maxFlyingSpeed = 10.0f;
    private int m_initialJumps = 10;

    public int InitialJumps { get { return m_initialJumps; } }

    public DifficultyTier GetTierForHeight(int y)
    {
        // 1. 순환 주기 계산 (마지막 티어의 minHeight와 그 이전 티어의 minHeight 차이 기준)
        int cycleHeight = 100;
        int count = m_difficultyTier.Count;
        if (count > 1)
        {
            int lastDiff = m_difficultyTier[count - 1].minHeight - m_difficultyTier[count - 2].minHeight;
            cycleHeight = m_difficultyTier[count - 1].minHeight + lastDiff;
        }

        // 음수 보정 및 순환 횟수/가상 높이 계산
        int cycleCount = 0;
        int virtualY = y;
        if (y > 0 && cycleHeight > 0)
        {
            cycleCount = y / cycleHeight;
            virtualY = y % cycleHeight;
        }

        // 2. 가상 높이(virtualY) 기준 기본 티어 조회
        DifficultyTier activeTier = new DifficultyTier();
        bool found = false;
        int maxMinHeight = -1;

        foreach (var tier in m_difficultyTier)
        {
            if (virtualY >= tier.minHeight && tier.minHeight > maxMinHeight)
            {
                activeTier = tier;
                maxMinHeight = tier.minHeight;
                found = true;
            }
        }

        if (!found)
        {
            // Fallback (아무것도 찾지 못했을 때 기본 세팅값 반환)
            activeTier.minHeight = 0;
            activeTier.minSpawnX = -30;
            activeTier.maxSpawnX = 30;
            activeTier.staticObstacleInterval = 5;
            activeTier.flyingObstacleInterval = 8;
            activeTier.blinkObstacleInterval = 0; // 기본 비활성
            activeTier.minBlinkHeight = 0;
            activeTier.coinInterval = 3;
            activeTier.coinSequence = 1;
            activeTier.minFlyingSpeed = 6.0f;
            activeTier.maxFlyingSpeed = 10.0f;
            activeTier.initialJumps = 10;
            activeTier.segmentPrefabs = null;
        }

        // 3. 순환 횟수(cycleCount)에 따른 보정치 적용 (패널티 강화, 어드밴티지 약화)
        if (cycleCount > 0)
        {
            // 3-1. 패널티 강화
            // staticObstacleInterval (장애물 간격 좁힘 -> 더 자주 나옴, 최소 2)
            activeTier.staticObstacleInterval = Mathf.Max(2, activeTier.staticObstacleInterval - cycleCount);

            // flyingObstacleInterval (비행 장애물 간격 좁힘 -> 더 자주 나옴, 최소 2)
            activeTier.flyingObstacleInterval = Mathf.Max(2, activeTier.flyingObstacleInterval - cycleCount);

            // blinkObstacleInterval (깜빡이 장애물 간격 좁힘 -> 더 자주 나옴, 최소 2)
            if (activeTier.blinkObstacleInterval > 0)
            {
                activeTier.blinkObstacleInterval = Mathf.Max(2, activeTier.blinkObstacleInterval - cycleCount);
            }

            // 비행 장애물 속도 증가 (순환당 1.5f씩 상승)
            activeTier.minFlyingSpeed += cycleCount * 1.5f;
            activeTier.maxFlyingSpeed += cycleCount * 1.5f;

            // 좌우 스폰 폭 좁히기 (순환당 좌우 1칸씩 좁힘 -> 기둥 간격 좁아져 위협 상승)
            // 단, 너무 좁아져 진행이 불가하지 않도록 최소 가로폭 10 유지
            int requestedMinX = activeTier.minSpawnX + cycleCount;
            int requestedMaxX = activeTier.maxSpawnX - cycleCount;
            if (requestedMaxX - requestedMinX >= 10)
            {
                activeTier.minSpawnX = requestedMinX;
                activeTier.maxSpawnX = requestedMaxX;
            }
            else
            {
                // 중간점을 기준으로 최소 가로폭 10 유지
                int center = (activeTier.minSpawnX + activeTier.maxSpawnX) / 2;
                activeTier.minSpawnX = center - 5;
                activeTier.maxSpawnX = center + 5;
            }

            // 3-2. 어드밴티지 약화
            // coinInterval (코인 획득 간격 늘림 -> 덜 나옴, 최대 20)
            activeTier.coinInterval = Mathf.Min(20, activeTier.coinInterval + cycleCount);

            // coinSequence (코인 연속 스폰 갯수 줄임 -> 덜 나옴, 최소 1)
            activeTier.coinSequence = Mathf.Max(1, activeTier.coinSequence - cycleCount);

            // initialJumps (시작 점프 부여 개수 낮춤, 최소 3)
            activeTier.initialJumps = Mathf.Max(3, activeTier.initialJumps - cycleCount);
        }

        return activeTier;
    }

    [Header("Infinite Scroll Settings")]
    [SerializeField] private bool m_enableInfiniteScroll = true;
    [SerializeField] private float m_scrollWidth = 60.0f;

    public int CoinCount { get { return m_listCoin.Count; } }

    void Awake()
    {
        m_instance = this;

        Renderer srcRend = goCubeSrc != null ? goCubeSrc.GetComponent<Renderer>() : null;
        if (srcRend != null && srcRend.sharedMaterial != null)
        {
            int texCount = texCube != null ? texCube.Length : 0;
            int matCount = Mathf.Max(6, texCount);
            m_sharedMaterials = new Material[matCount];
            for (int i = 0; i < matCount; i++)
            {
                m_sharedMaterials[i] = new Material(srcRend.sharedMaterial);
                if (texCube != null && i < texCube.Length && texCube[i] != null)
                {
                    m_sharedMaterials[i].mainTexture = texCube[i];
                }
            }
        }

        // 런타임에 레벨 설정에 따라 동적으로 보정됨

        if (goCubeSrc != null)
        {
            Collider col = goCubeSrc.GetComponent<Collider>();
            if (col != null)
            {
                if (col is BoxCollider boxCol)
                {
                    m_fCubeSize = boxCol.size.x * goCubeSrc.transform.localScale.x;
                }
                else
                {
                    m_fCubeSize = col.bounds.size.x;
                }
            }
            else
            {
                m_fCubeSize = 1.0f;
            }
        }
        else
        {
            m_fCubeSize = 1.0f;
        }

        if (m_fCubeSize < 0.1f)
        {
            m_fCubeSize = 1.0f;
            Debug.LogWarning($"[MapManager Safety] m_fCubeSize was detected as too small ({m_fCubeSize}). Forced to 1.0f.");
        }

        // 무한 스크롤 반복 가로폭을 기둥 사이의 정확한 거리로 동적 계산
        m_scrollWidth = (MaxSpawnX - MinSpawnX) * m_fCubeSize;
    }

    void Start()
    {
    }

    void Update()
    {
        if (MainManager.Instance == null) return;

        if (MainManager.Instance.eCurState == eGameState.eGameState_Play)
        {
            GameObject playerGo = CameraManager.Instance.Target != null ? CameraManager.Instance.Target.gameObject : null;
            if (playerGo != null)
            {
                float playerY = playerGo.transform.position.y / (m_fCubeSize > 0f ? m_fCubeSize : 1.0f);

                // 플레이어가 가로 경계선(minSpawnX, maxSpawnX) 바깥으로 나갔을 때 즉사 처리
                Player playerComp = playerGo.GetComponent<Player>();
                if (playerComp != null)
                {
                    int currentHeightY = Mathf.RoundToInt(playerY);
                    DifficultyTier tier = GetTierForHeight(currentHeightY);
                    
                    float playerWorldX = playerGo.transform.position.x;
                    float size = m_fCubeSize > 0f ? m_fCubeSize : 1.0f;
                    float currentScrollWidth = (tier.maxSpawnX - tier.minSpawnX) * size;
                    
                    if (currentScrollWidth > 0f)
                    {
                        float cycle = Mathf.Round(playerWorldX / currentScrollWidth);
                        float localPlayerX = playerWorldX - cycle * currentScrollWidth;
                        float maxLimit = tier.maxSpawnX * size;
                        
                        // 기둥 경계 벽 중심선 너머(마진 0.4f)로 넘어가려 할 때 즉시 사망 판정
                        if (Mathf.Abs(localPlayerX) > (maxLimit - 0.4f))
                        {
                            Debug.Log($"[MapManager OutOfBounds Check] Player out of bounds! heightY: {currentHeightY}, localX: {localPlayerX:F2}, Limit: {maxLimit - 0.4f:F2}. Killing player.");
                            MainManager.lastDeathCause = "DeadZone";
                            playerComp.KillPlayer();
                        }
                    }
                }

                // 배경색 RGB 무한 순환 연동 (높이 50m 당 한 사이클씩 더 자주 순환)
                float hueCycleHeight = 50.0f;
                float bgPlayerWorldY = playerGo.transform.position.y;
                float bgHue = (bgPlayerWorldY / hueCycleHeight) % 1.0f;
                if (bgHue < 0f) bgHue += 1.0f;

                // 채도(Saturation)와 명도(Value)를 상향 조정하여 화사함을 더하고 노란색 등의 색감이 묻히지 않게 설계
                float satFar = 0.55f + Mathf.Sin(bgPlayerWorldY * 0.04f) * 0.1f;
                float valFar = 0.25f + Mathf.Cos(bgPlayerWorldY * 0.02f + Time.time * 0.08f) * 0.06f;

                // 원경과 중경에 색조 오프셋(+0.15f)을 주어 서로 다른 색감이 공존하는 입체적 톤 연출
                float midHue = (bgHue + 0.15f) % 1.0f;
                float satMid = 0.6f + Mathf.Cos(bgPlayerWorldY * 0.05f) * 0.1f;
                float valMid = 0.35f + Mathf.Sin(bgPlayerWorldY * 0.03f + Time.time * 0.1f) * 0.07f;

                if (m_farSkyMaterial != null)
                {
                    m_farSkyMaterial.color = Color.HSVToRGB(bgHue, satFar, valFar);
                }
                if (m_midCubeMaterial != null)
                {
                    Color midColor = Color.HSVToRGB(midHue, satMid, valMid);
                    midColor.a = 0.45f;
                    m_midCubeMaterial.color = midColor;
                }

                // Update Floor_DeadZone position to follow player's Y position upwards
                if (m_goFloor != null)
                {
                    float cameraHeight = 15f;
                    if (CameraManager.Instance != null && CameraManager.Instance.mainCamera != null)
                    {
                        cameraHeight = CameraManager.Instance.mainCamera.orthographicSize;
                    }

                    float playerWorldY = playerGo.transform.position.y;
                    float currentFloorY = m_goFloor.transform.position.y;

                    // Place it sufficiently below the player, outside the bottom edge of the camera
                    float targetFloorWorldY = playerWorldY - cameraHeight - 12.0f;

                    // Y=100 거대 큐브의 윗면이 targetFloorWorldY + 1.0f 에 오도록 실제 중심 Y 포지션을 아래로 49.0f 보정

                    float targetPosCenterY = targetFloorWorldY - 49.0f;

                    // Only move UP, never down
                    if (targetPosCenterY > currentFloorY)
                    {
                        Vector3 floorPos = m_goFloor.transform.position;
                        floorPos.x = playerGo.transform.position.x; // Keep it centered horizontally with the player
                        floorPos.y = targetPosCenterY;
                        m_goFloor.transform.position = floorPos;
                    }


                }

                float playerX = playerGo.transform.position.x;

                // 무한 스크롤 오브젝트 일괄 갱신 (Update 인터롭 오버헤드 최적화)
                m_scrollObjects.RemoveAll(item => item == null);
                for (int i = 0; i < m_scrollObjects.Count; i++)
                {
                    m_scrollObjects[i].UpdateScroll(playerX);
                }

                // Parallax 배경 오브젝트 일괄 갱신
                int parallaxCount = m_parallaxObjects.Count;
                for (int i = 0; i < parallaxCount; i++)
                {
                    if (m_parallaxObjects[i] != null)
                    {
                        m_parallaxObjects[i].UpdateParallax();
                    }
                }

                // 1. Generate up to playerY + m_generationAheadRange rows ahead
                int targetY = Mathf.CeilToInt(playerY) + m_generationAheadRange;
                if (targetY > m_highestGeneratedY)
                {
                    GenerateRowsUpTo(targetY);
                }

                // 3. Fall to death check (Moved to OnTriggerEnter in Player.cs for a 1-second delay camera stop effect)

                // 4. Dynamic warning / visual feedback on all active coins
                Player player = playerGo.GetComponent<Player>();
                if (player != null)
                {
                    float speedMultiplier = Mathf.Max(1.0f, 15.0f / (player.JumpCount + 1f));

                    // Y spin and Z tilt
                    float targetZRotation = player.NextJumpDir > 0 ? -45f : 45f;
                    float spinSpeed = 30f * speedMultiplier;
                    float lerpSpeed = 5f * speedMultiplier;

                    // HSV rainbow shift
                    float hue = (Time.time * 0.08f * speedMultiplier) % 1.0f;
                    Color rainbowColor = Color.HSVToRGB(hue, 0.9f, 0.9f);

                    foreach (GameObject coin in m_listCoin)
                    {
                        if (coin != null)
                        {
                            coin.transform.Rotate(0, spinSpeed * Time.deltaTime, 0, Space.World);

                            Quaternion targetRot = Quaternion.Euler(0, coin.transform.rotation.eulerAngles.y, targetZRotation);
                            coin.transform.rotation = Quaternion.Lerp(coin.transform.rotation, targetRot, Time.deltaTime * lerpSpeed);


                        }
                    }
                }
            }
        }
    }

    public void ApplyLevelConfig(int nStage)
    {
        // 무한 모드에서는 stage 대신 시작점(y=0) 기준의 DifficultyTier 설정을 최초 적용합니다.
        DifficultyTier config = GetTierForHeight(0);

        m_staticObstacleInterval = config.staticObstacleInterval;
        m_flyingObstacleInterval = config.flyingObstacleInterval;
        m_coinInterval = config.coinInterval;
        m_coinSequence = config.coinSequence;
        m_minFlyingSpeed = config.minFlyingSpeed;
        m_maxFlyingSpeed = config.maxFlyingSpeed;
        m_initialJumps = config.initialJumps;

        // 무한 스크롤 반복 가로폭을 기둥 사이의 정확한 거리로 동적 계산
        m_scrollWidth = (config.maxSpawnX - config.minSpawnX) * m_fCubeSize;
    }

    public void LoadCubeMap(int nStage)
    {
        ApplyLevelConfig(nStage); // 레벨 디자인에 맞는 세팅 적용

        UnLoadCubeMap();
        m_highestGeneratedY = 0;
        m_nTotalCoinsCollected = 0;

        SpawnFloor();
        CreateBackground();

        // Spawn starting platform and walls
        GenerateRowsUpTo(m_generationAheadRange);

        if (m_cleanupCoroutine != null)
        {
            StopCoroutine(m_cleanupCoroutine);
        }
        m_cleanupCoroutine = StartCoroutine(CleanupRoutine_CR());
    }

    private void SpawnFloor()
    {
        if (m_goFloor == null)
        {
            m_goFloor = GameObject.CreatePrimitive(PrimitiveType.Cube);
            m_goFloor.name = "Floor_DeadZone";

            Renderer rend = m_goFloor.GetComponent<Renderer>();
            if (rend != null)
            {
                rend.material.color = new Color(0.12f, 0.12f, 0.12f, 1.0f); // 세련된 매트 챠콜 블랙
                rend.enabled = true; // 데드존 가시성 활성화
            }

            Collider col = m_goFloor.GetComponent<Collider>();
            if (col != null)
            {
                col.isTrigger = true;
            }
        }

        float initialFloorY = -10.0f;
        if (CameraManager.Instance != null && CameraManager.Instance.mainCamera != null)
        {
            initialFloorY = -CameraManager.Instance.mainCamera.orthographicSize - 12.0f;
        }

        // 가로 X scale 150f, 세로 Y 두께 100f, 앞뒤 Z 두께 10f 로 웅장하게 확대
        m_goFloor.transform.localScale = new Vector3(150f, 100f, 10f);

        // 큐브 상단면이 initialFloorY + 1.0f 에 놓이도록 Y 중심 위치를 아래로 49.0f 하향 조정

        m_goFloor.transform.position = new Vector3(0f, initialFloorY - 49.0f, 0f);
    }

    private eMapProp _GetMapProp(Color color)
    {
        if (_isEqual(Color.black, color))
            return eMapProp.eMapProp_Normal;
        else if (_isEqual(Color.green, color))
            return eMapProp.eMapProp_Coin;
        else if (_isEqual(Color.gray, color))
            return eMapProp.eMapProp_Break;
        else if (_isEqual(Color.red, color))
            return eMapProp.eMapProp_MoveX;
        else if (_isEqual(Color.blue, color))
            return eMapProp.eMapProp_MoveY;
        else
            return eMapProp.eMapProp_None;
    }

    private bool _isEqual(Color color1, Color color2)
    {
        if (_isEqual(color1.r, color2.r) && _isEqual(color1.g, color2.g) && _isEqual(color1.b, color2.b))
            return true;
        return false;
    }

    private bool _isEqual(float f1, float f2)
    {
        if (f1 == f2 || ((f1 + m_fLerp > f2) && (f1 - m_fLerp < f2)))
            return true;
        return false;
    }

    public Material GetSharedMaterial(int index)
    {
        if (m_sharedMaterials == null || m_sharedMaterials.Length == 0) return null;
        if (index >= 0 && index < m_sharedMaterials.Length && m_sharedMaterials[index] != null)
        {
            return m_sharedMaterials[index];
        }
        return m_sharedMaterials[0];
    }

    private GameObject _CreateCube(int x, int y, eMapProp prop, float scrollWidth, bool isBoundaryWall = false, bool isFlying = false, float customWorldX = 0f)
    {
        Vector3 vPos = Vector3.zero;
        GameObject go = GameObject.Instantiate(goCubeSrc) as GameObject;
        if (isFlying)
        {
            vPos.x = customWorldX;
        }
        else
        {
            vPos.x = m_fCubeSize * x - m_fCubeSize;
        }
        vPos.y = m_fCubeSize * y - m_fCubeSize;
        go.transform.position = vPos;
        go.transform.parent = this.transform;

        switch (prop)
        {
            case eMapProp.eMapProp_None: break;
            case eMapProp.eMapProp_Coin: break;

            case eMapProp.eMapProp_Normal:
                go.AddComponent<CubeNormal>();
                break;

            case eMapProp.eMapProp_Break:
                go.AddComponent<CubeBreak>();
                break;

            case eMapProp.eMapProp_MoveX:
                go.AddComponent<CubeMoveX>();
                break;

            case eMapProp.eMapProp_MoveY:
                go.AddComponent<CubeMoveY>();
                break;

            case eMapProp.eMapProp_JumpZero:
                if (isBoundaryWall)
                {
                    go.AddComponent<CubeDeadly>();
                }
                else if (isFlying)
                {
                    go.AddComponent<CubeFlyingJumpZero>();
                }
                else
                {
                    CubeJumpZero comp = go.AddComponent<CubeJumpZero>();
                    comp.isBoundaryWall = false;
                }
                break;

            case eMapProp.eMapProp_Blink:
                go.AddComponent<CubeBlink>();
                break;
        }

        if (m_enableInfiniteScroll && !isFlying)
        {
            InfiniteScrollObject scroll = go.AddComponent<InfiniteScrollObject>();
            Transform playerT = CameraManager.Instance != null ? CameraManager.Instance.Target : null;
            scroll.Init(vPos.x, scrollWidth, playerT);
            m_scrollObjects.Add(scroll);
        }

        return go;
    }

    private GameObject _CreateCoin(int x, int y, float scrollWidth)
    {
        Vector3 vPos = Vector3.zero;
        GameObject go = GameObject.Instantiate(goCoinSrc) as GameObject;
        vPos.x = m_fCubeSize * x - m_fCubeSize;
        vPos.y = m_fCubeSize * y - m_fCubeSize;
        go.transform.position = vPos;
        go.transform.parent = this.transform;

        MeshFilter mf = go.GetComponentInChildren<MeshFilter>();
        if (mf != null)
        {
            GameObject tempCube = GameObject.CreatePrimitive(PrimitiveType.Cube);
            MeshFilter tempMf = tempCube.GetComponent<MeshFilter>();
            if (tempMf != null)
            {
                mf.sharedMesh = tempMf.sharedMesh;
            }
            Util.MyDestroy(tempCube);
        }

        // Coin 컴포넌트 부착 및 등급(grade 1~5) 무작위 부여
        Coin coin = go.AddComponent<Coin>();
        coin.grade = Random.Range(1, 6);

        if (m_enableInfiniteScroll)
        {
            InfiniteScrollObject scroll = go.AddComponent<InfiniteScrollObject>();
            Transform playerT = CameraManager.Instance != null ? CameraManager.Instance.Target : null;
            scroll.Init(vPos.x, scrollWidth, playerT);
            m_scrollObjects.Add(scroll);
        }

        return go;
    }

    public void UnLoadCubeMap()
    {
        // cube
        foreach (GameObject go in m_listCube)
            Util.MyDestroy(go);

        m_listCube.Clear();

        // coin
        foreach (GameObject go in m_listCoin)
            Util.MyDestroy(go);

        m_listCoin.Clear();

        m_scrollObjects.Clear();

        if (m_cleanupCoroutine != null)
        {
            StopCoroutine(m_cleanupCoroutine);
            m_cleanupCoroutine = null;
        }

        if (m_goFloor != null)
        {
            Util.MyDestroy(m_goFloor);
            m_goFloor = null;
        }

        ClearBackground();
    }

    public void RemoveCoin(GameObject go)
    {
        Coin coinComp = go.GetComponent<Coin>();
        int gradeVal = coinComp != null ? coinComp.grade : 1;

        // 기본 이펙트 생성
        GameObject goEff = GameObject.Instantiate(goCoinEffSrc) as GameObject;
        goEff.transform.position = go.transform.position;

        // 4등급일 때 추가 1개(총 2개), 5등급일 때 추가 2개(총 3개) 이펙트 소환
        int extraEffects = 0;
        if (gradeVal == 4) extraEffects = 1;
        else if (gradeVal == 5) extraEffects = 2;

        for (int i = 0; i < extraEffects; i++)
        {
            GameObject extraEff = GameObject.Instantiate(goCoinEffSrc) as GameObject;
            // 시각적 입체 분산을 위해 미세 랜덤 오프셋 적용
            Vector3 offset = new Vector3(Random.Range(-0.4f, 0.4f), Random.Range(-0.4f, 0.4f), 0f);
            extraEff.transform.position = go.transform.position + offset;
        }

        AudioManager.Instance.Play("Sound/coin_eff", 0.3f);

        m_listCoin.Remove(go);

        InfiniteScrollObject scroll = go.GetComponent<InfiniteScrollObject>();
        if (scroll != null) m_scrollObjects.Remove(scroll);
        Util.MyDestroy(go);

        m_nTotalCoinsCollected++;

        int addJumpAmount = 3;
        if (gradeVal == 1) addJumpAmount = 1;
        else if (gradeVal == 2) addJumpAmount = 2;
        else if (gradeVal == 3) addJumpAmount = 3;
        else if (gradeVal == 4) addJumpAmount = 6;
        else if (gradeVal == 5) addJumpAmount = 10;

        if (CameraManager.Instance.Target != null)
        {
            Player player = CameraManager.Instance.Target.GetComponent<Player>();
            if (player != null)
            {
                player.AddJumps(addJumpAmount);
            }
        }

        if (UI_Play.Instance != null)
        {
            Player player = CameraManager.Instance.Target != null ? CameraManager.Instance.Target.GetComponent<Player>() : null;
            if (player != null)
            {
                UI_Play.Instance.SetPlayStats(m_nTotalCoinsCollected, player.JumpCount);
            }
        }
    }

    public void RemoveCube(GameObject go)
    {
        GameObject goEff = GameObject.Instantiate(goCubeEffSrc) as GameObject;
        goEff.transform.position = go.transform.position;

        AudioManager.Instance.Play("Sound/cube_break");

        InfiniteScrollObject scroll = go.GetComponent<InfiniteScrollObject>();
        if (scroll != null) m_scrollObjects.Remove(scroll);
        Util.MyDestroy(go);
    }

    private IEnumerator _LevelClear()
    {
        Debug.Log("[MapManager Debug] Starting _LevelClear sequence.");
        if (MainManager.Instance != null)
        {
            MainManager.Instance.eCurState = eGameState.eGameState_Result;
        }

        yield return new WaitForSeconds(0.5f);

        // Result 씬으로 넘어가기 전 Play 정보를 정적 필드에 안전하게 백업
        MainManager.lastTotalCoins = MapManager.Instance.TotalCoinsCollected;
        if (UI_Play.Instance != null)
        {
            MainManager.lastGameTime = UI_Play.Instance.nGameTime;
            // 사망하여 게임오버가 되었으므로 클리어 타입은 무조건 None(실패)으로 강제 설정합니다.
            MainManager.lastClearType = UI_Play.eLevelClearType.eLevelClearType_None;
            MainManager.lastMaxHeight = UI_Play.Instance.MaxHeightThisRun;

            int allTimeBest = 0;
            int levelIdx = MainManager.nCurLevelStatic - 1;
            if (MainManager.Instance != null && MainManager.Instance.nBestHeight != null && levelIdx >= 0 && levelIdx < MainManager.Instance.nBestHeight.Length)
            {
                allTimeBest = MainManager.Instance.nBestHeight[levelIdx];
            }
            MainManager.lastBestHeight = allTimeBest;
        }

        if (MainManager.Instance != null)
        {
            Debug.Log("[MapManager Debug] Transitioning to Result scene.");
            MainManager.Instance.TransitionToScene("Result");
        }
        else
        {
            // UI가 없을 경우 테스트를 위해 자동으로 다음 레벨 진행
            AudioManager.Instance.Play("Sound/clear");
            yield return new WaitForSeconds(1.0f);

            // MainManager가 존재하지 않는 특수한 경우엔 동작하지 않음
        }
    }

    public void TriggerGameOver()
    {
        Debug.Log($"[MapManager Debug] TriggerGameOver called. MainManager Instance: {(MainManager.Instance != null ? "Not Null" : "Null")}, CurState: {(MainManager.Instance != null ? MainManager.Instance.eCurState.ToString() : "N/A")}");
        if (MainManager.Instance != null && MainManager.Instance.eCurState == eGameState.eGameState_Play)
        {
            StartCoroutine(_LevelClear());
        }
    }

    private void GenerateRowsUpTo(int targetY)
    {
        if (m_fCubeSize < 0.1f)
        {
            m_fCubeSize = 1.0f;
        }

        while (m_highestGeneratedY <= targetY)
        {
            int y = m_highestGeneratedY;
            DifficultyTier tier = GetTierForHeight(y);

            // 해당 난이도 구간에 타일맵 세그먼트들이 등록되어 있다면 조립식 세그먼트 생성 작동
            if (tier.segmentPrefabs != null && tier.segmentPrefabs.Count > 0)
            {
                SpawnSegment(tier);
            }
            else
            {
                // 프리팹이 등록되지 않았다면 기존 완전 랜덤 1줄 스폰 구동
                SpawnRandomRow(y, tier);
                m_highestGeneratedY++;
            }
        }
    }

    private void SpawnSegment(DifficultyTier tier)
    {
        // 1. 등록된 프리팹 리스트 중 무작위 추첨
        GameObject prefab = tier.segmentPrefabs[Random.Range(0, tier.segmentPrefabs.Count)];
        if (prefab == null)
        {
            SpawnRandomRow(m_highestGeneratedY, tier);
            m_highestGeneratedY++;
            return;
        }

        GameObject inst = Instantiate(prefab);
        Tilemap[] tilemaps = inst.GetComponentsInChildren<Tilemap>();
        if (tilemaps == null || tilemaps.Length == 0)
        {
            Util.MyDestroy(inst);
            SpawnRandomRow(m_highestGeneratedY, tier);
            m_highestGeneratedY++;
            return;
        }

        // 2. 프리팹 내부 타일들의 실제 Y고도 한계 범위 분석 (원점 보정용)
        int minY = int.MaxValue;
        int maxY = int.MinValue;
        foreach (Tilemap tm in tilemaps)
        {
            BoundsInt bounds = tm.cellBounds;
            foreach (var pos in bounds.allPositionsWithin)
            {
                if (tm.HasTile(pos))
                {
                    if (pos.y < minY) minY = pos.y;
                    if (pos.y > maxY) maxY = pos.y;
                }
            }
        }

        if (minY == int.MaxValue)
        {
            Util.MyDestroy(inst);
            SpawnRandomRow(m_highestGeneratedY, tier);
            m_highestGeneratedY++;
            return;
        }

        int segmentHeight = (maxY - minY) + 1;
        int startY = m_highestGeneratedY;
        float rowScrollWidth = (tier.maxSpawnX - tier.minSpawnX) * m_fCubeSize;

        // 3. 복제 타일맵에서 3D 큐브/코인 번역 소환 및 양끝 벽 소환
        for (int yOffset = 0; yOffset < segmentHeight; yOffset++)
        {
            int currentY = startY + yOffset;

            // 가로폭을 고려하여 양쪽 끝에 무조건 고정 경계 벽 기둥 스폰
            m_listCube.Add(_CreateCube(tier.minSpawnX + 1, currentY, eMapProp.eMapProp_JumpZero, rowScrollWidth, true));
            m_listCube.Add(_CreateCube(tier.maxSpawnX + 1, currentY, eMapProp.eMapProp_JumpZero, rowScrollWidth, true));

            // 타일맵들을 훑으며 해당 yOffset 고도의 타일 매핑
            foreach (Tilemap tm in tilemaps)
            {
                BoundsInt bounds = tm.cellBounds;
                int sourceTileY = minY + yOffset;

                for (int tx = bounds.xMin; tx <= bounds.xMax; tx++)
                {
                    Vector3Int tilePos = new Vector3Int(tx, sourceTileY, 0);
                    if (tm.HasTile(tilePos))
                    {
                        TileBase tile = tm.GetTile(tilePos);
                        if (tile != null)
                        {
                            string tileName = tile.name.ToLower();
                            eMapProp prop = eMapProp.eMapProp_None;
                            bool isCoin = false;

                            if (tileName.Contains("normal")) prop = eMapProp.eMapProp_Normal;
                            else if (tileName.Contains("break")) prop = eMapProp.eMapProp_Break;
                            else if (tileName.Contains("movex")) prop = eMapProp.eMapProp_MoveX;
                            else if (tileName.Contains("movey")) prop = eMapProp.eMapProp_MoveY;
                            else if (tileName.Contains("jumpzero")) prop = eMapProp.eMapProp_JumpZero;
                            else if (tileName.Contains("blink")) prop = eMapProp.eMapProp_Blink;
                            else if (tileName.Contains("coin")) isCoin = true;

                            if (isCoin)
                            {
                                m_listCoin.Add(_CreateCoin(tx, currentY, rowScrollWidth));
                            }
                            else if (prop != eMapProp.eMapProp_None)
                            {
                                m_listCube.Add(_CreateCube(tx, currentY, prop, rowScrollWidth, false));
                            }
                        }
                    }
                }
            }
        }

        // 4. 전사 완료된 템플릿 오브젝트 즉각 메모리 해제 및 고도 정방향 갱신
        Util.MyDestroy(inst);
        m_highestGeneratedY = startY + segmentHeight;
    }

    private void SpawnRandomRow(int y, DifficultyTier tier)
    {
        int minX = tier.minSpawnX;
        int maxX = tier.maxSpawnX;
        int staticInterval = tier.staticObstacleInterval;
        int flyingInterval = tier.flyingObstacleInterval;
        int coinInt = tier.coinInterval;
        int coinSeq = tier.coinSequence;
        float minFlySpeed = tier.minFlyingSpeed;
        float maxFlySpeed = tier.maxFlyingSpeed;

        float rowScrollWidth = (maxX - minX) * m_fCubeSize;

        // 양쪽 끝 경계선(minX, maxX)에 대칭이 맞도록 기둥 배치
        m_listCube.Add(_CreateCube(minX + 1, y, eMapProp.eMapProp_JumpZero, rowScrollWidth, true));
        m_listCube.Add(_CreateCube(maxX + 1, y, eMapProp.eMapProp_JumpZero, rowScrollWidth, true));

        bool hasObstacle = false;
        int blinkInterval = tier.blinkObstacleInterval;
        int minBlinkY = tier.minBlinkHeight;

        // 난이도 티어에 설정된 조건(최소 높이 및 스폰 주기)에 따라 Blink 장애물 생성
        if (blinkInterval > 0 && y >= minBlinkY && y % blinkInterval == 0)
        {
            hasObstacle = true;
            int randomX = Random.Range(minX + 2, maxX + 1);
            m_listCube.Add(_CreateCube(randomX, y, eMapProp.eMapProp_Blink, rowScrollWidth, false));
        }

        // 설정된 주기에 따라 기존의 고정형(공중) JumpZero 장애물 생성 (Blink와 겹치지 않게)
        if (!hasObstacle && y >= 3 && staticInterval > 0 && y % staticInterval == 0)
        {
            hasObstacle = true;
            int randomX = Random.Range(minX + 2, maxX + 1);
            m_listCube.Add(_CreateCube(randomX, y, eMapProp.eMapProp_JumpZero, rowScrollWidth, false));
        }

        // 설정된 주기에 따라 화면 외곽에서 가로지르는 비행형 JumpZero 장애물 생성 (관통함)
        if (y >= 4 && flyingInterval > 0 && y % flyingInterval == 0)
        {
            hasObstacle = true;

            GameObject playerGo = CameraManager.Instance.Target != null ? CameraManager.Instance.Target.gameObject : null;
            float playerWorldX = 0f;
            if (playerGo != null)
            {
                playerWorldX = playerGo.transform.position.x;
            }

            bool flyRight = Random.value > 0.5f;
            float startWorldX = flyRight ? (playerWorldX - 35.0f) : (playerWorldX + 35.0f);
            float speedVal = Random.Range(minFlySpeed, maxFlySpeed);
            float finalSpeed = flyRight ? speedVal : -speedVal;

            GameObject flyingCube = _CreateCube(0, y, eMapProp.eMapProp_JumpZero, rowScrollWidth, false, true, startWorldX);
            CubeFlyingJumpZero jumpZeroComp = flyingCube.GetComponent<CubeFlyingJumpZero>();
            if (jumpZeroComp != null)
            {
                jumpZeroComp.InitFlying(finalSpeed, playerGo != null ? playerGo.transform : null);
            }
            m_listCube.Add(flyingCube);
        }

        // 설정된 주기에 따라 화면 외곽에서 아주 빠른 속도로 가로지르는 비행형 Fast Obstacle 장애물 생성
        if (y >= 6 && y % 15 == 0)
        {
            hasObstacle = true;

            GameObject playerGo = CameraManager.Instance.Target != null ? CameraManager.Instance.Target.gameObject : null;
            float playerWorldX = 0f;
            if (playerGo != null)
            {
                playerWorldX = playerGo.transform.position.x;
            }

            bool flyRight = Random.value > 0.5f;
            float startWorldX = flyRight ? (playerWorldX - 35.0f) : (playerWorldX + 35.0f);
            float speedMultiplier = Random.Range(1.8f, 2.5f);
            float speedVal = maxFlySpeed * speedMultiplier;
            if (speedVal < 15f) speedVal = 15f;
            float finalSpeed = flyRight ? speedVal : -speedVal;

            GameObject flyingFastCube = _CreateCube(0, y, eMapProp.eMapProp_JumpZero, rowScrollWidth, false, true, startWorldX);

            CubeFlyingJumpZero tempComp = flyingFastCube.GetComponent<CubeFlyingJumpZero>();
            if (tempComp != null)
            {
                Util.MyDestroy(tempComp);
            }

            CubeFastObstacle fastObstComp = flyingFastCube.AddComponent<CubeFastObstacle>();
            if (fastObstComp != null)
            {
                fastObstComp.InitFlying(finalSpeed, playerGo != null ? playerGo.transform : null);
            }

            Renderer rendFast = flyingFastCube.GetComponent<Renderer>();
            if (rendFast != null)
            {
                rendFast.sharedMaterial = GetSharedMaterial(9);
            }

            m_listCube.Add(flyingFastCube);
        }

        // 보석(Coin) 배치 (설정된 주기이며 장애물이 생성되지 않는 칸일 때만 스폰)
        if (y >= 3 && !hasObstacle)
        {
            if (coinInt > 0)
            {
                if (y % coinInt < coinSeq)
                {
                    int randomX = Random.Range(minX + 2, maxX + 1);
                    m_listCoin.Add(_CreateCoin(randomX, y, rowScrollWidth));
                }
            }
            else
            {
                int spawnCount = 2 - coinInt;
                List<int> availableX = new List<int>();
                for (int sx = minX + 2; sx <= maxX + 1; sx++)
                {
                    availableX.Add(sx);
                }

                for (int i = 0; i < spawnCount && availableX.Count > 0; i++)
                {
                    int randomIndex = Random.Range(0, availableX.Count);
                    int randomX = availableX[randomIndex];
                    availableX.RemoveAt(randomIndex);

                    m_listCoin.Add(_CreateCoin(randomX, y, rowScrollWidth));
                }
            }
        }
    }

    private IEnumerator CleanupRoutine_CR()
    {
        int cubeIndex = 0;
        int coinIndex = 0;
        WaitForSeconds wait = new WaitForSeconds(0.1f);

        while (true)
        {
            if (MainManager.Instance == null || MainManager.Instance.eCurState != eGameState.eGameState_Play)
            {
                yield return wait;
                continue;
            }

            GameObject playerGo = CameraManager.Instance != null && CameraManager.Instance.Target != null ? CameraManager.Instance.Target.gameObject : null;
            if (playerGo == null)
            {
                yield return wait;
                continue;
            }

            float playerY = playerGo.transform.position.y / (m_fCubeSize > 0f ? m_fCubeSize : 1.0f);
            int limitY = Mathf.FloorToInt(playerY) - m_cleanupBehindRange;

            // 1. 큐브 순차 검사 (0.1초당 최대 1개)
            if (m_listCube.Count > 0)
            {
                if (cubeIndex >= m_listCube.Count)
                {
                    cubeIndex = m_listCube.Count - 1;
                }

                GameObject go = m_listCube[cubeIndex];
                if (go != null)
                {
                    int gridY = Mathf.RoundToInt((go.transform.position.y + m_fCubeSize) / (m_fCubeSize > 0f ? m_fCubeSize : 1.0f));
                    if (gridY < limitY)
                    {
                        m_listCube.RemoveAt(cubeIndex);
                        InfiniteScrollObject scroll = go.GetComponent<InfiniteScrollObject>();
                        if (scroll != null) m_scrollObjects.Remove(scroll);
                        Util.MyDestroy(go);
                    }
                    else
                    {
                        cubeIndex--;
                    }
                }
                else
                {
                    m_listCube.RemoveAt(cubeIndex);
                }

                if (cubeIndex < 0)
                {
                    cubeIndex = m_listCube.Count - 1;
                }
            }

            // 2. 코인 순차 검사 (0.1초당 최대 1개)
            if (m_listCoin.Count > 0)
            {
                if (coinIndex >= m_listCoin.Count)
                {
                    coinIndex = m_listCoin.Count - 1;
                }

                GameObject go = m_listCoin[coinIndex];
                if (go != null)
                {
                    int gridY = Mathf.RoundToInt((go.transform.position.y + m_fCubeSize) / (m_fCubeSize > 0f ? m_fCubeSize : 1.0f));
                    if (gridY < limitY)
                    {
                        m_listCoin.RemoveAt(coinIndex);

                        InfiniteScrollObject scroll = go.GetComponent<InfiniteScrollObject>();
                        if (scroll != null) m_scrollObjects.Remove(scroll);
                        Util.MyDestroy(go);
                    }
                    else
                    {
                        coinIndex--;
                    }
                }
                else
                {
                    m_listCoin.RemoveAt(coinIndex);
                }

                if (coinIndex < 0)
                {
                    coinIndex = m_listCoin.Count - 1;
                }
            }

            yield return wait;
        }
    }

    private void CreateBackground()
    {
        ClearBackground();

        m_goBackgroundContainer = new GameObject("BackgroundContainer");
        m_goBackgroundContainer.transform.parent = this.transform;

        Transform cameraT = CameraManager.Instance != null && CameraManager.Instance.mainCamera != null ? CameraManager.Instance.mainCamera.transform : null;
        if (cameraT == null) return;

        // Sprites/Default 셰이더를 사용하여 직교 카메라 배경 렌더링 시 투명도 및 드로우콜 배칭 완벽 지원
        Shader bgShader = Shader.Find("Sprites/Default");
        if (bgShader == null)
        {
            bgShader = Shader.Find("UI/Default");
        }

        if (bgShader != null)
        {
            m_farSkyMaterial = new Material(bgShader);
            m_farSkyMaterial.color = new Color(0.04f, 0.04f, 0.12f, 1f); // 원경 짙은 남색
            m_farSkyMaterial.enableInstancing = true; // GPU 인스턴싱 활성화

            m_midCubeMaterial = new Material(bgShader);
            m_midCubeMaterial.color = new Color(0.12f, 0.1f, 0.22f, 0.45f); // 중경 반투명 보라색 (알파 0.45)
            m_midCubeMaterial.enableInstancing = true; // GPU 인스턴싱 활성화
        }

        // 1. 원경 Quad (Far Background Sky)
        GameObject goFar = GameObject.CreatePrimitive(PrimitiveType.Quad);
        goFar.name = "Far_Background_Quad";
        goFar.transform.parent = cameraT;
        goFar.transform.localPosition = new Vector3(0f, 0f, 100f); // Z=100 (중경 큐브 가려짐 방지를 위해 원경을 뒤로 대폭 배치)
        goFar.transform.localScale = new Vector3(600f, 600f, 1f); // 거리에 맞춘 크기 확대

        Collider colFar = goFar.GetComponent<Collider>();
        if (colFar != null) Util.MyDestroy(colFar);

        Renderer rendFar = goFar.GetComponent<Renderer>();
        if (rendFar != null && m_farSkyMaterial != null)
        {
            rendFar.sharedMaterial = m_farSkyMaterial;
        }

        // 2. 중경 거대 큐브군 (Mid Background Cubes)
        int cols = 2; // 가로 2칸
        int rows = 5; // 세로 5칸
        int midCubeCount = cols * rows; // 총 10개로 축소하여 오버드로우 렉 극적 개선
        float bgLoopHeight = 120f; // 세로 루프 주기

        float totalWidth = m_scrollWidth * 1.2f;
        float gridW = totalWidth / cols;
        float gridH = bgLoopHeight / rows;

        for (int r = 0; r < rows; r++)
        {
            for (int c = 0; c < cols; c++)
            {
                int index = r * cols + c;
                GameObject go = GameObject.CreatePrimitive(PrimitiveType.Quad); // 3D Cube 대신 2D Quad로 전환하여 프레임 대폭 상승
                go.name = "Background_MidCube_" + index;
                go.transform.parent = m_goBackgroundContainer.transform;

                Collider col = go.GetComponent<Collider>();
                if (col != null) Util.MyDestroy(col);

                // 기존 스케일 대비 1.2배 추가 확대적용 (12f ~ 31f)
                float scale = Random.Range(12f, 31f);
                go.transform.localScale = Vector3.one * scale;

                // 그리드 구역 안에서 약간의 랜덤 오프셋을 섞어 완전 겹침 방지 및 골고루 분포 유도
                float gridCenterX = -totalWidth * 0.5f + (c + 0.5f) * gridW;
                float gridCenterY = -30f + (r + 0.5f) * gridH;

                float posX = gridCenterX + Random.Range(-gridW * 0.25f, gridW * 0.25f);
                float posY = gridCenterY + Random.Range(-gridH * 0.25f, gridH * 0.25f);
                // 스폰 월드 Z 범위를 15f ~ 35f로 앞으로 당겨 원경 쿼드보다 앞에 렌더링되게 확실히 보장
                float posZ = Random.Range(15f, 35f);
                go.transform.position = new Vector3(posX, posY, posZ);

                Renderer rend = go.GetComponent<Renderer>();
                if (rend != null && m_midCubeMaterial != null)
                {
                    rend.sharedMaterial = m_midCubeMaterial;
                }

                ParallaxScroll parallax = go.AddComponent<ParallaxScroll>();
                parallax.Init(cameraT, 0.7f, 0.85f, m_scrollWidth, bgLoopHeight);
                m_parallaxObjects.Add(parallax);
            }
        }


    }

    private void ClearBackground()
    {
        m_parallaxObjects.Clear();

        // 1. 기존 컨테이너 변수 즉시 파괴 (지연 파괴 중복 방지)
        if (m_goBackgroundContainer != null)
        {
            DestroyImmediate(m_goBackgroundContainer);
            m_goBackgroundContainer = null;
        }

        // 2. 부모 자식 트리 구조 하위의 BackgroundContainer 추가적 완벽 검색 소거
        Transform childContainer = this.transform.Find("BackgroundContainer");
        if (childContainer != null)
        {
            DestroyImmediate(childContainer.gameObject);
        }

        if (CameraManager.Instance != null && CameraManager.Instance.mainCamera != null)
        {
            Transform farBg = CameraManager.Instance.mainCamera.transform.Find("Far_Background_Quad");
            if (farBg != null)
            {
                Util.MyDestroy(farBg.gameObject);
            }
        }

        // 동적 생성된 공유 머티리얼 메모리 안전하게 제거
        if (m_farSkyMaterial != null)
        {
            Util.MyDestroy(m_farSkyMaterial);
            m_farSkyMaterial = null;
        }
        if (m_midCubeMaterial != null)
        {
            Util.MyDestroy(m_midCubeMaterial);
            m_midCubeMaterial = null;
        }
    }
}
