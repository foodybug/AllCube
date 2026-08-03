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

	private float m_bgmVolume = 1.0f;
	private float m_sfxVolume = 1.0f;

	public float BgmVolume
	{
		get { return m_bgmVolume; }
		set
		{
			m_bgmVolume = Mathf.Clamp01(value);
			PlayerPrefs.SetFloat("BgmVolume", m_bgmVolume);
			PlayerPrefs.Save();
			if (m_goBgm != null)
			{
				AudioSource src = m_goBgm.GetComponent<AudioSource>();
				if (src != null) src.volume = m_bgmVolume;
			}
		}
	}

	public float SfxVolume
	{
		get { return m_sfxVolume; }
		set
		{
			m_sfxVolume = Mathf.Clamp01(value);
			PlayerPrefs.SetFloat("SfxVolume", m_sfxVolume);
			PlayerPrefs.Save();
		}
	}

	void Awake()
	{
		m_instance = this;
		m_bgmVolume = PlayerPrefs.GetFloat("BgmVolume", 1.0f);
		m_sfxVolume = PlayerPrefs.GetFloat("SfxVolume", 1.0f);
	}
	
	void Start()
	{
		GameObject goAudioSource = Resources.Load( "Sound/AudioSource") as GameObject;
		m_goBgm = GameObject.Instantiate( goAudioSource, Vector3.zero, Quaternion.identity) as GameObject;
		AudioSource bgmSource = m_goBgm.GetComponent<AudioSource>();
		if (bgmSource != null)
		{
			bgmSource.loop = true;
			bgmSource.volume = m_bgmVolume;
		}
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
		if( 0 == MainManager.Instance.nSoundEnable)
			return;

		if( null == m_goBgm)
			return;

		AudioClip clip = Resources.Load( strPath) as AudioClip;
		
		if( null == clip)
		{
			Debug.LogError( "AudioManager::PlayBgm(), null == clip: " + strPath);
			return;
		}

		AudioSource src = m_goBgm.GetComponent<AudioSource>();
		if (src != null)
		{
			src.clip = clip;
			src.volume = m_bgmVolume;
			src.Play();
		}
	}

	public void StopBgm()
	{
		if( null != m_goBgm)
			m_goBgm.GetComponent<AudioSource>().Stop();
	}
	
	public void Play(string strPath, float fVolume = 1.0f, float fPitch = 1.0f)
	{
		if( 0 == MainManager.Instance.nSoundEnable)
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
		AudioSource audioSource = go.GetComponent<AudioSource>();
		if (audioSource != null)
		{
			audioSource.clip = clip;
			audioSource.volume = fVolume * m_sfxVolume;
			audioSource.pitch = fPitch;
			audioSource.Play();
		}

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
