using System.Runtime.InteropServices;
using PaunixGuard.Core.Settings;
using PaunixGuard.Core.Triggers;

namespace PaunixGuard.Windows.Session;

public sealed class WindowsSystemEventRouter
{
    private const int WmPowerBroadcast = 0x0218;
    private const int WmQueryEndSession = 0x0011;
    private const int WmEndSession = 0x0016;
    private const int PbtApmSuspend = 0x0004;
    private const int PbtApmQuerySuspend = 0x0000;
    private const int PbtPowerSettingChange = 0x8013;

    private static readonly Guid GuidLidSwitchStateChange = new("BA3E0F4D-B817-4094-A2D1-D56379E6A0F3");

    private bool isArmed;
    private bool blockShutdown;
    private IntPtr mainWindowHandle;
    private IntPtr lidCloseNotificationHandle;

    public event EventHandler<TriggerSignal>? Triggered;

    public void AttachWindow(IntPtr windowHandle)
    {
        mainWindowHandle = windowHandle;
        UpdateShutdownBlockReason();
        RegisterLidCloseNotification();
    }

    public void Configure(bool armed, GuardSettings settings)
    {
        isArmed = armed;
        blockShutdown = settings.BlockShutdownWhileArmed;
        UpdateShutdownBlockReason();
    }

    public IntPtr HandleWindowMessage(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
    {
        if (!isArmed)
        {
            return IntPtr.Zero;
        }

        if (msg == WmPowerBroadcast)
        {
            var code = wParam.ToInt32();

            if (code is PbtApmSuspend or PbtApmQuerySuspend)
            {
                Raise(TriggerType.SleepAttempt, "Windows sleep/suspend was requested while guard mode was armed.");
                handled = code == PbtApmQuerySuspend;
                return new IntPtr(handled ? 0 : 1);
            }

            if (code == PbtPowerSettingChange)
            {
                HandleLidSwitchNotification(lParam);
            }
        }

        if (msg == WmQueryEndSession)
        {
            Raise(TriggerType.ShutdownAttempt, "Windows shutdown, restart, or logoff was requested while guard mode was armed.");
            if (blockShutdown)
            {
                handled = true;
                return IntPtr.Zero;
            }
        }

        if (msg == WmEndSession && wParam != IntPtr.Zero)
        {
            Raise(TriggerType.ShutdownAttempt, "Windows session is ending while guard mode was armed.");
        }

        return IntPtr.Zero;
    }

    public void Clear()
    {
        isArmed = false;
        blockShutdown = false;

        UnregisterLidCloseNotification();

        if (mainWindowHandle != IntPtr.Zero)
        {
            ShutdownBlockReasonDestroy(mainWindowHandle);
        }
    }

    private void RegisterLidCloseNotification()
    {
        if (mainWindowHandle == IntPtr.Zero || lidCloseNotificationHandle != IntPtr.Zero)
        {
            return;
        }

        var guid = GuidLidSwitchStateChange;
        lidCloseNotificationHandle = RegisterPowerSettingNotification(
            mainWindowHandle,
            ref guid,
            0);
    }

    private void UnregisterLidCloseNotification()
    {
        if (lidCloseNotificationHandle == IntPtr.Zero)
        {
            return;
        }

        UnregisterPowerSettingNotification(lidCloseNotificationHandle);
        lidCloseNotificationHandle = IntPtr.Zero;
    }

    private void HandleLidSwitchNotification(IntPtr lParam)
    {
        try
        {
            var setting = Marshal.PtrToStructure<PowerBroadcastSetting>(lParam);

            if (setting.PowerSetting != GuidLidSwitchStateChange || setting.DataLength < 4)
            {
                return;
            }

            var dataPtr = IntPtr.Add(lParam, Marshal.SizeOf<PowerBroadcastSetting>());
            var lidState = Marshal.ReadInt32(dataPtr);

            if (lidState == 0)
            {
                Raise(TriggerType.LidClosed, "Laptop lid was closed while guard mode was armed.");
            }
        }
        catch
        {
        }
    }

    private void Raise(TriggerType type, string reason)
    {
        Triggered?.Invoke(this, TriggerSignal.Create(type, reason, "WindowsSystemEventRouter", DateTimeOffset.UtcNow));
    }

    private void UpdateShutdownBlockReason()
    {
        if (mainWindowHandle == IntPtr.Zero)
        {
            return;
        }

        ShutdownBlockReasonDestroy(mainWindowHandle);

        if (isArmed && blockShutdown)
        {
            ShutdownBlockReasonCreate(
                mainWindowHandle,
                "Paunix Guard is armed. Disarm with your PIN before shutting down.");
        }
    }

    [DllImport("user32.dll", CharSet = CharSet.Unicode)]
    private static extern bool ShutdownBlockReasonCreate(IntPtr hWnd, string reason);

    [DllImport("user32.dll")]
    private static extern bool ShutdownBlockReasonDestroy(IntPtr hWnd);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern IntPtr RegisterPowerSettingNotification(IntPtr hRecipient, ref Guid PowerSettingGuid, int Flags);

    [DllImport("user32.dll")]
    private static extern bool UnregisterPowerSettingNotification(IntPtr Handle);

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    private struct PowerBroadcastSetting
    {
        public Guid PowerSetting;
        public uint DataLength;
    }
}

