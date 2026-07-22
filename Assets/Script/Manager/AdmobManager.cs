using UnityEngine;
using System.Collections;
using GoogleMobileAds.Api;

public class AdmobManager : MonoBehaviour
{
	private static AdmobManager m_instance;
	public static AdmobManager Instance
	{
		get
		{
			if (m_instance == null)
			{
				GameObject go = new GameObject("AdmobManager");
				m_instance = go.AddComponent<AdmobManager>();
				DontDestroyOnLoad(go);
			}
			return m_instance;
		}
	}

	BannerView bannerView;

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
		InitBanner();
	}

	public void InitBanner()
	{
		if (bannerView != null) return;
		try
		{
			bannerView = new BannerView("ca-app-pub-3940256099942544/6300978111", AdSize.SmartBanner, AdPosition.Bottom);
			AdRequest request = new AdRequest.Builder().Build();
			bannerView.LoadAd(request);
		}
		catch (System.Exception ex)
		{
			Debug.LogWarning("[AdmobManager] Admob initialization exception caught safely: " + ex.Message);
		}
	}

	public void Show()
	{
		if (bannerView == null)
		{
			InitBanner();
		}

		if (bannerView != null)
		{
			try
			{
				bannerView.Show();
			}
			catch (System.Exception ex)
			{
				Debug.LogWarning("[AdmobManager] Show exception caught: " + ex.Message);
			}
		}
	}

	public void Hide()
	{
		if (bannerView != null)
		{
			try
			{
				bannerView.Hide();
			}
			catch (System.Exception ex)
			{
				Debug.LogWarning("[AdmobManager] Hide exception caught: " + ex.Message);
			}
		}
	}
}
