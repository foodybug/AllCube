using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class TransitionCanvas : MonoBehaviour
{
    private static TransitionCanvas m_instance;
    public static TransitionCanvas Instance { get { return m_instance; } }

    [Header("Grid Configuration")]
    public int gridRows = 6;
    public int gridCols = 10;
    public RawImage[] gridCells; // 프리팹 인스펙터에서 사전 배치해둘 수 있는 1차원 셀 배열

    [HideInInspector]
    private bool m_isTransitioning = false;
    public bool IsTransitioning { get { return m_isTransitioning; } set { m_isTransitioning = value; } }

    void Awake()
    {
        if (m_instance != null && m_instance != this)
        {
            Debug.Log("[TransitionCanvas Debug] Duplicate TransitionCanvas instance detected. Self-destroying duplicate object.");
            Destroy(gameObject);
            return;
        }
        m_instance = this;
        DontDestroyOnLoad(gameObject);

        // 그리드가 설정되지 않은 상태라면 자동으로 하방 호환을 위한 폴백 그리드를 구축합니다.
        SetupGrid();
    }

    public void SetupGrid()
    {
        if (gridCells != null && gridCells.Length == gridRows * gridCols) return;

        Debug.Log("[TransitionCanvas Debug] SetupGrid fallback active. Dynamically building 10x6 transition grid.");

        Canvas canvas = GetComponent<Canvas>();
        if (canvas == null)
        {
            canvas = gameObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 9999;
        }

        CanvasScaler scaler = GetComponent<CanvasScaler>();
        if (scaler == null)
        {
            scaler = gameObject.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(800, 480);
        }

        gridCells = new RawImage[gridRows * gridCols];

        float cellWidthNormalized = 1.0f / gridCols;
        float cellHeightNormalized = 1.0f / gridRows;

        for (int r = 0; r < gridRows; r++)
        {
            for (int c = 0; c < gridCols; c++)
            {
                GameObject cellGo = new GameObject("GridCell_" + r + "_" + c);
                cellGo.transform.SetParent(transform, false);

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

                gridCells[r * gridCols + c] = img;
            }
        }
    }

    public IEnumerator PlayFadeOut_CR()
    {
        m_isTransitioning = true;
        SetupGrid();

        CanvasGroup group = GetComponent<CanvasGroup>();
        if (group == null)
        {
            group = gameObject.AddComponent<CanvasGroup>();
        }
        group.blocksRaycasts = true;

        float centerRow = (gridRows - 1) / 2.0f;
        float centerCol = (gridCols - 1) / 2.0f;
        float maxDist = Mathf.Sqrt(centerRow * centerRow + centerCol * centerCol);

        float duration = 0.4f;
        float cellAnimTime = 0.2f;
        float elapsed = 0.0f;

        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            for (int r = 0; r < gridRows; r++)
            {
                for (int c = 0; c < gridCols; c++)
                {
                    float dist = Mathf.Sqrt((r - centerRow) * (r - centerRow) + (c - centerCol) * (c - centerCol));
                    float normalizedDist = dist / maxDist;

                    float delay = normalizedDist * (duration - cellAnimTime);
                    float cellProgress = Mathf.Clamp01((elapsed - delay) / cellAnimTime);
                    
                    int idx = r * gridCols + c;
                    if (idx < gridCells.Length && gridCells[idx] != null)
                    {
                        gridCells[idx].transform.localScale = new Vector3(cellProgress, cellProgress, 1f);
                    }
                }
            }
            yield return null;
        }

        for (int i = 0; i < gridCells.Length; i++)
        {
            if (gridCells[i] != null)
            {
                gridCells[i].transform.localScale = Vector3.one;
            }
        }

        yield return new WaitForSecondsRealtime(0.05f);
    }

    public IEnumerator PlayFadeIn_CR()
    {
        m_isTransitioning = true;
        SetupGrid();

        for (int i = 0; i < gridCells.Length; i++)
        {
            if (gridCells[i] != null)
            {
                gridCells[i].transform.localScale = Vector3.one;
            }
        }

        CanvasGroup group = GetComponent<CanvasGroup>();
        if (group == null)
        {
            group = gameObject.AddComponent<CanvasGroup>();
        }
        group.blocksRaycasts = true;

        yield return new WaitForSecondsRealtime(0.15f);

        float centerRow = (gridRows - 1) / 2.0f;
        float centerCol = (gridCols - 1) / 2.0f;
        float maxDist = Mathf.Sqrt(centerRow * centerRow + centerCol * centerCol);

        float duration = 0.4f;
        float cellAnimTime = 0.2f;
        float elapsed = 0.0f;

        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            for (int r = 0; r < gridRows; r++)
            {
                for (int c = 0; c < gridCols; c++)
                {
                    float dist = Mathf.Sqrt((r - centerRow) * (r - centerRow) + (c - centerCol) * (c - centerCol));
                    float normalizedDist = dist / maxDist;

                    float delay = normalizedDist * (duration - cellAnimTime);
                    float cellProgress = Mathf.Clamp01(1.0f - ((elapsed - delay) / cellAnimTime));

                    int idx = r * gridCols + c;
                    if (idx < gridCells.Length && gridCells[idx] != null)
                    {
                        gridCells[idx].transform.localScale = new Vector3(cellProgress, cellProgress, 1f);
                    }
                }
            }
            yield return null;
        }

        for (int i = 0; i < gridCells.Length; i++)
        {
            if (gridCells[i] != null)
            {
                gridCells[i].transform.localScale = Vector3.zero;
            }
        }

        group.blocksRaycasts = false;
        m_isTransitioning = false;
    }
}
