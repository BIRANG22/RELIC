# Lobby Relic Shop Single Purchase Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Limit each lobby relic shop runtime to one successful relic purchase and block all later purchases and rerolls.

**Architecture:** A pure shared policy derives purchase completion from the intersection of current offer IDs and owned relic IDs. Purchase, refresh, and presenter boundaries consume that policy so UI refreshes and direct service calls cannot bypass the limit.

**Tech Stack:** Unity 6, C#, NUnit EditMode tests, uGUI

## Global Constraints

- Documents are written only under `AI_Docs`.
- Tests are written only under `Assets/Tests/EditMode~/`.
- Unity batchmode tests are not attempted while the editor is open.
- No branch, worktree, commit, Push, or PR operation is performed.
- Existing host-authoritative lobby synchronization remains unchanged.

---

### Task 1: Purchase Limit Policy and Service Guards

**Files:**
- Create: `Assets/Project/Scripts/Gameplay/Scene/Lobby/RelicShop/LobbyRelicShopPurchaseLimit.cs`
- Modify: `Assets/Project/Scripts/Gameplay/Scene/Lobby/RelicShop/LobbyRelicPurchaseService.cs`
- Modify: `Assets/Project/Scripts/Gameplay/Scene/Lobby/RelicShop/LobbyRelicRefreshService.cs`
- Modify: `Assets/Tests/EditMode~/LobbyRelicShopServiceTests.cs`

**Interfaces:**
- Produces: `LobbyRelicShopPurchaseLimit.HasPurchasedOffer(LobbyRuntimeData runtime)`.
- Produces: `LobbyRelicPurchaseFailure.PurchaseLimitReached` and `LobbyRelicRefreshFailure.PurchaseLimitReached`.

- [ ] Add failing tests for a second purchase and a refresh after one offered relic is owned.
- [ ] Compile the tests and confirm the missing policy/failure behavior.
- [ ] Implement the shared policy and guard both services before any mutation.
- [ ] Compile runtime and test assemblies.

### Task 2: Presenter-Wide Lock

**Files:**
- Modify: `Assets/Project/Scripts/Gameplay/Scene/Lobby/RelicShop/LobbyRelicShopPresenter.cs`
- Modify: `Assets/Tests/EditMode~/LobbyRelicOfferButtonUITests.cs`

**Interfaces:**
- Consumes: `LobbyRelicShopPurchaseLimit.HasPurchasedOffer(runtime)`.
- Produces: purchased slot Sold state, all other offer buttons disabled, refresh disabled.

- [ ] Add a failing presenter regression test for all-offer and reroll lock.
- [ ] Render offers using the shared purchase-complete state.
- [ ] Refresh the entire offer presentation after a successful purchase.
- [ ] Compile runtime and editor assemblies and inspect the final diff.
