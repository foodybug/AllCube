using UnityEngine;
using UnityEditor;
using UnityEngine.UI;

[InitializeOnLoad]
public class OptionPopupPrefabGenerator
{
    static OptionPopupPrefabGenerator()
    {
        EditorApplication.delayCall += () =>
        {
            if (!System.IO.File.Exists("Assets/Resources/UI/TitleOptionPopup.prefab"))
            {
                GeneratePrefab();
            }
        };
    }

    [MenuItem("Tools/AllCube/Generate Option Popup Prefab")]
    public static void GeneratePrefab()
    {
        if (!AssetDatabase.IsValidFolder("Assets/Resources"))
        {
            AssetDatabase.CreateFolder("Assets", "Resources");
        }
        if (!AssetDatabase.IsValidFolder("Assets/Resources/UI"))
        {
            AssetDatabase.CreateFolder("Assets/Resources", "UI");
        }

        string prefabPath = "Assets/Resources/UI/TitleOptionPopup.prefab";

        GameObject root = new GameObject("TitleOptionPopup");
        RectTransform rootRect = root.AddComponent<RectTransform>();
        rootRect.anchorMin = Vector2.zero;
        rootRect.anchorMax = Vector2.one;
        rootRect.offsetMin = Vector2.zero;
        rootRect.offsetMax = Vector2.zero;

        Image overlayImg = root.AddComponent<Image>();
        overlayImg.color = new Color(0f, 0f, 0f, 0.45f);

        TitleOptionPopup popupScript = root.AddComponent<TitleOptionPopup>();

        // 2. Main Dialog Panel
        GameObject panelObj = new GameObject("OptionPanel");
        panelObj.transform.SetParent(root.transform, false);

        RectTransform panelRect = panelObj.AddComponent<RectTransform>();
        panelRect.sizeDelta = new Vector2(560, 480);
        panelRect.anchoredPosition = Vector2.zero;

        RawImage panelImg = panelObj.AddComponent<RawImage>();
        Texture panelTex = AssetDatabase.LoadAssetAtPath<Texture>("Assets/Resources/UI/rounded_box.png");
        if (panelTex == null) panelTex = AssetDatabase.LoadAssetAtPath<Texture>("Assets/Resources/UI/msgbox.png");
        panelImg.texture = panelTex;
        panelImg.color = new Color(0.18f, 0.28f, 0.38f, 0.96f);

        Outline outline = panelObj.AddComponent<Outline>();
        outline.effectColor = new Color(0.47f, 0.92f, 0.98f, 0.9f);
        outline.effectDistance = new Vector2(3, -3);

        Font defaultFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

        // 3. Header Text ("SETTINGS")
        GameObject headerObj = new GameObject("TextHeader");
        headerObj.transform.SetParent(panelObj.transform, false);

        RectTransform headerRect = headerObj.AddComponent<RectTransform>();
        headerRect.anchoredPosition = new Vector2(0, 190);
        headerRect.sizeDelta = new Vector2(400, 50);

        Text headerText = headerObj.AddComponent<Text>();
        headerText.font = defaultFont;
        headerText.text = "SETTINGS";
        headerText.fontSize = 32;
        headerText.alignment = TextAnchor.MiddleCenter;
        headerText.fontStyle = FontStyle.Bold;
        headerText.color = new Color(1.0f, 0.93f, 0.49f, 1.0f);

        // 4. BGM Slider Row
        CreateSliderRow(panelObj.transform, defaultFont, "BGM Volume", new Vector2(0, 90));

        // 5. SFX Slider Row
        CreateSliderRow(panelObj.transform, defaultFont, "SFX Volume", new Vector2(0, -10));

        // 6. Quit Game Button
        CreateButton(panelObj.transform, defaultFont, "Btn_QuitGame", "Quit Game", new Vector2(-120, -160), new Vector2(200, 60), new Color(0.92f, 0.35f, 0.35f, 1f));

        // 7. Close Button
        CreateButton(panelObj.transform, defaultFont, "Btn_Close", "Close", new Vector2(120, -160), new Vector2(200, 60), new Color(0.47f, 0.92f, 0.98f, 1f));

        PrefabUtility.SaveAsPrefabAsset(root, prefabPath);
        Object.DestroyImmediate(root);

        Debug.Log("[OptionPopupPrefabGenerator] Prefab successfully saved at: " + prefabPath);
    }

    private static void CreateSliderRow(Transform parent, Font font, string labelStr, Vector2 pos)
    {
        string rowName = "Row_" + labelStr.Replace(" ", "");
        GameObject rowObj = new GameObject(rowName);
        rowObj.transform.SetParent(parent, false);

        RectTransform rowRect = rowObj.AddComponent<RectTransform>();
        rowRect.anchoredPosition = pos;
        rowRect.sizeDelta = new Vector2(480, 80);

        // Label
        GameObject labelObj = new GameObject("Text_Label");
        labelObj.transform.SetParent(rowObj.transform, false);

        RectTransform labelRect = labelObj.AddComponent<RectTransform>();
        labelRect.anchoredPosition = new Vector2(-120, 20);
        labelRect.sizeDelta = new Vector2(200, 30);

        Text labelText = labelObj.AddComponent<Text>();
        labelText.font = font;
        labelText.text = labelStr;
        labelText.fontSize = 20;
        labelText.fontStyle = FontStyle.Bold;
        labelText.color = Color.white;
        labelText.alignment = TextAnchor.MiddleLeft;

        // Value
        GameObject valObj = new GameObject("Text_Value");
        valObj.transform.SetParent(rowObj.transform, false);

        RectTransform valRect = valObj.AddComponent<RectTransform>();
        valRect.anchoredPosition = new Vector2(160, 20);
        valRect.sizeDelta = new Vector2(100, 30);

        Text valText = valObj.AddComponent<Text>();
        valText.font = font;
        valText.text = "100%";
        valText.fontSize = 20;
        valText.fontStyle = FontStyle.Bold;
        valText.color = new Color(0.4f, 0.9f, 1.0f, 1.0f);
        valText.alignment = TextAnchor.MiddleRight;

        // Slider Root
        GameObject sliderObj = new GameObject("Slider");
        sliderObj.transform.SetParent(rowObj.transform, false);

        RectTransform sliderRect = sliderObj.AddComponent<RectTransform>();
        sliderRect.anchoredPosition = new Vector2(0, -20);
        sliderRect.sizeDelta = new Vector2(440, 24);

        Slider slider = sliderObj.AddComponent<Slider>();

        Texture roundedTex = AssetDatabase.LoadAssetAtPath<Texture>("Assets/Resources/UI/rounded_box.png");

        // Background
        GameObject bgObj = new GameObject("Background");
        bgObj.transform.SetParent(sliderObj.transform, false);

        RectTransform bgRect = bgObj.AddComponent<RectTransform>();
        bgRect.anchorMin = Vector2.zero;
        bgRect.anchorMax = Vector2.one;
        bgRect.offsetMin = Vector2.zero;
        bgRect.offsetMax = Vector2.zero;

        RawImage bgImg = bgObj.AddComponent<RawImage>();
        if (roundedTex != null) bgImg.texture = roundedTex;
        bgImg.color = new Color(0.12f, 0.18f, 0.26f, 1.0f);

        // Fill Area
        GameObject fillArea = new GameObject("Fill Area");
        fillArea.transform.SetParent(sliderObj.transform, false);

        RectTransform fillAreaRect = fillArea.AddComponent<RectTransform>();
        fillAreaRect.anchorMin = Vector2.zero;
        fillAreaRect.anchorMax = Vector2.one;
        fillAreaRect.offsetMin = Vector2.zero;
        fillAreaRect.offsetMax = Vector2.zero;

        GameObject fillObj = new GameObject("Fill");
        fillObj.transform.SetParent(fillArea.transform, false);

        RectTransform fillRect = fillObj.AddComponent<RectTransform>();
        fillRect.anchorMin = Vector2.zero;
        fillRect.anchorMax = Vector2.one;
        fillRect.offsetMin = Vector2.zero;
        fillRect.offsetMax = Vector2.zero;

        RawImage fillImg = fillObj.AddComponent<RawImage>();
        if (roundedTex != null) fillImg.texture = roundedTex;
        fillImg.color = new Color(0.47f, 0.92f, 0.98f, 1.0f);

        slider.fillRect = fillRect;

        // Handle Area
        GameObject handleArea = new GameObject("Handle Slide Area");
        handleArea.transform.SetParent(sliderObj.transform, false);

        RectTransform handleAreaRect = handleArea.AddComponent<RectTransform>();
        handleAreaRect.anchorMin = Vector2.zero;
        handleAreaRect.anchorMax = Vector2.one;
        handleAreaRect.offsetMin = Vector2.zero;
        handleAreaRect.offsetMax = Vector2.zero;

        GameObject handleObj = new GameObject("Handle");
        handleObj.transform.SetParent(handleArea.transform, false);

        RectTransform handleRect = handleObj.AddComponent<RectTransform>();
        handleRect.sizeDelta = new Vector2(30, 30);

        RawImage handleImg = handleObj.AddComponent<RawImage>();
        if (roundedTex != null) handleImg.texture = roundedTex;
        handleImg.color = Color.white;

        slider.handleRect = handleRect;
        slider.targetGraphic = handleImg;

        slider.minValue = 0.0f;
        slider.maxValue = 1.0f;
        slider.value = 1.0f;
    }

    private static void CreateButton(Transform parent, Font font, string objName, string btnText, Vector2 pos, Vector2 size, Color btnColor)
    {
        GameObject btnObj = new GameObject(objName);
        btnObj.transform.SetParent(parent, false);

        RectTransform btnRect = btnObj.AddComponent<RectTransform>();
        btnRect.anchoredPosition = pos;
        btnRect.sizeDelta = size;

        RawImage btnImg = btnObj.AddComponent<RawImage>();
        Texture roundedTex = AssetDatabase.LoadAssetAtPath<Texture>("Assets/Resources/UI/rounded_box.png");
        if (roundedTex != null) btnImg.texture = roundedTex;
        btnImg.color = btnColor;

        Button btn = btnObj.AddComponent<Button>();
        btn.targetGraphic = btnImg;

        GameObject textObj = new GameObject("Text");
        textObj.transform.SetParent(btnObj.transform, false);

        RectTransform textRect = textObj.AddComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = Vector2.zero;
        textRect.offsetMax = Vector2.zero;

        Text txt = textObj.AddComponent<Text>();
        txt.font = font;
        txt.text = btnText;
        txt.fontSize = 22;
        txt.fontStyle = FontStyle.Bold;
        txt.color = Color.white;
        txt.alignment = TextAnchor.MiddleCenter;
    }
}
