using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Kingmaker.Blueprints.Classes;

using MicroWrath;
using MicroWrath.BlueprintInitializationContext;
using MicroWrath.Localization;

namespace HomebrewWarlock.Features.Invocations.Least
{
    internal static class EldritchGlaive
    {
        [LocalizedString]
        internal const string DisplayName = "Eldritch Glaive";

        internal static BlueprintInitializationContext.ContextInitializer<BlueprintFeature> Create(BlueprintInitializationContext context)
        {
            return context.NewBlueprint<BlueprintFeature>(GeneratedGuid.Get("EldritchGlaiveFeature"), nameof(GeneratedGuid.EldritchGlaiveFeature))
                .Map(feature =>
                {
                    feature.m_DisplayName = LocalizedStrings.Features_Invocations_Least_EldritchGlaive_DisplayName;
                    feature.m_Description = LocalizedStrings.Features_InvocationSelection_PlaceholderName;

                    return feature;
                });
        }
    }
}
