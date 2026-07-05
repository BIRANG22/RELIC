using System;
using UnityEngine;

[Serializable]
public class BattleProjectileVfxEntry
{
    public string skillId;
    public GameObject missilePrefab;
    public GameObject impactPrefab;
    public VfxFlipType missileFlipType = VfxFlipType.None;
    public VfxFlipType impactFlipType = VfxFlipType.None;
    public BattleVfxSfxEntry missileSfx = new();
    public BattleVfxSfxEntry impactSfx = new();
    public float launchDelay;
    public float travelDuration = 0.25f;
    public float arrivalDistance = 0.05f;
    public float impactLifeTime = 2f;
    public Vector3 launchOffset;
    public Vector3 impactOffset;
}
