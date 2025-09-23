// Copyright (c) 2025 Tao Energy SRL. Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using TAOSW.DSC_Decoder.Core;

namespace TAOSW.DSC_Decoder.UI
{
    public class FrequencyBarData
    {
        public double Frequency { get; set; }
        public double Power { get; set; }
        public double NormalizedHeight { get; set; }
        public double XPosition { get; set; }
    }

    public class FrequencyBarViewModel : INotifyPropertyChanged
    {
        private string _title = "Frequency Spectrum";
        private List<FrequencyBarData> _barData = new();
        private double _maxFrequency = 3000;
        private double _minFrequency = 0;
        private double _maxPower = 1.0;
        private int _frequencyStep = 500;
        private double _minDemodulatorFreq = 0;
        private double _maxDemodulatorFreq = 0;

        public string Title
        {
            get => _title;
            set
            {
                _title = value;
                OnPropertyChanged();
            }
        }

        public List<FrequencyBarData> BarData
        {
            get => _barData;
            private set
            {
                _barData = value;
                OnPropertyChanged();
            }
        }

        public double MaxFrequency
        {
            get => _maxFrequency;
            set
            {
                _maxFrequency = value;
                OnPropertyChanged();
            }
        }

        public double MinFrequency
        {
            get => _minFrequency;
            set
            {
                _minFrequency = value;
                OnPropertyChanged();
            }
        }

        public double MaxPower
        {
            get => _maxPower;
            private set
            {
                _maxPower = value;
                OnPropertyChanged();
            }
        }

        public int FrequencyStep
        {
            get => _frequencyStep;
            set
            {
                _frequencyStep = value;
                OnPropertyChanged();
            }
        }

        public double MinDemodulatorFreq
        {
            get => _minDemodulatorFreq;
            set
            {
                _minDemodulatorFreq = value;
                OnPropertyChanged();
            }
        }

        public double MaxDemodulatorFreq
        {
            get => _maxDemodulatorFreq;
            set
            {
                _maxDemodulatorFreq = value;
                OnPropertyChanged();
            }
        }

        /// <summary>
        /// Updates the bar chart with frequency cluster data
        /// </summary>
        /// <param name="frequencies">Frequency cluster power data</param>
        /// <param name="chartWidth">Chart width for positioning calculations</param>
        public void UpdateData(IEnumerable<FskAutoTuner.FrequencyClusterPower> frequencies, double chartWidth = 800)
        {
            if (frequencies == null)
            {
                BarData = new List<FrequencyBarData>();
                return;
            }

            var freqList = frequencies.ToList();
            if (!freqList.Any())
            {
                BarData = new List<FrequencyBarData>();
                return;
            }

            // Find max power for normalization
            var maxPowerValue = freqList.Max(f => f.Power);
            var newMaxPowerValue = maxPowerValue > 0 ? maxPowerValue : 1.0;
            if (newMaxPowerValue > MaxPower) MaxPower = newMaxPowerValue;

            // Filter frequencies within range
            var filteredFreqs = freqList.Where(f => f.Frequency >= MinFrequency && f.Frequency <= MaxFrequency).ToList();

            var frequencyRange = MaxFrequency - MinFrequency;
            var barWidth = chartWidth > 0 ? chartWidth / frequencyRange : 1.0;

            var newBarData = filteredFreqs.Select(freq =>
            {
                var normalizedHeight = MaxPower > 0 ? freq.Power / MaxPower : 0;
                var xPosition = (freq.Frequency - MinFrequency) / frequencyRange * chartWidth;

                return new FrequencyBarData
                {
                    Frequency = freq.Frequency,
                    Power = freq.Power,
                    NormalizedHeight = normalizedHeight,
                    XPosition = xPosition
                };
            }).ToList();

            BarData = newBarData;
        }

        /// <summary>
        /// Clears all bar data
        /// </summary>
        public void Clear()
        {
            BarData = new List<FrequencyBarData>();
            MaxPower = 1.0;
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        /// <summary>
        /// Sets the plot title
        /// </summary>
        /// <param name="title">New plot title</param>
        public void SetTitle(string title)
        {
            Title = title;
        }

        /// <summary>
        /// Sets the demodulator operating frequency range
        /// </summary>
        /// <param name="minFreq">Minimum demodulator frequency</param>
        /// <param name="maxFreq">Maximum demodulator frequency</param>
        public void SetDemodulatorRange(double minFreq, double maxFreq)
        {
            MinDemodulatorFreq = minFreq;
            MaxDemodulatorFreq = maxFreq;
        }

        public void SetFrequencyRange(double minFrequency, double maxFrequency)
        {
            MinFrequency = minFrequency;
            MaxFrequency = maxFrequency;
        }

        public void Draw(IEnumerable<FskAutoTuner.FrequencyClusterPower> frequencies)
        {
            UpdateData(frequencies);
        }
    }
}