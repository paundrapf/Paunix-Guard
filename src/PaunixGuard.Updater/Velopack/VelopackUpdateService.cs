using PaunixGuard.Core.Updates;
using Velopack;
using Velopack.Sources;

namespace PaunixGuard.Updater.Velopack;

public sealed class VelopackUpdateService : IUpdateService
{
    private const string DefaultRepositoryUrl = "https://github.com/paundrapf/Paunix-Guard";
    private UpdateManager? updateManager;
    private UpdateInfo? pendingUpdate;

    public Task InitializeAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        VelopackApp.Build().Run();
        return Task.CompletedTask;
    }

    public async Task<UpdateCheckResult> CheckAsync(string channel, CancellationToken cancellationToken)
    {
        try
        {
            updateManager ??= CreateUpdateManager(channel);
            pendingUpdate = await updateManager.CheckForUpdatesAsync();

            if (pendingUpdate is null)
            {
                return UpdateCheckResult.None();
            }

            return new UpdateCheckResult(
                true,
                pendingUpdate.TargetFullRelease.Version.ToString(),
                pendingUpdate.TargetFullRelease.NotesMarkdown,
                null);
        }
        catch (Exception ex)
        {
            return UpdateCheckResult.Failed(ex.Message);
        }
    }

    public async Task DownloadAndApplyAsync(CancellationToken cancellationToken)
    {
        if (updateManager is null || pendingUpdate is null)
        {
            return;
        }

        await updateManager.DownloadUpdatesAsync(pendingUpdate);
        updateManager.ApplyUpdatesAndRestart(pendingUpdate);
    }

    private static UpdateManager CreateUpdateManager(string channel)
    {
        var source = new GithubSource(
            DefaultRepositoryUrl,
            accessToken: null,
            prerelease: channel.Equals("beta", StringComparison.OrdinalIgnoreCase),
            downloader: null);

        return new UpdateManager(source, options: null, locator: null);
    }
}

