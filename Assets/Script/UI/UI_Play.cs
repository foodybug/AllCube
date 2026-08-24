using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class UI_Play : MonoBehaviour
{
    public enum eLevelClearType
    {
        eLevelClearType_None = 0,
        eLevelClearType_Gold,
        eLevelClearType_Silver,
        eLevelClearType_Bronze
    }

    static UI_Play m_instance;
    public static UI_Play Instance { get { return m_instance; } }

    private int m_nLevelBuff = 0;
    private eGameState m_eOldState = eGameState.eGameState_Logo;
    private bool m_bPauseTime = false;
    private float m_fStartTime = 0.0f;
    private float m_fPauseTime = 0.0f;
    private bool m_bHelpMsgBoxNext = false;
    private float m_comboPulseTimer = 0f;
    private float m_jumpsPulseTimer = 0f;
    private float m_jumpsPulseIntensity = 1f;
    private int m_lastDisplayCombo = 0;

    public bool bPauseTime { get { return m_bPauseTime; } }

    public int nGameTime = 0;
    public eLevelClearType eClearType = eLevelClearType.eLevelClearType_None;

    public PlayUIElements ui = new PlayUIElements();

    private int m_nCurrentJumps = 10;
    private int m_nMaxHeightThisRun = 0;
    public int MaxHeightThisRun { get { return m_nMaxHeightThisRun; } }

    private int m_lastCoin = -1;
    private int m_lastCurrentHeight = -1;
    private int m_lastJumps = -1;

    void Awake()
    {
        m_instance = this;
        AutoAssignComponents();
    }

    void Start()
    {
        // 미지정 컴포넌트 자동 복구 및 9:16 종횡비 / CanvasScaler 정밀 보정
        AutoAssignComponents();
        EnsurePlayCanvasAndCameraAspect();

        if (ui.textPlayInfo != null) ui.textPlayInfo.gameObject.SetActive(false);
        if (ui.textHeight != null) ui.textHeight.gameObject.SetActive(false);
        if (ui.textTime != null) ui.textTime.gameObject.SetActive(false);
        if (ui.texTimeIcon != null) ui.texTimeIcon.gameObject.SetActive(false);
        if (ui.textCombo != null) ui.textCombo.gameObject.SetActive(false);
        m_lastDisplayCombo = 0;
        m_comboPulseTimer = 0f;
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
        EnsurePlayCanvasAndCameraAspect();
        bool isPlayState = false;
        if (MainManager.Instance != null)
        {
            isPlayState = (eGameState.eGameState_Play == MainManager.Instance.eCurState);
        }
        else
        {
            isPlayState = (UnityEngine.SceneManagement.SceneManager.GetActiveScene().name == "Play");
        }

        if (isPlayState)
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
            UpdatePulseEffects();
        }
        else
            return;

        // Height UI Best record breaking pulse & color shift effect
        int levelIdx = m_nLevelBuff - 1;
        int bestHeight = 0;
        if (MainManager.Instance != null && MainManager.Instance.nBestHeight != null && levelIdx >= 0 && levelIdx < MainManager.Instance.nBestHeight.Length)
        {
            bestHeight = MainManager.Instance.nBestHeight[levelIdx];
        }

        bool isBreakingRecord = (m_nMaxHeightThisRun == bestHeight && bestHeight > 0);

        if (ui.textHeight != null && ui.textHeight.gameObject.activeInHierarchy)
        {
            if (isBreakingRecord)
            {
                float pulse = 1.0f + Mathf.Abs(Mathf.Sin(Time.realtimeSinceStartup * 6f)) * 0.15f;
                ui.textHeight.transform.localScale = new Vector3(pulse, pulse, 1f);
                ui.textHeight.color = Color.yellow;
            }
            else
            {
                ui.textHeight.transform.localScale = Vector3.one;
                ui.textHeight.color = Color.white;
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

    private Vector3 m_initialJumpsScale = Vector3.one;
    private Vector3 m_initialComboScale = Vector3.one;
    private int m_baseComboFontSize = 28;
    private bool m_isPlayUiScalesCached = false;

    private void CachePlayUiScales()
    {
        if (m_isPlayUiScalesCached) return;
        if (ui.textJumps != null) m_initialJumpsScale = ui.textJumps.transform.localScale;
        if (ui.textCombo != null)
        {
            m_initialComboScale = ui.textCombo.transform.localScale;
            m_baseComboFontSize = ui.textCombo.fontSize;
            ui.textCombo.verticalOverflow = VerticalWrapMode.Overflow;
            ui.textCombo.horizontalOverflow = HorizontalWrapMode.Overflow;
        }
        m_isPlayUiScalesCached = true;
    }

    private void UpdatePulseEffects()
    {
        CachePlayUiScales();

        // Jumps UI pulse effect (Gain pulse or Low Jumps warning pulse)
        if (ui.textJumps != null && ui.textJumps.gameObject.activeInHierarchy)
        {
            if (m_jumpsPulseTimer > 0f)
            {
                m_jumpsPulseTimer -= Time.deltaTime;
                float progress = Mathf.Max(0f, m_jumpsPulseTimer) / 0.22f;
                float pulse = 1.0f + progress * 0.10f * m_jumpsPulseIntensity; // 아주 살짝(약 10%~13%)만 커지는 미세하고 은은한 연출
                ui.textJumps.transform.localScale = new Vector3(m_initialJumpsScale.x * pulse, m_initialJumpsScale.y * pulse, m_initialJumpsScale.z);
                ui.textJumps.color = Color.white;
            }
            else if (m_nCurrentJumps <= 3)
            {
                float pulse = 1.0f + Mathf.Abs(Mathf.Sin(Time.realtimeSinceStartup * 10f)) * 0.25f;
                ui.textJumps.transform.localScale = new Vector3(m_initialJumpsScale.x * pulse, m_initialJumpsScale.y * pulse, m_initialJumpsScale.z);
                ui.textJumps.color = Color.red;
            }
            else
            {
                ui.textJumps.transform.localScale = m_initialJumpsScale;
                ui.textJumps.color = Color.white;
            }
        }

        // Combo text pulse & color cycle animation
        if (m_comboPulseTimer > 0f && ui.textCombo != null)
        {
            m_comboPulseTimer -= Time.deltaTime;
            float pulse = 1.0f + Mathf.Max(0f, m_comboPulseTimer) * 0.3f;
            ui.textCombo.transform.localScale = new Vector3(m_initialComboScale.x * pulse, m_initialComboScale.y * pulse, m_initialComboScale.z);
            ui.textCombo.color = Color.Lerp(Color.yellow, Color.red, Mathf.PingPong(Time.time * 10f, 1f));
        }
        else if (ui.textCombo != null)
        {
            ui.textCombo.transform.localScale = m_initialComboScale;
            ui.textCombo.color = Color.yellow;
        }
    }

    public void SetPlayInfo(int nLevel, int nCoin, int nJumps)
    {
        if (ui.textPlayInfo != null && false == ui.textPlayInfo.gameObject.activeInHierarchy)
            ui.textPlayInfo.gameObject.SetActive(true);
        if (ui.textHeight != null && false == ui.textHeight.gameObject.activeInHierarchy)
            ui.textHeight.gameObject.SetActive(true);
        if (ui.textCombo != null)
        {
            if (m_isPlayUiScalesCached) ui.textCombo.fontSize = m_baseComboFontSize;
            ui.textCombo.gameObject.SetActive(false);
        }

        m_nLevelBuff = nLevel;
        m_nMaxHeightThisRun = 0;
        m_lastDisplayCombo = 0;
        m_comboPulseTimer = 0f;

        SetPlayStats(nCoin, nJumps);
    }

    public void SetPlayInfo(int nLevel, int nCoin)
    {
        SetPlayInfo(nLevel, nCoin, 10);
    }

    public void SetPlayInfo(int nCoin)
    {
        SetPlayInfo(m_nLevelBuff, nCoin, 10);
    }

    public void SetPlayStats(int nCoin, int nJumps)
    {
        if (ui.textJumps == null || ui.textPlayInfo == null)
        {
            AutoAssignComponents();
        }

        if (ui.textPlayInfo != null) ui.textPlayInfo.verticalOverflow = VerticalWrapMode.Overflow;
        if (ui.textHeight != null) ui.textHeight.verticalOverflow = VerticalWrapMode.Overflow;
        if (ui.textJumps != null) ui.textJumps.verticalOverflow = VerticalWrapMode.Overflow;

        m_nCurrentJumps = nJumps;

        // 최대로 올라간 높이 계산 (1 unit = 1m)
        float playerY = 0f;
        if (CameraManager.Instance != null && CameraManager.Instance.Target != null)
        {
            playerY = CameraManager.Instance.Target.position.y;
        }
        int currentHeight = Mathf.Max(0, Mathf.FloorToInt(playerY));

        // 수치가 실제로 변했을 때만 UI 텍스트 갱신 (매 프레임 힙 가비지 생성 완전 차단)
        if (nCoin == m_lastCoin && currentHeight == m_lastCurrentHeight && nJumps == m_lastJumps)
        {
            return;
        }

        // 점프 횟수가 획득/증가했을 때 은은하고 살짝 튀어 오르는 연출 기동
        if (m_lastJumps >= 0 && nJumps > m_lastJumps)
        {
            int jumpDelta = nJumps - m_lastJumps;
            m_jumpsPulseTimer = 0.22f; // 은은하게 0.22초 기동
            m_jumpsPulseIntensity = Mathf.Min(1.3f, 1.0f + jumpDelta * 0.05f); // 미세한 강도 차이 (최대 1.3배)
        }

        m_lastCoin = nCoin;
        m_lastCurrentHeight = currentHeight;
        m_lastJumps = nJumps;

        if (currentHeight > m_nMaxHeightThisRun)
        {
            m_nMaxHeightThisRun = currentHeight;
            int levelIdx = m_nLevelBuff - 1;
            if (MainManager.Instance != null && MainManager.Instance.nBestHeight != null && levelIdx >= 0 && levelIdx < MainManager.Instance.nBestHeight.Length)
            {
                if (m_nMaxHeightThisRun > MainManager.Instance.nBestHeight[levelIdx])
                {
                    MainManager.Instance.nBestHeight[levelIdx] = m_nMaxHeightThisRun;
                }
            }
        }

        int allTimeBest = 0;
        int displayLevelIdx = m_nLevelBuff - 1;
        if (MainManager.Instance != null && MainManager.Instance.nBestHeight != null && displayLevelIdx >= 0 && displayLevelIdx < MainManager.Instance.nBestHeight.Length)
        {
            allTimeBest = MainManager.Instance.nBestHeight[displayLevelIdx];
        }

        string strLevel = "Level " + m_nLevelBuff.ToString();
        string strJewel = string.Format("Jewel {0:n0}", nCoin);
        string strHeight = string.Format("Height\n<size=36>{0}m</size>", currentHeight);
        string strJumps = string.Format("Jumps\n<size=36>{0:n0}</size>", nJumps);

        if (ui.textHeight != null)
        {
            ui.textHeight.supportRichText = true;
            ui.textHeight.text = strHeight;
            if (false == ui.textHeight.gameObject.activeInHierarchy)
            {
                ui.textHeight.gameObject.SetActive(true);
            }

            if (ui.textJumps != null)
            {
                ui.textJumps.supportRichText = true;
                if (ui.textPlayInfo != null) ui.textPlayInfo.text = strLevel + "\n" + strJewel;
                ui.textJumps.text = strJumps;
                if (false == ui.textJumps.gameObject.activeInHierarchy)
                {
                    ui.textJumps.gameObject.SetActive(true);
                }
            }
            else
            {
                if (ui.textPlayInfo != null) ui.textPlayInfo.text = strLevel + "\n" + strJewel + "\n" + strJumps;
            }
        }
        else
        {
            if (ui.textJumps != null)
            {
                ui.textJumps.supportRichText = true;
                if (ui.textPlayInfo != null) ui.textPlayInfo.text = strLevel + "\n" + strJewel + "\n" + strHeight;
                ui.textJumps.text = strJumps;
                if (false == ui.textJumps.gameObject.activeInHierarchy)
                {
                    ui.textJumps.gameObject.SetActive(true);
                }
            }
            else
            {
                if (ui.textPlayInfo != null) ui.textPlayInfo.text = strLevel + "\n" + strJewel + "\n" + strHeight + "\n" + strJumps;
            }
        }
    }

    private void SetupComboPosition()
    {
        if (ui == null || ui.textCombo == null) return;

        RectTransform rtCombo = ui.textCombo.GetComponent<RectTransform>();
        if (rtCombo != null)
        {
            // 가운데 위쪽 앵커 (Top-Center)
            rtCombo.anchorMin = new Vector2(0.5f, 1.0f);
            rtCombo.anchorMax = new Vector2(0.5f, 1.0f);
            rtCombo.pivot = new Vector2(0.5f, 1.0f);

            float targetY = -110f; // 기존 -90f에서 20px 아래쪽으로 이동(-110px)
            if (ui.textJumps != null)
            {
                RectTransform rtJumps = ui.textJumps.GetComponent<RectTransform>();
                if (rtJumps != null)
                {
                    targetY = rtJumps.anchoredPosition.y - 65f; // 기존 -45f에서 20px 아래쪽으로 배치(-65px)
                }
            }

            rtCombo.anchoredPosition = new Vector2(0f, targetY);
            ui.textCombo.alignment = TextAnchor.UpperCenter;
            ui.textCombo.transform.localScale = Vector3.one; // 선명한 폰트 출력을 위해 localScale = 1.0 유지가 선명함의 정석
        }
    }

    public void UpdateCombo(int comboCount)
    {
        if (ui.textCombo == null)
        {
            AutoAssignComponents();
        }

        CachePlayUiScales();

        if (ui.textCombo != null)
        {
            if (comboCount > 0)
            {
                int extraSize = comboCount / 3; // 3의 배수마다 1씩 증가
                ui.textCombo.fontSize = m_baseComboFontSize + extraSize;
                ui.textCombo.text = $"COMBO x{comboCount}";
                if (!ui.textCombo.gameObject.activeInHierarchy)
                {
                    ui.textCombo.gameObject.SetActive(true);
                }

                if (comboCount > m_lastDisplayCombo)
                {
                    m_comboPulseTimer = 0.3f; // 0.3초 동안 펄스 효과 기동
                }
            }
            else
            {
                ui.textCombo.fontSize = m_baseComboFontSize; // 콤보 초기화 시 원래 사이즈로 복원
                ui.textCombo.text = "";
                if (ui.textCombo.gameObject.activeInHierarchy)
                {
                    ui.textCombo.gameObject.SetActive(false);
                }
            }
            m_lastDisplayCombo = comboCount;
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

        if (eLevelClearType.eLevelClearType_None == MainManager.lastClearType)
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

    #endregion button message

    private void EnsurePlayCanvasAndCameraAspect()
    {
        Camera cam = CameraManager.GetMainCamera();
        if (cam != null)
        {
            CameraManager.ApplyAspect(cam);
        }
        else
        {
            CameraManager.EnsureBackgroundClearCamera();
        }

        Canvas[] canvases = FindObjectsByType<Canvas>(FindObjectsSortMode.None);
        if (canvases != null)
        {
            foreach (var canvas in canvases)
            {
                if (canvas != null)
                {
                    CanvasScaler scaler = canvas.GetComponent<CanvasScaler>();
                    if (scaler == null) scaler = canvas.gameObject.AddComponent<CanvasScaler>();
                    scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
                    scaler.referenceResolution = new Vector2(720, 1280);
                    scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
                    scaler.matchWidthOrHeight = 0.5f;

                    if (cam != null)
                    {
                        cam.cullingMask |= (1 << LayerMask.NameToLayer("UI")) | (1 << 5);
                    }

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
        if (ui.textHeight == null) ui.textHeight = FindChildByName<Text>("textHeight");


        if (ui.textCombo == null)
        {
            ui.textCombo = FindChildByName<Text>("textCombo");
            if (ui.textCombo == null && ui.textJumps != null)
            {
                GameObject goCombo = GameObject.Instantiate(ui.textJumps.gameObject, ui.textJumps.transform.parent);
                goCombo.name = "textCombo";
                ui.textCombo = goCombo.GetComponent<Text>();


                RectTransform rtJumps = ui.textJumps.GetComponent<RectTransform>();
                RectTransform rtCombo = ui.textCombo.GetComponent<RectTransform>();
                if (rtJumps != null && rtCombo != null)
                {
                    rtCombo.anchoredPosition = rtJumps.anchoredPosition + new Vector2(0f, -60f); // 20px 아래쪽에 생성
                }


                ui.textCombo.transform.localScale = Vector3.one; // 선명한 폰트 출력을 위해 localScale = 1.0 유지
                ui.textCombo.color = Color.yellow;
                ui.textCombo.text = "";
                ui.textCombo.gameObject.SetActive(false);
                Debug.Log("[UI_Play] textCombo dynamically created and positioned below textJumps.");
            }
        }
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
