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

    public static List<DifficultyTier> GenerateDefault50Tiers()
    {
        List<DifficultyTier> list = new List<DifficultyTier>();
        int totalTiers = 50;

        for (int i = 0; i < totalTiers; i++)
        {
            float progress = (float)i / (totalTiers - 1); // 0.0 ~ 1.0

            int minHeight = i * 20; // 0, 20, 40, ..., 980

            // 스폰 영역 폭: 초기 [-30, 30] (폭 60) 에서 후반부 [-9, 9] (폭 18) 로 점진 축소
            int spawnWidth = Mathf.RoundToInt(Mathf.Lerp(30f, 9f, progress));
            int minSpawnX = -spawnWidth;
            int maxSpawnX = spawnWidth;

            // 정적 및 비행 장애물 간격: 초반에는 12~16칸 간격, 후반부엔 3~4칸 간격으로 밀도 증가
            int staticInterval = Mathf.Max(3, Mathf.RoundToInt(Mathf.Lerp(12f, 3f, progress)));
            int flyingInterval = Mathf.Max(4, Mathf.RoundToInt(Mathf.Lerp(16f, 4f, progress)));

            // 깜빡이 장애물: Tier 8 (높이 160m) 이상부터 최초 등장하며 점점 빈도 증가
            int blinkInterval = 0;
            int minBlinkH = 0;
            if (i >= 8)
            {
                float blinkProgress = (float)(i - 8) / (totalTiers - 9);
                blinkInterval = Mathf.Max(3, Mathf.RoundToInt(Mathf.Lerp(10f, 3f, blinkProgress)));
                minBlinkH = minHeight;
            }

            // 코인 생성 간격: 초반 3칸마다 등장 -> 후반 9칸마다 희귀하게 등장
            int coinInterval = Mathf.Min(9, Mathf.RoundToInt(Mathf.Lerp(3f, 9f, progress)));
            int coinSequence = (i % 5 == 0 && i < 30) ? 2 : 1; // 특정 단계마다 연속 코인 이벤트 부여

            // 비행 장애물 이동 속도: 처음 초기값 유지 (4.0f ~ 6.0f)
            float minSpeed = 4.0f;
            float maxSpeed = 6.0f;

            list.Add(new DifficultyTier
            {
                minHeight = minHeight,
                minSpawnX = minSpawnX,
                maxSpawnX = maxSpawnX,
                staticObstacleInterval = staticInterval,
                flyingObstacleInterval = flyingInterval,
                blinkObstacleInterval = blinkInterval,
                minBlinkHeight = minBlinkH,
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
        // 1. 순환 주기 계산 (마지막 티어의 minHeight와 그 이전 티어의 minHeight 차이 기준)
        int cycleHeight = 1000;
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
            activeTier.blinkObstacleInterval = 0;
            activeTier.minBlinkHeight = 0;
            activeTier.coinInterval = 3;
            activeTier.coinSequence = 1;
            activeTier.minFlyingSpeed = 6.0f;
            activeTier.maxFlyingSpeed = 10.0f;
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

            // 비행 장애물 속도는 초반 초기값(4.0f ~ 6.0f)으로 고정 유지

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
        }

        return activeTier;
    }
}
