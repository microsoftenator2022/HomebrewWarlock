using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Classes;
using Kingmaker.UnitLogic.Abilities.Blueprints;
using Kingmaker.UnitLogic.Commands.Base;

using MicroWrath.BlueprintInitializationContext;
using MicroWrath.BlueprintsDb;

namespace HomebrewWarlock.Features.Invocations.Dark
{
    internal static class WordOfChanging
    {
        [LocalizedString]
        internal const string DisplayName = "Word Of Changing";

        [LocalizedString]
        internal static readonly string Description =
            "<b>Equivalent spell level:</b> 5" +
            Environment.NewLine +
            "You utter a powerful word that transforms a creature into an inoffensive form. This effect functions " +
            "like the baleful polymorph spell"
            //+ ", except that 24 hours after being transformed, the subject is " +
            //"entitled to a second saving throw (at its original save bonus) to spontaneously resume its normal form. " +
            //"If this second save fails, it remains in its new form permanently or until restored by some other means.";
            ;

        internal static BlueprintInitializationContext.ContextInitializer<BlueprintFeature> Create(BlueprintInitializationContext context)
        {
            var ability = context.CloneBlueprint(BlueprintsDb.Owlcat.BlueprintAbility.BalefulPolymorph,
                GeneratedGuid.Get("WordOfChangingAbility"))
                .Map(ability =>
                {
                    ability.Type = AbilityType.SpellLike;
                    ability.ActionType = UnitCommand.CommandType.Standard;

                    ability.AddInvocationComponents(5);

                    return ability;
                });

            var feature = context.NewBlueprint<BlueprintFeature>(
                GeneratedGuid.Get("WordOfChangingFeature"))
                .Combine(ability)
                .Map(bps =>
                {
                    var (feature, ability) = bps;

                    feature.m_DisplayName = LocalizedStrings.Features_Invocations_Dark_WordOfChanging_DisplayName;
                    feature.m_Description = LocalizedStrings.Features_Invocations_Dark_WordOfChanging_Description;
                    feature.m_Icon = ability.m_Icon;

                    feature.AddAddFacts(c => c.m_Facts = new[] { ability.ToReference<BlueprintUnitFactReference>() });

                    return feature;
                });

            return feature;
        }
    }
}
