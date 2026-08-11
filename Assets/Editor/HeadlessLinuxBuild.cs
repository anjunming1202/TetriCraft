using System;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

// Automated headless Linux build of the IPC training scene, for the cluster/WSL spawn-mode path.
// Requires the "Linux Build Support (Mono)" module for this Editor version.
//
// Invoke from the command line (from the project root):
//   "…/Unity.exe" -batchmode -quit -projectPath . -buildTarget Linux64 \
//       -executeMethod HeadlessLinuxBuild.Build -logFile build.log
//
// Optional overrides (append after the args above):
//   -scene  Assets/AgenticTetricraft/HeadlessIpcEnvironment.unity
//   -output Builds/LinuxHeadless/TetricraftHeadless.x86_64
//
// The produced player is driven exactly like the Editor server, e.g.:
//   ./TetricraftHeadless.x86_64 -batchmode -nographics -port 9876 -logFile unity.log
// (HeadlessIpcServer reads -port; on client disconnect it Application.Quit()s, which in a
// real player cleanly exits the process — ideal for spawn mode, one process per env.)
public static class HeadlessLinuxBuild
{
    const string DefaultScene = "Assets/AgenticTetricraft/HeadlessIpcEnvironment.unity";
    const string DefaultOutput = "Builds/LinuxHeadless/TetricraftHeadless.x86_64";

    public static void Build()
    {
        string scene = GetArg("-scene") ?? DefaultScene;
        string output = GetArg("-output") ?? DefaultOutput;

        var opts = new BuildPlayerOptions
        {
            scenes = new[] { scene },
            locationPathName = output,
            target = BuildTarget.StandaloneLinux64,
            options = BuildOptions.None,
        };

        // Note: building a normal player (not StandaloneBuildSubtarget.Server) — most compatible
        // with `-batchmode -nographics`. If the scene proves to have no render dependencies and you
        // want a smaller/graphics-stripped binary, set:
        //   EditorUserBuildSettings.standaloneBuildSubtarget = StandaloneBuildSubtarget.Server;

        BuildReport report = BuildPipeline.BuildPlayer(opts);
        BuildSummary s = report.summary;
        Debug.Log($"[HeadlessLinuxBuild] result={s.result} size={s.totalSize} " +
                  $"errors={s.totalErrors} scene={scene} -> {output}");

        if (s.result != BuildResult.Succeeded)
            EditorApplication.Exit(1);
    }

    // Builds the CURRENTLY ACTIVE build target + subtarget, inheriting whatever was configured in
    // Build Profiles (e.g. Linux Server / ARM64 / IL2CPP). Use this for the ARM64 Dedicated Server
    // cluster build — it avoids needing a scripted architecture API (Unity 6 only exposes ARM64 via
    // the UI), by building exactly the active configuration:
    //   "…/6000.x/Editor/Unity.exe" -batchmode -quit -projectPath . \
    //       -executeMethod HeadlessLinuxBuild.BuildActive -logFile build_arm64.log
    // IMPORTANT: do NOT pass -buildTarget — that would reset the active target/subtarget/architecture.
    // Optional -scene / -output overrides work as with Build().
    public static void BuildActive()
    {
        string scene = GetArg("-scene") ?? DefaultScene;
        string output = GetArg("-output") ?? "Builds/LinuxServerArm64/TetricraftHeadless.aarch64";

        var opts = new BuildPlayerOptions
        {
            scenes = new[] { scene },
            locationPathName = output,
            target = EditorUserBuildSettings.activeBuildTarget,                    // e.g. StandaloneLinux64
            subtarget = (int)EditorUserBuildSettings.standaloneBuildSubtarget,     // Server, as set in the UI
            options = BuildOptions.None,
        };

        // NOTE: ARM64 Linux ships ONLY IL2CPP player variations (linuxarm64_server_*_il2cpp). The
        // Scripting Backend must be IL2CPP (set in Player Settings) before building, else the build
        // fails looking for the nonexistent linuxarm64_server_*_mono variation.
        Debug.Log($"[HeadlessLinuxBuild] BuildActive: target={opts.target} " +
                  $"subtarget={(StandaloneBuildSubtarget)opts.subtarget} scene={scene} -> {output}");

        BuildReport report = BuildPipeline.BuildPlayer(opts);
        BuildSummary s = report.summary;
        Debug.Log($"[HeadlessLinuxBuild] result={s.result} size={s.totalSize} errors={s.totalErrors} " +
                  $"target={opts.target} subtarget={(StandaloneBuildSubtarget)opts.subtarget} -> {output}");

        if (s.result != BuildResult.Succeeded)
            EditorApplication.Exit(1);
    }

    static string GetArg(string name)
    {
        string[] args = Environment.GetCommandLineArgs();
        for (int i = 0; i < args.Length - 1; i++)
            if (args[i] == name) return args[i + 1];
        return null;
    }
}
