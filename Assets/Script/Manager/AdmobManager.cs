using UnityEngine;
using System.Collections;
using GoogleMobileAds.Api;

public class AdmobManager : MonoBehaviour
{
	static AdmobManager m_instance;
	public static AdmobManager Instance { get { return m_instance; } }
	BannerView bannerView;

	void Awake()
	{
		m_instance = this;
	}

	void Start()
	{
		try
		{
			bannerView = new BannerView("ca-app-pub-3940256099942544/6300978111", AdSize.SmartBanner, AdPosition.Bottom);
			AdRequest request = new AdRequest.Builder().Build();
			bannerView.LoadAd(request);
			bannerView.Hide();
		}
		catch (System.Exception ex)
		{
			Debug.LogWarning("[AdmobManager] Admob initialization exception caught safely: " + ex.Message);
		}
	}

	void Update()
	{
	}

	public void Show()
	{
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
}
