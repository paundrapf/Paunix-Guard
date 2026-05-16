using PaunixGuard.Core.Events;
using PaunixGuard.Core.Triggers;

namespace PaunixGuard.Core.GuardState;

public sealed class GuardStateChangedEventArgs(
    GuardState previousState,
    GuardState currentState,
    TriggerSignal? signal,
    GuardEvent? guardEvent)
    : EventArgs
{
    public GuardState PreviousState { get; } = previousState;

    public GuardState CurrentState { get; } = currentState;

    public TriggerSignal? Signal { get; } = signal;

    public GuardEvent? GuardEvent { get; } = guardEvent;
}

