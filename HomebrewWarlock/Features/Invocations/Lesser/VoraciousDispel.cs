using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using HomebrewWarlock.Resources;

using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Classes;
using Kingmaker.ElementsSystem;
using Kingmaker.RuleSystem;
using Kingmaker.RuleSystem.Rules.Damage;
using Kingmaker.UnitLogic.Abilities.Blueprints;
using Kingmaker.UnitLogic.Abilities.Components;
using Kingmaker.UnitLogic.Mechanics.Actions;

using MicroWrath.BlueprintInitializationContext;
using MicroWrath.BlueprintsDb;
using MicroWrath.Util.Linq;

namespace HomebrewWarlock.Features.Invocations.Lesser
{
    internal static class VoraciousDispel
    {
        internal class VoraciousDispelDamage : ContextAction
        {
            public override string GetCaption() => "Voracious dispel";
            public override void RunAction()
            {
                if (base.Context.GetDispelMagic().FirstOrDefault() is not { } rdm) return;

                MicroLogger.Debug(sb =>
                {
                    sb.AppendLine("RuleDispelMagic Effect");
                    sb.AppendLine($"Buff: {rdm.Buff}");
                    sb.AppendLine($"Buff Context: {rdm.Buff?.Context?.Name}");
                    sb.AppendLine($"AoE: {rdm.AreaEffect}");
                    sb.AppendLine($"AoE Context: {rdm.AreaEffect?.Context?.Name}");
                });

                if (rdm.Context is not { } dispelContext)
                {
                    MicroLogger.Warning("Null context for RuleDispelMagic");

                    return;
                }

                MicroLogger.Debug(() => $"Dispel Context: {dispelContext.Name}");
                MicroLogger.Debug(() => $"Dispel Context Caster: {dispelContext.MaybeCaster}");
                MicroLogger.Debug(() => $"Dispel Context Owner: {dispelContext.MaybeOwner}");
                MicroLogger.Debug(() => $"SourceAbilityContext: {dispelContext.SourceAbilityContext?.Name}");
                MicroLogger.Debug(() => $"SourceAbilityContext.Ability: {dispelContext.SourceAbilityContext?.Ability?.Name}");
                MicroLogger.Debug(() => $"SourceAbilityContext.Ability.SpellLevel: {dispelContext.SourceAbilityContext?.Ability?.SpellLevel}");

                if (dispelContext.SourceAbilityContext?.Ability is not { } abilityData)
                {
                    MicroLogger.Debug(() => "SourceAbilityContext.Ability is null");
                    return;
                }

                if (dispelContext.MaybeCaster is not { } target)
                {
                    MicroLogger.Debug(() => "Caster is null");
                    return;
                }

                var spellLevel = abilityData.SpellLevel;

                MicroLogger.Debug(() => "Create damage");

                var damage = new UntypedDamage(DiceFormula.Zero, spellLevel)
                {
                    SourceFact = ContextDataHelper.GetFact()
                };

                MicroLogger.Debug(() => "Create damage rule");

                MicroLogger.Debug(() => $"Context: {base.Context.Name}");
                MicroLogger.Debug(() => $"Caster: {base.Context.MaybeCaster}");
                MicroLogger.Debug(() => $"Source Ability: {base.Context.SourceAbility}");

                var rdd = new RuleDealDamage(base.Context.MaybeCaster, target, damage)
                {
                    Reason = new(base.Context)
                };

                MicroLogger.Debug(() => $"Dealing damage to {target}");

                base.Context.TriggerRule(rdd);
            }
        }

        [LocalizedString]
        internal const string DisplayName = "Voracious Dispelling";

        [LocalizedString]
        internal static readonly string Description =
            "<b>Equivalent spell level:</b> 4" +
            Environment.NewLine +
            "You can use dispel magic as the spell. Any creature with an active spell effect dispelled by this " +
            "invocation takes 1 point of damage per level of the spell effect (no save).";

        internal static BlueprintInitializationContext.ContextInitializer<BlueprintFeature> Create(BlueprintInitializationContext context)
        {
            var baseAbility = context.CloneBlueprint(BlueprintsDb.Owlcat.BlueprintAbility.DispelMagic,
                GeneratedGuid.Get("VoraciousDispelBaseAbility"))
                .Map(baseAbility =>
                {
                    baseAbility.m_DisplayName = LocalizedStrings.Features_Invocations_Lesser_VoraciousDispel_DisplayName;
                    baseAbility.m_Description = LocalizedStrings.Features_Invocations_Lesser_VoraciousDispel_Description;
                    baseAbility.m_Icon = Sprites.Piercing;

                    foreach (var (index, variant) in baseAbility.AbilityVariants.Variants.Indexed())
                    {
                        var name = variant.name.Replace("DispelMagic", "VoraciousDispel");
                        var copy = AssetUtils.CloneBlueprint(variant, GeneratedGuid.Get(name), name);

                        baseAbility.AbilityVariants.m_Variants[index] = copy.ToReference();

                        copy.m_Parent = baseAbility.ToReference();

                        copy.AddInvocationComponents(4);

                        copy.Type = AbilityType.SpellLike;

                        foreach (var dm in copy.GetComponent<AbilityEffectRunAction>().Actions.Actions.OfType<ContextActionDispelMagic>())
                        {
                            dm.OnSuccess.Add(new VoraciousDispelDamage());
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
