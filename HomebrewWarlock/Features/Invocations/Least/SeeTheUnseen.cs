using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Classes;
using Kingmaker.UnitLogic.Abilities.Blueprints;

using MicroWrath.BlueprintInitializationContext;
using MicroWrath.BlueprintsDb;

namespace HomebrewWarlock.Features.Invocations.Least
{
    internal static class SeeTheUnseen
    {
        [LocalizedString]
        internal const string DisplayName = "See the Unseen";

        [LocalizedString]
        internal const string Description = "You gain See Invisibility as the spell";

        internal static BlueprintInitializationContext.ContextInitializer<BlueprintFeature> Create(BlueprintInitializationContext context)
        {
            var ability = context.CloneBlueprint(BlueprintsDb.Owlcat.BlueprintAbility.SeeInvisibility,
                GeneratedGuid.Get("SeeTheUnseenAbility"))
                .Map(ability =>
                {
                    ability.AddInvocationComponents(2);
                    ability.Type = AbilityType.SpellLike;

                    return ability;
                });

            var feature = context.NewBlueprint<BlueprintFeature>(
                GeneratedGuid.Get("SeeTheUnseenFeature"))
                .Combine(ability)
                .Map(bps =>
                {
                    var (feature, ability) = bps;

                    feature.m_DisplayName = LocalizedStrings.Features_Invocations_Least_SeeTheUnseen_DisplayName;
                    feature.m_Description = LocalizedStrings.Features_Invocations_Least_SeeTheUnseen_Description;

                    feature.m_Icon = ability.m_Icon;

                    feature.AddAddFacts(c => c.m_Facts = new[] { ability.ToReference<BlueprintUnitFactReference>() });

                    return feature;
                });
                

            return feature;
        }
    }
}
