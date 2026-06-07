using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class UIManager : MonoBehaviour
{
    public enum eLevelClearType
    {
        eLevelClearType_None = 0,
        eLevelClearType_Gold,
        eLevelClearType_Silver,
        eLevelClearType_Bronze
    }

    static UIManager m_instance;
    public static UIManager Instance { get { return m_instance; } }

    private int m_nLevelBuff = 0;
    private eGameState m_eOldState = eGameState.eGameState_Logo;
    private bool m_bPauseTime = false;
    private float m_fStartTime = 0.0f;
    private float m_fPauseTime = 0.0f;
    private bool m_bHelpMsgBoxNext = false;

    public bool bPauseTime { get { return m_bPauseTime; } }

    public int nGameTime = 0;
    public eLevelClearType eClearType = eLevelClearType.eLevelClearType_None;

    public PlayUIElements ui = new PlayUIElements();

    private int m_nCurrentJumps = 10;

    void Awake()
    {
        m_instance = this;
        AutoAssignComponents();
    }

    void Start()
    {
        // 미지정 컴포넌트 자동 복구 및 할당 (Awake에서 수행하나 안전 보완용 호출 유지)
        AutoAssignComponents();

        if (ui.textPlayInfo != null) ui.textPlayInfo.gameObject.SetActive(false);
        if (ui.textTime != null) ui.textTime.gameObject.SetActive(false);
        if (ui.texTimeIcon != null) ui.texTimeIcon.gameObject.SetActive(false);
        if (ui.textSelectLevel != null) ui.textSelectLevel.gameObject.SetActive(false);
        if (ui.texNextBtnBg != null) ui.texNextBtnBg.gameObject.SetActive(false);
        if (ui.btnNext != null) ui.btnNext.gameObject.SetActive(false);
        if (ui.btnBack != null) ui.btnBack.gameObject.SetActive(false);
        if (ui.goBtnSound != null) ui.goBtnSound.SetActive(false);
        if (ui.goBtnRetry != null) ui.goBtnRetry.SetActive(false);
        if (ui.goLevelSelecter != null) ui.goLevelSelecter.SetActive(false);
        CloseMsgBox();
        CloseHelpMsgBox();

        if (ui.texLogo != null) ui.texLogo.transform.localPosition = new Vector3(0.0f, 480.0f * 0.2f, 0.0f);
        if (ui.textTouchScreen != null) ui.textTouchScreen.transform.localPosition = new Vector3(0.0f, -480.0f * 0.25f, 0.0f);

        // sound btn
        if (ui.btnBack != null && ui.goBtnSound != null)
        {
            Vector3 vBtnSound = ui.btnBack.transform.localPosition;
            vBtnSound.x -= 44.0f;
            ui.goBtnSound.transform.localPosition = vBtnSound;
        }

        // retry btn
        if (ui.btnBack != null && ui.goBtnRetry != null)
        {
            Vector3 vBtnRetry = ui.btnBack.transform.localPosition;
            vBtnRetry.y -= 44.0f;
            ui.goBtnRetry.transform.localPosition = vBtnRetry;
        }

        // time icon
        if (ui.textTime != null && ui.texTimeIcon != null)
        {
            Vector3 vTimeIcon = ui.textTime.transform.localPosition;
            vTimeIcon.x = ui.textTime.transform.localPosition.x - (ui.textTime.rectTransform.sizeDelta.x * 0.5f) - (ui.texTimeIcon.rectTransform.sizeDelta.x * 0.5f);
            vTimeIcon.y = ui.textTime.transform.localPosition.y;
            ui.texTimeIcon.transform.localPosition = vTimeIcon;
        }
    }

    void Update()
    {
        if (MainManager.Instance != null && eGameState.eGameState_Play == MainManager.Instance.eCurState)
        {
            if (ui.goHelpMsgBox != null && true == ui.goHelpMsgBox.activeInHierarchy)
                return;

            if (MapManager.Instance != null && CameraManager.Instance != null && CameraManager.Instance.Target != null)
            {
                Player player = CameraManager.Instance.Target.GetComponent<Player>();
                if (player != null)
                {
                    SetPlayStats(MapManager.Instance.TotalCoinsCollected, player.JumpCount);
                }
            }
        }
        else
            return;

        // Jumps UI warning pulse effect (Under 3 jumps left)
        if (ui.textJumps != null && ui.textJumps.gameObject.activeInHierarchy)
        {
            if (m_nCurrentJumps <= 3)
            {
                float pulse = 1.0f + Mathf.Abs(Mathf.Sin(Time.realtimeSinceStartup * 10f)) * 0.25f;
                ui.textJumps.transform.localScale = new Vector3(pulse, pulse, 1f);
                ui.textJumps.color = Color.red;
            }
            else
            {
                ui.textJumps.transform.localScale = Vector3.one;
                ui.textJumps.color = Color.white;
            }
        }

        // update time
        float fCurTime = Time.realtimeSinceStartup;
        nGameTime = (int)(fCurTime - m_fStartTime);

        string strTimeRes = string.Empty;

        int idx = m_nLevelBuff - 1;
        int nTime_gold = 0;
        int nTime_silver = 0;
        int nTime_bronze = 0;

        if (MainManager.Instance != null)
        {
            if (MainManager.Instance.nTime_gold != null && idx >= 0 && idx < MainManager.Instance.nTime_gold.Length)
                nTime_gold = MainManager.Instance.nTime_gold[idx];
            if (MainManager.Instance.nTime_silver != null && idx >= 0 && idx < MainManager.Instance.nTime_silver.Length)
                nTime_silver = MainManager.Instance.nTime_silver[idx];
            if (MainManager.Instance.nTime_bronze != null && idx >= 0 && idx < MainManager.Instance.nTime_bronze.Length)
                nTime_bronze = MainManager.Instance.nTime_bronze[idx];
        }

        int nMin = 0;
        int nSec = 0;

        if (nGameTime <= nTime_gold)
        {
            if (ui.texTimeIcon != null && false == ui.texTimeIcon.gameObject.activeInHierarchy)
                ui.texTimeIcon.gameObject.SetActive(true);

            if (ui.texTimeIcon != null && eLevelClearType.eLevelClearType_Gold != eClearType)
                ui.texTimeIcon.texture = Resources.Load("UI/ui_time_gold") as Texture;

            nMin = nTime_gold / 60;
            nSec = nTime_gold % 60;
            strTimeRes = string.Format("{0:D2}", nMin) + string.Format(":{0:D2}", nSec);
            if (ui.textTime != null) ui.textTime.color = Color.yellow;
            eClearType = eLevelClearType.eLevelClearType_Gold;
        }
        else if (nGameTime <= nTime_silver)
        {
            if (ui.texTimeIcon != null && eLevelClearType.eLevelClearType_Silver != eClearType)
                ui.texTimeIcon.texture = Resources.Load("UI/ui_time_silver") as Texture;

            nMin = nTime_silver / 60;
            nSec = nTime_silver % 60;
            strTimeRes = string.Format("{0:D2}", nMin) + string.Format(":{0:D2}", nSec);
            if (ui.textTime != null) ui.textTime.color = Color.white;
            eClearType = eLevelClearType.eLevelClearType_Silver;
        }
        else if (nGameTime <= nTime_bronze)
        {
            if (ui.texTimeIcon != null && eLevelClearType.eLevelClearType_Bronze != eClearType)
                ui.texTimeIcon.texture = Resources.Load("UI/ui_time_bronze") as Texture;

            nMin = nTime_bronze / 60;
            nSec = nTime_bronze % 60;
            strTimeRes = string.Format("{0:D2}", nMin) + string.Format(":{0:D2}", nSec);
            if (ui.textTime != null) ui.textTime.color = new Color(1.0f, 0.6823f, 0.0f);
            eClearType = eLevelClearType.eLevelClearType_Bronze;
        }
        else
        {
            if (ui.texTimeIcon != null) ui.texTimeIcon.gameObject.SetActive(false);
            strTimeRes = "--:--";
            if (ui.textTime != null) ui.textTime.color = Color.red;
            eClearType = eLevelClearType.eLevelClearType_None;
        }

        nMin = nGameTime / 60;
        nSec = nGameTime % 60;
        string strTime = string.Format("\n{0:D2}", nMin) + string.Format(":{0:D2}", nSec);
        if (ui.textTime != null) ui.textTime.text = strTimeRes + strTime;
    }

    public void SetPlayInfo(int nLevel, int nCoin, int nJumps)
    {
        if (ui.textPlayInfo != null && false == ui.textPlayInfo.gameObject.activeInHierarchy)
            ui.textPlayInfo.gameObject.SetActive(true);

        m_nLevelBuff = nLevel;

        SetPlayStats(nCoin, nJumps);
    }

    public void SetPlayInfo(int nLevel, int nCoin)
    {
        SetPlayInfo(nLevel, nCoin, 10);
    }

    public void SetPlayInfo(int nCoin)
    {
        SetPlayInfo(nCoin, 10);
    }

    public void SetPlayStats(int nCoin, int nJumps)
    {
        if (ui.textJumps == null || ui.textPlayInfo == null)
        {
            AutoAssignComponents();
        }

        m_nCurrentJumps = nJumps;

        string strLevel = "Level " + m_nLevelBuff.ToString();
        string strJewel = string.Format("Jewel {0:n0}", nCoin);

        if (ui.textJumps != null)
        {
            if (ui.textPlayInfo != null) ui.textPlayInfo.text = strLevel + "\n" + strJewel;
            ui.textJumps.text = string.Format("Jumps {0:n0}", nJumps);
            if (false == ui.textJumps.gameObject.activeInHierarchy)
            {
                ui.textJumps.gameObject.SetActive(true);
            }
        }
        else
        {
            string strJumps = string.Format("Jumps {0:n0}", nJumps);
            if (ui.textPlayInfo != null) ui.textPlayInfo.text = strLevel + "\n" + strJewel + "\n" + strJumps;
        }
    }

    public void StartTime()
    {
        if (ui.textTime != null && false == ui.textTime.gameObject.activeInHierarchy)
            ui.textTime.gameObject.SetActive(true);

        if (ui.texTimeIcon != null && true == ui.texTimeIcon.gameObject.activeInHierarchy)
            ui.texTimeIcon.gameObject.SetActive(false);

        m_fStartTime = Time.realtimeSinceStartup;
        nGameTime = 0;

        if (ui.textTime != null)
        {
            ui.textTime.text = "00:00\n00:00";
            ui.textTime.color = Color.white;
        }
    }

    public void PauseTime(bool bPause)
    {
        if (true == bPause)
        {
            if (m_bPauseTime != bPause)
            {
                m_fPauseTime = Time.realtimeSinceStartup;
                m_bPauseTime = bPause;
            }
        }
        else
        {
            if (ui != null && 
                (ui.goHelpMsgBox == null || false == ui.goHelpMsgBox.activeInHierarchy) && 
                (ui.goMsgBox == null || false == ui.goMsgBox.activeInHierarchy))
            {
                if (m_bPauseTime != bPause)
                {
                    float fTime = Time.realtimeSinceStartup;
                    m_fStartTime = m_fStartTime + (fTime - m_fPauseTime);
                    m_bPauseTime = bPause;
                }
            }
        }
    }

    public void OpenMsgBox(string strMsg)
    {
        if (ui.textMsgBox != null) ui.textMsgBox.text = strMsg;
        if (ui.goMsgBox != null) ui.goMsgBox.SetActive(true);

        if (ui.texMsgBoxBg != null) ui.texMsgBoxBg.gameObject.SetActive(true);
        if (ui.texNextBtnBg != null && true == ui.texNextBtnBg.gameObject.activeInHierarchy)
            ui.texNextBtnBg.gameObject.SetActive(false);
        if (ui.texHelpMsgBoxBg != null && true == ui.texHelpMsgBoxBg.gameObject.activeInHierarchy)
            ui.texHelpMsgBoxBg.gameObject.SetActive(false);

        PauseTime(true);
    }

    public void CloseMsgBox()
    {
        if (ui.goMsgBox != null) ui.goMsgBox.SetActive(false);
        if (ui.texMsgBoxBg != null) ui.texMsgBoxBg.gameObject.SetActive(false);

        if (ui.btnNext != null && true == ui.btnNext.gameObject.activeInHierarchy && ui.texNextBtnBg != null)
            ui.texNextBtnBg.gameObject.SetActive(true);
        if (ui.goHelpMsgBox != null && true == ui.goHelpMsgBox.activeInHierarchy && ui.texHelpMsgBoxBg != null)
            ui.texHelpMsgBoxBg.gameObject.SetActive(true);

        PauseTime(false);
    }

    public void OpenHelpMsgBox_1(int nLevel)
    {
        PauseTime(true);

        if (1 == nLevel)
        {
            if (ui.textTimeInfo != null) ui.textTimeInfo.text = "";
            if (ui.texHelpMsgBox != null) ui.texHelpMsgBox.texture = Resources.Load("UI/help_1") as Texture;

            if (ui.goHelpMsgBox != null) ui.goHelpMsgBox.SetActive(true);
            if (ui.texHelpMsgBoxBg != null) ui.texHelpMsgBoxBg.gameObject.SetActive(true);

            m_bHelpMsgBoxNext = true;
        }
        else
        {
            m_bHelpMsgBoxNext = false;
            OpenHelpMsgBox_2(nLevel);
        }
    }

    public void OpenHelpMsgBox_2(int nLevel)
    {
        if (MainManager.Instance == null) return;
        int nTime_gold = MainManager.Instance.nTime_gold[nLevel - 1];
        int nTime_silver = MainManager.Instance.nTime_silver[nLevel - 1];
        int nTime_bronze = MainManager.Instance.nTime_bronze[nLevel - 1];
        int nMin = 0;
        int nSec = 0;

        nMin = nTime_gold / 60;
        nSec = nTime_gold % 60;
        string strTime_gold = string.Format("{0:D2}", nMin) + string.Format(":{0:D2}", nSec);

        nMin = nTime_silver / 60;
        nSec = nTime_silver % 60;
        string strTime_silver = string.Format("{0:D2}", nMin) + string.Format(":{0:D2}", nSec);

        nMin = nTime_bronze / 60;
        nSec = nTime_bronze % 60;
        string strTime_bronze = string.Format("{0:D2}", nMin) + string.Format(":{0:D2}", nSec);

        if (ui.textTimeInfo != null) ui.textTimeInfo.text = strTime_gold + "\n\n" + strTime_silver + "\n\n" + strTime_bronze;

        if (ui.texHelpMsgBox != null) ui.texHelpMsgBox.texture = Resources.Load("UI/help_msgbox") as Texture;
        if (ui.goHelpMsgBox != null) ui.goHelpMsgBox.SetActive(true);
        if (ui.texHelpMsgBoxBg != null) ui.texHelpMsgBoxBg.gameObject.SetActive(true);
    }

    public void CloseHelpMsgBox()
    {
        if (true == m_bHelpMsgBoxNext)
        {
            if (ui.goHelpMsgBox != null) ui.goHelpMsgBox.SetActive(false);
            if (ui.texHelpMsgBoxBg != null) ui.texHelpMsgBoxBg.gameObject.SetActive(false);
            OpenHelpMsgBox_2(m_nLevelBuff);

            m_bHelpMsgBoxNext = false;
        }
        else
        {
            if (ui.goHelpMsgBox != null) ui.goHelpMsgBox.SetActive(false);
            if (ui.texHelpMsgBoxBg != null) ui.texHelpMsgBoxBg.gameObject.SetActive(false);
            PauseTime(false);
        }
    }

    public void ConformBackBtn()
    {
        if (ui.goHelpMsgBox != null && true == ui.goHelpMsgBox.activeInHierarchy)
        {
            CloseHelpMsgBox();
            return;
        }

        if (MainManager.Instance != null && eGameState.eGameState_Pause != MainManager.Instance.eCurState)
            m_eOldState = MainManager.Instance.eCurState;

        if (MainManager.Instance != null && eGameState.eGameState_Logo == MainManager.Instance.eCurState)
        {
            Util.Quit();
        }
        else if (MainManager.Instance != null && eGameState.eGameState_Select == MainManager.Instance.eCurState)
        {
            OpenMsgBox("Exit Game ?");
            MainManager.Instance.eCurState = eGameState.eGameState_Pause;
        }
        else if (MainManager.Instance != null && eGameState.eGameState_Play == MainManager.Instance.eCurState)
        {
            OpenMsgBox("Exit Level ?");
            MainManager.Instance.eCurState = eGameState.eGameState_Pause;
        }
        else if (MainManager.Instance != null && eGameState.eGameState_Result == MainManager.Instance.eCurState)
        {
            OpenMsgBox("Exit Level ?");
            MainManager.Instance.eCurState = eGameState.eGameState_Pause;
        }
        else if (MainManager.Instance != null && eGameState.eGameState_Pause == MainManager.Instance.eCurState)
        {
            CloseMsgBox();
            MainManager.Instance.eCurState = m_eOldState;
        }
    }

    public void CreateLevelSelectUI()
    {
        if (ui.textSelectLevel != null) ui.textSelectLevel.gameObject.SetActive(true);
        if (ui.goLevelSelecter != null) ui.goLevelSelecter.SetActive(true);
    }

    public void ApplySoundButton()
    {
        if (ui.goBtnSound == null) return;

        if (MainManager.Instance != null && 0 == MainManager.Instance.nSoundEnable)
        {
            RawImage tex = ui.goBtnSound.GetComponent<RawImage>();
            if (tex != null) tex.texture = Resources.Load("UI/sound_off") as Texture;
            AudioManager.Instance.StopBgm();
        }
        else
        {
            RawImage tex = ui.goBtnSound.GetComponent<RawImage>();
            if (tex != null) tex.texture = Resources.Load("UI/sound_on") as Texture;
            AudioManager.Instance.PlayBgm("Sound/bgm");
        }
    }

    #region button message
    public void onBtnNext()
    {
        if (MainManager.Instance != null && MainManager.Instance.IsTransitioning) return;
        AudioManager.Instance.Play("Sound/ui_button_down");

        if (ui.btnNext != null) ui.btnNext.gameObject.SetActive(false);
        if (ui.texNextBtnBg != null) ui.texNextBtnBg.gameObject.SetActive(false);

        if ((int)eLevelClearType.eLevelClearType_None == (int)MainManager.lastClearType)
        {
            if (MainManager.Instance != null)
                MainManager.Instance.StartLevel(MainManager.nCurLevelStatic);
        }
        else
        {
            if (MainManager.Instance != null)
            {
                if (MainManager.nCurLevelStatic == MainManager.Instance.nLevelCount)
                {
                    m_eOldState = eGameState.eGameState_Select;
                    if (ui.textPlayInfo != null) ui.textPlayInfo.gameObject.SetActive(false);
                    if (ui.textTime != null) ui.textTime.gameObject.SetActive(false);
                    if (ui.texTimeIcon != null) ui.texTimeIcon.gameObject.SetActive(false);
                    if (ui.goBtnRetry != null) ui.goBtnRetry.SetActive(false);
                    MainManager.Instance.GoLevelSelectScene();
                }
                else
                {
                    MainManager.nCurLevelStatic++;
                    MainManager.Instance.StartLevel(MainManager.nCurLevelStatic);
                }
            }
        }
    }

    public void onBtnBack()
    {
        AudioManager.Instance.Play("Sound/ui_button_down");
        ConformBackBtn();
    }

    public void onBtnSound()
    {
        AudioManager.Instance.Play("Sound/ui_button_down");

        if (MainManager.Instance != null)
        {
            if (0 == MainManager.Instance.nSoundEnable)
            {
                MainManager.Instance.nSoundEnable = 1;
                ApplySoundButton();
            }
            else
            {
                MainManager.Instance.nSoundEnable = 0;
                ApplySoundButton();
            }
        }
    }

    public void onBtnRetry()
    {
        if (MainManager.Instance != null && MainManager.Instance.IsTransitioning) return;
        AudioManager.Instance.Play("Sound/ui_button_down");
        if (MainManager.Instance != null)
        {
            MainManager.Instance.StartLevel(MainManager.nCurLevelStatic);
        }
    }

    public void onBtnNo()
    {
        AudioManager.Instance.Play("Sound/ui_button_down");

        CloseMsgBox();
        if (MainManager.Instance != null)
        {
            MainManager.Instance.eCurState = m_eOldState;
        }
    }

    public void onBtnYes()
    {
        if (MainManager.Instance != null && MainManager.Instance.IsTransitioning) return;
        AudioManager.Instance.Play("Sound/ui_button_down");

        CloseMsgBox();

        if (eGameState.eGameState_Logo == m_eOldState || eGameState.eGameState_Select == m_eOldState)
        {
            if (MainManager.Instance != null) MainManager.Instance.SaveData();
            Util.Quit();
        }
        else if (eGameState.eGameState_Play == m_eOldState || eGameState.eGameState_Result == m_eOldState)
        {
            m_eOldState = eGameState.eGameState_Select;
            if (ui.textPlayInfo != null) ui.textPlayInfo.gameObject.SetActive(false);
            if (ui.textTime != null) ui.textTime.gameObject.SetActive(false);
            if (ui.texTimeIcon != null) ui.texTimeIcon.gameObject.SetActive(false);
            if (ui.btnNext != null) ui.btnNext.gameObject.SetActive(false);
            if (ui.texNextBtnBg != null) ui.texNextBtnBg.gameObject.SetActive(false);
            if (ui.goBtnRetry != null) ui.goBtnRetry.SetActive(false);
            if (MainManager.Instance != null) MainManager.Instance.GoLevelSelectScene();
        }
        else
        {
            if (MainManager.Instance != null) MainManager.Instance.eCurState = m_eOldState;
        }
    }

    public void onBtnHelpOk()
    {
        AudioManager.Instance.Play("Sound/ui_button_down");
        CloseHelpMsgBox();
    }

    public void SetupResultScreen()
    {
        if (ui.btnNext != null) ui.btnNext.gameObject.SetActive(true);
        if (ui.texMsgBoxBg != null && false == ui.texMsgBoxBg.gameObject.activeInHierarchy && ui.texNextBtnBg != null)
            ui.texNextBtnBg.gameObject.SetActive(true);

        if ((int)eLevelClearType.eLevelClearType_None == (int)MainManager.lastClearType)
        {
            AudioManager.Instance.Play("Sound/fail", 0.3f);
            if (ui.textNext != null) ui.textNext.text = "Retry";
            if (ui.texNext != null) ui.texNext.texture = Resources.Load("UI/retry_bg") as Texture;

            if (ui.texResultIcon != null)
            {
                ui.texResultIcon.enabled = true;
                ui.texResultIcon.texture = Resources.Load("UI/ui_time_bronze") as Texture;
            }
            if (ui.textResultTime != null)
            {
                ui.textResultTime.enabled = true;
                int nMin = MainManager.lastGameTime / 60;
                int nSec = MainManager.lastGameTime % 60;
                ui.textResultTime.text = string.Format("{0:D2}", nMin) + string.Format(":{0:D2}", nSec);
            }
        }
        else
        {
            AudioManager.Instance.Play("Sound/clear");
            if (ui.texNext != null) ui.texNext.texture = Resources.Load("UI/done_bg") as Texture;

            if (ui.textNext != null)
            {
                if (MainManager.nCurLevelStatic == MainManager.Instance.nLevelCount)
                    ui.textNext.text = "Clear!";
                else
                    ui.textNext.text = "Done";
            }

            if (ui.texResultIcon != null) ui.texResultIcon.enabled = true;
            if (ui.textResultTime != null)
            {
                ui.textResultTime.enabled = true;
                int nMin = MainManager.lastGameTime / 60;
                int nSec = MainManager.lastGameTime % 60;
                ui.textResultTime.text = string.Format("{0:D2}", nMin) + string.Format(":{0:D2}", nSec);
            }

            if (ui.texResultIcon != null)
            {
                if ((int)eLevelClearType.eLevelClearType_Gold == (int)MainManager.lastClearType)
                    ui.texResultIcon.texture = Resources.Load("UI/ui_time_gold") as Texture;
                else if ((int)eLevelClearType.eLevelClearType_Silver == (int)MainManager.lastClearType)
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

    #endregion button message

    private void AutoAssignComponents()
    {
        if (ui == null) ui = new PlayUIElements();

        // 카메라
        if (ui.uiCamera == null) ui.uiCamera = FindAnyObjectByType<Camera>();

        // UI 텍스트 및 원본 텍스처들
        if (ui.texLogo == null) ui.texLogo = FindChildByName<RawImage>("texLogo");
        if (ui.textPlayInfo == null) ui.textPlayInfo = FindChildByName<Text>("textPlayInfo");
        if (ui.textTime == null) ui.textTime = FindChildByName<Text>("textTime");
        if (ui.textTouchScreen == null) ui.textTouchScreen = FindChildByName<Text>("textTouchScreen");
        if (ui.textSelectLevel == null) ui.textSelectLevel = FindChildByName<RawImage>("textSelectLevel");

        // 버튼들
        if (ui.btnNext == null) ui.btnNext = FindChildByName<Button>("btnNext");
        if (ui.btnBack == null) ui.btnBack = FindChildByName<Button>("btnBack");

        // 버튼 하위 텍스트/이미지
        if (ui.btnNext != null)
        {
            if (ui.textNext == null) ui.textNext = ui.btnNext.GetComponentInChildren<Text>();
            if (ui.texNext == null) ui.texNext = ui.btnNext.GetComponentInChildren<RawImage>();
        }
        else
        {
            if (ui.textNext == null) ui.textNext = FindChildByName<Text>("textNext");
            if (ui.texNext == null) ui.texNext = FindChildByName<RawImage>("texNext");
        }

        if (ui.textResultTime == null) ui.textResultTime = FindChildByName<Text>("textResultTime");
        if (ui.texResultIcon == null) ui.texResultIcon = FindChildByName<RawImage>("texResultIcon");
        if (ui.texNextBtnBg == null) ui.texNextBtnBg = FindChildByName<RawImage>("texNextBtnBg");

        // 게임 오브젝트들
        if (ui.goBtnSound == null) ui.goBtnSound = FindChildGameObjectByName("goBtnSound");
        if (ui.goBtnRetry == null) ui.goBtnRetry = FindChildGameObjectByName("goBtnRetry");
        if (ui.goMsgBox == null) ui.goMsgBox = FindChildGameObjectByName("goMsgBox");
        if (ui.goLevelSelecter == null) ui.goLevelSelecter = FindChildGameObjectByName("goLevelSelecter");
        if (ui.goHelpMsgBox == null) ui.goHelpMsgBox = FindChildGameObjectByName("goHelpMsgBox");

        // 메시지 박스 하위
        if (ui.goMsgBox != null)
        {
            if (ui.texMsgBoxBg == null) ui.texMsgBoxBg = ui.goMsgBox.GetComponentInChildren<RawImage>();
            if (ui.textMsgBox == null) ui.textMsgBox = ui.goMsgBox.GetComponentInChildren<Text>();
        }
        else
        {
            if (ui.texMsgBoxBg == null) ui.texMsgBoxBg = FindChildByName<RawImage>("texMsgBoxBg");
            if (ui.textMsgBox == null) ui.textMsgBox = FindChildByName<Text>("textMsgBox");
        }

        // 도움말 메시지 박스 하위
        if (ui.goHelpMsgBox != null)
        {
            if (ui.textTimeInfo == null) ui.textTimeInfo = ui.goHelpMsgBox.GetComponentInChildren<Text>();
            RawImage[] rawImages = ui.goHelpMsgBox.GetComponentsInChildren<RawImage>(true);
            if (rawImages != null)
            {
                foreach (var ri in rawImages)
                {
                    if (ri != null && ri.name != null)
                    {
                        if (ri.name.Contains("texHelpMsgBoxBg") || ri.name.Contains("Bg")) ui.texHelpMsgBoxBg = ri;
                        else if (ri.name.Contains("texHelpMsgBox") || ri.name.Contains("help")) ui.texHelpMsgBox = ri;
                    }
                }
            }
        }
        else
        {
            if (ui.textTimeInfo == null) ui.textTimeInfo = FindChildByName<Text>("textTimeInfo");
            if (ui.texHelpMsgBox == null) ui.texHelpMsgBox = FindChildByName<RawImage>("texHelpMsgBox");
            if (ui.texHelpMsgBoxBg == null) ui.texHelpMsgBoxBg = FindChildByName<RawImage>("texHelpMsgBoxBg");
        }

        if (ui.texTimeIcon == null) ui.texTimeIcon = FindChildByName<RawImage>("texTimeIcon");
        if (ui.textJumps == null) ui.textJumps = FindChildByName<Text>("textJumps");
    }

    private T FindChildByName<T>(string name) where T : Component
    {
        T[] comps = GetComponentsInChildren<T>(true);
        if (comps != null)
        {
            foreach (T comp in comps)
            {
                if (comp != null && comp.name != null)
                {
                    if (comp.name.Equals(name, System.StringComparison.OrdinalIgnoreCase))
                    {
                        return comp;
                    }
                }
            }
            foreach (T comp in comps)
            {
                if (comp != null && comp.name != null)
                {
                    if (comp.name.ToLower().Contains(name.ToLower()))
                    {
                        return comp;
                    }
                }
            }
        }

        // Fallback: 씬 내의 로드된 모든 컴포넌트 검색 (자식 계층구조 외부에 배치된 경우 지원)
        T[] allComps = Resources.FindObjectsOfTypeAll<T>();
        if (allComps != null)
        {
            foreach (T comp in allComps)
            {
                if (comp != null && comp.gameObject != null && comp.gameObject.scene.isLoaded && comp.name != null)
                {
                    if (comp.name.Equals(name, System.StringComparison.OrdinalIgnoreCase))
                    {
                        return comp;
                    }
                }
            }
            foreach (T comp in allComps)
            {
                if (comp != null && comp.gameObject != null && comp.gameObject.scene.isLoaded && comp.name != null)
                {
                    if (comp.name.ToLower().Contains(name.ToLower()))
                    {
                        return comp;
                    }
                }
            }
        }

        return null;
    }

    private GameObject FindChildGameObjectByName(string name)
    {
        Transform[] trans = GetComponentsInChildren<Transform>(true);
        if (trans != null)
        {
            foreach (Transform t in trans)
            {
                if (t != null && t.name != null)
                {
                    if (t.name.Equals(name, System.StringComparison.OrdinalIgnoreCase))
                    {
                        return t.gameObject;
                    }
                }
            }
            foreach (Transform t in trans)
            {
                if (t != null && t.name != null)
                {
                    if (t.name.ToLower().Contains(name.ToLower()))
                    {
                        return t.gameObject;
                    }
                }
            }
        }

        // Fallback: 씬 내의 로드된 모든 트랜스폼 검색 (자식 계층구조 외부에 배치된 경우 지원)
        Transform[] allTrans = Resources.FindObjectsOfTypeAll<Transform>();
        if (allTrans != null)
        {
            foreach (Transform t in allTrans)
            {
                if (t != null && t.gameObject != null && t.gameObject.scene.isLoaded && t.name != null)
                {
                    if (t.name.Equals(name, System.StringComparison.OrdinalIgnoreCase))
                    {
                        return t.gameObject;
                    }
                }
            }
            foreach (Transform t in allTrans)
            {
                if (t != null && t.gameObject != null && t.gameObject.scene.isLoaded && t.name != null)
                {
                    if (t.name.ToLower().Contains(name.ToLower()))
                    {
                        return t.gameObject;
                    }
                }
            }
        }

        return null;
    }
}
