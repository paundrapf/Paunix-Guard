namespace PaunixGuard.Core.Alarm;

public sealed record AudioStateSnapshot(
    float MasterVolume,
    bool IsMuted,
    string? OutputDeviceId,
    DateTimeOffset CapturedAt);

