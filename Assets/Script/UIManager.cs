using UnityEngine;
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
	public UITexture texLogo;
	public UILabel textPlayInfo;
	public UILabel textTime;
	public UILabel textTouchScreen;
	public UITexture textSelectLevel;
	public UIButton btnNext;
	public UILabel textNext;
	public UITexture texNext;
	public UILabel textResultTime;
	public UITexture texResultIcon;
	public UITexture texNextBtnBg;
	public UIButton btnBack;
	public GameObject goBtnSound;
	public GameObject goBtnRetry;
	public GameObject goMsgBox;
	public UITexture texMsgBoxBg;
	public UILabel textMsgBox;
	public GameObject goLevelSelecter;
	public GameObject goHelpMsgBox;
	public UILabel textTimeInfo;
	public UITexture texHelpMsgBox;
	public UITexture texHelpMsgBoxBg;
	public UITexture texTimeIcon;

	void Awake()
	{
		m_instance = this;
	}

	void Start()
	{
		//Debug.Log( "Screen.width: " + Screen.width + ", Screen.height: " + Screen.height);

		textPlayInfo.gameObject.SetActive( false);
		textTime.gameObject.SetActive( false);
		texTimeIcon.gameObject.SetActive( false);
		textSelectLevel.gameObject.SetActive( false);
		texNextBtnBg.gameObject.SetActive( false);
		btnNext.gameObject.SetActive( false);
		btnBack.gameObject.SetActive( false);
		goBtnSound.SetActive( false);
		goBtnRetry.SetActive( false);
		goLevelSelecter.SetActive( false);
		CloseMsgBox();
		CloseHelpMsgBox();

		texLogo.transform.localPosition = new Vector3( 0.0f, 480.0f * 0.2f, 0.0f);
		textTouchScreen.transform.localPosition = new Vector3( 0.0f, -480.0f * 0.25f, 0.0f);

		// sound btn
		Vector3 vBtnSound = btnBack.transform.localPosition;
		vBtnSound.x -= 44.0f;
		goBtnSound.transform.localPosition = vBtnSound;

		// retry btn
		Vector3 vBtnRetry = btnBack.transform.localPosition;
		vBtnRetry.y -= 44.0f;
		goBtnRetry.transform.localPosition = vBtnRetry;

		// time icon
		Vector3 vTimeIcon = textTime.transform.localPosition;
		vTimeIcon.x = textTime.transform.localPosition.x - (textTime.localSize.x * 0.5f) - (texTimeIcon.localSize.x * 0.5f);
		vTimeIcon.y = textTime.transform.localPosition.y;
		texTimeIcon.transform.localPosition = vTimeIcon;
	}
	
	void Update()
	{
		if( eGameState.eGameState_Play == GameMain.Instance.eCurState)
		{
			if( true == goHelpMsgBox.activeInHierarchy)
				return;
		}
		else
			return;

		// update time
		float fCurTime = Time.realtimeSinceStartup;
		nGameTime = (int)( fCurTime - m_fStartTime);

		string strTimeRes = string.Empty;
		int nTime_gold = GameMain.Instance.nTime_gold[ m_nLevelBuff - 1];
		int nTime_silver = GameMain.Instance.nTime_silver[ m_nLevelBuff - 1];
		int nTime_bronze = GameMain.Instance.nTime_bronze[ m_nLevelBuff - 1];
		int nMin = 0;
		int nSec = 0;

		if( nGameTime <= nTime_gold)
		{
			if( false == texTimeIcon.gameObject.activeInHierarchy)
				texTimeIcon.gameObject.SetActive( true);

			if( eLevelClearType.eLevelClearType_Gold != eClearType)
				texTimeIcon.mainTexture = Resources.Load( "UI/ui_time_gold") as Texture;

			nMin = nTime_gold / 60;
			nSec = nTime_gold % 60;
			strTimeRes = string.Format( "{0:D2}", nMin) + string.Format( ":{0:D2}", nSec);
			textTime.color = Color.yellow;
			eClearType = eLevelClearType.eLevelClearType_Gold;
		}
		else if( nGameTime <= nTime_silver)
		{
			if( eLevelClearType.eLevelClearType_Silver != eClearType)
				texTimeIcon.mainTexture = Resources.Load( "UI/ui_time_silver") as Texture;

			nMin = nTime_silver / 60;
			nSec = nTime_silver % 60;
			strTimeRes = string.Format( "{0:D2}", nMin) + string.Format( ":{0:D2}", nSec);
			textTime.color = Color.white;
			eClearType = eLevelClearType.eLevelClearType_Silver;
		}
		else if( nGameTime <= nTime_bronze)
		{
			if( eLevelClearType.eLevelClearType_Bronze != eClearType)
				texTimeIcon.mainTexture = Resources.Load( "UI/ui_time_bronze") as Texture;

			nMin = nTime_bronze / 60;
			nSec = nTime_bronze % 60;
			strTimeRes = string.Format( "{0:D2}", nMin) + string.Format( ":{0:D2}", nSec);
			textTime.color = new Color( 1.0f, 0.6823f, 0.0f);
			eClearType = eLevelClearType.eLevelClearType_Bronze;
		}
		else
		{
			texTimeIcon.gameObject.SetActive( false);
			strTimeRes = "--:--";
			textTime.color = Color.red;
			eClearType = eLevelClearType.eLevelClearType_None;
		}

		nMin = nGameTime / 60;
		nSec = nGameTime % 60;
		string strTime = string.Format( "\n{0:D2}", nMin) + string.Format( ":{0:D2}", nSec);
		textTime.text = strTimeRes + strTime;
	}

	public void SetPlayInfo(int nLevel, int nCoin)
	{
		if( false == textPlayInfo.gameObject.activeInHierarchy)
			textPlayInfo.gameObject.SetActive( true);

		m_nLevelBuff = nLevel;

		SetPlayInfo( nCoin);
	}

	public void SetPlayInfo(int nCoin)
	{
		string strLevel = "Level " + m_nLevelBuff.ToString ();
		string strJewel = string.Format( "Jewel {0:n0}", nCoin);
		textPlayInfo.text = strLevel + "\n" + strJewel;
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
		textMsgBox.text = strMsg;
		goMsgBox.SetActive( true);

		texMsgBoxBg.gameObject.SetActive( true);
		if( true == texNextBtnBg.gameObject.activeInHierarchy)
			texNextBtnBg.gameObject.SetActive( false);
		if( true == texHelpMsgBoxBg.gameObject.activeInHierarchy)
			texHelpMsgBoxBg.gameObject.SetActive( false);

		PauseTime( true);
	}

	public void CloseMsgBox()
	{
		goMsgBox.SetActive( false);
		texMsgBoxBg.gameObject.SetActive( false);

		if( true == btnNext.gameObject.activeInHierarchy)
			texNextBtnBg.gameObject.SetActive( true);
		if( true == goHelpMsgBox.activeInHierarchy)
			texHelpMsgBoxBg.gameObject.SetActive( true);

		PauseTime( false);
	}

	public void OpenHelpMsgBox_1(int nLevel)
	{
		PauseTime( true);

		if( 1 == nLevel)
		{
			textTimeInfo.text = "";
			texHelpMsgBox.mainTexture = Resources.Load( "UI/help_1") as Texture;
			
			goHelpMsgBox.SetActive( true);
			texHelpMsgBoxBg.gameObject.SetActive( true);

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
		int nTime_gold = GameMain.Instance.nTime_gold[ nLevel - 1];
		int nTime_silver = GameMain.Instance.nTime_silver[ nLevel - 1];
		int nTime_bronze = GameMain.Instance.nTime_bronze[ nLevel - 1];
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
		
		textTimeInfo.text = strTime_gold + "\n\n" + strTime_silver + "\n\n" + strTime_bronze;
		
		texHelpMsgBox.mainTexture = Resources.Load( "UI/help_msgbox") as Texture;
		goHelpMsgBox.SetActive( true);
		texHelpMsgBoxBg.gameObject.SetActive( true);
	}

	public void CloseHelpMsgBox()
	{
		if( true == m_bHelpMsgBoxNext)
		{
			goHelpMsgBox.SetActive( false);
			texHelpMsgBoxBg.gameObject.SetActive( false);
			OpenHelpMsgBox_2( m_nLevelBuff);

			m_bHelpMsgBoxNext = false;
		}
		else
		{
			goHelpMsgBox.SetActive( false);
			texHelpMsgBoxBg.gameObject.SetActive( false);
			PauseTime( false);
		}
	}

	public void ConformBackBtn()
	{
		if( true == goHelpMsgBox.activeInHierarchy)
		{
			CloseHelpMsgBox();
			return;
		}

		if( eGameState.eGameState_Pause != GameMain.Instance.eCurState)
			m_eOldState = GameMain.Instance.eCurState;

		if( eGameState.eGameState_Logo == GameMain.Instance.eCurState)
		{
			Util.Quit();
		}
		else if( eGameState.eGameState_Select == GameMain.Instance.eCurState)
		{
			OpenMsgBox( "Exit Game ?");
			GameMain.Instance.eCurState = eGameState.eGameState_Pause;
		}
		else if( eGameState.eGameState_Play == GameMain.Instance.eCurState)
		{
			OpenMsgBox( "Exit Level ?");
			GameMain.Instance.eCurState = eGameState.eGameState_Pause;
		}
		else if( eGameState.eGameState_Result == GameMain.Instance.eCurState)
		{
			OpenMsgBox( "Exit Level ?");
			GameMain.Instance.eCurState = eGameState.eGameState_Pause;
		}
		else if( eGameState.eGameState_Pause == GameMain.Instance.eCurState)
		{
			CloseMsgBox();
			GameMain.Instance.eCurState = m_eOldState;
		}
	}

	public void CreateLevelSelectUI()
	{
		textSelectLevel.gameObject.SetActive( true);
		goLevelSelecter.SetActive( true);
	}

	public void ApplySoundButton()
	{
		if( 0 == GameMain.Instance.nSoundEnable)
		{
			UITexture tex = goBtnSound.GetComponent<UITexture>();
			tex.mainTexture = Resources.Load( "UI/sound_off") as Texture;
			AudioManager.Instance.StopBgm();
		}
		else
		{
			UITexture tex = goBtnSound.GetComponent<UITexture>();
			tex.mainTexture = Resources.Load( "UI/sound_on") as Texture;
			AudioManager.Instance.PlayBgm( "Sound/bgm");
		}
	}

#region button message
	public void onBtnNext()
	{
		AudioManager.Instance.Play( "Sound/ui_button_down");

		btnNext.gameObject.SetActive( false);
		texNextBtnBg.gameObject.SetActive( false);

		if( eLevelClearType.eLevelClearType_None == eClearType)
			GameMain.Instance.StartLevel( m_nLevelBuff);
		else
		{
			if( GameMain.Instance.nCurLevel == GameMain.Instance.nLevelCount)
			{
				m_eOldState = eGameState.eGameState_Select;
				textPlayInfo.gameObject.SetActive( false);
				textTime.gameObject.SetActive( false);
				texTimeIcon.gameObject.SetActive( false);
				goBtnRetry.SetActive( false);
				GameMain.Instance.GoLevelSelectScene();
			}
			else
				GameMain.Instance.StartNextLevel();
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

		if( 0 == GameMain.Instance.nSoundEnable)
		{
			GameMain.Instance.nSoundEnable = 1;
			ApplySoundButton();
		}
		else
		{
			GameMain.Instance.nSoundEnable = 0;
			ApplySoundButton();
		}
	}

	public void onBtnRetry()
	{
		AudioManager.Instance.Play( "Sound/ui_button_down");
		GameMain.Instance.StartLevel( m_nLevelBuff);
	}

	public void onBtnNo()
	{
		AudioManager.Instance.Play( "Sound/ui_button_down");

		CloseMsgBox();
		GameMain.Instance.eCurState = m_eOldState;
	}

	public void onBtnYes()
	{
		AudioManager.Instance.Play( "Sound/ui_button_down");

		CloseMsgBox();

		if( eGameState.eGameState_Logo == m_eOldState || eGameState.eGameState_Select == m_eOldState)
		{
			GameMain.Instance.SaveData();
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
			GameMain.Instance.GoLevelSelectScene();
		}
		else
			GameMain.Instance.eCurState = m_eOldState;
	}

	public void onBtnHelpOk()
	{
		AudioManager.Instance.Play( "Sound/ui_button_down");
		CloseHelpMsgBox();
	}
#endregion button message
}
