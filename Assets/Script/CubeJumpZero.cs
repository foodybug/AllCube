using UnityEngine;

public class CubeJumpZero : MonoBehaviour
{
    public bool isBoundaryWall = false;

    private void OnTriggerEnter(Collider other)
    {
        if (isBoundaryWall) return; // 경계 장벽은 Trigger가 아니므로 처리하지 않음

        HandlePlayerCollision(other.gameObject);
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (!isBoundaryWall) return; // 일반 장애물은 물리 충돌을 처리하지 않음

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
                Debug.Log($"[CubeJumpZero] Player hit Red Block (Boundary: {isBoundaryWall})! Resetting jump count from {player.JumpCount} to 0.");
                MainManager.lastDeathCause = "JumpZero";
                player.ResetJumpCount(0);
                player.KillPlayer();



                // 일반 공중 장애물인 경우에만 블록 파괴 효과 연출 및 제거
                if (!isBoundaryWall)
                {
                    if (MapManager.Instance != null)
                    {
                        MapManager.Instance.RemoveCube(gameObject);
                    }
                    else
                    {
                        Destroy(gameObject);
                    }
                }
            }
        }
    }
}
