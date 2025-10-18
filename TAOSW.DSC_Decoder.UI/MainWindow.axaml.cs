// Copyright (c) 2025 Tao Energy SRL. Licensed under the MIT License.

using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Interactivity;
using Avalonia.Platform.Storage;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
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
            
            this.Closing += OnWindowClosing;
            
            Task.Run(() => StartDscReceiver());
        }

        private void InitializeControls()
        {
            var gridView = new DscGridView(_viewModel);
            var messagesContainer = this.FindControl<ContentControl>("MessagesContainer");
            messagesContainer!.Content = gridView;

            _frequencyChart = this.FindControl<FrequencyBarChart>("FrequencyChart");
            _frequencyChart?.SetTitle("FSK Auto-Tuner Frequencies");
            _frequencyChart?.SetFrequencyRange(0, 3000); // 0-3000 Hz as specified
            _frequencyChart?.SetDemodulatorRange(MinDecodeFreq, MaxDecodeFreq); // Highlight demodulator range

            // Subscribe to auto-tuning changes from FrequencyBarChart
            if (_frequencyChart != null)
            {
                _frequencyChart.AutoTuningChanged += OnAutoTuningChanged;
                _frequencyChart.ManualFrequencyAdjustmentRequested += OnManualFrequencyAdjustmentRequested;
            }

            _soundEffectsToggle = this.FindControl<ToggleButton>("SoundEffectsToggle");
            if (_soundEffectsToggle != null)
            {
                _soundEffectsToggle.IsChecked = _soundEffectsEnabled;
            }
        }

        /// <summary>
        /// Handles auto-tuning state changes from FrequencyBarChart
        /// </summary>
        /// <param name="sender">The FrequencyBarChart instance</param>
        /// <param name="isEnabled">True if auto-tuning is enabled, false otherwise</param>
        private void OnAutoTuningChanged(object? sender, bool isEnabled)
        {
            try
            {
                if (_autoTuner != null)
                {
                    _autoTuner.IsAutoTuningEnabled = isEnabled;
                    Console.WriteLine($"FskAutoTuner auto-tuning set to: {isEnabled}");
                    
                    // Optionally, show a brief status message
                    Avalonia.Threading.Dispatcher.UIThread.Post(async () =>
                    {
                        // You could show a toast or brief status message here if desired
                        // For now, just log the change
                        Console.WriteLine($"Auto-tuning {(isEnabled ? "enabled" : "disabled")} successfully");
                    });
                }
                else
                {
                    Console.WriteLine("Warning: AutoTuner is not initialized yet");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error changing auto-tuning state: {ex.Message}");
            }
        }

        /// <summary>
        /// Handles manual frequency adjustment requests from FrequencyBarChart
        /// </summary>
        /// <param name="sender">The FrequencyBarChart instance</param>
        /// <param name="increment">The frequency increment in Hz (positive for increase, negative for decrease)</param>
        private void OnManualFrequencyAdjustmentRequested(object? sender, float increment)
        {
            try
            {
                if (_autoTuner != null)
                {
                    var oldLeftFreq = _autoTuner.LeftFreq;
                    
                    // Apply the manual frequency adjustment
                    _autoTuner.SetManualLeftFreqIncrement(increment);
                    
                    var newLeftFreq = _autoTuner.LeftFreq;
                    var newRightFreq = _autoTuner.RightFreq;
                    
                    Console.WriteLine($"Manual frequency adjustment: {oldLeftFreq:F0} Hz → {newLeftFreq:F0} Hz (increment: {increment:F0} Hz)");
                    
                    // Update the frequency chart display
                    if (_frequencyChart != null)
                    {
                        _frequencyChart.UpdateFrequencyDisplay(newLeftFreq);
                        
                        // If both frequencies are valid, update the chart visualization
                        if (newLeftFreq > 0 && newRightFreq > 0)
                        {
                            _frequencyChart.SetSelectedFrequencies(newLeftFreq, newRightFreq);
                        }
                    }
                    
                    Console.WriteLine($"New FSK frequencies: Left={newLeftFreq:F0} Hz, Right={newRightFreq:F0} Hz");
                }
                else
                {
                    Console.WriteLine("Warning: AutoTuner is not initialized yet");
                    
                    Avalonia.Threading.Dispatcher.UIThread.Post(async () =>
                    {
                        await ShowMessageAsync("Manual Tuning Error", 
                            "The auto-tuner is not ready yet. Please wait for the system to initialize.");
                    });
                }
            }
            catch (ArgumentOutOfRangeException ex)
            {
                Console.WriteLine($"Frequency adjustment out of range: {ex.Message}");
                
                Avalonia.Threading.Dispatcher.UIThread.Post(async () =>
                {
                    await ShowMessageAsync("Frequency Out of Range", 
                        $"The requested frequency adjustment would exceed the valid range.\n\n{ex.Message}");
                });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error adjusting frequency manually: {ex.Message}");
                
                Avalonia.Threading.Dispatcher.UIThread.Post(async () =>
                {
                    await ShowMessageAsync("Manual Tuning Error", 
                        $"An error occurred while adjusting the frequency:\n{ex.Message}");
                });
            }
        }

        private async Task StartDscReceiver()
        {
            try
            {
                int sampleRate = 44100; // 88200;
                IAudioCapture audioCapture = new AudioCapture(sampleRate);
                _autoTuner = new FskAutoTuner(MaxDecodeFreq, MinDecodeFreq, sampleRate, 170);
                var squelchLevelDetector = new SquelchLevelDetector(0.0000001f, 0f);

                _autoTuner.OnFrequenciesDetected += OnFrequenciesDetected;
                
                // Sync the initial auto-tuning state with the FrequencyBarChart
                if (_frequencyChart != null)
                {
                    var initialAutoTuningState = _frequencyChart.IsAutoTuningEnabled;
                    _autoTuner.IsAutoTuningEnabled = initialAutoTuningState;
                    Console.WriteLine($"Initial auto-tuning state synchronized: {initialAutoTuningState}");
                    
                    // Initialize frequency display
                    _frequencyChart.UpdateFrequencyDisplay(_autoTuner.LeftFreq);
                }
                
                devices = new ObservableCollection<AudioDeviceInfo>(audioCapture.GetAudioCaptureDevices());

                if (devices.Count == 0)
                {
                    await Avalonia.Threading.Dispatcher.UIThread.InvokeAsync(async () =>
                    {
                        await ShowMessageAsync("No Audio Devices", "No audio capture devices were found. Please check your audio hardware and try again.");
                    });
                    return;
                }

                int deviceNumber = await SelectDeviceFromDialogBox(devices.ToList());

                _manager = new DscMessageManager(audioCapture, _autoTuner, squelchLevelDetector, sampleRate);
                
                _manager.OnError += (error) =>
                {
                    Console.WriteLine($"DSC Manager Error: {error}");
                    Avalonia.Threading.Dispatcher.UIThread.Post(async () =>
                    {
                        await ShowMessageAsync("DSC Processing Error", $"An error occurred during DSC processing:\n{error}");
                    });
                };

                _manager.OnStatusChanged += (status) =>
                {
                    Console.WriteLine($"DSC Manager Status: {status}");
                };

                if (audioCapture is AudioCapture audioCaptureImpl)
                {
                    audioCaptureImpl.OnError += (error) =>
                    {
                        Console.WriteLine($"Audio Capture Error: {error}");
                        Avalonia.Threading.Dispatcher.UIThread.Post(async () =>
                        {
                            await ShowMessageAsync("Audio Capture Error", $"An error occurred with audio capture:\n{error}");
                        });
                    };

                    audioCaptureImpl.OnStatusChanged += (status) =>
                    {
                        Console.WriteLine($"Audio Capture Status: {status}");
                    };
                }

                _manager.OnClusteredMessageSelected += (message) =>
                {
                    try
                    {
                        if (message.Time is null) message.Time = DateTimeOffset.UtcNow;
                        Avalonia.Threading.Dispatcher.UIThread.Post(() => _viewModel.AddMessage(message));

                       if (_soundEffectsEnabled)
                        {
                            switch (message.Category)
                            {
                                case CategoryOfCall.Distress:
                                    Task.Run(() => playSound(AlarmSoundFilePath));
                                    break;
                                case CategoryOfCall.Error:
                                    Task.Run(() => playSound(ErrorSoundFilePath));
                                    break;
                                case CategoryOfCall.Urgency:
                                    Task.Run(() => playSound(WarningSoundFilePath));
                                    break;
                                default:
                                    Task.Run(() => playSound(InfoSoundFilePath));
                                    break;
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Error processing message: {ex.Message}");
                    }
                };

                try
                {
                    _manager.Start(deviceNumber);
                    
                    _ = Task.Run(async () =>
                    {
                        while (_manager?.IsRunning == true)
                        {
                            await Task.Delay(TimeSpan.FromSeconds(30));
                            
                            bool managerHealthy = _manager?.IsHealthy() == true;
                            bool audioCaptureHealthy = true;
                            
                            if (audioCapture is AudioCapture audioCaptureImpl)
                            {
                                audioCaptureHealthy = audioCaptureImpl.IsHealthy();
                            }
                            
                            if (!managerHealthy || !audioCaptureHealthy)
                            {
                                Console.WriteLine("Health check failed - attempting restart");
                                await Avalonia.Threading.Dispatcher.UIThread.InvokeAsync(async () =>
                                {
                                    await ShowMessageAsync("Audio Processing Issue", 
                                        "Audio processing has encountered issues. The system will attempt to recover automatically.");
                                });
                                break;
                            }
                        }
                    });
                }
                catch (Exception ex)
                {
                    await Avalonia.Threading.Dispatcher.UIThread.InvokeAsync(async () =>
                    {
                        await ShowMessageAsync("Startup Error", 
                            $"Failed to start DSC receiver:\n{ex.Message}\n\nPlease check your audio device and try again.");
                    });
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Fatal error in StartDscReceiver: {ex.Message}");
                await Avalonia.Threading.Dispatcher.UIThread.InvokeAsync(async () =>
                {
                    await ShowMessageAsync("Critical Error", 
                        $"A critical error occurred during initialization:\n{ex.Message}");
                });
            }
        }

        private void OnFrequenciesDetected(IEnumerable<FskAutoTuner.FrequencyClusterPower> frequencies)
        {
            Avalonia.Threading.Dispatcher.UIThread.Post(() =>
            {
                try
                {
                    _frequencyChart.Draw(frequencies);
                    
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
                
                Console.WriteLine($"Sound effects {(_soundEffectsEnabled ? "enabled" : "disabled")}");
                
                if (_soundEffectsEnabled)Task.Run(() => playSound(WarningSoundFilePath));
                
            }
        }

        /// <summary>
        /// Gets the current state of sound effects
        /// </summary>
        public bool SoundEffectsEnabled => _soundEffectsEnabled;

        /// <summary>
        /// Gets the current state of auto-tuning
        /// </summary>
        public bool AutoTuningEnabled => _autoTuner?.IsAutoTuningEnabled ?? true;

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
        /// Programmatically sets the auto-tuning state
        /// </summary>
        /// <param name="enabled">True to enable auto-tuning, false to disable</param>
        public void SetAutoTuning(bool enabled)
        {
            if (_autoTuner != null)
            {
                _autoTuner.IsAutoTuningEnabled = enabled;
            }
            
            if (_frequencyChart != null)
            {
                _frequencyChart.SetAutoTuning(enabled);
            }
        }

        /// <summary>
        /// Gets the current left frequency
        /// </summary>
        public float CurrentLeftFrequency => _autoTuner?.LeftFreq ?? 0;

        /// <summary>
        /// Gets the current right frequency
        /// </summary>
        public float CurrentRightFrequency => _autoTuner?.RightFreq ?? 0;

        /// <summary>
        /// Manually adjusts the frequency by the specified increment
        /// </summary>
        /// <param name="increment">Frequency increment in Hz (positive to increase, negative to decrease)</param>
        public void AdjustFrequency(float increment)
        {
            OnManualFrequencyAdjustmentRequested(this, increment);
        }

        /// <summary>
        /// Sets a specific left frequency manually
        /// </summary>
        /// <param name="leftFreq">The left frequency to set in Hz</param>
        public void SetManualFrequency(float leftFreq)
        {
            try
            {
                if (_autoTuner != null)
                {
                    _autoTuner.SetManualLeftFreq(leftFreq);
                    
                    var newLeftFreq = _autoTuner.LeftFreq;
                    var newRightFreq = _autoTuner.RightFreq;
                    
                    // Update the frequency chart display
                    if (_frequencyChart != null)
                    {
                        _frequencyChart.UpdateFrequencyDisplay(newLeftFreq);
                        _frequencyChart.SetSelectedFrequencies(newLeftFreq, newRightFreq);
                    }
                    
                    Console.WriteLine($"Manual frequency set: Left={newLeftFreq:F0} Hz, Right={newRightFreq:F0} Hz");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error setting manual frequency: {ex.Message}");
                throw;
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

        private async void playSound(string filePath)
        {
            try
            {
                if (!_soundEffectsEnabled)
                    return;

                if (!File.Exists(filePath))
                {
                    Console.WriteLine($"Sound file not found: {filePath}");
                    return;
                }

                await Task.Run(() =>
                {
                    try
                    {
                        using (SoundPlayer player = new SoundPlayer(filePath))
                        {
                            player.LoadTimeout = 5000; 
                            player.Load();
                            player.Play();
                        }
                    }
                    catch (TimeoutException)
                    {
                        Console.WriteLine($"Timeout loading sound file: {filePath}");
                    }
                    catch (InvalidOperationException ex)
                    {
                        Console.WriteLine($"Invalid sound file format {filePath}: {ex.Message}");
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Error playing sound {filePath}: {ex.Message}");
                    }
                });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Unexpected error in playSound: {ex.Message}");
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

        private void OnWindowClosing(object? sender, WindowClosingEventArgs e)
        {
            try
            {
                _manager?.Stop();
                _manager?.Dispose();
                
                Console.WriteLine("Application resources cleaned up successfully");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error during cleanup: {ex.Message}");
            }
        }
    }
}
