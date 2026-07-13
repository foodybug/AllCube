using UnityEngine;
using System.Collections;

public class CubeBreak : MonoBehaviour
{
	public GameObject goCube;
	public int nLife = 3;

	void Awake()
	{
		goCube = gameObject;
	}

	void Start()
	{
		Renderer rend = GetComponent<Renderer>();
		if (rend != null && MapManager.Instance != null)
		{
			rend.sharedMaterial = MapManager.Instance.GetSharedMaterial(0);
		}
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
