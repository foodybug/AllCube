using UnityEngine;
using System.Collections;

public class CubeBreak : MonoBehaviour
{
	public GameObject goCube;
	public int nLife = 3;

	void Start()
	{
	}
	
	void Update()
	{
	}

	public int CollisionCube()
	{
		nLife--;

		if( 2 == nLife)
		{
			goCube.GetComponent<Renderer>().material.mainTexture = Resources.Load( "Cube/break1") as Texture;
			AudioManager.Instance.Play( "Sound/cube_break", 0.3f);
		}
		else if( 1 == nLife)
		{
			goCube.GetComponent<Renderer>().material.mainTexture = Resources.Load( "Cube/break2") as Texture;
			AudioManager.Instance.Play( "Sound/cube_break", 0.6f);
		}

		return nLife;
	}
}
