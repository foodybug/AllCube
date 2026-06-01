using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEngine;

public enum eGameState
{
    eGameState_Logo = 0,
    eGameState_Select,
    eGameState_Play,
    eGameState_Result,
    eGameState_Pause
}

public class GameMain : MonoBehaviour
{
    static GameMain m_instance;
    public static GameMain Instance { get { return m_instance; } }

    public eGameState eCurState = eGameState.eGameState_Logo;
    public GameObject goPlayerSrc;
    public GameObject goMainBg;
    public int nLevelCount;
    public int[] nTime_gold;
    public int[] nTime_silver;
    public int[] nTime_bronze;
    public int[] nClearType;
    public int nCurLevel = 1;
    public int nSaveLevel = 1;
    public int nSoundEnable = 1;

    private GameObject m_goPlayer;

    public static int nCurLevelStatic = 1;
    public static int lastTotalCoins = 0;
    public static int lastGameTime = 0;
    public static UIManager.eLevelClearType lastClearType = UIManager.eLevelClearType.eLevelClearType_None;
    public static bool StartInLevelSelect = false;

    void Awake()
    {
        if (m_instance != null && m_instance != this)
        {
            Debug.Log("[GameMain Debug] Duplicate GameMain instance detected in new scene. Self-destroying duplicate object.");
            Destroy(gameObject);
            return;
        }
        m_instance = this;
        DontDestroyOnLoad(gameObject);
        Application.targetFrameRate = 60;
    }

    void Start()
    {
        _LoadData();
        string sceneName = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;
        Debug.Log("[GameMain Debug] Start method active. Active Scene Name is: '" + sceneName + "'");

        if (sceneName == "Title")
        {
            Debug.Log("[GameMain Debug] Active Scene matches 'Title'. Check if Title component already exists.");
            if (FindAnyObjectByType<Title>() == null && GetComponent<Title>() == null)
            {
                Debug.Log("[GameMain Debug] Title component not found. Dynamically adding Title component.");
                gameObject.AddComponent<Title>();
            }
            else
            {
                Debug.Log("[GameMain Debug] Title component already exists in scene. Skip dynamic addition.");
            }
        }
        else if (sceneName == "Play")
        {
            SetupPlayStage(nCurLevelStatic);
        }
        else if (sceneName == "Result")
        {
            eCurState = eGameState.eGameState_Result;
            if (UIManager.Instance != null)
            {
                UIManager.Instance.SetupResultScreen();
            }
        }
        else
        {
            CameraManager.Instance.Init();
            if (UIManager.Instance != null)
                UIManager.Instance.ApplySoundButton();
            else
                StartLevel(nSaveLevel);
        }
    }

    void Update()
    {
#if UNITY_ANDROID
		if( true == Input.GetKeyDown( KeyCode.Escape))
		{
			AudioManager.Instance.Play( "Sound/ui_button_down");
			if (UIManager.Instance != null)
				UIManager.Instance.ConformBackBtn();
		}
#endif

        if (eGameState.eGameState_Logo != eCurState)
        {
            // update main bg position
            if (CameraManager.Instance != null && CameraManager.Instance.mainCamera != null && goMainBg != null)
            {
                Vector3 vPos = CameraManager.Instance.mainCamera.transform.position;
                vPos.z = goMainBg.transform.position.z;
                goMainBg.transform.position = vPos;
            }
        }
    }

    void OnApplicationQuit()
    {
        SaveData();
    }

    void OnApplicationPause(bool pause)
    {
        if (true == pause)
            SaveData();

        if (UIManager.Instance != null)
            UIManager.Instance.PauseTime(pause);
    }

    public void StartNextLevel()
    {
        nCurLevel++;
        if (nCurLevel > nLevelCount)
            nCurLevel = nLevelCount;
        StartLevel(nCurLevel);
    }

    public void StartLevel(int nLevel)
    {
        Debug.Log("[GameMain Debug] StartLevel called. Target Level: " + nLevel + ", Current Scene: " + UnityEngine.SceneManagement.SceneManager.GetActiveScene().name);
        nCurLevelStatic = nLevel;
        nCurLevel = nLevel;
        eCurState = eGameState.eGameState_Play;

        string sceneName = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;
        if (sceneName != "Play")
        {
            if (MainManager.Instance != null)
            {
                Debug.Log("[GameMain Debug] MainManager exists. Calling TransitionToScene('Play').");
                MainManager.Instance.TransitionToScene("Play");
            }
            else
            {
                Debug.LogWarning("[GameMain Debug] UIManager is missing! Directly loading scene 'Play' via SceneManager.");
                UnityEngine.SceneManagement.SceneManager.LoadScene("Play");
            }
        }
        else
        {
            Debug.Log("[GameMain Debug] Already in Play scene. Setting up stage.");
            SetupPlayStage(nLevel);
        }
    }

    public void SetupPlayStage(int nLevel)
    {
        eCurState = eGameState.eGameState_Play;
        nCurLevel = nLevel;

        if (null == m_goPlayer)
        {
            m_goPlayer = GameObject.Instantiate(goPlayerSrc) as GameObject;
            CameraManager.Instance.SetTarget(m_goPlayer);
        }

        Rigidbody playerRb = m_goPlayer.GetComponent<Rigidbody>();
        if (playerRb != null)
        {
            playerRb.linearVelocity = Vector3.zero;
            playerRb.angularVelocity = Vector3.zero;
            playerRb.Sleep();
        }
        m_goPlayer.transform.position = Vector3.zero;
        m_goPlayer.transform.rotation = goPlayerSrc.transform.rotation;
        CameraManager.Instance.Init();
        MapManager.Instance.UnLoadCubeMap();
        MapManager.Instance.LoadCubeMap(nLevel);
        if (playerRb != null)
        {
            playerRb.WakeUp();
        }

        if (UIManager.Instance != null)
        {
            UIManager.Instance.SetPlayInfo(nLevel, 0, 10);
            if (false == UIManager.Instance.goBtnRetry.activeInHierarchy)
                UIManager.Instance.goBtnRetry.SetActive(true);
            UIManager.Instance.StartTime();

            UIManager.Instance.OpenHelpMsgBox_1(nLevel);
        }
    }

    public void GoLevelSelectScene()
    {
        eCurState = eGameState.eGameState_Select;
        string sceneName = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;
        if (sceneName != "Title")
        {
            StartInLevelSelect = true;
            if (MainManager.Instance != null)
            {
                MainManager.Instance.TransitionToScene("Title");
            }
            else
            {
                UnityEngine.SceneManagement.SceneManager.LoadScene("Title");
            }
        }
        else
        {
            Util.MyDestroy(m_goPlayer);
            MapManager.Instance.UnLoadCubeMap();

            if (UIManager.Instance != null)
                UIManager.Instance.CreateLevelSelectUI();
        }
    }

    private void _LoadData()
    {
        for (int i = 0; i < nLevelCount; i++)
            nClearType[i] = 0;

        System.String[] res = System.IO.Directory.GetFiles(Application.persistentDataPath, "info.inf");

        if (res.Length > 0)
        {
            FileStream fs = new FileStream(res[0], FileMode.Open);

            if (null != fs)
            {
                BinaryReader br = new BinaryReader(fs);

                nSaveLevel = br.ReadInt32();
                nSoundEnable = br.ReadInt32();
                int nCount = br.ReadInt32();

                for (int i = 0; i < nCount; i++)
                    nClearType[i] = br.ReadInt32();

                br.Close();
                fs.Close();
                return;
            }

        }

        nSaveLevel = 1;
        nSoundEnable = 1;
    }

    public void SaveData()
    {
        if (nSaveLevel < nCurLevel)
            nSaveLevel = nCurLevel;

        StringBuilder sb = new StringBuilder(Application.persistentDataPath);
        sb.Append("/info.inf");

        FileStream fs = new FileStream(sb.ToString(), FileMode.Create);

        if (null == fs)
            return;

        fs.Seek(0, SeekOrigin.Begin);

        BinaryWriter bw = new BinaryWriter(fs);

        bw.Write(nSaveLevel);
        bw.Write(nSoundEnable);
        bw.Write(nLevelCount);
        for (int i = 0; i < nLevelCount; i++)
            bw.Write(nClearType[i]);

        bw.Close();
        fs.Close();
    }
}
