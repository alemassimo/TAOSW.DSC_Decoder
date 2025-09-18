using System.Collections.ObjectModel;
using TAOSW.DSC_Decoder.Core.Domain;

namespace TAOSW.DSC_Decoder.UI
{
    public class DscMessagesViewModel
    {
        public ObservableCollection<DSCMessage> Messages { get; } = new();
        public void AddMessage(DSCMessage message) => Messages.Insert(0, message);
    }
}
