using System.Globalization;
using UnityEngine;
using Verse;

namespace SocialWealth;

public class Settings : ModSettings
{
    public const float _defaultNaturalWealth = 200000f;
    public const float _defaultChangeThreshold = 0.05f;
    public const float _defaultConvertOrDieMin = 0.2f;
    public const float _defaultConvertOrDieMax = 0.7f;

    private float neutralWealth = -1f;
    private float changeThreshold = -1f;
    private float convertOrDieMinChance = 0.2f;
    private float convertOrDieMaxChance = 0.7f;
    public string neutralWealthBuffer = "";

    public float NeutralWealth => neutralWealth <= 0f ? _defaultNaturalWealth : neutralWealth;
    public float ChangeThreshold => changeThreshold <= 0f ? _defaultChangeThreshold : changeThreshold;

    public float ConvertOrDieMinChance => convertOrDieMinChance < 0f ? _defaultConvertOrDieMin : convertOrDieMinChance;
    public float ConvertOrDieMaxChance => convertOrDieMaxChance < 0f ? _defaultConvertOrDieMax : convertOrDieMaxChance;

    public float WealthFactor(Map map) => (map ?? Find.AnyPlayerHomeMap).wealthWatcher.WealthTotal / NeutralWealth;

    public void DoWindowContents(Rect wrect)
    {
        Listing_Standard options = new();
        options.Begin(wrect);
        if (changeThreshold <= 0f) changeThreshold = _defaultChangeThreshold;
        if (convertOrDieMinChance < 0f) convertOrDieMinChance = _defaultConvertOrDieMin;
        if (convertOrDieMaxChance < 0f) convertOrDieMaxChance = _defaultConvertOrDieMax;
        neutralWealthBuffer = NeutralWealth.ToString(CultureInfo.InvariantCulture);
        options.TextFieldNumericLabeled("SocialWealth_Settings_NeutralWealth".Translate(), ref neutralWealth, ref neutralWealthBuffer, 0f, 10000000f);
        changeThreshold = options.SliderLabeled("SocialWealth_Settings_ChangeThreshold".Translate(changeThreshold.ToStringPercent("0.###")), changeThreshold, 0f, 1f,
            tooltip: "SocialWealth_Settings_ChangeThreshold_Tip".Translate());
        convertOrDieMinChance = options.SliderLabeled("SocialWealth_Settings_ConvertOrDieMinChance".Translate(convertOrDieMinChance.ToStringPercent()), convertOrDieMinChance, 0f, 1f,
            tooltip: "SocialWealth_Settings_ConvertOrDieMinChance_Tip".Translate());
        convertOrDieMaxChance = options.SliderLabeled("SocialWealth_Settings_ConvertOrDieMaxChance".Translate(convertOrDieMaxChance.ToStringPercent()), convertOrDieMaxChance, 0f, 1f,
            tooltip: "SocialWealth_Settings_ConvertOrDieMaxChance_Tip".Translate());
        options.Gap();

        options.End();
    }

    public override void ExposeData()
    {
        Scribe_Values.Look(ref neutralWealth, "neutralWealth", _defaultNaturalWealth);
        Scribe_Values.Look(ref changeThreshold, "changeThreshold", _defaultChangeThreshold);
        Scribe_Values.Look(ref convertOrDieMinChance, "convertOrDieMinChance", _defaultConvertOrDieMin);
        Scribe_Values.Look(ref convertOrDieMaxChance, "convertOrDieMaxChance", _defaultConvertOrDieMax);

        if (Scribe.mode != LoadSaveMode.PostLoadInit) return;
        if (neutralWealth <= 0f) neutralWealth = _defaultNaturalWealth;
        if (changeThreshold <= 0f) changeThreshold = _defaultChangeThreshold;
        if (convertOrDieMinChance < 0f) convertOrDieMinChance = _defaultConvertOrDieMin;
        if (convertOrDieMaxChance < 0f) convertOrDieMaxChance = _defaultConvertOrDieMax;
        neutralWealthBuffer = NeutralWealth.ToString(CultureInfo.InvariantCulture);
    }
}
