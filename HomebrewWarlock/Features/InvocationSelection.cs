using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Kingmaker.Blueprints.Classes;
using Kingmaker.Blueprints.Classes.Selection;

using MicroWrath;
using MicroWrath.BlueprintInitializationContext;
using MicroWrath.Localization;

namespace HomebrewWarlock.Features
{
    internal static class InvocationSelection
    {
        [LocalizedString]
        internal const string PlaceholderName = "Placeholder";

        internal static BlueprintInitializationContext.ContextInitializer<BlueprintFeatureSelection> CreateSelection(BlueprintInitializationContext context)
        {
            var placeholderFeature = context.NewBlueprint<BlueprintFeature>(GeneratedGuid.Get("WarlockInvocationPlaceholder"), "WarlockInvocationPlaceholder")
                .Map((BlueprintFeature feature) =>
                {
                    feature.m_DisplayName = LocalizedStrings.Features_InvocationSelection_PlaceholderName;

                    feature.Ranks = 10;

                    return feature;
                });

            return context.NewBlueprint<BlueprintFeatureSelection>(GeneratedGuid.Get("WarlockInvocationSelection"), "WarlockInvocationSelection")
                .Map((BlueprintFeatureSelection selection) =>
                {


                    return selection;
                });
        }
    }
}
