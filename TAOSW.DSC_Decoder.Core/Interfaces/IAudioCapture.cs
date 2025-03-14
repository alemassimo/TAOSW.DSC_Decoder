
using TAOSW.DSC_Decoder.Core.Domain;

namespace TAOSW.DSC_Decoder.Core.Interfaces
{
    public interface IAudioCapture
    {
        List<AudioDeviceInfo> GetAudioCaptureDevices();
        public Task LoadAudioFile(string filePath);
        byte[] ReadAudioData(int byteCount);
        void StartAudioCapture(int deviceNumber);
        void StopAudioCapture();
    }
}
