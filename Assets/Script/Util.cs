using UnityEngine;
using System.Collections;
#if UNITY_EDITOR
using UnityEditor;
#endif

public class Util : MonoBehaviour
{
	static public void MyDestroy(Object o)
	{
#if UNITY_EDITOR
		//DestroyImmediate( o);
		Destroy( o);
#else
		Destroy( o);
#endif
	}

	static public void Quit()
	{
#if UNITY_EDITOR
		EditorApplication.isPlaying = false;
		EditorApplication.isPaused = false;
#else
	#if UNITY_ANDROID
		/*
		// dispose
		System.Diagnostics.ProcessThreadCollection ptc = System.Diagnostics.Process.GetCurrentProcess().Threads;
		foreach( System.Diagnostics.ProcessThread pt in ptc)
		{
			pt.Dispose();
		}
		System.Diagnostics.Process.GetCurrentProcess().Kill();
		*/
	#endif

		Application.Quit();
#endif
	}
}
