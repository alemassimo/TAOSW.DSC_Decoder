// Copyright (c) 2025 Tao Energy SRL. Licensed under the MIT License.

using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Interactivity;
using Avalonia.Platform.Storage;
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

        // Sound effects control
        private bool _soundEffectsEnabled = true;
        private ToggleButton _soundEffectsToggle;

        const string AlarmSoundFilePath = "Sounds/alarm.wav"; 
        const string ErrorSoundFilePath = "Sounds/error.wav"; 
        const string WarningSoundFilePath = "Sounds/warning.wav"; 
        const string InfoSoundFilePath = "Sounds/bip.wav";

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

            // Get reference to sound effects toggle
            _soundEffectsToggle = this.FindControl<ToggleButton>("SoundEffectsToggle");
            if (_soundEffectsToggle != null)
            {
                _soundEffectsToggle.IsChecked = _soundEffectsEnabled;
            }
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

                // Play sound based on message type only if sound effects are enabled
                if (_soundEffectsEnabled)
                {
                    if (message.Status.StartsWith("Error"))
                    {
                        playSound(ErrorSoundFilePath);
                        return;
                    }
                    
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
                            playSound(InfoSoundFilePath);
                            break;
                    }
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

        /// <summary>
        /// Handles the Save button click event to export messages to CSV
        /// </summary>
        private async void OnSaveButtonClicked(object? sender, RoutedEventArgs e)
        {
            try
            {
                if (_viewModel.Messages.Count == 0)
                {
                    await ShowMessageAsync("No Messages", "There are no DSC messages to save.");
                    return;
                }

                var storageProvider = GetTopLevel(this)?.StorageProvider;
                if (storageProvider == null) return;

                var file = await storageProvider.SaveFilePickerAsync(new FilePickerSaveOptions
                {
                    Title = "Save DSC Messages",
                    FileTypeChoices = new[]
                    {
                        new FilePickerFileType("CSV Files")
                        {
                            Patterns = new[] { "*.csv" }
                        }
                    },
                    SuggestedFileName = $"DSC_Messages_{DateTime.Now:yyyy-MM-dd_HH-mm-ss}.csv"
                });

                if (file != null)
                {
                    await _viewModel.SaveToCsvAsync(file.Path.LocalPath);
                    await ShowMessageAsync("Export Successful", $"Successfully saved {_viewModel.Messages.Count} messages to:\n{file.Path.LocalPath}");
                }
            }
            catch (Exception ex)
            {
                await ShowMessageAsync("Export Error", $"Failed to save messages: {ex.Message}");
            }
        }

        /// <summary>
        /// Handles the Clear button click event to clear all messages with confirmation
        /// </summary>
        private async void OnClearButtonClicked(object? sender, RoutedEventArgs e)
        {
            if (_viewModel.Messages.Count == 0)
            {
                await ShowMessageAsync("No Messages", "There are no DSC messages to clear.");
                return;
            }

            var result = await ShowConfirmationAsync("Confirm Clear", 
                $"Are you sure you want to clear all {_viewModel.Messages.Count} DSC messages?\n\nThis action cannot be undone.");
            
            if (result)
            {
                _viewModel.ClearMessages();
                await ShowMessageAsync("Messages Cleared", "All DSC messages have been cleared successfully.");
            }
        }

        /// <summary>
        /// Handles the sound effects toggle button click event
        /// </summary>
        private void OnSoundEffectsToggled(object? sender, RoutedEventArgs e)
        {
            if (sender is ToggleButton toggle)
            {
                _soundEffectsEnabled = toggle.IsChecked ?? false;
                
                // Provide feedback to user
                Console.WriteLine($"Sound effects {(_soundEffectsEnabled ? "enabled" : "disabled")}");
                
                // Optional: Play a test sound when enabling
                if (_soundEffectsEnabled)
                {
                    // Play a brief test sound to confirm sound is working
                    Task.Run(() => playSound(WarningSoundFilePath));
                }
            }
        }

        /// <summary>
        /// Gets the current state of sound effects
        /// </summary>
        public bool SoundEffectsEnabled => _soundEffectsEnabled;

        /// <summary>
        /// Programmatically sets the sound effects state
        /// </summary>
        /// <param name="enabled">True to enable sound effects, false to disable</param>
        public void SetSoundEffects(bool enabled)
        {
            _soundEffectsEnabled = enabled;
            if (_soundEffectsToggle != null)
            {
                _soundEffectsToggle.IsChecked = enabled;
            }
        }

        /// <summary>
        /// Shows a simple message dialog
        /// </summary>
        private async Task ShowMessageAsync(string title, string message)
        {
            var dialog = new Window
            {
                Title = title,
                Width = 400,
                Height = 200,
                WindowStartupLocation = WindowStartupLocation.CenterOwner,
                CanResize = false,
                Content = new StackPanel
                {
                    Margin = new Avalonia.Thickness(20),
                    Spacing = 15,
                    Children =
                    {
                        new TextBlock
                        {
                            Text = message,
                            TextWrapping = Avalonia.Media.TextWrapping.Wrap,
                            FontSize = 14
                        },
                        new Button
                        {
                            Content = "OK",
                            HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center,
                            MinWidth = 80,
                            Classes = { "ActionButton" }
                        }
                    }
                }
            };

            if (dialog.Content is StackPanel panel && panel.Children.LastOrDefault() is Button okButton)
            {
                okButton.Click += (s, e) => dialog.Close();
            }

            await dialog.ShowDialog(this);
        }

        /// <summary>
        /// Shows a confirmation dialog and returns the user's choice
        /// </summary>
        private async Task<bool> ShowConfirmationAsync(string title, string message)
        {
            bool result = false;
            
            var dialog = new Window
            {
                Title = title,
                Width = 450,
                Height = 220,
                WindowStartupLocation = WindowStartupLocation.CenterOwner,
                CanResize = false,
                Content = new StackPanel
                {
                    Margin = new Avalonia.Thickness(20),
                    Spacing = 15,
                    Children =
                    {
                        new TextBlock
                        {
                            Text = message,
                            TextWrapping = Avalonia.Media.TextWrapping.Wrap,
                            FontSize = 14
                        },
                        new StackPanel
                        {
                            Orientation = Avalonia.Layout.Orientation.Horizontal,
                            HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center,
                            Spacing = 15,
                            Children =
                            {
                                new Button
                                {
                                    Content = "Clear All",
                                    MinWidth = 100,
                                    Classes = { "ClearButton" }
                                },
                                new Button
                                {
                                    Content = "Cancel",
                                    MinWidth = 100,
                                    Classes = { "ActionButton" }
                                }
                            }
                        }
                    }
                }
            };

            if (dialog.Content is StackPanel panel && 
                panel.Children.LastOrDefault() is StackPanel buttonPanel)
            {
                if (buttonPanel.Children.FirstOrDefault() is Button clearButton)
                {
                    clearButton.Click += (s, e) => { result = true; dialog.Close(); };
                }
                
                if (buttonPanel.Children.LastOrDefault() is Button cancelButton)
                {
                    cancelButton.Click += (s, e) => { result = false; dialog.Close(); };
                }
            }

            await dialog.ShowDialog(this);
            return result;
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
