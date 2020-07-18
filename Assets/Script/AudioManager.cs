using UnityEngine;
using System;
using System.Collections;
using System.IO;

public class AudioManager : MonoBehaviour
{
	static AudioManager m_instance;
	public static AudioManager Instance{ get{ return m_instance;}}
	
	private GameObject m_goAudio = null;
	private GameObject m_goBgm = null;

	void Awake()
	{
		m_instance = this;
	}
	
	void Start()
	{
		GameObject goAudioSource = Resources.Load( "Sound/AudioSource") as GameObject;
		m_goBgm = GameObject.Instantiate( goAudioSource, Vector3.zero, Quaternion.identity) as GameObject;
		m_goBgm.GetComponent<AudioSource>().loop = true;
	}
	
	void Update()
	{
	}
	
	void OnApplicationQuit()
	{
		_DestroyAudio();
	}

	public void PlayBgm(string strPath)
	{
		if( 0 == GameMain.Instance.nSoundEnable)
			return;

		if( null == m_goBgm)
			return;

		AudioClip clip = Resources.Load( strPath) as AudioClip;
		
		if( null == clip)
		{
			Debug.LogError( "AudioManager::PlayBgm(), null == clip: " + strPath);
			return;
		}

		m_goBgm.GetComponent<AudioSource>().clip = clip;
		m_goBgm.GetComponent<AudioSource>().Play();
	}

	public void StopBgm()
	{
		if( null != m_goBgm)
			m_goBgm.GetComponent<AudioSource>().Stop();
	}
	
	public void Play(string strPath, float fVolume = 1.0f)
	{
		if( 0 == GameMain.Instance.nSoundEnable)
			return;

		AudioClip clip = Resources.Load( strPath) as AudioClip;
		
		if( null == clip)
		{
			Debug.LogError( "AudioManager::Play(), null == clip: " + strPath);
			return;
		}
		
		GameObject goAudioSource = Resources.Load( "Sound/AudioSource") as GameObject;
		GameObject go = GameObject.Instantiate( goAudioSource, Vector3.zero, Quaternion.identity) as GameObject;
		//go.transform.parent = this.gameObject.transform;
		go.GetComponent<AudioSource>().clip = clip;
		go.GetComponent<AudioSource>().volume = fVolume;
		go.GetComponent<AudioSource>().Play();

		MyAudioSource myAudioSource = go.GetComponentInChildren<MyAudioSource>();
		if( null != myAudioSource)
			myAudioSource.Init( go, MyAudioSource.eAudioType.eAudioType_Eff);
	}

	private void _DestroyAudio()
	{
		if( null != m_goAudio)
			Util.MyDestroy( m_goAudio);

		if( null != m_goBgm)
			Util.MyDestroy( m_goBgm);
	}
}
