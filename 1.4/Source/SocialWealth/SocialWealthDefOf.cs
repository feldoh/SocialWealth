using RimWorld;
using Verse;

namespace SocialWealth;

[DefOf]
public static class SocialWealthDefOf
{
    // Remember to annotate any Defs that require a DLC as needed e.g.
    // [MayRequireBiotech]
    // public static GeneDef YourPrefix_YourGeneDefName;
    
    static SocialWealthDefOf() => DefOfHelper.EnsureInitializedInCtor(typeof(SocialWealthDefOf));
}
