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

	private int m_nTotalCoinsCollected = 0;
	public int TotalCoinsCollected { get { return m_nTotalCoinsCollected; } }

	private int m_highestGeneratedY = 0;
	private GameObject m_goFloor;

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
		if (MainManager.Instance == null) return;

		if (MainManager.Instance.eCurState == eGameState.eGameState_Play)
		{
			GameObject playerGo = CameraManager.Instance.Target != null ? CameraManager.Instance.Target.gameObject : null;
			if (playerGo != null)
			{
				float playerY = playerGo.transform.position.y / (m_fCubeSize > 0f ? m_fCubeSize : 1.0f);

				// 1. Generate up to playerY + 15 rows ahead
				int targetY = Mathf.CeilToInt(playerY) + 15;
				if (targetY > m_highestGeneratedY)
				{
					GenerateRowsUpTo(targetY);
				}

				// 2. Clean up old blocks that are far below the player Y - 10
				CleanupBlocksBelow(Mathf.FloorToInt(playerY) - 10);

				// 3. Fall to death check (Moved to OnTriggerEnter in Player.cs for a 1-second delay camera stop effect)

				// 4. Dynamic warning / visual feedback on all active coins
				Player player = playerGo.GetComponent<Player>();
				if (player != null)
				{
					float speedMultiplier = Mathf.Max(1.0f, 15.0f / (player.JumpCount + 1f));

					// Y spin and Z tilt
					float targetZRotation = player.NextJumpDir > 0 ? -45f : 45f;
					float spinSpeed = 30f * speedMultiplier;
					float lerpSpeed = 5f * speedMultiplier;

					// HSV rainbow shift
					float hue = (Time.time * 0.08f * speedMultiplier) % 1.0f;
					Color rainbowColor = Color.HSVToRGB(hue, 0.9f, 0.9f);

					foreach (GameObject coin in m_listCoin)
					{
						if (coin != null)
						{
							coin.transform.Rotate(0, spinSpeed * Time.deltaTime, 0, Space.World);

							Quaternion targetRot = Quaternion.Euler(0, coin.transform.rotation.eulerAngles.y, targetZRotation);
							coin.transform.rotation = Quaternion.Lerp(coin.transform.rotation, targetRot, Time.deltaTime * lerpSpeed);

							Renderer r = coin.GetComponentInChildren<Renderer>();
							if (r != null)
							{
								r.material.color = rainbowColor;
							}
						}
					}
				}
			}
		}
	}

	public void LoadCubeMap(int nStage)
	{
		UnLoadCubeMap();
		m_highestGeneratedY = 0;
		m_nTotalCoinsCollected = 0;

		SpawnFloor();

		// Spawn starting platform and walls
		GenerateRowsUpTo(15);
	}

	private void SpawnFloor()
	{
		if (m_goFloor == null)
		{
			m_goFloor = GameObject.CreatePrimitive(PrimitiveType.Cube);
			m_goFloor.name = "Floor_DeadZone";
			
			Renderer rend = m_goFloor.GetComponent<Renderer>();
			if (rend != null)
			{
				rend.material.color = Color.gray;
			}

			Collider col = m_goFloor.GetComponent<Collider>();
			if (col != null)
			{
				col.isTrigger = true;
			}
		}

		m_goFloor.transform.position = new Vector3(0f, -2.5f, 0f);
		m_goFloor.transform.localScale = new Vector3(100f, 2f, 100f); // XZ 100, Y 두께 2
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
			Rigidbody rbX = go.GetComponent<Rigidbody>();
			if (rbX == null) rbX = go.AddComponent<Rigidbody>();
			rbX.isKinematic = true;
			rbX.useGravity = false;
			go.AddComponent<CubeMoveX>();
			CubeMoveX cubeMoveX = go.GetComponent<CubeMoveX>();
			cubeMoveX.Init( go);
			break;

		case eMapProp.eMapProp_MoveY:
			go.GetComponent<Renderer>().material.mainTexture = texCube[5];
			Rigidbody rbY = go.GetComponent<Rigidbody>();
			if (rbY == null) rbY = go.AddComponent<Rigidbody>();
			rbY.isKinematic = true;
			rbY.useGravity = false;
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

		MeshFilter mf = go.GetComponentInChildren<MeshFilter>();
		if (mf != null)
		{
			GameObject tempCube = GameObject.CreatePrimitive(PrimitiveType.Cube);
			MeshFilter tempMf = tempCube.GetComponent<MeshFilter>();
			if (tempMf != null)
			{
				mf.sharedMesh = tempMf.sharedMesh;
			}
			Util.MyDestroy(tempCube);
		}

		go.transform.localScale = new Vector3(0.5f, 0.5f, 0.5f);

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

		if (m_goFloor != null)
		{
			Util.MyDestroy(m_goFloor);
			m_goFloor = null;
		}
	}

	public void RemoveCoin(GameObject go)
	{
		GameObject goEff = GameObject.Instantiate( goCoinEffSrc) as GameObject;
		goEff.transform.position = go.transform.position;

		AudioManager.Instance.Play( "Sound/coin_eff", 0.3f);

		m_listCoin.Remove( go);
		Util.MyDestroy( go);

		m_nTotalCoinsCollected++;

		if (CameraManager.Instance.Target != null)
		{
			Player player = CameraManager.Instance.Target.GetComponent<Player>();
			if (player != null)
			{
				player.AddJumps(3);
			}
		}

		if (UI_Play.Instance != null)
		{
			Player player = CameraManager.Instance.Target != null ? CameraManager.Instance.Target.GetComponent<Player>() : null;
			if (player != null)
			{
				UI_Play.Instance.SetPlayStats(m_nTotalCoinsCollected, player.JumpCount);
			}
		}
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
		Debug.Log("[MapManager Debug] Starting _LevelClear sequence.");
		if (MainManager.Instance != null)
		{
			MainManager.Instance.eCurState = eGameState.eGameState_Result;
		}

		yield return new WaitForSeconds( 0.5f);

		// Result 씬으로 넘어가기 전 Play 정보를 정적 필드에 안전하게 백업
		MainManager.lastTotalCoins = MapManager.Instance.TotalCoinsCollected;
		if (UI_Play.Instance != null)
		{
			MainManager.lastGameTime = UI_Play.Instance.nGameTime;
			MainManager.lastClearType = UI_Play.Instance.eClearType;
			MainManager.lastMaxHeight = UI_Play.Instance.MaxHeightThisRun;

			int allTimeBest = 0;
			int levelIdx = MainManager.nCurLevelStatic - 1;
			if (MainManager.Instance != null && MainManager.Instance.nBestHeight != null && levelIdx >= 0 && levelIdx < MainManager.Instance.nBestHeight.Length)
			{
				allTimeBest = MainManager.Instance.nBestHeight[levelIdx];
			}
			MainManager.lastBestHeight = allTimeBest;
		}

		if (MainManager.Instance != null)
		{
			Debug.Log("[MapManager Debug] Transitioning to Result scene.");
			MainManager.Instance.TransitionToScene("Result");
		}
		else
		{
			// UI가 없을 경우 테스트를 위해 자동으로 다음 레벨 진행
			AudioManager.Instance.Play("Sound/clear");
			yield return new WaitForSeconds(1.0f);
			
			// MainManager가 존재하지 않는 특수한 경우엔 동작하지 않음
		}
	}

	public void TriggerGameOver()
	{
		Debug.Log($"[MapManager Debug] TriggerGameOver called. MainManager Instance: {(MainManager.Instance != null ? "Not Null" : "Null")}, CurState: {(MainManager.Instance != null ? MainManager.Instance.eCurState.ToString() : "N/A")}");
		if (MainManager.Instance != null && MainManager.Instance.eCurState == eGameState.eGameState_Play)
		{
			StartCoroutine(_LevelClear());
		}
	}

	private void GenerateRowsUpTo(int targetY)
	{
		if (m_fCubeSize <= 0f)
		{
			m_fCubeSize = 1.0f;
		}

		for (int y = m_highestGeneratedY; y <= targetY; y++)
		{
			m_listCube.Add(_CreateCube(-4, y, eMapProp.eMapProp_Normal));
			m_listCube.Add(_CreateCube(4, y, eMapProp.eMapProp_Normal));

			// 보석(Coin) 배치 (y >= 3 이고 3의 배수 행마다 좌우 교대로 공중에 배치)
			if (y >= 3 && y % 3 == 0)
			{
				bool isLeft = (y / 3) % 2 == 1;
				if (isLeft)
				{
					m_listCoin.Add(_CreateCoin(-2, y));
				}
				else
				{
					m_listCoin.Add(_CreateCoin(2, y));
				}
			}
		}
		m_highestGeneratedY = targetY + 1;
	}

	private void CleanupBlocksBelow(int limitY)
	{
		for (int i = m_listCube.Count - 1; i >= 0; i--)
		{
			GameObject go = m_listCube[i];
			if (go != null)
			{
				int gridY = Mathf.RoundToInt((go.transform.position.y + m_fCubeSize) / (m_fCubeSize > 0f ? m_fCubeSize : 1.0f));
				if (gridY < limitY)
				{
					m_listCube.RemoveAt(i);
					Util.MyDestroy(go);
				}
			}
			else
			{
				m_listCube.RemoveAt(i);
			}
		}

		for (int i = m_listCoin.Count - 1; i >= 0; i--)
		{
			GameObject go = m_listCoin[i];
			if (go != null)
			{
				int gridY = Mathf.RoundToInt((go.transform.position.y + m_fCubeSize) / (m_fCubeSize > 0f ? m_fCubeSize : 1.0f));
				if (gridY < limitY)
				{
					m_listCoin.RemoveAt(i);
					Util.MyDestroy(go);
				}
			}
			else
			{
				m_listCoin.RemoveAt(i);
			}
		}
	}
}
