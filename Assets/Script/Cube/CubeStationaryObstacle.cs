using UnityEngine;

/// <summary>
/// 제자리에 가만히 고정되어 있으며 플레이어 충돌 시 치명적인 피해를 주는 정적 장애물 블럭
/// </summary>
public class CubeStationaryObstacle : MonoBehaviour
{
    void Awake()
    {
        Collider col = GetComponent<Collider>();
        if (col != null) col.isTrigger = true;
        Rigidbody rb = GetComponent<Rigidbody>();
        if (rb != null) Destroy(rb);

        if (GetComponent<EnemyGlitchTextureEffect>() == null)
        {
            gameObject.AddComponent<EnemyGlitchTextureEffect>();
        }
    }

    void Start()
    {
        Renderer rend = GetComponent<Renderer>();
        if (rend != null && MapManager.Instance != null)
        {
            rend.sharedMaterial = MapManager.Instance.GetSharedMaterial(6);
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        HandlePlayerCollision(other.gameObject);
    }

    private void OnCollisionEnter(Collision collision)
    {
        HandlePlayerCollision(collision.gameObject);
    }

    private void HandlePlayerCollision(GameObject otherGo)
    {
        Player player = otherGo.GetComponent<Player>();
        if (player != null || otherGo.name.Contains("Player") || otherGo.CompareTag("Player"))
        {
            if (player == null)
            {
                player = Player.Instance;
            }

            if (player != null)
            {
                Debug.Log($"[CubeStationaryObstacle] Player collided with stationary obstacle! Killing player.");
                MainManager.lastDeathCause = "StationaryObstacle";
                player.KillPlayer();
            }
        }
    }
}
