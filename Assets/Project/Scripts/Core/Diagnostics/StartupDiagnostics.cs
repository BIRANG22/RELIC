using System;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class StartupDiagnostics
{
    private const string Tag = "[Startup]";
    private static int sequence;
    private static bool exceptionHandlersRegistered;

    public static string LastCheckpoint { get; private set; } = "(none)";

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Initialize()
    {
        sequence = 0;
        LastCheckpoint = "(none)";
        RegisterExceptionHandlers();
        Write("BOOT", $"product={Application.productName} version={Application.version} buildGuid={Application.buildGUID} unity={Application.unityVersion} platform={Application.platform} scene={SceneManager.GetActiveScene().name}");
    }

    public static void Begin(string step) => Write("BEGIN", step);
    public static void Success(string step) => Write("OK", step);
    public static void Complete(string detail) => Write("STARTUP COMPLETE", detail);

    public static void Fail(string step, Exception exception)
    {
        WriteError("FAIL", $"{step} lastCheckpoint={LastCheckpoint} exception={exception}");
    }

    public static string FormatCheckpoint(int checkpointSequence, string status, string detail)
    {
        return $"{Tag} #{checkpointSequence:000} {status} {detail}";
    }

    private static void RegisterExceptionHandlers()
    {
        if (exceptionHandlersRegistered)
            return;

        exceptionHandlersRegistered = true;
        AppDomain.CurrentDomain.UnhandledException += OnUnhandledException;
        TaskScheduler.UnobservedTaskException += OnUnobservedTaskException;
    }

    private static void OnUnhandledException(object sender, UnhandledExceptionEventArgs args)
    {
        if (args.ExceptionObject is Exception exception)
            Fail("UnhandledException", exception);
    }

    private static void OnUnobservedTaskException(object sender, UnobservedTaskExceptionEventArgs args)
    {
        Fail("UnobservedTaskException", args.Exception);
    }

    private static void Write(string status, string detail)
    {
        string checkpoint = FormatCheckpoint(++sequence, status, detail);
        LastCheckpoint = checkpoint;
        Debug.Log(checkpoint);
    }

    private static void WriteError(string status, string detail)
    {
        string checkpoint = FormatCheckpoint(++sequence, status, detail);
        LastCheckpoint = checkpoint;
        Debug.LogError(checkpoint);
    }
}
