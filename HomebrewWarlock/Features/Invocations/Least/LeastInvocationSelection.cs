using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using HomebrewWarlock.Features.EldritchBlast;

using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Classes;
using Kingmaker.Blueprints.Classes.Selection;
using Kingmaker.UnitLogic.Abilities.Blueprints;

using MicroWrath.BlueprintInitializationContext;

namespace HomebrewWarlock.Features.Invocations.Least
{
    internal static class LeastInvocationSelection
    {
        [LocalizedString]
        internal const string DisplayName = "Least";

        internal static BlueprintInitializationContext.ContextInitializer<BlueprintFeatureSelection>
            CreateSelection(BlueprintInitializationContext context,
            BlueprintInitializationContext.ContextInitializer<EldritchBlastFeatures> ebFeatures)
        {
            var selection = context.NewBlueprint<BlueprintFeatureSelection>(
                GeneratedGuid.Get(nameof(LeastInvocationSelection)),
                nameof(LeastInvocationSelection))
                .Combine(BeguilingInfluence.Create(context))
                .Combine(DarkOnesOwnLuck.Create(context))
                .Combine(OtherworldlyWhispers.Create(context))
                .Combine(ebFeatures)
                .Combine(AddEldritchBlastRankFeature.Create(context, ebFeatures))
                .Map(sf =>
                {
                    var (selection,
                        beguilingInfluence,
                        darkOnesOwnLuck,
                        otherworldlyWhispers,
                        eldritchBlastFeatures,
                        addRankFeature) = sf.Expand();

                    selection.m_DisplayName = LocalizedStrings.Features_Invocations_Least_LeastInvocationSelection_DisplayName;
                    //selection.m_Description = LocalizedStrings.Features_InvocationSelection_Description;

                    selection.AddFeatures(
                        beguilingInfluence,
                        darkOnesOwnLuck,
                        otherworldlyWhispers,
                        eldritchBlastFeatures.Essence.Least.FrightfulBlast,
                        eldritchBlastFeatures.Essence.Least.SickeningBlast,
                        eldritchBlastFeatures.Blasts.Least.EldritchSpear,
                        eldritchBlastFeatures.Blasts.Least.HideousBlow,
                        eldritchBlastFeatures.Blasts.Least.EldritchGlaive,
                        eldritchBlastFeatures.EldritchBlastBase,
                        addRankFeature);

                    return selection;
                })
                .Combine(SummonSwarm.CreateFeature(context))
                .Combine(Least.SeeTheUnseen.Create(context))
                .Map(sf =>
                {
                    var (selection,
                        summonSwarm,
                        seeTheUnseen) = sf.Expand();

                    selection.AddFeatures(
                        summonSwarm.ToMicroBlueprint(),
                        seeTheUnseen.ToMicroBlueprint());

                    return selection;
                });
                

            return selection;
        }
    }
}
