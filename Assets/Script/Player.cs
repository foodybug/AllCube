using UnityEngine;
using System.Collections;

public class Player : MonoBehaviour
{
	public Texture texPlayer_On;
	public Texture texPlayer_Off;

	private float addForceLimit = 0.5f;
	private float amount = 400.0f;
	private float torque = 40;
	private float forceWait = 0;
	private float moveX = 0.0f;
	private bool AllowAddForce { get { return forceWait < 0.0f;}}
	private float moveCubeForce = 5.0f;
	private float nextJumpDir = 1.0f;
	
	void Start()
	{
	}
	
	void Update()
	{
		if( eGameState.eGameState_Play != GameMain.Instance.eCurState)
			return;

		if( UIManager.Instance != null && true == UIManager.Instance.bPauseTime)
			return;

		forceWait -= Time.deltaTime;
		
		Texture tex = AllowAddForce ? texPlayer_On : texPlayer_Off;
		if( transform.GetComponent<Renderer>().material.mainTexture != tex)
			transform.GetComponent<Renderer>().material.mainTexture = tex;
		
		if( true == AllowAddForce)
		{
			bool bInput = false;
			
			if( Input.GetKeyDown( KeyCode.RightArrow) || Input.GetKeyDown( KeyCode.LeftArrow) || Input.GetKeyDown( KeyCode.Space))
				bInput = true;

			if( Input.touchCount > 0 && Input.touches[0].phase == TouchPhase.Began)
			{
				Ray ray = CameraManager.Instance.uiCamera.ScreenPointToRay( Input.GetTouch( 0).position);
				RaycastHit hit;
				if( true == Physics.Raycast( ray, out hit) && hit.collider.gameObject.layer == LayerMask.NameToLayer( "MyUI"))
				{
				}
				else
					bInput = true;
			}

			if( bInput)
			{
				moveX = nextJumpDir;
				nextJumpDir *= -1.0f;
			}
		}
	}
	
	void FixedUpdate()
	{
		if( AllowAddForce && moveX != 0)
		{
			forceWait = addForceLimit;
			
			if( GetComponent<Rigidbody>() != null)
			{
				GetComponent<Rigidbody>().AddForce( new Vector3( moveX * amount, amount, 0) * Time.deltaTime, ForceMode.Impulse);
				GetComponent<Rigidbody>().AddTorque( new Vector3( 0, 0, -moveX * torque) * Time.deltaTime, ForceMode.Impulse);

				AudioManager.Instance.Play( "Sound/jump", 0.5f);
			}
			
			moveX = 0;
		}
	}
	
	void OnTriggerEnter(Collider collider)
	{
		CubeBreak cubeBreak = collider.gameObject.GetComponent<CubeBreak>();

		if( null == cubeBreak)
			MapManager.Instance.RemoveCoin( collider.gameObject);
		else
			MapManager.Instance.RemoveCube( collider.gameObject);
	}

	void OnCollisionEnter(Collision collision)
	{
		// Break
		CubeBreak cubeBreak = collision.gameObject.GetComponent<CubeBreak>();
		if( null != cubeBreak)
		{
			if( GetComponent<Rigidbody>().linearVelocity.x > 1.0f || GetComponent<Rigidbody>().linearVelocity.y > 1.0f)
			{
				//Debug.Log( "OnTriggerEnter: Cube: " + rigidbody.velocity);
				if( cubeBreak.CollisionCube() <= 0)
					collision.collider.isTrigger = true;
			}

			return;
		}

		// MoveX
		CubeMoveX cubeMoveX = collision.gameObject.GetComponent<CubeMoveX>();
		if( null != cubeMoveX)
		{
			Vector3 vForce = GetComponent<Rigidbody>().position - cubeMoveX.CurPos;
			vForce.Normalize();
			vForce *= moveCubeForce;
			GetComponent<Rigidbody>().AddForce( vForce, ForceMode.Impulse);
			AudioManager.Instance.Play( "Sound/jumppad");

			return;
		}

		// MoveY
		CubeMoveY cubeMoveY = collision.gameObject.GetComponent<CubeMoveY>();
		if( null != cubeMoveY)
		{
			Vector3 vForce = GetComponent<Rigidbody>().position - cubeMoveY.CurPos;
			vForce.Normalize();
			vForce *= moveCubeForce;
			GetComponent<Rigidbody>().AddForce( vForce, ForceMode.Impulse);
			AudioManager.Instance.Play( "Sound/jumppad");

			return;
		}
	}
}
