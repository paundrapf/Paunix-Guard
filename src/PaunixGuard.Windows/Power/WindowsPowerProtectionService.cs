using System.Runtime.InteropServices;
using PaunixGuard.Core.GuardState;
using PaunixGuard.Core.Settings;

namespace PaunixGuard.Windows.Power;

public sealed class WindowsPowerProtectionService : IPowerProtectionService
{
    private IntPtr requestHandle;

    public Task EnableAsync(GuardSettings settings, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

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
            if (requestHandle == IntPtr.Zero)
            {
                return Task.CompletedTask;
            }

            if (settings.KeepSystemAwakeWhileArmed)
            {
                PowerSetRequest(requestHandle, PowerRequestType.PowerRequestSystemRequired);
            }

            if (settings.KeepDisplayAwakeWhileArmed)
            {
                PowerSetRequest(requestHandle, PowerRequestType.PowerRequestDisplayRequired);
            }

            PowerSetRequest(requestHandle, PowerRequestType.PowerRequestExecutionRequired);
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

        if (requestHandle != IntPtr.Zero)
        {
            PowerClearRequest(requestHandle, PowerRequestType.PowerRequestSystemRequired);
            PowerClearRequest(requestHandle, PowerRequestType.PowerRequestDisplayRequired);
            PowerClearRequest(requestHandle, PowerRequestType.PowerRequestExecutionRequired);
            CloseHandle(requestHandle);
            requestHandle = IntPtr.Zero;
        }

        return Task.CompletedTask;
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
