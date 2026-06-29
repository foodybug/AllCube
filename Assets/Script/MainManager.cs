using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public enum eGameState
{
    eGameState_Logo = 0,
    eGameState_Select,
    eGameState_Play,
    eGameState_Result,
    eGameState_Pause
}

public class MainManager : MonoBehaviour
{
    private static MainManager m_instance;
    public static MainManager Instance
    {
        get
        {
            if (m_instance == null)
            {
                SceneManager.LoadScene("Title");
                return null;
            }
            return m_instance;
        }
    }

    public static bool StartWithFadeIn = false;

    [Header("Global Game State & Data")]
    public eGameState eCurState = eGameState.eGameState_Logo;
    public int nLevelCount = 10;
    public int[] nTime_gold;
    public int[] nTime_silver;
    public int[] nTime_bronze;
    public int[] nClearType;
    public int nCurLevel = 1;
    public int nSaveLevel = 1;
    public int nSoundEnable = 1;
    public int[] nBestHeight;

    [Header("Static Match Data Backups")]
    public static int nCurLevelStatic = 1;
    public static int lastTotalCoins = 0;
    public static int lastGameTime = 0;
    public static UI_Play.eLevelClearType lastClearType = UI_Play.eLevelClearType.eLevelClearType_None;
    public static int lastMaxHeight = 0;
    public static int lastBestHeight = 0;
    public static int lastJumpCount = 0;
    public static string lastDeathCause = "";
    public static int lastServerRank = -1;
    public static double lastServerPercentage = -1.0;
    public static bool StartInLevelSelect = false;

    public bool IsTransitioning
    {
        get
        {
            return TransitionCanvas.Instance != null && TransitionCanvas.Instance.IsTransitioning;
        }
    }

    void Awake()
    {
        if (m_instance != null && m_instance != this)
        {
            Debug.Log("[MainManager Debug] Duplicate MainManager instance detected in new scene. Self-destroying duplicate object.");
            Destroy(gameObject);
            return;
        }
        m_instance = this;
        DontDestroyOnLoad(gameObject);

        // 씬 로드 완료 이벤트 구독
        SceneManager.sceneLoaded += OnSceneLoaded;

        // 데이터 로드
        _LoadData();
    }

    void OnDestroy()
    {
        if (m_instance == this)
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
        }
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        Debug.Log("[MainManager Debug] OnSceneLoaded triggered. Active Scene: " + scene.name + ", StartWithFadeIn: " + StartWithFadeIn);


        if (scene.name == "Title")
        {
            if (StartInLevelSelect)
            {
                eCurState = eGameState.eGameState_Select;
            }
            else
            {
                eCurState = eGameState.eGameState_Logo;
            }
        }
        else if (scene.name == "Play")
        {
            eCurState = eGameState.eGameState_Play;
        }
        else if (scene.name == "Result")
        {
            eCurState = eGameState.eGameState_Result;
            if (UI_Play.Instance != null)
            {
                UI_Play.Instance.SetupResultScreen();
            }
        }

        if (StartWithFadeIn)
        {
            StartWithFadeIn = false;
            StartCoroutine(ReturnToScene_CR());
        }
    }

    private void EnsureTransitionCanvasExists()
    {
        if (TransitionCanvas.Instance != null) return;

        GameObject prefab = Resources.Load<GameObject>("Prefabs/TransitionCanvas");
        if (prefab == null)
        {
            prefab = Resources.Load<GameObject>("TransitionCanvas");
        }

        GameObject inst;
        if (prefab != null)
        {
            Debug.Log("[MainManager Debug] Instantiating TransitionCanvas from resources prefab.");
            inst = Instantiate(prefab);
            inst.name = "TransitionCanvas";
        }
        else
        {
            Debug.Log("[MainManager Debug] TransitionCanvas prefab not found. Dynamically creating fallback TransitionCanvas.");
            inst = new GameObject("TransitionCanvas");
            inst.AddComponent<TransitionCanvas>();
        }
    }

    private IEnumerator ReturnToScene_CR()
    {
        EnsureTransitionCanvasExists();
        yield return StartCoroutine(TransitionCanvas.Instance.PlayFadeIn_CR());
    }

    public void TransitionToScene(string sceneName)
    {
        if (IsTransitioning) return;
        StartCoroutine(TransitionToScene_CR(sceneName));
    }

    private IEnumerator TransitionToScene_CR(string sceneName)
    {
        EnsureTransitionCanvasExists();
        yield return StartCoroutine(TransitionCanvas.Instance.PlayFadeOut_CR());

        StartWithFadeIn = true;
        SceneManager.LoadScene(sceneName);
    }

    // --- 글로벌 생명주기 및 레벨 제어 로직 ---

    public void StartNextLevel()
    {
        nCurLevel++;
        if (nCurLevel > nLevelCount)
            nCurLevel = nLevelCount;
        StartLevel(nCurLevel);
    }

    public void StartLevel(int nLevel)
    {
        Debug.Log("[MainManager Debug] StartLevel called. Target Level: " + nLevel + ", Current Scene: " + SceneManager.GetActiveScene().name);
        nCurLevelStatic = nLevel;
        nCurLevel = nLevel;
        eCurState = eGameState.eGameState_Play;

        string sceneName = SceneManager.GetActiveScene().name;
        if (sceneName != "Play")
        {
            TransitionToScene("Play");
        }
        else
        {
            if (PlayMain.Instance != null)
            {
                Debug.Log("[MainManager Debug] Already in Play scene. Re-initializing stage.");
                PlayMain.Instance.SetupPlayStage(nLevel);
            }
            else
            {
                Debug.LogWarning("[MainManager Debug] Already in Play scene but GameMain.Instance is missing!");
            }
        }
    }

    public void GoLevelSelectScene()
    {
        eCurState = eGameState.eGameState_Select;
        string sceneName = SceneManager.GetActiveScene().name;
        if (sceneName != "Title")
        {
            StartInLevelSelect = true;
            TransitionToScene("Title");
        }
        else
        {
            if (PlayMain.Instance != null)
            {
                PlayMain.Instance.CleanUpStage();
            }
            else
            {
                MapManager.Instance.UnLoadCubeMap();
            }

            if (UI_Play.Instance != null)
            {
                UI_Play.Instance.CreateLevelSelectUI();
            }
        }
    }

    private void _LoadData()
    {
        if (nLevelCount <= 0) nLevelCount = 10;
        if (nClearType == null || nClearType.Length < nLevelCount)
        {
            nClearType = new int[nLevelCount];
        }
        for (int i = 0; i < nLevelCount; i++)
            nClearType[i] = 0;

        if (nBestHeight == null || nBestHeight.Length < nLevelCount)
        {
            nBestHeight = new int[nLevelCount];
        }
        for (int i = 0; i < nLevelCount; i++)
            nBestHeight[i] = 0;

        string[] res = System.IO.Directory.GetFiles(Application.persistentDataPath, "info.inf");

        if (res.Length > 0)
        {
            try
            {
                using (System.IO.FileStream fs = new System.IO.FileStream(res[0], System.IO.FileMode.Open))
                {
                    if (null != fs)
                    {
                        using (System.IO.FileStream readerFs = fs)
                        using (System.IO.BinaryReader br = new System.IO.BinaryReader(readerFs))
                        {
                            nSaveLevel = br.ReadInt32();
                            nSoundEnable = br.ReadInt32();
                            int nCount = br.ReadInt32();

                            if (nClearType.Length < nCount)
                            {
                                nClearType = new int[nCount];
                            }
                            for (int i = 0; i < nCount; i++)
                            {
                                if (i < nClearType.Length)
                                    nClearType[i] = br.ReadInt32();
                                else
                                    br.ReadInt32();
                            }

                            // 로드된 파일의 데이터 스트림 끝에 도달했는지 확인하고 nBestHeight 로드
                            if (readerFs.Position < readerFs.Length)
                            {
                                int nBestHeightCount = br.ReadInt32();
                                if (nBestHeight.Length < nBestHeightCount)
                                {
                                    nBestHeight = new int[nBestHeightCount];
                                }
                                for (int i = 0; i < nBestHeightCount; i++)
                                {
                                    if (i < nBestHeight.Length)
                                        nBestHeight[i] = br.ReadInt32();
                                    else
                                        br.ReadInt32();
                                }
                            }
                        }
                    }
                }
                Debug.Log("[MainManager Debug] Game Data successfully loaded from info.inf. SaveLevel: " + nSaveLevel + ", Sound: " + nSoundEnable);
                return;
            }
            catch (System.Exception ex)
            {
                Debug.LogWarning("[MainManager Debug] Failed to read save file info.inf. Creating new file. Error: " + ex.Message);
            }
        }

        nSaveLevel = 1;
        nSoundEnable = 1;
    }

    public void SaveData()
    {
        if (nSaveLevel < nCurLevel)
            nSaveLevel = nCurLevel;

        System.Text.StringBuilder sb = new System.Text.StringBuilder(Application.persistentDataPath);
        sb.Append("/info.inf");

        try
        {
            using (System.IO.FileStream fs = new System.IO.FileStream(sb.ToString(), System.IO.FileMode.Create))
            {
                if (null == fs)
                    return;

                fs.Seek(0, System.IO.SeekOrigin.Begin);

                using (System.IO.BinaryWriter bw = new System.IO.BinaryWriter(fs))
                {
                    bw.Write(nSaveLevel);
                    bw.Write(nSoundEnable);
                    bw.Write(nLevelCount);
                    for (int i = 0; i < nLevelCount; i++)
                    {
                        if (i < nClearType.Length)
                            bw.Write(nClearType[i]);
                        else
                            bw.Write(0);
                    }

                    // BestHeight 저장
                    bw.Write(nLevelCount);
                    for (int i = 0; i < nLevelCount; i++)
                    {
                        if (i < nBestHeight.Length)
                            bw.Write(nBestHeight[i]);
                        else
                            bw.Write(0);
                    }
                }
            }
            Debug.Log("[MainManager Debug] Game Data successfully saved to info.inf. SaveLevel: " + nSaveLevel + ", Sound: " + nSoundEnable);
        }
        catch (System.Exception ex)
        {
            Debug.LogError("[MainManager Debug] Failed to save game data. Error: " + ex.Message);
        }
    }

    void OnApplicationQuit()
    {
        SaveData();
    }

    void OnApplicationPause(bool pause)
    {
        if (true == pause)
        {
            SaveData();
        }

        if (UI_Play.Instance != null)
        {
            UI_Play.Instance.PauseTime(pause);
        }
    }
}
