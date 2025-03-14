using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TAOSW.DSC_Decoder.Core.Domain
{
    public class AudioDeviceInfo
    {
        public int DeviceNumber { get; set; }
        public string ProductName { get; set; }

        public override string ToString()
        {
            return $"{DeviceNumber}: {ProductName}";
        }
    }
}
