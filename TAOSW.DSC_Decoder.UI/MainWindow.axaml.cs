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
            messagesContainer.Content = gridView;

            // Get reference to frequency bar chart
            _frequencyChart = this.FindControl<FrequencyBarChart>("FrequencyChart");
            _frequencyChart.SetTitle("FSK Auto-Tuner Frequencies");
            _frequencyChart.SetFrequencyRange(0, 3000); // 0-3000 Hz as specified
        }

        private async Task StartDscReceiver()
        {
            int sampleRate = 88200;
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
                
                if (message.TC1 != FirstCommand.Test)
                    onePing();
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
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error updating frequency chart: {ex.Message}");
                }
            });
        }

        private void onePing()
        {
            SystemSounds.Beep.Play();
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
