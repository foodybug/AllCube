using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

public class InGameDebugConsole : MonoBehaviour
{
    [System.Serializable]
    private struct LogEntry
    {
        public string message;
        public string stackTrace;
        public LogType type;
        public string time;
    }

    private static InGameDebugConsole m_instance;
    private List<LogEntry> m_logEntries = new List<LogEntry>();
    private bool m_showConsole = false;
    private Vector2 m_scrollPosition;
    private bool m_showErrorsOnly = false;
    private static string m_logFilePath;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void AutoInitialize()
    {
        if (m_instance == null)
        {
            GameObject go = new GameObject("InGameDebugConsole");
            m_instance = go.AddComponent<InGameDebugConsole>();
            DontDestroyOnLoad(go);
        }
    }

    void Awake()
    {
        m_logFilePath = Path.Combine(Application.persistentDataPath, "allcube_crash_log.txt");
        try
        {
            File.AppendAllText(m_logFilePath, $"\n\n==================== [App Started: {DateTime.Now:yyyy-MM-dd HH:mm:ss}] ====================\n");
        }
        catch { }

        Application.logMessageReceivedThreaded += HandleLogThreaded;
        System.AppDomain.CurrentDomain.UnhandledException += HandleUnhandledException;
    }

    void OnDestroy()
    {
        Application.logMessageReceivedThreaded -= HandleLogThreaded;
        System.AppDomain.CurrentDomain.UnhandledException -= HandleUnhandledException;
    }

    private void HandleUnhandledException(object sender, UnhandledExceptionEventArgs e)
    {
        Exception ex = e.ExceptionObject as Exception;
        string msg = ex != null ? ex.Message : "Unknown Unhandled Exception";
        string st = ex != null ? ex.StackTrace : "";
        HandleLogThreaded(msg, st, LogType.Exception);
    }

    private void HandleLogThreaded(string logString, string stackTrace, LogType type)
    {
        string timeStr = DateTime.Now.ToString("HH:mm:ss");
        LogEntry entry = new LogEntry
        {
            message = logString,
            stackTrace = stackTrace,
            type = type,
            time = timeStr
        };

        lock (m_logEntries)
        {
            m_logEntries.Add(entry);
            if (m_logEntries.Count > 300)
            {
                m_logEntries.RemoveAt(0);
            }
        }

        // Auto open GUI if Exception or Error occurs
        if (type == LogType.Exception || type == LogType.Error)
        {
            m_showConsole = true;
        }

        // Write directly to local file for offline device inspection
        if (!string.IsNullOrEmpty(m_logFilePath))
        {
            try
            {
                string logLine = $"[{timeStr}] [{type}] {logString}\n";
                if (type == LogType.Exception || type == LogType.Error)
                {
                    if (!string.IsNullOrEmpty(stackTrace))
                    {
                        logLine += $"StackTrace:\n{stackTrace}\n";
                    }
                }
                File.AppendAllText(m_logFilePath, logLine);
            }
            catch { }
        }
    }

    void OnGUI()
    {
        float screenWidth = Screen.width;
        float screenHeight = Screen.height;
        float btnWidth = Mathf.Max(140f, screenWidth * 0.22f);
        float btnHeight = Mathf.Max(65f, screenHeight * 0.08f);

        // Toggle Console Button at bottom-left corner
        GUI.backgroundColor = m_showConsole ? Color.red : Color.black;
        if (GUI.Button(new Rect(10, screenHeight - btnHeight - 10, btnWidth, btnHeight), m_showConsole ? "Close Debug" : "Open Debug"))
        {
            m_showConsole = !m_showConsole;
        }

        if (!m_showConsole) return;

        // Draw Full-Screen Debug Panel
        float panelWidth = screenWidth * 0.96f;
        float panelHeight = screenHeight * 0.82f;
        float panelX = (screenWidth - panelWidth) * 0.5f;
        float panelY = 15f;

        GUI.backgroundColor = new Color(0.05f, 0.05f, 0.05f, 0.95f);
        GUI.Box(new Rect(panelX, panelY, panelWidth, panelHeight), "=== AllCube In-Game & Crash Logger ===");

        // Display Log File Location
        GUIStyle pathStyle = new GUIStyle(GUI.skin.label);
        pathStyle.fontSize = Mathf.Max(11, Mathf.RoundToInt(screenHeight * 0.015f));
        pathStyle.normal.textColor = Color.yellow;
        GUI.Label(new Rect(panelX + 10, panelY + 25f, panelWidth - 20, 25f), $"Log File: {m_logFilePath}", pathStyle);

        // Control Buttons
        float ctrlY = panelY + 50f;
        if (GUI.Button(new Rect(panelX + 10, ctrlY, 100, 40), "Clear Logs"))
        {
            lock (m_logEntries)
            {
                m_logEntries.Clear();
            }
        }

        GUI.backgroundColor = m_showErrorsOnly ? Color.red : Color.gray;
        if (GUI.Button(new Rect(panelX + 120, ctrlY, 140, 40), m_showErrorsOnly ? "Show All" : "Errors Only"))
        {
            m_showErrorsOnly = !m_showErrorsOnly;
        }

        if (GUI.Button(new Rect(panelX + 270, ctrlY, 140, 40), "Clear Log File"))
        {
            try
            {
                if (File.Exists(m_logFilePath)) File.Delete(m_logFilePath);
            }
            catch { }
        }

        // Log Content Scroll View
        float scrollY = ctrlY + 50f;
        float scrollHeight = panelHeight - 110f;
        Rect scrollRect = new Rect(panelX + 10, scrollY, panelWidth - 20, scrollHeight);

        List<LogEntry> currentLogs;
        lock (m_logEntries)
        {
            currentLogs = new List<LogEntry>(m_logEntries);
        }

        Rect viewRect = new Rect(0, 0, panelWidth - 40, currentLogs.Count * 50f + 100f);

        m_scrollPosition = GUI.BeginScrollView(scrollRect, m_scrollPosition, viewRect);

        float entryY = 0f;
        GUIStyle textStyle = new GUIStyle(GUI.skin.label);
        textStyle.fontSize = Mathf.Max(13, Mathf.RoundToInt(screenHeight * 0.018f));
        textStyle.wordWrap = true;

        for (int i = currentLogs.Count - 1; i >= 0; i--)
        {
            var entry = currentLogs[i];
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

            string displayText = $"[{entry.time}] [{entry.type}] {entry.message}";
            if ((entry.type == LogType.Exception || entry.type == LogType.Error) && !string.IsNullOrEmpty(entry.stackTrace))
            {
                displayText += $"\nStackTrace:\n{entry.stackTrace}";
            }

            float textHeight = textStyle.CalcHeight(new GUIContent(displayText), panelWidth - 40);
            GUI.Label(new Rect(0, entryY, panelWidth - 40, textHeight), displayText, textStyle);
            entryY += textHeight + 6f;
        }

        GUI.EndScrollView();
    }
}
