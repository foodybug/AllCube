using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ResultMain : MonoBehaviour
{
    private static ResultMain s_Instance = null;
    private bool m_bDestroyed = false;

    [Header("Web Leaderboard Settings")]
    [SerializeField] private string m_rankingServerUrl = "https://all-cube-server.vercel.app/submit_score";

    [Header("UI View Reference")]
    [SerializeField] private UI_Result ui;

    // Parallax 배경 연출용 변수들
    private GameObject m_goBackgroundContainer = null;
    private List<ParallaxScroll> m_parallaxObjects = new List<ParallaxScroll>();
    private Material m_farSkyMaterial = null;
    private Material m_midCubeMaterial = null; // 배경용 공유 머티리얼 (정적 스크롤 보장)
    private GameObject m_goVirtualCamera = null;
    private float m_virtualCameraY = 0f;

    // 지그재그 점핑 플레이어 복제본 리스트 (Title 씬과 대칭)
    private List<GameObject> m_playerObjects = new List<GameObject>();
    private List<Material> m_playerMaterials = new List<Material>();

    // 실시간 서버 랭킹 결과 문자열 캐싱
    private string m_serverLeaderboardText = "";

    void Awake()
    {
        if (s_Instance != null && s_Instance != this)
        {
            Debug.LogWarning($"[ResultMain Duplication Clean] Multiple ResultMain components detected! Destroying duplicate on {gameObject.name}");
            m_bDestroyed = true;
            Destroy(this);
            return;
        }
        s_Instance = this;
        Screen.orientation = ScreenOrientation.Portrait;
    }

    void Start()
    {
        if (m_bDestroyed) return;
        if (ui == null)
        {
            ui = GetComponent<UI_Result>();
            if (ui == null)
            {
                ui = FindFirstObjectByType<UI_Result>();
            }
        }

        // 가상 카메라 오브젝트 생성 및 배경 연출 준비
        m_goVirtualCamera = new GameObject("VirtualCameraAnchor");
        m_virtualCameraY = 0f;
        m_goVirtualCamera.transform.position = Vector3.zero;

        CreateBackground();
        EnsureCanvasAndCameraAspect();

        // Result 씬 하단 구글 배너 광고 노출
        if (AdmobManager.Instance != null)
        {
            AdmobManager.Instance.Show();
        }

        StartCoroutine(SubmitAndRefreshUI_CR());
    }

    private void EnsureCanvasAndCameraAspect()
    {
        Camera cam = CameraManager.GetMainCamera();
        if (cam != null)
        {
            CameraManager.ApplyAspect(cam);
        }

        Canvas[] canvases = FindObjectsByType<Canvas>(FindObjectsSortMode.None);
        if (canvases != null)
        {
            foreach (var canvas in canvases)
            {
                if (canvas != null)
                {
                    if (cam != null)
                    {
                        cam.cullingMask |= (1 << LayerMask.NameToLayer("UI")) | (1 << 5);
                    }

                    UnityEngine.UI.CanvasScaler scaler = canvas.GetComponent<UnityEngine.UI.CanvasScaler>();
                    if (scaler == null) scaler = canvas.gameObject.AddComponent<UnityEngine.UI.CanvasScaler>();
                    scaler.uiScaleMode = UnityEngine.UI.CanvasScaler.ScaleMode.ScaleWithScreenSize;
                    scaler.referenceResolution = new Vector2(720, 1280);
                    scaler.screenMatchMode = UnityEngine.UI.CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
                    scaler.matchWidthOrHeight = 0.5f;

                    // 9:16 카메라 뷰포트 내부에 UI를 정밀하게 가두기 위한 컨테이너 바인딩
                    Transform container = canvas.transform.Find("UIViewportContainer");
                    GameObject containerGo = null;
                    if (container == null)
                    {
                        containerGo = new GameObject("UIViewportContainer", typeof(RectTransform));
                        containerGo.transform.SetParent(canvas.transform, false);
                        RectTransform rt = containerGo.GetComponent<RectTransform>();
                        rt.offsetMin = Vector2.zero;
                        rt.offsetMax = Vector2.zero;

                        System.Collections.Generic.List<Transform> childrenToMove = new System.Collections.Generic.List<Transform>();
                        for (int i = 0; i < canvas.transform.childCount; i++)
                        {
                            Transform child = canvas.transform.GetChild(i);
                            if (child != containerGo.transform)
                            {
                                childrenToMove.Add(child);
                            }
                        }
                        foreach (Transform child in childrenToMove)
                        {
                            child.SetParent(containerGo.transform, false);
                        }
                    }
                    else
                    {
                        containerGo = container.gameObject;
                    }

                    if (containerGo != null)
                    {
                        UIViewportEnforcer enforcer = containerGo.GetComponent<UIViewportEnforcer>();
                        if (enforcer == null) enforcer = containerGo.AddComponent<UIViewportEnforcer>();
                        enforcer.UpdateViewportBounds();
                    }
                }
            }
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

        // 2. 카메라 배경색 및 배경 머티리얼 실시간 HSV 순환 (Play 씬과 일관성 유지)
        float baseHue = (MainManager.lastMaxHeight / 50.0f) % 1.0f;
        float slowTimeHue = (baseHue + Time.time * 0.03f) % 1.0f;
        if (slowTimeHue < 0f) slowTimeHue += 1.0f;

        Color farColor = Color.HSVToRGB(slowTimeHue, 0.5f, 0.28f);
        
        Camera cam = CameraManager.GetMainCamera();
        
        if (cam != null)
        {
            CameraManager.ApplyAspect(cam);
            cam.backgroundColor = farColor;
        }

        if (m_farSkyMaterial != null)
        {
            m_farSkyMaterial.color = farColor;
        }

        // 3. 정적 배경 큐브들의 색상을 은은한 어둠으로 펄싱
        if (m_midCubeMaterial != null)
        {
            float midHue = (slowTimeHue + 0.15f) % 1.0f;
            Color midColor = Color.HSVToRGB(midHue, 0.6f, 0.35f);
            midColor.a = 0.45f;
            m_midCubeMaterial.color = midColor;
        }
    }

    private void TriggerNextButton()
    {
        OnBtnNextClicked();
    }

    private void OnBtnNextClicked()
    {
        AudioManager.Instance.Play("Sound/ui_button_down");
        if (MainManager.Instance != null)
        {
            MainManager.Instance.TransitionToScene("Play");
        }
        else
        {
            UnityEngine.SceneManagement.SceneManager.LoadScene("Play");
        }
    }

    private void OnBtnTitleClicked()
    {
        AudioManager.Instance.Play("Sound/ui_button_down");
        if (MainManager.Instance != null)
        {
            MainManager.Instance.TransitionToScene("Title");
        }
        else
        {
            UnityEngine.SceneManagement.SceneManager.LoadScene("Title");
        }
    }

    private IEnumerator SubmitAndRefreshUI_CR()
    {
        SetupResultScreen();
        yield return StartCoroutine(SubmitScoreToWebserver_CR(MainManager.lastMaxHeight));
        UpdateResultText();
    }

    private void SetupResultScreen()
    {
        if (ui == null) return;

        if (ui.btnRetry != null)
        {
            ui.btnRetry.gameObject.SetActive(true);
            ui.btnRetry.onClick.RemoveAllListeners();
            ui.btnRetry.onClick.AddListener(OnBtnNextClicked);
        }

        if (ui.texRetryBtnBg != null) ui.texRetryBtnBg.gameObject.SetActive(true);

        if (ui.btnTitle != null)
        {
            ui.btnTitle.gameObject.SetActive(true);
            if (ui.textTitle != null) ui.textTitle.text = "Title";
            ui.btnTitle.onClick.RemoveAllListeners();
            ui.btnTitle.onClick.AddListener(OnBtnTitleClicked);

            var titleRaw = ui.btnTitle.GetComponent<UnityEngine.UI.RawImage>();
            if (titleRaw != null)
            {
                titleRaw.texture = Resources.Load("UI/msgbox") as Texture;
                titleRaw.color = new Color(0.8f, 0.75f, 0.72f, 0.9f);
            }
        }

        if (ui.textResultTime != null) ui.textResultTime.verticalOverflow = VerticalWrapMode.Overflow;

        if (UI_Play.eLevelClearType.eLevelClearType_None == MainManager.lastClearType)
        {
            if (ui.textRetry != null) ui.textRetry.text = "Retry";
            if (ui.texRetry != null) ui.texRetry.texture = Resources.Load("UI/retry_bg") as Texture;

            if (ui.texResultIcon != null)
            {
                ui.texResultIcon.enabled = true;
                ui.texResultIcon.texture = Resources.Load("UI/ui_time_bronze") as Texture;
            }
            UpdateResultText();
        }
        else
        {
            AudioManager.Instance.Play("Sound/clear");
            if (ui.texRetry != null) ui.texRetry.texture = Resources.Load("UI/done_bg") as Texture;

            if (ui.textRetry != null)
            {
                if (MainManager.nCurLevelStatic == MainManager.Instance.nLevelCount)
                    ui.textRetry.text = "Clear!";
                else
                    ui.textRetry.text = "Done";
            }

            if (ui.texResultIcon != null) ui.texResultIcon.enabled = true;
            UpdateResultText();

            if (ui.texResultIcon != null)
            {
                UI_Play.eLevelClearType clearType = UI_Play.eLevelClearType.eLevelClearType_None;
                if (UI_Play.Instance != null) clearType = UI_Play.Instance.eClearType;
                else if (UIManager.Instance != null) clearType = (UI_Play.eLevelClearType)UIManager.Instance.eClearType;

                if (clearType == UI_Play.eLevelClearType.eLevelClearType_Gold)
                    ui.texResultIcon.texture = Resources.Load("UI/ui_time_gold") as Texture;
                else if (clearType == UI_Play.eLevelClearType.eLevelClearType_Silver)
                    ui.texResultIcon.texture = Resources.Load("UI/ui_time_silver") as Texture;
                else
                    ui.texResultIcon.texture = Resources.Load("UI/ui_time_bronze") as Texture;
            }

            if (0 == MainManager.Instance.nClearType[MainManager.nCurLevelStatic - 1])
                MainManager.Instance.nClearType[MainManager.nCurLevelStatic - 1] = (int)(MainManager.lastClearType);
            else
            {
                if (MainManager.Instance.nClearType[MainManager.nCurLevelStatic - 1] > (int)(MainManager.lastClearType))
                    MainManager.Instance.nClearType[MainManager.nCurLevelStatic - 1] = (int)(MainManager.lastClearType);
            }

            if (LevelSelecter.Instance != null)
            {
                LevelSelecter.Instance.UpdateSelectBtnStateAndSaveData();
            }
        }
    }

    private void UpdateResultText()
    {
        if (ui == null) return;

        int nMin = MainManager.lastGameTime / 60;
        int nSec = MainManager.lastGameTime % 60;
        string bestSuffix = "";
        if (MainManager.lastMaxHeight >= MainManager.lastBestHeight && MainManager.lastMaxHeight > 0)
        {
            bestSuffix = " <color=yellow>[NEW BEST!]</color>";
        }

        string rankHeader = "";
        string leaderboardBody = "";

        if (MainManager.lastServerRank > 0)
        {
            // 1. 서버 실시간 랭킹 연동 성공
            string suffix = GetRankSuffix(MainManager.lastServerRank);
            rankHeader = string.Format("<size=22><color=yellow><b>Rank: {0}{1} (Top {2:F2}%)</b></color></size>", 
                MainManager.lastServerRank, suffix, MainManager.lastServerPercentage);
            leaderboardBody = m_serverLeaderboardText;
        }
        else if (MainManager.lastServerRank == -2)
        {
            // 2. 서버 연동 실패 (최종 로컬 폴백)
            string rankStr = GetWorldRankString(MainManager.lastMaxHeight);
            rankHeader = string.Format("<size=22><color=yellow><b>{0}</b></color></size>", rankStr);
            leaderboardBody = "Server offline. Shown local estimation.";
        }
        else
        {
            // 3. 서버 응답 대기 상태 (Connecting...)
            rankHeader = "<size=22><color=gray><b>Connecting Server...</b></color></size>";
            leaderboardBody = "Loading global leaderboard rankings...";
        }

        string textContent = string.Format("{0:D2}:{1:D2}\nHeight {2}m  Best {3}m{4}\n\n{5}\n\n{6}", 
            nMin, nSec, MainManager.lastMaxHeight, MainManager.lastBestHeight, bestSuffix, rankHeader, leaderboardBody);

        if (ui.textResultTime != null)
        {
            ui.textResultTime.text = textContent;
            ui.textResultTime.enabled = true;
        }
    }

    private IEnumerator SubmitScoreToWebserver_CR(int score)
    {
        MainManager.lastServerRank = -1;
        MainManager.lastServerPercentage = -1.0;
        m_serverLeaderboardText = "";

        string url = m_rankingServerUrl;
        string json = string.Format("{{\"deviceId\":\"{0}\",\"height\":{1}}}", SystemInfo.deviceUniqueIdentifier, score);

        using (UnityEngine.Networking.UnityWebRequest request = new UnityEngine.Networking.UnityWebRequest(url, "POST"))
        {
            byte[] bodyRaw = System.Text.Encoding.UTF8.GetBytes(json);
            request.uploadHandler = new UnityEngine.Networking.UploadHandlerRaw(bodyRaw);
            request.downloadHandler = new UnityEngine.Networking.DownloadHandlerBuffer();
            request.SetRequestHeader("Content-Type", "application/json");
            request.timeout = 5; 

            yield return request.SendWebRequest();

            if (request.result == UnityEngine.Networking.UnityWebRequest.Result.Success)
            {
                try
                {
                    string responseText = request.downloadHandler.text;
                    Debug.Log("[ResultMain WebServer] Submit Score Success: " + responseText);

                    RankingResponse res = JsonUtility.FromJson<RankingResponse>(responseText);
                    if (res != null)
                    {
                        MainManager.lastServerRank = res.rank;
                        MainManager.lastServerPercentage = res.topPercentage;

                        System.Text.StringBuilder sb = new System.Text.StringBuilder();
                        if (res.leaderboardWindow != null && res.leaderboardWindow.Count > 0)
                        {
                            int entryCount = 0;
                            foreach (var entry in res.leaderboardWindow)
                            {
                                if (entryCount >= 5) break; // 최대 5개 항목만 텍스트 오버플로우 방지용 표시
                                string suffix = GetRankSuffix(entry.rank);
                                string nameStr = entry.deviceId;
                                if (nameStr.Length > 8)
                                {
                                    nameStr = nameStr.Substring(0, 4) + "****";
                                }

                                if (entry.isSelf)
                                {
                                    sb.AppendLine(string.Format("<color=yellow><b>▶ {0}{1} YOU {2}m ◀</b></color>", entry.rank, suffix, entry.height));
                                }
                                else
                                {
                                    sb.AppendLine(string.Format("{0}{1}  {2}  {3}m", entry.rank, suffix, nameStr, entry.height));
                                }
                                entryCount++;
                            }
                        }
                        else
                        {
                            sb.AppendLine("No leaderboard entries found.");
                        }
                        m_serverLeaderboardText = sb.ToString();
                    }
                }
                catch (System.Exception e)
                {
                    Debug.LogError("[ResultMain WebServer] JSON parsing error: " + e.Message + " | Response: " + request.downloadHandler.text);
                    MainManager.lastServerRank = -2;
                    m_serverLeaderboardText = "";
                }
            }
            else
            {
                Debug.LogWarning("[ResultMain WebServer] Score submit failed or server offline: " + request.error);
                MainManager.lastServerRank = -2;
                m_serverLeaderboardText = "";
            }
        }
        UpdateResultText();
    }

    private static string GetRankSuffix(int rank)
    {
        if (rank % 100 >= 11 && rank % 100 <= 13) return "th";
        switch (rank % 10)
        {
            case 1: return "st";
            case 2: return "nd";
            case 3: return "rd";
            default: return "th";
        }
    }

    private string GetWorldRankString(int height)
    {
        if (height <= 0) return "Rank: -";

        if (MainManager.lastServerRank > 0)
        {
            string suffix = GetRankSuffix(MainManager.lastServerRank);
            return string.Format("Rank: {0}{1} (Top {2:F2}% / Server)", MainManager.lastServerRank, suffix, MainManager.lastServerPercentage);
        }

        long totalPlayers = 1542800;
        double factor = System.Math.Max(0.00001, System.Math.Exp(-height * 0.05));
        long rank = (long)System.Math.Max(1, System.Math.Round(totalPlayers * factor));
        double topPercentage = (double)rank / totalPlayers * 100.0;

        if (rank == 1)
        {
            return "Rank: 1st (Top 0.0001%)";
        }
        else if (topPercentage < 0.1)
        {
            return string.Format("Rank: {0:n0}th (Top {1:F4}%)", rank, topPercentage);
        }
        else
        {
            string suffix = GetRankSuffix((int)rank);
            return string.Format("Rank: {0:n0}{1} (Top {2:F2}%)", rank, suffix, topPercentage);
        }
    }

    private void CreateBackground()
    {
        ClearBackground();

        m_goBackgroundContainer = new GameObject("BackgroundContainer");
        m_goBackgroundContainer.transform.parent = null;
        UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(m_goBackgroundContainer, UnityEngine.SceneManagement.SceneManager.GetActiveScene());

        Camera cam = CameraManager.GetMainCamera();
        
        if (cam == null)
        {
            Debug.LogError("[ResultMain] Camera is missing in scene! Background creation aborted.");
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

        // 2. 중경 배경용 정적 Parallax 3D Cube군 (통통 튀지 않고 유유히 흐르는 먼배경)
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

    }

    private void ClearBackground()
    {
        m_parallaxObjects.Clear();

        // 1. 기존 컨테이너 변수 즉시 파괴
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

        Camera cam = CameraManager.GetMainCamera();
        
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

        int playerGoCount = m_playerObjects.Count;
        for (int i = 0; i < playerGoCount; i++)
        {
            if (m_playerObjects[i] != null)
            {
                Destroy(m_playerObjects[i]);
            }
        }
        m_playerObjects.Clear();

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
}

[System.Serializable]
public class RankingEntry
{
    public int rank;
    public string deviceId;
    public int height;
    public bool isSelf;
}

[System.Serializable]
public class RankingResponse
{
    public int rank;
    public double topPercentage;
    public int totalPlayers;
    public List<RankingEntry> leaderboardWindow;
}
