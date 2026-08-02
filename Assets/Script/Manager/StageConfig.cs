using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public struct DifficultyTier
{
    public int minHeight;
    public int minSpawnX;
    public int maxSpawnX;
    public int staticObstacleInterval;
    public int stationaryObstacleInterval; // 제자리에 가만히 있는 장애물 생성 주기 (0 이면 미생성)
    public int minStationaryHeight;        // 제자리 장애물 등장 최소 높이
    public int flyingObstacleInterval;
    public int blinkObstacleInterval;     // Blink 장애물 생성 주기 (0 이면 미생성)
    public int minBlinkHeight;            // Blink 장애물이 등장하기 시작하는 최소 높이
    public int homingObstacleInterval;    // 느린 추적 장애물 생성 주기 (0 이면 미생성)
    public int minHomingHeight;           // 추적 장애물 등장 최소 높이
    public float homingSpeed;             // 추적 장애물 이동 속도 (초반 1.8f -> 후반 5.5f)
    public int coinInterval;
    public int coinSequence;
    public float minFlyingSpeed;
    public float maxFlyingSpeed;
    public List<GameObject> segmentPrefabs; // 타일맵 기반 조립식 맵 세그먼트 프리팹 리스트
}

[System.Serializable]
public class StageConfig
{
    [SerializeField]
    private List<DifficultyTier> m_difficultyTier = GenerateDefault50Tiers();

    public List<DifficultyTier> DifficultyTiers
    {
        get { return m_difficultyTier; }
        set { m_difficultyTier = value; }
    }

    public void ResetToDefaultTiers()
    {
        m_difficultyTier = GenerateDefault50Tiers();
    }

    public static List<DifficultyTier> GenerateDefault50Tiers()
    {
        List<DifficultyTier> list = new List<DifficultyTier>();
        int totalTiers = 50;

        for (int i = 0; i < totalTiers; i++)
        {
            float progress = (float)i / (totalTiers - 1); // 0.0 ~ 1.0

            // minHeight 수치를 기존(i * 20)에서 반으로 축소 (0, 10, 20, ..., 490)
            int minHeight = i * 10;

            // 스폰 영역 폭: 기존 [-30, 30]에서 반으로 축소하여 [-15, 15] ~ [-5, 5]로 점진 축소
            int spawnWidth = Mathf.Max(5, Mathf.RoundToInt(Mathf.Lerp(15f, 5f, progress)));
            int minSpawnX = -spawnWidth;
            int maxSpawnX = spawnWidth;

            // 정적 및 비행 장애물 간격
            int staticInterval = Mathf.Max(3, Mathf.RoundToInt(Mathf.Lerp(12f, 3f, progress)));

            // 제자리에 가만히 있는 장애물: 전체 50개 Tier 전반에 걸쳐 골고루 분배 (간격 14 -> 3)
            int stationaryInterval = Mathf.Max(3, Mathf.RoundToInt(Mathf.Lerp(14f, 3f, progress)));
            int minStationaryH = minHeight;

            int flyingInterval = Mathf.Max(4, Mathf.RoundToInt(Mathf.Lerp(16f, 4f, progress)));

            // 깜빡이 장애물: Tier 8 이상부터 등장
            int blinkInterval = 0;
            int minBlinkH = 0;
            if (i >= 8)
            {
                float blinkProgress = (float)(i - 8) / (totalTiers - 9);
                blinkInterval = Mathf.Max(3, Mathf.RoundToInt(Mathf.Lerp(10f, 3f, blinkProgress)));
                minBlinkH = minHeight;
            }

            // 추적 장애물 (CubeHomingObstacle): 최소 이동 속도 0.5f에서 50개 티어 동안 단계적으로 점진 상승 (0.5f -> 5.0f)
            int minHomingH = 10;
            int homingInterval = Mathf.Max(4, Mathf.RoundToInt(Mathf.Lerp(18f, 4f, progress)));
            float homingSpeed = Mathf.Lerp(0.5f, 5.0f, progress);

            // 코인 생성 간격 및 연속 스폰 이벤트
            int coinInterval = Mathf.Min(3, Mathf.Max(1, Mathf.RoundToInt(Mathf.Lerp(1f, 3f, progress))));
            int coinSequence = (i % 5 == 0 && i < 30) ? 2 : 1;

            float minSpeed = 4.0f;
            float maxSpeed = 6.0f;

            list.Add(new DifficultyTier
            {
                minHeight = minHeight,
                minSpawnX = minSpawnX,
                maxSpawnX = maxSpawnX,
                staticObstacleInterval = staticInterval,
                stationaryObstacleInterval = stationaryInterval,
                minStationaryHeight = minStationaryH,
                flyingObstacleInterval = flyingInterval,
                blinkObstacleInterval = blinkInterval,
                minBlinkHeight = minBlinkH,
                homingObstacleInterval = homingInterval,
                minHomingHeight = minHomingH,
                homingSpeed = homingSpeed,
                coinInterval = coinInterval,
                coinSequence = coinSequence,
                minFlyingSpeed = minSpeed,
                maxFlyingSpeed = maxSpeed,
                segmentPrefabs = null
            });
        }
        return list;
    }

    public DifficultyTier GetTierForHeight(int y)
    {
        // 1. 순환 주기 계산
        int cycleHeight = 500;
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
            // Fallback (기본 세팅 반으로 축소 반영)
            activeTier.minHeight = 0;
            activeTier.minSpawnX = -15;
            activeTier.maxSpawnX = 15;
            activeTier.staticObstacleInterval = 5;
            activeTier.stationaryObstacleInterval = 6;
            activeTier.minStationaryHeight = 0;
            activeTier.flyingObstacleInterval = 8;
            activeTier.blinkObstacleInterval = 0;
            activeTier.minBlinkHeight = 0;
            activeTier.coinInterval = 3;
            activeTier.coinSequence = 1;
            activeTier.minFlyingSpeed = 6.0f;
            activeTier.maxFlyingSpeed = 10.0f;
            activeTier.segmentPrefabs = null;
        }

        // 3. 순환 횟수(cycleCount)에 따른 보정치 적용
        if (cycleCount > 0)
        {
            activeTier.staticObstacleInterval = Mathf.Max(2, activeTier.staticObstacleInterval - cycleCount);
            activeTier.stationaryObstacleInterval = Mathf.Max(2, activeTier.stationaryObstacleInterval - cycleCount);
            activeTier.flyingObstacleInterval = Mathf.Max(2, activeTier.flyingObstacleInterval - cycleCount);

            if (activeTier.blinkObstacleInterval > 0)
            {
                activeTier.blinkObstacleInterval = Mathf.Max(2, activeTier.blinkObstacleInterval - cycleCount);
            }

            int requestedMinX = activeTier.minSpawnX + cycleCount;
            int requestedMaxX = activeTier.maxSpawnX - cycleCount;
            if (requestedMaxX - requestedMinX >= 5)
            {
                activeTier.minSpawnX = requestedMinX;
                activeTier.maxSpawnX = requestedMaxX;
            }
            else
            {
                int center = (activeTier.minSpawnX + activeTier.maxSpawnX) / 2;
                activeTier.minSpawnX = center - 2;
                activeTier.maxSpawnX = center + 2;
            }

            activeTier.coinInterval = Mathf.Min(20, activeTier.coinInterval + cycleCount);
            activeTier.coinSequence = Mathf.Max(1, activeTier.coinSequence - cycleCount);
        }

        return activeTier;
    }
}
