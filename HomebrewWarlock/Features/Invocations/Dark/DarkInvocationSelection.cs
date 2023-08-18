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
                GeneratedGuid.Get("DarkInvocationSelection"),
                nameof(GeneratedGuid.DarkInvocationSelection))
                .Combine(ebFeatures)
                .Combine(prerequisite)
                .Combine(WordOfChanging.Create(context))
                .Map(bps =>
                {
                    var (selection, ebFeatures, prerequisite, wordOfChanging) = bps.Expand();

                    selection.m_DisplayName = LocalizedStrings.Features_Invocations_Dark_DarkInvocationSelection_DisplayName;

#if !DEBUG
                    selection.AddPrerequisiteFeature(prerequisite.ToMicroBlueprint());
                    prerequisite.IsPrerequisiteFor = new() { selection.ToReference<BlueprintFeatureReference>() };
#endif

                    selection.AddFeatures(
                        ebFeatures.Essence.Dark.UtterdarkBlast,
                        ebFeatures.Blasts.Dark.EldritchDoom,
                        wordOfChanging);

                    return selection;
                });

            return selection;
        }
    }
}
