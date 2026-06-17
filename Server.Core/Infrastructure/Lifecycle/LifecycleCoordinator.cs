// Copyright 2026 Matthew Schatz
// SPDX-License-Identifier: Apache-2.0
using Server.Core.Infrastructure.Events;
using Shared.EventBus;
using Shared.EventBus.EventTypes;

namespace Server.Core.Infrastructure.Lifecycle
{
    public sealed class LifecycleCoordinator : IServerLifecycle
    {
        private readonly List<IStartable> _startableItems = new();
        private readonly List<IStoppable> _stoppableItems = new();
        private readonly List<IShutdownAware> _shutdownAwareItems = new();

        private volatile ServerStateEnum _previousState = ServerStateEnum.LOADING;
        private volatile ServerStateEnum _currentState = ServerStateEnum.LOADING;

        private IEventBus _eventBus;
        public bool IsLoading => (_currentState == ServerStateEnum.LOADING);

        public bool IsActive => (_currentState == ServerStateEnum.ACTIVE);

        public bool IsShuttingDown => (_currentState == ServerStateEnum.SHUTTING_DOWN);

        public bool IsInMaintenance => (_currentState == ServerStateEnum.MAINTENANCE);

        public ServerStateEnum CurrentState => _currentState;

        public LifecycleCoordinator(IEventBus eventBus)
        {
            _eventBus = eventBus;
        }

        /// <summary>
        /// Start all items in the list of startables.
        /// </summary>
        public bool StartServer()
        {
            // Check if transitioning to active is valid. If not, return false.
            if (!TryTransition(_currentState, ServerStateEnum.ACTIVE)) return false;

            foreach (var startable in _startableItems)
            {
                startable.Start();
            }

            return true;
        }


        /// <summary>
        /// Stop all the items in the list of stoppables, then send a signal to all the shutdownAware items.
        /// </summary>
        public bool ShutdownServer()
        {
            // Check if the transition state is valid. If we are already shutting down it is not valid.
            if (!TryTransition(_currentState, ServerStateEnum.SHUTTING_DOWN)) return false;

            foreach (var stoppable in _stoppableItems)
            {
                stoppable.Stop();
            }

            foreach (var aware in _shutdownAwareItems)
            {
                aware.OnShutdown();
            }

            return true;
        }

        /// <summary>
        /// Registers an object into the startables list.
        /// </summary>
        /// <param name="startable"> Item to be registered as a startable.</param>
        public void RegisterStartableItem(IStartable startable)
        {
            _startableItems.Add(startable);
        }

        /// <summary>
        /// Registers an object into the stoppables list.
        /// </summary>
        /// <param name="stoppable"> Item to be registered as stoppable.</param>
        public void RegisterStoppableItem(IStoppable stoppable)
        {
            _stoppableItems.Add(stoppable);
        }

        /// <summary>
        /// Registers an object into the shutdown aware list.
        /// </summary>
        /// <param name="shutdownAware">
        /// Item to be registered as shutdown aware. This item will be sent a signal when the server is shutting down.
        /// </param>
        public void RegisterShutdownAwareItem(IShutdownAware shutdownAware)
        {
            _shutdownAwareItems.Add(shutdownAware);
        }

        /// <summary>
        /// Attempts to transition the server to the specified state from the current state.
        /// Uses the same allowed-transitions table as all other state changes.
        /// </summary>
        /// <param name="newState">The desired target state.</param>
        /// <returns>True if the transition was valid and applied; false otherwise.</returns>
        public bool SetState(ServerStateEnum newState)
        {
            return TryTransition(_currentState, newState);
        }

        /// <summary>
        /// Attempts to transition the server's state from an expected value to a new value.
        /// </summary>
        /// <param name="expected">The current expected state.</param>
        /// <param name="next">The requested state to change to.</param>
        /// <returns>True if transition succeeded; false if the current state did not match expected.</returns>
        public bool TryTransition(ServerStateEnum expected, ServerStateEnum next)
        {
            // Check if the caller has provided a valid expected state.
            if (_currentState != expected) return false;

            // Check if this transition is valid by checking the allowed transitions hash set.
            if (!AllowedTransitions.Contains((expected, next))) return false;

            // All validations good - record previous state and set new state.
            _previousState = _currentState;
            _currentState = next;

            // Fire off the state changed event to notify any listeners of the new state.
            _eventBus.Publish(EventMessageType.System, new  SystemEvents.Lifecycle.ServerStateChangedEvent(_previousState, next));

            return true;
        }

        /// <summary>
        /// This hash set defines the allowed states for the server, and provides a way to map valid transitions between them.
        /// </summary>
        private static readonly HashSet<(ServerStateEnum from, ServerStateEnum to)> AllowedTransitions = new()
        {
            // From LOADING - this state change is allowed.
            (ServerStateEnum.LOADING, ServerStateEnum.ACTIVE),
    
            // From ACTIVE - these states are allowed.
            (ServerStateEnum.ACTIVE, ServerStateEnum.MAINTENANCE),
            (ServerStateEnum.ACTIVE, ServerStateEnum.SHUTTING_DOWN),
    
            // From MAINTENANCE - these states are allowed.
            (ServerStateEnum.MAINTENANCE, ServerStateEnum.ACTIVE),
            (ServerStateEnum.MAINTENANCE, ServerStateEnum.SHUTTING_DOWN),
    
            // From SHUTTING_DOWN - no state changes allowed.
        };

    }
}
