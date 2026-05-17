using PaunixGuard.Core.Alarm;
using PaunixGuard.Core.Events;
using PaunixGuard.Core.Settings;
using PaunixGuard.Core.Triggers;
using Xunit;

namespace PaunixGuard.App.Tests.Core;

public sealed class AlarmOrchestratorTests
{
    [Fact]
    public async Task StartAsync_StartsSound_WhenAudioPrepareFails()
    {
        var audio = new FakeAudioService { ThrowOnPrepare = true };
        var sound = new FakeSoundPlayer();
        var orchestrator = new AlarmOrchestrator(audio, sound);

        await orchestrator.StartAsync(CreateSignal(), new GuardSettings(), new GuardEvent(), CancellationToken.None);

        Assert.Equal(1, audio.PrepareCount);
        Assert.Equal(1, sound.StartCount);
    }

    [Fact]
    public async Task StopAsync_RestoresAudio_WhenSettingEnabled()
    {
        var audio = new FakeAudioService();
        var sound = new FakeSoundPlayer();
        var orchestrator = new AlarmOrchestrator(audio, sound);

        await orchestrator.StartAsync(CreateSignal(), new GuardSettings
        {
            ForceVolumeEnabled = true,
            RestoreAudioAfterDisarm = true
        }, new GuardEvent(), CancellationToken.None);
        await orchestrator.StopAsync(CancellationToken.None);

        Assert.Equal(1, audio.CaptureCount);
        Assert.Equal(1, audio.RestoreCount);
        Assert.Equal(1, sound.StopCount);
    }

    [Fact]
    public async Task StopAsync_DoesNotRestoreAudio_WhenSettingDisabled()
    {
        var audio = new FakeAudioService();
        var sound = new FakeSoundPlayer();
        var orchestrator = new AlarmOrchestrator(audio, sound);

        await orchestrator.StartAsync(CreateSignal(), new GuardSettings
        {
            ForceVolumeEnabled = true,
            RestoreAudioAfterDisarm = false
        }, new GuardEvent(), CancellationToken.None);
        await orchestrator.StopAsync(CancellationToken.None);

        Assert.Equal(0, audio.CaptureCount);
        Assert.Equal(1, audio.PrepareCount);
        Assert.Equal(0, audio.RestoreCount);
        Assert.Equal(1, sound.StopCount);
    }

    private static TriggerSignal CreateSignal()
    {
        return TriggerSignal.Create(
            TriggerType.ManualPanic,
            "test",
            "test",
            DateTimeOffset.UtcNow);
    }

    private sealed class FakeAudioService : IAudioService
    {
        public bool ThrowOnPrepare { get; init; }

        public int CaptureCount { get; private set; }

        public int PrepareCount { get; private set; }

        public int RestoreCount { get; private set; }

        public Task<AudioStateSnapshot> CaptureAsync(CancellationToken cancellationToken)
        {
            CaptureCount++;
            return Task.FromResult(new AudioStateSnapshot(0.25f, false, "Device", DateTimeOffset.UtcNow));
        }

        public Task PrepareForAlarmAsync(BluetoothAlarmBehavior behavior, CancellationToken cancellationToken)
        {
            PrepareCount++;
            if (ThrowOnPrepare)
            {
                throw new InvalidOperationException("Audio prepare failed.");
            }

            return Task.CompletedTask;
        }

        public Task RestoreAsync(AudioStateSnapshot snapshot, CancellationToken cancellationToken)
        {
            RestoreCount++;
            return Task.CompletedTask;
        }
    }

    private sealed class FakeSoundPlayer : IAlarmSoundPlayer
    {
        public bool IsPlaying { get; private set; }

        public int StartCount { get; private set; }

        public int StopCount { get; private set; }

        public Task StartAsync(string? alarmSound, CancellationToken cancellationToken)
        {
            StartCount++;
            IsPlaying = true;
            return Task.CompletedTask;
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            StopCount++;
            IsPlaying = false;
            return Task.CompletedTask;
        }
    }
}
