namespace PaunixGuard.Core.Updates;

public interface IUpdateService
{
    Task InitializeAsync(CancellationToken cancellationToken);

    Task<UpdateCheckResult> CheckAsync(string channel, CancellationToken cancellationToken);

    Task DownloadAndApplyAsync(CancellationToken cancellationToken);
}

