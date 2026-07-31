using System.Runtime.InteropServices;

namespace Arcana.App;

/// <summary>
/// Bridges stdout/stderr to the parent console for GUI-subsystem (WinExe) apps,
/// so Serilog's console sink shows output when the app is launched from a
/// terminal. Serilog's console sink writes through Console.OpenStandardOutput(),
/// which resolves the (newly attached) console handle, so no Console.SetOut is
/// needed. No-op when stdout is already redirected (pipe) or no parent console
/// exists (double-click launch). Never throws.
/// </summary>
internal static class ConsoleAttach
{
    private const int AttachParentProcess = -1;
    private const int StdOutputHandle = -11;

    [DllImport("kernel32.dll")]
    private static extern bool AttachConsole(int dwProcessId);

    [DllImport("kernel32.dll")]
    private static extern IntPtr GetStdHandle(int nStdHandle);

    /// <summary>Last bridge state, for logging after Serilog is configured.</summary>
    public static BridgeState LastState { get; private set; } = new(false, false);

    /// <summary>
    /// Call before configuring Serilog so the console sink has a real stdout to
    /// write to. Console.IsOutputRedirected can't be used to decide: for a
    /// GUI-subsystem (WinExe) app with no console it reports "redirected" even
    /// though the stdout handle is invalid, so we key on the handle itself.
    ///   - stdout valid (pipe/file)  → skip attach, Serilog already writes there.
    ///   - stdout invalid + console  → AttachConsole to the parent terminal.
    ///   - stdout invalid + no console (double-click) → attach fails, file log only.
    /// Never throws.
    /// </summary>
    public static BridgeState AttachParentConsole()
    {
        try
        {
            var hOut = GetStdHandle(StdOutputHandle);
            if (hOut != IntPtr.Zero)
                return LastState = new BridgeState(true, false);
            return LastState = new BridgeState(false, AttachConsole(AttachParentProcess));
        }
        catch (Exception)
        {
            return LastState = new BridgeState(false, false);
        }
    }

    public readonly record struct BridgeState(bool Redirected, bool Attached);
}
