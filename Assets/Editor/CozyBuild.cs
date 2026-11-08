using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

/// Player builds. Menu: Cozy Horror ▸ Build ...   Headless: -executeMethod CozyBuild.Windows / CozyBuild.WebGL
/// Output folder comes from the COZY_BUILD env var, else Builds/<Platform> (Builds/ is git-ignored).
public static class CozyBuild
{
    static readonly string[] Scenes = { "Assets/Scenes/Game.unity" };

    [MenuItem("Cozy Horror/Build/Windows (Development)")]
    public static void WindowsDev() => Build(BuildTarget.StandaloneWindows64, "Builds/Windows/CozyHorror.exe", BuildOptions.Development);

    [MenuItem("Cozy Horror/Build/Windows")]
    public static void Windows() => Build(BuildTarget.StandaloneWindows64, "Builds/Windows/CozyHorror.exe", BuildOptions.None);

    [MenuItem("Cozy Horror/Build/WebGL (itch.io)")]
    public static void WebGL() => Build(BuildTarget.WebGL, "Builds/WebGL", BuildOptions.None);

    static void Build(BuildTarget target, string defaultPath, BuildOptions options)
    {
        string path = System.Environment.GetEnvironmentVariable("COZY_BUILD") ?? defaultPath;
        var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
        {
            scenes = Scenes,
            locationPathName = path,
            target = target,
            options = options,
        });
        Debug.Log($"[Cozy] Build {target}: {report.summary.result}, {report.summary.totalSize / (1024 * 1024)} MB, {report.summary.totalErrors} errors -> {path}");
        if (Application.isBatchMode && report.summary.result != BuildResult.Succeeded) EditorApplication.Exit(1);
    }
}
