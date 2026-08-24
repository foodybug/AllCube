using System;
using System.Collections;
using GoogleMobileAds.Api;
using UnityEngine;

public class AdmobManager : MonoBehaviour
{
    private static AdmobManager m_instance;
    public static AdmobManager Instance
    {
        get { return m_instance; }
    }

    [Header("AdMob Settings")]
    [Tooltip("Google AdMob 배너 광고 단위 ID (Ad Unit ID)")]
    [SerializeField] private string m_bannerAdUnitId = "ca-app-pub-3940256099942544/6300978111"; // 구글 테스트 배너 ID

    private BannerView bannerView;
    private bool m_isSdkInitialized = false;
    private bool m_isShowing = false;

    void Awake()
    {
        if (m_instance == null)
        {
            m_instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else if (m_instance != this)
        {
            Destroy(gameObject);
            return;
        }
    }

    void Start()
    {
        if (m_instance != this) return;
        InitializeSDK();
    }

    private void InitializeSDK()
    {
        try
        {
            // 최신 Google Mobile Ads SDK 비동기 초기화
            MobileAds.Initialize(initStatus =>
            {
                Debug.Log("[AdmobManager] Google Mobile Ads SDK Initialized.");
                m_isSdkInitialized = true;
                InitBanner();
            });
        }
        catch (Exception ex)
        {
            Debug.LogWarning("[AdmobManager] MobileAds.Initialize Exception: " + ex.Message);
        }
    }

    public void InitBanner()
    {
        if (m_instance != this) return;

        // 기존 배너가 이미 존재할 경우 완전 파괴하여 중복 겹침 노출 차단
        if (bannerView != null)
        {
            try
            {
                bannerView.Destroy();
                bannerView = null;
            }
            catch (Exception ex)
            {
                Debug.LogWarning("[AdmobManager] Destroy old banner Exception: " + ex.Message);
            }
        }

        try
        {
            // 배너 뷰 생성 (AdSize.Banner: 320x50 표준 배너)
            bannerView = new BannerView(m_bannerAdUnitId, AdSize.Banner, AdPosition.Bottom);
            RegisterBannerEvents(bannerView);

            // 광고 요청 생성 및 로드
            AdRequest request = new AdRequest();
            bannerView.LoadAd(request);

            m_isShowing = true;
            Debug.Log("[AdmobManager] Single Banner View created and load requested successfully.");
        }
        catch (Exception ex)
        {
            Debug.LogWarning("[AdmobManager] InitBanner Exception: " + ex.Message);
        }
    }

    private void RegisterBannerEvents(BannerView banner)
    {
        if (banner == null) return;

        banner.OnBannerAdLoaded += () =>
        {
            Debug.Log("[AdmobManager] Banner Ad Loaded Successfully.");
            m_isShowing = true;
        };

        banner.OnBannerAdLoadFailed += (LoadAdError error) =>
        {
            Debug.LogWarning("[AdmobManager] Banner Ad Load Failed: " + (error != null ? error.GetMessage() : "Unknown Error"));
        };
    }

    public void Show()
    {
        m_isShowing = true;
        try
        {
            if (bannerView == null)
            {
                InitBanner();
            }

            bannerView?.Show();
        }
        catch (Exception ex)
        {
            Debug.LogWarning("[AdmobManager] Show Exception: " + ex.Message);
        }
    }

    public void Hide()
    {
        m_isShowing = false;
        try
        {
            bannerView?.Hide();
        }
        catch (Exception ex)
        {
            Debug.LogWarning("[AdmobManager] Hide Exception: " + ex.Message);
        }
    }

    private void OnDestroy()
    {
        if (bannerView != null)
        {
            try
            {
                bannerView.Destroy();
                bannerView = null;
            }
            catch (Exception ex)
            {
                Debug.LogWarning("[AdmobManager] OnDestroy Exception: " + ex.Message);
            }
        }
    }

#if UNITY_EDITOR
    private void OnGUI()
    {
        if (!m_isShowing) return;

        float bannerWidth = 320f;
        float bannerHeight = 50f;
        float xPos = (Screen.width - bannerWidth) / 2f;
        float yPos = Screen.height - bannerHeight - 10f;

        GUI.Box(new Rect(xPos, yPos, bannerWidth, bannerHeight), "Google AdMob Banner (Unity Editor Preview)");
    }
#endif
}
