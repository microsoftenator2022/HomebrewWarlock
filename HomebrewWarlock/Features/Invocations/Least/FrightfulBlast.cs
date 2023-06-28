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
    internal static class FrightfulBlast
    {
        [LocalizedString]
        internal const string DisplayName = "Frightful Blast";

        internal static BlueprintInitializationContext.ContextInitializer<BlueprintFeature> Create(BlueprintInitializationContext context)
        {
            return context.NewBlueprint<BlueprintFeature>(GeneratedGuid.Get("FrightfulBlastFeature"), nameof(GeneratedGuid.FrightfulBlastFeature))
                .Map(feature =>
                {
                    feature.m_DisplayName = LocalizedStrings.Features_Invocations_FrightfulBlast_DisplayName;
                    feature.m_Description = LocalizedStrings.Features_InvocationSelection_PlaceholderName;

                    return feature;
                });
        }
    }
}
