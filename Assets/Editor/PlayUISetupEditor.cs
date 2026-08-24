#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

public class PlayUISetupEditor : EditorWindow
{
    [MenuItem("Tools/AllCube/Complete Play UI Bindings", false, 5)]
    public static void SetupPlayUI()
    {
        string scenePath = "Assets/Scene/Play.unity";
        var scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
        if (!scene.IsValid())
        {
            Debug.LogError("[PlayUISetup] Play.unity scene is invalid or missing at: " + scenePath);
            return;
        }

        Debug.Log("[PlayUISetup] Successfully loaded Play scene.");

        UI_Play uiPlay = Object.FindFirstObjectByType<UI_Play>();
        if (uiPlay == null)
        {
            GameObject newGo = new GameObject("PlayUI");
            uiPlay = Undo.AddComponent<UI_Play>(newGo);
        }

        GameObject targetGo = uiPlay.gameObject;
        PlayMain playMain = targetGo.GetComponent<PlayMain>();
        if (playMain == null)
        {
            playMain = Undo.AddComponent<PlayMain>(targetGo);
        }

        Canvas canvas = Object.FindFirstObjectByType<Canvas>();
        if (canvas == null)
        {
            GameObject canvasGo = new GameObject("Canvas");
            canvas = canvasGo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvasGo.AddComponent<CanvasScaler>();
            canvasGo.AddComponent<GraphicRaycaster>();
            Undo.RegisterCreatedObjectUndo(canvasGo, "Create Canvas");
        }

        if (Object.FindFirstObjectByType<UnityEngine.EventSystems.EventSystem>() == null)
        {
            GameObject esGo = new GameObject("EventSystem");
            esGo.AddComponent<UnityEngine.EventSystems.EventSystem>();
            esGo.AddComponent<UnityEngine.EventSystems.StandaloneInputModule>();
            Undo.RegisterCreatedObjectUndo(esGo, "Create EventSystem");
        }

        Transform canvasTrans = canvas.transform;
        Font pretendardFont = AssetDatabase.LoadAssetAtPath<Font>("Assets/Resources/Font/PretendardVariable.ttf");
        if (pretendardFont == null) pretendardFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

        // 1. textJumps (jumpcount UI)
        Text textJumps = FindComponentInScene<Text>("textJumps");
        if (textJumps == null)
        {
            GameObject jumpsGo = new GameObject("textJumps");
            jumpsGo.transform.SetParent(canvasTrans, false);
            textJumps = jumpsGo.AddComponent<Text>();
            
            textJumps.rectTransform.anchorMin = new Vector2(0.5f, 1.0f);
            textJumps.rectTransform.anchorMax = new Vector2(0.5f, 1.0f);
            textJumps.rectTransform.pivot = new Vector2(0.5f, 1.0f);
            textJumps.rectTransform.anchoredPosition = new Vector2(0, -45f);
            textJumps.rectTransform.sizeDelta = new Vector2(400, 50);

            textJumps.font = pretendardFont;
            textJumps.text = "Jumps 10";
            textJumps.fontSize = 28;
            textJumps.fontStyle = FontStyle.Bold;
            textJumps.alignment = TextAnchor.UpperCenter;
            textJumps.color = Color.white;
            textJumps.raycastTarget = false;

            AddOutlineAndShadow(jumpsGo);
            Undo.RegisterCreatedObjectUndo(jumpsGo, "Create textJumps");
        }
        else
        {
            if (pretendardFont != null) textJumps.font = pretendardFont;
            textJumps.raycastTarget = false;
        }

        // 2. textCombo (Combo UI)
        Text textCombo = FindComponentInScene<Text>("textCombo");
        if (textCombo == null)
        {
            GameObject comboGo = new GameObject("textCombo");
            comboGo.transform.SetParent(canvasTrans, false);
            textCombo = comboGo.AddComponent<Text>();

            textCombo.rectTransform.anchorMin = new Vector2(0.5f, 1.0f);
            textCombo.rectTransform.anchorMax = new Vector2(0.5f, 1.0f);
            textCombo.rectTransform.pivot = new Vector2(0.5f, 1.0f);
            textCombo.rectTransform.anchoredPosition = new Vector2(0, -110f);
            textCombo.rectTransform.sizeDelta = new Vector2(400, 50);
            textCombo.rectTransform.localScale = Vector3.one;

            textCombo.font = pretendardFont;
            textCombo.text = "COMBO x2";
            textCombo.fontSize = 38;
            textCombo.fontStyle = FontStyle.Bold;
            textCombo.alignment = TextAnchor.UpperCenter;
            textCombo.color = Color.yellow;
            textCombo.raycastTarget = false;

            AddOutlineAndShadow(comboGo);
            Undo.RegisterCreatedObjectUndo(comboGo, "Create textCombo");
        }
        else
        {
            if (pretendardFont != null) textCombo.font = pretendardFont;
            textCombo.raycastTarget = false;
        }

        // Bind references to UI_Play
        uiPlay.ui.textJumps = textJumps;
        uiPlay.ui.textCombo = textCombo;

        EditorUtility.SetDirty(targetGo);
        EditorUtility.SetDirty(uiPlay);
        EditorUtility.SetDirty(playMain);

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);

        Debug.Log("[PlayUISetup] Successfully placed and bound textJumps (jumpcount) and textCombo in Play.unity!");
        EditorUtility.DisplayDialog("Play UI Setup", "Play 씬 내에 textJumps (jumpcount) 및 textCombo UI 배치가 완료되었습니다!\n\n유니티 에디터의 Hierarchy에서 직접 클릭하여 위치, 크기, 스타일을 자유롭게 편집하실 수 있습니다.", "확인");
    }

    private static void AddOutlineAndShadow(GameObject go)
    {
        if (go.GetComponent<Outline>() == null)
        {
            var outline = go.AddComponent<Outline>();
            outline.effectColor = new Color(0.12f, 0.08f, 0.05f, 1.0f);
            outline.effectDistance = new Vector2(1.5f, -1.5f);
        }
        if (go.GetComponent<Shadow>() == null)
        {
            var shadow = go.AddComponent<Shadow>();
            shadow.effectColor = new Color(0, 0, 0, 0.5f);
            shadow.effectDistance = new Vector2(2f, -2f);
        }
    }

    private static T FindComponentInScene<T>(string name) where T : Component
    {
        T[] items = Resources.FindObjectsOfTypeAll<T>();
        if (items != null)
        {
            foreach (T item in items)
            {
                if (item != null && item.gameObject != null && item.gameObject.scene.isLoaded && item.name != null)
                {
                    if (item.name.Equals(name, System.StringComparison.OrdinalIgnoreCase))
                    {
                        return item;
                    }
                }
            }
        }
        return null;
    }
}
#endif
