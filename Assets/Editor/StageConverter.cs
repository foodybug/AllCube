using UnityEngine;
using UnityEditor;
using UnityEngine.Tilemaps;
using System.IO;

public class StageConverter : EditorWindow
{
    [MenuItem("Tools/Convert Stage Bitmaps to Tilemaps")]
    public static void ConvertStages()
    {
        // 1. 프리팹 저장소 디렉토리 준비
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

        // 2. 비트맵 색상별 매핑할 타일 에셋 로드 또는 생성
        Tile tileNormal = CreateOrGetTile("Tile_Normal", Color.green);
        Tile tileBreak = CreateOrGetTile("Tile_Break", Color.yellow);
        Tile tileMoveX = CreateOrGetTile("Tile_MoveX", Color.blue);
        Tile tileMoveY = CreateOrGetTile("Tile_MoveY", new Color(0f, 0.5f, 1f)); // 세로 이동 발판용 하늘색 타일
        Tile tileCoin = CreateOrGetTile("Tile_Coin", Color.cyan);

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        // 3. Stage 폴더 내 모든 비트맵(bmp) 파일 탐색 및 역직렬화 루프
        string stageDirPath = "Assets/Resources/Stage";
        if (!Directory.Exists(stageDirPath))
        {
            EditorUtility.DisplayDialog("Stage Converter", $"비트맵 스테이지 폴더를 찾을 수 없습니다: {stageDirPath}", "확인");
            return;
        }

        string[] bmpFiles = Directory.GetFiles(stageDirPath, "*.bmp");
        if (bmpFiles.Length == 0)
        {
            EditorUtility.DisplayDialog("Stage Converter", "Stage 폴더 안에 변환 대상 bmp 파일이 없습니다.", "확인");
            return;
        }

        int successCount = 0;

        foreach (string bmpPath in bmpFiles)
        {
            // 파일명에서 스테이지 인덱스 추출 (예: "Assets/Resources/Stage/1.bmp" -> "1")
            string fileName = Path.GetFileNameWithoutExtension(bmpPath);
            
            // Resources.Load는 확장자와 Assets/Resources/ 상대 경로가 생략되어야 함 -> "Stage/1"
            string loadPath = "Stage/" + fileName;
            Texture2D texStage = Resources.Load(loadPath) as Texture2D;

            if (texStage == null)
            {
                Debug.LogWarning($"[StageConverter] Failed to load texture at path: {loadPath}");
                continue;
            }

            // 읽기 불가능한 텍스처 데이터 대비 사전 백업 및 Read/Write Enable 처리 안내
            // 텍스처 임포터 설정을 코드로 강제 조절하여 GetPixel 가능하게 제어
            string assetPath = AssetDatabase.GetAssetPath(texStage);
            TextureImporter importer = AssetImporter.GetAtPath(assetPath) as TextureImporter;
            if (importer != null && !importer.isReadable)
            {
                importer.isReadable = true;
                importer.filterMode = FilterMode.Point; // 픽셀 아트 형태
                importer.textureCompression = TextureImporterCompression.Uncompressed;
                AssetDatabase.ImportAsset(assetPath, ImportAssetOptions.ForceUpdate);
            }

            // 가상 임시 게임오브젝트 생성
            GameObject gridGo = new GameObject("Stage_" + fileName + "_Grid");
            gridGo.AddComponent<Grid>();

            GameObject tilemapGo = new GameObject("Tilemap");
            tilemapGo.transform.parent = gridGo.transform;
            Tilemap tilemap = tilemapGo.AddComponent<Tilemap>();
            tilemapGo.AddComponent<TilemapRenderer>();

            // 픽셀 전체 분석 및 타일 배치
            for (int y = 0; y < texStage.height; y++)
            {
                for (int x = 0; x < texStage.width; x++)
                {
                    Color color = texStage.GetPixel(x, y);
                    
                    // 예전 GetMapProp 로직 구현
                    Tile targetTile = null;
                    bool isNone = false;

                    if (IsColorEqual(Color.black, color))
                        targetTile = tileNormal;
                    else if (IsColorEqual(Color.green, color))
                        targetTile = tileCoin;
                    else if (IsColorEqual(Color.gray, color))
                        targetTile = tileBreak;
                    else if (IsColorEqual(Color.red, color))
                        targetTile = tileMoveX;
                    else if (IsColorEqual(Color.blue, color))
                        targetTile = tileMoveY;
                    else
                        isNone = true;

                    if (!isNone && targetTile != null)
                    {
                        // 픽셀 인덱스 (x, y) 그대로 타일 배치
                        tilemap.SetTile(new Vector3Int(x, y, 0), targetTile);
                    }
                }
            }

            // 프리팹 에셋으로 영구 굽기 및 메모리 해제
            string prefabSavePath = $"{prefabDirPath}/Stage_{fileName}.prefab";
            PrefabUtility.SaveAsPrefabAsset(gridGo, prefabSavePath);
            DestroyImmediate(gridGo);

            successCount++;
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        EditorUtility.DisplayDialog("Stage Converter", $"총 {successCount}개의 스테이지 비트맵을 타일맵 프리팹으로 전환 완료했습니다!", "확인");
        Debug.Log($"[StageConverter] Successfully converted {successCount} stage bitmaps to Tilemap prefabs inside Assets/Prefabs/");
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

    private static bool IsColorEqual(Color color1, Color color2)
    {
        float tolerance = 0.1f;
        if (Mathf.Abs(color1.r - color2.r) < tolerance &&
            Mathf.Abs(color1.g - color2.g) < tolerance &&
            Mathf.Abs(color1.b - color2.b) < tolerance)
        {
            return true;
        }
        return false;
    }
}
