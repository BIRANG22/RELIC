using System;
using Relic.Gameplay.Data;
using UnityEngine;

// Compatibility component for existing scenes. Party consumables do not select cells.
public class ActiveRelicTargetingController : MonoBehaviour
{
    public bool IsTargeting => false;
    public bool BeginTargeting(ActiveRelicService activeRelicService, CharacterRuntimeData runtime,
        ActiveRelicAvailability availability, Action succeededCallback = null) => false;
    public bool BeginConsumableTargeting(ActiveRelicService activeRelicService, BattleRuntimeData battle,
        UseConsumableCommand command, ActiveRelicAvailability availability,
        Action succeededCallback = null) => false;
    public void CancelTargeting() { }
}
