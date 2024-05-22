using System.Collections.Generic;
using RimWorld;
using Verse;

namespace SocialWealth;

public class StatPart_Wealth : StatPart
{
    private Dictionary<Def, SimpleCurve> defCurveCache = new();
    public SimpleCurve curve;
    public bool allowForNonPlayer;

    public SimpleCurve Curve => curve ??=
    [
        new CurvePoint(0f, 0.5f),
        new CurvePoint(1f, 1f),
        new CurvePoint(2f, 1.5f),
        new CurvePoint(4f, 2f),
        new CurvePoint(8f, 3f),
        new CurvePoint(16f, 4f)
    ];

    public SimpleCurve CurveFor(Def def)
    {
        if (def is null) return Curve;
        defCurveCache.TryGetValue(def, out SimpleCurve cachedCurve);
        if (cachedCurve is not null) return cachedCurve;
        cachedCurve = def.GetModExtension<CurveModExtension>() is { } modExt
            ? modExt.curve
            : Curve;
        defCurveCache.Add(def, cachedCurve);

        return cachedCurve;
    }

    public bool ShouldApply(StatRequest req) =>
        (req.Pawn?.Faction
         ?? req.Thing?.Faction
         ?? (req.Thing?.ParentHolder as Pawn_EquipmentTracker)?.pawn?.Faction)?.IsPlayer
        ?? false;

    public override void TransformValue(StatRequest req, ref float val)
    {
        if (ShouldApply(req) && Find.AnyPlayerHomeMap is { } map) val *= WealthMultiplier(req, map);
    }

    public override string ExplanationPart(StatRequest req)
    {
        return ShouldApply(req) && Find.AnyPlayerHomeMap is { } map
            ? "Wealth: x" + WealthMultiplier(req, map).ToStringPercent()
            : "";
    }

    public float WealthMultiplierForDef(Def def, Map map)
    {
        return CurveFor(def).Evaluate(SocialWealthMod.settings.WealthFactor(map));
    }

    public float WealthMultiplier(StatRequest req, Map map)
    {
        return WealthMultiplierForDef(req.Def, map);
    }
}
