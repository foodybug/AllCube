using UnityEngine;
using UnityEngine.UI;

public class TitleOptionPopup : MonoBehaviour
{
    [Header("Popup UI Components")]
    public GameObject popupRoot;
    public Slider sliderBgm;
    public Slider sliderSfx;
    public Text textBgmValue;
    public Text textSfxValue;
    public Button btnQuitGame;
    public Button btnClose;

    private static float m_lastCloseTime = -10f;
    public static TitleOptionPopup Instance { get; private set; }

    public static bool IsPopupJustClosed()
    {
        return (Time.unscaledTime - m_lastCloseTime) < 0.35f;
    }

    public static bool IsAnyOptionPopupShowing()
    {
        if (Instance != null && Instance.IsShowing()) return true;

        GameObject root = GameObject.Find("TitleOptionPopup(Clone)");
        if (root != null && root.activeInHierarchy) return true;

        GameObject overlay = GameObject.Find("OptionPopupOverlay");
        if (overlay != null && overlay.activeInHierarchy) return true;

        return false;
    }

    private void Awake()
    {
        Instance = this;
        AutoBindComponents();
        // AdjustScaleFromRootCanvas();
    }

    private void Start()
    {
        AutoBindComponents();
        // AdjustScaleFromRootCanvas();
    }

    public void AdjustScaleFromRootCanvas()
    {
        Canvas rootCanvas = GetComponentInParent<Canvas>();
        if (rootCanvas == null) rootCanvas = FindFirstObjectByType<Canvas>();

        if (rootCanvas == null)
        {
            Debug.LogWarning("[CanvasScale Log] rootCanvas is NULL in AdjustScaleFromRootCanvas!");
            return;
        }

        Vector3 canvasObjScale = rootCanvas.transform.localScale;
        float scaleFactor = rootCanvas.scaleFactor;

        Transform panel = transform.Find("OptionPanel");
        if (panel == null && popupRoot != null) panel = popupRoot.transform.Find("OptionPanel");
        Transform targetTransform = panel != null ? panel : transform;

        Debug.Log(string.Format("[CanvasScale Log] Canvas Name: '{0}', GameObject localScale: {1}, scaleFactor: {2}, Target: '{3}'",
            rootCanvas.gameObject.name, canvasObjScale, scaleFactor, targetTransform.gameObject.name));

        // Canvas 오브젝트 자체의 transform.localScale 직접 반영
        targetTransform.localScale = canvasObjScale;

        Debug.Log(string.Format("[CanvasScale Log] Applied final localScale to '{0}': {1}",
            targetTransform.gameObject.name, targetTransform.localScale));
    }

    public void AutoBindComponents()
    {
        if (popupRoot == null) popupRoot = gameObject;

        if (btnQuitGame == null) btnQuitGame = FindChildByName<Button>("Btn_QuitGame");
        if (btnQuitGame == null) btnQuitGame = FindChildByName<Button>("Btn_Quit");

        if (btnClose == null) btnClose = FindChildByName<Button>("Btn_Close");
        if (btnClose == null) btnClose = FindChildByName<Button>("Close");

        if (sliderBgm == null) sliderBgm = FindChildByName<Slider>("Slider", "Row_BGMVolume");
        if (sliderBgm == null) sliderBgm = FindChildByName<Slider>("Slider");

        if (sliderSfx == null) sliderSfx = FindChildByName<Slider>("Slider", "Row_SFXVolume");

        if (textBgmValue == null) textBgmValue = FindChildByName<Text>("Text_Value", "Row_BGMVolume");
        if (textSfxValue == null) textSfxValue = FindChildByName<Text>("Text_Value", "Row_SFXVolume");

        // Ensure Pretendard font and raycastTarget settings on all text components
        Font pretendardFont = GlobalFontManager.PretendardFont;
        Text[] allTexts = GetComponentsInChildren<Text>(true);
        foreach (Text t in allTexts)
        {
            if (pretendardFont != null) t.font = pretendardFont;
            t.raycastTarget = false;
        }

        Button overlayBtn = popupRoot.GetComponent<Button>();
        if (overlayBtn != null)
        {
            Destroy(overlayBtn);
        }

        if (btnQuitGame != null)
        {
            btnQuitGame.onClick.RemoveAllListeners();
            btnQuitGame.onClick.AddListener(OnQuitGameClicked);
        }

        if (btnClose != null)
        {
            btnClose.onClick.RemoveAllListeners();
            btnClose.onClick.AddListener(OnCloseClicked);
        }

        if (sliderBgm != null)
        {
            sliderBgm.onValueChanged.RemoveAllListeners();
            sliderBgm.onValueChanged.AddListener(OnBgmSliderChanged);
        }

        if (sliderSfx != null)
        {
            sliderSfx.onValueChanged.RemoveAllListeners();
            sliderSfx.onValueChanged.AddListener(OnSfxSliderChanged);
        }
    }

    public void Show()
    {
        AutoBindComponents();
        // AdjustScaleFromRootCanvas();

        GameObject targetRoot = popupRoot != null ? popupRoot : gameObject;
        targetRoot.transform.SetAsLastSibling();

        targetRoot.SetActive(true);
        RefreshValues();
    }

    public void Hide()
    {
        m_lastCloseTime = Time.unscaledTime;
        GameObject targetRoot = popupRoot != null ? popupRoot : gameObject;
        targetRoot.SetActive(false);
    }

    public void Toggle()
    {
        if (IsShowing()) Hide();
        else Show();
    }

    public bool IsShowing()
    {
        GameObject targetRoot = popupRoot != null ? popupRoot : gameObject;
        return targetRoot != null && targetRoot.activeInHierarchy;
    }

    public void RefreshValues()
    {
        float bgmVal = PlayerPrefs.GetFloat("BgmVolume", 1.0f);
        float sfxVal = PlayerPrefs.GetFloat("SfxVolume", 1.0f);

        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.BgmVolume = bgmVal;
            AudioManager.Instance.SfxVolume = sfxVal;
        }

        if (sliderBgm != null) sliderBgm.SetValueWithoutNotify(bgmVal);
        if (sliderSfx != null) sliderSfx.SetValueWithoutNotify(sfxVal);

        if (textBgmValue != null) textBgmValue.text = string.Format("{0}%", Mathf.RoundToInt(bgmVal * 100f));
        if (textSfxValue != null) textSfxValue.text = string.Format("{0}%", Mathf.RoundToInt(sfxVal * 100f));
    }

    private void OnBgmSliderChanged(float val)
    {
        val = Mathf.Clamp01(val);
        PlayerPrefs.SetFloat("BgmVolume", val);
        PlayerPrefs.Save();

        if (AudioManager.Instance != null) AudioManager.Instance.BgmVolume = val;
        if (textBgmValue != null) textBgmValue.text = string.Format("{0}%", Mathf.RoundToInt(val * 100f));
    }

    private void OnSfxSliderChanged(float val)
    {
        val = Mathf.Clamp01(val);
        PlayerPrefs.SetFloat("SfxVolume", val);
        PlayerPrefs.Save();

        if (AudioManager.Instance != null) AudioManager.Instance.SfxVolume = val;
        if (textSfxValue != null) textSfxValue.text = string.Format("{0}%", Mathf.RoundToInt(val * 100f));
    }

    private void OnQuitGameClicked()
    {
        if (AudioManager.Instance != null) AudioManager.Instance.Play("Sound/ui_button_down");
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

    private T FindChildByName<T>(string name, string parentNameFilter = null) where T : Component
    {
        T[] comps = GetComponentsInChildren<T>(true);
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

    private void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }
}
