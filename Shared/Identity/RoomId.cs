using System;
using System.Collections.Generic;
using System.Text;

namespace Shared.Identity
{
    /// <summary>
    /// A wrapper to represent the unique identifier for a room in the game world.
    /// This helps prevent accidentally using a room ID in place of another string value, and vice versa.
    /// </summary>
    public readonly struct RoomId
    {
        public string Value { get; }

        public RoomId(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
                throw new ArgumentException("RoomId cannot be empty.", nameof(value));

            Value = value;
        }
    }
}
