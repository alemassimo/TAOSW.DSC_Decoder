// Copyright (c) 2025 Tao Energy SRL. Licensed under the MIT License.

using TAOSW.DSC_Decoder.Core.Domain;

namespace TAOSW.DSC_Decoder.Core.Interfaces
{
    public interface IAudioCapture : IDisposable
    {
        event Action<string>? OnError;
        event Action<string>? OnStatusChanged;
        bool IsHealthy();
        List<AudioDeviceInfo> GetAudioCaptureDevices();
        Task LoadAudioFile(string filePath);
        byte[] ReadAudioData(int byteCount);
        void StartAudioCapture(int deviceNumber);
        void StopAudioCapture();
    }
}
