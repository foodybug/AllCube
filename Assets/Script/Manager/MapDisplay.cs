using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 인게임 MapManager의 실제 스폰 알고리즘, 컴포넌트, 머티리얼, 메쉬와 100% 동일하게
/// 전체 50개 DifficultyTier의 모든 맵 요소를 씬 내에 시각화해주는 맵 디스플레이 매니저
/// </summary>
public class MapDisplay : MonoBehaviour
{
    [Header("Target Holder & Manager Reference")]
    [SerializeField]
    private StageDifficultyHolder m_difficultyHolder;

    [SerializeField]
    private MapManager m_mapManager;

    [Header("Display Settings")]
    [SerializeField]
    private float m_fCubeSize = 1.0f;

    [SerializeField]
    private bool m_autoGenerateOnStart = true;

    [SerializeField]
    private int m_randomSeed = 12345; // 시각화 맵 생성용 고정 난수 시드 (동일 레이아웃 재현)

    private Transform m_displayRoot;

    private void Awake()
    {
        if (m_autoGenerateOnStart)
        {
            GenerateFullStageMap();
        }
    }

    [ContextMenu("Generate Full Stage Map Visualizer (Identical to MapManager)")]
    public void GenerateFullStageMap()
    {
        // 1. StageDifficultyHolder 참조 확보 및 티어 동기화
        if (m_difficultyHolder == null)
        {
            m_difficultyHolder = FindFirstObjectByType<StageDifficultyHolder>();
            if (m_difficultyHolder == null)
            {
                GameObject holderGo = new GameObject("StageDifficultyHolder");
                m_difficultyHolder = holderGo.AddComponent<StageDifficultyHolder>();
            }
        }

        m_difficultyHolder.SyncTiers();
        List<DifficultyTier> tiers = m_difficultyHolder.difficultyTiers;

        // 2. MapManager 참조 확보 (프리팹 및 공유 머티리얼 참조용)
        if (m_mapManager == null)
        {
            m_mapManager = MapManager.Instance != null ? MapManager.Instance : FindFirstObjectByType<MapManager>();
        }

        // 3. 기존 생성 루트 오브젝트 초기화
        if (m_displayRoot != null)
        {
            if (Application.isPlaying)
                Destroy(m_displayRoot.gameObject);
            else
                DestroyImmediate(m_displayRoot.gameObject);
        }

        GameObject rootGo = new GameObject("MapDisplay_FullStageRoot");
        m_displayRoot = rootGo.transform;

        if (tiers == null || tiers.Count == 0)
        {
            Debug.LogWarning("[MapDisplay] No difficulty tiers found in StageDifficultyHolder.");
            return;
        }

        UnityEngine.Random.InitState(m_randomSeed);

        int maxTierHeight = tiers[tiers.Count - 1].minHeight + 10;
        Debug.Log($"[MapDisplay] Generating identical full stage map up to Y={maxTierHeight} (Tiers 0~{tiers.Count - 1})...");

        // 4. MapManager의 실제 스폰 알고리즘과 동일하게 0m부터 최상단까지 전체 스폰 진행
        for (int y = 0; y <= maxTierHeight; y++)
        {
            DifficultyTier currentTier = m_difficultyHolder.GetTierForHeight(y);
            SpawnIdenticalRow(y, currentTier);

            // 각 티어 시작 고도 마커 라벨 생성
            for (int t = 0; t < tiers.Count; t++)
            {
                if (tiers[t].minHeight == y)
                {
                    CreateTierLabelMarker(t, y, tiers[t]);
                }
            }
        }

        // 5. 오버뷰 탐색 카메라 배치
        SetupOverviewCamera(maxTierHeight);

        Debug.Log($"[MapDisplay] Map display generation complete! Total tiers visualized: {tiers.Count}");
    }

    private void SpawnIdenticalRow(int y, DifficultyTier tier)
    {
        int minX = tier.minSpawnX;
        int maxX = tier.maxSpawnX;
        int staticInterval = tier.staticObstacleInterval;
        int stationaryInterval = tier.stationaryObstacleInterval;
        int minStationaryY = tier.minStationaryHeight;
        int flyingInterval = tier.flyingObstacleInterval;
        int blinkInterval = tier.blinkObstacleInterval;
        int minBlinkY = tier.minBlinkHeight;
        int coinInt = tier.coinInterval;
        int coinSeq = tier.coinSequence;
        float minFlySpeed = tier.minFlyingSpeed;
        float maxFlySpeed = tier.maxFlyingSpeed;

        float rowScrollWidth = (maxX - minX) * m_fCubeSize;

        // MapManager와 100% 동일: 양쪽 끝 경계선(minX + 1, maxX + 1) 기둥 배치 (CubeDeadly)
        CreateCubeIdentical(minX + 1, y, MapManager.eMapProp.eMapProp_JumpZero, true, false);
        CreateCubeIdentical(maxX + 1, y, MapManager.eMapProp.eMapProp_JumpZero, true, false);

        bool hasObstacle = false;

        // 1. 제자리에 고정된 Stationary 장애물 (CubeStationaryObstacle)
        if (stationaryInterval > 0 && y >= minStationaryY && y % stationaryInterval == 0)
        {
            hasObstacle = true;
            int randomX = UnityEngine.Random.Range(minX + 2, maxX + 1);
            CreateCubeIdentical(randomX, y, MapManager.eMapProp.eMapProp_Stationary, false, false);
        }

        // 2. Blink 장애물 (CubeBlink)
        if (!hasObstacle && blinkInterval > 0 && y >= minBlinkY && y % blinkInterval == 0)
        {
            hasObstacle = true;
            int randomX = UnityEngine.Random.Range(minX + 2, maxX + 1);
            CreateCubeIdentical(randomX, y, MapManager.eMapProp.eMapProp_Blink, false, false);
        }

        // 3. 고정형 JumpZero 장애물 (CubeJumpZero)
        if (!hasObstacle && y >= 3 && staticInterval > 0 && y % staticInterval == 0)
        {
            hasObstacle = true;
            int randomX = UnityEngine.Random.Range(minX + 2, maxX + 1);
            CreateCubeIdentical(randomX, y, MapManager.eMapProp.eMapProp_JumpZero, false, false);
        }

        // 4. 느린 추적 장애물 (CubeHomingObstacle)
        int homingInterval = tier.homingObstacleInterval;
        int minHomingY = tier.minHomingHeight;
        if (!hasObstacle && homingInterval > 0 && y >= minHomingY && y % homingInterval == 0)
        {
            hasObstacle = true;
            int randomX = UnityEngine.Random.Range(minX + 2, maxX + 1);
            GameObject homingCube = CreateCubeIdentical(randomX, y, MapManager.eMapProp.eMapProp_Homing, false, false);
            CubeHomingObstacle homingComp = homingCube.GetComponent<CubeHomingObstacle>();
            if (homingComp != null) homingComp.enabled = false;
        }

        // 4. 비행형 JumpZero 장애물 (CubeFlyingJumpZero)
        if (y >= 4 && flyingInterval > 0 && y % flyingInterval == 0)
        {
            hasObstacle = true;
            bool flyRight = UnityEngine.Random.value > 0.5f;
            float startWorldX = flyRight ? -15.0f : 15.0f;
            float speedVal = UnityEngine.Random.Range(minFlySpeed, maxFlySpeed);
            float finalSpeed = flyRight ? speedVal : -speedVal;

            GameObject flyingCube = CreateCubeIdentical(0, y, MapManager.eMapProp.eMapProp_JumpZero, false, true, startWorldX);
            CubeFlyingJumpZero comp = flyingCube.GetComponent<CubeFlyingJumpZero>();
            if (comp != null)
            {
                comp.InitFlying(0f, null);
                comp.enabled = false;
            }
        }

        // 5. 비행형 Fast Obstacle 장애물 (CubeFastObstacle)
        if (y >= 6 && y % 15 == 0)
        {
            hasObstacle = true;
            bool flyRight = UnityEngine.Random.value > 0.5f;
            float startWorldX = flyRight ? -15.0f : 15.0f;

            GameObject flyingFastCube = CreateCubeIdentical(0, y, MapManager.eMapProp.eMapProp_JumpZero, false, true, startWorldX);
            CubeFlyingJumpZero tempComp = flyingFastCube.GetComponent<CubeFlyingJumpZero>();
            if (tempComp != null)
            {
                if (Application.isPlaying) Destroy(tempComp);
                else DestroyImmediate(tempComp);
            }

            CubeFastObstacle fastComp = flyingFastCube.AddComponent<CubeFastObstacle>();
            if (fastComp != null)
            {
                fastComp.InitFlying(0f, null);
                fastComp.enabled = false;
            }

            // MapDisplay 씬 전용: Fast Obstacle의 X 스케일이 PingPong 형태로 가변 생동되도록 처리
            if (flyingFastCube.GetComponent<MapDisplayFastObstaclePingPong>() == null)
            {
                flyingFastCube.AddComponent<MapDisplayFastObstaclePingPong>();
            }

            Renderer rendFast = flyingFastCube.GetComponent<Renderer>();
            if (rendFast != null && m_mapManager != null) rendFast.sharedMaterial = m_mapManager.GetSharedMaterial(9);
        }

        // 6. 타겟팅 레이저 장애물 (CubeLaser - 90m부터 등장)
        if (y >= 90 && y % 15 == 0)
        {
            bool spawnLeft = UnityEngine.Random.value > 0.5f;
            float startWorldX = spawnLeft ? -10.0f : 10.0f;
            CreateCubeIdentical(0, y, MapManager.eMapProp.eMapProp_Laser, false, true, startWorldX);
        }

        // 7. 광란의 사방팔방 무작위 이동 장애물 (CubeCrazy - 150m부터 등장, 빈도 축소: 60m 주기)
        if (y >= 150 && y % 60 == 0)
        {
            int randomX = UnityEngine.Random.Range(minX + 2, maxX + 1);
            CreateCubeIdentical(randomX, y, MapManager.eMapProp.eMapProp_Crazy, false, false);
        }

        // 7. 보석(Coin) 배치 (MapManager의 4개 스폰 방식 100% 동일 구현)
        if (y >= 3 && !hasObstacle)
        {
            if (coinInt > 0 && (y % coinInt < coinSeq))
            {
                List<int> availX = new List<int>();
                for (int sx = minX + 2; sx <= maxX + 1; sx++)
                {
                    availX.Add(sx);
                }
                for (int c = 0; c < 4 && availX.Count > 0; c++)
                {
                    int randIdx = UnityEngine.Random.Range(0, availX.Count);
                    int randomX = availX[randIdx];
                    availX.RemoveAt(randIdx);
                    CreateCoinIdentical(randomX, y);
                }
            }
        }
    }

    private GameObject CreateCubeIdentical(int x, int y, MapManager.eMapProp prop, bool isBoundaryWall, bool isFlying, float customWorldX = 0f)
    {
        GameObject srcPrefab = (m_mapManager != null && m_mapManager.goCubeSrc != null) ? m_mapManager.goCubeSrc : null;
        GameObject go = srcPrefab != null ? Instantiate(srcPrefab) : PrimitiveUtil.CreatePrimitive(PrimitiveType.Cube);

        go.name = $"Cube_{prop}_X{x}_Y{y}";
        go.transform.parent = m_displayRoot;

        Vector3 vPos = Vector3.zero;
        vPos.x = isFlying ? customWorldX : (m_fCubeSize * x - m_fCubeSize);
        vPos.y = m_fCubeSize * y - m_fCubeSize;
        go.transform.position = vPos;

        switch (prop)
        {
            case MapManager.eMapProp.eMapProp_Normal:
                Renderer rendNormal = go.GetComponent<Renderer>();
                if (rendNormal != null && m_mapManager != null)
                {
                    rendNormal.sharedMaterial = m_mapManager.GetSharedMaterial(UnityEngine.Random.Range(1, 5));
                }
                break;

            case MapManager.eMapProp.eMapProp_Break:
                if (go.GetComponent<CubeBreak>() == null) go.AddComponent<CubeBreak>();
                break;

            case MapManager.eMapProp.eMapProp_MoveX:
                if (go.GetComponent<CubeMoveX>() == null) go.AddComponent<CubeMoveX>();
                break;

            case MapManager.eMapProp.eMapProp_MoveY:
                if (go.GetComponent<CubeMoveY>() == null) go.AddComponent<CubeMoveY>();
                break;

            case MapManager.eMapProp.eMapProp_JumpZero:
                if (isBoundaryWall)
                {
                    if (go.GetComponent<CubeDeadly>() == null) go.AddComponent<CubeDeadly>();
                }
                else if (isFlying)
                {
                    if (go.GetComponent<CubeFlyingJumpZero>() == null) go.AddComponent<CubeFlyingJumpZero>();
                }
                else
                {
                    CubeJumpZero comp = go.GetComponent<CubeJumpZero>();
                    if (comp == null) comp = go.AddComponent<CubeJumpZero>();
                    comp.isBoundaryWall = false;
                }
                break;

            case MapManager.eMapProp.eMapProp_Blink:
                if (go.GetComponent<CubeBlink>() == null) go.AddComponent<CubeBlink>();
                break;

            case MapManager.eMapProp.eMapProp_Laser:
                if (go.GetComponent<CubeLaser>() == null) go.AddComponent<CubeLaser>();
                break;

            case MapManager.eMapProp.eMapProp_Stationary:
                if (go.GetComponent<CubeStationaryObstacle>() == null) go.AddComponent<CubeStationaryObstacle>();
                break;

            case MapManager.eMapProp.eMapProp_Homing:
                CubeHomingObstacle hComp = go.GetComponent<CubeHomingObstacle>();
                if (hComp == null) hComp = go.AddComponent<CubeHomingObstacle>();
                hComp.enabled = false;
                break;

            case MapManager.eMapProp.eMapProp_Crazy:
                if (go.GetComponent<CubeCrazy>() == null) go.AddComponent<CubeCrazy>();
                break;
        }

        return go;
    }

    private GameObject CreateCoinIdentical(int x, int y)
    {
        GameObject srcPrefab = (m_mapManager != null && m_mapManager.goCoinSrc != null) ? m_mapManager.goCoinSrc : null;
        GameObject go = srcPrefab != null ? Instantiate(srcPrefab) : PrimitiveUtil.CreatePrimitive(PrimitiveType.Cube);

        go.name = $"Coin_X{x}_Y{y}";
        go.transform.parent = m_displayRoot;

        Vector3 vPos = Vector3.zero;
        vPos.x = m_fCubeSize * x - m_fCubeSize;
        vPos.y = m_fCubeSize * y - m_fCubeSize;
        go.transform.position = vPos;

        MeshFilter mf = go.GetComponentInChildren<MeshFilter>();
        if (mf != null)
        {
            mf.sharedMesh = PrimitiveUtil.GetCubeMesh();
        }

        // 보석(Coin) 컴포넌트 부착 및 원래 게임 내 오리지널 텍스처/머티리얼 적용
        Coin coin = go.GetComponent<Coin>();
        if (coin == null)
        {
            coin = go.AddComponent<Coin>();
        }

        coin.grade = UnityEngine.Random.Range(1, 6);
        coin.ApplyGradeScale();

        Renderer rend = go.GetComponentInChildren<Renderer>();
        if (rend != null && m_mapManager != null)
        {
            rend.sharedMaterial = m_mapManager.GetSharedMaterial(UnityEngine.Random.Range(1, 5));
        }

        return go;
    }

    private void CreateTierLabelMarker(int tierIndex, int y, DifficultyTier tier)
    {
        GameObject labelObj = new GameObject($"TierMarker_{tierIndex}_{y}m");
        labelObj.transform.parent = m_displayRoot;
        labelObj.transform.position = new Vector3(tier.minSpawnX - 6.5f, y - 0.5f, 0f);

        TextMesh textMesh = labelObj.AddComponent<TextMesh>();
        textMesh.text = $"[Tier {tierIndex}] {y}m | SpawnX:[{tier.minSpawnX},{tier.maxSpawnX}] | StatInt:{tier.stationaryObstacleInterval} | FlyInt:{tier.flyingObstacleInterval}";
        textMesh.characterSize = 0.45f;
        textMesh.fontSize = 24;
        textMesh.color = Color.cyan;
        textMesh.anchor = TextAnchor.MiddleRight;
    }

    private void SetupOverviewCamera(int maxH)
    {
        Camera mainCam = Camera.main;
        if (mainCam == null)
        {
            GameObject camGo = new GameObject("OverviewCamera");
            mainCam = camGo.AddComponent<Camera>();
            camGo.tag = "MainCamera";
        }

        mainCam.orthographic = true;
        mainCam.orthographicSize = 25f;
        mainCam.transform.position = new Vector3(0f, 20f, -20f);
        if (mainCam.GetComponent<MapDisplayCameraController>() == null)
        {
            mainCam.gameObject.AddComponent<MapDisplayCameraController>();
        }
    }
}

/// <summary>
/// MapDisplay 씬 전용 전체 맵 조망 카메라 컨트롤러 (키보드 W/S/Up/Down 및 마우스 스크롤로 전체 맵 상하 탐색)
/// </summary>
public class MapDisplayCameraController : MonoBehaviour
{
    [SerializeField] private float moveSpeed = 35.0f;
    [SerializeField] private float zoomSpeed = 15.0f;

    private void Update()
    {
        float vertical = Input.GetAxis("Vertical");
        if (Mathf.Abs(vertical) > 0.01f)
        {
            transform.Translate(0f, vertical * moveSpeed * Time.deltaTime, 0f, Space.World);
        }

        float scroll = Input.GetAxis("Mouse ScrollWheel");
        if (Mathf.Abs(scroll) > 0.01f)
        {
            Camera cam = GetComponent<Camera>();
            if (cam != null)
            {
                cam.orthographicSize = Mathf.Clamp(cam.orthographicSize - scroll * zoomSpeed, 5f, 250f);
            }
        }
    }
}

/// <summary>
/// MapDisplay 씬 전용: CubeFastObstacle의 X 스케일이 PingPong 형태로 늘어났다 줄어들도록 연출하는 컴포넌트
/// </summary>
public class MapDisplayFastObstaclePingPong : MonoBehaviour
{
    [SerializeField] private float minScaleX = 0.5f;
    [SerializeField] private float maxScaleX = 5.0f;
    [SerializeField] private float pingPongSpeed = 2.5f;

    private Vector3 initialScale;
    private float timeOffset;

    private void Start()
    {
        initialScale = transform.localScale;
        timeOffset = Random.Range(0f, 10f); // 각 장애물마다 무작위 주기로 변동되도록 상이한 위상 설정
    }

    private void Update()
    {
        float t = Mathf.PingPong((Time.time + timeOffset) * pingPongSpeed, 1.0f);
        float currentScaleX = Mathf.Lerp(minScaleX, maxScaleX, t);
        transform.localScale = new Vector3(currentScaleX, initialScale.y, initialScale.z);
    }
}
