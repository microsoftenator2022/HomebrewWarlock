using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using HomebrewWarlock.Resources;

using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Classes;
using Kingmaker.UnitLogic.Abilities.Blueprints;
using Kingmaker.UnitLogic.Abilities.Components;
using Kingmaker.UnitLogic.Mechanics.Actions;

using MicroWrath;
using MicroWrath.BlueprintInitializationContext;
using MicroWrath.BlueprintsDb;
using MicroWrath.Components;
using MicroWrath.Extensions;
using MicroWrath.Extensions.Components;
using MicroWrath.Localization;
using MicroWrath.Util;
using MicroWrath.Util.Linq;

namespace HomebrewWarlock.Features.Invocations.Lesser
{
    internal static class VoraciousDispel
    {
        [LocalizedString]
        internal const string DisplayName = "Voracious Dispelling";

        [LocalizedString]
        internal const string Description =
            "You can use dispel magic as the spell. Any creature with an active spell effect dispelled by this " +
            "invocation takes 1 point of damage per level of the spell effect (no save).";

        internal static BlueprintInitializationContext.ContextInitializer<BlueprintFeature> Create(BlueprintInitializationContext context)
        {
            var baseAbility = context.CloneBlueprint(BlueprintsDb.Owlcat.BlueprintAbility.DispelMagic,
                GeneratedGuid.Get("VoraciousDispelBaseAbility"),
                nameof(GeneratedGuid.VoraciousDispelBaseAbility))
                .Map(baseAbility =>
                {
                    baseAbility.m_DisplayName = LocalizedStrings.Features_Invocations_Lesser_VoraciousDispel_DisplayName;
                    baseAbility.m_Description = LocalizedStrings.Features_Invocations_Lesser_VoraciousDispel_Description;
                    baseAbility.m_Icon = Sprites.Piercing;

                    foreach (var (index, variant) in baseAbility.AbilityVariants.Variants.Indexed())
                    {
                        var name = variant.name.Replace("DispelMagic", "VoraciousDispel");
                        var copy = AssetUtils.CloneBlueprint(variant, GeneratedGuid.Get(name), name);

                        baseAbility.AbilityVariants.m_Variants[index] = copy.ToReference<BlueprintAbilityReference>();

                        copy.m_Parent = baseAbility.ToReference<BlueprintAbilityReference>();

                        copy.AddInvocationComponents(4);

                        copy.Type = AbilityType.SpellLike;

                        foreach (var dm in copy.GetComponent<AbilityEffectRunAction>().Actions.Actions.OfType<ContextActionDispelMagic>())
                        {
                            dm.OnSuccess.Add(new InvocationComponents.VoraciousDispelDamage());
                        }
                    }

                    return baseAbility;
                });

            var feature = context.NewBlueprint<BlueprintFeature>(
                GeneratedGuid.Get(nameof(VoraciousDispel)),
                nameof(VoraciousDispel))
                .Combine(baseAbility)
                .Map(bps =>
                {
                    var (feature, ability) = bps;

                    feature.m_DisplayName = ability.m_DisplayName;
                    feature.m_Description = ability.m_Description;
                    feature.m_Icon = ability.m_Icon;

                    feature.AddAddFacts(c => c.m_Facts = new[] { ability.ToReference<BlueprintUnitFactReference>() });

                    return feature;
                });

            return feature;
        }
    }
}
