using PaunixGuard.Core.Updates;
using Velopack;
using Velopack.Sources;

namespace PaunixGuard.Updater.Velopack;

public sealed class VelopackUpdateService : IUpdateService
{
    private const string DefaultRepositoryUrl = "https://github.com/paundrapf/Paunix-Guard";
    private static int startupHooksRun;
    private UpdateManager? updateManager;
    private string? updateManagerChannel;
    private UpdateInfo? pendingUpdate;

    public static void RunVelopackStartupHooks()
    {
        if (Interlocked.Exchange(ref startupHooksRun, 1) == 1)
        {
            return;
        }

        VelopackApp.Build()
            .SetAutoApplyOnStartup(false)
            .Run();
    }

    public static void MarkStartupHooksRun()
    {
        Interlocked.Exchange(ref startupHooksRun, 1);
    }

    public Task InitializeAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        RunVelopackStartupHooks();
        return Task.CompletedTask;
    }

    public async Task<UpdateCheckResult> CheckAsync(string channel, CancellationToken cancellationToken)
    {
        try
        {
            cancellationToken.ThrowIfCancellationRequested();
            updateManager = GetUpdateManager(channel);
            if (!updateManager.IsInstalled)
            {
                pendingUpdate = null;
                return UpdateCheckResult.Failed(
                    "Auto-update requires the Paunix Guard installer. Standalone EXE builds can be updated by downloading the latest release manually.");
            }

            if (updateManager.UpdatePendingRestart is not null)
            {
                pendingUpdate = null;
                return new UpdateCheckResult(
                    true,
                    updateManager.UpdatePendingRestart.Version.ToString(),
                    "An update is already downloaded and ready to apply.",
                    null);
            }

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
        cancellationToken.ThrowIfCancellationRequested();

        if (updateManager is null)
        {
            return;
        }

        if (!updateManager.IsInstalled)
        {
            throw new InvalidOperationException("Auto-update requires the Paunix Guard installer.");
        }

        var updateToApply = updateManager.UpdatePendingRestart;
        if (pendingUpdate is not null)
        {
            await updateManager.DownloadUpdatesAsync(pendingUpdate, progress: null, cancellationToken);
            updateToApply = pendingUpdate.TargetFullRelease;
        }

        if (updateToApply is not null)
        {
            updateManager.ApplyUpdatesAndRestart(updateToApply);
        }
    }

    private UpdateManager GetUpdateManager(string channel)
    {
        var resolvedChannel = ResolveVelopackChannel(channel);
        if (updateManager is not null
            && string.Equals(updateManagerChannel, resolvedChannel, StringComparison.OrdinalIgnoreCase))
        {
            return updateManager;
        }

        updateManagerChannel = resolvedChannel;
        pendingUpdate = null;
        return CreateUpdateManager(resolvedChannel);
    }

    private static UpdateManager CreateUpdateManager(string channel)
    {
        var source = new GithubSource(
            DefaultRepositoryUrl,
            accessToken: null,
            prerelease: channel.Equals("win-beta", StringComparison.OrdinalIgnoreCase),
            downloader: null);

        var options = new UpdateOptions
        {
            ExplicitChannel = channel
        };

        return new UpdateManager(source, options, locator: null);
    }

    private static string ResolveVelopackChannel(string channel)
    {
        return channel.Equals("beta", StringComparison.OrdinalIgnoreCase)
            ? "win-beta"
            : "win";
    }
}
