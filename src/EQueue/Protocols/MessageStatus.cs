using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace EQueue.Protocols
{
    public enum MessageStatus
    {
        Pending = 0,
        Waiting = 1,
        Sent = 2,
        Canceled = 3
    }
}
