using UnityEngine;
using System.Collections;

public class CubeMoveY : MonoBehaviour
{
	private GameObject m_goCube;
	private Vector3 m_vOrgPos;
	private Vector3 m_vCurPos;
	private bool m_bIncrease = true;
	private float m_fMoveUp = 0.0f;
	private float m_fMoveDown = 0.0f;
	public Vector3 CurPos { get{ return m_vCurPos;}}

	void Start()
	{
	}
	
	void Update()
	{
		if( 0.0f == m_fMoveUp && 0.0f == m_fMoveDown)
			return;

		if( eGameState.eGameState_Pause == GameMain.Instance.eCurState || true == UIManager.Instance.goHelpMsgBox.activeInHierarchy)
			return;

		if( true == m_bIncrease)
		{
			m_vCurPos.y += ( 3.0f * Time.deltaTime);
			
			if( m_vCurPos.y >= m_vOrgPos.y + m_fMoveUp)
			{
				m_vCurPos.y = m_vOrgPos.y + m_fMoveUp;
				m_bIncrease = false;
			}
		}
		else
		{
			m_vCurPos.y -= ( 3.0f * Time.deltaTime);
			
			if( m_vCurPos.y <= m_vOrgPos.y - m_fMoveDown)
			{
				m_vCurPos.y = m_vOrgPos.y - m_fMoveDown;
				m_bIncrease = true;
			}
		}
		
		m_goCube.transform.position = m_vCurPos;
	}

	public void Init(GameObject go)
	{
		m_goCube = go;
		m_vOrgPos = m_vCurPos = m_goCube.transform.position;
	}

	public void SetMove(float fUp, float fDown)
	{
		m_fMoveUp = fUp;
		m_fMoveDown = fDown;
	}
}
