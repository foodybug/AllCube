using System.Collections.Generic;
using UnityEngine;

public class InGameDebugConsole : MonoBehaviour
{
    private struct LogEntry
    {
        public string message;
        public string stackTrace;
        public LogType type;
    }

    private static InGameDebugConsole m_instance;
    private List<LogEntry> m_logEntries = new List<LogEntry>();
    private bool m_showConsole = false;
    private Vector2 m_scrollPosition;
    private bool m_showErrorsOnly = false;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void AutoInitialize()
    {
        if (m_instance == null && (Debug.isDebugBuild || Application.isEditor))
        {
            GameObject go = new GameObject("InGameDebugConsole");
            m_instance = go.AddComponent<InGameDebugConsole>();
            DontDestroyOnLoad(go);
        }
    }

    void OnEnable()
    {
        Application.logMessageReceived += HandleLog;
    }

    void OnDisable()
    {
        Application.logMessageReceived -= HandleLog;
    }

    private void HandleLog(string logString, string stackTrace, LogType type)
    {
        m_logEntries.Add(new LogEntry
        {
            message = logString,
            stackTrace = stackTrace,
            type = type
        });

        // Limit entries to prevent memory inflation
        if (m_logEntries.Count > 200)
        {
            m_logEntries.RemoveAt(0);
        }
    }

    void OnGUI()
    {
        if (!Debug.isDebugBuild && !Application.isEditor) return;

        float screenWidth = Screen.width;
        float screenHeight = Screen.height;
        float btnWidth = Mathf.Max(120f, screenWidth * 0.2f);
        float btnHeight = Mathf.Max(60f, screenHeight * 0.08f);

        // Toggle Console Button at bottom-left corner
        GUI.backgroundColor = m_showConsole ? Color.red : Color.black;
        if (GUI.Button(new Rect(10, screenHeight - btnHeight - 10, btnWidth, btnHeight), m_showConsole ? "Close Debug" : "Open Debug"))
        {
            m_showConsole = !m_showConsole;
        }

        if (!m_showConsole) return;

        // Draw Full-Screen Debug Panel
        float panelWidth = screenWidth * 0.95f;
        float panelHeight = screenHeight * 0.75f;
        float panelX = (screenWidth - panelWidth) * 0.5f;
        float panelY = 20f;

        GUI.backgroundColor = new Color(0.1f, 0.1f, 0.1f, 0.9f);
        GUI.Box(new Rect(panelX, panelY, panelWidth, panelHeight), "=== Android Debug Console ===");

        // Filter Controls
        float ctrlY = panelY + 30f;
        if (GUI.Button(new Rect(panelX + 10, ctrlY, 100, 40), "Clear"))
        {
            m_logEntries.Clear();
        }

        GUI.backgroundColor = m_showErrorsOnly ? Color.red : Color.gray;
        if (GUI.Button(new Rect(panelX + 120, ctrlY, 140, 40), m_showErrorsOnly ? "Show All" : "Errors Only"))
        {
            m_showErrorsOnly = !m_showErrorsOnly;
        }

        // Log Content Scroll View
        float scrollY = ctrlY + 50f;
        float scrollHeight = panelHeight - 90f;
        Rect scrollRect = new Rect(panelX + 10, scrollY, panelWidth - 20, scrollHeight);
        Rect viewRect = new Rect(0, 0, panelWidth - 40, m_logEntries.Count * 45f + 100f);

        m_scrollPosition = GUI.BeginScrollView(scrollRect, m_scrollPosition, viewRect);

        float entryY = 0f;
        GUIStyle textStyle = new GUIStyle(GUI.skin.label);
        textStyle.fontSize = Mathf.Max(14, Mathf.RoundToInt(screenHeight * 0.02f));
        textStyle.wordWrap = true;

        for (int i = m_logEntries.Count - 1; i >= 0; i--)
        {
            var entry = m_logEntries[i];
            if (m_showErrorsOnly && entry.type != LogType.Error && entry.type != LogType.Exception)
                continue;

            switch (entry.type)
            {
                case LogType.Error:
                case LogType.Exception:
                    textStyle.normal.textColor = Color.red;
                    break;
                case LogType.Warning:
                    textStyle.normal.textColor = Color.yellow;
                    break;
                default:
                    textStyle.normal.textColor = Color.cyan;
                    break;
            }

            string displayText = $"[{entry.type}] {entry.message}";
            if (entry.type == LogType.Exception && !string.IsNullOrEmpty(entry.stackTrace))
            {
                displayText += $"\n{entry.stackTrace}";
            }

            float textHeight = textStyle.CalcHeight(new GUIContent(displayText), panelWidth - 40);
            GUI.Label(new Rect(0, entryY, panelWidth - 40, textHeight), displayText, textStyle);
            entryY += textHeight + 5f;
        }

        GUI.EndScrollView();
    }
}
