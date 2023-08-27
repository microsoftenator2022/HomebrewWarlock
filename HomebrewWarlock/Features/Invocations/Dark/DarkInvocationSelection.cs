using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using HomebrewWarlock.Features.EldritchBlast;

using Kingmaker.Blueprints.Classes;
using Kingmaker.Blueprints.Classes.Selection;

using MicroWrath.BlueprintInitializationContext;

namespace HomebrewWarlock.Features.Invocations.Dark
{
    internal static class DarkInvocationSelection
    {
        [LocalizedString]
        internal const string DisplayName = "Dark";

        internal static BlueprintInitializationContext.ContextInitializer<BlueprintFeatureSelection> Create(
            BlueprintInitializationContext context,
            BlueprintInitializationContext.ContextInitializer<EldritchBlastFeatures> ebFeatures,
            BlueprintInitializationContext.ContextInitializer<BlueprintFeature> prerequisite)
        {
            var selection = context.NewBlueprint<BlueprintFeatureSelection>(
                GeneratedGuid.Get("DarkInvocationSelection"))
                .Combine(ebFeatures)
                .Combine(prerequisite)
                .Combine(WordOfChanging.Create(context))
                .Combine(DarkDiscorporation.Create(context))
                .Map(bps =>
                {
                    var (selection, ebFeatures, prerequisite, wordOfChanging, darkDiscorporation) = bps.Expand();

                    selection.m_DisplayName = LocalizedStrings.Features_Invocations_Dark_DarkInvocationSelection_DisplayName;

#if !DEBUG
                    selection.AddPrerequisiteFeature(prerequisite.ToMicroBlueprint());
#endif

                    selection.AddFeatures(
                        ebFeatures.Essence.Dark.UtterdarkBlast,
                        ebFeatures.Blasts.Dark.EldritchDoom,
                        wordOfChanging,
                        darkDiscorporation);

                    return selection;
                })
                .Combine(prerequisite)
                .Map(bps =>
                {
                    var (selection, prerequisite) = bps;

                    prerequisite.IsPrerequisiteFor = selection.m_AllFeatures.ToList();

                    return selection;
                }); ;

            return selection;
        }
    }
}
