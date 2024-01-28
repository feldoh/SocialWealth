using RimWorld;
using Verse;

namespace SocialWealth;

public class ThoughtWorker_Precept_Wealth : ThoughtWorker_Precept
{
    readonly float[] DefaultStageByWealthFactor =
    [
        0.2f,
        0.5f,
        1,
        2,
        4
    ];

    public virtual float[] StageByWealthFactor => DefaultStageByWealthFactor;

    protected override ThoughtState ShouldHaveThought(Pawn p)
    {
        return !ModsConfig.IdeologyActive || !p.IsColonistPlayerControlled
            ? ThoughtState.Inactive
            : ThoughtState.ActiveAtStage(ThoughtStageIndex(p));
    }

    public virtual int ThoughtStageIndex(Pawn p)
    {
        float wealthFactor = SocialWealthMod.settings.WealthFactor(null);
        for (int index = StageByWealthFactor.Length - 1; index >= 0; --index)
        {
            if (wealthFactor >= StageByWealthFactor[index])
                return index + 2;
        }

        return 1;
    }
}
