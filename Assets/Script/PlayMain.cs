using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayMain : MonoBehaviour
{
    private static PlayMain m_instance;
    public static PlayMain Instance { get { return m_instance; } }

    [Header("Local Stage Settings")]
    public GameObject goPlayerSrc;
    public GameObject goMainBg;

    private GameObject m_goPlayer;

    void Awake()
    {
        m_instance = this;
    }

    void Start()
    {
        string sceneName = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;
        Debug.Log("[PlayMain Debug] Start method active. Scene Name: " + sceneName);

        if (sceneName == "Play")
        {
            SetupPlayStage(MainManager.nCurLevelStatic);
        }
    }

    void Update()
    {
#if UNITY_ANDROID
		if( true == Input.GetKeyDown( KeyCode.Escape))
		{
			AudioManager.Instance.Play( "Sound/ui_button_down");
			if (UI_Play.Instance != null)
				UI_Play.Instance.ConformBackBtn();
		}
#endif

        if (MainManager.Instance != null && eGameState.eGameState_Logo != MainManager.Instance.eCurState)
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

    void OnApplicationPause(bool pause)
    {
        if (UI_Play.Instance != null)
            UI_Play.Instance.PauseTime(pause);
    }

    private bool m_isGameStarted = false;
    public bool IsGameStarted { get { return m_isGameStarted; } }

    public void SetupPlayStage(int nLevel)
    {
        Debug.Log("[PlayMain Debug] SetupPlayStage called. Level: " + nLevel);
        m_isGameStarted = false;

        if (MainManager.Instance != null)
        {
            MainManager.Instance.eCurState = eGameState.eGameState_Play;
            MainManager.Instance.nCurLevel = nLevel;
        }

        if (MapManager.Instance != null)
        {
            MapManager.Instance.ApplyLevelConfig(nLevel);
        }

        if (null == m_goPlayer)
        {
            m_goPlayer = GameObject.Instantiate(goPlayerSrc) as GameObject;
            CameraManager.Instance.SetTarget(m_goPlayer);
        }

        Player playerComp = m_goPlayer.GetComponent<Player>();
        if (playerComp != null)
        {
            playerComp.ResetJumpCount(MapManager.Instance != null ? MapManager.Instance.InitialJumps : 10);
        }

        Rigidbody playerRb = m_goPlayer.GetComponent<Rigidbody>();
        if (playerRb != null)
        {
            playerRb.linearVelocity = Vector3.zero;
            playerRb.angularVelocity = Vector3.zero;
            playerRb.isKinematic = true; // 대기 상태 동안 물리 중력 작동 멈춤
            playerRb.Sleep();
        }
        m_goPlayer.transform.position = Vector3.zero;
        m_goPlayer.transform.rotation = goPlayerSrc.transform.rotation;
        CameraManager.Instance.Init();
        MapManager.Instance.UnLoadCubeMap();
        MapManager.Instance.LoadCubeMap(nLevel);

        if (UI_Play.Instance != null)
        {
            UI_Play.Instance.SetPlayInfo(nLevel, 0, 10);
            if (UI_Play.Instance.ui != null && UI_Play.Instance.ui.goBtnRetry != null && false == UI_Play.Instance.ui.goBtnRetry.activeInHierarchy)
                UI_Play.Instance.ui.goBtnRetry.SetActive(true);
            UI_Play.Instance.StartTime();
            UI_Play.Instance.PauseTime(true); // 대기 상태 동안 시간 흐름 일시정지

            // UI_Play.Instance.OpenHelpMsgBox_1(nLevel);
        }
    }

    public void StartGame()
    {
        if (m_isGameStarted) return;
        m_isGameStarted = true;
        Debug.Log("[PlayMain Debug] Game officially started by player jump input.");

        if (m_goPlayer != null)
        {
            Rigidbody playerRb = m_goPlayer.GetComponent<Rigidbody>();
            if (playerRb != null)
            {
                playerRb.isKinematic = false;
                playerRb.WakeUp();
            }

            Player player = m_goPlayer.GetComponent<Player>();
            if (player != null)
            {
                player.ExecuteFirstJump();
            }
        }

        if (UI_Play.Instance != null)
        {
            UI_Play.Instance.PauseTime(false); // 게임 기동 시 타이머 재개
        }
    }

    public void CleanUpStage()
    {
        if (m_goPlayer != null)
        {
            Util.MyDestroy(m_goPlayer);
            m_goPlayer = null;
        }
        if (MapManager.Instance != null)
        {
            MapManager.Instance.UnLoadCubeMap();
        }
    }

    public GameObject GetPlayerObject()
    {
        return m_goPlayer;
    }
}
