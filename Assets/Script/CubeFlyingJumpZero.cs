using UnityEngine;

public class CubeFlyingJumpZero : MonoBehaviour
{
    public float speed = 0f;
    private float m_lifetime = 15.0f;
    private Transform m_playerTransform;

    void Awake()
    {
        Collider col = GetComponent<Collider>();
        if (col != null) col.isTrigger = true;
        Rigidbody rb = GetComponent<Rigidbody>();
        if (rb != null) Destroy(rb);
    }

    void Start()
    {
        Renderer rend = GetComponent<Renderer>();
        if (rend != null && MapManager.Instance != null)
        {
            rend.sharedMaterial = MapManager.Instance.GetSharedMaterial(6);
        }
    }

    public void InitFlying(float flySpeed, Transform playerTransform)
    {
        speed = flySpeed;
        m_playerTransform = playerTransform;
        Destroy(gameObject, m_lifetime);
    }

    void Update()
    {
        if (MainManager.Instance != null && MainManager.Instance.eCurState == eGameState.eGameState_Play)
        {
            transform.Translate(speed * Time.deltaTime, 0f, 0f, Space.World);
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        HandlePlayerCollision(other.gameObject);
    }

    private void HandlePlayerCollision(GameObject otherGo)
    {
        Player player = otherGo.GetComponent<Player>();
        if (player != null || otherGo.name.Contains("Player") || otherGo.CompareTag("Player"))
        {
            if (player == null)
            {
                player = FindAnyObjectByType<Player>();
            }

            if (player != null)
            {
                Debug.Log($"[CubeFlyingJumpZero] Player hit Flying Red Block! Resetting jump count from {player.JumpCount} to 0.");
                MainManager.lastDeathCause = "JumpZero";
                player.ResetJumpCount(0);
                player.KillPlayer();


            }
        }
    }
}
