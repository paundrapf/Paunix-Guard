using System.Runtime.InteropServices;

namespace PaunixGuard.Windows.Input;

public sealed class KeyboardInterceptor : IDisposable
{
    private const int WhKeyboardLl = 13;
    private const int WhMouseLl = 14;
    private const int WmKeydown = 0x0100;
    private const int WmSyskeydown = 0x0104;
    private const int WmKeyup = 0x0101;
    private const int WmSyskeyup = 0x0105;
    private const int WmRbuttondown = 0x0204;
    private const int WmRbuttonup = 0x0205;
    private const int WmNcrbuttondown = 0x00A4;
    private const int WmNcrbuttonup = 0x00A5;
    private const int WmXbuttondown = 0x020B;
    private const int WmXbuttonup = 0x020C;
    private const int WmNcxbuttondown = 0x00AB;
    private const int WmNcxbuttonup = 0x00AC;

    private const int VkLwin = 0x5B;
    private const int VkRwin = 0x5C;
    private const int VkMenu = 0x12;
    private const int VkLmenu = 0xA4;
    private const int VkRmenu = 0xA5;
    private const int VkControl = 0x11;
    private const int VkLcontrol = 0xA2;
    private const int VkRcontrol = 0xA3;
    private const int VkEscape = 0x1B;
    private const int VkTab = 0x09;
    private const int VkF4 = 0x73;
    private const int VkSpace = 0x20;
    private const int VkApps = 0x5D;
    private const int VkDelete = 0x2E;
    private const int VkLeft = 0x25;
    private const int VkRight = 0x27;
    private const int VkUp = 0x26;
    private const int VkDown = 0x28;
    private const int LlkhfAltdown = 0x20;

    private LowLevelKeyboardProc? kbProc;
    private LowLevelMouseProc? mouseProc;
    private IntPtr kbHookHandle;
    private IntPtr mouseHookHandle;
    private volatile bool isArmed;
    private volatile bool altDown;
    private volatile bool ctrlDown;
    private volatile bool winDown;

    public void SetArmed(bool armed)
    {
        isArmed = armed;
        if (armed)
        {
            InstallHooks();
        }
        else
        {
            UninstallHooks();
        }
    }

    private void InstallHooks()
    {
        if (kbHookHandle != IntPtr.Zero && mouseHookHandle != IntPtr.Zero)
        {
            return;
        }

        var hMod = GetModuleHandle(null);

        if (kbHookHandle == IntPtr.Zero)
        {
            kbProc = KeyboardHookProc;
            kbHookHandle = SetWindowsHookEx(WhKeyboardLl, kbProc, hMod, 0);
            if (kbHookHandle == IntPtr.Zero)
            {
                LogHookError("KB_LL", Marshal.GetLastWin32Error());
            }
        }

        if (mouseHookHandle == IntPtr.Zero)
        {
            mouseProc = MouseHookProc;
            mouseHookHandle = SetWindowsHookEx(WhMouseLl, mouseProc, hMod, 0);
            if (mouseHookHandle == IntPtr.Zero)
            {
                LogHookError("MS_LL", Marshal.GetLastWin32Error());
            }
        }
    }

    private void UninstallHooks()
    {
        if (kbHookHandle != IntPtr.Zero)
        {
            UnhookWindowsHookEx(kbHookHandle);
            kbHookHandle = IntPtr.Zero;
            kbProc = null;
        }

        if (mouseHookHandle != IntPtr.Zero)
        {
            UnhookWindowsHookEx(mouseHookHandle);
            mouseHookHandle = IntPtr.Zero;
            mouseProc = null;
        }

        altDown = false;
        ctrlDown = false;
        winDown = false;
    }

    public void Dispose()
    {
        isArmed = false;
        UninstallHooks();
    }

    private IntPtr KeyboardHookProc(int nCode, IntPtr wParam, IntPtr lParam)
    {
        try
        {
            if (nCode < 0 || !isArmed)
            {
                return CallNextHookEx(IntPtr.Zero, nCode, wParam, lParam);
            }

            var msg = wParam.ToInt32();
            if (msg == WmKeyup || msg == WmSyskeyup)
            {
                TrackModifierUp(lParam);
                return CallNextHookEx(IntPtr.Zero, nCode, wParam, lParam);
            }

            if (msg != WmKeydown && msg != WmSyskeydown)
            {
                return CallNextHookEx(IntPtr.Zero, nCode, wParam, lParam);
            }

            var kb = Marshal.PtrToStructure<KbdllHookStruct>(lParam);

            altDown = altDown || (kb.flags & LlkhfAltdown) != 0;
            TrackModifierDown(kb.vkCode);

            if (ShouldBlockKey(kb.vkCode))
            {
                return new IntPtr(1);
            }
        }
        catch
        {
            return new IntPtr(1);
        }

        return CallNextHookEx(IntPtr.Zero, nCode, wParam, lParam);
    }

    private IntPtr MouseHookProc(int nCode, IntPtr wParam, IntPtr lParam)
    {
        try
        {
            if (nCode < 0 || !isArmed)
            {
                return CallNextHookEx(IntPtr.Zero, nCode, wParam, lParam);
            }

            var msg = wParam.ToInt32();
            if (msg is WmRbuttondown or WmRbuttonup
                or WmNcrbuttondown or WmNcrbuttonup
                or WmXbuttondown or WmXbuttonup
                or WmNcxbuttondown or WmNcxbuttonup)
            {
                return new IntPtr(1);
            }
        }
        catch
        {
            return new IntPtr(1);
        }

        return CallNextHookEx(IntPtr.Zero, nCode, wParam, lParam);
    }

    private void TrackModifierDown(int vkCode)
    {
        if (vkCode is VkMenu or VkLmenu or VkRmenu) altDown = true;
        if (vkCode is VkControl or VkLcontrol or VkRcontrol) ctrlDown = true;
        if (vkCode is VkLwin or VkRwin) winDown = true;
    }

    private void TrackModifierUp(IntPtr lParam)
    {
        var kb = Marshal.PtrToStructure<KbdllHookStruct>(lParam);
        if (kb.vkCode is VkMenu or VkLmenu or VkRmenu) altDown = false;
        if (kb.vkCode is VkControl or VkLcontrol or VkRcontrol) ctrlDown = false;
        if (kb.vkCode is VkLwin or VkRwin) winDown = false;
    }

    private bool ShouldBlockKey(int vkCode)
    {
        if (vkCode is VkLwin or VkRwin)
        {
            return true;
        }

        if (winDown)
        {
            return true;
        }

        if (altDown)
        {
            if (vkCode is VkTab or VkF4 or VkSpace or VkEscape
                or VkLeft or VkRight or VkUp or VkDown)
            {
                return true;
            }
        }

        if (ctrlDown && vkCode == VkEscape)
        {
            return true;
        }

        if (ctrlDown && altDown && vkCode == VkDelete)
        {
            return true;
        }

        if (vkCode == VkApps)
        {
            return true;
        }

        return false;
    }

    private static void LogHookError(string hookName, int errorCode)
    {
        try
        {
            var logDir = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "PaunixGuard");
            Directory.CreateDirectory(logDir);

            var line = $"{DateTimeOffset.UtcNow:O} [Hook] {hookName} failed with error code {errorCode}";
            File.AppendAllText(Path.Combine(logDir, "error.log"), line + Environment.NewLine);
        }
        catch
        {
        }
    }

    [DllImport("kernel32.dll", CharSet = CharSet.Auto)]
    private static extern IntPtr GetModuleHandle(string? lpModuleName);

    [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
    private static extern IntPtr SetWindowsHookEx(int idHook, LowLevelKeyboardProc lpfn, IntPtr hMod, uint dwThreadId);

    [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
    private static extern IntPtr SetWindowsHookEx(int idHook, LowLevelMouseProc lpfn, IntPtr hMod, uint dwThreadId);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool UnhookWindowsHookEx(IntPtr hhk);

    [DllImport("user32.dll")]
    private static extern IntPtr CallNextHookEx(IntPtr hhk, int nCode, IntPtr wParam, IntPtr lParam);

    private delegate IntPtr LowLevelKeyboardProc(int nCode, IntPtr wParam, IntPtr lParam);
    private delegate IntPtr LowLevelMouseProc(int nCode, IntPtr wParam, IntPtr lParam);

    [StructLayout(LayoutKind.Sequential)]
    private struct KbdllHookStruct
    {
        public int vkCode;
        public int scanCode;
        public int flags;
        public int time;
        public long dwExtraInfo;
    }
}
