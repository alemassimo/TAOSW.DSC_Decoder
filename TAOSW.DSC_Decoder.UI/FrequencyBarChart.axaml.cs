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

            // Calculate bar width based on frequency density
            var frequencyRange = _viewModel.MaxFrequency - _viewModel.MinFrequency;
            var baseBarWidth = Math.Max(MinBarWidth, Math.Min(MaxBarWidth, canvasWidth / frequencyRange * 10));

            // Draw bars
            foreach (var bar in barData)
            {
                DrawBar(bar, canvasWidth, canvasHeight, baseBarWidth);
            }

            // Draw Y-axis labels
            DrawYAxisLabels(canvasHeight);

            // Draw X-axis labels
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
                    Text = $"{bar.Frequency:F0}",
                    FontSize = 10,
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
        /// Event fired when the chart needs to be refreshed
        /// </summary>
        public event EventHandler? ChartRefreshRequested;
    }
}