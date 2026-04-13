using System;
using System.Collections.Generic;
using System.Text;

namespace Shared.EventBus
{
    /// <summary>
    /// A record type representing the reason for a Event, including a message describing the reason and optional additional data.
    /// This can be used to provide context for events published to the event bus, allowing subscribers to understand why an event
    /// occurred and any relevant information associated with it.
    /// </summary>
    /// <param name="Message"></param>
    /// <param name="Data"></param>
    public sealed record EventReason(string Message, object? Data = null);

}
