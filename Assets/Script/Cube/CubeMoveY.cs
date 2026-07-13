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

	private Rigidbody m_rb;

	void Awake()
	{
		m_goCube = gameObject;
		m_rb = GetComponent<Rigidbody>();
		if (m_rb == null)
		{
			m_rb = gameObject.AddComponent<Rigidbody>();
		}
		m_rb.isKinematic = true;
		m_rb.useGravity = false;
	}

	void Start()
	{
		m_vOrgPos = m_vCurPos = transform.position;

		Renderer rend = GetComponent<Renderer>();
		if (rend != null && MapManager.Instance != null)
		{
			rend.sharedMaterial = MapManager.Instance.GetSharedMaterial(5);
		}
	}
	
	void FixedUpdate()
	{
		if( 0.0f == m_fMoveUp && 0.0f == m_fMoveDown)
			return;

		if (MainManager.Instance == null)
			return;

		bool isHelpActive = false;
		if (UI_Play.Instance != null && UI_Play.Instance.ui != null && UI_Play.Instance.ui.goHelpMsgBox != null)
		{
			isHelpActive = UI_Play.Instance.ui.goHelpMsgBox.activeInHierarchy;
		}

		if (eGameState.eGameState_Pause == MainManager.Instance.eCurState || isHelpActive)
			return;

		if( true == m_bIncrease)
		{
			m_vCurPos.y += ( 3.0f * Time.fixedDeltaTime);
			
			if( m_vCurPos.y >= m_vOrgPos.y + m_fMoveUp)
			{
				m_vCurPos.y = m_vOrgPos.y + m_fMoveUp;
				m_bIncrease = false;
			}
		}
		else
		{
			m_vCurPos.y -= ( 3.0f * Time.fixedDeltaTime);
			
			if( m_vCurPos.y <= m_vOrgPos.y - m_fMoveDown)
			{
				m_vCurPos.y = m_vOrgPos.y - m_fMoveDown;
				m_bIncrease = true;
			}
		}
		
		if (m_rb != null)
		{
			m_rb.MovePosition(m_vCurPos);
		}
		else
		{
			m_goCube.transform.position = m_vCurPos;
		}
	}

	public void Init(GameObject go)
	{
		if (m_goCube == null) m_goCube = go;
		if (m_rb == null) m_rb = m_goCube.GetComponent<Rigidbody>();
		m_vOrgPos = m_vCurPos = m_goCube.transform.position;
	}

	public void SetMove(float fUp, float fDown)
	{
		m_fMoveUp = fUp;
		m_fMoveDown = fDown;
	}
}
