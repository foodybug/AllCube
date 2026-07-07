using UnityEditor;
using UnityEngine;
using System.IO;

public class BuildScript
{
    public static void PerformAndroidBuild()
    {
        Debug.Log("[BuildScript] Starting Android Build...");

        // 안드로이드 패키지 명(Application Identifier) 및 버전 코드 자동 세팅
        PlayerSettings.SetApplicationIdentifier(BuildTargetGroup.Android, "com.foodybug.allcube");
        PlayerSettings.bundleVersion = "1.0.0";
        PlayerSettings.defaultInterfaceOrientation = UIOrientation.Portrait;
        // 안드로이드 호환성 API 레벨 설정 (최신 빌드 안정화: Min API 25 / Target API 34)
        PlayerSettings.Android.minSdkVersion = AndroidSdkVersions.AndroidApiLevel25;
        PlayerSettings.Android.targetSdkVersion = AndroidSdkVersions.AndroidApiLevel34;
        PlayerSettings.Android.bundleVersionCode = 1;

        // 스크립팅 백엔드 IL2CPP 전환 및 CPU 아키텍처 ARMv7 + ARM64 멀티 빌드 지원 (최신 64비트 단말 설치용)
        PlayerSettings.SetScriptingBackend(BuildTargetGroup.Android, ScriptingImplementation.IL2CPP);
        PlayerSettings.Android.targetArchitectures = AndroidArchitecture.ARMv7 | AndroidArchitecture.ARM64;

        
        // 빌드 대상 씬 목록 가져오기
        EditorBuildSettingsScene[] scenes = EditorBuildSettings.scenes;
        string[] scenePaths = new string[scenes.Length];
        for (int i = 0; i < scenes.Length; i++)
        {
            scenePaths[i] = scenes[i].path;
            Debug.Log($"[BuildScript] Scene: {scenePaths[i]}");
        }

        // 빌드 타겟 폴더 생성 ( Builds/AllCube.apk )
        string buildDirectory = "Builds";
        if (!Directory.Exists(buildDirectory))
        {
            Directory.CreateDirectory(buildDirectory);
        }
        string buildPath = Path.Combine(buildDirectory, "AllCube.apk");

        // 빌드 옵션 구성 (테스트 실기기 설치 호환성을 위해 개발용 디버그 빌드로 지정)
        BuildPlayerOptions buildPlayerOptions = new BuildPlayerOptions();
        buildPlayerOptions.scenes = scenePaths;
        buildPlayerOptions.locationPathName = buildPath;
        buildPlayerOptions.target = BuildTarget.Android;
        buildPlayerOptions.options = BuildOptions.Development;

        // 안드로이드 빌드 파이프라인 기동
        BuildPipeline.BuildPlayer(buildPlayerOptions);
        Debug.Log("[BuildScript] Android Build Completed successfully!");
    }
}
