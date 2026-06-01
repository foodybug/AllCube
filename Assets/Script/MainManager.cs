using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class MainManager : MonoBehaviour
{
    private static MainManager m_instance;
    public static MainManager Instance { get { return m_instance; } }

    public static bool StartWithFadeIn = false;

    [Header("Transition Settings")]
    public bool IsTransitioning { get; private set; }
    private GameObject m_transitionContainer;
    private RawImage[,] m_transitionGrid;
    private int m_gridCols = 10;
    private int m_gridRows = 6;

    void Awake()
    {
        if (m_instance != null && m_instance != this)
        {
            Debug.Log("[MainManager Debug] Duplicate MainManager instance detected in new scene. Self-destroying duplicate object.");
            Destroy(gameObject);
            return;
        }
        m_instance = this;
        DontDestroyOnLoad(gameObject);

        // 씬 로드 완료 이벤트 구독
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    void OnDestroy()
    {
        if (m_instance == this)
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
        }
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        Debug.Log("[MainManager Debug] OnSceneLoaded triggered. Active Scene: " + scene.name + ", StartWithFadeIn: " + StartWithFadeIn);
        if (StartWithFadeIn)
        {
            StartWithFadeIn = false;
            StartCoroutine(StartFadeInCoroutine());
        }
    }

    private void CreateTransitionGrid()
    {
        if (m_transitionContainer != null) return;

        m_transitionContainer = new GameObject("TransitionContainer");
        DontDestroyOnLoad(m_transitionContainer);

        Canvas canvas = m_transitionContainer.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 9999;

        CanvasScaler scaler = m_transitionContainer.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(800, 480);

        m_transitionGrid = new RawImage[m_gridRows, m_gridCols];

        float cellWidthNormalized = 1.0f / m_gridCols;
        float cellHeightNormalized = 1.0f / m_gridRows;

        for (int r = 0; r < m_gridRows; r++)
        {
            for (int c = 0; c < m_gridCols; c++)
            {
                GameObject cellGo = new GameObject("GridCell_" + r + "_" + c);
                cellGo.transform.SetParent(m_transitionContainer.transform, false);

                RawImage img = cellGo.AddComponent<RawImage>();
                img.color = new Color(0.12f, 0.12f, 0.12f, 1f); // Sleek dark gray
                img.raycastTarget = false;

                RectTransform cellRect = cellGo.GetComponent<RectTransform>();
                cellRect.anchorMin = new Vector2(c * cellWidthNormalized, r * cellHeightNormalized);
                cellRect.anchorMax = new Vector2((c + 1) * cellWidthNormalized, (r + 1) * cellHeightNormalized);
                cellRect.pivot = new Vector2(0.5f, 0.5f);
                cellRect.anchoredPosition = Vector2.zero;
                cellRect.sizeDelta = Vector2.zero;
                cellRect.localScale = Vector3.zero;

                m_transitionGrid[r, c] = img;
            }
        }
    }

    public void TransitionToScene(string sceneName)
    {
        if (IsTransitioning) return;
        StartCoroutine(TransitionToSceneCoroutine(sceneName));
    }

    private IEnumerator TransitionToSceneCoroutine(string sceneName)
    {
        IsTransitioning = true;
        CreateTransitionGrid();

        CanvasGroup group = m_transitionContainer.GetComponent<CanvasGroup>();
        if (group == null)
        {
            group = m_transitionContainer.AddComponent<CanvasGroup>();
        }
        group.blocksRaycasts = true;

        float centerRow = (m_gridRows - 1) / 2.0f;
        float centerCol = (m_gridCols - 1) / 2.0f;
        float maxDist = Mathf.Sqrt(centerRow * centerRow + centerCol * centerCol);

        // 1. Scale Up Grid Blocks (Outward from center)
        float duration = 0.4f;
        float cellAnimTime = 0.2f;
        float elapsed = 0.0f;

        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            for (int r = 0; r < m_gridRows; r++)
            {
                for (int c = 0; c < m_gridCols; c++)
                {
                    float dist = Mathf.Sqrt((r - centerRow) * (r - centerRow) + (c - centerCol) * (c - centerCol));
                    float normalizedDist = dist / maxDist;

                    float delay = normalizedDist * (duration - cellAnimTime);
                    float cellProgress = Mathf.Clamp01((elapsed - delay) / cellAnimTime);
                    m_transitionGrid[r, c].transform.localScale = new Vector3(cellProgress, cellProgress, 1f);
                }
            }
            yield return null;
        }

        for (int r = 0; r < m_gridRows; r++)
        {
            for (int c = 0; c < m_gridCols; c++)
            {
                m_transitionGrid[r, c].transform.localScale = Vector3.one;
            }
        }

        // 화면이 큐브 격자로 완전히 덮여 렌더링될 수 있도록 아주 미세하게 대기
        yield return new WaitForSecondsRealtime(0.05f);

        StartWithFadeIn = true;
        SceneManager.LoadScene(sceneName);
    }

    private IEnumerator StartFadeInCoroutine()
    {
        IsTransitioning = true;
        CreateTransitionGrid();

        for (int r = 0; r < m_gridRows; r++)
        {
            for (int c = 0; c < m_gridCols; c++)
            {
                m_transitionGrid[r, c].transform.localScale = Vector3.one;
            }
        }

        CanvasGroup group = m_transitionContainer.GetComponent<CanvasGroup>();
        if (group == null)
        {
            group = m_transitionContainer.AddComponent<CanvasGroup>();
        }
        group.blocksRaycasts = true;

        // 새로운 씬/상태 배치가 완전히 완료될 때까지 검은 화면에서 대기
        yield return new WaitForSecondsRealtime(0.15f);

        float centerRow = (m_gridRows - 1) / 2.0f;
        float centerCol = (m_gridCols - 1) / 2.0f;
        float maxDist = Mathf.Sqrt(centerRow * centerRow + centerCol * centerCol);

        // Scale Down Grid Blocks (Outward from center)
        float duration = 0.4f;
        float cellAnimTime = 0.2f;
        float elapsed = 0.0f;

        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            for (int r = 0; r < m_gridRows; r++)
            {
                for (int c = 0; c < m_gridCols; c++)
                {
                    float dist = Mathf.Sqrt((r - centerRow) * (r - centerRow) + (c - centerCol) * (c - centerCol));
                    float normalizedDist = dist / maxDist;

                    float delay = normalizedDist * (duration - cellAnimTime);
                    float cellProgress = Mathf.Clamp01(1.0f - ((elapsed - delay) / cellAnimTime));
                    m_transitionGrid[r, c].transform.localScale = new Vector3(cellProgress, cellProgress, 1f);
                }
            }
            yield return null;
        }

        for (int r = 0; r < m_gridRows; r++)
        {
            for (int c = 0; c < m_gridCols; c++)
            {
                m_transitionGrid[r, c].transform.localScale = Vector3.zero;
            }
        }

        group.blocksRaycasts = false;
        IsTransitioning = false;
    }
}
