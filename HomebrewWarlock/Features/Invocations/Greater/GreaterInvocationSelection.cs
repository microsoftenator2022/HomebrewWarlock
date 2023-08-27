using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using HomebrewWarlock.Features.EldritchBlast;

using Kingmaker.Blueprints;
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
                GeneratedGuid.Get("GreaterInvocationSelection"))
                .Combine(ebFeatures)
                .Combine(prerequisite)
                .Combine(ChillingTentacles.Create(context))
                .Combine(DevourMagic.Create(context))
                .Map(bps =>
                {
                    var (selection, features, prerequisite, chillingTentacles, devourMagic) = bps.Expand();

                    selection.m_DisplayName = LocalizedStrings.Features_Invocations_Greater_GreaterInvocationSelection_DispayName;

#if !DEBUG
                    selection.AddPrerequisiteFeature(prerequisite.ToMicroBlueprint());
#endif

                    selection.AddFeatures(
                        features.Essence.Greater.BewitchingBlast,
                        features.Essence.Greater.NoxiousBlast,
                        features.Essence.Greater.VitriolicBlast,
                        features.Essence.Greater.RepellingBlast,
                        features.Blasts.Greater.EldritchCone,
                        chillingTentacles,
                        devourMagic);

                    return selection;
                })
                .Combine(prerequisite)
                .Map(bps =>
                {
                    var (selection, prerequisite) = bps;

                    prerequisite.IsPrerequisiteFor = selection.m_AllFeatures.ToList();

                    return selection;
                });
        }
    }
}
