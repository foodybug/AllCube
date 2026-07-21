using UnityEngine;
using UnityEditor;
using UnityEngine.Tilemaps;
using System.IO;
using System.Collections.Generic;

public class StagePrefabGenerator : EditorWindow
{
    [MenuItem("Tools/Generate Stage Tilemap Prefabs")]
    public static void GenerateStagePrefabs()
    {
        // 1. 저장 디렉토리 준비
        string prefabDirPath = "Assets/Prefabs";
        if (!Directory.Exists(prefabDirPath))
        {
            Directory.CreateDirectory(prefabDirPath);
        }

        string tilemapSamplePath = "Assets/TilemapSample";
        if (!Directory.Exists(tilemapSamplePath))
        {
            Directory.CreateDirectory(tilemapSamplePath);
        }

        // 2. 타일 에셋 준비 (없으면 생성)
        Tile tileNormal = CreateOrGetTile("Tile_Normal", Color.green);
        Tile tileBreak = CreateOrGetTile("Tile_Break", Color.yellow);
        Tile tileMoveX = CreateOrGetTile("Tile_MoveX", Color.blue);
        Tile tileMoveY = CreateOrGetTile("Tile_MoveY", new Color(0f, 0.5f, 1f));
        Tile tileCoin = CreateOrGetTile("Tile_Coin", Color.cyan);

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        // 3. MapManager 정보 획득 (리플렉션 활용)
        MapManager mapManager = FindAnyObjectByType<MapManager>();
        if (mapManager == null)
        {
            string[] guids = AssetDatabase.FindAssets("MapManager t:Prefab");
            if (guids.Length > 0)
            {
                string path = AssetDatabase.GUIDToAssetPath(guids[0]);
                GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                if (prefab != null)
                {
                    mapManager = prefab.GetComponent<MapManager>();
                }
            }
        }

        // 폴백용 정적 티어 데이터 선언 (MapManager 정보 로드 실패 시용)
        int tierCount = 5;
        List<int> minSpawnXList = new List<int> { -30, -25, -20, -15, -10 };
        List<int> maxSpawnXList = new List<int> { 30, 25, 20, 15, 10 };

        if (mapManager != null)
        {
            try
            {
                System.Collections.IList list = null;
                System.Reflection.FieldInfo stageConfigField = typeof(MapManager).GetField("m_stageConfig", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                if (stageConfigField != null)
                {
                    object stageConfig = stageConfigField.GetValue(mapManager);
                    if (stageConfig != null)
                    {
                        System.Reflection.FieldInfo tierListField = stageConfig.GetType().GetField("m_difficultyTier", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                        if (tierListField != null)
                        {
                            list = tierListField.GetValue(stageConfig) as System.Collections.IList;
                        }
                    }
                }

                if (list == null || list.Count == 0)
                {
                    System.Reflection.FieldInfo field = typeof(MapManager).GetField("m_difficultyTier", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                    if (field != null)
                    {
                        list = field.GetValue(mapManager) as System.Collections.IList;
                    }
                }

                if (list != null && list.Count > 0)
                {
                    tierCount = list.Count;
                    minSpawnXList.Clear();
                    maxSpawnXList.Clear();

                    for (int i = 0; i < list.Count; i++)
                    {
                        object tier = list[i];
                        System.Reflection.FieldInfo minXField = tier.GetType().GetField("minSpawnX");
                        System.Reflection.FieldInfo maxXField = tier.GetType().GetField("maxSpawnX");

                        int minX = (int)minXField.GetValue(tier);
                        int maxX = (int)maxXField.GetValue(tier);

                        minSpawnXList.Add(minX);
                        maxSpawnXList.Add(maxX);
                    }
                    Debug.Log($"[StagePrefabGenerator] Found MapManager with {tierCount} tiers dynamically via reflection.");
                }
            }
            catch (System.Exception ex)
            {
                Debug.LogWarning($"[StagePrefabGenerator] Failed to parse MapManager via reflection, using fallback values. Exception: {ex.Message}");
            }
        }
        else
        {
            Debug.Log("[StagePrefabGenerator] MapManager not found in scene or prefabs, using default difficulty tier values.");
        }

        // 4. 각 Stage(DifficultyTier)별 프리팹 생성 루프
        int generatedCount = 0;
        for (int i = 0; i < tierCount; i++)
        {
            int minX = minSpawnXList[i];
            int maxX = maxSpawnXList[i];

            // 가상 그리드 / 타일맵 구조 구축
            string stageName = $"Stage_{i}_Segment";
            GameObject gridGo = new GameObject(stageName);
            gridGo.AddComponent<Grid>();

            GameObject tilemapGo = new GameObject("Tilemap");
            tilemapGo.transform.parent = gridGo.transform;
            Tilemap tilemap = tilemapGo.AddComponent<Tilemap>();
            tilemapGo.AddComponent<TilemapRenderer>();

            // Y=0: 바닥 발판 (minSpawnX + 2 ~ maxSpawnX 범위 내에 3칸 간격으로 예시 Normal 발판 배치)
            // 경계벽(minSpawnX + 1, maxSpawnX + 1)을 고려하여 안쪽에 배치
            for (int x = minX + 2; x <= maxX; x += 3)
            {
                tilemap.SetTile(new Vector3Int(x, 0, 0), tileNormal);
            }

            // Y=2: 깨지는 디딤판 배치 (중앙 및 주변 배치)
            int centerX = (minX + maxX) / 2;
            tilemap.SetTile(new Vector3Int(centerX - 3, 2, 0), tileBreak);
            tilemap.SetTile(new Vector3Int(centerX + 3, 2, 0), tileBreak);

            // Y=4: 가로 이동 발판 배치
            tilemap.SetTile(new Vector3Int(centerX, 4, 0), tileMoveX);

            // Y=6: 코인 배치
            tilemap.SetTile(new Vector3Int(centerX, 6, 0), tileCoin);
            tilemap.SetTile(new Vector3Int(centerX - 3, 6, 0), tileCoin);
            tilemap.SetTile(new Vector3Int(centerX + 3, 6, 0), tileCoin);

            // Y=8: 세로 이동 발판 배치
            tilemap.SetTile(new Vector3Int(centerX - 6, 8, 0), tileMoveY);
            tilemap.SetTile(new Vector3Int(centerX + 6, 8, 0), tileMoveY);

            // 프리팹 에셋으로 저장 후 임시 오브젝트 파괴
            string prefabSavePath = $"{prefabDirPath}/{stageName}.prefab";
            PrefabUtility.SaveAsPrefabAsset(gridGo, prefabSavePath);
            DestroyImmediate(gridGo);

            generatedCount++;
            Debug.Log($"[StagePrefabGenerator] Generated Tilemap segment prefab for Tier {i} at: {prefabSavePath} (Width: {minX} to {maxX})");
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        EditorUtility.DisplayDialog("Stage Prefab Generator", $"성공적으로 {generatedCount}개의 DifficultyTier 세그먼트 프리팹을 빌드했습니다!", "확인");
    }

    private static Tile CreateOrGetTile(string name, Color color)
    {
        string path = $"Assets/TilemapSample/{name}.asset";
        Tile tile = AssetDatabase.LoadAssetAtPath<Tile>(path);
        if (tile == null)
        {
            tile = ScriptableObject.CreateInstance<Tile>();
            Sprite defaultSprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/Background.psd");
            if (defaultSprite != null)
            {
                tile.sprite = defaultSprite;
                tile.color = color;
            }
            AssetDatabase.CreateAsset(tile, path);
        }
        return tile;
    }
}
