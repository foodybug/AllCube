using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public struct DifficultyTier
{
    public int minHeight;
    public int minSpawnX;
    public int maxSpawnX;
    public int staticObstacleInterval;
    public int flyingObstacleInterval;
    public int blinkObstacleInterval; // Blink 장애물 생성 주기 (0 이면 미생성)
    public int minBlinkHeight;        // Blink 장애물이 등장하기 시작하는 최소 높이
    public int coinInterval;
    public int coinSequence;
    public float minFlyingSpeed;
    public float maxFlyingSpeed;
    public int initialJumps;
    public List<GameObject> segmentPrefabs; // 타일맵 기반 조립식 맵 세그먼트 프리팹 리스트
}

[System.Serializable]
public class StageConfig
{
    [SerializeField]
    private List<DifficultyTier> m_difficultyTier = new List<DifficultyTier>()
    {
        new DifficultyTier { minHeight = 0, minSpawnX = -30, maxSpawnX = 30, staticObstacleInterval = 10, flyingObstacleInterval = 15, blinkObstacleInterval = 0, minBlinkHeight = 0, coinInterval = 3, coinSequence = 1, minFlyingSpeed = 4f, maxFlyingSpeed = 6f, initialJumps = 10, segmentPrefabs = null },
        new DifficultyTier { minHeight = 20, minSpawnX = -25, maxSpawnX = 25, staticObstacleInterval = 8, flyingObstacleInterval = 12, blinkObstacleInterval = 0, minBlinkHeight = 0, coinInterval = 4, coinSequence = 1, minFlyingSpeed = 6f, maxFlyingSpeed = 8f, initialJumps = 8, segmentPrefabs = null },
        new DifficultyTier { minHeight = 40, minSpawnX = -20, maxSpawnX = 20, staticObstacleInterval = 6, flyingObstacleInterval = 9, blinkObstacleInterval = 8, minBlinkHeight = 40, coinInterval = 5, coinSequence = 1, minFlyingSpeed = 8f, maxFlyingSpeed = 11f, initialJumps = 7, segmentPrefabs = null },
        new DifficultyTier { minHeight = 60, minSpawnX = -15, maxSpawnX = 15, staticObstacleInterval = 5, flyingObstacleInterval = 7, blinkObstacleInterval = 6, minBlinkHeight = 60, coinInterval = 6, coinSequence = 1, minFlyingSpeed = 10f, maxFlyingSpeed = 14f, initialJumps = 5, segmentPrefabs = null },
        new DifficultyTier { minHeight = 80, minSpawnX = -10, maxSpawnX = 10, staticObstacleInterval = 4, flyingObstacleInterval = 5, blinkObstacleInterval = 5, minBlinkHeight = 80, coinInterval = 7, coinSequence = 1, minFlyingSpeed = 12f, maxFlyingSpeed = 18f, initialJumps = 4, segmentPrefabs = null }
    };

    public List<DifficultyTier> DifficultyTiers
    {
        get { return m_difficultyTier; }
        set { m_difficultyTier = value; }
    }

    public DifficultyTier GetTierForHeight(int y)
    {
        // 1. 순환 주기 계산 (마지막 티어의 minHeight와 그 이전 티어의 minHeight 차이 기준)
        int cycleHeight = 100;
        int count = m_difficultyTier.Count;
        if (count > 1)
        {
            int lastDiff = m_difficultyTier[count - 1].minHeight - m_difficultyTier[count - 2].minHeight;
            cycleHeight = m_difficultyTier[count - 1].minHeight + lastDiff;
        }

        // 음수 보정 및 순환 횟수/가상 높이 계산
        int cycleCount = 0;
        int virtualY = y;
        if (y > 0 && cycleHeight > 0)
        {
            cycleCount = y / cycleHeight;
            virtualY = y % cycleHeight;
        }

        // 2. 가상 높이(virtualY) 기준 기본 티어 조회
        DifficultyTier activeTier = new DifficultyTier();
        bool found = false;
        int maxMinHeight = -1;

        foreach (var tier in m_difficultyTier)
        {
            if (virtualY >= tier.minHeight && tier.minHeight > maxMinHeight)
            {
                activeTier = tier;
                maxMinHeight = tier.minHeight;
                found = true;
            }
        }

        if (!found)
        {
            // Fallback (아무것도 찾지 못했을 때 기본 세팅값 반환)
            activeTier.minHeight = 0;
            activeTier.minSpawnX = -30;
            activeTier.maxSpawnX = 30;
            activeTier.staticObstacleInterval = 5;
            activeTier.flyingObstacleInterval = 8;
            activeTier.blinkObstacleInterval = 0; // 기본 비활성
            activeTier.minBlinkHeight = 0;
            activeTier.coinInterval = 3;
            activeTier.coinSequence = 1;
            activeTier.minFlyingSpeed = 6.0f;
            activeTier.maxFlyingSpeed = 10.0f;
            activeTier.initialJumps = 10;
            activeTier.segmentPrefabs = null;
        }

        // 3. 순환 횟수(cycleCount)에 따른 보정치 적용 (패널티 강화, 어드밴티지 약화)
        if (cycleCount > 0)
        {
            // 3-1. 패널티 강화
            // staticObstacleInterval (장애물 간격 좁힘 -> 더 자주 나옴, 최소 2)
            activeTier.staticObstacleInterval = Mathf.Max(2, activeTier.staticObstacleInterval - cycleCount);

            // flyingObstacleInterval (비행 장애물 간격 좁힘 -> 더 자주 나옴, 최소 2)
            activeTier.flyingObstacleInterval = Mathf.Max(2, activeTier.flyingObstacleInterval - cycleCount);

            // blinkObstacleInterval (깜빡이 장애물 간격 좁힘 -> 더 자주 나옴, 최소 2)
            if (activeTier.blinkObstacleInterval > 0)
            {
                activeTier.blinkObstacleInterval = Mathf.Max(2, activeTier.blinkObstacleInterval - cycleCount);
            }

            // 비행 장애물 속도 증가 (순환당 1.5f씩 상승)
            activeTier.minFlyingSpeed += cycleCount * 1.5f;
            activeTier.maxFlyingSpeed += cycleCount * 1.5f;

            // 좌우 스폰 폭 좁히기 (순환당 좌우 1칸씩 좁힘 -> 기둥 간격 좁아져 위협 상승)
            // 단, 너무 좁아져 진행이 불가하지 않도록 최소 가로폭 10 유지
            int requestedMinX = activeTier.minSpawnX + cycleCount;
            int requestedMaxX = activeTier.maxSpawnX - cycleCount;
            if (requestedMaxX - requestedMinX >= 10)
            {
                activeTier.minSpawnX = requestedMinX;
                activeTier.maxSpawnX = requestedMaxX;
            }
            else
            {
                // 중간점을 기준으로 최소 가로폭 10 유지
                int center = (activeTier.minSpawnX + activeTier.maxSpawnX) / 2;
                activeTier.minSpawnX = center - 5;
                activeTier.maxSpawnX = center + 5;
            }

            // 3-2. 어드밴티지 약화
            // coinInterval (코인 획득 간격 늘림 -> 덜 나옴, 최대 20)
            activeTier.coinInterval = Mathf.Min(20, activeTier.coinInterval + cycleCount);

            // coinSequence (코인 연속 스폰 갯수 줄임 -> 덜 나옴, 최소 1)
            activeTier.coinSequence = Mathf.Max(1, activeTier.coinSequence - cycleCount);

            // initialJumps (시작 점프 부여 개수 낮춤, 최소 3)
            activeTier.initialJumps = Mathf.Max(3, activeTier.initialJumps - cycleCount);
        }

        return activeTier;
    }
}
