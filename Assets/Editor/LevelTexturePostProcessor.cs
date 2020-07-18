using UnityEngine;
using UnityEditor;

public class LevelTexturePostProcessor : AssetPostprocessor
{
	void OnPreprocessTexture()
	{
		if( assetPath.Contains("Stage/"))
		{
			TextureImporter ti = (TextureImporter)assetImporter;
			ti.textureType = TextureImporterType.Advanced;
			ti.isReadable = true;
			ti.mipmapEnabled = false;
			ti.wrapMode = TextureWrapMode.Clamp;
			ti.filterMode = FilterMode.Point;
			ti.textureFormat = TextureImporterFormat.RGBA16;
		}
	}
}
