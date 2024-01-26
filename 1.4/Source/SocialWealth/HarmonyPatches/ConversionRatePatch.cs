using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
using HarmonyLib;
using RimWorld;
using Verse;

namespace SocialWealth.HarmonyPatches;

[HarmonyPatch(typeof(CompAbilityEffect_Convert))]
[HarmonyPatch("ExtraLabelMouseAttachment")]
public static class ExtraLabelMouseAttachmentTranspiler
{
    public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
    {
        List<CodeInstruction> codes = [..instructions];
        List<CodeInstruction> newCodes = [];
        bool found = false;
        foreach (CodeInstruction t in codes)
        {
            switch (found)
            {
                case false when t.Calls(AccessTools.Method(typeof(GenText), nameof(GenText.ToStringPercent), new[]
                {
                    typeof(float)
                })):
                    found = true;
                    newCodes.Add(new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(ExtraLabelMouseAttachmentTranspiler), nameof(GetWealthAdjustedCertaintyReduction))));
                    break;
                case true when t.opcode == OpCodes.Ret:
                    newCodes.Add(new CodeInstruction(OpCodes.Call,
                        AccessTools.Method(typeof(ExtraLabelMouseAttachmentTranspiler), nameof(AppendWealthAdjustedCertaintyReduction))));
                    newCodes.Add(new CodeInstruction(OpCodes.Ret));
                    break;
                default:
                    newCodes.Add(t);
                    break;
            }
        }

        return newCodes.AsEnumerable();
    }

    public static string GetWealthAdjustedCertaintyReduction(float originalValue)
    {
        // We know this is an attempt to reduce certainty, and they must be not our ideo so we always want to multiply
        float certaintyChangeMultiplier = originalValue * GetCertaintyChangeMultiplier();
        return certaintyChangeMultiplier.ToStringPercent();
    }

    public static float GetCertaintyChangeMultiplier()
    {
        return Find.CurrentMap.wealthWatcher.WealthTotal / SocialWealthMod.settings.neutralWealth;
    }

    public static string AppendWealthAdjustedCertaintyReduction(string s)
    {
        return $"{s}\n -  " + "SocialWealth_WealthConversionFactorDesc".Translate() + ": " +
               GetCertaintyChangeMultiplier().ToStringPercent();
    }
}


[HarmonyPatch(typeof(Pawn_IdeoTracker), "IdeoConversionAttempt")]
public static class IdeoConversionAttemptTranspiler
{
    public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
    {
        List<CodeInstruction> codes = [..instructions];
        List<CodeInstruction> newCodes = [];
        int callCount = 0;
        foreach (CodeInstruction t in codes)
        {
            if (t.Calls(AccessTools.Method(typeof(GenText), nameof(GenText.ToStringPercent), new[]
                {
                    typeof(float)
                })))
            {
                callCount++;
                switch (callCount)
                {
                    case 2:
                        newCodes.Add(new CodeInstruction(OpCodes.Ldarg_0));
                        newCodes.Add(new CodeInstruction(OpCodes.Call, AccessTools.PropertyGetter(typeof(Pawn_IdeoTracker), nameof(Pawn_IdeoTracker.Certainty))));
                        newCodes.Add(new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(IdeoConversionAttemptTranspiler), nameof(GetWealthAdjustedCertaintyReducedToValue))));
                        break;
                }
            }
            newCodes.Add(t);
        }

        return newCodes.AsEnumerable();
    }

    public static float GetWealthAdjustedCertaintyReducedToValue(float newValue, float originalValue)
    {
        float diff = originalValue - newValue;
        Verse.Log.Message($"{originalValue} - {newValue} = {diff} * {ExtraLabelMouseAttachmentTranspiler.GetCertaintyChangeMultiplier()}");
        // We know this is an attempt to reduce certainty, and they must be not our ideo so we always want to multiply
        return originalValue - diff * ExtraLabelMouseAttachmentTranspiler.GetCertaintyChangeMultiplier();
    }

}

[HarmonyPatch(typeof(Pawn_IdeoTracker), nameof(Pawn_IdeoTracker.Certainty), MethodType.Setter)]
public static class CertaintyPrefix
{
    public static bool Prefix(Pawn_IdeoTracker __instance, ref float value, Pawn ___pawn)
    {
        float currentCertainty = __instance.Certainty;
        float certaintyDiff = currentCertainty - value;
        if (!___pawn.Spawned || Math.Abs(certaintyDiff) < SocialWealthMod.settings.changeThreshold) return true;

        // Calculate the scaling factor based on the colony's wealth
        float scalingFactor = ExtraLabelMouseAttachmentTranspiler.GetCertaintyChangeMultiplier();
        Verse.Log.Message($"Scaling factor: {scalingFactor}. Applying to {currentCertainty} -> {value}. Diff: {certaintyDiff}");

        float finalCertaintyDiff;
        // If the pawn's ideo is not the player's ideo
        if (__instance.Ideo != Faction.OfPlayer.ideos.PrimaryIdeo)
        {
            finalCertaintyDiff = (value < currentCertainty
                ? certaintyDiff * scalingFactor
                : certaintyDiff / scalingFactor);
        }
        else
        {
            finalCertaintyDiff = (value < currentCertainty
                ? certaintyDiff / scalingFactor
                : certaintyDiff * scalingFactor);
        }

        value = currentCertainty - finalCertaintyDiff;
        Verse.Log.Message($"Scaled certainty to {value} diff change: {certaintyDiff}->{finalCertaintyDiff} with scaling factor {scalingFactor}");

        // Return true to allow the original method to execute
        return true;
    }
}
