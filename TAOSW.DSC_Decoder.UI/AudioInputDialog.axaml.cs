using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using TAOSW.DSC_Decoder.Core.Domain;

namespace TAOSW.DSC_Decoder.UI;

public partial class AudioInputDialog : Window
{
    public AudioDeviceInfo SelectedDevice { get; private set; }

    private ComboBox comboBox;

    public AudioInputDialog(IEnumerable<AudioDeviceInfo> audioDevices)
    {
        InitializeComponent();
        comboBox = this.FindControl<ComboBox>("DeviceComboBox");
        comboBox.ItemsSource = audioDevices;
    }

    public AudioInputDialog()
    {
        InitializeComponent();
        comboBox = this.FindControl<ComboBox>("DeviceComboBox");
        comboBox.ItemsSource = new Collection<AudioDeviceInfo>();
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }


    private void OnOkClick(object? sender, RoutedEventArgs e)
    {
        comboBox.SelectedItem ??= DeviceComboBox.Items[0]; // Default to the first item if none is selected
        SelectedDevice = ((AudioDeviceInfo?)comboBox.SelectedItem);
        Close(SelectedDevice);
    }

    private void OnCancelClick(object? sender, RoutedEventArgs e)
    {
        Close(null);
    }
}