using RimWorld;

namespace SocialWealth;

public class CompProperties_AbilityConvertOrDie : CompProperties_AbilityConvert
{
    public CompProperties_AbilityConvertOrDie() => compClass = typeof(CompAbilityEffect_ConvertOrDie);
}
