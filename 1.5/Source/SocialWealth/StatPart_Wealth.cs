using RimWorld;
using Verse;

namespace SocialWealth;

public class StatPart_Wealth : StatPart
{
    private SimpleCurve curve;

    public SimpleCurve Curve => curve ??=
    [
        new CurvePoint(0f, 0.5f),
        new CurvePoint(1f, 1f),
        new CurvePoint(2f, 1.5f),
        new CurvePoint(4f, 2f),
        new CurvePoint(8f, 3f),
        new CurvePoint(16f, 4f)
    ];

    public override void TransformValue(StatRequest req, ref float val)
    {
        if (((req.Pawn ?? req.Thing as Pawn)?.Faction?.IsPlayer ?? false) && Find.AnyPlayerHomeMap is { } map)
        {
            val *= WealthMultiplier(req, map);
        }
    }

    public override string ExplanationPart(StatRequest req)
    {
        return ((req.Pawn ?? req.Thing as Pawn)?.Faction?.IsPlayer ?? false) && Find.AnyPlayerHomeMap is { } map ? "Wealth: x" + WealthMultiplier(req, map).ToStringPercent() : "";
    }

    private float WealthMultiplier(StatRequest req, Map map)
    {
        return Curve.Evaluate(SocialWealthMod.settings.WealthFactor(map));
    }
}
