using System;
using UnityEngine;

[Serializable]
public class BattleStatusVfxSet
{
    [Header("Buff")]
    public BattleVfxEntry aimingVfx;
    public BattleVfxEntry armorVfx;
    public BattleVfxEntry blockVfx;
    public BattleVfxEntry focusVfx;
    public BattleVfxEntry powerVfx;
    public BattleVfxEntry rechargeVfx;
    public BattleVfxEntry recoverVfx;
    public BattleVfxEntry swiftVfx;
    public BattleVfxEntry thornsVfx;

    [Header("Debuff")]
    public BattleVfxEntry addictedVfx;
    public BattleVfxEntry bleedingVfx;
    public BattleVfxEntry burnVfx;
    public BattleVfxEntry corrosionVfx;
    public BattleVfxEntry grudgeVfx;
    public BattleVfxEntry vulnerableVfx;
    public BattleVfxEntry weakenVfx;

    public BattleVfxEntry Get(string effectId)
    {
        if (string.IsNullOrWhiteSpace(effectId))
            return null;

        return effectId.Trim() switch
        {
            "E_Aiming" => aimingVfx,
            "E_Armor" => armorVfx,
            "E_Block" => blockVfx,
            "E_Focus" => focusVfx,
            "E_Power" => powerVfx,
            "E_Recharge" => rechargeVfx,
            "E_Recover" => recoverVfx,
            "E_Swift" => swiftVfx,
            "E_Thorns" => thornsVfx,
            "E_Addicted" => addictedVfx,
            "E_Bleeding" => bleedingVfx,
            "E_Burn" => burnVfx,
            "E_Corrosion" => corrosionVfx,
            "E_Grudge" => grudgeVfx,
            "E_Vulnerable" => vulnerableVfx,
            "E_Weaken" => weakenVfx,
            _ => null
        };
    }
}
