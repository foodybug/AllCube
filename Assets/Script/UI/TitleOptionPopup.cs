using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class TitleOptionPopup : MonoBehaviour
{
    private GameObject m_goPopupRoot;
    private Slider m_sliderBgm;
    private Slider m_sliderSfx;
    private Text m_textBgmValue;
    private Text m_textSfxValue;
    private bool m_isInitialized = false;
    private static float m_lastCloseTime = -10f;

    public static TitleOptionPopup Instance { get; private set; }

    public static bool IsPopupJustClosed()
    {
        return (Time.unscaledTime - m_lastCloseTime) < 0.35f;
    }

    private void Awake()
    {
        Instance = this;
    }

    public void Show()
    {
        if (IsShowing())
        {
            return;
        }

        if (!m_isInitialized || m_goPopupRoot == null)
        {
            BuildOptionPopupUI();
            m_isInitialized = true;
        }

        if (m_goPopupRoot != null)
        {
            m_goPopupRoot.transform.SetAsLastSibling();
            m_goPopupRoot.SetActive(true);
            RefreshValues();
        }
    }

    public void Hide()
    {
        m_lastCloseTime = Time.unscaledTime;
        if (m_goPopupRoot != null)
        {
            m_goPopupRoot.SetActive(false);
        }
    }

    public bool IsShowing()
    {
        return m_goPopupRoot != null && m_goPopupRoot.activeSelf;
    }

    private void RefreshValues()
    {
        float bgmVal = AudioManager.Instance != null ? AudioManager.Instance.BgmVolume : 1.0f;
        float sfxVal = AudioManager.Instance != null ? AudioManager.Instance.SfxVolume : 1.0f;

        if (m_sliderBgm != null) m_sliderBgm.value = bgmVal;
        if (m_sliderSfx != null) m_sliderSfx.value = sfxVal;

        if (m_textBgmValue != null) m_textBgmValue.text = string.Format("{0}%", Mathf.RoundToInt(bgmVal * 100f));
        if (m_textSfxValue != null) m_textSfxValue.text = string.Format("{0}%", Mathf.RoundToInt(sfxVal * 100f));
    }

    private void BuildOptionPopupUI()
    {
        if (m_goPopupRoot != null) return;
        Canvas parentCanvas = GetComponentInParent<Canvas>();
        if (parentCanvas == null) parentCanvas = FindFirstObjectByType<Canvas>();

        if (parentCanvas == null)
        {
            GameObject canvasGo = new GameObject("OptionCanvas");
            parentCanvas = canvasGo.AddComponent<Canvas>();
            parentCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvasGo.AddComponent<UnityEngine.UI.CanvasScaler>();
            canvasGo.AddComponent<UnityEngine.UI.GraphicRaycaster>();
        }

        Transform parentTransform = parentCanvas.transform;

        // 0. Prefab 우선 로드 (UI/TitleOptionPopup 프리팹이 존재하는 경우 직접 편집본 사용)
        GameObject prefabAsset = Resources.Load<GameObject>("UI/TitleOptionPopup");
        if (prefabAsset != null)
        {
            m_goPopupRoot = Instantiate(prefabAsset, parentTransform, false);
            m_goPopupRoot.transform.SetAsLastSibling();
            BindPrefabUIReferences(m_goPopupRoot);
            return;
        }

        // 1. Dimmed Background Overlay
        m_goPopupRoot = new GameObject("OptionPopupOverlay");
        m_goPopupRoot.transform.SetParent(parentTransform, false);
        m_goPopupRoot.transform.SetAsLastSibling();

        RectTransform overlayRect = m_goPopupRoot.AddComponent<RectTransform>();
        overlayRect.anchorMin = Vector2.zero;
        overlayRect.anchorMax = Vector2.one;
        overlayRect.offsetMin = Vector2.zero;
        overlayRect.offsetMax = Vector2.zero;

        Image overlayImg = m_goPopupRoot.AddComponent<Image>();
        overlayImg.color = new Color(0f, 0f, 0f, 0.45f);

        // 2. Main Dialog Panel (Player Cyan-Slate Brighter Color)
        GameObject panelObj = new GameObject("OptionPanel");
        panelObj.transform.SetParent(m_goPopupRoot.transform, false);

        RectTransform panelRect = panelObj.AddComponent<RectTransform>();
        panelRect.sizeDelta = new Vector2(560, 480);
        panelRect.anchoredPosition = Vector2.zero;

        RawImage panelImg = panelObj.AddComponent<RawImage>();
        Texture panelTex = Resources.Load("UI/rounded_box") as Texture;
        if (panelTex == null) panelTex = Resources.Load("UI/msgbox") as Texture;
        panelImg.texture = panelTex;
        panelImg.color = new Color(0.18f, 0.28f, 0.38f, 0.96f); // Player Cyber Slate

        // Panel Border Outline (Player Sky Cyan)
        Outline outline = panelObj.AddComponent<Outline>();
        outline.effectColor = new Color(0.47f, 0.92f, 0.98f, 0.9f);
        outline.effectDistance = new Vector2(3, -3);

        Font defaultFont = null;
        try
        {
            defaultFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        }
        catch (System.Exception) { }

        if (defaultFont == null)
        {
            try
            {
                defaultFont = Font.CreateDynamicFontFromOSFont("Arial", 16);
            }
            catch (System.Exception) { }
        }

        // 3. Header Text ("SETTINGS") - Player Pastel Yellow
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
        headerText.color = new Color(1.0f, 0.93f, 0.49f, 1.0f); // Player Pastel Yellow

        // 4. BGM Volume Slider & Label
        CreateSliderRow(panelObj.transform, defaultFont, "BGM Volume", new Vector2(0, 90), out m_sliderBgm, out m_textBgmValue);
        Text textBgm = m_textBgmValue;
        if (m_sliderBgm != null)
        {
            m_sliderBgm.onValueChanged.AddListener((val) =>
            {
                if (AudioManager.Instance != null) AudioManager.Instance.BgmVolume = val;
                if (textBgm != null) textBgm.text = string.Format("{0}%", Mathf.RoundToInt(val * 100f));
            });
        }

        // 5. SFX Volume Slider & Label
        CreateSliderRow(panelObj.transform, defaultFont, "SFX Volume", new Vector2(0, -10), out m_sliderSfx, out m_textSfxValue);
        Text textSfx = m_textSfxValue;
        if (m_sliderSfx != null)
        {
            m_sliderSfx.onValueChanged.AddListener((val) =>
            {
                if (AudioManager.Instance != null) AudioManager.Instance.SfxVolume = val;
                if (textSfx != null) textSfx.text = string.Format("{0}%", Mathf.RoundToInt(val * 100f));
            });
        }

        // 6. Quit Game Button ("Quit Game") - Rounded Corners
        GameObject btnQuitObj = CreateStyledButton(panelObj.transform, defaultFont, "Quit Game", new Vector2(-120, -160), new Vector2(200, 60), new Color(0.9f, 0.28f, 0.28f, 1f));
        Button btnQuit = btnQuitObj.GetComponent<Button>();
        btnQuit.onClick.AddListener(OnQuitGameClicked);

        // 7. Close Button ("Close") - Player Sky Cyan Rounded Button
        GameObject btnCloseObj = CreateStyledButton(panelObj.transform, defaultFont, "Close", new Vector2(120, -160), new Vector2(200, 60), new Color(0.47f, 0.92f, 0.98f, 1f));
        Button btnClose = btnCloseObj.GetComponent<Button>();
        btnClose.onClick.AddListener(OnCloseClicked);
    }

    private void CreateSliderRow(Transform parent, Font font, string labelStr, Vector2 pos, out Slider sliderComp, out Text valueTextComp)
    {
        GameObject rowObj = new GameObject("Row_" + labelStr);
        rowObj.transform.SetParent(parent, false);

        RectTransform rowRect = rowObj.AddComponent<RectTransform>();
        rowRect.anchoredPosition = pos;
        rowRect.sizeDelta = new Vector2(480, 80);

        // Label
        GameObject labelObj = new GameObject("Label");
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

        // Value Text
        GameObject valObj = new GameObject("Value");
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
        valueTextComp = valText;

        // Slider Root
        GameObject sliderObj = new GameObject("Slider");
        sliderObj.transform.SetParent(rowObj.transform, false);

        RectTransform sliderRect = sliderObj.AddComponent<RectTransform>();
        sliderRect.anchoredPosition = new Vector2(0, -20);
        sliderRect.sizeDelta = new Vector2(440, 24);

        Slider slider = sliderObj.AddComponent<Slider>();
        sliderComp = slider;

        Texture roundedTex = Resources.Load("UI/rounded_box") as Texture;

        // Slider Background (Rounded)
        GameObject bgObj = new GameObject("Background");
        bgObj.transform.SetParent(sliderObj.transform, false);

        RectTransform bgRect = bgObj.AddComponent<RectTransform>();
        bgRect.anchorMin = Vector2.zero;
        bgRect.anchorMax = Vector2.one;
        bgRect.offsetMin = Vector2.zero;
        bgRect.offsetMax = Vector2.zero;

        RawImage bgImg = bgObj.AddComponent<RawImage>();
        if (roundedTex != null) bgImg.texture = roundedTex;
        bgImg.color = new Color(0.15f, 0.20f, 0.32f, 1.0f);

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
        fillImg.color = new Color(0.0f, 0.85f, 1.0f, 1.0f);

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
    }

    private GameObject CreateStyledButton(Transform parent, Font font, string btnText, Vector2 pos, Vector2 size, Color btnColor)
    {
        GameObject btnObj = new GameObject("Btn_" + btnText);
        btnObj.transform.SetParent(parent, false);

        RectTransform btnRect = btnObj.AddComponent<RectTransform>();
        btnRect.anchoredPosition = pos;
        btnRect.sizeDelta = size;

        RawImage btnImg = btnObj.AddComponent<RawImage>();
        Texture roundedTex = Resources.Load("UI/rounded_box") as Texture;
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

        return btnObj;
    }

    private void OnQuitGameClicked()
    {
        if (AudioManager.Instance != null) AudioManager.Instance.Play("Sound/ui_button_down");
        Debug.Log("[TitleOptionPopup] Quit Game Clicked!");
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    private void OnCloseClicked()
    {
        if (AudioManager.Instance != null) AudioManager.Instance.Play("Sound/ui_button_down");
        Hide();
    }

    private void BindPrefabUIReferences(GameObject root)
    {
        m_sliderBgm = FindInChild<Slider>(root, "Slider", "Row_BGMVolume");
        if (m_sliderBgm == null) m_sliderBgm = FindInChild<Slider>(root, "Slider");

        m_sliderSfx = FindInChild<Slider>(root, "Slider", "Row_SFXVolume");

        m_textBgmValue = FindInChild<Text>(root, "Text_Value", "Row_BGMVolume");
        m_textSfxValue = FindInChild<Text>(root, "Text_Value", "Row_SFXVolume");

        Button btnQuit = FindInChild<Button>(root, "Btn_QuitGame");
        if (btnQuit != null)
        {
            btnQuit.onClick.RemoveAllListeners();
            btnQuit.onClick.AddListener(OnQuitGameClicked);
        }

        Button btnClose = FindInChild<Button>(root, "Btn_Close");
        if (btnClose != null)
        {
            btnClose.onClick.RemoveAllListeners();
            btnClose.onClick.AddListener(OnCloseClicked);
        }

        Text textBgm = m_textBgmValue;
        if (m_sliderBgm != null)
        {
            m_sliderBgm.onValueChanged.RemoveAllListeners();
            m_sliderBgm.onValueChanged.AddListener((val) =>
            {
                if (AudioManager.Instance != null) AudioManager.Instance.BgmVolume = val;
                if (textBgm != null) textBgm.text = string.Format("{0}%", Mathf.RoundToInt(val * 100f));
            });
        }

        Text textSfx = m_textSfxValue;
        if (m_sliderSfx != null)
        {
            m_sliderSfx.onValueChanged.RemoveAllListeners();
            m_sliderSfx.onValueChanged.AddListener((val) =>
            {
                if (AudioManager.Instance != null) AudioManager.Instance.SfxVolume = val;
                if (textSfx != null) textSfx.text = string.Format("{0}%", Mathf.RoundToInt(val * 100f));
            });
        }
    }

    private T FindInChild<T>(GameObject root, string name, string parentNameFilter = null) where T : Component
    {
        T[] comps = root.GetComponentsInChildren<T>(true);
        foreach (T c in comps)
        {
            if (c.name.Equals(name, System.StringComparison.OrdinalIgnoreCase))
            {
                if (string.IsNullOrEmpty(parentNameFilter) || (c.transform.parent != null && c.transform.parent.name.Equals(parentNameFilter, System.StringComparison.OrdinalIgnoreCase)))
                {
                    return c;
                }
            }
        }
        foreach (T c in comps)
        {
            if (c.name.Equals(name, System.StringComparison.OrdinalIgnoreCase)) return c;
        }
        return null;
    }
}
