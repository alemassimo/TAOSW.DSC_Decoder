using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using TAOSW.DSC_Decoder.Core.Domain;

namespace TAOSW.DSC_Decoder.UI
{
    public class DscMessagesViewModel : INotifyPropertyChanged
    {
        private bool _excludeTestMessages;

        public ObservableCollection<DSCMessage> Messages { get; } = new();
        public ObservableCollection<DSCMessage> VisibleMessages { get; } = new();

        public bool ExcludeTestMessages
        {
            get => _excludeTestMessages;
            set
            {
                if (_excludeTestMessages == value) return;

                _excludeTestMessages = value;
                RefreshVisibleMessages();
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(ExcludeTestMessages)));
            }
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        public void AddMessage(DSCMessage message)
        {
            Messages.Insert(0, message);
            if (ShouldShowMessage(message))
                VisibleMessages.Insert(0, message);
        }

        public void ClearMessages()
        {
            Messages.Clear();
            VisibleMessages.Clear();
        }

        private bool ShouldShowMessage(DSCMessage message) =>
            !_excludeTestMessages || !message.TC1.ToString().Contains("test", StringComparison.OrdinalIgnoreCase);

        private void RefreshVisibleMessages()
        {
            VisibleMessages.Clear();
            foreach (var message in Messages)
            {
                if (ShouldShowMessage(message))
                    VisibleMessages.Add(message);
            }
        }
        
        public async Task SaveToCsvAsync(string filePath)
        {
            var csv = new StringBuilder();
            
            // Add CSV header
            csv.AppendLine("Time,Category,Format,From,To,Frequency,Position,TC1,TC2,EOS,CECC,Status,NatureDescription");
            
            // Add data rows
            foreach (var message in Messages)
            {
                csv.AppendLine($"{EscapeCsvValue(message.Time?.ToString("yyyy-MM-dd HH:mm:ss") ?? "")}," +
                              $"{EscapeCsvValue(message.Category.ToString())}," +
                              $"{EscapeCsvValue(message.Format.ToString())}," +
                              $"{EscapeCsvValue(message.From ?? "")}," +
                              $"{EscapeCsvValue(message.To ?? "")}," +
                              $"{EscapeCsvValue(message.Frequency ?? "")}," +
                              $"{EscapeCsvValue(message.Position ?? "")}," +
                              $"{EscapeCsvValue(message.TC1.ToString())}," +
                              $"{EscapeCsvValue(message.TC2.ToString())}," +
                              $"{EscapeCsvValue(message.EOS.ToString())}," +
                              $"{EscapeCsvValue(message.CECC.ToString())}," +
                              $"{EscapeCsvValue(message.Status ?? "")}," +
                              $"{EscapeCsvValue(message.NatureDescription ?? "")}");
            }
            
            await File.WriteAllTextAsync(filePath, csv.ToString(), Encoding.UTF8);
        }
        
        private static string EscapeCsvValue(string value)
        {
            if (string.IsNullOrEmpty(value))
                return "";
                
            // Escape quotes and wrap in quotes if necessary
            if (value.Contains(',') || value.Contains('"') || value.Contains('\n') || value.Contains('\r'))
            {
                value = value.Replace("\"", "\"\"");
                return $"\"{value}\"";
            }
            
            return value;
        }
    }
}
