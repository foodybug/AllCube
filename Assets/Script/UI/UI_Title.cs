using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UI_Title : MonoBehaviour
{
    [Header("UI Component Assigns")]
    public UnityEngine.UI.RawImage texLogo;
    public UnityEngine.UI.Text textTouchScreen;

    [Header("Option Button Components")]
    public UnityEngine.UI.Button btnOption;

    private void Awake()
    {
        Screen.orientation = ScreenOrientation.Portrait;
        AutoAssignComponents();
    }

    public void AutoAssignComponents()
    {
        if (texLogo == null) texLogo = FindChildByName<UnityEngine.UI.RawImage>("texLogo");
        if (textTouchScreen == null) textTouchScreen = FindChildByName<UnityEngine.UI.Text>("textTouchScreen");

        if (texLogo != null) texLogo.raycastTarget = false;
        if (textTouchScreen != null) textTouchScreen.raycastTarget = false;

        if (btnOption == null) btnOption = FindChildByName<UnityEngine.UI.Button>("btnOption");
        if (btnOption == null) btnOption = FindChildByName<UnityEngine.UI.Button>("btnSetting");

        if (btnOption == null)
        {
            EnsureOptionButtonCreated();
        }

        if (btnOption != null)
        {
            btnOption.transform.SetAsLastSibling(); // 다른 UI 레이어보다 최상단에 노출되도록 설정

            UnityEngine.UI.RawImage rawImg = btnOption.GetComponent<UnityEngine.UI.RawImage>();
            if (rawImg != null)
            {
                rawImg.raycastTarget = true;
                Texture optionTex = Resources.Load("UI/btn_option") as Texture;
                if (optionTex != null) rawImg.texture = optionTex;
            }

            btnOption.onClick.RemoveAllListeners();
            btnOption.onClick.AddListener(() =>
            {
                Debug.Log("[UI_Title] Option button clicked!");
                if (AudioManager.Instance != null) AudioManager.Instance.Play("Sound/ui_button_down");

                TitleOptionPopup popup = TitleOptionPopup.Instance;
                if (popup == null) popup = FindFirstObjectByType<TitleOptionPopup>();

                if (popup == null)
                {
                    Canvas canvas = FindFirstObjectByType<Canvas>();
                    if (canvas != null)
                    {
                        popup = canvas.GetComponent<TitleOptionPopup>();
                        if (popup == null) popup = canvas.gameObject.AddComponent<TitleOptionPopup>();
                    }
                }

                if (popup != null)
                {
                    if (popup.IsShowing())
                    {
                        popup.Hide();
                    }
                    else
                    {
                        popup.Show();
                    }
                }
            });
        }

        TitleOptionPopup popup = GetComponent<TitleOptionPopup>();
        if (popup == null) popup = gameObject.AddComponent<TitleOptionPopup>();
    }

    private void EnsureOptionButtonCreated()
    {
        Canvas canvas = GetComponentInParent<Canvas>();
        if (canvas == null) canvas = FindFirstObjectByType<Canvas>();
        Transform parentTransform = canvas != null ? canvas.transform : transform;

        if (canvas != null && canvas.GetComponent<UnityEngine.UI.GraphicRaycaster>() == null)
        {
            canvas.gameObject.AddComponent<UnityEngine.UI.GraphicRaycaster>();
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

        UnityEngine.UI.RawImage rawImg = btnGo.AddComponent<UnityEngine.UI.RawImage>();
        rawImg.raycastTarget = true;
        rawImg.rectTransform.anchorMin = new Vector2(1, 1);
        rawImg.rectTransform.anchorMax = new Vector2(1, 1);
        rawImg.rectTransform.pivot = new Vector2(1, 1);
        rawImg.rectTransform.anchoredPosition = new Vector2(-40, -40);
        rawImg.rectTransform.sizeDelta = new Vector2(80, 80);

        Texture iconTex = Resources.Load("UI/btn_option") as Texture;
        if (iconTex == null) iconTex = Resources.Load("UI/sound_on") as Texture;

        rawImg.texture = iconTex;
        rawImg.color = Color.white;

        btnOption = btnGo.AddComponent<UnityEngine.UI.Button>();
        btnOption.targetGraphic = rawImg;
    }

    private T FindChildByName<T>(string name) where T : Component
    {
        T comp = GetComponentInChildren<T>(true);
        if (comp != null && comp.name.Equals(name, System.StringComparison.OrdinalIgnoreCase))
        {
            return comp;
        }

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
