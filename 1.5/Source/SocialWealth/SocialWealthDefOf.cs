using RimWorld;
using Verse;

namespace SocialWealth;

[DefOf]
public static class SocialWealthDefOf
{
    // Remember to annotate any Defs that require a DLC as needed e.g.
    // [MayRequireBiotech]
    public static RulePackDef SocialWealth_Sentence_ConvertOrDie_Success;
    public static RulePackDef SocialWealth_Sentence_ConvertOrDie_Failure;

    static SocialWealthDefOf() => DefOfHelper.EnsureInitializedInCtor(typeof(SocialWealthDefOf));
}
