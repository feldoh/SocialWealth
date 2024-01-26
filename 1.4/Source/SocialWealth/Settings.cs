using UnityEngine;
using Verse;

namespace SocialWealth;

public class Settings : ModSettings
{
    //Use Mod.settings.setting to refer to this setting.
    public float neutralWealth = 100000f;
    public float changeThreshold = 0.05f;
    public string neutralWealthBuffer = "";
    public string changeThresholdBuffer = "";

    public void DoWindowContents(Rect wrect)
    {
        Listing_Standard options = new();
        options.Begin(wrect);
        
        options.TextFieldNumericLabeled("SocialWealth_Settings_NeutralWealth".Translate(), ref neutralWealth, ref neutralWealthBuffer, 0f, 10000000f);
        options.TextFieldNumericLabeled("SocialWealth_Settings_ChangeThreshold", ref changeThreshold, ref changeThresholdBuffer, 0f, 1f);
        options.Gap();

        options.End();
    }
    
    public override void ExposeData()
    {
        Scribe_Values.Look(ref neutralWealth, "neutralWealth", 100000);
        Scribe_Values.Look(ref changeThreshold, "changeThreshold", 0.05f);
    }
}
