# TAOSW DSC Decoder

A .NET library for decoding DSC (Digital Selective Calling) messages from audio signals coming from SDR radios or HF receivers with USB (SSB) demodulation.

## Overview

This project provides a comprehensive solution for decoding DSC maritime communication messages. It includes both a console application and a GUI application built with Avalonia UI framework.

## Prerequisites

- .NET 8.0 or higher
- USB/SSB demodulated audio signal from a radio (SDR or HF receiver)
- PC audio device configured (microphone, line-in, virtual audio cable)

## Project Structure

- **TAOSW.DSC_Decoder.Core**: Core decoding library
- **TAOSW.DSC_Decoder**: Console application
- **TAOSW.DSC_Decoder.UI**: GUI application with Avalonia UI
- **Gateways**: Audio capture implementation
- **TAOSW.DSC_Decoder.Core.Tests**: Unit tests

## Quick Start

### Console Application

Run the console application:

```bash
dotnet run --project TAOSW.DSC_Decoder
```

The application will:
1. List available audio devices
2. Ask you to select a device number
3. Start listening for DSC messages
4. Display decoded messages in the console

### GUI Application

Run the GUI application:

```bash
dotnet run --project TAOSW.DSC_Decoder.UI
```

The GUI application will:
1. Show a dialog to select audio device
2. Display decoded messages in a grid
3. Play notification sounds for non-test messages

## Using the Core Library

### Basic Setup

```csharp
using TAOSW.DSC_Decoder.Core;
using TAOSW.DSC_Decoder.Core.Interfaces;

// Initialize components
int sampleRate = 88200;
IAudioCapture audioCapture = new AudioCapture(sampleRate);
FskAutoTuner autoTuner = new FskAutoTuner(700, 300, sampleRate, 170);
var squelchLevelDetector = new SquelchLevelDetector(0.0000001f, 0f);

// Get available audio devices
var devices = audioCapture.GetAudioCaptureDevices();
```

### Console Application Example

```csharp
class Program
{
    private const int SampleRate = 88200;

    static void Main(string[] args)
    {
        IAudioCapture audioCapture = new AudioCapture(SampleRate);
        FskAutoTuner autoTuner = new FskAutoTuner(700, 300, SampleRate, 170);
        var squelchLevelDetector = new SquelchLevelDetector(0.0000001f, 0f);
        var devices = audioCapture.GetAudioCaptureDevices();

        Console.WriteLine("Available audio capture devices:");
        foreach (var device in devices) Console.WriteLine(device);

        Console.WriteLine("Enter the device number to start capturing:");
        string? s = Console.ReadLine();

        if (int.TryParse(s, out int deviceNumber))
        {
            var manager = new DscMessageManager(audioCapture, autoTuner, 
                                              squelchLevelDetector, SampleRate);
            manager.OnClusteredMessageSelected += (message) =>
            {
                Console.WriteLine($"Time: {DateTime.UtcNow}");
                Console.WriteLine($"Message: {message}");
            };
            
            Console.WriteLine("Listening...");
            manager.Start(deviceNumber);
        }
    }
}
```

### GUI Application Example (Avalonia)

```csharp
private async Task StartDscReceiver()
{
    int sampleRate = 88200;
    IAudioCapture audioCapture = new AudioCapture(sampleRate);
    FskAutoTuner autoTuner = new FskAutoTuner(700, 300, sampleRate, 170);
    var squelchLevelDetector = new SquelchLevelDetector(0.0000001f, 0f);

    // Get audio devices and show selection dialog
    devices = new ObservableCollection<AudioDeviceInfo>(
        audioCapture.GetAudioCaptureDevices());
    int deviceNumber = await SelectDeviceFromDialogBox(devices.ToList());

    var manager = new DscMessageManager(audioCapture, autoTuner, 
                                      squelchLevelDetector, sampleRate);
    manager.OnClusteredMessageSelected += (message) =>
    {
        if (string.IsNullOrEmpty(message.Time)) 
            message.Time = DateTime.UtcNow.ToString("MM/dd/yy H:mm:ss");
        
        // Update UI on main thread
        Avalonia.Threading.Dispatcher.UIThread.Post(() => 
            _viewModel.AddMessage(message));
        
        // Audio notification for non-test messages
        if (message.TC1 != FirstCommand.Test)
            SystemSounds.Beep.Play();
    };
    
    manager.Start(deviceNumber);
}
```

## Core Components

### DscMessageManager
Main orchestrator component that coordinates all decoding processes.

**Parameters:**
- `IAudioCapture audioCapture`: Audio input interface
- `FskAutoTuner autoTuner`: FSK signal processing and auto-tuning
- `SquelchLevelDetector squelchLevelDetector`: Signal threshold detection
- `int sampleRate`: Audio sample rate (recommended: 88200 Hz)

### FskAutoTuner
Handles FSK (Frequency Shift Keying) signal processing and automatic frequency tuning.

**Constructor parameters:**
- `freq1`: First FSK frequency (typically 700 Hz)
- `freq2`: Second FSK frequency (typically 300 Hz)  
- `sampleRate`: Audio sample rate
- `baudRate`: Baud rate (typically 170 baud for DSC)

### SquelchLevelDetector
Detects when audio signal exceeds minimum threshold to trigger processing.

**Constructor parameters:**
- `threshold`: Minimum signal level threshold (e.g., 0.0000001f)
- `noiseLevel`: Background noise level (e.g., 0f)

### AudioCapture
Provides audio input from system devices.

**Methods:**
- `GetAudioCaptureDevices()`: Returns list of available audio devices
- `StartAudioCapture(deviceNumber)`: Starts capturing from specified device

## DSC Message Structure

Decoded messages are `DSCMessage` objects containing:

```csharp
public class DSCMessage
{
    public string? Frequency { get; set; }        // Operating frequency
    public List<int> Symbols { get; set; }        // Raw decoded symbols
    public FormatSpecifier Format { get; set; }   // Message format (SEL, ALL, etc.)
    public CategoryOfCall Category { get; set; }   // Call category (SAF, URG, ROU, etc.)
    public NatureOfDistress? Nature { get; set; } // Distress nature (if applicable)
    public string To { get; set; }                // Destination station ID
    public string From { get; set; }              // Source station ID
    public FirstCommand TC1 { get; set; }         // First telecommand
    public SecondCommand TC2 { get; set; }        // Second telecommand
    public string? Position { get; set; }         // Geographic position
    public string Time { get; set; }              // Message timestamp
    public EndOfSequence EOS { get; set; }        // End of sequence indicator
    public int CECC { get; set; }                 // Error correction check code
    public string Status { get; set; }            // Decoding status
}
```

## Audio Configuration

### Radio Setup
- **Mode**: USB (Upper Side Band) - **CRITICAL**
- **DSC Frequencies**: 
  - 2187.5 kHz (MF - Medium Frequency)
  - 4207.5 kHz, 6312.0 kHz, 8414.5 kHz, 12577.0 kHz, 16804.5 kHz (HF)
- **Bandwidth**: 3 kHz
- **Filters**: Disable excessive noise reduction that might affect DSC signals
- **AGC**: Set to slow or off to avoid amplitude variations

### PC Audio Setup
- **Sample Rate**: 88200 Hz (strongly recommended)
- **Audio Level**: Adjust to prevent clipping while maintaining good signal strength
- **Virtual Audio Cable**: Recommended for connecting SDR software to the decoder
- **Buffer Size**: Use appropriate buffer sizes to prevent dropouts

### Signal Quality Tips
- Ensure clean USB demodulation
- Avoid overdriving the audio input
- Monitor for audio distortion or clipping
- Use proper grounding to minimize noise
- Consider using audio isolators if needed

## Technical Features

- **Multi-window Processing**: Uses multiple parallel decoders for improved reliability
- **Automatic Error Correction**: Built-in FEC (Forward Error Correction)
- **Message Clustering**: Groups related message fragments
- **Auto-tuning**: Automatically adjusts to signal characteristics
- **FFT Analysis**: Advanced frequency domain processing
- **Signal Quality Detection**: Squelch and threshold management

## Example Output

```
TIME: 2025-01-15 14:30:22 FREQ: 8414.5
FMT: SEL (Selective Call)
CAT: SAF (Safety)
TO: COAST,002241022,ESP,Coruna Radio
FROM: SHIP,241348000,Unknown Vessel
TC1: TEST (Test Message)
TC2: NOINF (No Information)
FREQ: --
POS: --
EOS: REQ (Request)
cECC: 101 OK
```

## Troubleshooting

### No Messages Decoded
- Verify radio is in USB/SSB mode
- Check audio levels (not too low, not clipping)
- Ensure correct frequency tuning
- Verify audio device selection

### Poor Decoding Performance
- Check signal quality and antenna
- Reduce interference sources
- Adjust audio levels
- Try different audio devices
- Verify sample rate settings

### Audio Issues
- Check device permissions
- Verify audio device is not in use by other applications
- Test with different audio sources
- Check virtual audio cable configuration

### Common Errors
- **Device not found**: Check audio device availability and permissions
- **High error rates**: Usually indicates poor signal quality or incorrect demodulation
- **No audio input**: Verify device selection and audio routing

## Development

### Building the Project
```bash
dotnet build
```

### Running Tests
```bash
dotnet test
```

### Contributing
1. Fork the repository
2. Create a feature branch
3. Make your changes
4. Add tests if applicable
5. Submit a pull request

## License

This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.

Copyright © 2025 Tao Energy SRL. All rights reserved.

## Acknowledgments

This project implements DSC (Digital Selective Calling) protocols as defined by ITU-R M.493 and related maritime communication standards.

## Support

For issues, questions, or contributions, please visit the project repository or contact the maintainers.