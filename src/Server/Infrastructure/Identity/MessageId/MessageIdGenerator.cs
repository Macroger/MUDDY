using System;
using System.Collections.Generic;
using System.Text;

namespace Server.Infrastructure.Identity.MessageId
{
    public sealed class MessageIdGenerator: IMessageIdGenerator
    {
        private static int _counter = 0;
        public Shared.Identity.MessageId New()
        {
            // Interlocked.Increment returns the incremented value, so we can directly cast it to UInt32.
            UInt32 next = (UInt32)System.Threading.Interlocked.Increment(ref _counter);

            // Return a new MessageId struct with the generated value.
            return new Shared.Identity.MessageId(next);
        }
    }
}
