using UnityEditor;
using UnityEngine;
using System.IO;

public class BuildScript
{
    [MenuItem("Build/Build Google Play AAB (App Bundle Release)", false, 0)]
    [MenuItem("Tools/Build Google Play AAB (App Bundle Release)", false, 0)]
    public static void BuildAndroidAABRelease()
    {
        EditorUserBuildSettings.buildAppBundle = true;
        PerformAndroidBuild(BuildOptions.None, isAAB: true);
    }

    [MenuItem("Build/Build Android APK (Development Debug)", false, 1)]
    [MenuItem("Tools/Build Android APK (Development Debug)", false, 1)]
    public static void BuildAndroidAPKDev()
    {
        EditorUserBuildSettings.buildAppBundle = false;
        PerformAndroidBuild(BuildOptions.Development | BuildOptions.AllowDebugging | BuildOptions.ConnectWithProfiler, isAAB: false);
    }

    [MenuItem("Build/Build & Run Android APK (Debug)", false, 2)]
    [MenuItem("Tools/Build & Run Android APK (Debug)", false, 2)]
    public static void BuildAndRunAndroidAPKDev()
    {
        EditorUserBuildSettings.buildAppBundle = false;
        PerformAndroidBuild(BuildOptions.Development | BuildOptions.AllowDebugging | BuildOptions.AutoRunPlayer, isAAB: false);
    }

    [MenuItem("Build/Build Android APK (Release)", false, 3)]
    [MenuItem("Tools/Build Android APK (Release)", false, 3)]
    public static void BuildAndroidAPKRelease()
    {
        EditorUserBuildSettings.buildAppBundle = false;
        PerformAndroidBuild(BuildOptions.None, isAAB: false);
    }

    public static void PerformAndroidBuild(BuildOptions buildOptions, bool isAAB = false)
    {
        Debug.Log($"[BuildScript] Starting Android Build (IsAAB: {isAAB}, Options: {buildOptions})...");

        // 안드로이드 타겟 플랫폼 스위치 보장
        if (EditorUserBuildSettings.activeBuildTarget != BuildTarget.Android)
        {
            Debug.Log("[BuildScript] Switching active build target to Android...");
            EditorUserBuildSettings.SwitchActiveBuildTarget(BuildTargetGroup.Android, BuildTarget.Android);
        }

        // 안드로이드 패키지 명(Application Identifier) 및 버전 코드 자동 세팅
        PlayerSettings.SetApplicationIdentifier(BuildTargetGroup.Android, "com.foodybug.allcube");
        PlayerSettings.bundleVersion = "1.0.0";
        PlayerSettings.defaultInterfaceOrientation = UIOrientation.Portrait;
        // 안드로이드 호환성 API 레벨 설정 (구글 최신 정책: Min API 25 / Target API 35)
        PlayerSettings.Android.minSdkVersion = AndroidSdkVersions.AndroidApiLevel25;
        PlayerSettings.Android.targetSdkVersion = (AndroidSdkVersions)35; // API Level 35 (Android 15) 최신 구글 플레이 정책 대응
        PlayerSettings.Android.bundleVersionCode = 3; // 구글 플레이 신규 업로드 버전 코드 (+1 증가)

        // 스크립팅 백엔드 IL2CPP 전환 및 CPU 아키텍처 ARM64 빌드 (LLVM out of memory 방지 및 구글 최신 64비트 단말 표준)
        PlayerSettings.SetScriptingBackend(BuildTargetGroup.Android, ScriptingImplementation.IL2CPP);
        PlayerSettings.Android.targetArchitectures = AndroidArchitecture.ARM64;

        // 빌드 대상 씬 목록 가져오기
        EditorBuildSettingsScene[] scenes = EditorBuildSettings.scenes;
        if (scenes == null || scenes.Length == 0)
        {
            Debug.LogError("[BuildScript] No scenes found in EditorBuildSettings!");
            EditorUtility.DisplayDialog("Android Build Error", "빌드 대상 씬 목록이 비어있습니다.\nBuild Settings에서 씬을 추가해주세요.", "확인");
            return;
        }

        string[] scenePaths = new string[scenes.Length];
        for (int i = 0; i < scenes.Length; i++)
        {
            scenePaths[i] = scenes[i].path;
            Debug.Log($"[BuildScript] Scene [{i}]: {scenePaths[i]}");
        }

        // 빌드 타겟 폴더 생성 ( Builds/AllCube.apk )
        string buildDirectory = "Builds";
        if (!Directory.Exists(buildDirectory))
        {
            Directory.CreateDirectory(buildDirectory);
        }
        string buildFileName = isAAB ? "AllCube_Release.aab" : "AllCube.apk";
        string buildPath = Path.Combine(buildDirectory, buildFileName);
        string fullPath = Path.GetFullPath(buildPath);

        // 이전 빌드 파일이 남아있는 경우 타임스탬프 갱신 보장을 위해 준비
        if (File.Exists(buildPath))
        {
            try
            {
                File.Delete(buildPath);
            }
            catch (System.Exception ex)
            {
                Debug.LogWarning($"[BuildScript] Could not delete old APK before build: {ex.Message}");
            }
        }

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
            System.DateTime now = System.DateTime.Now;
            if (File.Exists(buildPath))
            {
                File.SetLastWriteTime(buildPath, now);
            }
            string timeStr = File.Exists(buildPath) ? File.GetLastWriteTime(buildPath).ToString("yyyy-MM-dd HH:mm:ss") : now.ToString("yyyy-MM-dd HH:mm:ss");

            Debug.Log($"[BuildScript] Android Build Completed successfully! Saved to: {fullPath} (Modified: {timeStr})");
            EditorUtility.RevealInFinder(buildPath);
            EditorUtility.DisplayDialog("Android Build", $"안드로이드 APK 빌드가 성공적으로 완료되었습니다!\n\n저장 위치: {fullPath}\n완료 시각: {timeStr}", "확인");
        }
        else
        {
            Debug.LogError($"[BuildScript] Android Build Failed! Result: {report.summary.result}");
            EditorUtility.DisplayDialog("Android Build Error", $"안드로이드 APK 빌드 중 오류가 발생했습니다.\n\n결과: {report.summary.result}\n\nUnity Editor의 Console 창에서 자세한 에러 로그를 확인하세요.", "확인");
        }
    }
}
