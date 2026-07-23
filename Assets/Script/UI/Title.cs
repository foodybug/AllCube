using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Title : MonoBehaviour
{
    private static Title s_Instance = null;
    private bool m_bDestroyed = false;
    private bool m_bInitialized = false;

    [Header("UI View Reference")]
    [SerializeField] private UI_Title ui;

    // Parallax 배경 연출용 변수들
    private GameObject m_goBackgroundContainer = null;
    private List<ParallaxScroll> m_parallaxObjects = new List<ParallaxScroll>();
    private Material m_farSkyMaterial = null;
    private Material m_midCubeMaterial = null; // 배경용 공유 머티리얼 (정적 스크롤 보장)
    private GameObject m_goVirtualCamera = null;
    private float m_virtualCameraY = 0f;

    // 지그재그 점핑 플레이어 복제본 리스트
    private List<GameObject> m_playerObjects = new List<GameObject>();
    private List<Material> m_playerMaterials = new List<Material>();

    void Awake()
    {
        if (s_Instance != null && s_Instance != this)
        {
            Debug.LogWarning($"[Title Duplication Clean] Multiple Title components detected! Destroying duplicate on {gameObject.name}");
            m_bDestroyed = true;
            Destroy(this);
            return;
        }
        s_Instance = this;
    }

    void Start()
    {
        if (m_bDestroyed) return;
        Debug.Log("[Title Debug] Start active. Title script is successfully attached and running.");

        if (ui == null)
        {
            ui = GetComponent<UI_Title>();
            if (ui == null)
            {
                ui = FindFirstObjectByType<UI_Title>();
            }
        }

        // 가상 카메라 오브젝트 생성 및 배경 연출 준비
        m_goVirtualCamera = new GameObject("VirtualCameraAnchor");
        m_virtualCameraY = 0f;
        m_goVirtualCamera.transform.position = Vector3.zero;

        CreateBackground();
    }

    void OnDestroy()
    {
        if (s_Instance == this)
        {
            s_Instance = null;
        }
        ClearBackground();
        if (m_goVirtualCamera != null)
        {
            Destroy(m_goVirtualCamera);
            m_goVirtualCamera = null;
        }
    }

    private void Initialize()
    {
        try
        {
            if (MainManager.Instance == null)
            {
                MainManager[] inactiveMain = Resources.FindObjectsOfTypeAll<MainManager>();
                if (inactiveMain != null && inactiveMain.Length > 0)
                {
                    if (inactiveMain[0].gameObject.scene.isLoaded)
                    {
                        Debug.Log("[Title Debug] Found inactive MainManager in scene. Forcing Active!");
                        inactiveMain[0].gameObject.SetActive(true);
                    }
                }
            }

            if (MainManager.Instance == null) return;

            Debug.Log("[Title Debug] Step 1: Initialize active. MainManager.Instance is valid.");
            MainManager.Instance.eCurState = eGameState.eGameState_Logo;

            Debug.Log("[Title Debug] Step 2: Checking CameraManager...");
            if (CameraManager.Instance != null)
            {
                CameraManager.Instance.Init();
            }
            else
            {
                Debug.LogWarning("[Title Debug] CameraManager.Instance is null! Searching in scene...");
                CameraManager camMgr = FindFirstObjectByType<CameraManager>();
                if (camMgr != null)
                {
                    camMgr.Init();
                }
            }

            Debug.Log("[Title Debug] Step 3: Configuring Title UI elements...");
            if (ui != null)
            {
                if (ui.texLogo != null && ui.texLogo.gameObject != null) ui.texLogo.gameObject.SetActive(true);
                if (ui.textTouchScreen != null && ui.textTouchScreen.gameObject != null) ui.textTouchScreen.gameObject.SetActive(true);
                if (ui.goBtnSound != null) ui.goBtnSound.SetActive(true);

                if (ui.btnSound != null)
                {
                    ui.btnSound.onClick.RemoveAllListeners();
                    ui.btnSound.onClick.AddListener(OnBtnSoundClicked);
                }

                RefreshSoundButtonIcon();
            }

            Debug.Log("[Title Debug] Step 4: Checking LevelSelect state...");
            if (MainManager.StartInLevelSelect)
            {
                MainManager.StartInLevelSelect = false;
                MainManager.Instance.eCurState = eGameState.eGameState_Select;

                if (ui != null)
                {
                    if (ui.textTouchScreen != null && ui.textTouchScreen.gameObject != null) ui.textTouchScreen.gameObject.SetActive(false);
                    if (ui.texLogo != null && ui.texLogo.gameObject != null) ui.texLogo.gameObject.SetActive(false);
                    if (ui.goBtnSound != null) ui.goBtnSound.SetActive(true);
                }

                MainManager.Instance.GoLevelSelectScene();
            }

            m_bInitialized = true;
            Debug.Log("[Title Debug] Step 5: Title Initialization completed successfully.");
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"[Title Debug] Exception in Initialize(): {ex.Message}\n{ex.StackTrace}");
        }
    }

    void Update()
    {
        // 1. 가상 카메라 앵커 위치 갱신 (Y축으로 빠르게 상승하고 X축을 Sine파로 리드미컬하게 흔듦)
        if (m_goVirtualCamera != null)
        {
            m_virtualCameraY += Time.deltaTime * 3.6f; 
            float virtualCameraX = Mathf.Sin(Time.time * 0.25f) * 3.5f; 
            m_goVirtualCamera.transform.position = new Vector3(virtualCameraX, m_virtualCameraY, 0f);

            // Parallax 정적 배경 스크롤 일괄 업데이트 (통통 튀지 않고 우아하게 흘러감)
            int parallaxCount = m_parallaxObjects.Count;
            for (int i = 0; i < parallaxCount; i++)
            {
                if (m_parallaxObjects[i] != null)
                {
                    m_parallaxObjects[i].UpdateParallax();
                }
            }
        }

        // 2. 카메라 배경색 및 배경 머티리얼 실시간 HSV 순환 (오로라 펄싱 연동)
        float baseHue = 0f;
        float slowTimeHue = (baseHue + Time.time * 0.03f) % 1.0f;
        if (slowTimeHue < 0f) slowTimeHue += 1.0f;

        Color farColor = Color.HSVToRGB(slowTimeHue, 0.5f, 0.28f);
        
        Camera cam = Camera.main;
        if (cam == null) cam = FindFirstObjectByType<Camera>();
        
        if (cam != null)
        {
            cam.backgroundColor = farColor;
        }

        if (m_farSkyMaterial != null)
        {
            m_farSkyMaterial.color = farColor;
        }

        // 3. 정적 배경 큐브들의 색상(Color)을 은은한 어둠으로 펄싱
        if (m_midCubeMaterial != null)
        {
            float midHue = (slowTimeHue + 0.15f) % 1.0f;
            Color midColor = Color.HSVToRGB(midHue, 0.6f, 0.35f);
            midColor.a = 0.45f;
            m_midCubeMaterial.color = midColor;
        }

        // 2-1. 터치 유도 텍스트 부드러운 Fade 블링크 애니메이션
        if (ui != null && ui.textTouchScreen != null && ui.textTouchScreen.gameObject.activeInHierarchy)
        {
            float alpha = 0.5f + Mathf.Sin(Time.time * 3f) * 0.5f; 
            Color c = ui.textTouchScreen.color;
            c.a = alpha;
            ui.textTouchScreen.color = c;
        }

        // 3. Lazy Initialization 제어
        if (!m_bInitialized)
        {
            if (MainManager.Instance == null)
            {
                MainManager[] inactiveMain = Resources.FindObjectsOfTypeAll<MainManager>();
                if (inactiveMain != null && inactiveMain.Length > 0)
                {
                    if (inactiveMain[0].gameObject.scene.isLoaded)
                    {
                        inactiveMain[0].gameObject.SetActive(true);
                    }
                }
            }

            if (MainManager.Instance != null)
            {
                Initialize();
            }
            return;
        }

        if (MainManager.Instance == null) return;

        // 로고 화면에서 터치 입력 감지 시 곧바로 Play 씬으로 전환 처리
        if (eGameState.eGameState_Logo == MainManager.Instance.eCurState)
        {
            if (MainManager.Instance != null && MainManager.Instance.IsTransitioning)
            {
                return;
            }

            if (Input.GetMouseButtonUp(0) || (Input.touchCount > 0 && Input.touches[0].phase == TouchPhase.Began))
            {
                if (ui != null && ui.goBtnSound != null)
                {
                    RectTransform soundRect = ui.goBtnSound.transform as RectTransform;
                    if (soundRect != null && RectTransformUtility.RectangleContainsScreenPoint(
                        soundRect, Input.mousePosition, null))
                    {
                        return; 
                    }
                }

                Debug.Log("[Title Debug] Screen Click/Touch detected. Calling StartLevel on MainManager.");
                AudioManager.Instance.PlayBgm("Sound/bgm");
                AdmobManager.Instance.Show();

                MainManager.Instance.StartLevel(MainManager.Instance.nSaveLevel);
            }
        }
    }

    private void OnBtnSoundClicked()
    {
        if (MainManager.Instance == null) return;

        if (0 == MainManager.Instance.nSoundEnable)
        {
            MainManager.Instance.nSoundEnable = 1;
            AudioManager.Instance.PlayBgm("Sound/bgm");
        }
        else
        {
            MainManager.Instance.nSoundEnable = 0;
            AudioManager.Instance.StopBgm();
        }

        AudioManager.Instance.Play("Sound/ui_button_down");
        RefreshSoundButtonIcon();
    }

    private void RefreshSoundButtonIcon()
    {
        if (ui == null || ui.texSound == null || MainManager.Instance == null) return;

        if (0 == MainManager.Instance.nSoundEnable)
        {
            ui.texSound.texture = Resources.Load("UI/sound_off") as Texture;
        }
        else
        {
            ui.texSound.texture = Resources.Load("UI/sound_on") as Texture;
        }
    }

    private void CreateBackground()
    {
        ClearBackground();

        m_goBackgroundContainer = new GameObject("BackgroundContainer");
        m_goBackgroundContainer.transform.parent = null;
        UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(m_goBackgroundContainer, UnityEngine.SceneManagement.SceneManager.GetActiveScene());

        Camera cam = Camera.main;
        if (cam == null) cam = FindFirstObjectByType<Camera>();
        
        if (cam == null)
        {
            Debug.LogError("[Title] Camera is missing in scene! Background creation aborted.");
            return;
        }

        Transform cameraT = cam.transform;
        Transform trackerT = m_goVirtualCamera != null ? m_goVirtualCamera.transform : cameraT;

        cam.cullingMask = -1;
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.farClipPlane = Mathf.Max(cam.farClipPlane, 300f);

        Shader bgShader = Shader.Find("Sprites/Default");
        if (bgShader == null) bgShader = Shader.Find("UI/Default");
        if (bgShader == null)
        {
            if (MapManager.Instance != null && MapManager.Instance.goCubeSrc != null)
            {
                Renderer r = MapManager.Instance.goCubeSrc.GetComponent<Renderer>();
                if (r != null && r.sharedMaterial != null)
                {
                    bgShader = r.sharedMaterial.shader;
                }
            }
        }
        if (bgShader == null) bgShader = Shader.Find("Standard");

        if (bgShader != null)
        {
            m_farSkyMaterial = new Material(bgShader);
            m_farSkyMaterial.color = new Color(0.04f, 0.04f, 0.12f, 1f);
            m_farSkyMaterial.enableInstancing = true;

            m_midCubeMaterial = new Material(bgShader);
            m_midCubeMaterial.color = new Color(0.12f, 0.1f, 0.22f, 0.45f);
            m_midCubeMaterial.enableInstancing = true;
        }

        // 1. 원경 Quad
        GameObject goFar = PrimitiveUtil.CreatePrimitive(PrimitiveType.Quad);
        goFar.name = "Far_Background_Quad";
        goFar.transform.parent = cameraT;
        goFar.transform.localPosition = new Vector3(0f, 0f, 120f);
        goFar.transform.localScale = new Vector3(800f, 800f, 1f);

        Collider colFar = goFar.GetComponent<Collider>();
        if (colFar != null) Destroy(colFar);

        Renderer rendFar = goFar.GetComponent<Renderer>();
        if (rendFar != null && m_farSkyMaterial != null)
        {
            rendFar.sharedMaterial = m_farSkyMaterial;
        }

        // 2. 중경 배경용 정적 Parallax 3D Cube군 (바운싱 없는 조용히 흐르는 스크롤용 배경)
        int cols = 3;
        int rows = 4;
        float bgLoopHeight = 70f;
        float scrollWidth = 60f;

        if (MapManager.Instance != null)
        {
            float maxSpawnX = MapManager.Instance.MaxSpawnX;
            float minSpawnX = MapManager.Instance.MinSpawnX;
            scrollWidth = (maxSpawnX - minSpawnX) * 1.0f; 
        }

        float totalWidth = scrollWidth * 1.2f;
        float gridW = totalWidth / cols;
        float gridH = bgLoopHeight / rows;

        for (int r = 0; r < rows; r++)
        {
            for (int c = 0; c < cols; c++)
            {
                int index = r * cols + c;
                GameObject go = PrimitiveUtil.CreatePrimitive(PrimitiveType.Cube);
                go.name = "Background_ParallaxCube_" + index;
                go.transform.parent = m_goBackgroundContainer.transform;

                Collider col = go.GetComponent<Collider>();
                if (col != null) Destroy(col);

                float scale = Random.Range(10f, 23f);
                go.transform.localScale = Vector3.one * scale;

                float gridCenterX = -totalWidth * 0.5f + (c + 0.5f) * gridW;
                float gridCenterY = (cameraT.position.y - 35f) + (r + 0.5f) * gridH;

                float posX = gridCenterX + Random.Range(-gridW * 0.25f, gridW * 0.25f);
                float posY = gridCenterY + Random.Range(-gridH * 0.25f, gridH * 0.25f);
                float posZ = Random.Range(25f, 48f); // 먼거리에 배치
                go.transform.position = new Vector3(posX, posY, posZ);

                Renderer rend = go.GetComponent<Renderer>();
                if (rend != null && m_midCubeMaterial != null)
                {
                    rend.sharedMaterial = m_midCubeMaterial;
                }

                ParallaxScroll parallax = go.AddComponent<ParallaxScroll>();
                parallax.Init(trackerT, 0.7f, 0.85f, scrollWidth, bgLoopHeight);
                m_parallaxObjects.Add(parallax);
            }
        }

        // 3. [실제 플레이어와 100% 동일한 외형/점핑 궤적을 띄는 복제본들 소환 (총 5개)]
        Texture playerTexOff = Resources.Load("Player/texPlayerOff") as Texture;
        for (int i = 0; i < 5; i++)
        {
            GameObject pGo = PrimitiveUtil.CreatePrimitive(PrimitiveType.Cube);
            pGo.name = "Title_Rhythmic_Player_" + i;
            pGo.transform.parent = m_goBackgroundContainer.transform;

            // 로비 씬 내에서 다른 연출 큐브와 물리 충돌하는 것 방지 (트리거 설정)
            Collider col = pGo.GetComponent<Collider>();
            if (col != null) col.isTrigger = true;

            pGo.transform.localScale = Vector3.one * Random.Range(2.0f, 2.8f);

            // [초기 스폰 가로 좌표 X - 중앙 로고 영역을 피해 좌우 날개로 완전 분산 지정]
            float cameraX = cameraT.position.x;
            bool spawnOnLeft = (Random.value > 0.5f);
            float startX = spawnOnLeft
                ? cameraX - Random.Range(3.8f, 6.8f)
                : cameraX + Random.Range(3.8f, 6.8f);

            float startY = cameraT.position.y - 12.0f - 6.0f - (i * 12.0f);
            float posZ = Random.Range(6.0f, 8.5f); // 전면 밀착 레이어에 소환
            pGo.transform.position = new Vector3(startX, startY, posZ);

            Material playerMat = new Material(Shader.Find("Sprites/Default"));
            if (playerTexOff != null)
            {
                playerMat.mainTexture = playerTexOff;
            }
            playerMat.color = Color.white;
            playerMat.enableInstancing = true;

            Renderer rend = pGo.GetComponent<Renderer>();
            if (rend != null)
            {
                rend.sharedMaterial = playerMat;
            }
            m_playerMaterials.Add(playerMat);

            RhythmicPlayerCube pc = pGo.AddComponent<RhythmicPlayerCube>();
            pc.Init(trackerT, startX, startY);

            m_playerObjects.Add(pGo);
        }
    }

    private void ClearBackground()
    {
        m_parallaxObjects.Clear();

        if (m_goBackgroundContainer != null)
        {
            Destroy(m_goBackgroundContainer);
            m_goBackgroundContainer = null;
        }

        Transform childContainer = this.transform.Find("BackgroundContainer");
        if (childContainer != null)
        {
            Destroy(childContainer.gameObject);
        }

        int playerCount = m_playerObjects.Count;
        for (int i = 0; i < playerCount; i++)
        {
            if (m_playerObjects[i] != null)
            {
                Destroy(m_playerObjects[i]);
            }
        }
        m_playerObjects.Clear();

        int matCount = m_playerMaterials.Count;
        for (int i = 0; i < matCount; i++)
        {
            if (m_playerMaterials[i] != null)
            {
                Destroy(m_playerMaterials[i]);
            }
        }
        m_playerMaterials.Clear();

        Camera cam = Camera.main;
        if (cam == null) cam = FindFirstObjectByType<Camera>();
        
        if (cam != null)
        {
            Transform farBg = cam.transform.Find("Far_Background_Quad");
            if (farBg != null)
            {
                Destroy(farBg.gameObject);
            }
        }

        if (m_farSkyMaterial != null)
        {
            Destroy(m_farSkyMaterial);
            m_farSkyMaterial = null;
        }

        if (m_midCubeMaterial != null)
        {
            Destroy(m_midCubeMaterial);
            m_midCubeMaterial = null;
        }

        // 지그재그 플레이어 큐브들 제거
        int playerGoCount = m_playerObjects.Count;
        for (int i = 0; i < playerGoCount; i++)
        {
            if (m_playerObjects[i] != null)
            {
                Destroy(m_playerObjects[i]);
            }
        }
        m_playerObjects.Clear();

        // 지그재그 플레이어용 머티리얼 소멸
        int playerMatCount = m_playerMaterials.Count;
        for (int i = 0; i < playerMatCount; i++)
        {
            if (m_playerMaterials[i] != null)
            {
                Destroy(m_playerMaterials[i]);
            }
        }
        m_playerMaterials.Clear();
    }
}
