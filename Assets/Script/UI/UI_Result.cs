using UnityEngine;

public class UI_Result : MonoBehaviour
{
    [Header("UI Component Assigns")]
    public UnityEngine.UI.Button btnRetry;
    public UnityEngine.UI.RawImage texRetryBtnBg;
    public UnityEngine.UI.RawImage texRetry;
    public UnityEngine.UI.Text textRetry;
    public UnityEngine.UI.Text textResultTime;
    public UnityEngine.UI.RawImage texResultIcon;

    [Header("Title Transition Component")]
    public UnityEngine.UI.Button btnTitle;
    public UnityEngine.UI.Text textTitle;

    private void Awake()
    {
        AutoAssignComponents();
    }

    private void AutoAssignComponents()
    {
        if (btnRetry == null) btnRetry = FindChildByName<UnityEngine.UI.Button>("btnRetry");
        if (texRetryBtnBg == null) texRetryBtnBg = FindChildByName<UnityEngine.UI.RawImage>("texRetryBtnBg");
        if (textResultTime == null) textResultTime = FindChildByName<UnityEngine.UI.Text>("textResultTime");
        if (texResultIcon == null) texResultIcon = FindChildByName<UnityEngine.UI.RawImage>("texResultIcon");

        if (btnRetry != null)
        {
            if (textRetry == null) textRetry = btnRetry.GetComponentInChildren<UnityEngine.UI.Text>();
            if (texRetry == null) texRetry = btnRetry.GetComponentInChildren<UnityEngine.UI.RawImage>();
        }

        if (btnTitle == null) btnTitle = FindChildByName<UnityEngine.UI.Button>("btnTitle");
        if (btnTitle != null)
        {
            if (textTitle == null) textTitle = btnTitle.GetComponentInChildren<UnityEngine.UI.Text>();
        }
    }

    private T FindChildByName<T>(string name) where T : Component
    {
        // 1. 직계 자식 컴포넌트 우선 탐색
        T comp = GetComponentInChildren<T>(true);
        if (comp != null && comp.name.Equals(name, System.StringComparison.OrdinalIgnoreCase))
        {
            return comp;
        }

        // 2. 자식들 중 이름 매칭 검색
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
            foreach (T child in children)
            {
                if (child.name.ToLower().Contains(name.ToLower()))
                {
                    return child;
                }
            }
        }

        // 3. Fallback: 씬 내 모든 활성화/비활성화 오브젝트 탐색
        T[] all = Resources.FindObjectsOfTypeAll<T>();
        if (all != null)
        {
            foreach (T item in all)
            {
                if (item != null && item.gameObject != null && item.gameObject.scene.isLoaded && item.name != null)
                {
                    if (item.name.Equals(name, System.StringComparison.OrdinalIgnoreCase))
                    {
                        return item;
                    }
                }
            }
            foreach (T item in all)
            {
                if (item != null && item.gameObject != null && item.gameObject.scene.isLoaded && item.name != null)
                {
                    if (item.name.ToLower().Contains(name.ToLower()))
                    {
                        return item;
                    }
                }
            }
        }

        return null;
    }
}
