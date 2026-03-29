using System;
using System.Collections.Generic;
using System.Text;

namespace Server.Infrastructure.Identity.ConnectionId
{
    public sealed class ConnectionIdGenerator : IConnectionIdGenerator
    {
        private static int _counter = 0;
        public Shared.Identity.ConnectionId New()
        {
           int nextId = System.Threading.Interlocked.Increment(ref _counter);

            // Add a prefix to help distinguish connections from other elements in the logs
            string value = $"conn-{nextId}";

            return new Shared.Identity.ConnectionId(value);
        }
    }
}
