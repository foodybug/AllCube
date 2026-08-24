using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class GlobalFontManager : MonoBehaviour
{
    private static Font s_pretendardFont;

    public static Font PretendardFont
    {
        get
        {
            if (s_pretendardFont == null)
            {
                s_pretendardFont = Resources.Load<Font>("Font/PretendardVariable");
                if (s_pretendardFont == null)
                {
                    s_pretendardFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
                }
            }
            return s_pretendardFont;
        }
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void InitGlobalFontManager()
    {
        ApplyFontToActiveScene();
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        ApplyFontToActiveScene();
    }

    public static void ApplyFontToActiveScene()
    {
        Font targetFont = PretendardFont;
        if (targetFont == null) return;

        Text[] allTexts = FindObjectsByType<Text>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        int count = 0;
        foreach (Text t in allTexts)
        {
            if (t != null && t.font != targetFont)
            {
                t.font = targetFont;
                t.SetAllDirty();
                count++;
            }
        }
    }

    public static void ApplyFontToHierarchy(GameObject root)
    {
        if (root == null) return;
        Font targetFont = PretendardFont;
        if (targetFont == null) return;

        Text[] allTexts = root.GetComponentsInChildren<Text>(true);
        foreach (Text t in allTexts)
        {
            if (t != null && t.font != targetFont)
            {
                t.font = targetFont;
                t.SetAllDirty();
            }
        }
    }
}
