# RELIC Script Review (Multiplayer Readiness)

Date: 2026-04-13

## Critical issues

1. EventBus unsubscribe did not remove registered handlers correctly.
   - Root cause: subscribe/unsubscribe used different lambda instances.
   - Impact: listener leak, duplicate callbacks, stale object references.
   - Fix: switched to `Delegate.Combine/Remove` with typed delegate storage.

2. Singleton lifecycle cleanup missing.
   - Root cause: `Instance` was never reset on object destruction.
   - Impact: stale static references after scene/domain transitions.
   - Fix: added `OnDestroy` cleanup when the destroyed object is current instance.

## High-priority multiplayer risks found (not yet fixed in this commit)

1. Client-authoritative input path.
   - `OnMouseDown` directly drives tile click and selection logic.
   - For multiplayer, this should become server-authoritative command flow.

2. Global singleton dependence for gameplay flow.
   - Several managers are process-global singletons and assume one world/session.
   - For multiplayer sessions/match instances, this can cause cross-session coupling.

3. Hard scene transitions in state machine.
   - State transitions directly trigger scene loads.
   - In multiplayer, scene synchronization and late-join handling need explicit network orchestration.

## Recommended next steps

1. Introduce command layer (Input -> Command -> Validation -> Apply).
2. Separate deterministic battle state from MonoBehaviour view layer.
3. Define ownership/authority rules per action (move/select/use skill).
4. Add disconnect/reconnect-safe state snapshot path.
5. Add multiplayer-safe event channels (local-only vs replicated events).
