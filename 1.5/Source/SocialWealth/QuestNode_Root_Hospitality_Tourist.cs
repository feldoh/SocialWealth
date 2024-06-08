using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using RimWorld.QuestGen;
using UnityEngine;
using Verse;

namespace SocialWealth;

public class QuestNode_Root_Hospitality_Tourist : QuestNode
{
    private static FloatRange MutinyTimeRange = new(0.2f, 1f);
    private static IntRange QuestDurationDaysRange = new(5, 20);

    private bool TryGetRelicQuest(out Precept_Relic relic, out Quest epicQuest)
    {
        epicQuest = Find.QuestManager?.QuestsListForReading?.Find(q => (q.root?.IsEpic ?? false) && q.State == QuestState.Ongoing);
        QuestPart_SubquestGenerator_RelicHunt relicPart = epicQuest?.PartsListForReading?.Find(p => p is QuestPart_SubquestGenerator_RelicHunt { relic: not null }) as QuestPart_SubquestGenerator_RelicHunt;
        relic = relicPart?.relic;
        return relic != null && epicQuest != null;
    }

    protected override void RunInt()
    {
        if (!ModLister.CheckRoyalty("Hospitality refugee"))
            return;
        Quest quest = QuestGen.quest;
        Slate slate = QuestGen.slate;
        Map map = QuestGen_Get.GetMap();
        int lodgerCount = QuestNode_Root_Beggars.LodgerCountFromPopulation(slate, map);
        int var1 = 0;
        bool var2 = false;
        if (Find.Storyteller.difficulty.ChildrenAllowed && lodgerCount >= 2)
        {
            new List<(int, float)>
            {
                (0, 0.7f),
                (Rand.Range(1, lodgerCount / 2), 0.2f),
                (lodgerCount - 1, 0.1f)
            }.TryRandomElementByWeight(p => p.Item2, out (int, float) result);
            var1 = result.Item1;
            var2 = var1 == lodgerCount - 1;
        }

        int questDurationDays = QuestDurationDaysRange.RandomInRange;
        int questDurationTicks = questDurationDays * 60000;
        List<FactionRelation> relations = [];
        foreach (Faction faction1 in Find.FactionManager.AllFactionsListForReading)
            if (!faction1.def.permanentEnemy)
                relations.Add(new FactionRelation
                {
                    other = faction1,
                    kind = FactionRelationKind.Neutral
                });

        FactionGeneratorParms parms = new(FactionDefOf.OutlanderRefugee, hidden: true);
        if (ModsConfig.IdeologyActive)
            parms.ideoGenerationParms = new IdeoGenerationParms(parms.factionDef);
        Faction faction = FactionGenerator.NewGeneratedFactionWithRelations(parms, relations);
        faction.temporary = true;
        Find.FactionManager.Add(faction);
        string lodgerRecruitedSignal = QuestGenUtility.HardcodedSignalWithQuestID("lodgers.Recruited");
        string lodgersArrestedSignal = QuestGenUtility.HardcodedSignalWithQuestID("lodgers.Arrested");
        string lodgersDestroyedSignal = QuestGenUtility.HardcodedSignalWithQuestID("lodgers.Destroyed");
        string lodgersKidnappedSignal = QuestGenUtility.HardcodedSignalWithQuestID("lodgers.Kidnapped");
        string lodgersLeftMapSignal = QuestGenUtility.HardcodedSignalWithQuestID("lodgers.LeftMap");
        string lodgersBanished = QuestGenUtility.HardcodedSignalWithQuestID("lodgers.Banished");
        string lodgerArrestedOrRecruited = QuestGen.GenerateNewSignal("Lodger_ArrestedOrRecruited");
        quest.AnySignal(new List<string>
        {
            lodgerRecruitedSignal,
            lodgersArrestedSignal
        }, outSignals: new List<string>
        {
            lodgerArrestedOrRecruited
        });
        List<Pawn> pawns = [];
        for (int index = 0; index < lodgerCount; ++index)
        {
            DevelopmentalStage developmentalStages = index <= 0 || index < lodgerCount - var1 ? DevelopmentalStage.Adult : DevelopmentalStage.Child;
            Pawn pawn = quest.GeneratePawn(PawnKindDefOf.Empire_Common_Lodger, faction, forceGenerateNewPawn: true, developmentalStages: developmentalStages, allowPregnant: false);
            pawns.Add(pawn);
            quest.PawnJoinOffer(pawn, "LetterJoinOfferLabel".Translate(pawn.Named("PAWN")), "LetterJoinOfferTitle".Translate(pawn.Named("PAWN")),
                "SocialWealth_LetterJoinOfferText".Translate(pawn.Named("PAWN"), map.Parent.Named("MAP")), () =>
                {
                    quest.JoinPlayer(map.Parent, Gen.YieldSingle(pawn), true);
                    LetterDef positiveEvent = LetterDefOf.PositiveEvent;
                    string newRecruitLetterLabel = "LetterLabelMessageRecruitSuccess".Translate() + ": " + pawn.LabelShortCap;
                    string text = "MessageRecruitJoinOfferAccepted".Translate(pawn.Named("RECRUITEE"));
                    quest.Letter(positiveEvent, text: text, label: newRecruitLetterLabel);
                    quest.SignalPass(outSignal: lodgerRecruitedSignal);
                }, charity: false);
        }

        slate.Set("lodgers", pawns);
        faction.leader = pawns.First();
        Pawn asker = pawns.First();
        quest.SetFactionHidden(faction);
        quest.ExtraFaction(faction, pawns, ExtraFactionType.MiniFaction, inSignalsRemovePawn: [lodgerRecruitedSignal]);
        quest.PawnsArrive(pawns, mapParent: map.Parent, joinPlayer: true, customLetterLabel: "[lodgersArriveLetterLabel]",
            customLetterText: "[lodgersArriveLetterText]");
        QuestPart_Choice questPartChoice = quest.RewardChoice();
        QuestPart_Choice.Choice choice = new();
        choice.rewards.Add(new Reward_VisitorsHelp());

        if (TryGetRelicQuest(out Precept_Relic relic, out Quest epicQuest))
        {
            choice.rewards.Add(new Reward_RelicInfo { relic = relic, quest = quest });
            slate.Set("relic", relic);
            quest.parent = epicQuest;
        }

        if (ModsConfig.IdeologyActive && Faction.OfPlayer.ideos.FluidIdeo != null)
            choice.rewards.Add(new Reward_DevelopmentPoints(quest));

        questPartChoice.choices.Add(choice);
        quest.SetAllApparelLocked(pawns);
        string assaultColonySignal = QuestGen.GenerateNewSignal("AssaultColony");
        Action betrayalAction = () =>
        {
            int num = Mathf.FloorToInt(MutinyTimeRange.RandomInRange * questDurationTicks);
            quest.Delay(num, () =>
            {
                quest.Letter(LetterDefOf.ThreatBig, text: "[mutinyLetterText]", label: "[mutinyLetterLabel]");
                quest.SignalPass(outSignal: assaultColonySignal);
                QuestGen_End.End(quest, QuestEndOutcome.Unknown);
            }, debugLabel: "Betrayal (" + num.ToStringTicksToDays() + ")");
        };

        if (Find.Storyteller.difficulty.allowViolentQuests)
        {
            List<Tuple<float, Action>> source = [];
            if (!Find.Storyteller.difficulty.ChildRaidersAllowed && var1 > 0)
                source.Add(Tuple.Create(0.25f, () => { }));
            else
                source.Add(Tuple.Create(0.25f, betrayalAction));
            source.Add(Tuple.Create(0.5f, () => { }));
            Tuple<float, Action> result;
            if (source.TryRandomElementByWeight(t => t.Item1, out result))
                result.Item2();
        }

        QuestPart_RefugeeInteractions part = new()
        {
            inSignalEnable = QuestGen.slate.Get<string>("inSignal"),
            inSignalDestroyed = lodgersDestroyedSignal,
            inSignalArrested = lodgersArrestedSignal,
            inSignalSurgeryViolation = QuestGenUtility.HardcodedSignalWithQuestID("lodgers.SurgeryViolation"),
            inSignalKidnapped = lodgersKidnappedSignal,
            inSignalRecruited = lodgerRecruitedSignal,
            inSignalAssaultColony = assaultColonySignal,
            inSignalLeftMap = lodgersLeftMapSignal,
            inSignalBanished = lodgersBanished,
            outSignalDestroyed_AssaultColony = QuestGen.GenerateNewSignal("LodgerDestroyed_AssaultColony"),
            outSignalDestroyed_LeaveColony = QuestGen.GenerateNewSignal("LodgerDestroyed_LeaveColony"),
            outSignalDestroyed_BadThought = QuestGen.GenerateNewSignal("LodgerDestroyed_BadThought"),
            outSignalArrested_AssaultColony = QuestGen.GenerateNewSignal("LodgerArrested_AssaultColony"),
            outSignalArrested_LeaveColony = QuestGen.GenerateNewSignal("LodgerArrested_LeaveColony"),
            outSignalArrested_BadThought = QuestGen.GenerateNewSignal("LodgerArrested_BadThought"),
            outSignalSurgeryViolation_AssaultColony = QuestGen.GenerateNewSignal("LodgerSurgeryViolation_AssaultColony"),
            outSignalSurgeryViolation_LeaveColony = QuestGen.GenerateNewSignal("LodgerSurgeryViolation_LeaveColony"),
            outSignalSurgeryViolation_BadThought = QuestGen.GenerateNewSignal("LodgerSurgeryViolation_BadThought"),
            outSignalLast_Destroyed = QuestGen.GenerateNewSignal("LastLodger_Destroyed"),
            outSignalLast_Arrested = QuestGen.GenerateNewSignal("LastLodger_Arrested"),
            outSignalLast_Kidnapped = QuestGen.GenerateNewSignal("LastLodger_Kidnapped"),
            outSignalLast_Recruited = QuestGen.GenerateNewSignal("LastLodger_Recruited"),
            outSignalLast_LeftMapAllHealthy = QuestGen.GenerateNewSignal("LastLodger_LeftMapAllHealthy"),
            outSignalLast_LeftMapAllNotHealthy = QuestGen.GenerateNewSignal("LastLodger_LeftMapAllNotHealthy"),
            outSignalLast_Banished = QuestGen.GenerateNewSignal("LastLodger_Banished")
        };
        part.pawns.AddRange(pawns);
        part.faction = faction;
        part.mapParent = map.Parent;
        part.signalListenMode = QuestPart.SignalListenMode.Always;
        quest.AddPart(part);
        quest.Delay(questDurationTicks, () =>
        {
            quest.SignalPassWithFaction(faction,
                outAction: () => quest.Letter(LetterDefOf.PositiveEvent, text: "[lodgersLeavingLetterText]", label: "[lodgersLeavingLetterLabel]"));
            quest.Leave(pawns, sendStandardLetter: false, leaveOnCleanup: false, inSignalRemovePawn: lodgerArrestedOrRecruited, wakeUp: true);
        }, expiryInfoPart: "GuestsDepartsIn".Translate(), expiryInfoPartTip: "GuestsDepartsOn".Translate(), debugLabel: "QuestDelay");
        quest.Letter(LetterDefOf.NegativeEvent, part.outSignalDestroyed_BadThought, text: "[lodgerDiedMemoryThoughtLetterText]", label: "[lodgerDiedMemoryThoughtLetterLabel]");
        quest.Letter(LetterDefOf.NegativeEvent, part.outSignalDestroyed_AssaultColony, text: "[lodgerDiedAttackPlayerLetterText]", label: "[lodgerDiedAttackPlayerLetterLabel]");
        quest.Letter(LetterDefOf.NegativeEvent, part.outSignalDestroyed_LeaveColony, text: "[lodgerDiedLeaveMapLetterText]", label: "[lodgerDiedLeaveMapLetterLabel]");
        quest.Letter(LetterDefOf.NegativeEvent, part.outSignalLast_Destroyed, text: "[lodgersAllDiedLetterText]", label: "[lodgersAllDiedLetterLabel]");
        quest.Letter(LetterDefOf.NegativeEvent, part.outSignalArrested_BadThought, text: "[lodgerArrestedMemoryThoughtLetterText]",
            label: "[lodgerArrestedMemoryThoughtLetterLabel]");
        quest.Letter(LetterDefOf.NegativeEvent, part.outSignalArrested_AssaultColony, text: "[lodgerArrestedAttackPlayerLetterText]",
            label: "[lodgerArrestedAttackPlayerLetterLabel]");
        quest.Letter(LetterDefOf.NegativeEvent, part.outSignalArrested_LeaveColony, text: "[lodgerArrestedLeaveMapLetterText]", label: "[lodgerArrestedLeaveMapLetterLabel]");
        quest.Letter(LetterDefOf.NegativeEvent, part.outSignalLast_Arrested, text: "[lodgersAllArrestedLetterText]", label: "[lodgersAllArrestedLetterLabel]");
        quest.Letter(LetterDefOf.NegativeEvent, part.outSignalSurgeryViolation_BadThought, text: "[lodgerViolatedMemoryThoughtLetterText]",
            label: "[lodgerViolatedMemoryThoughtLetterLabel]");
        quest.Letter(LetterDefOf.NegativeEvent, part.outSignalSurgeryViolation_AssaultColony, text: "[lodgerViolatedAttackPlayerLetterText]",
            label: "[lodgerViolatedAttackPlayerLetterLabel]");
        quest.Letter(LetterDefOf.NegativeEvent, part.outSignalSurgeryViolation_LeaveColony, text: "[lodgerViolatedLeaveMapLetterText]",
            label: "[lodgerViolatedLeaveMapLetterLabel]");
        quest.AddMemoryThought(pawns, ThoughtDefOf.OtherTravelerDied, part.outSignalDestroyed_BadThought);
        quest.AddMemoryThought(pawns, ThoughtDefOf.OtherTravelerArrested, part.outSignalArrested_BadThought);
        quest.AddMemoryThought(pawns, ThoughtDefOf.OtherTravelerSurgicallyViolated, part.outSignalSurgeryViolation_BadThought);
        quest.End(QuestEndOutcome.Fail, inSignal: part.outSignalDestroyed_AssaultColony, sendStandardLetter: true);
        quest.End(QuestEndOutcome.Fail, inSignal: part.outSignalDestroyed_LeaveColony, sendStandardLetter: true);
        quest.End(QuestEndOutcome.Fail, inSignal: part.outSignalLast_Destroyed);
        quest.End(QuestEndOutcome.Fail, inSignal: part.outSignalArrested_AssaultColony, sendStandardLetter: true);
        quest.End(QuestEndOutcome.Fail, inSignal: part.outSignalArrested_LeaveColony, sendStandardLetter: true);
        quest.End(QuestEndOutcome.Fail, inSignal: part.outSignalLast_Arrested);
        quest.End(QuestEndOutcome.Fail, inSignal: part.outSignalSurgeryViolation_AssaultColony, sendStandardLetter: true);
        quest.End(QuestEndOutcome.Fail, inSignal: part.outSignalSurgeryViolation_LeaveColony, sendStandardLetter: true);
        quest.End(QuestEndOutcome.Fail, inSignal: part.outSignalLast_Kidnapped, sendStandardLetter: true);
        quest.End(QuestEndOutcome.Fail, inSignal: part.outSignalLast_Banished, sendStandardLetter: true);
        quest.End(QuestEndOutcome.Success, inSignal: part.outSignalLast_Recruited, sendStandardLetter: true);
        quest.End(QuestEndOutcome.Success, inSignal: part.outSignalLast_LeftMapAllNotHealthy, sendStandardLetter: true);
        quest.SignalPass(() =>
        {
            quest.End(QuestEndOutcome.Success, sendStandardLetter: true);
        }, part.outSignalLast_LeftMapAllHealthy);
        slate.Set("lodgerCount", lodgerCount);
        slate.Set("lodgersCountMinusOne", lodgerCount - 1);
        slate.Set("asker", asker);
        slate.Set("map", map);
        slate.Set("questDurationTicks", questDurationTicks);
        slate.Set("faction", faction);
        slate.Set("childCount", var1);
        slate.Set("allButOneChildren", var2);
    }

    protected override bool TestRunInt(Slate slate)
    {
        return QuestGen_Get.GetMap() != null;
    }
}
