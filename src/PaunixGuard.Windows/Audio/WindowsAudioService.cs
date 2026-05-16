using System.Runtime.InteropServices;
using PaunixGuard.Core.Alarm;

namespace PaunixGuard.Windows.Audio;

public sealed class WindowsAudioService : IAudioService
{
    public Task<AudioStateSnapshot> CaptureAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var endpoint = GetEndpointVolume();
        endpoint.GetMasterVolumeLevelScalar(out var volume);
        endpoint.GetMute(out var muted);

        return Task.FromResult(new AudioStateSnapshot(
            volume,
            muted,
            OutputDeviceId: null,
            DateTimeOffset.UtcNow));
    }

    public Task PrepareForAlarmAsync(BluetoothAlarmBehavior bluetoothBehavior, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var endpoint = GetEndpointVolume();
        endpoint.SetMute(false, Guid.Empty);
        endpoint.SetMasterVolumeLevelScalar(1.0f, Guid.Empty);

        // Output switching and Bluetooth disabling are intentionally best-effort future hooks.
        return Task.CompletedTask;
    }

    public Task RestoreAsync(AudioStateSnapshot snapshot, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var endpoint = GetEndpointVolume();
        endpoint.SetMasterVolumeLevelScalar(Math.Clamp(snapshot.MasterVolume, 0f, 1f), Guid.Empty);
        endpoint.SetMute(snapshot.IsMuted, Guid.Empty);
        return Task.CompletedTask;
    }

    private static IAudioEndpointVolume GetEndpointVolume()
    {
        var enumeratorType = Type.GetTypeFromCLSID(new Guid("BCDE0395-E52F-467C-8E3D-C4579291692E"), throwOnError: true)!;
        var enumerator = (IMMDeviceEnumerator)Activator.CreateInstance(enumeratorType)!;
        enumerator.GetDefaultAudioEndpoint(EDataFlow.eRender, ERole.eMultimedia, out var device);
        var iid = typeof(IAudioEndpointVolume).GUID;
        device.Activate(ref iid, ClsCtx.All, IntPtr.Zero, out var endpoint);
        return (IAudioEndpointVolume)endpoint;
    }

    private enum EDataFlow
    {
        eRender = 0
    }

    private enum ERole
    {
        eMultimedia = 1
    }

    [Flags]
    private enum ClsCtx
    {
        All = 23
    }

    [ComImport]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    [Guid("A95664D2-9614-4F35-A746-DE8DB63617E6")]
    private interface IMMDeviceEnumerator
    {
        void EnumAudioEndpoints();

        void GetDefaultAudioEndpoint(EDataFlow dataFlow, ERole role, out IMMDevice endpoint);
    }

    [ComImport]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    [Guid("D666063F-1587-4E43-81F1-B948E807363F")]
    private interface IMMDevice
    {
        void Activate(ref Guid iid, ClsCtx dwClsCtx, IntPtr pActivationParams, [MarshalAs(UnmanagedType.IUnknown)] out object ppInterface);
    }

    [ComImport]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    [Guid("5CDF2C82-841E-4546-9722-0CF74078229A")]
    private interface IAudioEndpointVolume
    {
        void RegisterControlChangeNotify(IntPtr client);

        void UnregisterControlChangeNotify(IntPtr client);

        void GetChannelCount(out uint channelCount);

        void SetMasterVolumeLevel(float level, Guid eventContext);

        void SetMasterVolumeLevelScalar(float level, Guid eventContext);

        void GetMasterVolumeLevel(out float level);

        void GetMasterVolumeLevelScalar(out float level);

        void SetChannelVolumeLevel(uint channelNumber, float level, Guid eventContext);

        void SetChannelVolumeLevelScalar(uint channelNumber, float level, Guid eventContext);

        void GetChannelVolumeLevel(uint channelNumber, out float level);

        void GetChannelVolumeLevelScalar(uint channelNumber, out float level);

        void SetMute([MarshalAs(UnmanagedType.Bool)] bool isMuted, Guid eventContext);

        void GetMute([MarshalAs(UnmanagedType.Bool)] out bool isMuted);
    }
}

