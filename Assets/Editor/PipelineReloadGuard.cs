using System;
using System.IO;
using System.Reflection;
using UnityEditor;
using UnityEngine;

// Work around the experimental Pipeline server leaving listeners behind on assembly reload.
[InitializeOnLoad]
internal static class PipelineReloadGuard
{
    private const double CheckIntervalSeconds = 5;
    private static readonly Type ServerStartup = Type.GetType("Unity.Pipeline.Editor.PipelineServerStartup, Unity.Pipeline.Editor");
    private static double _nextCheck;

    static PipelineReloadGuard()
    {
        if (ServerStartup == null || AssetDatabase.IsAssetImportWorkerProcess())
            return;

        AssemblyReloadEvents.beforeAssemblyReload += StopBeforeReload;
        EditorApplication.update += RestoreMissingDescriptor;
    }

    private static void StopBeforeReload()
    {
        Invoke("StopServer");
    }

    private static void RestoreMissingDescriptor()
    {
        if (EditorApplication.timeSinceStartup < _nextCheck)
            return;

        _nextCheck = EditorApplication.timeSinceStartup + CheckIntervalSeconds;
        var descriptorPath = Path.Combine(Directory.GetParent(Application.dataPath).FullName, "Library", "Pipeline", ".unity-pipeline-port");
        if (File.Exists(descriptorPath))
            return;

        var server = ServerStartup.GetProperty("Server", BindingFlags.Public | BindingFlags.Static)?.GetValue(null);
        if (server == null)
            return; // Respect a server intentionally stopped through the Pipeline menu.

        var running = server.GetType().GetProperty("IsRunning", BindingFlags.Public | BindingFlags.Instance);
        if (running?.GetValue(server) is bool isRunning && isRunning)
        {
            Debug.LogWarning("Pipeline 연결 정보가 없어 서버를 다시 시작합니다.");
            Invoke("RestartServer");
        }
    }

    private static void Invoke(string methodName)
    {
        try
        {
            ServerStartup.GetMethod(methodName, BindingFlags.Public | BindingFlags.Static)?.Invoke(null, null);
        }
        catch (Exception exception)
        {
            Debug.LogWarning($"Pipeline 서버 {methodName} 실패: {exception.GetBaseException().Message}");
        }
    }
}
