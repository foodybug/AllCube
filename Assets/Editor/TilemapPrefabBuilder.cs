using UnityEngine;
using UnityEditor;
using UnityEngine.Tilemaps;
using System.IO;

public class TilemapPrefabBuilder : EditorWindow
{
    [MenuItem("Tools/Generate Sample Tilemap Prefab")]
    public static void GenerateSamplePrefab()
    {
        // 1. 디렉토리 검사 및 생성
        if (!Directory.Exists("Assets/Prefabs"))
        {
            Directory.CreateDirectory("Assets/Prefabs");
        }
        if (!Directory.Exists("Assets/TilemapSample"))
        {
            Directory.CreateDirectory("Assets/TilemapSample");
        }

        // 2. 이름 규격에 맞춘 테스트용 타일 에셋들(ScriptableObject) 생성
        Tile tileNormal = CreateOrGetTile("Tile_Normal", Color.green);
        Tile tileLaser = CreateOrGetTile("Tile_Laser", new Color(0.9f, 0.1f, 0.6f)); // 핑크/마젠타 빛 레이저 타일 생성
        Tile tileBreak = CreateOrGetTile("Tile_Break", Color.yellow);
        Tile tileMoveX = CreateOrGetTile("Tile_MoveX", Color.blue);
        Tile tileJumpZero = CreateOrGetTile("Tile_JumpZero", Color.red);
        Tile tileCoin = CreateOrGetTile("Tile_Coin", Color.cyan);

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        // 3. 씬 뷰 계통상에 임시 가상 Grid 와 자식 Tilemap 생성
        GameObject gridGo = new GameObject("Sample_Grid_Segment");
        gridGo.AddComponent<Grid>();

        GameObject tilemapGo = new GameObject("Tilemap");
        tilemapGo.transform.parent = gridGo.transform;
        Tilemap tilemap = tilemapGo.AddComponent<Tilemap>();
        tilemapGo.AddComponent<TilemapRenderer>();

        // 4. 예시 점프 루트 맵 드로잉 (그리드 페인팅 모사)
        // Y=0: 기초 안전 발판 배치 (가로 범위) -> laser 블럭으로 대체
        for (int x = -5; x <= 5; x++)
        {
            tilemap.SetTile(new Vector3Int(x, 0, 0), tileLaser);
        }

        // Y=2: 중간 깨지는 디딤판 배치
        tilemap.SetTile(new Vector3Int(-3, 2, 0), tileBreak);
        tilemap.SetTile(new Vector3Int(0, 2, 0), tileBreak);
        tilemap.SetTile(new Vector3Int(3, 2, 0), tileBreak);

        // Y=4~5: 가로 이동 발판과 먹을 코인 배치
        tilemap.SetTile(new Vector3Int(-1, 4, 0), tileMoveX);
        tilemap.SetTile(new Vector3Int(0, 5, 0), tileCoin);
        tilemap.SetTile(new Vector3Int(2, 5, 0), tileCoin);

        // Y=7~8: 공중 위협 장애물(JumpZero)과 점프 목표 코인 배치
        tilemap.SetTile(new Vector3Int(1, 7, 0), tileJumpZero);
        tilemap.SetTile(new Vector3Int(1, 8, 0), tileCoin);

        // 5. 구성 완료된 오브젝트를 실제 유니티 프리팹 에셋으로 영구 굽기
        string prefabPath = "Assets/Prefabs/SampleSegment.prefab";
        PrefabUtility.SaveAsPrefabAsset(gridGo, prefabPath);

        // 6. 임시 메모리 씬 오브젝트 해제 및 에셋 뷰 동기화
        DestroyImmediate(gridGo);

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        EditorUtility.DisplayDialog("AllCube Tools", $"성공적으로 예시 타일맵 프리팹을 빌드했습니다!\n경로: {prefabPath}", "확인");
        Debug.Log($"[TilemapPrefabBuilder] Sample prefab generated successfully at: {prefabPath}");
    }

    private static Tile CreateOrGetTile(string name, Color color)
    {
        string path = $"Assets/TilemapSample/{name}.asset";
        Tile tile = AssetDatabase.LoadAssetAtPath<Tile>(path);
        if (tile == null)
        {
            tile = ScriptableObject.CreateInstance<Tile>();
            
            // 유니티 내장 기본 흰색 스프라이트를 가져와 타일맵 렌더러 컬러 틴트가 보이도록 대입
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
