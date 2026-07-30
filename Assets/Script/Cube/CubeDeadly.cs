using UnityEngine;

public class CubeDeadly : MonoBehaviour
{
    void Awake()
    {
        Collider col = GetComponent<Collider>();
        if (col != null) col.isTrigger = false; // 물리 벽
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
            rend.sharedMaterial = MapManager.Instance.GetSharedMaterial(8);

            Texture[] texCube = MapManager.Instance.texCube;
            if (texCube != null && texCube.Length > 0)
            {
                int randIdx = Random.Range(4, 8);
                if (randIdx < texCube.Length && texCube[randIdx] != null)
                {
                    Material deadlyMat = new Material(MapManager.Instance.GetSharedMaterial(8));
                    deadlyMat.mainTexture = texCube[randIdx];
                    rend.material = deadlyMat;
                }
            }
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
                Debug.Log($"[CubeDeadly] Player hit deadly block! Killing player.");
                MainManager.lastDeathCause = "DeadlyWall";
                player.KillPlayer();
            }
        }
    }
}
