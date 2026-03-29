using System;
using System.Collections.Generic;
using System.Text;

namespace Shared.Protocol.Types
{
    public enum MessageType
    {
        Command = 0,   // Client → server request
        Response,      // Server → client reply to a command
        Event,         // Server → client unsolicited event
        Error,         // Error response (protocol or server-side)
        System         // Infrastructure / system-level message
    }

}
