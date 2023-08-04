using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using HomebrewWarlock.Features.EldritchBlast;

using Kingmaker.Blueprints.Classes;
using Kingmaker.Blueprints.Classes.Selection;

using MicroWrath.BlueprintInitializationContext;

namespace HomebrewWarlock.Features.Invocations.Greater
{
    internal static class GreaterInvocationSelection
    {
        [LocalizedString]
        internal const string DispayName = "Greater";

        internal static BlueprintInitializationContext.ContextInitializer<BlueprintFeatureSelection>
            CreateSelection(BlueprintInitializationContext context,
            BlueprintInitializationContext.ContextInitializer<EldritchBlastFeatures> ebFeatures,
            BlueprintInitializationContext.ContextInitializer<BlueprintFeature> prerequisite)
        {
            return context.NewBlueprint<BlueprintFeatureSelection>(
                GeneratedGuid.Get("GreaterInvocationSelection"),
                nameof(GeneratedGuid.GreaterInvocationSelection))
                .Combine(ebFeatures)
                .Map(bps =>
                {
                    var (selection, features) = bps;

                    selection.m_DisplayName = LocalizedStrings.Features_Invocations_Greater_GreaterInvocationSelection_DispayName;

#if !DEBUG
                    selection.AddPrerequisiteFeature(prerequisite.ToMicroBlueprint());
                    prerequisite.IsPrerequisiteFor = new() { selection.ToReference<BlueprintFeatureReference>() };
#endif

                    selection.AddFeatures(
                        features.Essence.Greater.BewitchingBlast,
                        features.Essence.Greater.NoxiousBlast,
                        features.Blasts.Greater.EldritchCone);

                    return selection;
                });
        }
    }
}
