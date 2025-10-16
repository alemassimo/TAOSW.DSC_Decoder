// Copyright (c) 2025 Tao Energy SRL. Licensed under the MIT License.

using TAOSW.DSC_Decoder.Core;
using TAOSW.DSC_Decoder.Core.Interfaces;
using TAOSW.DSC_Decoder.Core.TAOSW.DSC_Decoder.Core;
using static TAOSW.DSC_Decoder.Core.FskAutoTuner;

public class DscMessageManager : IDisposable
{
    private readonly IAudioCapture _audioCapture;
    private readonly FskAutoTuner _autoTuner;
    private readonly SquelchLevelDetector _squelchLevelDetector;
    private readonly GMDSSDecoder[] _decoders;
    private readonly DSCDecoder _dscDecoder;
    private readonly DscMessageClusterizer _dscMessageClusterizer;
    
    private volatile bool _isRunning = false;
    private volatile bool _isDisposed = false;
    private CancellationTokenSource? _cancellationTokenSource;
    private Task? _processingTask;
    
    // Error handling and recovery
    private int _consecutiveReadErrors = 0;
    private int _consecutiveProcessingErrors = 0;
    private const int MaxConsecutiveErrors = 10;
    private DateTime _lastErrorTime = DateTime.MinValue;
    private readonly TimeSpan _errorResetInterval = TimeSpan.FromMinutes(2);
    
    // Performance monitoring
    private DateTime _lastDataProcessed = DateTime.Now;
    private readonly TimeSpan _dataTimeoutInterval = TimeSpan.FromSeconds(30);

    public event Action<DSCMessage>? OnClusteredMessageSelected;
    public event Action<IEnumerable<FrequencyClusterPower>>? OnFrequenciesDetected;
    public event Action<string>? OnError;
    public event Action<string>? OnStatusChanged;

    public DscMessageManager(IAudioCapture audioCapture, FskAutoTuner autoTuner, SquelchLevelDetector squelchLevelDetector, int sampleRate)
    {
        _audioCapture = audioCapture ?? throw new ArgumentNullException(nameof(audioCapture));
        _autoTuner = autoTuner ?? throw new ArgumentNullException(nameof(autoTuner));
        _squelchLevelDetector = squelchLevelDetector ?? throw new ArgumentNullException(nameof(squelchLevelDetector));
        
        _decoders = new GMDSSDecoder[DSCDecoder.SlideWindowsNumber];
        _dscDecoder = new DSCDecoder(100, sampleRate);
        _dscMessageClusterizer = new DscMessageClusterizer(new TimeSpan(0, 0, 2));
        
        // Wire up events
        _dscMessageClusterizer.OnClusteredMessageSelected += (msg) => OnClusteredMessageSelected?.Invoke(msg);
        _autoTuner.OnFrequenciesDetected += (freqs) => OnFrequenciesDetected?.Invoke(freqs);
        
        // Subscribe to audio capture events if available
        if (_audioCapture is IDisposable)
        {
            // Note: Only log basic status, no direct access to AudioCapture specific methods
            Console.WriteLine("Audio capture initialized");
        }
    }

    public void Start(int deviceNumber)
    {
        if (_isDisposed)
            throw new ObjectDisposedException(nameof(DscMessageManager));

        if (_isRunning)
        {
            Console.WriteLine("DSC Message Manager is already running");
            return;
        }

        try
        {
            // Initialize decoders
            for (int i = 0; i < DSCDecoder.SlideWindowsNumber; i++)
            {
                _decoders[i] = new GMDSSDecoder();
                _decoders[i].OnMessageDecoded += (message) =>
                {
                    try
                    {
                        _dscMessageClusterizer.AddMessage(message);
                    }
                    catch (Exception ex)
                    {
                        HandleError($"Error adding message to clusterizer: {ex.Message}");
                    }
                };
            }

            // Start audio capture
            _audioCapture.StartAudioCapture(deviceNumber);
            
            // Create cancellation token for clean shutdown
            _cancellationTokenSource = new CancellationTokenSource();
            
            // Start processing task
            _processingTask = Task.Run(() => ProcessAudioLoop(_cancellationTokenSource.Token), _cancellationTokenSource.Token);
            
            _isRunning = true;
            OnStatusChanged?.Invoke("DSC Message Manager started successfully");
            Console.WriteLine($"DSC Message Manager started on device {deviceNumber}");
        }
        catch (Exception ex)
        {
            HandleError($"Failed to start DSC Message Manager: {ex.Message}");
            Stop();
            throw;
        }
    }

    private async Task ProcessAudioLoop(CancellationToken cancellationToken)
    {
        const int expectedDataSize = 1764; // Expected audio data size
        const int sleepDelayMs = 10; // Small delay to prevent busy waiting
        int consecutiveEmptyReads = 0;
        const int maxEmptyReads = 100; // Allow some empty reads before considering it an issue

        try
        {
            while (!cancellationToken.IsCancellationRequested && _isRunning)
            {
                try
                {
                    // Read audio data
                    var audioData = _audioCapture.ReadAudioData(expectedDataSize);
                    
                    if (audioData == null || audioData.Length == 0)
                    {
                        consecutiveEmptyReads++;
                        
                        if (consecutiveEmptyReads > maxEmptyReads)
                        {
                            HandleError($"No audio data received for {consecutiveEmptyReads} consecutive reads");
                            consecutiveEmptyReads = 0; // Reset to prevent spam
                        }
                        
                        await Task.Delay(sleepDelayMs, cancellationToken);
                        continue;
                    }

                    // Reset empty read counter on successful read
                    consecutiveEmptyReads = 0;
                    _lastDataProcessed = DateTime.Now;

                    // Convert to float array
                    float[] signal = Utils.ConvertToFloatArray(audioData);
                    
                    if (signal.Length == 0)
                    {
                        await Task.Delay(sleepDelayMs, cancellationToken);
                        continue;
                    }

                    // Apply squelch detection
                    if (!_squelchLevelDetector.Detect(signal))
                    {
                        await Task.Delay(sleepDelayMs, cancellationToken);
                        continue;
                    }

                    // Process signal through auto-tuner
                    float[] processedSignal = _autoTuner.ProcessSignal(signal);
                    
                    if (processedSignal == null || processedSignal.Length == 0)
                    {
                        await Task.Delay(sleepDelayMs, cancellationToken);
                        continue;
                    }

                    // Decode FSK
                    var bits = _dscDecoder.DecodeFSK(processedSignal, _autoTuner.LeftFreq, _autoTuner.RightFreq);
                    
                    if (bits != null)
                    {
                        // Process bits through decoders
                        for (int i = 0; i < Math.Min(DSCDecoder.SlideWindowsNumber, bits.Length); i++)
                        {
                            if (bits[i] != null && _decoders[i] != null)
                            {
                                _decoders[i].AddBits(bits[i]);
                            }
                        }
                    }

                    // Reset error counters on successful processing
                    _consecutiveReadErrors = 0;
                    _consecutiveProcessingErrors = 0;

                    // Small delay to prevent excessive CPU usage
                    if (sleepDelayMs > 0)
                    {
                        await Task.Delay(sleepDelayMs, cancellationToken);
                    }
                }
                catch (OperationCanceledException)
                {
                    // Expected when cancellation is requested
                    break;
                }
                catch (Exception ex)
                {
                    _consecutiveProcessingErrors++;
                    HandleError($"Error in audio processing loop: {ex.Message}");
                    
                    if (_consecutiveProcessingErrors >= MaxConsecutiveErrors)
                    {
                        HandleError($"Too many consecutive processing errors ({_consecutiveProcessingErrors}). Stopping.");
                        break;
                    }
                    
                    // Wait before retrying to prevent tight error loops
                    await Task.Delay(Math.Min(1000 * _consecutiveProcessingErrors, 5000), cancellationToken);
                }
            }
        }
        catch (OperationCanceledException)
        {
            // Expected during shutdown
        }
        catch (Exception ex)
        {
            HandleError($"Fatal error in processing loop: {ex.Message}");
        }
        finally
        {
            OnStatusChanged?.Invoke("Audio processing loop stopped");
            Console.WriteLine("Audio processing loop terminated");
        }
    }

    public void Stop()
    {
        if (!_isRunning || _isDisposed)
            return;

        try
        {
            _isRunning = false;
            
            // Cancel processing task
            _cancellationTokenSource?.Cancel();
            
            // Wait for processing task to complete (with timeout)
            if (_processingTask != null)
            {
                var completed = _processingTask.Wait(TimeSpan.FromSeconds(5));
                if (!completed)
                {
                    Console.WriteLine("Warning: Processing task did not complete within timeout");
                }
            }
            
            // Stop audio capture
            _audioCapture.StopAudioCapture();
            
            OnStatusChanged?.Invoke("DSC Message Manager stopped");
            Console.WriteLine("DSC Message Manager stopped successfully");
        }
        catch (Exception ex)
        {
            HandleError($"Error stopping DSC Message Manager: {ex.Message}");
        }
        finally
        {
            // Cleanup
            _cancellationTokenSource?.Dispose();
            _cancellationTokenSource = null;
            _processingTask = null;
        }
    }

    private void HandleError(string errorMessage)
    {
        Console.WriteLine($"DSC Manager Error: {errorMessage}");
        OnError?.Invoke(errorMessage);
        _lastErrorTime = DateTime.Now;
    }

    public bool IsRunning => _isRunning && !_isDisposed;
    
    public bool IsHealthy()
    {
        if (_isDisposed || !_isRunning)
            return false;
            
        // Check if we're receiving data
        var timeSinceLastData = DateTime.Now - _lastDataProcessed;
        if (timeSinceLastData > _dataTimeoutInterval)
        {
            return false;
        }
        
        // Reset error counters if enough time has passed
        if (DateTime.Now - _lastErrorTime > _errorResetInterval)
        {
            _consecutiveReadErrors = 0;
            _consecutiveProcessingErrors = 0;
        }
        
        return _consecutiveReadErrors < MaxConsecutiveErrors && 
               _consecutiveProcessingErrors < MaxConsecutiveErrors;
    }

    public void Dispose()
    {
        if (!_isDisposed)
        {
            _isDisposed = true;
            Stop();
            
            // Dispose decoders
            if (_decoders != null)
            {
                foreach (var decoder in _decoders)
                {
                    if (decoder is IDisposable disposableDecoder)
                    {
                        disposableDecoder.Dispose();
                    }
                }
            }
            
            // Dispose other components if they implement IDisposable
            if (_dscMessageClusterizer is IDisposable disposableClusterizer)
            {
                disposableClusterizer.Dispose();
            }
            
            if (_audioCapture is IDisposable disposableCapture)
            {
                disposableCapture.Dispose();
            }
            
            GC.SuppressFinalize(this);
        }
    }

    ~DscMessageManager()
    {
        Dispose();
    }
}
