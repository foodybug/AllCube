using System.Collections;
using System.Collections.Generic;
using UnityEngine;

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
        public int coinInterval;
        public float minFlyingSpeed;
        public float maxFlyingSpeed;
        public int initialJumps;
    }

    [Header("Stage Generation Settings")]
    [SerializeField] private int m_minSpawnX = -30;
    [SerializeField] private int m_maxSpawnX = 30;
    [SerializeField] private int m_generationAheadRange = 30;
    [SerializeField] private int m_cleanupBehindRange = 25;

    public int MinSpawnX { get { return m_minSpawnX; } }
    public int MaxSpawnX { get { return m_maxSpawnX; } }

    [Header("Level Design Settings")]
    [SerializeField]
    private List<DifficultyTier> m_difficultyTier = new List<DifficultyTier>()
    {
        new DifficultyTier { minHeight = 0, minSpawnX = -30, maxSpawnX = 30, staticObstacleInterval = 10, flyingObstacleInterval = 15, coinInterval = 3, minFlyingSpeed = 4f, maxFlyingSpeed = 6f, initialJumps = 10 },
        new DifficultyTier { minHeight = 20, minSpawnX = -25, maxSpawnX = 25, staticObstacleInterval = 8, flyingObstacleInterval = 12, coinInterval = 4, minFlyingSpeed = 6f, maxFlyingSpeed = 8f, initialJumps = 8 },
        new DifficultyTier { minHeight = 40, minSpawnX = -20, maxSpawnX = 20, staticObstacleInterval = 6, flyingObstacleInterval = 9, coinInterval = 5, minFlyingSpeed = 8f, maxFlyingSpeed = 11f, initialJumps = 7 },
        new DifficultyTier { minHeight = 60, minSpawnX = -15, maxSpawnX = 15, staticObstacleInterval = 5, flyingObstacleInterval = 7, coinInterval = 6, minFlyingSpeed = 10f, maxFlyingSpeed = 14f, initialJumps = 5 },
        new DifficultyTier { minHeight = 80, minSpawnX = -10, maxSpawnX = 10, staticObstacleInterval = 4, flyingObstacleInterval = 5, coinInterval = 7, minFlyingSpeed = 12f, maxFlyingSpeed = 18f, initialJumps = 4 }
    };

    // 현재 레벨에 적용되는 런타임 세팅 값들
    private int m_staticObstacleInterval = 5;
    private int m_flyingObstacleInterval = 8;
    private int m_coinInterval = 3;
    private float m_minFlyingSpeed = 6.0f;
    private float m_maxFlyingSpeed = 10.0f;
    private int m_initialJumps = 10;

    public int InitialJumps { get { return m_initialJumps; } }

    public DifficultyTier GetTierForHeight(int y)
    {
        DifficultyTier activeTier = new DifficultyTier();
        bool found = false;
        int maxMinHeight = -1;

        foreach (var tier in m_difficultyTier)
        {
            if (y >= tier.minHeight && tier.minHeight > maxMinHeight)
            {
                activeTier = tier;
                maxMinHeight = tier.minHeight;
                found = true;
            }
        }

        if (found)
        {
            return activeTier;
        }

        // Fallback (아무것도 찾지 못했을 때 기본 세팅값 반환)
        DifficultyTier fallback = new DifficultyTier();
        fallback.minHeight = 0;
        fallback.minSpawnX = -30;
        fallback.maxSpawnX = 30;
        fallback.staticObstacleInterval = 5;
        fallback.flyingObstacleInterval = 8;
        fallback.coinInterval = 3;
        fallback.minFlyingSpeed = 6.0f;
        fallback.maxFlyingSpeed = 10.0f;
        fallback.initialJumps = 10;
        return fallback;
    }

    [Header("Infinite Scroll Settings")]
    [SerializeField] private bool m_enableInfiniteScroll = true;
    [SerializeField] private float m_scrollWidth = 60.0f;



    public int CoinCount { get { return m_listCoin.Count; } }

    void Awake()
    {
        m_instance = this;

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

                    // Place it sufficiently below the player, slightly inside the bottom edge of the camera so it remains visible
                    float targetFloorWorldY = playerWorldY - cameraHeight + 2.0f;

                    // Only move UP, never down
                    if (targetFloorWorldY > currentFloorY)
                    {
                        Vector3 floorPos = m_goFloor.transform.position;
                        floorPos.x = playerGo.transform.position.x; // Keep it centered horizontally with the player
                        floorPos.y = targetFloorWorldY;
                        m_goFloor.transform.position = floorPos;
                    }

                    // Ensure it is always visible
                    Renderer floorRenderer = m_goFloor.GetComponent<Renderer>();
                    if (floorRenderer != null && !floorRenderer.enabled)
                    {
                        floorRenderer.enabled = true;
                    }
                }

                // 1. Generate up to playerY + m_generationAheadRange rows ahead
                int targetY = Mathf.CeilToInt(playerY) + m_generationAheadRange;
                if (targetY > m_highestGeneratedY)
                {
                    GenerateRowsUpTo(targetY);
                }

                // 2. Clean up old blocks that are far below the player Y - m_cleanupBehindRange
                CleanupBlocksBelow(Mathf.FloorToInt(playerY) - m_cleanupBehindRange);

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

                            Renderer r = coin.GetComponentInChildren<Renderer>();
                            if (r != null)
                            {
                                r.material.color = rainbowColor;
                            }
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

        m_minSpawnX = config.minSpawnX;
        m_maxSpawnX = config.maxSpawnX;
        m_staticObstacleInterval = config.staticObstacleInterval;
        m_flyingObstacleInterval = config.flyingObstacleInterval;
        m_coinInterval = config.coinInterval;
        m_minFlyingSpeed = config.minFlyingSpeed;
        m_maxFlyingSpeed = config.maxFlyingSpeed;
        m_initialJumps = config.initialJumps;

        // 무한 스크롤 반복 가로폭을 기둥 사이의 정확한 거리로 동적 계산
        m_scrollWidth = (m_maxSpawnX - m_minSpawnX) * m_fCubeSize;
    }

    public void LoadCubeMap(int nStage)
    {
        ApplyLevelConfig(nStage); // 레벨 디자인에 맞는 세팅 적용

        UnLoadCubeMap();
        m_highestGeneratedY = 0;
        m_nTotalCoinsCollected = 0;

        SpawnFloor();

        // Spawn starting platform and walls
        GenerateRowsUpTo(m_generationAheadRange);
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
                rend.material.color = Color.gray;
                rend.enabled = true; // Always visible
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
            initialFloorY = -CameraManager.Instance.mainCamera.orthographicSize + 2.0f;
        }

        m_goFloor.transform.position = new Vector3(0f, initialFloorY, 0f);
        m_goFloor.transform.localScale = new Vector3(100f, 2f, 2f); // X 100, Y 두께 2, Z 두께 2 (프러스텀 클리핑 방지)
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
                go.GetComponent<Renderer>().material.mainTexture = texCube[(int)(Random.Range(1, 5))];
                go.GetComponent<Renderer>().material.color = Color.white;
                break;

            case eMapProp.eMapProp_Break:
                go.GetComponent<Renderer>().material.mainTexture = texCube[0];
                go.GetComponent<Renderer>().material.color = Color.white;
                go.AddComponent<CubeBreak>();
                CubeBreak cubeBreak = go.GetComponent<CubeBreak>();
                cubeBreak.goCube = go;
                break;

            case eMapProp.eMapProp_MoveX:
                go.GetComponent<Renderer>().material.mainTexture = texCube[5];
                go.GetComponent<Renderer>().material.color = Color.white;
                Rigidbody rbX = go.GetComponent<Rigidbody>();
                if (rbX == null) rbX = go.AddComponent<Rigidbody>();
                rbX.isKinematic = true;
                rbX.useGravity = false;
                go.AddComponent<CubeMoveX>();
                CubeMoveX cubeMoveX = go.GetComponent<CubeMoveX>();
                cubeMoveX.Init(go);
                break;

            case eMapProp.eMapProp_MoveY:
                go.GetComponent<Renderer>().material.mainTexture = texCube[5];
                go.GetComponent<Renderer>().material.color = Color.white;
                Rigidbody rbY = go.GetComponent<Rigidbody>();
                if (rbY == null) rbY = go.AddComponent<Rigidbody>();
                rbY.isKinematic = true;
                rbY.useGravity = false;
                go.AddComponent<CubeMoveY>();
                CubeMoveY cubeMoveY = go.GetComponent<CubeMoveY>();
                cubeMoveY.Init(go);
                break;

            case eMapProp.eMapProp_JumpZero:
                Renderer rend = go.GetComponent<Renderer>();
                if (rend != null)
                {
                    // 0번(부서지는 블록) 대신 5번 텍스처를 장애물의 베이스로 사용하여 구분감을 줍니다.
                    if (texCube.Length > 5 && texCube[5] != null)
                    {
                        rend.material.mainTexture = texCube[5];
                    }
                    // 기둥(isBoundaryWall)은 흰색, 일반 공중 장애물은 빨간색으로 매핑
                    rend.material.color = isBoundaryWall ? Color.white : Color.red;
                }
                Collider colZero = go.GetComponent<Collider>();
                if (colZero != null)
                {
                    colZero.isTrigger = !isBoundaryWall;
                }
                Rigidbody rbZero = go.GetComponent<Rigidbody>();
                if (rbZero != null)
                {
                    Util.MyDestroy(rbZero);
                }
                if (isFlying && !isBoundaryWall)
                {
                    go.AddComponent<CubeFlyingJumpZero>();
                }
                else
                {
                    CubeJumpZero comp = go.AddComponent<CubeJumpZero>();
                    comp.isBoundaryWall = isBoundaryWall;
                }
                break;

            case eMapProp.eMapProp_Blink:
                Renderer rendBlink = go.GetComponent<Renderer>();
                if (rendBlink != null)
                {
                    // 0번 대신 5번 텍스처를 사용하고 청록색(Cyan) 틴트를 입혀 타이밍 장애물임을 명확하게 합니다.
                    if (texCube.Length > 5 && texCube[5] != null)
                    {
                        rendBlink.material.mainTexture = texCube[5];
                    }
                    rendBlink.material.color = isBoundaryWall ? Color.white : new Color(0.3f, 0.8f, 1.0f, 1.0f); // 청록색
                }
                Collider colBlink = go.GetComponent<Collider>();
                if (colBlink != null)
                {
                    colBlink.isTrigger = !isBoundaryWall;
                }
                Rigidbody rbBlink = go.GetComponent<Rigidbody>();
                if (rbBlink != null)
                {
                    Util.MyDestroy(rbBlink);
                }
                go.AddComponent<CubeBlink>();
                break;
        }

        if (m_enableInfiniteScroll && !isFlying)
        {
            InfiniteScrollObject scroll = go.AddComponent<InfiniteScrollObject>();
            Transform playerT = CameraManager.Instance != null ? CameraManager.Instance.Target : null;
            scroll.Init(vPos.x, scrollWidth, playerT);
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

        go.transform.localScale = new Vector3(0.5f, 0.5f, 0.5f);

        if (m_enableInfiniteScroll)
        {
            InfiniteScrollObject scroll = go.AddComponent<InfiniteScrollObject>();
            Transform playerT = CameraManager.Instance != null ? CameraManager.Instance.Target : null;
            scroll.Init(vPos.x, scrollWidth, playerT);
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

        if (m_goFloor != null)
        {
            Util.MyDestroy(m_goFloor);
            m_goFloor = null;
        }
    }

    public void RemoveCoin(GameObject go)
    {
        GameObject goEff = GameObject.Instantiate(goCoinEffSrc) as GameObject;
        goEff.transform.position = go.transform.position;

        AudioManager.Instance.Play("Sound/coin_eff", 0.3f);

        m_listCoin.Remove(go);
        Util.MyDestroy(go);

        m_nTotalCoinsCollected++;

        if (CameraManager.Instance.Target != null)
        {
            Player player = CameraManager.Instance.Target.GetComponent<Player>();
            if (player != null)
            {
                player.AddJumps(3);
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
            MainManager.lastClearType = UI_Play.Instance.eClearType;
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

        for (int y = m_highestGeneratedY; y <= targetY; y++)
        {
            // 현재 Row 'y'에 알맞는 난이도 티어 정보를 가져옵니다.
            DifficultyTier tier = GetTierForHeight(y);
            int minX = tier.minSpawnX;
            int maxX = tier.maxSpawnX;
            int staticInterval = tier.staticObstacleInterval;
            int flyingInterval = tier.flyingObstacleInterval;
            int coinInt = tier.coinInterval;
            float minFlySpeed = tier.minFlyingSpeed;
            float maxFlySpeed = tier.maxFlyingSpeed;

            // 해당 Row의 기둥 간 거리에 따른 무한 스크롤 폭 계산
            float rowScrollWidth = (maxX - minX) * m_fCubeSize;

            // 양쪽 끝 경계선(minX, maxX)에 대칭이 맞도록 기둥 배치
            m_listCube.Add(_CreateCube(minX + 1, y, eMapProp.eMapProp_JumpZero, rowScrollWidth, true));
            m_listCube.Add(_CreateCube(maxX + 1, y, eMapProp.eMapProp_JumpZero, rowScrollWidth, true));

            bool hasObstacle = false;
            // 고도 Y >= 8 이상에서 5블록 주기마다 Blink 장애물 생성 (위험과 안전 반복)
            if (y >= 8 && y % 5 == 0)
            {
                hasObstacle = true;
                int randomX = Random.Range(minX + 2, maxX + 1);
                m_listCube.Add(_CreateCube(randomX, y, eMapProp.eMapProp_Blink, rowScrollWidth, false));
            }

            // 설정된 주기에 따라 기존의 고정형(공중) JumpZero 장애물 생성 (Blink와 겹치지 않게)
            if (!hasObstacle && y >= 3 && staticInterval > 0 && y % staticInterval == 0)
            {
                hasObstacle = true;
                // 실제 월드 위치로 대칭 범위 내에서 랜덤 지정
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

                // 무작위로 왼쪽 -> 오른쪽 또는 오른쪽 -> 왼쪽으로 날아오도록 설정
                bool flyRight = Random.value > 0.5f;
                // 화면 밖 스폰 위치 계산
                float startWorldX = flyRight ? (playerWorldX - 35.0f) : (playerWorldX + 35.0f);
                float speedVal = Random.Range(minFlySpeed, maxFlySpeed);
                float finalSpeed = flyRight ? speedVal : -speedVal;

                // 비행 장애물은 일회성으로 가로지르므로 무한 스크롤에 묶이지 않지만, 구조상 rowScrollWidth를 제공합니다.
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

                // 무작위로 왼쪽 -> 오른쪽 또는 오른쪽 -> 왼쪽으로 날아오도록 설정
                bool flyRight = Random.value > 0.5f;
                // 화면 밖 스폰 위치 계산
                float startWorldX = flyRight ? (playerWorldX - 35.0f) : (playerWorldX + 35.0f);
                // 속도는 일반 비행 장애물 최대 속도의 약 1.8배 ~ 2.5배로 빠르게 설정
                float speedMultiplier = Random.Range(1.8f, 2.5f);
                float speedVal = maxFlySpeed * speedMultiplier;
                if (speedVal < 15f) speedVal = 15f; // 너무 느리지 않게 최소 한계 지정
                float finalSpeed = flyRight ? speedVal : -speedVal;

                GameObject flyingFastCube = _CreateCube(0, y, eMapProp.eMapProp_JumpZero, rowScrollWidth, false, true, startWorldX);
                
                // _CreateCube에서 자동으로 생성되는 CubeFlyingJumpZero 제거 후 CubeFastObstacle 추가
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
                
                // 시각적으로 구분되도록 주황색(오렌지색)으로 틴트
                Renderer rendFast = flyingFastCube.GetComponent<Renderer>();
                if (rendFast != null)
                {
                    rendFast.material.color = new Color(1.0f, 0.5f, 0.0f);
                }

                m_listCube.Add(flyingFastCube);
            }

            // 보석(Coin) 배치 (설정된 주기이며 장애물이 생성되지 않는 칸일 때만 스폰)
            if (y >= 3 && coinInt > 0 && y % coinInt == 0 && !hasObstacle)
            {
                int randomX = Random.Range(minX + 2, maxX + 1);
                m_listCoin.Add(_CreateCoin(randomX, y, rowScrollWidth));
            }
        }
        m_highestGeneratedY = targetY + 1;
    }

    private void CleanupBlocksBelow(int limitY)
    {
        for (int i = m_listCube.Count - 1; i >= 0; i--)
        {
            GameObject go = m_listCube[i];
            if (go != null)
            {
                int gridY = Mathf.RoundToInt((go.transform.position.y + m_fCubeSize) / (m_fCubeSize > 0f ? m_fCubeSize : 1.0f));
                if (gridY < limitY)
                {
                    m_listCube.RemoveAt(i);
                    Util.MyDestroy(go);
                }
            }
            else
            {
                m_listCube.RemoveAt(i);
            }
        }

        for (int i = m_listCoin.Count - 1; i >= 0; i--)
        {
            GameObject go = m_listCoin[i];
            if (go != null)
            {
                int gridY = Mathf.RoundToInt((go.transform.position.y + m_fCubeSize) / (m_fCubeSize > 0f ? m_fCubeSize : 1.0f));
                if (gridY < limitY)
                {
                    m_listCoin.RemoveAt(i);
                    Util.MyDestroy(go);
                }
            }
            else
            {
                m_listCoin.RemoveAt(i);
            }
        }
    }
}
