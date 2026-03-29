using System;
using System.Collections.Generic;
using System.Text;

namespace Shared.Protocol.Types
{
    [Flags]
    public enum MessageFlags
    {
        None = 0,

        RequiresAuthentication = 1 << 0,
        Encrypted = 1 << 1,
        Compressed = 1 << 2,

        SystemMessage = 1 << 3
    }
}
