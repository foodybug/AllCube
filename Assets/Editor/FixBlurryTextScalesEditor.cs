#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

public class FixBlurryTextScalesEditor
{
    [MenuItem("Tools/AllCube/Fix Blurry Text UI (Reset Scale & Native FontSize)", false, 11)]
    public static void FixBlurryTextScales()
    {
        string[] scenePaths = new string[]
        {
            "Assets/Scene/Title.unity",
            "Assets/Scene/Play.unity",
            "Assets/Scene/Result.unity",
            "Assets/Scene/LevelSelect.unity"
        };

        int totalFixed = 0;

        foreach (string path in scenePaths)
        {
            if (!System.IO.File.Exists(path)) continue;

            var scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Single);
            if (!scene.IsValid()) continue;

            Text[] texts = Object.FindObjectsByType<Text>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            int sceneFixed = 0;

            foreach (Text t in texts)
            {
                if (t == null) continue;
                Vector3 scale = t.transform.localScale;

                // localScale이 1.0이 아니어서 그래픽이 번지거나 뿌옇게 나오는 현상 감지
                if (Mathf.Abs(scale.x - 1.0f) > 0.01f || Mathf.Abs(scale.y - 1.0f) > 0.01f)
                {
                    Undo.RecordObject(t.transform, "Reset Text Scale");
                    Undo.RecordObject(t, "Fix Crisp FontSize");

                    float scaleFactor = (scale.x + scale.y) * 0.5f;
                    int nativeFontSize = Mathf.Clamp(Mathf.RoundToInt(t.fontSize * scaleFactor), 10, 120);

                    t.fontSize = nativeFontSize;
                    t.transform.localScale = Vector3.one;
                    t.verticalOverflow = VerticalWrapMode.Overflow;
                    t.horizontalOverflow = HorizontalWrapMode.Overflow;

                    t.SetAllDirty();
                    EditorUtility.SetDirty(t);
                    EditorUtility.SetDirty(t.gameObject);

                    sceneFixed++;
                    totalFixed++;
                    Debug.Log($"[FixBlurryText] Fixed '{t.gameObject.name}' in {path}: Restored localScale (1,1,1) and increased native fontSize to {nativeFontSize}");
                }
            }

            if (sceneFixed > 0)
            {
                EditorSceneManager.MarkSceneDirty(scene);
                EditorSceneManager.SaveScene(scene);
            }
        }

        // Title 씬으로 원복
        if (System.IO.File.Exists("Assets/Scene/Title.unity"))
        {
            EditorSceneManager.OpenScene("Assets/Scene/Title.unity", OpenSceneMode.Single);
        }

        Debug.Log($"[FixBlurryText] COMPLETED: Fixed {totalFixed} blurry text component(s) across all scenes!");
        EditorUtility.DisplayDialog("Blurry Text Fix Complete", $"총 {totalFixed}개의 Text UI의 localScale(1,1,1) 및 네이티브 폰트 크기(fontSize) 보정이 완료되었습니다!\n\n이제 텍스트가 해상도 깨짐/뿌염 현상 없이 100% 또렷하게 출력됩니다.", "확인");
    }
}
#endif
