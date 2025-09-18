using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TAOSW.DSC_Decoder.UI.Model
{
    public class DSCMessageDataView
    {
        public ObservableCollection<DSCMessage> Messages { get; set; }

        public DSCMessageDataView()
        {
            Messages = new ObservableCollection<DSCMessage>();
        }

        public void AddMessage(DSCMessage message)
        {
            if (message != null)
            {
                Messages.Add(message);
            }
        }
    }
}
