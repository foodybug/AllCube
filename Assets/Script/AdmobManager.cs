using UnityEngine;
using System.Collections;
using GoogleMobileAds.Api;

public class AdmobManager : MonoBehaviour
{
	static AdmobManager m_instance;
	public static AdmobManager Instance{ get{ return m_instance;}}
	BannerView bannerView;
	
	void Awake()
	{
		m_instance = this;
	}
	
	void Start()
	{
		//AdSize adSize = new AdSize( 360, 50);
		bannerView = new BannerView( "ca-app-pub-2192399648152961/8991012335", AdSize.SmartBanner, AdPosition.Bottom);
		AdRequest request = new AdRequest.Builder().Build();
		bannerView.LoadAd( request);
		bannerView.Hide();
	}

	void Update()
	{
	}

	public void Show()
	{
		bannerView.Show();
	}
}
