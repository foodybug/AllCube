using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class MapManager : MonoBehaviour
{
	public enum eMapProp
	{
		eMapProp_None = 0,
		eMapProp_Coin,
		eMapProp_Normal,
		eMapProp_Break,
		eMapProp_MoveX,
		eMapProp_MoveY
	}

	static MapManager m_instance;
	public static MapManager Instance{ get{ return m_instance;}}

	public GameObject goCubeSrc;
	public GameObject goCoinSrc;
	public GameObject goCoinEffSrc;
	public GameObject goCubeEffSrc;

	public Texture[] texCube = new Texture[6];

	private List<GameObject> m_listCube = new List<GameObject>();
	private List<GameObject> m_listCoin = new List<GameObject>();
	private float m_fCubeSize = 0.0f;
	private float m_fLerp = 0.1f;

	public int CoinCount { get{ return m_listCoin.Count;}}

	void Awake()
	{
		m_instance = this;
	}

	void Start()
	{
		GameObject goCube = GameObject.Instantiate( goCubeSrc) as GameObject;
		m_fCubeSize = goCube.GetComponent<Collider>().bounds.size.x;
		Util.MyDestroy( goCube);
	}
	
	void Update()
	{
	}

	public void LoadCubeMap(int nStage)
	{
		string strPath = "Stage/" + nStage.ToString();
		Texture2D texStage = Resources.Load( strPath) as Texture2D;
		eMapProp[,] eProp = new eMapProp[ texStage.height, texStage.width];

		for( int y = 0; y < texStage.height; y++)
		{
			for( int x = 0; x < texStage.width; x++)
			{
				Color color = texStage.GetPixel( x, y);
				eMapProp prop = _GetMapProp( color);
				eProp[ y, x] = prop;
			}
		}

		for( int y = 0; y < texStage.height; y++)
		{
			for( int x = 0; x < texStage.width; x++)
			{
				eMapProp prop = eProp[ y, x];
				if( eMapProp.eMapProp_Coin == prop)
				{
					m_listCoin.Add( _CreateCoin( x, y));
				}
				else
				{
					if( eMapProp.eMapProp_None != prop)
					{
						GameObject go = _CreateCube( x, y, prop);
						int nMoveLeft = 0;
						int nMoveRight = 0;
						int nMoveUp = 0;
						int nMoveDown = 0;
						if( prop == eMapProp.eMapProp_MoveX)
						{
							for( int i = x-1; i > 0; i--)
							{
								if( eMapProp.eMapProp_None != eProp[ y, i])
									break;
								nMoveLeft++;
							}

							for( int k = x+1; k < texStage.width; k++)
							{
								if( eMapProp.eMapProp_None != eProp[ y, k])
									break;
								nMoveRight++;
							}

							CubeMoveX moveX = go.GetComponent<CubeMoveX>();
							moveX.SetMove( (float)nMoveLeft * m_fCubeSize, (float)nMoveRight * m_fCubeSize);
						}
						else if( prop == eMapProp.eMapProp_MoveY)
						{
							for( int i = y-1; i > 0; i--)
							{
								if( eMapProp.eMapProp_None != eProp[ i, x])
									break;
								nMoveDown++;
							}
							
							for( int k = y+1; k < texStage.height; k++)
							{
								if( eMapProp.eMapProp_None != eProp[ k, x])
									break;
								nMoveUp++;
							}
							
							CubeMoveY moveY = go.GetComponent<CubeMoveY>();
							moveY.SetMove( (float)nMoveUp * m_fCubeSize, (float)nMoveDown * m_fCubeSize);
						}

						m_listCube.Add( go);
					}
				}
			}
		}
	}

	private eMapProp _GetMapProp(Color color)
	{
		if( _isEqual( Color.black, color))
			return eMapProp.eMapProp_Normal;
		else if( _isEqual( Color.green, color))
			return eMapProp.eMapProp_Coin;
		else if( _isEqual( Color.gray, color))
			return eMapProp.eMapProp_Break;
		else if( _isEqual( Color.red, color))
			return eMapProp.eMapProp_MoveX;
		else if( _isEqual( Color.blue, color))
			return eMapProp.eMapProp_MoveY;
		else
			return eMapProp.eMapProp_None;
	}

	private bool _isEqual(Color color1, Color color2)
	{
		if( _isEqual( color1.r, color2.r) && _isEqual( color1.g, color2.g) && _isEqual( color1.b, color2.b))
			return true;
		return false;
	}

	private bool _isEqual(float f1, float f2)
	{
		if( f1 == f2 || ( ( f1 + m_fLerp > f2) && ( f1 - m_fLerp < f2)))
		   return true;
		return false;
	}

	private GameObject _CreateCube(int x, int y, eMapProp prop)
	{
		Vector3 vPos = Vector3.zero;
		GameObject go = GameObject.Instantiate( goCubeSrc) as GameObject;
		vPos.x = m_fCubeSize * x - m_fCubeSize;
		vPos.y = m_fCubeSize * y - m_fCubeSize;
		go.transform.position = vPos;
		go.transform.parent = this.transform;

		switch( prop)
		{
		case eMapProp.eMapProp_None: break;
		case eMapProp.eMapProp_Coin: break;

		case eMapProp.eMapProp_Normal:
			go.GetComponent<Renderer>().material.mainTexture = texCube[ (int)( Random.Range( 1, 5))];
			break;

		case eMapProp.eMapProp_Break:
			go.GetComponent<Renderer>().material.mainTexture = texCube[0];
			go.AddComponent<CubeBreak>();
			CubeBreak cubeBreak = go.GetComponent<CubeBreak>();
			cubeBreak.goCube = go;
			break;

		case eMapProp.eMapProp_MoveX:
			go.GetComponent<Renderer>().material.mainTexture = texCube[5];
			go.AddComponent<CubeMoveX>();
			CubeMoveX cubeMoveX = go.GetComponent<CubeMoveX>();
			cubeMoveX.Init( go);
			break;

		case eMapProp.eMapProp_MoveY:
			go.GetComponent<Renderer>().material.mainTexture = texCube[5];
			go.AddComponent<CubeMoveY>();
			CubeMoveY cubeMoveY = go.GetComponent<CubeMoveY>();
			cubeMoveY.Init( go);
			break;
		}

		return go;
	}

	private GameObject _CreateCoin(int x, int y)
	{
		Vector3 vPos = Vector3.zero;
		GameObject go = GameObject.Instantiate( goCoinSrc) as GameObject;
		vPos.x = m_fCubeSize * x - m_fCubeSize;
		vPos.y = m_fCubeSize * y - m_fCubeSize;
		go.transform.position = vPos;
		go.transform.parent = this.transform;

		return go;
	}

	public void UnLoadCubeMap()
	{
		// cube
		foreach( GameObject go in m_listCube)
			Util.MyDestroy( go);

		m_listCube.Clear();

		// coin
		foreach( GameObject go in m_listCoin)
			Util.MyDestroy( go);
		
		m_listCoin.Clear();
	}

	public void RemoveCoin(GameObject go)
	{
		GameObject goEff = GameObject.Instantiate( goCoinEffSrc) as GameObject;
		goEff.transform.position = go.transform.position;

		AudioManager.Instance.Play( "Sound/coin_eff", 0.3f);

		m_listCoin.Remove( go);
		Util.MyDestroy( go);

		if (UIManager.Instance != null)
			UIManager.Instance.SetPlayInfo( CoinCount);

		// level clear
		if( 0 == CoinCount)
			StartCoroutine( _LevelClear());
	}

	public void RemoveCube(GameObject go)
	{
		GameObject goEff = GameObject.Instantiate( goCubeEffSrc) as GameObject;
		goEff.transform.position = go.transform.position;
		
		AudioManager.Instance.Play( "Sound/cube_break");
		
		Util.MyDestroy( go);
	}

	private IEnumerator _LevelClear()
	{
		GameMain.Instance.eCurState = eGameState.eGameState_Result;

		yield return new WaitForSeconds( 0.5f);

		if (UIManager.Instance != null)
		{
			if( UIManager.eLevelClearType.eLevelClearType_None == UIManager.Instance.eClearType)
			{
				AudioManager.Instance.Play( "Sound/fail", 0.3f);
				UIManager.Instance.textNext.text = "Retry";
				UIManager.Instance.texNext.texture = Resources.Load( "UI/retry_bg") as Texture;
				UIManager.Instance.texResultIcon.enabled = false;
				UIManager.Instance.textResultTime.enabled = false;
			}
			else
			{
				AudioManager.Instance.Play( "Sound/clear");
				UIManager.Instance.texNext.texture = Resources.Load( "UI/done_bg") as Texture;

				if( GameMain.Instance.nCurLevel == GameMain.Instance.nLevelCount)
					UIManager.Instance.textNext.text = "Clear!";
				else
					UIManager.Instance.textNext.text = "Done";

				// < result
				UIManager.Instance.texResultIcon.enabled = true;
				UIManager.Instance.textResultTime.enabled = true;
				int nMin = UIManager.Instance.nGameTime / 60;
				int nSec = UIManager.Instance.nGameTime % 60;
				UIManager.Instance.textResultTime.text = string.Format( "{0:D2}", nMin) + string.Format( ":{0:D2}", nSec);
				if( UIManager.eLevelClearType.eLevelClearType_Gold == UIManager.Instance.eClearType)
					UIManager.Instance.texResultIcon.texture = Resources.Load( "UI/ui_time_gold") as Texture;
				else if( UIManager.eLevelClearType.eLevelClearType_Silver == UIManager.Instance.eClearType)
					UIManager.Instance.texResultIcon.texture = Resources.Load( "UI/ui_time_silver") as Texture;
				else
					UIManager.Instance.texResultIcon.texture = Resources.Load( "UI/ui_time_bronze") as Texture;
				// result >

				if( 0 == GameMain.Instance.nClearType[ GameMain.Instance.nCurLevel - 1])
					GameMain.Instance.nClearType[ GameMain.Instance.nCurLevel - 1] = (int)( UIManager.Instance.eClearType);
				else
				{
					if( GameMain.Instance.nClearType[ GameMain.Instance.nCurLevel - 1] > (int)( UIManager.Instance.eClearType))
						GameMain.Instance.nClearType[ GameMain.Instance.nCurLevel - 1] = (int)( UIManager.Instance.eClearType);
				}
				LevelSelecter.Instance.UpdateSelectBtnStateAndSaveData();
			}

			UIManager.Instance.btnNext.gameObject.SetActive( true);
			
			if( false == UIManager.Instance.texMsgBoxBg.gameObject.activeInHierarchy)
				UIManager.Instance.texNextBtnBg.gameObject.SetActive( true);
		}
	}
}
