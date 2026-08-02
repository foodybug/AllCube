using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 전체 DifficultyTier 직렬화 정보를 직접 보관하고 관리하는 독립 MonoBehaviour 클래스.
/// MapManager와 MapDisplay가 이 컴포넌트를 참조하여 맵 스테이지 난이도 정보를 조회합니다.
/// </summary>
public class StageDifficultyHolder : MonoBehaviour
{
    private static StageDifficultyHolder m_instance;
    public static StageDifficultyHolder Instance { get { return m_instance; } }

    [Header("Stage Config System")]
    [SerializeField]
    public StageConfig stageConfig = new StageConfig();

    [Header("Serialized Difficulty Tier List (50 Tiers)")]
    [SerializeField]
    public List<DifficultyTier> difficultyTiers = StageConfig.GenerateDefault50Tiers();

    private void Awake()
    {
        if (m_instance == null)
        {
            m_instance = this;
        }

        SyncTiers();
    }

    public void SyncTiers()
    {
        if (difficultyTiers == null || difficultyTiers.Count < 50 ||
           (difficultyTiers.Count > 1 && difficultyTiers[1].minHeight > 12) ||
           (difficultyTiers.Count > 0 && difficultyTiers[0].minSpawnX < -20) ||
           (difficultyTiers.Count > 0 && difficultyTiers[0].stationaryObstacleInterval == 0))
        {
            difficultyTiers = StageConfig.GenerateDefault50Tiers();
        }

        if (stageConfig == null)
        {
            stageConfig = new StageConfig();
        }

        stageConfig.DifficultyTiers = new List<DifficultyTier>(difficultyTiers);
    }

    private void OnValidate()
    {
        SyncTiers();
    }

    [ContextMenu("Force Reset All Tiers To New Default 50 Tiers")]
    public void ForceResetDefaultTiers()
    {
        difficultyTiers = StageConfig.GenerateDefault50Tiers();
        if (stageConfig == null) stageConfig = new StageConfig();
        stageConfig.DifficultyTiers = new List<DifficultyTier>(difficultyTiers);
        Debug.Log("[StageDifficultyHolder] Successfully reset serialized difficultyTiers to default 50 tiers.");
    }

    public DifficultyTier GetTierForHeight(int y)
    {
        SyncTiers();
        return stageConfig.GetTierForHeight(y);
    }
}
