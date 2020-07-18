using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class LevelSelecter : MonoBehaviour
{
	public enum eLevelSelectBtnState
	{
		eLevelSelectBtnState_Lock = 0,
		eLevelSelectBtnState_Clear,
		eLevelSelectBtnState_Cur
	}

	static LevelSelecter m_instance;
	public static LevelSelecter Instance{ get{ return m_instance;}}

	public GameObject goSelectSrc;
	public int nSelectCubeCountX;
	public int nSelectCubeCountY;

	private Dictionary<int, LevelSelect> m_dicLevelSelect = new Dictionary<int, LevelSelect>();

	void Awake()
	{
		m_instance = this;
	}

	void Start()
	{
		_Create();
	}
	
	void Update()
	{
	}

	private void _Create()
	{
		int nLevel = 1;
		float fSize = 40.0f;
		float fSpace = 5.0f;
		float fStartX = -100.0f - ( fSpace * 2);
		float fStartY = 160.0f + ( fSpace * 4);

		for( int y = 0; y < nSelectCubeCountY; y++)
		{
			for( int x = 0; x < nSelectCubeCountX; x++)
			{
				Vector3 vPos = new Vector3( fStartX + ( x * fSize) + ( x * fSpace), fStartY - ( y * fSize) - ( y * fSpace), 0.0f);
				_Create( vPos, nLevel);
				nLevel++;
			}
		}
	}

	private void _Create(Vector3 vPos, int nLevel)
	{
		GameObject go = GameObject.Instantiate( goSelectSrc) as GameObject;
		go.transform.parent = this.transform;
		go.transform.localPosition = vPos;
		go.transform.localScale = Vector3.one;
		
		LevelSelect lvSelect = go.GetComponent<LevelSelect>();
		if( GameMain.Instance.nSaveLevel < nLevel)
			lvSelect.SetState( nLevel, eLevelSelectBtnState.eLevelSelectBtnState_Lock);
		else if( GameMain.Instance.nSaveLevel > nLevel)
			lvSelect.SetState( nLevel, eLevelSelectBtnState.eLevelSelectBtnState_Clear);
		else
		{
			if( GameMain.Instance.nLevelCount == nLevel && GameMain.Instance.nClearType[ nLevel - 1] != 0)
				lvSelect.SetState( nLevel, eLevelSelectBtnState.eLevelSelectBtnState_Clear);
			else
				lvSelect.SetState( nLevel, eLevelSelectBtnState.eLevelSelectBtnState_Cur);
		}

		m_dicLevelSelect.Add( nLevel, lvSelect);
	}

	public void UpdateSelectBtnStateAndSaveData()
	{
		if( GameMain.Instance.nSaveLevel == GameMain.Instance.nCurLevel && GameMain.Instance.nLevelCount > GameMain.Instance.nCurLevel)
		{
			int nLevel = GameMain.Instance.nCurLevel + 1;

			if( nLevel > GameMain.Instance.nLevelCount)
				return;

			m_dicLevelSelect[nLevel-1].SetState( nLevel-1, eLevelSelectBtnState.eLevelSelectBtnState_Clear);

			if( nLevel == GameMain.Instance.nLevelCount)
				m_dicLevelSelect[nLevel].SetState( nLevel, eLevelSelectBtnState.eLevelSelectBtnState_Clear);
			else
				m_dicLevelSelect[nLevel].SetState( nLevel, eLevelSelectBtnState.eLevelSelectBtnState_Cur);

			GameMain.Instance.nSaveLevel = nLevel;
			GameMain.Instance.SaveData();
		}
		else
			m_dicLevelSelect[GameMain.Instance.nCurLevel].SetState( GameMain.Instance.nCurLevel, eLevelSelectBtnState.eLevelSelectBtnState_Clear);
	}
}
