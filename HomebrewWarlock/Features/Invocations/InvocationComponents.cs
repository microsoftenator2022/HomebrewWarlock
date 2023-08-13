using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Kingmaker.ElementsSystem;
using Kingmaker.EntitySystem.Stats;
using Kingmaker.RuleSystem;
using Kingmaker.RuleSystem.Rules;
using Kingmaker.RuleSystem.Rules.Damage;
using Kingmaker.UnitLogic.Abilities.Blueprints;
using Kingmaker.UnitLogic.Mechanics;
using Kingmaker.UnitLogic.Mechanics.Actions;
using Kingmaker.UnitLogic.Mechanics.Components;

namespace HomebrewWarlock.Features.Invocations
{
    internal class InvocationComponent : ArcaneSpellFailureComponent { }

    internal class EldritchBlastComponent : InvocationComponent { }

    internal static class InvocationComponents
    {
        internal static readonly ExtraActivatableAbilityGroup EssenceInvocationAbilityGroup = new(0x8a31cafe);

        internal static void AddInvocationComponents<TComponent>(this BlueprintAbility invocation, int equivalentSpellLevel = 1)
            where TComponent : InvocationComponent, new()
        {
            invocation.AddComponent<ContextCalculateAbilityParams>(c =>
            {
                c.StatType = StatType.Charisma;
                c.ReplaceSpellLevel = true;
                c.SpellLevel = equivalentSpellLevel;
            });

            invocation.AddComponent<TComponent>();
        }

        internal static void AddInvocationComponents(this BlueprintAbility invocation, int equivalentSpellLevel = 1) =>
            AddInvocationComponents<InvocationComponent>(invocation, equivalentSpellLevel);

        internal static RuleDispelMagic[] GetDispelMagic(this MechanicsContext context)
        {           
            var rdms = context.SourceAbilityContext.RulebookContext?.AllEvents?.OfType<RuleDispelMagic>();

            if (rdms is null || !rdms.Any())
            {
                MicroLogger.Debug(() => $"No RuleDispelMagic in {context.SourceAbilityContext?.Name?.ToString() ?? "null"} context");

                return Array.Empty<RuleDispelMagic>();
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
    }
}
