using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using HomebrewWarlock.Resources;

using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Classes;

using MicroWrath.BlueprintInitializationContext;

namespace HomebrewWarlock.Features
{
    internal static class HellfireFireResist
    {
        [LocalizedString]
        internal const string DisplayName = "Resistance to Fire";

        [LocalizedString]
        internal const string Description =
            "At 2nd level, you gain resistance to fire 10. This resistance stacks with any resistance to fire you " +
            "have gained from warlock class levels.";

        public static BlueprintInitializationContext.ContextInitializer<BlueprintFeature> Create(BlueprintInitializationContext context)
        {
            return context.NewBlueprint<BlueprintFeature>(GeneratedGuid.Get($"{nameof(HellfireFireResist)}"))
                .Map(feature =>
                {
                    feature.m_DisplayName = LocalizedStrings.Features_HellfireFireResist_DisplayName;
                    feature.m_Description = LocalizedStrings.Features_HellfireFireResist_Description;
                    feature.m_Icon = Sprites.ResistEnergyFire;

                    feature.AddAddFacts(c =>
                    {
                        var fireResist = GeneratedGuid.WarlockEnergyResistanceFire.ToBlueprintReference<BlueprintUnitFactReference>();
                        c.m_Facts = [fireResist, fireResist];
                    });

                    return feature;
                });
        }
    }
}
