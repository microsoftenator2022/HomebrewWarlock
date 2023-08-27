using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Classes;
using Kingmaker.UnitLogic.Abilities.Blueprints;
using Kingmaker.UnitLogic.Abilities.Components;
using Kingmaker.UnitLogic.Mechanics;
using Kingmaker.UnitLogic.Mechanics.Actions;

using MicroWrath.BlueprintInitializationContext;
using MicroWrath.BlueprintsDb;

namespace HomebrewWarlock.Features.Invocations.Lesser
{
    internal static class TheDeadWalk
    {
        [LocalizedString]
        internal const string DisplayName = "The Dead Walk";

        [LocalizedString]
        internal const string Description =
            "You can turn the bones or bodies of dead creatures into undead skeletons or zombies (as the animate " +
            "dead spell). Undead created by this ability crumble into dust after 1 minute per caster level.";

        [LocalizedString]
        internal const string Duration = "1 minute/level";

        internal static BlueprintInitializationContext.ContextInitializer<BlueprintFeature> Create(BlueprintInitializationContext context)
        {
            var ability = context.CloneBlueprint(BlueprintsDb.Owlcat.BlueprintAbility.AnimateDead,
                GeneratedGuid.Get("TheDeadWalkAbility"))
                .Map(ability =>
                {
                    ability.Type = AbilityType.SpellLike;
                    ability.AddInvocationComponents(4);

                    foreach (var casm in ability.GetComponent<AbilityEffectRunAction>().Actions.Actions
                        .OfType<ContextActionSpawnMonster>()
                        .Where(a => a.DurationValue.Rate is DurationRate.Rounds))
                        casm.DurationValue.Rate = DurationRate.Minutes;

                    ability.LocalizedDuration = LocalizedStrings.Features_Invocations_Lesser_TheDeadWalk_Duration;

                    return ability;
                });

            var feature = context.NewBlueprint<BlueprintFeature>(
                GeneratedGuid.Get("TheDeadWalkFeature"))
                .Combine(ability)
                .Map(bps =>
                {
                    var (feature, ability) = bps;

                    feature.m_DisplayName = LocalizedStrings.Features_Invocations_Lesser_TheDeadWalk_DisplayName;
                    feature.m_Description = LocalizedStrings.Features_Invocations_Lesser_TheDeadWalk_Description;
                    feature.m_Icon = ability.m_Icon;

                    feature.AddAddFacts(c => c.m_Facts = new[] { ability.ToReference<BlueprintUnitFactReference>() });

                    return feature;
                });

            return feature;
        }
    }
}
