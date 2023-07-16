using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Classes.Spells;
using Kingmaker.ElementsSystem;
using Kingmaker.EntitySystem.Entities;
using Kingmaker.EntitySystem.Stats;
using Kingmaker.RuleSystem;
using Kingmaker.RuleSystem.Rules;
using Kingmaker.RuleSystem.Rules.Damage;
using Kingmaker.UnitLogic.Abilities;
using Kingmaker.UnitLogic.Abilities.Blueprints;
using Kingmaker.UnitLogic.Mechanics;
using Kingmaker.UnitLogic.Mechanics.Actions;
using Kingmaker.UnitLogic.Mechanics.Components;

using MicroWrath;
using MicroWrath.Components;
using MicroWrath.Extensions;
using MicroWrath.Extensions.Components;

namespace HomebrewWarlock.Features.Invocations
{
    internal static class InvocationComponents
    {
        internal static readonly ExtraActivatableAbilityGroup EssenceInvocationAbilityGroup = new(0x8a31cafe);

        internal static void AddInvocationComponents(this BlueprintAbility invocation, int equivalentSpellLevel = 1)
        {
            invocation.AddComponent<ContextCalculateAbilityParams>(c =>
            {
                c.StatType = StatType.Charisma;
                c.ReplaceSpellLevel = true;
                c.SpellLevel = equivalentSpellLevel;
            });

            invocation.AddComponent<ArcaneSpellFailureComponent>();
        }

        internal static RuleDispelMagic[] GetDispelMagic(this MechanicsContext context)
        {           
            var rdms = context.SourceAbilityContext.RulebookContext?.AllEvents?.OfType<RuleDispelMagic>();

            if (rdms is null || !rdms.Any())
            {
                MicroLogger.Debug(() => $"No RuleDispelMagic in {context.SourceAbilityContext?.Name?.ToString() ?? "null"} context");

                return new RuleDispelMagic[0];
            }

            MicroLogger.Debug(sb =>
            {
                sb.AppendLine($"Current Context: {context.Name}");
                sb.AppendLine($"Current AbilityContext: {context.SourceAbilityContext.Name}");
                sb.AppendLine($"SourceAbilityContext.Ability: {context.SourceAbilityContext.Ability}");
                sb.Append("DispelMagic rules:");

                foreach (var rdm in rdms)
                {
                    sb.AppendLine();

                    //var context = rdm.Context;
                    var sourceAbility = rdm.Context?.SourceAbility;
                    
                    sb.AppendLine($"{rdm} ability {sourceAbility?.ToString() ?? "null"}");

                    sb.AppendLine($"Reason.Name: {rdm.Reason.Name}");
                    sb.AppendLine($"Reason.Rule: {rdm.Reason.Rule?.GetType()}");
                    sb.AppendLine($"Reason.Ability: {rdm.Reason.Ability}");
                    sb.AppendLine($"Reason.Fact: {rdm.Reason.Fact}");
                    sb.AppendLine($"Reason.Context.Name: {rdm.Reason.Context?.Name}");
                    sb.AppendLine($"Reason.Ability == Source Ability? {rdm.Reason.Ability == context.SourceAbilityContext?.Ability}");
                }
            });

            return rdms.Where(rdm => rdm.Reason?.Ability == context.SourceAbilityContext.Ability).ToArray();
        }

        internal class VoraciousDispelDamage : ContextAction
        {
            public override string GetCaption() => "Voracious dispel";
            public override void RunAction()
            {
                if (this.Context.GetDispelMagic().FirstOrDefault() is not { } rdm) return;

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
                    MicroLogger.Warning("Null context for RuleDispelMagic effect");

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
    }
}
