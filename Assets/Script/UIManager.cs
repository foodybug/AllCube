using UnityEngine;
using UnityEngine.UI;
using System.Collections;

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
	public static UIManager Instance{ get{ return m_instance;}}

	private int m_nLevelBuff = 0;
	private eGameState m_eOldState = eGameState.eGameState_Logo;
	private bool m_bPauseTime = false;
	private float m_fStartTime = 0.0f;
	private float m_fPauseTime = 0.0f;
	private bool m_bHelpMsgBoxNext = false;

	public bool bPauseTime { get{ return m_bPauseTime; }}

	public int nGameTime = 0;
	public eLevelClearType eClearType = eLevelClearType.eLevelClearType_None;


	public Camera uiCamera;
	public RawImage texLogo;
	public Text textPlayInfo;
	public Text textTime;
	public Text textTouchScreen;
	public RawImage textSelectLevel;
	public Button btnNext;
	public Text textNext;
	public RawImage texNext;
	public Text textResultTime;
	public RawImage texResultIcon;
	public RawImage texNextBtnBg;
	public Button btnBack;
	public GameObject goBtnSound;
	public GameObject goBtnRetry;
	public GameObject goMsgBox;
	public RawImage texMsgBoxBg;
	public Text textMsgBox;
	public GameObject goLevelSelecter;
	public GameObject goHelpMsgBox;
	public Text textTimeInfo;
	public RawImage texHelpMsgBox;
	public RawImage texHelpMsgBoxBg;
	public RawImage texTimeIcon;
	public Text textJumps; // 독립된 점프 카운트 전용 UI 텍스트 필드

	private int m_nCurrentJumps = 10;

	void Awake()
	{
		m_instance = this;
	}

	void Start()
	{
		// 미지정 컴포넌트 자동 복구 및 할당
		AutoAssignComponents();

		if (textPlayInfo != null) textPlayInfo.gameObject.SetActive( false);
		if (textTime != null) textTime.gameObject.SetActive( false);
		if (texTimeIcon != null) texTimeIcon.gameObject.SetActive( false);
		if (textSelectLevel != null) textSelectLevel.gameObject.SetActive( false);
		if (texNextBtnBg != null) texNextBtnBg.gameObject.SetActive( false);
		if (btnNext != null) btnNext.gameObject.SetActive( false);
		if (btnBack != null) btnBack.gameObject.SetActive( false);
		if (goBtnSound != null) goBtnSound.SetActive( false);
		if (goBtnRetry != null) goBtnRetry.SetActive( false);
		if (goLevelSelecter != null) goLevelSelecter.SetActive( false);
		CloseMsgBox();
		CloseHelpMsgBox();

		if (texLogo != null) texLogo.transform.localPosition = new Vector3( 0.0f, 480.0f * 0.2f, 0.0f);
		if (textTouchScreen != null) textTouchScreen.transform.localPosition = new Vector3( 0.0f, -480.0f * 0.25f, 0.0f);

		// sound btn
		if (btnBack != null && goBtnSound != null)
		{
			Vector3 vBtnSound = btnBack.transform.localPosition;
			vBtnSound.x -= 44.0f;
			goBtnSound.transform.localPosition = vBtnSound;
		}

		// retry btn
		if (btnBack != null && goBtnRetry != null)
		{
			Vector3 vBtnRetry = btnBack.transform.localPosition;
			vBtnRetry.y -= 44.0f;
			goBtnRetry.transform.localPosition = vBtnRetry;
		}

		// time icon
		if (textTime != null && texTimeIcon != null)
		{
			Vector3 vTimeIcon = textTime.transform.localPosition;
			vTimeIcon.x = textTime.transform.localPosition.x - (textTime.rectTransform.sizeDelta.x * 0.5f) - (texTimeIcon.rectTransform.sizeDelta.x * 0.5f);
			vTimeIcon.y = textTime.transform.localPosition.y;
			texTimeIcon.transform.localPosition = vTimeIcon;
		}
	}
	
	void Update()
	{
		if( MainManager.Instance != null && eGameState.eGameState_Play == MainManager.Instance.eCurState)
		{
			if( goHelpMsgBox != null && true == goHelpMsgBox.activeInHierarchy)
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
		if (textJumps != null && textJumps.gameObject.activeInHierarchy)
		{
			if (m_nCurrentJumps <= 3)
			{
				float pulse = 1.0f + Mathf.Abs(Mathf.Sin(Time.realtimeSinceStartup * 10f)) * 0.25f;
				textJumps.transform.localScale = new Vector3(pulse, pulse, 1f);
				textJumps.color = Color.red;
			}
			else
			{
				textJumps.transform.localScale = Vector3.one;
				textJumps.color = Color.white;
			}
		}

		// update time
		float fCurTime = Time.realtimeSinceStartup;
		nGameTime = (int)( fCurTime - m_fStartTime);

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

		if( nGameTime <= nTime_gold)
		{
			if( texTimeIcon != null && false == texTimeIcon.gameObject.activeInHierarchy)
				texTimeIcon.gameObject.SetActive( true);

			if( texTimeIcon != null && eLevelClearType.eLevelClearType_Gold != eClearType)
				texTimeIcon.texture = Resources.Load( "UI/ui_time_gold") as Texture;

			nMin = nTime_gold / 60;
			nSec = nTime_gold % 60;
			strTimeRes = string.Format( "{0:D2}", nMin) + string.Format( ":{0:D2}", nSec);
			if (textTime != null) textTime.color = Color.yellow;
			eClearType = eLevelClearType.eLevelClearType_Gold;
		}
		else if( nGameTime <= nTime_silver)
		{
			if( texTimeIcon != null && eLevelClearType.eLevelClearType_Silver != eClearType)
				texTimeIcon.texture = Resources.Load( "UI/ui_time_silver") as Texture;

			nMin = nTime_silver / 60;
			nSec = nTime_silver % 60;
			strTimeRes = string.Format( "{0:D2}", nMin) + string.Format( ":{0:D2}", nSec);
			if (textTime != null) textTime.color = Color.white;
			eClearType = eLevelClearType.eLevelClearType_Silver;
		}
		else if( nGameTime <= nTime_bronze)
		{
			if( texTimeIcon != null && eLevelClearType.eLevelClearType_Bronze != eClearType)
				texTimeIcon.texture = Resources.Load( "UI/ui_time_bronze") as Texture;

			nMin = nTime_bronze / 60;
			nSec = nTime_bronze % 60;
			strTimeRes = string.Format( "{0:D2}", nMin) + string.Format( ":{0:D2}", nSec);
			if (textTime != null) textTime.color = new Color( 1.0f, 0.6823f, 0.0f);
			eClearType = eLevelClearType.eLevelClearType_Bronze;
		}
		else
		{
			if (texTimeIcon != null) texTimeIcon.gameObject.SetActive( false);
			strTimeRes = "--:--";
			if (textTime != null) textTime.color = Color.red;
			eClearType = eLevelClearType.eLevelClearType_None;
		}

		nMin = nGameTime / 60;
		nSec = nGameTime % 60;
		string strTime = string.Format( "\n{0:D2}", nMin) + string.Format( ":{0:D2}", nSec);
		if (textTime != null) textTime.text = strTimeRes + strTime;
	}

	public void SetPlayInfo(int nLevel, int nCoin, int nJumps)
	{
		if( false == textPlayInfo.gameObject.activeInHierarchy)
			textPlayInfo.gameObject.SetActive( true);

		m_nLevelBuff = nLevel;

		SetPlayStats( nCoin, nJumps);
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
		m_nCurrentJumps = nJumps;

		string strLevel = "Level " + m_nLevelBuff.ToString ();
		string strJewel = string.Format( "Jewel {0:n0}", nCoin);
		
		if (textJumps != null)
		{
			textPlayInfo.text = strLevel + "\n" + strJewel;
			textJumps.text = string.Format("Jumps {0:n0}", nJumps);
			if (false == textJumps.gameObject.activeInHierarchy)
			{
				textJumps.gameObject.SetActive(true);
			}
		}
		else
		{
			string strJumps = string.Format( "Jumps {0:n0}", nJumps);
			textPlayInfo.text = strLevel + "\n" + strJewel + "\n" + strJumps;
		}
	}

	public void StartTime()
	{
		if( false == textTime.gameObject.activeInHierarchy)
			textTime.gameObject.SetActive( true);

		if( true == texTimeIcon.gameObject.activeInHierarchy)
			texTimeIcon.gameObject.SetActive( false);

		m_fStartTime = Time.realtimeSinceStartup;
		nGameTime = 0;

		textTime.text = "00:00\n00:00";
		textTime.color = Color.white;
	}

	public void PauseTime(bool bPause)
	{
		if( true == bPause)
		{
			if( m_bPauseTime != bPause)
			{
				m_fPauseTime = Time.realtimeSinceStartup;
				m_bPauseTime = bPause;
			}
		}
		else
		{
			if( false == goHelpMsgBox.activeInHierarchy && false == goMsgBox.activeInHierarchy)
			{
				if( m_bPauseTime != bPause)
				{
					float fTime = Time.realtimeSinceStartup;
					m_fStartTime = m_fStartTime + ( fTime - m_fPauseTime);
					m_bPauseTime = bPause;
				}
			}
		}
	}

	public void OpenMsgBox(string strMsg)
	{
		if (textMsgBox != null) textMsgBox.text = strMsg;
		if (goMsgBox != null) goMsgBox.SetActive( true);

		if (texMsgBoxBg != null) texMsgBoxBg.gameObject.SetActive( true);
		if (texNextBtnBg != null && true == texNextBtnBg.gameObject.activeInHierarchy)
			texNextBtnBg.gameObject.SetActive( false);
		if (texHelpMsgBoxBg != null && true == texHelpMsgBoxBg.gameObject.activeInHierarchy)
			texHelpMsgBoxBg.gameObject.SetActive( false);

		PauseTime( true);
	}

	public void CloseMsgBox()
	{
		if (goMsgBox != null) goMsgBox.SetActive( false);
		if (texMsgBoxBg != null) texMsgBoxBg.gameObject.SetActive( false);

		if (btnNext != null && true == btnNext.gameObject.activeInHierarchy && texNextBtnBg != null)
			texNextBtnBg.gameObject.SetActive( true);
		if (goHelpMsgBox != null && true == goHelpMsgBox.activeInHierarchy && texHelpMsgBoxBg != null)
			texHelpMsgBoxBg.gameObject.SetActive( true);

		PauseTime( false);
	}

	public void OpenHelpMsgBox_1(int nLevel)
	{
		PauseTime( true);

		if( 1 == nLevel)
		{
			if (textTimeInfo != null) textTimeInfo.text = "";
			if (texHelpMsgBox != null) texHelpMsgBox.texture = Resources.Load( "UI/help_1") as Texture;
			
			if (goHelpMsgBox != null) goHelpMsgBox.SetActive( true);
			if (texHelpMsgBoxBg != null) texHelpMsgBoxBg.gameObject.SetActive( true);

			m_bHelpMsgBoxNext = true;
		}
		else
		{
			m_bHelpMsgBoxNext = false;
			OpenHelpMsgBox_2( nLevel);
		}
	}

	public void OpenHelpMsgBox_2(int nLevel)
	{
		if (MainManager.Instance == null) return;
		int nTime_gold = MainManager.Instance.nTime_gold[ nLevel - 1];
		int nTime_silver = MainManager.Instance.nTime_silver[ nLevel - 1];
		int nTime_bronze = MainManager.Instance.nTime_bronze[ nLevel - 1];
		int nMin = 0;
		int nSec = 0;
		
		nMin = nTime_gold / 60;
		nSec = nTime_gold % 60;
		string strTime_gold = string.Format( "{0:D2}", nMin) + string.Format( ":{0:D2}", nSec);
		
		nMin = nTime_silver / 60;
		nSec = nTime_silver % 60;
		string strTime_silver = string.Format( "{0:D2}", nMin) + string.Format( ":{0:D2}", nSec);
		
		nMin = nTime_bronze / 60;
		nSec = nTime_bronze % 60;
		string strTime_bronze = string.Format( "{0:D2}", nMin) + string.Format( ":{0:D2}", nSec);
		
		if (textTimeInfo != null) textTimeInfo.text = strTime_gold + "\n\n" + strTime_silver + "\n\n" + strTime_bronze;
		
		if (texHelpMsgBox != null) texHelpMsgBox.texture = Resources.Load( "UI/help_msgbox") as Texture;
		if (goHelpMsgBox != null) goHelpMsgBox.SetActive( true);
		if (texHelpMsgBoxBg != null) texHelpMsgBoxBg.gameObject.SetActive( true);
	}

	public void CloseHelpMsgBox()
	{
		if( true == m_bHelpMsgBoxNext)
		{
			if (goHelpMsgBox != null) goHelpMsgBox.SetActive( false);
			if (texHelpMsgBoxBg != null) texHelpMsgBoxBg.gameObject.SetActive( false);
			OpenHelpMsgBox_2( m_nLevelBuff);

			m_bHelpMsgBoxNext = false;
		}
		else
		{
			if (goHelpMsgBox != null) goHelpMsgBox.SetActive( false);
			if (texHelpMsgBoxBg != null) texHelpMsgBoxBg.gameObject.SetActive( false);
			PauseTime( false);
		}
	}

	public void ConformBackBtn()
	{
		if( goHelpMsgBox != null && true == goHelpMsgBox.activeInHierarchy)
		{
			CloseHelpMsgBox();
			return;
		}

		if( MainManager.Instance != null && eGameState.eGameState_Pause != MainManager.Instance.eCurState)
			m_eOldState = MainManager.Instance.eCurState;

		if( MainManager.Instance != null && eGameState.eGameState_Logo == MainManager.Instance.eCurState)
		{
			Util.Quit();
		}
		else if( MainManager.Instance != null && eGameState.eGameState_Select == MainManager.Instance.eCurState)
		{
			OpenMsgBox( "Exit Game ?");
			MainManager.Instance.eCurState = eGameState.eGameState_Pause;
		}
		else if( MainManager.Instance != null && eGameState.eGameState_Play == MainManager.Instance.eCurState)
		{
			OpenMsgBox( "Exit Level ?");
			MainManager.Instance.eCurState = eGameState.eGameState_Pause;
		}
		else if( MainManager.Instance != null && eGameState.eGameState_Result == MainManager.Instance.eCurState)
		{
			OpenMsgBox( "Exit Level ?");
			MainManager.Instance.eCurState = eGameState.eGameState_Pause;
		}
		else if( MainManager.Instance != null && eGameState.eGameState_Pause == MainManager.Instance.eCurState)
		{
			CloseMsgBox();
			MainManager.Instance.eCurState = m_eOldState;
		}
	}

	public void CreateLevelSelectUI()
	{
		if (textSelectLevel != null) textSelectLevel.gameObject.SetActive( true);
		if (goLevelSelecter != null) goLevelSelecter.SetActive( true);
	}

	public void ApplySoundButton()
	{
		if (goBtnSound == null) return;
		
		if( MainManager.Instance != null && 0 == MainManager.Instance.nSoundEnable)
		{
			RawImage tex = goBtnSound.GetComponent<RawImage>();
			if (tex != null) tex.texture = Resources.Load( "UI/sound_off") as Texture;
			AudioManager.Instance.StopBgm();
		}
		else
		{
			RawImage tex = goBtnSound.GetComponent<RawImage>();
			if (tex != null) tex.texture = Resources.Load( "UI/sound_on") as Texture;
			AudioManager.Instance.PlayBgm( "Sound/bgm");
		}
	}

#region button message
	public void onBtnNext()
	{
		if (MainManager.Instance != null && MainManager.Instance.IsTransitioning) return;
		AudioManager.Instance.Play( "Sound/ui_button_down");

		btnNext.gameObject.SetActive( false);
		texNextBtnBg.gameObject.SetActive( false);

		if( (int)eLevelClearType.eLevelClearType_None == (int)MainManager.lastClearType)
		{
			if (MainManager.Instance != null)
				MainManager.Instance.StartLevel( MainManager.nCurLevelStatic);
		}
		else
		{
			if (MainManager.Instance != null)
			{
				if( MainManager.nCurLevelStatic == MainManager.Instance.nLevelCount)
				{
					m_eOldState = eGameState.eGameState_Select;
					if (textPlayInfo != null) textPlayInfo.gameObject.SetActive( false);
					if (textTime != null) textTime.gameObject.SetActive( false);
					if (texTimeIcon != null) texTimeIcon.gameObject.SetActive( false);
					if (goBtnRetry != null) goBtnRetry.SetActive( false);
					MainManager.Instance.GoLevelSelectScene();
				}
				else
				{
					MainManager.nCurLevelStatic++;
					MainManager.Instance.StartLevel( MainManager.nCurLevelStatic );
				}
			}
		}
	}

	public void onBtnBack()
	{
		AudioManager.Instance.Play( "Sound/ui_button_down");
		ConformBackBtn();
	}

	public void onBtnSound()
	{
		AudioManager.Instance.Play( "Sound/ui_button_down");

		if (MainManager.Instance != null)
		{
			if( 0 == MainManager.Instance.nSoundEnable)
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
		AudioManager.Instance.Play( "Sound/ui_button_down");
		if (MainManager.Instance != null)
		{
			MainManager.Instance.StartLevel( MainManager.nCurLevelStatic );
		}
	}

	public void onBtnNo()
	{
		AudioManager.Instance.Play( "Sound/ui_button_down");

		CloseMsgBox();
		if (MainManager.Instance != null)
		{
			MainManager.Instance.eCurState = m_eOldState;
		}
	}

	public void onBtnYes()
	{
		if (MainManager.Instance != null && MainManager.Instance.IsTransitioning) return;
		AudioManager.Instance.Play( "Sound/ui_button_down");

		CloseMsgBox();

		if( eGameState.eGameState_Logo == m_eOldState || eGameState.eGameState_Select == m_eOldState)
		{
			if (MainManager.Instance != null) MainManager.Instance.SaveData();
			Util.Quit();
		}
		else if( eGameState.eGameState_Play == m_eOldState || eGameState.eGameState_Result == m_eOldState)
		{
			m_eOldState = eGameState.eGameState_Select;
			textPlayInfo.gameObject.SetActive( false);
			textTime.gameObject.SetActive( false);
			texTimeIcon.gameObject.SetActive( false);
			btnNext.gameObject.SetActive( false);
			texNextBtnBg.gameObject.SetActive( false);
			goBtnRetry.SetActive( false);
			if (MainManager.Instance != null) MainManager.Instance.GoLevelSelectScene();
		}
		else
		{
			if (MainManager.Instance != null) MainManager.Instance.eCurState = m_eOldState;
		}
	}

	public void onBtnHelpOk()
	{
		AudioManager.Instance.Play( "Sound/ui_button_down");
		CloseHelpMsgBox();
	}





	public void SetupResultScreen()
	{
		if (btnNext != null) btnNext.gameObject.SetActive( true);
		if (texMsgBoxBg != null && false == texMsgBoxBg.gameObject.activeInHierarchy && texNextBtnBg != null)
			texNextBtnBg.gameObject.SetActive( true);

		if( (int)eLevelClearType.eLevelClearType_None == (int)MainManager.lastClearType)
		{
			AudioManager.Instance.Play( "Sound/fail", 0.3f);
			if (textNext != null) textNext.text = "Retry";
			if (texNext != null) texNext.texture = Resources.Load( "UI/retry_bg") as Texture;
			
			if (texResultIcon != null)
			{
				texResultIcon.enabled = true;
				texResultIcon.texture = Resources.Load( "UI/ui_time_bronze") as Texture;
			}
			if (textResultTime != null)
			{
				textResultTime.enabled = true;
				int nMin = MainManager.lastGameTime / 60;
				int nSec = MainManager.lastGameTime % 60;
				textResultTime.text = string.Format( "{0:D2}", nMin) + string.Format( ":{0:D2}", nSec);
			}
		}
		else
		{
			AudioManager.Instance.Play( "Sound/clear");
			if (texNext != null) texNext.texture = Resources.Load( "UI/done_bg") as Texture;

			if (textNext != null)
			{
				if( MainManager.nCurLevelStatic == MainManager.Instance.nLevelCount)
					textNext.text = "Clear!";
				else
					textNext.text = "Done";
			}

			if (texResultIcon != null) texResultIcon.enabled = true;
			if (textResultTime != null)
			{
				textResultTime.enabled = true;
				int nMin = MainManager.lastGameTime / 60;
				int nSec = MainManager.lastGameTime % 60;
				textResultTime.text = string.Format( "{0:D2}", nMin) + string.Format( ":{0:D2}", nSec);
			}
			
			if (texResultIcon != null)
			{
				if( (int)eLevelClearType.eLevelClearType_Gold == (int)MainManager.lastClearType)
					texResultIcon.texture = Resources.Load( "UI/ui_time_gold") as Texture;
				else if( (int)eLevelClearType.eLevelClearType_Silver == (int)MainManager.lastClearType)
					texResultIcon.texture = Resources.Load( "UI/ui_time_silver") as Texture;
				else
					texResultIcon.texture = Resources.Load( "UI/ui_time_bronze") as Texture;
			}

			if( 0 == MainManager.Instance.nClearType[ MainManager.nCurLevelStatic - 1])
				MainManager.Instance.nClearType[ MainManager.nCurLevelStatic - 1] = (int)( MainManager.lastClearType);
			else
			{
				if( MainManager.Instance.nClearType[ MainManager.nCurLevelStatic - 1] > (int)( MainManager.lastClearType))
					MainManager.Instance.nClearType[ MainManager.nCurLevelStatic - 1] = (int)( MainManager.lastClearType);
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
		// 카메라
		if (uiCamera == null) uiCamera = FindAnyObjectByType<Camera>();

		// UI 텍스트 및 원본 텍스처들
		if (texLogo == null) texLogo = FindChildByName<RawImage>("texLogo");
		if (textPlayInfo == null) textPlayInfo = FindChildByName<Text>("textPlayInfo");
		if (textTime == null) textTime = FindChildByName<Text>("textTime");
		if (textTouchScreen == null) textTouchScreen = FindChildByName<Text>("textTouchScreen");
		if (textSelectLevel == null) textSelectLevel = FindChildByName<RawImage>("textSelectLevel");
		
		// 버튼들
		if (btnNext == null) btnNext = FindChildByName<Button>("btnNext");
		if (btnBack == null) btnBack = FindChildByName<Button>("btnBack");

		// 버튼 하위 텍스트/이미지
		if (btnNext != null)
		{
			if (textNext == null) textNext = btnNext.GetComponentInChildren<Text>();
			if (texNext == null) texNext = btnNext.GetComponentInChildren<RawImage>();
		}
		else
		{
			if (textNext == null) textNext = FindChildByName<Text>("textNext");
			if (texNext == null) texNext = FindChildByName<RawImage>("texNext");
		}

		if (textResultTime == null) textResultTime = FindChildByName<Text>("textResultTime");
		if (texResultIcon == null) texResultIcon = FindChildByName<RawImage>("texResultIcon");
		if (texNextBtnBg == null) texNextBtnBg = FindChildByName<RawImage>("texNextBtnBg");

		// 게임 오브젝트들
		if (goBtnSound == null) goBtnSound = FindChildGameObjectByName("goBtnSound");
		if (goBtnRetry == null) goBtnRetry = FindChildGameObjectByName("goBtnRetry");
		if (goMsgBox == null) goMsgBox = FindChildGameObjectByName("goMsgBox");
		if (goLevelSelecter == null) goLevelSelecter = FindChildGameObjectByName("goLevelSelecter");
		if (goHelpMsgBox == null) goHelpMsgBox = FindChildGameObjectByName("goHelpMsgBox");

		// 메시지 박스 하위
		if (goMsgBox != null)
		{
			if (texMsgBoxBg == null) texMsgBoxBg = goMsgBox.GetComponentInChildren<RawImage>();
			if (textMsgBox == null) textMsgBox = goMsgBox.GetComponentInChildren<Text>();
		}
		else
		{
			if (texMsgBoxBg == null) texMsgBoxBg = FindChildByName<RawImage>("texMsgBoxBg");
			if (textMsgBox == null) textMsgBox = FindChildByName<Text>("textMsgBox");
		}

		// 도움말 메시지 박스 하위
		if (goHelpMsgBox != null)
		{
			if (textTimeInfo == null) textTimeInfo = goHelpMsgBox.GetComponentInChildren<Text>();
			RawImage[] rawImages = goHelpMsgBox.GetComponentsInChildren<RawImage>(true);
			if (rawImages != null)
			{
				foreach (var ri in rawImages)
				{
					if (ri != null && ri.name != null)
					{
						if (ri.name.Contains("texHelpMsgBoxBg") || ri.name.Contains("Bg")) texHelpMsgBoxBg = ri;
						else if (ri.name.Contains("texHelpMsgBox") || ri.name.Contains("help")) texHelpMsgBox = ri;
					}
				}
			}
		}
		else
		{
			if (textTimeInfo == null) textTimeInfo = FindChildByName<Text>("textTimeInfo");
			if (texHelpMsgBox == null) texHelpMsgBox = FindChildByName<RawImage>("texHelpMsgBox");
			if (texHelpMsgBoxBg == null) texHelpMsgBoxBg = FindChildByName<RawImage>("texHelpMsgBoxBg");
		}

		if (texTimeIcon == null) texTimeIcon = FindChildByName<RawImage>("texTimeIcon");
		if (textJumps == null) textJumps = FindChildByName<Text>("textJumps");
	}

	private T FindChildByName<T>(string name) where T : Component
	{
		T[] comps = GetComponentsInChildren<T>(true);
		if (comps == null) return null;

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
		return null;
	}

	private GameObject FindChildGameObjectByName(string name)
	{
		Transform[] trans = GetComponentsInChildren<Transform>(true);
		if (trans == null) return null;

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
		return null;
	}
}
