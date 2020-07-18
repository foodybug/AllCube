using UnityEngine;
using System.Collections;

public class CubeMoveX : MonoBehaviour
{
	private GameObject m_goCube;
	private Vector3 m_vOrgPos;
	private Vector3 m_vCurPos;
	private bool m_bIncrease = true;
	private float m_fMoveLeft = 0.0f;
	private float m_fMoveRight = 0.0f;
	public Vector3 CurPos { get{ return m_vCurPos;}}

	void Start()
	{
	}
	
	void Update()
	{
		if( 0.0f == m_fMoveLeft && 0.0f == m_fMoveRight)
			return;

		if( eGameState.eGameState_Pause == GameMain.Instance.eCurState || true == UIManager.Instance.goHelpMsgBox.activeInHierarchy)
			return;

		if( true == m_bIncrease)
		{
			m_vCurPos.x += ( 3.0f * Time.deltaTime);

			if( m_vCurPos.x >= m_vOrgPos.x + m_fMoveRight)
			{
				m_vCurPos.x = m_vOrgPos.x + m_fMoveRight;
				m_bIncrease = false;
			}
		}
		else
		{
			m_vCurPos.x -= ( 3.0f * Time.deltaTime);
			
			if( m_vCurPos.x <= m_vOrgPos.x - m_fMoveLeft)
			{
				m_vCurPos.x = m_vOrgPos.x - m_fMoveLeft;
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

	public void SetMove(float fLeft, float fRight)
	{
		m_fMoveLeft = fLeft;
		m_fMoveRight = fRight;
	}
}
