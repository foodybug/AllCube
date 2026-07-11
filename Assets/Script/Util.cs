using UnityEngine;
using System.Collections;
#if UNITY_EDITOR
using UnityEditor;
#endif

public class Util : MonoBehaviour
{
	static public void MyDestroy(Object o)
	{
#if UNITY_EDITOR
		//DestroyImmediate( o);
		Destroy( o);
#else
		Destroy( o);
#endif
	}

	static public void Quit()
	{
#if UNITY_EDITOR
		EditorApplication.isPlaying = false;
		EditorApplication.isPaused = false;
#else
	#if UNITY_ANDROID
		/*
		// dispose
		System.Diagnostics.ProcessThreadCollection ptc = System.Diagnostics.Process.GetCurrentProcess().Threads;
		foreach( System.Diagnostics.ProcessThread pt in ptc)
		{
			pt.Dispose();
		}
		System.Diagnostics.Process.GetCurrentProcess().Kill();
		*/
	#endif

		Application.Quit();
#endif
	}
}

public class Coin : MonoBehaviour
{
    private Renderer m_renderer;
    private Transform m_playerTransform;

    void Awake()
    {
        m_renderer = GetComponentInChildren<Renderer>();
    }

    void Start()
    {
        ApplyRandomTexture();
    }

    void Update()
    {
        if (m_playerTransform == null)
        {
            Player player = FindAnyObjectByType<Player>();
            if (player != null)
            {
                m_playerTransform = player.transform;
            }
        }

        if (m_playerTransform != null)
        {
            transform.rotation = m_playerTransform.rotation;
        }
    }

    public void ApplyRandomTexture()
    {
        if (m_renderer == null)
        {
            m_renderer = GetComponentInChildren<Renderer>();
        }

        if (m_renderer != null && MapManager.Instance != null)
        {
            Texture[] texCube = MapManager.Instance.texCube;
            if (texCube != null && texCube.Length > 0)
            {
                int maxIdx = Mathf.Min(3, texCube.Length - 1);
                if (maxIdx >= 0)
                {
                    int randIdx = Random.Range(0, maxIdx + 1);
                    Texture targetTex = texCube[randIdx];

                    if (targetTex != null)
                    {
                        m_renderer.material.mainTexture = targetTex;
                    }
                }
            }
        }
    }
}
