using System;
using System.Collections.Generic;
using System.Text;

namespace Server.Core.Infrastructure.Timing
{
    public class ScheduledTask : IScheduledTask
    {
        private readonly Action _action;
        public long ScheduledTick { get; private set; }
        public bool IsCancelled { get; private set; } = false;
        public bool IsCompleted { get; private set; } = false;
        public bool IsRecurring { get; }
        public long Interval { get; }

        public ScheduledTask(long scheduledTick, Action action, bool recurring, long interval)
        {
            ScheduledTick = scheduledTick;
            _action = action;
            IsRecurring = recurring;
            Interval = interval;
        }

        public void Cancel()
        {
            IsCancelled = true;
        }

        public void Execute()
        {
            _action();
            if (!IsRecurring)
            {
                IsCompleted = true;
            }
        }

        public void UpdateScheduledTick(long newTick)
        {
            ScheduledTick = newTick;
        }
    }
}
