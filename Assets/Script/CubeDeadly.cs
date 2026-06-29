using UnityEngine;

public class CubeDeadly : MonoBehaviour
{
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
                player = FindAnyObjectByType<Player>();
            }

            if (player != null)
            {
                Debug.Log($"[CubeDeadly] Player hit deadly block! Killing player.");
                MainManager.lastDeathCause = "DeadlyWall";
                player.KillPlayer();
            }
        }
    }
}
