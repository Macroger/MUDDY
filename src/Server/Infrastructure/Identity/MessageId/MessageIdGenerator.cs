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
            int next = System.Threading.Interlocked.Increment(ref _counter);

            // Add a prefix to help distinguish connections from other elements in the logs
            string value = $"msg-{next}";

            return new Shared.Identity.MessageId(value);
        }
    }
}
