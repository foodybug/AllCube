using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using System.IO;

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
	public static GameMain Instance{ get{ return m_instance;}}

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

	void Awake()
	{
		m_instance = this;
		Application.targetFrameRate = 60;
	}

	void Start()
	{
		_LoadData();
		CameraManager.Instance.Init();
		if (UIManager.Instance != null)
			UIManager.Instance.ApplySoundButton();
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

		if( eGameState.eGameState_Logo == eCurState)
		{
			if( true == Input.GetMouseButtonUp( 0) || ( Input.touchCount > 0 && TouchPhase.Began == Input.touches[0].phase))
			{
				if (UIManager.Instance != null)
				{
					eCurState = eGameState.eGameState_Select;
					UIManager.Instance.textTouchScreen.gameObject.SetActive( false);
					UIManager.Instance.texLogo.gameObject.SetActive( false);
					UIManager.Instance.textSelectLevel.gameObject.SetActive( true);
					UIManager.Instance.btnBack.gameObject.SetActive( true);
					UIManager.Instance.goBtnSound.SetActive( true);

					UIManager.Instance.CreateLevelSelectUI();
				}
				else
				{
					// UI가 없을 경우 테스트를 위해 로고 화면에서 바로 최근 레벨로 직행
					StartLevel(nSaveLevel);
				}
				
				AudioManager.Instance.PlayBgm( "Sound/bgm");
				AdmobManager.Instance.Show();
			}
		}
		else
		{
			// update main bg position
			Vector3 vPos = CameraManager.Instance.mainCamera.transform.position;
			vPos.z = goMainBg.transform.position.z;
			goMainBg.transform.position = vPos;
		}
	}

	void OnApplicationQuit()
	{
		SaveData();
	}
	
	void OnApplicationPause(bool pause)
	{
		if( true == pause)
			SaveData();

		if (UIManager.Instance != null)
			UIManager.Instance.PauseTime( pause);
	}

	public void StartNextLevel()
	{
		nCurLevel++;
		if( nCurLevel > nLevelCount)
			nCurLevel = nLevelCount;
		StartLevel( nCurLevel);
	}

	public void StartLevel(int nLevel)
	{
		eCurState = eGameState.eGameState_Play;

		if( null == m_goPlayer)
		{
			m_goPlayer = GameObject.Instantiate( goPlayerSrc) as GameObject;
			CameraManager.Instance.SetTarget( m_goPlayer);
		}

		m_goPlayer.GetComponent<Rigidbody>().linearVelocity = Vector3.zero;
		m_goPlayer.GetComponent<Rigidbody>().Sleep();
		m_goPlayer.transform.position = Vector3.zero;
		CameraManager.Instance.Init();
		MapManager.Instance.UnLoadCubeMap();
		MapManager.Instance.LoadCubeMap( nLevel);
		m_goPlayer.GetComponent<Rigidbody>().WakeUp();

		if (UIManager.Instance != null)
		{
			UIManager.Instance.SetPlayInfo( nLevel, MapManager.Instance.CoinCount);
			if( false == UIManager.Instance.goBtnRetry.activeInHierarchy)
				UIManager.Instance.goBtnRetry.SetActive( true);
			UIManager.Instance.StartTime();

			UIManager.Instance.OpenHelpMsgBox_1( nLevel);
		}
	}

	public void GoLevelSelectScene()
	{
		eCurState = eGameState.eGameState_Select;
		Util.MyDestroy( m_goPlayer);
		MapManager.Instance.UnLoadCubeMap();

		if (UIManager.Instance != null)
			UIManager.Instance.CreateLevelSelectUI();
	}

	private void _LoadData()
	{
		for( int i = 0; i < nLevelCount; i++)
			nClearType[i] = 0;

		System.String[] res = System.IO.Directory.GetFiles( Application.persistentDataPath, "info.inf");
		
		if( res.Length > 0)
		{
			FileStream fs = new FileStream( res[0], FileMode.Open);
			
			if( null != fs)
			{
				BinaryReader br = new BinaryReader( fs);
				
				nSaveLevel = br.ReadInt32();
				nSoundEnable = br.ReadInt32();
				int nCount = br.ReadInt32();

				for( int i = 0; i < nCount; i++)
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
		if( nSaveLevel < nCurLevel)
			nSaveLevel = nCurLevel;

		StringBuilder sb = new StringBuilder( Application.persistentDataPath);
		sb.Append( "/info.inf");
		
		FileStream fs = new FileStream( sb.ToString(), FileMode.Create);
		
		if( null == fs)
			return;

		fs.Seek( 0, SeekOrigin.Begin);
		
		BinaryWriter bw = new BinaryWriter( fs);
		
		bw.Write( nSaveLevel);
		bw.Write( nSoundEnable);
		bw.Write( nLevelCount);
		for( int i = 0; i < nLevelCount; i++)
			bw.Write( nClearType[i]);

		bw.Close();
		fs.Close();
	}
}
