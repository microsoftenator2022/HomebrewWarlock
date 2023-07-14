using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Classes;
using Kingmaker.UnitLogic.ActivatableAbilities;
using Kingmaker.UnitLogic.Commands.Base;
using Kingmaker.UnitLogic.FactLogic;

using MicroWrath;
using MicroWrath.BlueprintInitializationContext;
using MicroWrath.BlueprintsDb;
using MicroWrath.Localization;
using MicroWrath.Util;

namespace HomebrewWarlock.Features.Invocations.Lesser
{
    internal static class FellFlight
    {
        [LocalizedString]
        internal const string DisplayName = "Fell Flight";

        internal static BlueprintInitializationContext.ContextInitializer<BlueprintFeature> Create(BlueprintInitializationContext context)
        {
            var feature = context.CloneBlueprint(
                BlueprintsDb.Owlcat.BlueprintFeature.FeatureWingsDemon,
                GeneratedGuid.Get("FellFlightFeature"),
                nameof(GeneratedGuid.FellFlightFeature))
                .Combine(
                    context.CloneBlueprint(BlueprintsDb.Owlcat.BlueprintActivatableAbility.AbilityWingsDemon,
                    GeneratedGuid.Get("FellFlightAbility"),
                    nameof(GeneratedGuid.FellFlightAbility)))
                .Combine(context.GetBlueprint(BlueprintsDb.Owlcat.BlueprintBuff.WingsAngelBlack))
                .Map(bps =>
                {
                    var (feature, ability, buff) = bps.Expand();

                    feature.m_DisplayName = LocalizedStrings.Features_Invocations_Lesser_FellFlight_DisplayName;

                    feature.GetComponent<AddFacts>().m_Facts = new[] { ability.ToReference<BlueprintUnitFactReference>() };

                    ability.m_Buff = buff.ToReference<BlueprintBuffReference>();

                    ability.ActivationType = AbilityActivationType.WithUnitCommand;
                    ability.m_ActivateWithUnitCommand = UnitCommand.CommandType.Standard;

                    return feature;
                });

            return feature;
        }
    }
}
