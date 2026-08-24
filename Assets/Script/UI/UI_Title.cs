using UnityEngine;
using UnityEngine.UI;

public class UI_Title : MonoBehaviour
{
    [Header("UI Component Assigns")]
    public RawImage texLogo;
    public Text textTouchScreen;

    [Header("Option Button & Popup Components")]
    public Button btnOption;
    public TitleOptionPopup optionPopup;

    private void Awake()
    {
        Screen.orientation = ScreenOrientation.Portrait;
        AutoAssignComponents();
    }

    private void OnEnable()
    {
        AutoAssignComponents();
        if (btnOption != null && btnOption.gameObject != null)
        {
            btnOption.gameObject.SetActive(true);
        }
    }

    public void AutoAssignComponents()
    {
        if (texLogo == null) texLogo = FindChildByName<RawImage>("texLogo");
        if (textTouchScreen == null) textTouchScreen = FindChildByName<Text>("textTouchScreen");

        if (texLogo != null) texLogo.raycastTarget = false;
        if (textTouchScreen != null) textTouchScreen.raycastTarget = false;

        if (btnOption == null) btnOption = FindChildByName<Button>("btnOption");
        if (btnOption == null) btnOption = FindChildByName<Button>("btnSetting");

        // 1. Ensure btnOption exists
        if (btnOption == null)
        {
            EnsureOptionButtonCreated();
        }

        if (btnOption != null)
        {
            btnOption.gameObject.SetActive(true);
            btnOption.transform.SetAsLastSibling();

            RectTransform rect = btnOption.transform as RectTransform;
            if (rect != null)
            {
                rect.anchorMin = new Vector2(1, 1);
                rect.anchorMax = new Vector2(1, 1);
                rect.pivot = new Vector2(1, 1);
                rect.anchoredPosition = new Vector2(-80, -100);
                rect.sizeDelta = new Vector2(80, 80);
            }

            RawImage rawImg = btnOption.GetComponent<RawImage>();
            if (rawImg != null)
            {
                rawImg.raycastTarget = true;
                Texture optionTex = Resources.Load("UI/btn_option") as Texture;
                if (optionTex != null) rawImg.texture = optionTex;
            }

            btnOption.onClick.RemoveAllListeners();
            btnOption.onClick.AddListener(OnBtnOptionClicked);
        }

        // 2. Ensure TitleOptionPopup exists on Title Scene Canvas
        if (optionPopup == null)
        {
            optionPopup = FindFirstObjectByType<TitleOptionPopup>();
        }

        Canvas titleCanvas = FindTitleCanvas();
        if (optionPopup == null)
        {
            GameObject prefabAsset = Resources.Load<GameObject>("UI/TitleOptionPopup");
            if (prefabAsset != null)
            {
                Transform parentT = titleCanvas != null ? titleCanvas.transform : transform;
                GameObject popupGo = Instantiate(prefabAsset, parentT, false);
                optionPopup = popupGo.GetComponent<TitleOptionPopup>();
                if (optionPopup == null) optionPopup = popupGo.AddComponent<TitleOptionPopup>();
            }
        }

        if (optionPopup != null)
        {
            if (titleCanvas != null && optionPopup.transform.parent != titleCanvas.transform)
            {
                optionPopup.transform.SetParent(titleCanvas.transform, false);
            }
            optionPopup.transform.SetAsLastSibling();
            optionPopup.Hide();
        }
    }

    public void OnBtnOptionClicked()
    {
        if (AudioManager.Instance != null) AudioManager.Instance.Play("Sound/ui_button_down");
        if (optionPopup != null)
        {
            optionPopup.Toggle();
        }
        else
        {
            AutoAssignComponents();
            if (optionPopup != null) optionPopup.Toggle();
        }
    }

    private Canvas FindTitleCanvas()
    {
        Canvas inParent = GetComponentInParent<Canvas>();
        if (inParent != null && !inParent.gameObject.name.Equals("TransitionCanvas", System.StringComparison.OrdinalIgnoreCase))
        {
            return inParent;
        }

        Canvas[] allCanvases = FindObjectsByType<Canvas>(FindObjectsSortMode.None);
        foreach (Canvas c in allCanvases)
        {
            if (c.gameObject.name.Equals("TransitionCanvas", System.StringComparison.OrdinalIgnoreCase)) continue;
            if (c.gameObject.scene.name == "DontDestroyOnLoad") continue;
            if (c.enabled && c.gameObject.activeInHierarchy) return c;
        }

        foreach (Canvas c in allCanvases)
        {
            if (!c.gameObject.name.Equals("TransitionCanvas", System.StringComparison.OrdinalIgnoreCase)) return c;
        }

        return null;
    }

    private void EnsureOptionButtonCreated()
    {
        Canvas canvas = FindTitleCanvas();
        Transform parentTransform = canvas != null ? canvas.transform : transform;

        if (canvas != null && canvas.GetComponent<GraphicRaycaster>() == null)
        {
            canvas.gameObject.AddComponent<GraphicRaycaster>();
        }

        if (FindFirstObjectByType<UnityEngine.EventSystems.EventSystem>() == null)
        {
            GameObject es = new GameObject("EventSystem");
            es.AddComponent<UnityEngine.EventSystems.EventSystem>();
            es.AddComponent<UnityEngine.EventSystems.StandaloneInputModule>();
        }

        GameObject btnGo = new GameObject("btnOption");
        btnGo.transform.SetParent(parentTransform, false);
        btnGo.transform.SetAsLastSibling();

        RawImage rawImg = btnGo.AddComponent<RawImage>();
        rawImg.raycastTarget = true;
        rawImg.rectTransform.anchorMin = new Vector2(1, 1);
        rawImg.rectTransform.anchorMax = new Vector2(1, 1);
        rawImg.rectTransform.pivot = new Vector2(1, 1);
        rawImg.rectTransform.anchoredPosition = new Vector2(-80, -100);
        rawImg.rectTransform.sizeDelta = new Vector2(80, 80);

        Texture iconTex = Resources.Load("UI/btn_option") as Texture;
        if (iconTex == null) iconTex = Resources.Load("UI/sound_on") as Texture;

        rawImg.texture = iconTex;
        rawImg.color = Color.white;

        btnOption = btnGo.AddComponent<Button>();
        btnOption.targetGraphic = rawImg;
    }

    private T FindChildByName<T>(string name) where T : Component
    {
        T[] children = GetComponentsInChildren<T>(true);
        if (children != null)
        {
            foreach (T child in children)
            {
                if (child.name.Equals(name, System.StringComparison.OrdinalIgnoreCase))
                {
                    return child;
                }
            }
        }
        return null;
    }
}
