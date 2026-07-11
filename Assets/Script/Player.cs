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

    [Header("Combo Physics Settings")]
    [SerializeField] private float m_comboJumpForceMultiplier = 0.02f;
    [SerializeField] private float m_maxComboJumpForceBonus = 0.40f;

    [Header("Combo Audio Settings")]
    [SerializeField] private string m_jumpSoundPath = "Sound/jump";
    [SerializeField] private float m_jumpSoundVolume = 0.5f;
    [SerializeField] private float m_comboPitchMultiplier = 0.05f;
    [SerializeField] private float m_maxComboPitchBonus = 1.0f;
    private float forceWait = 0;
    private float moveX = 0.0f;
    private bool AllowAddForce { get { return forceWait < 0.0f && !m_bDead; } }
    private float moveCubeForce = 5.0f;
    private float nextJumpDir = 1.0f;
    private float lastMoveX = 0.0f;
    private bool m_bDead = false;



    // 캐싱된 컴포넌트 변수
    private Rigidbody m_rb;
    private Renderer m_renderer;

    [SerializeField] private int m_jumpCount = 10;
    public int JumpCount { get { return m_jumpCount; } }
    public float NextJumpDir { get { return nextJumpDir; } }

    private float initialRotationX;
    private float initialRotationY;

    void Awake()
    {
        initialRotationX = transform.rotation.eulerAngles.x;
        initialRotationY = transform.rotation.eulerAngles.y;
        m_rb = GetComponent<Rigidbody>();
        m_renderer = GetComponent<Renderer>();
    }

    void Start()
    {
        // 2D 평면 움직임과 회전을 위해 Z축 이동 및 X/Y축 회전을 고정
        if (m_rb != null)
        {
            m_rb.constraints = RigidbodyConstraints.FreezePositionZ | RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationY;
            m_rb.collisionDetectionMode = CollisionDetectionMode.Continuous; // 물리 관통(터널링) 방지
        }
    }

    void Update()
    {
        if (MainManager.Instance != null && eGameState.eGameState_Play != MainManager.Instance.eCurState)
            return;

        // 테스트/디버그용: R키를 누르면 점프 횟수 +10
        if (Input.GetKeyDown(KeyCode.R))
        {
            AddJumps(10);
            Debug.Log($"[Player Test] R Key pressed. Added 10 jumps. Current JumpCount: {JumpCount}");
        }

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
        if (m_renderer != null && m_renderer.material.mainTexture != tex)
            m_renderer.material.mainTexture = tex;

        if (true == AllowAddForce)
        {
            bool bInput = false;

            if (Input.GetKeyDown(KeyCode.RightArrow) || Input.GetKeyDown(KeyCode.LeftArrow) || Input.GetKeyDown(KeyCode.Space) || Input.GetMouseButtonDown(0))
            {
                bInput = true;
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
                    }
                }
                else
                {
                    bInput = true;
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
                }
            }
        }
    }

    void FixedUpdate()
    {
        if (AllowAddForce && moveX != 0)
        {
            if (m_jumpCount > 0)
            {
                m_jumpCount--;
                forceWait = addForceLimit;

                if (JumpIntervalTracker.Instance != null)
                {
                    JumpIntervalTracker.Instance.RecordJumpInput();
                }

                if (m_rb != null)
                {
                    m_rb.linearVelocity = Vector3.zero;
                    m_rb.angularVelocity = Vector3.zero;

                    int combo = JumpIntervalTracker.Instance != null ? JumpIntervalTracker.Instance.ComboCount : 0;
                    float comboJumpMultiplier = 1.0f + Mathf.Min(combo * m_comboJumpForceMultiplier, m_maxComboJumpForceBonus);
                    float finalJumpForce = amount * comboJumpMultiplier;

                    m_rb.AddForce(new Vector3(moveX * amountX, finalJumpForce, 0) * Time.deltaTime, ForceMode.Impulse);
                    m_rb.AddTorque(new Vector3(0, 0, -moveX * torque) * Time.deltaTime, ForceMode.Impulse);

                    float pitch = 1.0f + Mathf.Min(combo * m_comboPitchMultiplier, m_maxComboPitchBonus);
                    AudioManager.Instance.Play(m_jumpSoundPath, m_jumpSoundVolume, pitch);
                    lastMoveX = moveX;
                }
            }
            else
            {
                Debug.LogWarning("[Player Debug] Jump failed because JumpCount is 0!");
            }

            moveX = 0;
        }
    }

    private void HandleWrapAround()
    {
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
    }

    public void AddJumps(int count)
    {
        m_jumpCount += count;
    }

    public void ResetJumpCount(int count = 10)
    {
        m_jumpCount = count;
        m_bDead = false;

        if (JumpIntervalTracker.Instance != null)
        {
            JumpIntervalTracker.Instance.ResetTracker();
        }

        if (m_renderer != null)
        {
            m_renderer.enabled = true;
        }
        Collider col = GetComponent<Collider>();
        if (col != null)
        {
            col.enabled = true;
        }
        if (m_rb != null)
        {
            m_rb.isKinematic = false;
        }
    }

    public void KillPlayer()
    {
        if (!m_bDead)
        {
            m_bDead = true;
            StartCoroutine(DeadZoneRoutine_CR());
        }
    }

    void OnTriggerEnter(Collider collider)
    {
        if (collider.gameObject.name.Contains("Floor_DeadZone"))
        {
            if (!m_bDead)
            {
                m_bDead = true;
                if (MainManager.lastDeathCause != "JumpZero")
                {
                    MainManager.lastDeathCause = "DeadZone";
                }
                StartCoroutine(DeadZoneRoutine_CR());
            }
            return;
        }

        // JumpZero 블록 충돌 처리는 CubeJumpZero 및 CubeFlyingJumpZero 컴포넌트 측에서 전담하므로 Player.cs에서는 통과시킴
        if (collider.gameObject.GetComponent<CubeJumpZero>() != null || collider.gameObject.GetComponent<CubeFlyingJumpZero>() != null)
        {
            return;
        }

        CubeBreak cubeBreak = collider.gameObject.GetComponent<CubeBreak>();

        if (null == cubeBreak)
        {
            // 부딪힌 대상이 진짜 Coin 또는 Coin 컴포넌트를 가진 경우에만 획득 처리
            if (collider.gameObject.name.Contains("Coin") || collider.gameObject.GetComponent<Coin>() != null)
            {
                MapManager.Instance.RemoveCoin(collider.gameObject);
            }
        }
        else
        {
            MapManager.Instance.RemoveCube(collider.gameObject);
        }
    }

    private IEnumerator DeadZoneRoutine_CR()
    {
        Debug.Log($"[Player Debug] DeadZoneRoutine_CR! m_jumpCount: {m_jumpCount}");
        MainManager.lastJumpCount = m_jumpCount;

        if (CameraManager.Instance != null)
        {
            CameraManager.Instance.isFollowing = false;
        }

        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.Play("Sound/cube_break");
        }

        if (MapManager.Instance != null && MapManager.Instance.goCubeEffSrc != null)
        {
            GameObject goEff = Instantiate(MapManager.Instance.goCubeEffSrc) as GameObject;
            goEff.transform.position = transform.position;
        }

        if (m_renderer != null)
        {
            m_renderer.enabled = false;
        }

        if (m_rb != null)
        {
            m_rb.linearVelocity = Vector3.zero;
            m_rb.angularVelocity = Vector3.zero;
            m_rb.isKinematic = true;
        }

        Collider col = GetComponent<Collider>();
        if (col != null)
        {
            col.enabled = false;
        }

        yield return new WaitForSeconds(1.0f);

        MapManager.Instance.TriggerGameOver();
    }

    void OnCollisionEnter(Collision collision)
    {
        Rigidbody rb = m_rb;
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
            if (rb != null && (rb.linearVelocity.x > 1.0f || rb.linearVelocity.y > 1.0f))
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
            Vector3 playerPos = rb != null ? rb.position : transform.position;
            Vector3 vForce = playerPos - cubeMoveX.CurPos;
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
            if (rb != null) rb.AddForce(vForce, ForceMode.Impulse);
            AudioManager.Instance.Play("Sound/jumppad");

            return;
        }

        // MoveY
        CubeMoveY cubeMoveY = collision.gameObject.GetComponent<CubeMoveY>();
        if (null != cubeMoveY)
        {
            Vector3 playerPos = rb != null ? rb.position : transform.position;
            Vector3 vForce = playerPos - cubeMoveY.CurPos;
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
            if (rb != null) rb.AddForce(vForce, ForceMode.Impulse);
            AudioManager.Instance.Play("Sound/jumppad");

            return;
        }
    }
}
