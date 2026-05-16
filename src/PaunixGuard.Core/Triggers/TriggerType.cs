namespace PaunixGuard.Core.Triggers;

public enum TriggerType
{
    ChargerUnplugged = 0,
    InputActivity = 1,
    LidClosed = 2,
    SleepAttempt = 3,
    ShutdownAttempt = 4,
    ManualPanic = 5,
    PhoneDisconnected = 6,
    WebcamMotion = 7,
    WifiChanged = 8
}

