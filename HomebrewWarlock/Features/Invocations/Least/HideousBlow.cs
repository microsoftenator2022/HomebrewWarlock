using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Kingmaker.Blueprints.Classes;

using MicroWrath;
using MicroWrath.BlueprintInitializationContext;
using MicroWrath.Localization;

namespace HomebrewWarlock.Features.Invocations
{
    internal static class HideousBlow
    {
        [LocalizedString]
        internal const string DisplayName = "Hideous Blow";

        internal static BlueprintInitializationContext.ContextInitializer<BlueprintFeature> Create(BlueprintInitializationContext context)
        {
            return context.NewBlueprint<BlueprintFeature>(GeneratedGuid.Get("HideousBlowFeature"), nameof(GeneratedGuid.HideousBlowFeature))
                .Map(feature =>
                {
                    feature.m_DisplayName = LocalizedStrings.Features_Invocations_HideousBlow_DisplayName;
                    feature.m_Description = LocalizedStrings.Features_InvocationSelection_PlaceholderName;

                    return feature;
                });
        }
    }
}
