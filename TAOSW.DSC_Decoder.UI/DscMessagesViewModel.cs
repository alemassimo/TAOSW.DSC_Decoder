using System.Collections.ObjectModel;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using TAOSW.DSC_Decoder.Core.Domain;

namespace TAOSW.DSC_Decoder.UI
{
    public class DscMessagesViewModel
    {
        public ObservableCollection<DSCMessage> Messages { get; } = new();
        
        public void AddMessage(DSCMessage message) => Messages.Insert(0, message);
        
        public void ClearMessages() => Messages.Clear();
        
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
