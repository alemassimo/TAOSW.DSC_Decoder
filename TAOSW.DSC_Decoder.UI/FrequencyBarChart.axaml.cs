// Copyright (c) 2025 Tao Energy SRL. Licensed under the MIT License.

using Avalonia.Controls;
using Avalonia.Controls.Shapes;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using Avalonia.Media;
using System;
using System.Collections.Generic;
using System.Linq;
using TAOSW.DSC_Decoder.Core;

namespace TAOSW.DSC_Decoder.UI
{
    public partial class FrequencyBarChart : UserControl
    {
        private readonly FrequencyBarViewModel _viewModel;
        private Canvas? _chartCanvas;
        private StackPanel? _yAxisLabels;
        private TextBlock? _statusText;
        private TextBlock? _infoText;

        private const double MinBarWidth = 2.0;
        private const double MaxBarWidth = 20.0;

        // Store selected frequencies for FSK demodulation
        private float _selectedLeftFreq = 0;
        private float _selectedRightFreq = 0;

        public FrequencyBarChart()
        {
            InitializeComponent();
            _viewModel = new FrequencyBarViewModel();
            _viewModel.Title = "FSK Frequency Detection";
            DataContext = _viewModel;
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }

        protected override void OnLoaded(RoutedEventArgs e)
        {
            base.OnLoaded(e);
            _chartCanvas = this.FindControl<Canvas>("ChartCanvas");
            _yAxisLabels = this.FindControl<StackPanel>("YAxisLabels");
            _statusText = this.FindControl<TextBlock>("StatusText");
            _infoText = this.FindControl<TextBlock>("InfoText");
            
            UpdateInfo();
        }

        /// <summary>
        /// Main method to draw the frequency bar chart
        /// </summary>
        /// <param name="frequencies">Frequency cluster power data from FskAutoTuner</param>
        public void Draw(IEnumerable<FskAutoTuner.FrequencyClusterPower> frequencies)
        {
            try
            {
                if (frequencies == null)
                {
                    Clear();
                    return;
                }

                var freqList = frequencies.ToList();
                UpdateStatus($"Drawing {freqList.Count} frequency bars");

                // Update view model data
                var canvasWidth = _chartCanvas?.Bounds.Width ?? 800;
                _viewModel.UpdateData(frequencies, canvasWidth);

                // Draw the chart
                DrawChart();

                UpdateStatus($"Chart updated - {freqList.Count} frequencies");
            }
            catch (Exception ex)
            {
                UpdateStatus($"Error: {ex.Message}");
            }
        }

        /// <summary>
        /// Sets the frequency range for the chart
        /// </summary>
        /// <param name="minFreq">Minimum frequency in Hz</param>
        /// <param name="maxFreq">Maximum frequency in Hz</param>
        public void SetFrequencyRange(double minFreq, double maxFreq)
        {
            _viewModel.MinFrequency = minFreq;
            _viewModel.MaxFrequency = maxFreq;
            UpdateInfo();
        }

        /// <summary>
        /// Sets the demodulator operating frequency range to highlight
        /// </summary>
        /// <param name="minDecodeFreq">Minimum demodulator frequency in Hz</param>
        /// <param name="maxDecodeFreq">Maximum demodulator frequency in Hz</param>
        public void SetDemodulatorRange(double minDecodeFreq, double maxDecodeFreq)
        {
            _viewModel.SetDemodulatorRange(minDecodeFreq, maxDecodeFreq);
        }

        /// <summary>
        /// Sets the chart title
        /// </summary>
        /// <param name="title">Chart title</param>
        public void SetTitle(string title)
        {
            _viewModel.Title = title;
        }

        /// <summary>
        /// Clears the chart
        /// </summary>
        public void Clear()
        {
            _viewModel.Clear();
            _chartCanvas?.Children.Clear();
            _yAxisLabels?.Children.Clear();
            _selectedLeftFreq = 0;
            _selectedRightFreq = 0;
            UpdateStatus("Chart cleared");
        }

        private void DrawChart()
        {
            if (_chartCanvas == null || _yAxisLabels == null) return;

            var canvasWidth = _chartCanvas.Bounds.Width;
            var canvasHeight = _chartCanvas.Bounds.Height;

            if (canvasWidth <= 10 || canvasHeight <= 10) return;

            _chartCanvas.Children.Clear();
            _yAxisLabels.Children.Clear();

            var barData = _viewModel.BarData;
            if (!barData.Any()) return;

            // Draw grid lines first
            DrawGrid(canvasWidth, canvasHeight);

            // Draw demodulator range highlight
            DrawDemodulatorRange(canvasWidth, canvasHeight);

            // Calculate bar width based on frequency density
            var frequencyRange = _viewModel.MaxFrequency - _viewModel.MinFrequency;
            var baseBarWidth = Math.Max(MinBarWidth, Math.Min(MaxBarWidth, canvasWidth / frequencyRange * 10));

            // Draw bars
            for (int i = 0; i < barData.Count; i++)
            {
                DrawBar(barData[i], canvasWidth, canvasHeight, baseBarWidth);
            }

            // Draw selected frequencies if available
            if (_selectedLeftFreq > 0 && _selectedRightFreq > 0)
            {
                DrawSelectedFrequencies(_selectedLeftFreq, _selectedRightFreq);
            }

            // Draw Y-axis labels
            DrawYAxisLabels(canvasHeight);

            // Draw X-axis labels (general scale)
            DrawXAxisLabels(canvasWidth, canvasHeight);
        }

        private void DrawBar(FrequencyBarData bar, double canvasWidth, double canvasHeight, double barWidth)
        {
            if (_chartCanvas == null) return;

            var frequencyRange = _viewModel.MaxFrequency - _viewModel.MinFrequency;
            var x = (bar.Frequency - _viewModel.MinFrequency) / frequencyRange * canvasWidth;
            var barHeight = bar.NormalizedHeight * canvasHeight * 0.9; // 90% of canvas height
            var y = canvasHeight - barHeight;

            // Create bar rectangle
            var rect = new Rectangle
            {
                Width = barWidth,
                Height = barHeight,
                Fill = GetBarColor(bar.Power, _viewModel.MaxPower),
                Stroke = new SolidColorBrush(Colors.DarkBlue),
                StrokeThickness = 0.5
            };

            Canvas.SetLeft(rect, x - barWidth / 2);
            Canvas.SetTop(rect, y);
            _chartCanvas.Children.Add(rect);

            // Add frequency label for significant peaks
            if (bar.NormalizedHeight > 0.3) // Show labels only for bars > 30% of max height
            {
                var label = new TextBlock
                {
                    Text = $"{bar.Power:F2}",
                    FontSize = 9,
                    Foreground = new SolidColorBrush(Colors.DarkBlue),
                    Background = new SolidColorBrush(Colors.White) { Opacity = 0.8 }
                };

                Canvas.SetLeft(label, x - 15);
                Canvas.SetTop(label, y - 15);
                _chartCanvas.Children.Add(label);
            }
        }

        private void DrawGrid(double canvasWidth, double canvasHeight)
        {
            if (_chartCanvas == null) return;

            var gridBrush = new SolidColorBrush(Colors.LightGray) { Opacity = 0.5 };

            // Vertical grid lines (frequency)
            var frequencyStep = (_viewModel.MaxFrequency - _viewModel.MinFrequency) / 10;
            for (int i = 1; i < 10; i++)
            {
                var x = (i * canvasWidth) / 10;
                var line = new Line
                {
                    StartPoint = new Avalonia.Point(x, 0),
                    EndPoint = new Avalonia.Point(x, canvasHeight),
                    Stroke = gridBrush,
                    StrokeThickness = 1
                };
                _chartCanvas.Children.Add(line);
            }

            // Horizontal grid lines (power)
            for (int i = 1; i < 10; i++)
            {
                var y = (i * canvasHeight) / 10;
                var line = new Line
                {
                    StartPoint = new Avalonia.Point(0, y),
                    EndPoint = new Avalonia.Point(canvasWidth, y),
                    Stroke = gridBrush,
                    StrokeThickness = 1
                };
                _chartCanvas.Children.Add(line);
            }
        }

        private void DrawDemodulatorRange(double canvasWidth, double canvasHeight)
        {
            if (_chartCanvas == null) return;

            var minDemodFreq = _viewModel.MinDemodulatorFreq;
            var maxDemodFreq = _viewModel.MaxDemodulatorFreq;

            // Only draw if valid range is set
            if (minDemodFreq <= 0 || maxDemodFreq <= 0 || minDemodFreq >= maxDemodFreq) return;

            var frequencyRange = _viewModel.MaxFrequency - _viewModel.MinFrequency;
            
            // Calculate positions
            var xStart = Math.Max(0, (minDemodFreq - _viewModel.MinFrequency) / frequencyRange * canvasWidth);
            var xEnd = Math.Min(canvasWidth, (maxDemodFreq - _viewModel.MinFrequency) / frequencyRange * canvasWidth);
            
            if (xStart >= canvasWidth || xEnd <= 0) return; // Range is outside visible area

            // Draw highlighted rectangle
            var highlightRect = new Rectangle
            {
                Width = xEnd - xStart,
                Height = canvasHeight,
                Fill = new SolidColorBrush(Colors.Yellow) { Opacity = 0.2 },
                Stroke = new SolidColorBrush(Colors.Orange),
                StrokeThickness = 2
            };

            Canvas.SetLeft(highlightRect, xStart);
            Canvas.SetTop(highlightRect, 0);
            _chartCanvas.Children.Add(highlightRect);

            // Add label for demodulator range
            var labelText = $"Demod: {minDemodFreq:F0}-{maxDemodFreq:F0} Hz";
            var label = new TextBlock
            {
                Text = labelText,
                FontSize = 11,
                FontWeight = FontWeight.Bold,
                Foreground = new SolidColorBrush(Colors.DarkOrange),
                Background = new SolidColorBrush(Colors.White) { Opacity = 0.9 },
                Padding = new Avalonia.Thickness(4, 2)
            };

            // Position label at top of highlighted area
            Canvas.SetLeft(label, xStart + 5);
            Canvas.SetTop(label, 5);
            _chartCanvas.Children.Add(label);

            // Draw vertical lines at boundaries
            var leftLine = new Line
            {
                StartPoint = new Avalonia.Point(xStart, 0),
                EndPoint = new Avalonia.Point(xStart, canvasHeight),
                Stroke = new SolidColorBrush(Colors.Orange),
                StrokeThickness = 2
            };
            _chartCanvas.Children.Add(leftLine);

            var rightLine = new Line
            {
                StartPoint = new Avalonia.Point(xEnd, 0),
                EndPoint = new Avalonia.Point(xEnd, canvasHeight),
                Stroke = new SolidColorBrush(Colors.Orange),
                StrokeThickness = 2
            };
            _chartCanvas.Children.Add(rightLine);
        }

        private void DrawYAxisLabels(double canvasHeight)
        {
            if (_yAxisLabels == null) return;

            _yAxisLabels.Children.Clear();

            var maxPower = _viewModel.MaxPower;
            var steps = 5;

            for (int i = steps; i >= 0; i--)
            {
                var value = (maxPower / steps) * i;
                var label = new TextBlock
                {
                    Text = value.ToString("F2"),
                    FontSize = 10,
                    HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Right,
                    VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center,
                    Margin = new Avalonia.Thickness(0, canvasHeight / steps / 2, 0, 0)
                };
                _yAxisLabels.Children.Add(label);
            }
        }

        private void DrawXAxisLabels(double canvasWidth, double canvasHeight)
        {
            if (_chartCanvas == null) return;

            var frequencyRange = _viewModel.MaxFrequency - _viewModel.MinFrequency;
            var steps = 6;

            for (int i = 0; i <= steps; i++)
            {
                var freq = _viewModel.MinFrequency + (frequencyRange / steps) * i;
                var x = (canvasWidth / steps) * i;

                var label = new TextBlock
                {
                    Text = freq.ToString("F0"),
                    FontSize = 10,
                    HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center
                };

                Canvas.SetLeft(label, x - 20);
                Canvas.SetTop(label, canvasHeight + 5);
                _chartCanvas.Children.Add(label);
            }
        }

        private SolidColorBrush GetBarColor(double power, double maxPower)
        {
            var intensity = maxPower > 0 ? power / maxPower : 0;

            // Color gradient from blue (low) to red (high)
            if (intensity < 0.3)
                return new SolidColorBrush(Colors.LightBlue);
            else if (intensity < 0.6)
                return new SolidColorBrush(Colors.Blue);
            else if (intensity < 0.8)
                return new SolidColorBrush(Colors.Orange);
            else
                return new SolidColorBrush(Colors.Red);
        }

        protected override void OnSizeChanged(SizeChangedEventArgs e)
        {
            base.OnSizeChanged(e);
            
            // Redraw chart when size changes
            if (_viewModel.BarData.Any())
            {
                DrawChart();
            }
        }

        private void OnClearClicked(object? sender, RoutedEventArgs e)
        {
            Clear();
        }

        private void OnAutoScaleClicked(object? sender, RoutedEventArgs e)
        {
            // Re-draw with current data to auto-scale
            if (_viewModel.BarData.Any())
            {
                DrawChart();
                UpdateStatus("Auto-scaled chart");
            }
        }

        private void UpdateStatus(string status)
        {
            if (_statusText != null)
            {
                _statusText.Text = status;
            }
        }

        private void UpdateInfo()
        {
            if (_infoText != null)
            {
                _infoText.Text = $"Range: {_viewModel.MinFrequency:F0} - {_viewModel.MaxFrequency:F0} Hz";
            }
        }

        /// <summary>
        /// Sets the selected frequencies for FSK demodulation and redraws the chart
        /// </summary>
        /// <param name="leftFreq">Left frequency (lower) for FSK demodulation</param>
        /// <param name="rightFreq">Right frequency (higher) for FSK demodulation</param>
        public void SetSelectedFrequencies(float leftFreq, float rightFreq)
        {
            _selectedLeftFreq = leftFreq;
            _selectedRightFreq = rightFreq;
            
            // Redraw chart to show the selected frequencies
            DrawChart();
            
            UpdateStatus($"FSK frequencies: {leftFreq:F0} / {rightFreq:F0} Hz");
        }

        /// <summary>
        /// Clears the selected frequencies
        /// </summary>
        public void ClearSelectedFrequencies()
        {
            _selectedLeftFreq = 0;
            _selectedRightFreq = 0;
            DrawChart();
            UpdateStatus("FSK frequencies cleared");
        }

        /// <summary>
        /// Draws vertical red lines to mark the selected frequencies for DSC decoding
        /// </summary>
        /// <param name="leftFreq">Left frequency (lower) for FSK demodulation</param>
        /// <param name="rightFreq">Right frequency (higher) for FSK demodulation</param>
        internal void DrawSelectedFrequencies(float leftFreq, float rightFreq)
        {
            if (_chartCanvas == null) return;

            var canvasWidth = _chartCanvas.Bounds.Width;
            var canvasHeight = _chartCanvas.Bounds.Height;

            if (canvasWidth <= 10 || canvasHeight <= 10) return;

            var frequencyRange = _viewModel.MaxFrequency - _viewModel.MinFrequency;

            // Draw left frequency line (lower frequency)
            if (leftFreq >= _viewModel.MinFrequency && leftFreq <= _viewModel.MaxFrequency)
            {
                var xLeft = (leftFreq - _viewModel.MinFrequency) / frequencyRange * canvasWidth;

                var leftLine = new Line
                {
                    StartPoint = new Avalonia.Point(xLeft, 0),
                    EndPoint = new Avalonia.Point(xLeft, canvasHeight),
                    Stroke = new SolidColorBrush(Colors.Red),
                    StrokeThickness = 3,
                    StrokeDashArray = new Avalonia.Collections.AvaloniaList<double> { 5, 3 } // Dashed line
                };
                _chartCanvas.Children.Add(leftLine);

                // Add frequency label for left frequency
                var leftLabel = new TextBlock
                {
                    Text = $"{leftFreq:F0} Hz",
                    FontSize = 10,
                    FontWeight = FontWeight.Bold,
                    Foreground = new SolidColorBrush(Colors.Red),
                    Background = new SolidColorBrush(Colors.White) { Opacity = 0.9 },
                    Padding = new Avalonia.Thickness(3, 2)
                };

                Canvas.SetLeft(leftLabel, xLeft + 5);
                Canvas.SetTop(leftLabel, canvasHeight - 60);
                _chartCanvas.Children.Add(leftLabel);
            }

            // Draw right frequency line (higher frequency)
            if (rightFreq >= _viewModel.MinFrequency && rightFreq <= _viewModel.MaxFrequency)
            {
                var xRight = (rightFreq - _viewModel.MinFrequency) / frequencyRange * canvasWidth;

                var rightLine = new Line
                {
                    StartPoint = new Avalonia.Point(xRight, 0),
                    EndPoint = new Avalonia.Point(xRight, canvasHeight),
                    Stroke = new SolidColorBrush(Colors.Red),
                    StrokeThickness = 3,
                    StrokeDashArray = new Avalonia.Collections.AvaloniaList<double> { 5, 3 } // Dashed line
                };
                _chartCanvas.Children.Add(rightLine);

                // Add frequency label for right frequency
                var rightLabel = new TextBlock
                {
                    Text = $"{rightFreq:F0} Hz",
                    FontSize = 10,
                    FontWeight = FontWeight.Bold,
                    Foreground = new SolidColorBrush(Colors.Red),
                    Background = new SolidColorBrush(Colors.White) { Opacity = 0.9 },
                    Padding = new Avalonia.Thickness(3, 2)
                };

                Canvas.SetLeft(rightLabel, xRight + 5);
                Canvas.SetTop(rightLabel, canvasHeight - 40);
                _chartCanvas.Children.Add(rightLabel);
            }

            // Add a combined label showing the frequency pair
            if (leftFreq > 0 && rightFreq > 0)
            {
                var pairLabel = new TextBlock
                {
                    Text = $"FSK Pair: {leftFreq:F0} / {rightFreq:F0} Hz",
                    FontSize = 11,
                    FontWeight = FontWeight.Bold,
                    Foreground = new SolidColorBrush(Colors.DarkRed),
                    Background = new SolidColorBrush(Colors.White) { Opacity = 0.9 },
                    Padding = new Avalonia.Thickness(4, 3)
                };

                Canvas.SetLeft(pairLabel, 10);
                Canvas.SetTop(pairLabel, canvasHeight - 25);
                _chartCanvas.Children.Add(pairLabel);
            }
        }

        /// <summary>
        /// Event fired when the chart needs to be refreshed
        /// </summary>
        public event EventHandler? ChartRefreshRequested;
    }
}