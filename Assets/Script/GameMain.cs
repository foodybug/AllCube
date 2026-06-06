using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GameMain : MonoBehaviour
{
    static GameMain m_instance;
    public static GameMain Instance { get { return m_instance; } }

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
        Debug.Log("[GameMain Debug] Start method active. Scene Name: " + sceneName);

        if (sceneName == "Play")
        {
            SetupPlayStage(MainManager.nCurLevelStatic);
        }
        else if (sceneName == "Title")
        {
            CleanUpStage();
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

    public void SetupPlayStage(int nLevel)
    {
        Debug.Log("[GameMain Debug] SetupPlayStage called. Level: " + nLevel);
        if (MainManager.Instance != null)
        {
            MainManager.Instance.eCurState = eGameState.eGameState_Play;
            MainManager.Instance.nCurLevel = nLevel;
        }

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

        if (UI_Play.Instance != null)
        {
            UI_Play.Instance.SetPlayInfo(nLevel, 0, 10);
            if (UI_Play.Instance.ui != null && UI_Play.Instance.ui.goBtnRetry != null && false == UI_Play.Instance.ui.goBtnRetry.activeInHierarchy)
                UI_Play.Instance.ui.goBtnRetry.SetActive(true);
            UI_Play.Instance.StartTime();

            UI_Play.Instance.OpenHelpMsgBox_1(nLevel);
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
