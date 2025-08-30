using Verse;
using UnityEngine;
using HarmonyLib;

namespace SocialWealth;

public class SocialWealthMod : Mod
{
    public static Settings settings;

    public SocialWealthMod(ModContentPack content) : base(content)
    {
        Log.Message("Hello world from SocialWealth");

        // initialize settings
        settings = GetSettings<Settings>();

#if DEBUG
        Harmony.DEBUG = true;
#endif

        Harmony harmony = new("feldoh.rimworld.SocialWealth.main");
        harmony.PatchAll();
    }

    public override void DoSettingsWindowContents(Rect inRect)
    {
        base.DoSettingsWindowContents(inRect);
        settings.DoWindowContents(inRect);
    }

    public override string SettingsCategory()
    {
        return "SocialWealth_SettingsCategory".Translate();
    }
}
