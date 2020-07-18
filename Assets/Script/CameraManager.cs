using UnityEngine;
using System.Collections;

public class CameraManager : MonoBehaviour
{
	static CameraManager m_instance;
	public static CameraManager Instance{ get{ return m_instance;}}

	public Camera mainCamera;
	public Camera uiCamera;

	private Transform m_target;
	private float m_fFollowSpeed = 1.0f;
	private float m_fMinZoomSpeed = 15.0f;
	private float m_fMaxZoomSpeed = 40.0f;
	private float m_fOrthographicSize_Min = 12;
	private float m_fOrthographicSize_Max = 20;
	private float m_fZoomFactorMin = 0.1f;
	private float m_fZoomFactorMax = 0.1f;

	void Awake()
	{
		m_instance = this;
	}

	void Start()
	{
	}
	
	void Update()
	{
	}

	void FixedUpdate()
	{
		if( mainCamera == null || m_target == null || m_target.GetComponent<Rigidbody>() == null)
			return;

		// update camera pos
		Vector3 vStart = mainCamera.transform.position;
		Vector3 vEnd = Vector3.MoveTowards( vStart, m_target.position, m_fFollowSpeed);// * Time.deltaTime);
		vEnd.z = vStart.z;
		mainCamera.transform.position = vEnd;

		// update orthographic size
		float fSpeed = m_target.GetComponent<Rigidbody>().velocity.magnitude;
		float scl = Mathf.Clamp01( ( fSpeed - m_fMinZoomSpeed) / (m_fMaxZoomSpeed - m_fMinZoomSpeed));
		float targetZoomFactor = Mathf.Lerp( m_fOrthographicSize_Min, m_fOrthographicSize_Max, scl);
		float fDelta = ( targetZoomFactor - mainCamera.orthographicSize) > 0.0f ? m_fZoomFactorMax : m_fZoomFactorMin;
		mainCamera.orthographicSize = Mathf.MoveTowards( mainCamera.orthographicSize, targetZoomFactor, fDelta);// * Time.deltaTime);
	}

	public void Init()
	{
		if( mainCamera == null)
			return;

		mainCamera.transform.position = new Vector3 (0.0f, 0.0f, -10.0f);
		mainCamera.orthographicSize = m_fOrthographicSize_Min;
	}

	public void SetTarget(GameObject go)
	{
		if( null != go)
			m_target = go.transform;
	}
}
