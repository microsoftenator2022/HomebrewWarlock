using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Classes;
using Kingmaker.UnitLogic.Abilities.Blueprints;
using Kingmaker.UnitLogic.Abilities.Components;
using Kingmaker.UnitLogic.Buffs.Blueprints;
using Kingmaker.UnitLogic.Mechanics;
using Kingmaker.UnitLogic.Mechanics.Actions;

using MicroWrath;
using MicroWrath.BlueprintInitializationContext;
using MicroWrath.BlueprintsDb;
using MicroWrath.Extensions;
using MicroWrath.Extensions.Components;
using MicroWrath.Localization;
using MicroWrath.Util;
using MicroWrath.Util.Linq;

namespace HomebrewWarlock.Features.Invocations.Lesser
{
    internal class CurseOfDespair
    {
        [LocalizedString]
        internal const string DisplayName = "Curse of Despair";
        
        [LocalizedString]
        internal const string Description =
            "You can use this invocation to bestow a curse upon a touched opponent (as bestow curse). If the save " +
            "against this ability succeeds, the creature takes a –1 penalty on attack rolls for 1 minute.";

        internal static BlueprintInitializationContext.ContextInitializer<BlueprintFeature> Create(BlueprintInitializationContext context)
        {
            var buff = context.NewBlueprint<BlueprintBuff>(
                GeneratedGuid.Get("CurseOfDespairBuff"),
                nameof(GeneratedGuid.CurseOfDespairBuff))
                .Map(buff =>
                {
                    buff.AddAddAttackBonus(c => c.Bonus = -1);
                    buff.m_Flags = BlueprintBuff.Flags.Harmful;

                    return buff;
                });

            var baseAbility = context.CloneBlueprint(BlueprintsDb.Owlcat.BlueprintAbility.BestowCurse,
                GeneratedGuid.Get("CurseOfDespairBaseAbility"),
                nameof(GeneratedGuid.CurseOfDespairBaseAbility))
                .Combine(buff)
                .Map(bps =>
                {
                    var (ability, buff) = bps;

                    buff.m_DisplayName = ability.m_DisplayName = LocalizedStrings.Features_Invocations_Lesser_CurseOfDespair_DisplayName;
                    buff.m_Description = ability.m_Description = LocalizedStrings.Features_Invocations_Lesser_CurseOfDespair_Description;
                    buff.m_Icon = ability.m_Icon;

                    foreach (var (index, variantCastAbility) in ability.AbilityVariants.Variants.Indexed())
                    {
                        var castName = variantCastAbility.name.Replace("BestowCurse", "CurseOfDespair");
                        var castCopy = AssetUtils.CloneBlueprint(variantCastAbility, GeneratedGuid.Get(castName), castName);

                        castCopy.m_Parent = ability.ToReference<BlueprintAbilityReference>();
                        castCopy.AddInvocationComponents(4);

                        var effectAbility = castCopy.GetComponent<AbilityEffectStickyTouch>().TouchDeliveryAbility;

                        var effectName = effectAbility.name.Replace("BestowCurse", "CurseOfDespair");
                        var effectCopy = AssetUtils.CloneBlueprint(effectAbility, GeneratedGuid.Get(effectName), effectName);

                        effectCopy.GetComponent<AbilityEffectRunAction>().Actions.Actions
                            .OfType<ContextActionConditionalSaved>()
                            .FirstOrDefault().Succeed.Add(GameActions.ContextActionApplyBuff(a =>
                            {
                                a.m_Buff = buff.ToReference<BlueprintBuffReference>();
                                a.DurationValue.Rate = DurationRate.Minutes;
                                a.DurationValue.BonusValue.Value = 1;
                            }));

                        castCopy.GetComponent<AbilityEffectStickyTouch>().m_TouchDeliveryAbility = effectCopy.ToReference<BlueprintAbilityReference>();

                        ability.AbilityVariants.m_Variants[index] = castCopy.ToReference<BlueprintAbilityReference>();
                    }

                    return ability;
                });

            var feature = context.NewBlueprint<BlueprintFeature>(
                GeneratedGuid.Get("CurseOfDespairFeature"),
                nameof(GeneratedGuid.CurseOfDespairFeature))
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
