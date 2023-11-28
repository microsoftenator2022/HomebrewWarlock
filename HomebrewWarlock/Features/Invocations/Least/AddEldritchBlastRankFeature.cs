using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using HomebrewWarlock.Features.EldritchBlast;

using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Classes;
using Kingmaker.Blueprints.Classes.Prerequisites;
using Kingmaker.UnitLogic.Abilities.Blueprints;

using MicroWrath.BlueprintInitializationContext;

namespace HomebrewWarlock.Features.Invocations.Least
{
    internal static class AddEldritchBlastRankFeature
    {
        [LocalizedString]
        internal const string DisplayName = "Increase Eldritch Blast Damage";

        public static BlueprintInitializationContext.ContextInitializer<BlueprintFeature> Create(
            BlueprintInitializationContext context,
            BlueprintInitializationContext.ContextInitializer<EldritchBlastFeatures> ebFeatures)
        {
            var feature = context.NewBlueprint<BlueprintFeature>(GeneratedGuid.Get(nameof(AddEldritchBlastRankFeature)))
                .Combine(ebFeatures)
                .Map(bps =>
                {
                    var (feature, blastFeatures) = bps;

                    var rank = blastFeatures.EldritchBlastRank;
                    var prerequisite = blastFeatures.EldritchBlastPrerequisiteFeature;

                    feature.m_DisplayName = LocalizedStrings.Features_Invocations_Least_AddEldritchBlastRankFeature_DisplayName;
                    feature.m_Icon = rank.m_Icon;

                    feature.AddComponent<PrerequisiteFeature>(c =>
                    {
                        c.m_Feature = prerequisite.ToReference();
                        c.HideInUI = true;
                    });

                    feature.AddComponent<PrerequisiteNoFeature>(c =>
                    {
                        c.m_Feature = GeneratedGuid.EldritchBlastProgression.ToBlueprintReference<BlueprintFeatureReference>();
                        c.HideInUI = true;
                    });

                    feature.AddAddFacts(c =>
                    {
                        c.m_Facts = [rank.ToReference<BlueprintUnitFactReference>()];
                    });

                    feature.HideNotAvailibleInUI = true;

                    feature.Ranks = 8;

                    feature.HideInCharacterSheetAndLevelUp = true;

                    return feature;
                });

            return feature;
        }
    }
}
