using System.Collections.Generic;
using UnityEngine;

public class JumpIntervalTracker : MonoBehaviour
{
    private static JumpIntervalTracker m_instance;
    public static JumpIntervalTracker Instance
    {
        get
        {
            if (m_instance == null)
            {
                // 씬 내에 활성화된 트래커가 있는지 검사
                m_instance = FindAnyObjectByType<JumpIntervalTracker>();

                // 씬에 없으면 런타임에 동적으로 게임오브젝트를 생성하여 부착
                if (m_instance == null)
                {
                    GameObject go = new GameObject("JumpIntervalTracker");
                    m_instance = go.AddComponent<JumpIntervalTracker>();
                    DontDestroyOnLoad(go);
                }
            }
            return m_instance;
        }
    }

    private float m_lastJumpTime = 0.0f;
    private bool m_isFirstJump = true;
    [SerializeField] private List<float> m_intervals = new List<float>();

    private int m_comboCount = 0;
    private int m_maxComboCount = 0;

    [Header("Combo Tolerance Settings")]
    [SerializeField] private float m_rhythmTolerance = 0.25f; // 초 단위 허용 오차
    [SerializeField] private float m_maxIdleTime = 2.0f; // 초 단위 최대 대기 시간 (콤보 유지 제한 시간)

    [Header("Combo Reward Settings")]
    [SerializeField] private int m_rewardComboInterval = 3; // 몇 콤보마다 보너스를 줄 것인지
    [SerializeField] private int m_rewardJumpCount = 1; // 지급할 보너스 점프 개수

    [Header("Interval Tracker Settings")]
    [SerializeField] private int m_maxIntervalsCapacity = 20; // 최대 보관 점프 간격 개수

    public int ComboCount { get { return m_comboCount; } }
    public int MaxComboCount { get { return m_maxComboCount; } }

    private void Awake()
    {
        if (m_instance == null)
        {
            m_instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else if (m_instance != this)
        {
            Destroy(gameObject);
        }
    }

    private void Update()
    {
        // 게임 진행 중에만 타임아웃 검사
        if (PlayMain.Instance != null && PlayMain.Instance.IsGameStarted &&
            MainManager.Instance != null && MainManager.Instance.eCurState == eGameState.eGameState_Play)
        {
            if (!m_isFirstJump && m_comboCount > 0)
            {
                float idleTime = Time.realtimeSinceStartup - m_lastJumpTime;
                if (idleTime > m_maxIdleTime)
                {
                    m_comboCount = 0;
                    if (UI_Play.Instance != null)
                    {
                        UI_Play.Instance.UpdateCombo(m_comboCount);
                    }
                }
            }
        }
    }

    /// <summary>
    /// 점프 입력을 기록하고 이전 점프와의 시간 간격(초)을 연산하여 저장합니다.
    /// </summary>
    public void RecordJumpInput()
    {
        float currentTime = Time.realtimeSinceStartup;

        if (m_isFirstJump)
        {
            m_isFirstJump = false;
            m_lastJumpTime = currentTime;
            m_comboCount = 0;
        }
        else
        {
            float interval = currentTime - m_lastJumpTime;
            m_intervals.Add(interval);

            // 콤보 판정 로직 (직전 간격 또는 전전 간격과의 차이가 허용 오차 이내인 경우 인정)
            bool isRhythmic = false;
            if (m_intervals.Count > 1)
            {
                float lastInterval = m_intervals[m_intervals.Count - 2];
                float diff = Mathf.Abs(interval - lastInterval);
                if (diff <= m_rhythmTolerance && interval <= m_maxIdleTime)
                {
                    isRhythmic = true;
                }
                else if (m_intervals.Count > 2)
                {
                    float prevLastInterval = m_intervals[m_intervals.Count - 3];
                    float diffPrev = Mathf.Abs(interval - prevLastInterval);
                    if (diffPrev <= m_rhythmTolerance && interval <= m_maxIdleTime)
                    {
                        isRhythmic = true;
                    }
                }
            }
            else
            {
                if (interval <= m_maxIdleTime)
                {
                    isRhythmic = true;
                }
            }

            if (isRhythmic)
            {
                m_comboCount++;
                if (m_comboCount > m_maxComboCount)
                {
                    m_maxComboCount = m_comboCount;
                }

                // 콤보 달성에 따른 보너스 점프 지급 및 UI 동기화
                if (m_comboCount % m_rewardComboInterval == 0)
                {
                    Player player = FindAnyObjectByType<Player>();
                    if (player != null)
                    {
                        player.AddJumps(m_rewardJumpCount);
                        if (UI_Play.Instance != null && MapManager.Instance != null)
                        {
                            UI_Play.Instance.SetPlayStats(MapManager.Instance.TotalCoinsCollected, player.JumpCount);
                        }
                    }
                }
            }
            else
            {
                m_comboCount = 0;
            }

            // 인스펙터 확인을 위해 최대 수량만큼만 저장 (Queue 동작 모사)
            if (m_intervals.Count > m_maxIntervalsCapacity)
            {
                m_intervals.RemoveAt(0);
            }

            m_lastJumpTime = currentTime;
        }

        // UI에 콤보 정보 전달
        if (UI_Play.Instance != null)
        {
            UI_Play.Instance.UpdateCombo(m_comboCount);
        }
    }

    /// <summary>
    /// 추적 기록과 시간 상태를 초기화합니다. (스테이지 재시작 등에서 사용)
    /// </summary>
    public void ResetTracker()
    {
        m_intervals.Clear();
        m_lastJumpTime = 0.0f;
        m_isFirstJump = true;
        m_comboCount = 0;
        if (UI_Play.Instance != null)
        {
            UI_Play.Instance.UpdateCombo(0);
        }
    }

    /// <summary>
    /// 저장된 모든 점프 시간 간격 목록을 반환합니다.
    /// </summary>
    public List<float> GetIntervals()
    {
        return m_intervals;
    }
}
