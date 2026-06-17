# Server GUI Refactor Plan: Alignment with Client GUI Pattern

**Status**: Planning phase complete, ready for implementation  
**Last Updated**: Current session  
**Branch**: `eventBus-strongly-typed-events-only`

---

## Context & Motivation

The server GUI (`Server.GUI\MainWindow.xaml.cs`) has grown organically and now exhibits mixed architectural patterns:
- Some subscriptions use strongly-typed event handlers (desired pattern)
- Some subscriptions use legacy `EventEnvelope` with runtime casting (anti-pattern)
- Contains debugging code and reflection-era helpers that no longer fit the architecture
- Event subscriptions are fragmented across multiple methods with inconsistent naming

The client GUI (`Client.GUI\MainWindow.xaml.cs`) demonstrates the target architecture:
- Single unified `SubscribeToEventBus()` method
- All subscriptions use strongly-typed event handlers
- Clear, concise comments explaining each subscription's purpose
- Consistent error handling via event publishing

**Goal**: Refactor server GUI to match the client's cohesive, strongly-typed pattern.

---

## Architectural Decisions Established

1. **Event Subscription Pattern**: All subscriptions must use strongly-typed `Subscribe<T>` with an explicit `EventMessageType` category
2. **Error Publishing**: User-facing errors and state changes are published onto the event bus, not silently logged
3. **Single Subscription Method**: All event bus subscriptions occur in one dedicated `SubscribeToEventBus()` method called from the constructor
4. **Dispatcher Safety**: All UI updates from background event handlers use `DispatcherQueue.TryEnqueue()`
5. **Event Log Management**: Use the named constant `MaxEventLogEntries` consistently; extract repetitive log-append logic into a helper
6. **No Catch-All Handlers**: No `OnAnyEventReceived` or reflection-based pattern matching on the UI thread

---

## Current State Summary

### What's Already Good
- File header with Doxygen-style documentation (`@file`, `@namespace`, `@brief`, `@details`)
- Named constant `MaxEventLogEntries = 100` for event log cap
- Proper XML doc comments on public binding properties (`ServerStateText`, `ServerStateBrush`, `ListenerStateText`, `ListenerStateBrush`, `Events`)
- Some strongly-typed subscriptions already in place: `ServerStateChangedEvent`, `ServerStateChangeRequestedEvent`, `ListenerStateChanged`, player events
- `ToEventEntry()` helper method for consistent event log entry formatting
- Proper `ISubscriptionToken` storage in a list field

### What Needs Removal (Legacy Code)
- **Lines 159–162**: `EventEnvelope` subscriptions with runtime casting handlers
  - `OnSystemEventReceived` (lines 186–204): tries to cast `EventEnvelope.Payload` to `EventReason`
  - `OnNetworkEventReceived` (lines 206–224): identical pattern
  - `OnAuthEventReceived` (lines 226–244): identical pattern
  - `OnErrorEventReceived` (lines 246–264): identical pattern
- **Lines 274–344**: `OnAnyEventReceived()` — catch-all handler with reflection-based pattern matching
  - Used for cross-cutting event logging but conflicts with strongly-typed model
- **Lines 346–351**: `FormatEventReason()` helper — only returns `reason.Message`, never used for data formatting
- **Lines 353–368**: `GetIdentityValue()` helper — reflection-based identity extraction, no longer needed
- **Lines 133, 139–144**: Inline lambda timers for UI updates (mixed concerns with event handling)
- **Excessive Debug Output**: Many `System.Diagnostics.Debug.WriteLine()` calls in handlers (lines 441, 447, 457, 466–471, 477–478)

### What Needs Clarification / Investigation
- **Event categories for server GUI notifications**: Should we create `Server.Core.Infrastructure.Events.ServerGuiEvents` to publish GUI-specific notifications (e.g., "player joined", "server state changed") so they can be logged by external observers?
- **Which events does the server GUI actually display?** The current code logs system, network, auth, and error messages, but it's unclear which ones come from the event bus vs. legacy sources.
- **Relationship with `WorldEvents`**: Player enter/leave events currently use `EventMessageType.World`. Is this the correct category, or should they have their own player-lifecycle category?

---

## Implementation Plan

### Phase 1: Identify Event Sources (Analysis)
**Objective**: Determine exactly which events the server GUI should subscribe to.

**Tasks**:
1. [ ] Review all current event subscriptions (lines 150–162)
2. [ ] Trace each subscription to understand what event type it's meant to display
3. [ ] Document the mapping: Event Type → Display Source → Handler
4. [ ] Identify which `EventMessageType` categories are actually used
5. [ ] Verify that `WorldEvents.Notifications.PlayerEnteredWorldEvent` and `PlayerLeftWorldEvent` are the only player-related events we need
6. [ ] Confirm whether system/network/auth/error logging should come from dedicated event types or from a catch-all

**Output**: A clear list of event subscriptions the server GUI should maintain (see "Target Subscription List" section below).

---

### Phase 2: Create Unified Subscription Method
**Objective**: Consolidate all subscriptions into a single, well-documented method.

**Tasks**:
1. [ ] Create a new `private void SubscribeToEventBus()` method in the constructor (after field initialization, before timers/UI setup)
2. [ ] Move all `_eventSubscriptions.Add(...)` calls into this method
3. [ ] Add clear XML doc comments or inline comments above each subscription explaining:
   - What event type it subscribes to
   - Which handler processes it
   - Why we care about this event
4. [ ] Remove the try-catch block around subscriptions (let errors bubble to the constructor's catch handler)
5. [ ] Call `SubscribeToEventBus()` once in the constructor after the event bus is assigned

**Example Pattern** (from client GUI):
```csharp
private void SubscribeToEventBus()
{
	// Subscribe to connection status changes
	_subscriptions.Add(_eventBus.Subscribe<ClientNetworkEvents.Lifecycle.ConnectionStatusChangedEvent>(
		eventType: EventMessageType.Network,
		handler: OnConnectionStatusChanged
	));

	// Subscribe to GUI errors
	_subscriptions.Add(_eventBus.Subscribe<ClientGuiEvents.Errors.GuiError>(
		eventType: EventMessageType.Gui,
		handler: OnGuiError
	));
}
```

---

### Phase 3: Replace Legacy Event Envelope Handlers
**Objective**: Remove the fragmented `OnSystemEventReceived`, `OnNetworkEventReceived`, `OnAuthEventReceived`, `OnErrorEventReceived` pattern.

**Tasks**:
1. [ ] Determine what types of events should actually be logged from each category (System, Network, Auth, Error)
2. [ ] Check whether we have strongly-typed event definitions for each, or if we need to create them
3. [ ] Replace `EventEnvelope` subscriptions with subscriptions to actual event types
4. [ ] Remove the four identical `OnXxxEventReceived` methods
5. [ ] If logging of raw events is needed, create a dedicated logger subscriber instead of GUI handlers

**Key Decision**: Should the server GUI be responsible for displaying system/network/auth/error messages, or should a separate logger service handle this?

---

### Phase 4: Remove Catch-All and Reflection Helpers
**Objective**: Eliminate `OnAnyEventReceived`, `FormatEventReason`, and `GetIdentityValue`.

**Tasks**:
1. [ ] Remove the subscription to `SubscribeAll` or `SubscribeToCategory` (if it exists)
2. [ ] Delete the `OnAnyEventReceived()` method entirely
3. [ ] Delete the `FormatEventReason()` helper method
4. [ ] Delete the `GetIdentityValue()` helper method
5. [ ] Verify that no remaining code references these deleted methods
6. [ ] Remove any debug `WriteLine` calls that are no longer needed

**Verification**: Build and ensure no compilation errors.

---

### Phase 5: Simplify and Consolidate Handlers
**Objective**: Create focused, minimal event handlers that follow the client GUI pattern.

**Tasks**:
1. [ ] Review each event handler method and ensure it:
   - Receives a strongly-typed event (not `EventEnvelope`)
   - Performs exactly one UI operation (update binding property, append to log, etc.)
   - Uses `DispatcherQueue.TryEnqueue()` for UI updates
   - Does NOT contain debug output or unnecessary comments
2. [ ] Consolidate repeated "add to event log" logic into a helper method:
   ```csharp
   private void AddEventLogEntry(string source, string message)
   {
	   DispatcherQueue.TryEnqueue(() =>
	   {
		   _events.Insert(0, ToEventEntry(DateTime.Now, source, message));
		   if (_events.Count > MaxEventLogEntries)
		   {
			   _events.RemoveAt(_events.Count - 1);
		   }
	   });
   }
   ```
3. [ ] Update all handlers to use this helper instead of inline logic
4. [ ] Remove excessive debug logging (keep only critical errors)
5. [ ] Ensure all debug output is removed from player event handlers

---

### Phase 6: Verify Event Categories
**Objective**: Ensure all subscriptions use the correct `EventMessageType` category.

**Tasks**:
1. [ ] Check `WorldEvents.Notifications.PlayerEnteredWorldEvent` subscription uses correct category
2. [ ] Check `WorldEvents.Notifications.PlayerLeftWorldEvent` subscription uses correct category
3. [ ] Verify that server state change events use `EventMessageType.System`
4. [ ] Verify that listener state events use `EventMessageType.Network`
5. [ ] Document why each category is chosen in inline comments

---

### Phase 7: Clean Up Initialization
**Objective**: Separate concerns in the constructor.

**Tasks**:
1. [ ] Ensure this constructor order:
   - `InitializeComponent()`
   - Button state setup (Mute, Kick disabled)
   - Event bus assignment and validation
   - Data source binding (ItemsSource)
   - Window sizing
   - Simple UI text updates (e.g., player count)
   - Timer setup
   - Event subscription (call `SubscribeToEventBus()`)
2. [ ] Use the named constant `MaxEventLogEntries` in the initial player count display
3. [ ] Remove nested try-catch; let exceptions propagate to the outer catch

---

### Phase 8: Final Verification & Testing
**Objective**: Ensure the refactored code is correct, builds, and maintains functionality.

**Tasks**:
1. [ ] Build the solution and verify no compilation errors
2. [ ] Run the server GUI and verify:
   - Server state changes display correctly in the event log
   - Listener state changes display correctly
   - Player enter/leave events display correctly
   - Window and UI elements appear as before
3. [ ] Check that no debug output appears in the output window during normal operation
4. [ ] Verify that event log respects the `MaxEventLogEntries` cap
5. [ ] Test player muting and kicking (if implemented)

---

## Target Subscription List

Based on the current code, the server GUI should subscribe to these events. **Verify with team before implementing:**

| Event Type | Category | Handler | Purpose |
|---|---|---|---|
| `ServerStateChangedEvent` | System | `OnServerStateChanged` | Update server state display |
| `ServerStateChangeRequestedEvent` | System | `OnServerStateChangeRequested` | Log state change requests |
| `ListenerStateChanged` | Network | `OnListenerStateChanged` | Update listener online/offline status |
| `WorldEvents.Notifications.PlayerEnteredWorldEvent` | World | `OnPlayerEnteredWorld` | Add player to active list, update count, log event |
| `WorldEvents.Notifications.PlayerLeftWorldEvent` | World | `OnPlayerLeftWorld` | Remove player from list, update count, log event |

---

## Known Issues / Open Questions

1. **EventEnvelope Legacy**: Why are system/network/auth/error messages being published as `EventEnvelope` instead of strongly-typed events? Should the event bus support these categories natively, or should we define specific event types?

2. **Debugging Output**: The player event handlers have extensive debug output (`Debug.WriteLine`). Is this for development only, or should we keep some for diagnostics?

3. **Event Log Sources**: Currently, the event log displays source as "System", "Network", "Auth", "Error", or "PlayerEvents". Should we use the event's full type name, a human-readable category, or a custom label defined per event?

4. **GUI Error Publishing**: Should the server GUI publish its own errors (e.g., "failed to update player list") onto the event bus for logging, similar to the client GUI? Or should it log internally?

5. **Listener Request Events**: The buttons `ToggleListenerButton_Click`, `SetActiveButton_Click`, etc. publish request events. Should these also be logged in the event display, or are they only for state change handlers to respond to?

---

## Files Involved

- **Main refactor target**: `Server.GUI\MainWindow.xaml.cs` (609 lines)
- **Reference implementation**: `Client.GUI\MainWindow.xaml.cs` (434 lines)
- **Event definitions to verify**:
  - `Server.Core\Infrastructure\Events\WorldEvents.cs` (player events)
  - `Server.Core\Infrastructure\Events\ServerSystemEvents.cs` or similar (server state events)
  - `Server.Core\Infrastructure\Events\NetworkEvents.cs` (listener state events)

---

## Execution Workflow

1. **Before starting**: Review this plan and resolve open questions (esp. Phase 1 analysis)
2. **During implementation**: Follow each phase in order; complete verification in each phase before moving to the next
3. **Between sessions**: Update this file with completion status and any blockers
4. **After completion**: Archive this file and update the session summary

---

## Completion Checklist

- [ ] Phase 1: Event sources identified and documented
- [ ] Phase 2: Unified subscription method created
- [ ] Phase 3: Legacy event envelope handlers removed
- [ ] Phase 4: Catch-all and reflection helpers deleted
- [ ] Phase 5: Handlers simplified and consolidated
- [ ] Phase 6: Event categories verified
- [ ] Phase 7: Initialization cleaned up
- [ ] Phase 8: Build and functional testing completed
- [ ] Code review: Plan reviewed and approved by team

---

## Notes for Future Sessions

If picking up this work in a future session:
1. Read the "Current State Summary" section for context
2. Check the completion checklist to see which phases are done
3. Start with the next incomplete phase
4. Update the "Last Updated" date at the top and add session notes below
5. If blockers are encountered, document them in the "Known Issues / Open Questions" section

---

## Session Notes

### Session 1 (Current)
- Analyzed both client and server GUI architectures
- Identified key differences and anti-patterns in server GUI
- Created this comprehensive plan with 8 phases
- Standing ready to implement Phase 1 analysis

