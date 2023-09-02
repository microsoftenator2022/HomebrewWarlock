using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Classes;
using Kingmaker.Blueprints.Classes.Spells;
using Kingmaker.UnitLogic.Abilities;
using Kingmaker.UnitLogic.Abilities.Blueprints;
using Kingmaker.UnitLogic.Abilities.Components;
using Kingmaker.UnitLogic.Buffs.Blueprints;
using Kingmaker.UnitLogic.Mechanics;
using Kingmaker.UnitLogic.Mechanics.Actions;

using MicroWrath.BlueprintInitializationContext;
using MicroWrath.BlueprintsDb;
using MicroWrath.Internal.Components;

namespace HomebrewWarlock.Features.Invocations.Greater
{
    internal static class DevourMagic
    {
        internal class DevourMagicAction : ContextAction
        {
            public AbilitySharedValue SharedValue;

            public override void RunAction()
            {
                if (base.Context.MaybeCaster is not { } caster) return;

                var dispelRules = base.Context.GetDispelMagic();

                if (!dispelRules.Any()) return;

                var spellLevel = 0;

                foreach (var dispelRule in dispelRules)
                {
                    if (!dispelRule.Success) continue;

                    if (dispelRule.Context?.SourceAbilityContext?.Ability is not { } dispelled) continue;

                    if (dispelRule.Context.MaybeCaster == caster &&
                        dispelled.Blueprint.Components.OfType<InvocationComponent>().Any())
                        continue;

                    spellLevel = Math.Max(spellLevel, dispelled.SpellLevel);
                }

                base.Context[this.SharedValue] = spellLevel * 5;
            }

            public override string GetCaption() => "Devour Magic";
        }

        [LocalizedString]
        internal const string DisplayName = "Devour Magic";

        [LocalizedString]
        internal static readonly string Description =
            "This invocation allows you to deliver a targeted greater dispel magic with your touch." +
            Environment.NewLine +
            "You gain 5 temporary hit points for each spell level dispelled by this touch. These temporary hit " +
            "points do not stack with other temporary hit points." +
            Environment.NewLine +
            "You cannot devour your own invocations.";

        [LocalizedString]
        internal const string LocalizedDuration = "1 minute";

        internal static BlueprintInitializationContext.ContextInitializer<BlueprintFeature> Create(BlueprintInitializationContext context)
        {
            var buff = context.NewBlueprint<BlueprintBuff>(GeneratedGuid.Get("DevourMagicTempHPBuff"))
                .Map((BlueprintBuff buff) =>
                {
                    buff.AddComponent<ContextAddTemporaryHP>(c =>
                    {
                        c.Value.ValueType = ContextValueType.Shared;
                        c.Value.ValueShared = AbilitySharedValue.StatBonus;
                    });

                    buff.AddSpellDescriptorComponent(c => c.Descriptor = SpellDescriptor.TemporaryHP);

                    buff.m_Flags = BlueprintBuff.Flags.HiddenInUi;

                    return buff;
                });

            var ability = context.CloneBlueprint(BlueprintsDb.Owlcat.BlueprintAbility.DispelMagicGreaterTarget,
                GeneratedGuid.Get("DevourMagicAbility"))
                .Combine(buff)
                .Map(bps =>
                {
                    var (ability, buff) = bps;

                    ability.m_DisplayName = LocalizedStrings.Features_Invocations_Greater_DevourMagic_DisplayName;
                    ability.m_Description = LocalizedStrings.Features_Invocations_Greater_DevourMagic_Description;
                    ability.LocalizedDuration = LocalizedStrings.Features_Invocations_Greater_DevourMagic_LocalizedDuration;

                    ability.Type = AbilityType.SpellLike;
                    ability.Range = AbilityRange.Touch;
                    
                    ability.AddInvocationComponents(6);

                    foreach (var dispel in ability.GetComponent<AbilityEffectRunAction>().Actions.Actions.OfType<ContextActionDispelMagic>())
                    {
                        dispel.OnSuccess.Add(new DevourMagicAction() { SharedValue = AbilitySharedValue.StatBonus },
                            GameActions.ContextActionApplyBuff(a =>
                            {
                                a.ToCaster = true;

                                a.m_Buff = buff.ToReference();
                                
                                a.DurationValue.Rate = DurationRate.Minutes;
                                a.DurationValue.BonusValue = 1;

                                a.IsNotDispelable = true;
                            }));
                    }

                    return ability;
                });

            var feature = context.NewBlueprint<BlueprintFeature>(GeneratedGuid.Get("DevourMagicFeature"))
                .Combine(ability)
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
