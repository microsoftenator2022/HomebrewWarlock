using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Classes;
using Kingmaker.UnitLogic.ActivatableAbilities;
using Kingmaker.UnitLogic.Commands.Base;

using MicroWrath;
using MicroWrath.BlueprintInitializationContext;
using MicroWrath.BlueprintsDb;
using MicroWrath.Extensions;
using MicroWrath.Extensions.Components;
using MicroWrath.Localization;
using MicroWrath.Util;

namespace HomebrewWarlock.Features.Invocations.Lesser
{
    internal static class WalkUnseen
    {
        [LocalizedString]
        internal const string DisplayName = "Walk Unseen";

        [LocalizedString]
        internal const string Description = "You gain the ability to fade from view. You can use invisibility (self only) with unlimited duration.";

        internal static BlueprintInitializationContext.ContextInitializer<BlueprintFeature> Create(BlueprintInitializationContext context)
        {
            var ability = context.NewBlueprint<BlueprintActivatableAbility>(
                GeneratedGuid.Get("WalkUnseenToggleAbility"),
                nameof(GeneratedGuid.WalkUnseenToggleAbility))
                .Combine(context.GetBlueprint(BlueprintsDb.Owlcat.BlueprintAbility.Invisibility))
                .Combine(context.GetBlueprint(BlueprintsDb.Owlcat.BlueprintBuff.InvisibilityBuff))
                .Map(bps =>
                {
                    var (ability, invisibilityAbility, invisibilityBuff) = bps.Expand();

                    ability.m_DisplayName = LocalizedStrings.Features_Invocations_Lesser_WalkUnseen_DisplayName;
                    ability.m_Description = new()
                    {
                        m_Key = invisibilityAbility.m_Description.m_Key,
                        m_ShouldProcess = invisibilityAbility.m_Description.m_ShouldProcess
                    };

                    ability.m_DescriptionShort = new()
                    {
                        m_Key = invisibilityAbility.m_DescriptionShort.m_Key,
                        m_ShouldProcess = invisibilityAbility.m_DescriptionShort.m_ShouldProcess
                    };

                    ability.m_Icon = invisibilityAbility.m_Icon;

                    ability.m_Buff = invisibilityBuff.ToReference<BlueprintBuffReference>();

                    ability.ActivationType = AbilityActivationType.WithUnitCommand;
                    ability.m_ActivateWithUnitCommand = UnitCommand.CommandType.Standard;

                    return ability;
                });

            var feature = context.NewBlueprint<BlueprintFeature>(
                GeneratedGuid.Get("WalkUnseenFeature"),
                nameof(GeneratedGuid.WalkUnseenFeature))
                .Combine(ability)
                .Map(bps =>
                {
                    var (feature, ability) = bps;

                    feature.m_DisplayName = LocalizedStrings.Features_Invocations_Lesser_WalkUnseen_DisplayName;
                    
                    feature.m_Description = LocalizedStrings.Features_Invocations_Lesser_WalkUnseen_Description;
                    
                    feature.m_Icon = ability.m_Icon;

                    feature.AddAddFacts(c => c.m_Facts = new[] { ability.ToReference<BlueprintUnitFactReference>() });

                    return feature;
                });

            return feature;
        }
    }
}
