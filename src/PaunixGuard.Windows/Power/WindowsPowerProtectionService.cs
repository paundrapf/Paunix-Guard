using System.Runtime.InteropServices;
using PaunixGuard.Core.GuardState;
using PaunixGuard.Core.Settings;

namespace PaunixGuard.Windows.Power;

public sealed class WindowsPowerProtectionService : IPowerProtectionService
{
    public Task EnableAsync(GuardSettings settings, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (!settings.KeepSystemAwakeWhileArmed && !settings.KeepDisplayAwakeWhileArmed)
        {
            return Task.CompletedTask;
        }

        var state = ExecutionState.EsContinuous;
        if (settings.KeepSystemAwakeWhileArmed)
        {
            state |= ExecutionState.EsSystemRequired;
        }

        if (settings.KeepDisplayAwakeWhileArmed)
        {
            state |= ExecutionState.EsDisplayRequired;
        }

        SetThreadExecutionState(state);
        return Task.CompletedTask;
    }

    public Task DisableAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        SetThreadExecutionState(ExecutionState.EsContinuous);
        return Task.CompletedTask;
    }

    [DllImport("kernel32.dll")]
    private static extern ExecutionState SetThreadExecutionState(ExecutionState esFlags);

    [Flags]
    private enum ExecutionState : uint
    {
        EsContinuous = 0x80000000,
        EsSystemRequired = 0x00000001,
        EsDisplayRequired = 0x00000002
    }
}

