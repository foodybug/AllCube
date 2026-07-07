using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UI_Title : MonoBehaviour
{
    [Header("UI Component Assigns")]
    public UnityEngine.UI.RawImage texLogo;
    public UnityEngine.UI.Text textTouchScreen;
    public GameObject goBtnSound;

    [Header("Sound Button Sub-Components")]
    public UnityEngine.UI.Button btnSound;
    public UnityEngine.UI.RawImage texSound;

    private void Awake()
    {
        Screen.orientation = ScreenOrientation.Portrait;
        AutoAssignComponents();
    }

    public void AutoAssignComponents()
    {
        if (texLogo == null) texLogo = FindChildByName<UnityEngine.UI.RawImage>("texLogo");
        if (textTouchScreen == null) textTouchScreen = FindChildByName<UnityEngine.UI.Text>("textTouchScreen");
        if (goBtnSound == null)
        {
            var btnObj = FindChildByName<UnityEngine.UI.Button>("goBtnSound");
            if (btnObj != null) goBtnSound = btnObj.gameObject;
        }

        if (goBtnSound != null)
        {
            if (btnSound == null) btnSound = goBtnSound.GetComponent<UnityEngine.UI.Button>();
            if (texSound == null) texSound = goBtnSound.GetComponent<UnityEngine.UI.RawImage>();
        }
    }

    private T FindChildByName<T>(string name) where T : Component
    {
        T comp = GetComponentInChildren<T>(true);
        if (comp != null && comp.name.Equals(name, System.StringComparison.OrdinalIgnoreCase))
        {
            return comp;
        }

        T[] children = GetComponentsInChildren<T>(true);
        if (children != null)
        {
            foreach (T child in children)
            {
                if (child.name.Equals(name, System.StringComparison.OrdinalIgnoreCase))
                {
                    return child;
                }
            }
            foreach (T child in children)
            {
                if (child.name.ToLower().Contains(name.ToLower()))
                {
                    return child;
                }
            }
        }

        T[] all = Resources.FindObjectsOfTypeAll<T>();
        if (all != null)
        {
            foreach (T item in all)
            {
                if (item != null && item.gameObject != null && item.gameObject.scene.isLoaded && item.name != null)
                {
                    if (item.name.Equals(name, System.StringComparison.OrdinalIgnoreCase))
                    {
                        return item;
                    }
                }
            }
            foreach (T item in all)
            {
                if (item != null && item.gameObject != null && item.gameObject.scene.isLoaded && item.name != null)
                {
                    if (item.name.ToLower().Contains(name.ToLower()))
                    {
                        return item;
                    }
                }
            }
        }

        return null;
    }
}

public class RhythmicPlayerCube : MonoBehaviour
{
    private Transform m_cameraTransform;
    private Camera m_camera;
    private Rigidbody m_rb;
    private Renderer m_renderer;
    private Texture m_texOn;
    private Texture m_texOff;

    private float m_amountX;
    private float m_amountY;
    private float m_torque;

    private float m_jumpInterval;
    private float m_jumpTimer;
    private float m_jumpDir = 1f;

    // 텍스처 상태 제어 변수 (도약 직후 눈을 깜빡이게 유도)
    private float m_forceWaitTimer = 0f;

    public void Init(Transform cameraTransform, float startX, float startY)
    {
        m_cameraTransform = cameraTransform;
        m_camera = cameraTransform.GetComponent<Camera>();
        m_renderer = GetComponent<Renderer>();

        // 실제 인게임 플레이어 전용 On/Off 텍스처 리소스 로드
        m_texOn = Resources.Load("Player/texPlayerOn") as Texture;
        m_texOff = Resources.Load("Player/texPlayerOff") as Texture;

        // Rigidbody를 동적으로 부착하고 Constraints를 인게임과 완전 동일하게 구성
        m_rb = gameObject.GetComponent<Rigidbody>();
        if (m_rb == null)
        {
            m_rb = gameObject.AddComponent<Rigidbody>();
        }
        m_rb.useGravity = false; // 첫 도약 전까지 중력 OFF
        m_rb.mass = 1.0f;
        m_rb.constraints = RigidbodyConstraints.FreezePositionZ | RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationY;
        m_rb.collisionDetectionMode = CollisionDetectionMode.Continuous;
        m_rb.interpolation = RigidbodyInterpolation.Interpolate; // 물리 렌더링 저더(떨림) 현상 완전 제거
        m_rb.linearDamping = 0.05f; // 자연스러운 가로 감속 유도
        m_rb.angularDamping = 0.05f; // 부드러운 회전 감속 유도

        m_jumpDir = Random.value > 0.5f ? 1f : -1f;

        ResetParams(startX, startY, true); // 최초 스폰 플래그 true
    }

    private void ResetParams(float startX, float startY, bool isInitialSpawn)
    {
        // 도약 개시 전 Y축 속도를 가상 카메라 스크롤 속도(3.6f)로 동기화하여 관성 부여
        if (m_rb != null)
        {
            m_rb.linearVelocity = new Vector3(0f, 3.6f, 0f);
            m_rb.angularVelocity = Vector3.zero;
            m_rb.useGravity = false; // 리스폰 복귀 후 첫 도약 전까지 중력 OFF
        }

        transform.position = new Vector3(startX, startY, transform.position.z);
        transform.rotation = Quaternion.identity;

        // 임펄스(Impulse) 물리 스케일에 맞춘 보정 난수
        m_amountY = Random.Range(3.6f, 4.8f); 
        m_amountX = Random.Range(1.5f, 2.5f);
        m_torque = Random.Range(1.8f, 2.8f);

        // 점프 시간 간격 (1.0초 ~ 1.4초마다 점프 입력 강제 가동)
        m_jumpInterval = Random.Range(1.0f, 1.4f);
        
        if (isInitialSpawn)
        {
            m_jumpTimer = Random.Range(0f, m_jumpInterval * 0.8f); // 최초에는 부드러운 시간차 대기
        }
        else
        {
            m_jumpTimer = m_jumpInterval; // 화면 밖 리스폰 복귀 시에는 즉각적 격발 유도!
        }
    }

    void Update()
    {
        if (m_cameraTransform == null) return;

        // 1. 실제 플레이어와 동일한 On/Off 텍스처 스와핑 제어
        m_forceWaitTimer -= Time.deltaTime;
        if (m_renderer != null)
        {
            Texture targetTex = (m_forceWaitTimer > 0f) ? m_texOff : m_texOn;
            if (m_renderer.sharedMaterial.mainTexture != targetTex && targetTex != null)
            {
                m_renderer.sharedMaterial.mainTexture = targetTex;
            }
        }

        // 2. 물리 도약 타이머 가동
        m_jumpTimer += Time.deltaTime;
        if (m_jumpTimer >= m_jumpInterval)
        {
            m_jumpTimer = 0f;
            ExecuteJump();
        }

        // 3. 카메라 Frustum(절두체) 렌더링 영역 외곽 이탈 감지 및 하단 복귀 처리
        if (m_camera == null) return;
        Vector3 pos = transform.position;
        Vector3 viewportPos = m_camera.WorldToViewportPoint(pos);

        // frustum 밖으로 완전히 벗어났거나(마진 0.15f 적용), 카메라 뒤쪽으로 넘어간 경우 감지
        if (viewportPos.x < -0.15f || viewportPos.x > 1.15f || viewportPos.y < -0.2f || viewportPos.y > 1.2f || viewportPos.z < 0f)
        {
            // [중앙 로고 가림 방지 - 뷰포트 좌우 날개 가장자리 영역 역산 지정]
            bool spawnOnLeft = (Random.value > 0.5f);
            
            // 좌측 날개는 뷰포트 x 기준 0.01 ~ 0.26, 우측 날개는 0.74 ~ 0.99
            float targetViewportX = spawnOnLeft 
                ? Random.Range(0.01f, 0.26f) 
                : Random.Range(0.74f, 0.99f);
                
            // 충분히 공간을 둔 frustum 하단 (-0.25 지점)
            float targetViewportY = -0.25f;
            
            // 카메라와 큐브 사이의 원거리 깊이 (기존 6.0f ~ 8.5f 범위 중 하나)
            float targetZ = Random.Range(6.0f, 8.5f);
            
            // 뷰포트 좌표를 카메라의 월드 공간 좌표로 정확하게 역산 변환
            Vector3 spawnWorldPos = m_camera.ViewportToWorldPoint(new Vector3(targetViewportX, targetViewportY, targetZ));

            ResetParams(spawnWorldPos.x, spawnWorldPos.y, false); // 리스폰 복귀 플래그 false
        }
    }

    private void ExecuteJump()
    {
        if (m_rb == null) return;
        m_rb.useGravity = true; // 도약이 시작되면 중력을 복구해 정상 물리 포물선을 그리게 함

        // 도약 개시 시점에 Y속도를 카메라 속도(3.6f)로 관성 보정하여 큐브가 카메라에 밀려 처지지 않게 방지
        m_rb.linearVelocity = new Vector3(0f, 3.6f, 0f);
        m_rb.angularVelocity = Vector3.zero;

        // [윗방향 기준 정돈된 각도 궤적 도약 - 좌우 도약 반경을 넓히기 위해 ±13도~±25도로 확장 조율]
        float angleOffset = Random.Range(13.0f, 25.0f) * m_jumpDir;
        Vector3 jumpDir = Quaternion.Euler(0f, 0f, angleOffset) * Vector3.up;

        // 인게임 실제 물리 충격량(4.0 수준)에 맞춘 튜닝
        float jumpForceMagnitude = Random.Range(3.6f, 4.8f);
        float torqueVal = Random.Range(1.8f, 2.8f);

        // 윗방향에서 크게 벗어나지 않는 오가닉한 물리 점프 힘 인가 (Time.deltaTime 곱 제외)
        m_rb.AddForce(jumpDir * jumpForceMagnitude, ForceMode.Impulse);

        // 가로 진행 성향 부호(jumpDir.x)의 반대 방향으로 리얼 토크 롤링 가동 (Time.deltaTime 곱 제외)
        float torqueDirection = jumpDir.x > 0f ? -1f : 1f;
        m_rb.AddTorque(new Vector3(0f, 0f, torqueDirection * torqueVal), ForceMode.Impulse);

        // 0.05초간 쿨타임(눈 감음 상태) 지정 (실제 player.cs 의 forceWait 쿨타임과 동치)
        m_forceWaitTimer = 0.05f;

        // 다음 점프 방향 반전
        m_jumpDir *= -1f;
    }
}

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
            DestroyImmediate(this);
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
                ui = FindObjectOfType<UI_Title>();
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

        Debug.Log("[Title Debug] Initialize active. MainManager.Instance is now valid.");
        MainManager.Instance.eCurState = eGameState.eGameState_Logo;
        CameraManager.Instance.Init();
        
        if (ui != null)
        {
            if (ui.texLogo != null) ui.texLogo.gameObject.SetActive(true);
            if (ui.textTouchScreen != null) ui.textTouchScreen.gameObject.SetActive(true);
            if (ui.goBtnSound != null) ui.goBtnSound.SetActive(true);

            if (ui.btnSound != null)
            {
                ui.btnSound.onClick.RemoveAllListeners();
                ui.btnSound.onClick.AddListener(OnBtnSoundClicked);
            }

            RefreshSoundButtonIcon();
        }

        if (MainManager.StartInLevelSelect)
        {
            MainManager.StartInLevelSelect = false;
            MainManager.Instance.eCurState = eGameState.eGameState_Select;
            
            if (ui != null)
            {
                if (ui.textTouchScreen != null) ui.textTouchScreen.gameObject.SetActive(false);
                if (ui.texLogo != null) ui.texLogo.gameObject.SetActive(false);
                if (ui.goBtnSound != null) ui.goBtnSound.SetActive(true);
            }

            MainManager.Instance.GoLevelSelectScene();
        }

        m_bInitialized = true;
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
        if (cam == null) cam = FindObjectOfType<Camera>();
        
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
        m_goBackgroundContainer.transform.parent = this.transform;

        Camera cam = Camera.main;
        if (cam == null) cam = FindObjectOfType<Camera>();
        
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
        GameObject goFar = GameObject.CreatePrimitive(PrimitiveType.Quad);
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
                GameObject go = GameObject.CreatePrimitive(PrimitiveType.Cube);
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
            GameObject pGo = GameObject.CreatePrimitive(PrimitiveType.Cube);
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

        // 1. 기존 컨테이너 변수 즉시 파괴 (지연 파괴 Jitter 방지)
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

        // 3. 지그재그 플레이어 복제본들 즉각 청소
        int playerCount = m_playerObjects.Count;
        for (int i = 0; i < playerCount; i++)
        {
            if (m_playerObjects[i] != null)
            {
                DestroyImmediate(m_playerObjects[i]);
            }
        }
        m_playerObjects.Clear();

        int matCount = m_playerMaterials.Count;
        for (int i = 0; i < matCount; i++)
        {
            if (m_playerMaterials[i] != null)
            {
                DestroyImmediate(m_playerMaterials[i]);
            }
        }
        m_playerMaterials.Clear();

        Camera cam = Camera.main;
        if (cam == null) cam = FindObjectOfType<Camera>();
        
        if (cam != null)
        {
            Transform farBg = cam.transform.Find("Far_Background_Quad");
            if (farBg != null)
            {
                DestroyImmediate(farBg.gameObject);
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
