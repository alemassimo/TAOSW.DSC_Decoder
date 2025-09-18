using System;
using TAOSW.DSC_Decoder.Core;
using TAOSW.DSC_Decoder.Core.Interfaces;
using TAOSW.DSC_Decoder.Core.Domain;
using TAOSW.DSC_Decoder.Core.TAOSW.DSC_Decoder.Core;

public class DscMessageManager
{
    private readonly IAudioCapture _audioCapture;
    private readonly FskAutoTuner _autoTuner;
    private readonly SquelchLevelDetector _squelchLevelDetector;
    private readonly GMDSSDecoder[] _decoders;
    private readonly DSCDecoder _dscDecoder;
    private readonly DscMessageClusterizer _dscMessageClusterizer;

    public event Action<DSCMessage> OnClusteredMessageSelected;

    public DscMessageManager(IAudioCapture audioCapture, FskAutoTuner autoTuner, SquelchLevelDetector squelchLevelDetector, int sampleRate)
    {
        _audioCapture = audioCapture;
        _autoTuner = autoTuner;
        _squelchLevelDetector = squelchLevelDetector;
        _decoders = new GMDSSDecoder[DSCDecoder.SlideWindowsNumber];
        _dscDecoder = new DSCDecoder(100, sampleRate);
        _dscMessageClusterizer = new DscMessageClusterizer(new TimeSpan(0, 0, 2));
        _dscMessageClusterizer.OnClusteredMessageSelected += (msg) => OnClusteredMessageSelected?.Invoke(msg);
    }

    public void Start(int deviceNumber)
    {
        _audioCapture.StartAudioCapture(deviceNumber);
        for (int i = 0; i < DSCDecoder.SlideWindowsNumber; i++)
        {
            _decoders[i] = new GMDSSDecoder();
            _decoders[i].OnMessageDecoded += (message) =>
            {
                _dscMessageClusterizer.AddMessage(message);
            };
        }

        while (true)
        {
            var result = _audioCapture.ReadAudioData(1764);
            float[] signal = Utils.ConvertToFloatArray(result);

            if (!_squelchLevelDetector.Detect(signal)) continue;

            float[] processedSignal = _autoTuner.ProcessSignal(signal);

            var bits = _dscDecoder.DecodeFSK(processedSignal, _autoTuner.LeftFreq, _autoTuner.RightFreq);
            for (int i = 0; i < DSCDecoder.SlideWindowsNumber; i++) _decoders[i].AddBits(bits[i]);
        }
        _audioCapture.StopAudioCapture();
    }
}
