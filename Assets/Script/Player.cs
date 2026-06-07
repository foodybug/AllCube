using System.Collections;
using UnityEngine;

public class Player : MonoBehaviour
{
    public Texture texPlayer_On;
    public Texture texPlayer_Off;

    [SerializeField] private float addForceLimit = 0.05f;
    [SerializeField] private float amount = 200.0f;
    [SerializeField] private float amountX = 140.0f; // 줄어든 가로 점프 거리 (기본값 140, 기존 400)
    [SerializeField] private float torque = 40;
    private float forceWait = 0;
    private float moveX = 0.0f;
    private bool AllowAddForce { get { return forceWait < 0.0f; } }
    private float moveCubeForce = 5.0f;
    private float nextJumpDir = 1.0f;
    private float lastMoveX = 0.0f;

    [SerializeField] private int m_jumpCount = 10;
    public int JumpCount { get { return m_jumpCount; } }
    public float NextJumpDir { get { return nextJumpDir; } }

    private float initialRotationX;
    private float initialRotationY;

    void Awake()
    {
        initialRotationX = transform.rotation.eulerAngles.x;
        initialRotationY = transform.rotation.eulerAngles.y;
    }

    void Start()
    {
        // 2D 평면 움직임과 회전을 위해 Z축 이동 및 X/Y축 회전을 고정
        Rigidbody rb = GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.constraints = RigidbodyConstraints.FreezePositionZ | RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationY;
        }
    }

    void Update()
    {
        if (eGameState.eGameState_Play != MainManager.Instance.eCurState)
            return;

        if (UI_Play.Instance != null && true == UI_Play.Instance.bPauseTime)
        {
            bool isWaitingStart = (PlayMain.Instance != null && !PlayMain.Instance.IsGameStarted);
            bool isPopUpActive = (UI_Play.Instance.ui != null && UI_Play.Instance.ui.goHelpMsgBox != null && UI_Play.Instance.ui.goHelpMsgBox.activeInHierarchy)
                              || (UI_Play.Instance.ui != null && UI_Play.Instance.ui.goMsgBox != null && UI_Play.Instance.ui.goMsgBox.activeInHierarchy);

            if (!isWaitingStart || isPopUpActive)
            {
                return;
            }
        }

        forceWait -= Time.deltaTime;

        Texture tex = AllowAddForce ? texPlayer_On : texPlayer_Off;
        if (transform.GetComponent<Renderer>().material.mainTexture != tex)
            transform.GetComponent<Renderer>().material.mainTexture = tex;

        if (true == AllowAddForce)
        {
            bool bInput = false;

            if (Input.GetKeyDown(KeyCode.RightArrow) || Input.GetKeyDown(KeyCode.LeftArrow) || Input.GetKeyDown(KeyCode.Space) || Input.GetMouseButtonDown(0))
            {
                bInput = true;
                Debug.Log($"[Player Debug] Input detected. KeyCode/Mouse. AllowAddForce: {AllowAddForce}, JumpCount: {m_jumpCount}");
            }

            if (Input.touchCount > 0 && Input.touches[0].phase == TouchPhase.Began)
            {
                if (CameraManager.Instance.uiCamera != null)
                {
                    Ray ray = CameraManager.Instance.uiCamera.ScreenPointToRay(Input.GetTouch(0).position);
                    RaycastHit hit;
                    if (true == Physics.Raycast(ray, out hit) && hit.collider.gameObject.layer == LayerMask.NameToLayer("MyUI"))
                    {
                    }
                    else
                    {
                        bInput = true;
                        Debug.Log($"[Player Debug] Touch Input detected. AllowAddForce: {AllowAddForce}, JumpCount: {m_jumpCount}");
                    }
                }
                else
                {
                    bInput = true;
                    Debug.Log($"[Player Debug] Touch Input detected (No UI Camera). AllowAddForce: {AllowAddForce}, JumpCount: {m_jumpCount}");
                }
            }

            if (bInput)
            {
                if (PlayMain.Instance != null && !PlayMain.Instance.IsGameStarted)
                {
                    PlayMain.Instance.StartGame();
                }
                else
                {
                    moveX = nextJumpDir;
                    nextJumpDir *= -1.0f;
                    Debug.Log($"[Player Debug] Jump prepared. moveX: {moveX}, nextJumpDir: {nextJumpDir}");
                }
            }
        }
    }

    void FixedUpdate()
    {
        if (AllowAddForce && moveX != 0)
        {
            Debug.Log($"[Player Debug] FixedUpdate Jump execution. JumpCount: {m_jumpCount}, moveX: {moveX}");
            if (m_jumpCount > 0)
            {
                m_jumpCount--;
                forceWait = addForceLimit;

                if (GetComponent<Rigidbody>() != null)
                {
                    Rigidbody rb = GetComponent<Rigidbody>();
                    rb.linearVelocity = Vector3.zero;
                    rb.angularVelocity = Vector3.zero;

                    rb.AddForce(new Vector3(moveX * amountX, amount, 0) * Time.deltaTime, ForceMode.Impulse);
                    rb.AddTorque(new Vector3(0, 0, -moveX * torque) * Time.deltaTime, ForceMode.Impulse);

                    AudioManager.Instance.Play("Sound/jump", 0.5f);
                    lastMoveX = moveX;
                    Debug.Log($"[Player Debug] Jump force applied. New velocity: {rb.linearVelocity}, New JumpCount: {m_jumpCount}");
                }
            }
            else
            {
                Debug.LogWarning("[Player Debug] Jump failed because JumpCount is 0!");
            }

            moveX = 0;
        }
    }

    public void ExecuteFirstJump()
    {
        StartCoroutine(FirstJumpDelay_CR(nextJumpDir));
        nextJumpDir *= -1.0f;
    }

    private IEnumerator FirstJumpDelay_CR(float jumpDir)
    {
        yield return new WaitForSeconds(0.05f);
        moveX = jumpDir;
        Debug.Log($"[Player Debug] First Jump executed after safety time delay. moveX: {moveX}");
    }

    public void AddJumps(int count)
    {
        m_jumpCount += count;
    }

    public void ResetJumpCount(int count = 10)
    {
        m_jumpCount = count;
    }

    void OnTriggerEnter(Collider collider)
    {
        CubeBreak cubeBreak = collider.gameObject.GetComponent<CubeBreak>();

        if (null == cubeBreak)
            MapManager.Instance.RemoveCoin(collider.gameObject);
        else
            MapManager.Instance.RemoveCube(collider.gameObject);
    }

    void OnCollisionEnter(Collision collision)
    {
        // Floor_DeadZone 충돌 검출 (낙하사)
        if (collision.gameObject.name == "Floor_DeadZone")
        {
            if (m_jumpCount <= 0)
            {
                MapManager.Instance.TriggerGameOver();
            }
            return;
        }

        Rigidbody rb = GetComponent<Rigidbody>();
        if (rb != null && collision.contacts.Length > 0)
        {
            float normalY = collision.contacts[0].normal.y;

            // 천장에 부딪혔을 때 가로 속도(모멘텀) 유지
            if (normalY < -0.5f)
            {
                Vector3 vel = rb.linearVelocity;
                vel.x = lastMoveX * amountX * Time.fixedDeltaTime / rb.mass;
                rb.linearVelocity = vel;
            }

            // 충돌 발생 시 물리 회전 속도를 멈추고 90도 단위로 깔끔하게 정렬 (초기 X, Y 회전값 유지)
            rb.angularVelocity = Vector3.zero;
            Vector3 euler = transform.rotation.eulerAngles;
            float snappedZ = Mathf.Round(euler.z / 90.0f) * 90.0f;
            transform.rotation = Quaternion.Euler(initialRotationX, initialRotationY, snappedZ);
        }

        // Break
        CubeBreak cubeBreak = collision.gameObject.GetComponent<CubeBreak>();
        if (null != cubeBreak)
        {
            if (GetComponent<Rigidbody>().linearVelocity.x > 1.0f || GetComponent<Rigidbody>().linearVelocity.y > 1.0f)
            {
                //Debug.Log( "OnTriggerEnter: Cube: " + rigidbody.velocity);
                if (cubeBreak.CollisionCube() <= 0)
                    collision.collider.isTrigger = true;
            }

            return;
        }

        // MoveX
        CubeMoveX cubeMoveX = collision.gameObject.GetComponent<CubeMoveX>();
        if (null != cubeMoveX)
        {
            Vector3 playerPos = GetComponent<Rigidbody>().position;
            Vector3 vForce = playerPos - cubeMoveX.CurPos;
            Debug.Log($"[Player Debug] Collision with CubeMoveX. Player Pos: {playerPos}, Block Pos: {cubeMoveX.CurPos}, Raw Force Vector: {vForce}");
            if (vForce.sqrMagnitude < 0.001f)
            {
                vForce = new Vector3(nextJumpDir, 1.0f, 0.0f);
            }
            vForce.Normalize();
            if (vForce.y < 0.5f)
            {
                vForce.y = 0.5f;
                vForce.Normalize();
            }
            vForce *= moveCubeForce;
            GetComponent<Rigidbody>().AddForce(vForce, ForceMode.Impulse);
            AudioManager.Instance.Play("Sound/jumppad");
            Debug.Log($"[Player Debug] CubeMoveX Force Applied: {vForce}. Player Velocity: {GetComponent<Rigidbody>().linearVelocity}");

            return;
        }

        // MoveY
        CubeMoveY cubeMoveY = collision.gameObject.GetComponent<CubeMoveY>();
        if (null != cubeMoveY)
        {
            Vector3 playerPos = GetComponent<Rigidbody>().position;
            Vector3 vForce = playerPos - cubeMoveY.CurPos;
            Debug.Log($"[Player Debug] Collision with CubeMoveY. Player Pos: {playerPos}, Block Pos: {cubeMoveY.CurPos}, Raw Force Vector: {vForce}");
            if (vForce.sqrMagnitude < 0.001f)
            {
                vForce = new Vector3(nextJumpDir, 1.0f, 0.0f);
            }
            vForce.Normalize();
            if (vForce.y < 0.5f)
            {
                vForce.y = 0.5f;
                vForce.Normalize();
            }
            vForce *= moveCubeForce;
            GetComponent<Rigidbody>().AddForce(vForce, ForceMode.Impulse);
            AudioManager.Instance.Play("Sound/jumppad");
            Debug.Log($"[Player Debug] CubeMoveY Force Applied: {vForce}. Player Velocity: {GetComponent<Rigidbody>().linearVelocity}");

            return;
        }
    }
}
