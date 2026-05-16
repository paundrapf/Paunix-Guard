namespace PaunixGuard.Core.Updates;

public sealed record UpdateCheckResult(
    bool IsAvailable,
    string? Version,
    string? ReleaseNotes,
    string? ErrorMessage)
{
    public static UpdateCheckResult None() => new(false, null, null, null);

    public static UpdateCheckResult Failed(string errorMessage) => new(false, null, null, errorMessage);
}

