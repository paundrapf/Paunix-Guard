using System.Runtime.InteropServices;
using PaunixGuard.Core.GuardState;
using PaunixGuard.Core.Settings;

namespace PaunixGuard.Windows.Power;

public sealed class WindowsPowerProtectionService : IPowerProtectionService
{
    private static readonly IntPtr InvalidHandleValue = new(-1);
    private IntPtr requestHandle;

    public Task EnableAsync(GuardSettings settings, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (requestHandle != IntPtr.Zero && requestHandle != InvalidHandleValue)
        {
            return Task.CompletedTask;
        }

        if (!settings.KeepSystemAwakeWhileArmed && !settings.KeepDisplayAwakeWhileArmed)
        {
            return Task.CompletedTask;
        }

        var context = new PowerRequestContext
        {
            Version = 0,
            Flags = 1,
            SimpleReasonString = Marshal.StringToHGlobalUni("Paunix Guard is protecting this laptop")
        };

        try
        {
            requestHandle = PowerCreateRequest(ref context);
            if (requestHandle == IntPtr.Zero || requestHandle == InvalidHandleValue)
            {
                LogNativeError("PowerCreateRequest", Marshal.GetLastWin32Error());
                requestHandle = IntPtr.Zero;
                return Task.CompletedTask;
            }

            if (settings.KeepSystemAwakeWhileArmed)
            {
                TrySetRequest(PowerRequestType.PowerRequestSystemRequired);
            }

            if (settings.KeepDisplayAwakeWhileArmed)
            {
                TrySetRequest(PowerRequestType.PowerRequestDisplayRequired);
            }

            TrySetRequest(PowerRequestType.PowerRequestExecutionRequired);
        }
        finally
        {
            Marshal.FreeHGlobal(context.SimpleReasonString);
        }

        return Task.CompletedTask;
    }

    public Task DisableAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (requestHandle != IntPtr.Zero && requestHandle != InvalidHandleValue)
        {
            PowerClearRequest(requestHandle, PowerRequestType.PowerRequestSystemRequired);
            PowerClearRequest(requestHandle, PowerRequestType.PowerRequestDisplayRequired);
            PowerClearRequest(requestHandle, PowerRequestType.PowerRequestExecutionRequired);
            CloseHandle(requestHandle);
            requestHandle = IntPtr.Zero;
        }

        return Task.CompletedTask;
    }

    private void TrySetRequest(PowerRequestType requestType)
    {
        if (!PowerSetRequest(requestHandle, requestType))
        {
            LogNativeError($"PowerSetRequest:{requestType}", Marshal.GetLastWin32Error());
        }
    }

    private static void LogNativeError(string operation, int errorCode)
    {
        try
        {
            var logDir = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "PaunixGuard");
            Directory.CreateDirectory(logDir);

            var line = $"{DateTimeOffset.UtcNow:O} [Power] {operation} failed with error code {errorCode}";
            File.AppendAllText(Path.Combine(logDir, "error.log"), line + Environment.NewLine);
        }
        catch
        {
        }
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct PowerRequestContext
    {
        public uint Version;
        public uint Flags;
        public IntPtr SimpleReasonString;
    }

    private enum PowerRequestType
    {
        PowerRequestDisplayRequired = 0,
        PowerRequestSystemRequired = 1,
        PowerRequestAwayModeRequired = 2,
        PowerRequestExecutionRequired = 3
    }

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern IntPtr PowerCreateRequest(ref PowerRequestContext Context);

    [DllImport("kernel32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool PowerSetRequest(IntPtr PowerRequest, PowerRequestType RequestType);

    [DllImport("kernel32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool PowerClearRequest(IntPtr PowerRequest, PowerRequestType RequestType);

    [DllImport("kernel32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool CloseHandle(IntPtr hObject);
}
