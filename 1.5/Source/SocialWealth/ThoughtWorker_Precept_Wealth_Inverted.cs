using Verse;

namespace SocialWealth;

class ThoughtWorker_Precept_Wealth_Inverted : ThoughtWorker_Precept_Wealth
{
    public override int ThoughtStageIndex(Pawn p)
    {
        float wealthFactor = SocialWealthMod.settings.WealthFactor(null);
        for (int index = 0; index <= StageByWealthFactor.Length; index++)
        {
            if (wealthFactor <= StageByWealthFactor[index])
                return index + 1;
        }

        return StageByWealthFactor.Length + 1;
    }
}