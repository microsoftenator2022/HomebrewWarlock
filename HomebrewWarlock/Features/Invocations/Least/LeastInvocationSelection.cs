using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using HomebrewWarlock.Features.EldritchBlast;

using Kingmaker.Blueprints.Classes.Selection;
using Kingmaker.UnitLogic.Abilities.Blueprints;

using MicroWrath;
using MicroWrath.BlueprintInitializationContext;
using MicroWrath.Extensions;
using MicroWrath.Extensions.Components;
using MicroWrath.Localization;
using MicroWrath.Util;

namespace HomebrewWarlock.Features.Invocations
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
                .Combine(EldritchGlaive.Create(context))
                .Map(sf =>
                {
                    var (selection,
                        beguilingInfluence,
                        darkOnesOwnLuck,
                        otherworldlyWhispers,
                        eldritchBlastFeatures,
                        eldritchGlaive) = sf.Expand();

                    selection.m_DisplayName = LocalizedStrings.Features_Invocations_LeastInvocationSelection_DisplayName;
                    selection.m_Description = LocalizedStrings.Features_InvocationSelection_Description;

                    selection.AddFeatures(
                        beguilingInfluence.ToMicroBlueprint(),
                        darkOnesOwnLuck.ToMicroBlueprint(),
                        otherworldlyWhispers.ToMicroBlueprint(),
                        eldritchBlastFeatures.FrightfulBlast.Value!.feature.ToMicroBlueprint(),
                        //sickeningBlast.ToMicroBlueprint(),
                        eldritchBlastFeatures.EldritchSpear.Value!.ToMicroBlueprint()
                        #if DEBUG
                        , eldritchGlaive.ToMicroBlueprint()
                        #endif
                        );

                    return selection;
                })
                .Combine(HideousBlow.Create(context))
                .Combine(SummonSwarm.CreateFeature(context))
                .Map(sf =>
                {
                    var (selection,
                        hideousBlow,
                        summonSwarm) = sf.Expand();

                    selection.AddFeatures(
                        hideousBlow.ToMicroBlueprint(),
                        summonSwarm.ToMicroBlueprint());

                    return selection;
                });
                

            return selection;
        }
    }
}
