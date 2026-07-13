using UnityEngine;
using System.Collections;

public class MyAudioSource : MonoBehaviour
{
	public enum eAudioType
	{
		eAudioType_Invalid = 0,
		eAudioType_Bgm,
		eAudioType_Eff,
		eAudioType_Max
	}
	
	private eAudioType m_eAudioType = eAudioType.eAudioType_Invalid;
	private GameObject m_goAudio = null;
	
	void Start()
	{
	}
	
	void Update()
	{
		if( null != m_goAudio)
		{
			if( eAudioType.eAudioType_Eff == m_eAudioType)
			{
				if( false == m_goAudio.GetComponent<AudioSource>().isPlaying)
					Util.MyDestroy( m_goAudio);
			}
		}
	}
	
	public void Init(GameObject goAudio, eAudioType type)
	{
		m_goAudio = goAudio;
		m_eAudioType = type;
	}
}
