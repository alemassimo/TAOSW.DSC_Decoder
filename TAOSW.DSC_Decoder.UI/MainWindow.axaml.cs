// Copyright (c) 2025 Tao Energy SRL. Licensed under the MIT License.

using Avalonia.Controls;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Media;
using System.Threading.Tasks;
using TAOSW.DSC_Decoder.Core;
using TAOSW.DSC_Decoder.Core.Domain;
using TAOSW.DSC_Decoder.Core.Interfaces;

namespace TAOSW.DSC_Decoder.UI
{
    public partial class MainWindow : Window
    {
        private const int MaxDecodeFreq = 700;
        private const int MinDecodeFreq = 300;
        private readonly DscMessagesViewModel _viewModel = new();
        private ObservableCollection<AudioDeviceInfo> devices;
        private FrequencyBarChart _frequencyChart;
        private DscMessageManager _manager;
        private FskAutoTuner _autoTuner;

        const string AlarmSoundFilePath = "Sounds/alarm.wav"; 
        const string ErrorSoundFilePath = "Sounds/error.wav"; 
        const string WarningSoundFilePath = "Sounds/warning.wav"; 

        public MainWindow()
        {
            InitializeComponent();
            InitializeControls();
            Task.Run(() => StartDscReceiver());
        }

        private void InitializeControls()
        {
            // Add the DSC messages grid
            var gridView = new DscGridView(_viewModel);
            var messagesContainer = this.FindControl<ContentControl>("MessagesContainer");
            messagesContainer!.Content = gridView;

            // Get reference to frequency bar chart
            _frequencyChart = this.FindControl<FrequencyBarChart>("FrequencyChart");
            _frequencyChart?.SetTitle("FSK Auto-Tuner Frequencies");
            _frequencyChart?.SetFrequencyRange(0, 3000); // 0-3000 Hz as specified
            _frequencyChart?.SetDemodulatorRange(MinDecodeFreq, MaxDecodeFreq); // Highlight demodulator range
        }

        private async Task StartDscReceiver()
        {
            int sampleRate = 44100; // 88200;
            IAudioCapture audioCapture = new AudioCapture(sampleRate);
            _autoTuner = new FskAutoTuner(MaxDecodeFreq, MinDecodeFreq, sampleRate, 170);
            var squelchLevelDetector = new SquelchLevelDetector(0.0000001f, 0f);

            // Subscribe to frequency detection events from FskAutoTuner
            _autoTuner.OnFrequenciesDetected += OnFrequenciesDetected;
            
            // Convert List<AudioDeviceInfo> to ObservableCollection<AudioDeviceInfo>
            devices = new ObservableCollection<AudioDeviceInfo>(audioCapture.GetAudioCaptureDevices());

            int deviceNumber = await SelectDeviceFromDialogBox(devices.ToList());

            _manager = new DscMessageManager(audioCapture, _autoTuner, squelchLevelDetector, sampleRate);
            _manager.OnClusteredMessageSelected += (message) =>
            {
                if (message.Time is null) message.Time = DateTimeOffset.UtcNow;
                Avalonia.Threading.Dispatcher.UIThread.Post(() => _viewModel.AddMessage(message));

                // Play sound based on message type
                switch (message.Category)
                {
                    case CategoryOfCall.Distress:
                        playSound(AlarmSoundFilePath);
                        break;
                    case CategoryOfCall.Error:
                        playSound(ErrorSoundFilePath);
                        break;
                    case CategoryOfCall.Urgency:
                        playSound(WarningSoundFilePath);
                        break;
                    default:
                        // No sound for other message types
                        break;
                }

            };

            // Start the DSC manager
            _manager.Start(deviceNumber);
        }

        private void OnFrequenciesDetected(IEnumerable<FskAutoTuner.FrequencyClusterPower> frequencies)
        {
            // Update frequency bar chart on UI thread
            Avalonia.Threading.Dispatcher.UIThread.Post(() =>
            {
                try
                {
                    _frequencyChart.Draw(frequencies);
                    
                    // Also show the selected frequencies from the auto-tuner
                    var leftFreq = _autoTuner.LeftFreq;
                    var rightFreq = _autoTuner.RightFreq;
                    
                    if (leftFreq > 0 && rightFreq > 0)
                    {
                        _frequencyChart.SetSelectedFrequencies(leftFreq, rightFreq);
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error updating frequency chart: {ex.Message}");
                }
            });
        }

        

        // method to play a sound from a .wav file
        private void playSound(string filePath)
        {
            try
            {
                using (SoundPlayer player = new SoundPlayer(filePath))
                {
                    player.Load();
                    player.Play();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error playing sound: {ex.Message}");
            }
        }

        private async Task<int> SelectDeviceFromDialogBox(IEnumerable<AudioDeviceInfo> devices)
        {
            AudioDeviceInfo? selectedDevice = await Avalonia.Threading.Dispatcher.UIThread.InvokeAsync(async () =>
            {
                var dialog = new AudioInputDialog(devices);
                return await dialog.ShowDialog<AudioDeviceInfo?>(this);
            });

            return selectedDevice?.DeviceNumber ?? 0; 
        }
    }
}
