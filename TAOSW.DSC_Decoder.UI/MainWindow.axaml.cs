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
        private readonly DscMessagesViewModel _viewModel = new();
        private ObservableCollection<AudioDeviceInfo> devices;

        public MainWindow()
        {
            InitializeComponent();

            var gridView = new DscGridView(_viewModel);
            this.FindControl<Grid>("RootGrid").Children.Add(gridView);

            Task.Run(() => StartDscReceiver());
        }

        private async Task StartDscReceiver()
        {
            int sampleRate = 88200;
            IAudioCapture audioCapture = new AudioCapture(sampleRate);
            FskAutoTuner autoTuner = new FskAutoTuner(700, 300, sampleRate, 170);
            var squelchLevelDetector = new SquelchLevelDetector(0.0000001f, 0f);

            // Convert List<AudioDeviceInfo> to ObservableCollection<AudioDeviceInfo>
            devices = new ObservableCollection<AudioDeviceInfo>(audioCapture.GetAudioCaptureDevices());

            int deviceNumber = await SelectDeviceFromDialogBox(devices.ToList()); // Convert back to List for dialog box selection

            var manager = new DscMessageManager(audioCapture, autoTuner, squelchLevelDetector, sampleRate);
            manager.OnClusteredMessageSelected += (message) =>
            {
                if (string.IsNullOrEmpty(message.Time)) message.Time = DateTime.UtcNow.ToString("MM/dd/yy H:mm:ss");
                Avalonia.Threading.Dispatcher.UIThread.Post(() => _viewModel.AddMessage(message));
                
                if ( message.TC1 != FirstCommand.Test)
                    onePing();
                
            };
            manager.Start(deviceNumber);
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
