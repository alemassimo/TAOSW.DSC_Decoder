using System;
using System.Collections.Generic;
using System.Linq;
using TAOSW.DSC_Decoder.Core.Domain;

public class DscMessageClusterizer
{
    private readonly TimeSpan _timeInterval;
    private readonly List<DSCMessage> _messageBuffer;
    private System.Timers.Timer _timer;

    public event Action<DSCMessage> OnClusteredMessageSelected;

    public DscMessageClusterizer(TimeSpan timeInterval)
    {
        _timeInterval = timeInterval;
        _messageBuffer = new List<DSCMessage>();

        // create a timer to clear the buffer periodically
        _timer = new System.Timers.Timer(_timeInterval.TotalMilliseconds);

        _timer.Elapsed += EvaluateBuffer();
    }

    private System.Timers.ElapsedEventHandler EvaluateBuffer()
    {
        return (sender, e) =>
        {
            // Clear the buffer if no messages have been added in the last time interval
            if (_messageBuffer.Count > 0)
            {

                var selectedMessage = _messageBuffer.OrderByDescending(m => m.Symbols.Count(s => s != -1))
                    .FirstOrDefault();
                if (selectedMessage != null)
                {
                    OnClusteredMessageSelected?.Invoke(selectedMessage);
                }
            }
            _messageBuffer.Clear();
            _timer.Stop();
        };
        
    }

    public void AddMessage(DSCMessage message)
    {
        _messageBuffer.Add(message);
        _timer.Start();
    }

       
    
}
