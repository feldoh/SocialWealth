using System;
using System.Text;
using RimWorld;
using SocialWealth.HarmonyPatches;
using UnityEngine;
using Verse;
using Verse.Sound;

namespace SocialWealth;

public class CompAbilityEffect_ConvertOrDie : CompAbilityEffect_Convert
{
    public CompProperties_AbilityConvertOrDie ConvertOrDieProps => (CompProperties_AbilityConvertOrDie)props;

    public float ChanceOfConversion(Pawn initiator, Pawn targetPawn) =>
        Mathf.Clamp(ExtraLabelMouseAttachmentTranspiler.ApplyWealthAdjustedCertaintyFactor(InteractionWorker_ConvertIdeoAttempt.CertaintyReduction(initiator, targetPawn) *
                                                                                           Props.convertPowerFactor), SocialWealthMod.settings.ConvertOrDieMinChance,
            SocialWealthMod.settings.ConvertOrDieMaxChance);

    public override string ExtraLabelMouseAttachment(LocalTargetInfo target)
    {
        if (target.Pawn == null || !Valid(target))
            return null;
        Pawn initiator = parent.pawn;
        Pawn targetPawn = target.Pawn;
        float successChance = ChanceOfConversion(initiator, targetPawn);
        TaggedString taggedString = "SocialWealth_AbilityIdeoConvertOrDieBreakdownLabel".Translate(successChance.ToStringPercent());
        StringBuilder sb = new StringBuilder(taggedString.CapitalizeFirst().ToString()).AppendLine();

        sb.AppendInNewLine("Factors".Translate() + ":");
        taggedString = "Base".Translate();
        string text = " -  " + taggedString.CapitalizeFirst() + ": " + 0.06f.ToStringPercent();
        sb.AppendInNewLine(text);
        sb.AppendInNewLine(" -  " + "AbilityIdeoConvertBreakdownUsingAbility".Translate(parent.def.LabelCap.Named("ABILITY")) + ": " + Props.convertPowerFactor.ToStringPercent());
        float statValue = initiator.GetStatValue(StatDefOf.ConversionPower);
        if (Math.Abs(statValue - 1.0) > 0.0001f)
            sb.AppendInNewLine(" -  " + "AbilityIdeoConvertBreakdownConversionPower".Translate(initiator.Named("PAWN")) + ": " + statValue.ToStringPercent());
        TaggedString factorsDescription = ConversionUtility.GetCertaintyReductionFactorsDescription(targetPawn);
        if (!factorsDescription.NullOrEmpty())
            sb.AppendInNewLine(" -  " + factorsDescription);
        Precept_Role role = targetPawn.Ideo?.GetRole(targetPawn);
        if (role != null && Math.Abs(role.def.certaintyLossFactor - 1.0) > 0.0001f)
            sb.AppendInNewLine(" -  " + "AbilityIdeoConvertBreakdownRole".Translate(targetPawn.Named("PAWN"), role.Named("ROLE")) + ": " +
                               role.def.certaintyLossFactor.ToStringPercent());
        ReliquaryUtility.GetRelicConvertPowerFactorForPawn(initiator, sb);
        ConversionUtility.ConversionPowerFactor_MemesVsTraits(initiator, targetPawn, sb);
        sb.AppendInNewLine(" -  " + "SocialWealth_SuccessRange".Translate(SocialWealthMod.settings.ConvertOrDieMinChance.ToStringPercent(),
            SocialWealthMod.settings.ConvertOrDieMaxChance.ToStringPercent()));
        return sb.ToString();
    }

    public override void Apply(LocalTargetInfo target, LocalTargetInfo dest)
    {
        if (!ModLister.CheckIdeology("Ideoligion conversion")) return;
        Pawn initiator = parent.pawn;
        Pawn targetPawn = target.Pawn;
        float conversionChance = ChanceOfConversion(initiator, targetPawn);
        if (Rand.Chance(conversionChance))
        {
            targetPawn.ideo.SetIdeo(parent.pawn.Ideo);
            Messages.Message(Props.successMessage.Formatted(initiator.Named("INITIATOR"), targetPawn.Named("RECIPIENT"), initiator.Ideo.name.Named("IDEO")), new LookTargets(
                new[]
                {
                    initiator,
                    targetPawn
                }), MessageTypeDefOf.PositiveEvent);
            Find.PlayLog.Add(
                new PlayLogEntry_Interaction(InteractionDefOf.Convert_Success, parent.pawn, targetPawn, [SocialWealthDefOf.SocialWealth_Sentence_ConvertOrDie_Success]));
        }
        else
        {
            initiator.needs.mood.thoughts.memories.TryGainMemory(Props.failedThoughtInitiator, targetPawn);

            ExecutionUtility.DoExecutionByCut(initiator, targetPawn);
            Messages.Message(Props.failMessage.Formatted(initiator.Named("INITIATOR"), targetPawn.Named("RECIPIENT"), initiator.Ideo.name.Named("IDEO")), new LookTargets(
                new Pawn[2]
                {
                    initiator,
                    targetPawn
                }), MessageTypeDefOf.NegativeEvent);
            Find.PlayLog.Add(
                new PlayLogEntry_Interaction(InteractionDefOf.Convert_Failure, parent.pawn, targetPawn, [SocialWealthDefOf.SocialWealth_Sentence_ConvertOrDie_Failure]));
        }

        Props.sound?.PlayOneShot(new TargetInfo(target.Cell, parent.pawn.Map));
    }

    public override bool Valid(LocalTargetInfo target, bool throwMessages = false)
    {
        return base.Valid(target, throwMessages) && target.Pawn.IsPrisonerOfColony;
    }
}
