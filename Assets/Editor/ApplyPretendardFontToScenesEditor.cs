using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

public class ApplyPretendardFontToScenesEditor
{
    [MenuItem("Tools/AllCube/Apply Pretendard Font to All Scenes", false, 10)]
    public static void ApplyPretendardFontToAllScenes()
    {
        Font pretendardFont = AssetDatabase.LoadAssetAtPath<Font>("Assets/Resources/Font/PretendardVariable.ttf");
        if (pretendardFont == null)
        {
            Debug.LogError("[ApplyPretendardFont] Could not find PretendardVariable.ttf at Assets/Resources/Font/PretendardVariable.ttf");
            return;
        }

        string[] scenePaths = new string[]
        {
            "Assets/Scene/Title.unity",
            "Assets/Scene/Play.unity",
            "Assets/Scene/Result.unity",
            "Assets/Scene/LevelSelect.unity"
        };

        int totalModifiedCount = 0;

        foreach (string path in scenePaths)
        {
            if (!System.IO.File.Exists(path)) continue;

            var scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Single);
            if (!scene.IsValid()) continue;

            Text[] texts = Object.FindObjectsByType<Text>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            int sceneModifiedCount = 0;

            foreach (Text t in texts)
            {
                if (t != null && t.font != pretendardFont)
                {
                    Undo.RecordObject(t, "Apply Pretendard Font");
                    t.font = pretendardFont;
                    t.SetAllDirty();
                    EditorUtility.SetDirty(t);
                    sceneModifiedCount++;
                    totalModifiedCount++;
                }
            }

            if (sceneModifiedCount > 0)
            {
                EditorSceneManager.MarkSceneDirty(scene);
                EditorSceneManager.SaveScene(scene);
                Debug.Log($"[ApplyPretendardFont] Applied PretendardVariable font to {sceneModifiedCount} text(s) in {path}");
            }
        }

        // Return to Title scene
        if (System.IO.File.Exists("Assets/Scene/Title.unity"))
        {
            EditorSceneManager.OpenScene("Assets/Scene/Title.unity", OpenSceneMode.Single);
        }

        Debug.Log($"[ApplyPretendardFont] COMPLETED: Applied PretendardVariable font to {totalModifiedCount} text elements across all scenes!");
        EditorUtility.DisplayDialog("Font Application Complete", $"PretendardVariable 폰트가 모든 씬(Title, Play, Result 등)의 총 {totalModifiedCount}개 Text UI에 성공적으로 반영되었습니다!", "확인");
    }
}
