using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UI_Title : MonoBehaviour
{
    [Header("UI Component Assigns")]
    public UnityEngine.UI.RawImage texLogo;
    public UnityEngine.UI.Text textTouchScreen;
    public GameObject goBtnSound;

    [Header("Sound Button Sub-Components")]
    public UnityEngine.UI.Button btnSound;
    public UnityEngine.UI.RawImage texSound;

    private void Awake()
    {
        Screen.orientation = ScreenOrientation.Portrait;
        AutoAssignComponents();
    }

    public void AutoAssignComponents()
    {
        if (texLogo == null) texLogo = FindChildByName<UnityEngine.UI.RawImage>("texLogo");
        if (textTouchScreen == null) textTouchScreen = FindChildByName<UnityEngine.UI.Text>("textTouchScreen");
        if (goBtnSound == null)
        {
            var btnObj = FindChildByName<UnityEngine.UI.Button>("goBtnSound");
            if (btnObj != null) goBtnSound = btnObj.gameObject;
        }

        if (goBtnSound != null)
        {
            if (btnSound == null) btnSound = goBtnSound.GetComponent<UnityEngine.UI.Button>();
            if (texSound == null) texSound = goBtnSound.GetComponent<UnityEngine.UI.RawImage>();
        }
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
