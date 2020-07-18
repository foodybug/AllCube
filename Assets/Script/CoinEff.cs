using UnityEngine;
using System.Collections;

public class CoinEff : MonoBehaviour
{
	ParticleSystem particle;

	void Start()
	{
		particle = gameObject.GetComponent<ParticleSystem>();
	}
	
	void Update()
	{
		if( null != particle && false == particle.isPlaying)
			Util.MyDestroy( gameObject);
	}
}
