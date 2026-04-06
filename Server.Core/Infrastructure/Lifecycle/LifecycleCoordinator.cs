using System;
using System.Collections.Generic;
using System.Text;

namespace Server.Core.Infrastructure.Lifecycle
{
    public sealed class LifecycleCoordinator
    {
        private readonly List<IStartable> _startableItems = new();
        private readonly List<IStoppable> _stoppableItems = new();
        private readonly List<IShutdownAware> _shutdownAwareItems = new();


        public void Start()
        {
            foreach (var startable in _startables)
            {
                startable.Start();
            }
        }


        public void Shutdown()
        {
            foreach (var stoppable in _stoppables)
            {
                stoppable.Stop();
            }

            foreach (var aware in _shutdownAware)
            {
                aware.OnShutdown();
            }
        }

        public void RegisterStartableItem(IStartable startable)
        {
            _startableItems.Add(startable);
        }

        public void RegisterStoppableItem(IStoppable stoppable)
        {
            _stoppableItems.Add(stoppable);
        }

        public void RegisterShutdownAwareItem(IShutdownAware shutdownAware)
        {
            _shutdownAwareItems.Add(shutdownAware);
        }

    }
}
