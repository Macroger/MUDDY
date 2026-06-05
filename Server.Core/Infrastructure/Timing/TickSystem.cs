using Shared.EventBus;
using System;
using System.Collections.Generic;
using System.Text;

namespace Server.Core.Infrastructure.Timing
{
    public class TickSystem: ITickSystem
    {
        /// <summary>
        /// A reference to the eventBus to publish tick events to.
        /// </summary>
        private IEventBus _eventBus;

        /// <summary>
        /// The current tick count.
        /// </summary>
        private long _currentTick = 0;

        /// <summary>
        /// Ticks per second
        /// </summary>
        private int _tickRate; 

        /// <summary>
        /// Indicates whether the tick system is currently running (producing ticks).
        /// </summary>
        private bool _isRunning = false;

        public TickSystem(IEventBus eventBus, int tickRate = 10)
        {
            _eventBus = eventBus;
            _tickRate = tickRate;
        }

        public void Start()
        {
            // If the system is already running, we throw an exception.
            if (_isRunning) 
                throw new InvalidOperationException("Tick system is already running.");

            _isRunning = true;

            RunTickLoop();
        }        

        public void Stop()
        {
            // If the system is not running, we can ignore the stop request.
            if (!_isRunning) return;

            _isRunning = false;
        }

        private async void RunTickLoop()
        {
            while(_isRunning)
            {
                // Publish a tick event with the current tick count.
                _eventBus.Publish(new TickEvent(_currentTick));
                // Increment the tick count for the next tick.
                _currentTick++;
                // Wait for the next tick based on the tick rate.
                await Task.Delay(1000 / _tickRate);
            })
        }
    }
}
