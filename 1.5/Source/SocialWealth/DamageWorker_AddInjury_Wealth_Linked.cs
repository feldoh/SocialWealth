using System;
using RimWorld;
using Verse;

namespace SocialWealth;

/**
 * We can't really calculate the wealth of the NPCs so we just don't apply the multiplier.
 * The actual multiplier is in the stat part so it shows up in the stats window so we need to reverse the multiplier here for NPCs.
 */
public class DamageWorker_AddInjury_Wealth_Linked : DamageWorker_AddInjury
{
    Lazy<StatPart_Wealth> StatPartWealth = new(() => StatDefOf.RangedWeapon_DamageMultiplier.GetStatPart<StatPart_Wealth>());
    public override DamageResult Apply(DamageInfo dinfo, Thing victim)
    {
        if (dinfo.Instigator is Pawn instigator && !instigator.Faction.IsPlayer)
        {
            float wealthMultiplierForDef = StatPartWealth.Value?.WealthMultiplierForDef(dinfo.Weapon, null) ?? 1f;
            dinfo.SetAmount(dinfo.Amount / wealthMultiplierForDef);
        }
        return base.Apply(dinfo, victim);
    }
}
