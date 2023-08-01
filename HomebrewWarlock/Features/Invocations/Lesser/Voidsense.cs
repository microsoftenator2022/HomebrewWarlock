using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Kingmaker.Blueprints.Classes;

using MicroWrath.BlueprintInitializationContext;
using MicroWrath.BlueprintsDb;

namespace HomebrewWarlock.Features.Invocations.Lesser
{
    internal static class Voidsense
    {
        [LocalizedString]
        internal const string DisplayName = "Voidsense";

        [LocalizedString]
        internal const string Description =
            "You can sharpen your hearing and sight, gaining blindsense out to 30 feet.";
        internal static BlueprintInitializationContext.ContextInitializer<BlueprintFeature> Create(BlueprintInitializationContext context)
        {
            var feature = context.NewBlueprint<BlueprintFeature>(
                GeneratedGuid.Get(nameof(Voidsense)),
                nameof(Voidsense))
                .Combine(context.GetBlueprint(BlueprintsDb.Owlcat.BlueprintFeature.Blindsight))
                .Map(bps =>
                {
                    var (feature, blindsight) = bps;

                    feature.m_DisplayName = LocalizedStrings.Features_Invocations_Lesser_Voidsense_DisplayName;
                    feature.m_Description = LocalizedStrings.Features_Invocations_Lesser_Voidsense_Description;
                    feature.m_Icon = blindsight.m_Icon;

                    return feature;
                });

            return feature;
        }
    }
}
