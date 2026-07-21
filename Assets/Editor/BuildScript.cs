using UnityEditor;
using UnityEngine;
using System.IO;

public class BuildScript
{
    [MenuItem("Build/Build Android APK (Development)", false, 1)]
    [MenuItem("Tools/Build Android APK (Development)", false, 1)]
    public static void BuildAndroidAPKDev()
    {
        PerformAndroidBuild(BuildOptions.Development);
    }

    [MenuItem("Build/Build Android APK (Release)", false, 2)]
    [MenuItem("Tools/Build Android APK (Release)", false, 2)]
    public static void BuildAndroidAPKRelease()
    {
        PerformAndroidBuild(BuildOptions.None);
    }

    public static void PerformAndroidBuild()
    {
        PerformAndroidBuild(BuildOptions.Development);
    }

    public static void PerformAndroidBuild(BuildOptions buildOptions)
    {
        Debug.Log($"[BuildScript] Starting Android Build ({buildOptions})...");

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
        string buildFileName = buildOptions.HasFlag(BuildOptions.Development) ? "AllCube_Dev.apk" : "AllCube.apk";
        string buildPath = Path.Combine(buildDirectory, buildFileName);

        // 빌드 옵션 구성
        BuildPlayerOptions buildPlayerOptions = new BuildPlayerOptions();
        buildPlayerOptions.scenes = scenePaths;
        buildPlayerOptions.locationPathName = buildPath;
        buildPlayerOptions.target = BuildTarget.Android;
        buildPlayerOptions.options = buildOptions;

        // 안드로이드 빌드 파이프라인 기동
        var report = BuildPipeline.BuildPlayer(buildPlayerOptions);
        if (report.summary.result == UnityEditor.Build.Reporting.BuildResult.Succeeded)
        {
            Debug.Log($"[BuildScript] Android Build Completed successfully! Saved to: {buildPath}");
            EditorUtility.RevealInFinder(buildPath);
            EditorUtility.DisplayDialog("Android Build", $"안드로이드 APK 빌드가 성공적으로 완료되었습니다!\n\n저장 위치: {buildPath}", "확인");
        }
        else
        {
            Debug.LogError($"[BuildScript] Android Build Failed! Result: {report.summary.result}");
            EditorUtility.DisplayDialog("Android Build Error", $"안드로이드 APK 빌드 중 오류가 발생했습니다.\n\n결과: {report.summary.result}", "확인");
        }
    }
}
